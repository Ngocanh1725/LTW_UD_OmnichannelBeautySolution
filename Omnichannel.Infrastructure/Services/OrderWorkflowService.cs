using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.Common;
using Omnichannel.Application.DTOs.Inventory;
using Omnichannel.Application.DTOs.Notifications;
using Omnichannel.Application.DTOs.Sales;
using Omnichannel.Application.Interfaces.Loyalty;
using Omnichannel.Application.Interfaces.Notifications;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Domain.Common;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services
{
    public class OrderWorkflowService : IOrderWorkflowService
    {
        private readonly ApplicationDbContext _context;
        private readonly IInventoryAllocationEngine _invEngine;
        private readonly ICartService _cartService;
        private readonly ILoyaltyService? _loyaltyService;
        private readonly IPromotionEngine? _promotionEngine;
        private readonly INotificationService? _notificationService;
        private readonly IServiceScopeFactory? _scopeFactory;
        private readonly ILogger<OrderWorkflowService> _logger;

        // Khóa đồng thời tối đa 10 đơn đồng thời để bảo vệ phân bổ tồn kho mà không gây nghẽn toàn hệ thống
        private static readonly SemaphoreSlim _orderLock = new(10, 10);

        public OrderWorkflowService(
            ApplicationDbContext context,
            IInventoryAllocationEngine invEngine,
            ICartService cartService,
            ILogger<OrderWorkflowService> logger,
            ILoyaltyService? loyaltyService = null,
            IPromotionEngine? promotionEngine = null,
            INotificationService? notificationService = null,
            IServiceScopeFactory? scopeFactory = null)
        {
            _context = context;
            _invEngine = invEngine;
            _cartService = cartService;
            _logger = logger;
            _loyaltyService = loyaltyService;
            _promotionEngine = promotionEngine;
            _notificationService = notificationService;
            _scopeFactory = scopeFactory;
        }

        public async Task<(bool Success, string Message, OrderSuccessViewModel? Result)> CreateOrderWithSoftReserveAsync(
            string cartId, CheckoutViewModel model, string? customerUserId = null, CancellationToken cancellationToken = default)
        {
            var cart = await _cartService.GetCartAsync(cartId, customerUserId, cancellationToken);
            if (cart == null || !cart.Items.Any())
            {
                return (false, "Giỏ hàng của bạn đang trống.", null);
            }

            bool isStorePickup = string.Equals(model.DeliveryMethod, "STORE_PICKUP", StringComparison.OrdinalIgnoreCase)
                                 || string.Equals(model.ShippingMethod, "STORE_PICKUP", StringComparison.OrdinalIgnoreCase);

            Store? store;
            if (isStorePickup)
            {
                if (!model.PickupStoreId.HasValue || model.PickupStoreId.Value <= 0)
                {
                    return (false, "Vui lòng chọn Showroom nhận hàng (Click & Collect).", null);
                }

                store = await _context.Stores
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == model.PickupStoreId.Value && s.IsActive, cancellationToken);

                if (store == null)
                {
                    return (false, "Showroom nhận hàng đã chọn không tồn tại hoặc đang tạm ngưng hoạt động.", null);
                }
            }
            else
            {
                // Mặc định lấy từ Tổng kho Điều Phối hoặc Kho đầu tiên còn hoạt động
                store = await _context.Stores
                    .AsNoTracking()
                    .Where(s => s.IsActive)
                    .OrderByDescending(s => s.IsHeadquarters)
                    .FirstOrDefaultAsync(cancellationToken);

                if (store == null)
                {
                    return (false, "Không tìm thấy kho hàng hợp lệ để xuất bán.", null);
                }
            }

            await _orderLock.WaitAsync(cancellationToken);
            try
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync<(bool Success, string Message, OrderSuccessViewModel? Result)>(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                    try
                    {
                        // Danh sách lưu trữ kế hoạch phân bổ lô trước khi ghi vào CSDL
                        var allocationPlan = new List<(int ProductId, decimal Price, int BatchId, string BatchCode, int Qty)>();

                        // 1. Kiểm tra tồn khả dụng và lập kế hoạch khóa tạm (Soft Reserve) theo FEFO
                        foreach (var item in cart.Items)
                        {
                            var allocatedBatches = await _invEngine.AllocateBatchesFEFOAsync(store.Id.ToString(), item.ProductId.ToString(), item.Quantity, cancellationToken);
                            if (!allocatedBatches.Any())
                            {
                                await transaction.RollbackAsync(cancellationToken);
                                return (false, $"Sản phẩm '{item.ProductName}' không đủ tồn kho khả dụng để đặt hàng.", null);
                            }

                            foreach (var batch in allocatedBatches)
                            {
                                allocationPlan.Add((item.ProductId, item.UnitPrice, batch.BatchId, batch.BatchNumber, batch.AllocatedQuantity));
                            }
                        }

                        // 2. Tính toán lại chiết khấu khuyến mãi & Coupon
                        int? uId = int.TryParse(customerUserId, out int uid) ? uid : null;
                        decimal subTotal = cart.SubTotal;
                        decimal discountAmount = cart.DiscountAmount;
                        decimal couponDiscount = 0m;

                        if (_promotionEngine != null)
                        {
                            var promoEval = await _promotionEngine.EvaluateCartAsync(cart.Items, model.CouponCode, uId, cancellationToken);
                            discountAmount = promoEval.TotalDiscount;
                            couponDiscount = promoEval.CouponDiscount;
                        }

                        // Chiết khấu điểm thưởng Loyalty (nếu khách hàng chọn sử dụng)
                        decimal loyaltyDiscount = 0m;
                        if (model.UseLoyaltyPoints && model.LoyaltyDiscountAmount > 0)
                        {
                            loyaltyDiscount = model.LoyaltyDiscountAmount;
                            discountAmount += loyaltyDiscount;
                        }

                        // Tính phí giao hàng: Đơn Click & Collect (STORE_PICKUP) miễn phí vận chuyển 100% (ShippingFee = 0)
                        decimal shippingFee = isStorePickup
                            ? 0m
                            : (model.ShippingMethod == "EXPRESS_2H"
                                ? OrderPricingConstants.ExpressShippingFee
                                : OrderPricingConstants.CalculateStandardShippingFee(Math.Max(0m, subTotal - discountAmount)));

                        decimal totalAmount = Math.Max(0, subTotal - discountAmount) + shippingFee;
                        string newOrderId = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

                        string paymentMethodCode = model.PaymentMethod switch
                        {
                            1 => "CASH",
                            2 => "VIETQR",
                            3 => "VNPAY",
                            4 => "COD",
                            5 => "MOMO",
                            _ => "COD"
                        };

                        string? pickupCode = null;
                        DateTime? pickupExpiresAt = null;
                        string effectiveDeliveryMethod = isStorePickup ? "STORE_PICKUP" : "HOME_DELIVERY";
                        string effectiveShippingAddress = isStorePickup
                            ? store.Address
                            : (model.ShippingAddress?.Trim() ?? string.Empty);

                        if (isStorePickup)
                        {
                            // Sinh mã PIN 6 chữ số ngẫu nhiên lưu vào cột PickupCode có cấu trúc (KHÔNG nhồi vào ShippingAddress)
                            pickupCode = Random.Shared.Next(100000, 999999).ToString();
                            pickupExpiresAt = DateTime.UtcNow.AddHours(48);
                        }

                        var order = new Order
                        {
                            OrderCode = newOrderId,
                            OrderSource = "WEBSITE",
                            StoreId = store.Id,
                            DeliveryMethod = effectiveDeliveryMethod,
                            PickupStoreId = isStorePickup ? store.Id : null,
                            PickupCode = pickupCode,
                            PickupExpiresAt = pickupExpiresAt,
                            UserId = uId,
                            CustomerName = model.RecipientName.Trim(),
                            CustomerPhone = model.RecipientPhone.Trim(),
                            CustomerEmail = string.IsNullOrWhiteSpace(model.RecipientEmail) ? null : model.RecipientEmail.Trim(),
                            ShippingAddress = effectiveShippingAddress,
                            Note = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim(),
                            SubTotal = subTotal,
                            DiscountAmount = discountAmount,
                            ShippingFee = shippingFee,
                            TotalAmount = totalAmount,
                            PaymentMethod = paymentMethodCode,
                            PaymentStatus = "UNPAID",
                            OrderStatus = "PENDING",
                            CreatedAt = DateTime.UtcNow
                        };

                        order.AddDomainEvent(new OrderPlacedDomainEvent(
                            order.Id,
                            order.OrderCode,
                            order.UserId,
                            order.StoreId,
                            order.TotalAmount,
                            order.OrderSource));

                        await _context.Orders.AddAsync(order, cancellationToken);
                        await _context.SaveChangesAsync(cancellationToken);

                        // 3. Tăng ReservedQuantity trên từng Lô và thêm OrderDetails
                        foreach (var alloc in allocationPlan)
                        {
                            var inventory = await _context.StoreInventories
                                .FirstOrDefaultAsync(si => si.StoreId == store.Id && si.ProductBatchId == alloc.BatchId, cancellationToken);

                            if (inventory != null)
                            {
                                inventory.ReservedQuantity += alloc.Qty;
                                inventory.LastUpdated = DateTime.UtcNow;
                            }

                            var orderDetail = new OrderDetail
                            {
                                OrderId = order.Id,
                                ProductId = alloc.ProductId,
                                ProductBatchId = alloc.BatchId,
                                Quantity = alloc.Qty,
                                UnitPrice = alloc.Price,
                                DiscountRate = 0,
                                LineTotal = alloc.Price * alloc.Qty
                            };

                            await _context.OrderDetails.AddAsync(orderDetail, cancellationToken);
                        }

                        // Ghi lịch sử trạng thái bước 1 (Đặt hàng thành công)
                        var batchListText = string.Join(", ", allocationPlan.Select(a => $"{a.BatchCode} (x{a.Qty})").Distinct());
                        await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
                        {
                            OrderId = order.Id,
                            Status = "PENDING",
                            Title = isStorePickup ? "Đặt hàng Click & Collect (Đã giữ hàng tại Showroom)" : "Đặt hàng thành công",
                            Description = isStorePickup
                                ? $"Đã khóa tạm lô FEFO ({batchListText}) tại {store.Name}. Hạn nhận hàng trong 48h (đến {pickupExpiresAt!.Value.AddHours(7):HH:mm dd/MM/yyyy})."
                                : $"Hệ thống đã tiếp nhận đơn hàng #{order.OrderCode} và khóa tạm lô FEFO: {batchListText}.",
                            ChangedByUserId = uId,
                            ChangedByName = order.CustomerName,
                            CreatedAt = order.CreatedAt
                        }, cancellationToken);

                        await _context.SaveChangesAsync(cancellationToken);

                        // Ghi nhận sử dụng coupon nếu có
                        if (_promotionEngine != null && !string.IsNullOrWhiteSpace(model.CouponCode) && couponDiscount > 0)
                        {
                            await _promotionEngine.ApplyCouponUsageAsync(model.CouponCode, uId, order.Id, couponDiscount, cancellationToken);
                        }

                        await transaction.CommitAsync(cancellationToken);

                        // Xóa giỏ hàng sau khi đặt thành công
                        await _cartService.ClearCartAsync(cartId, customerUserId, cancellationToken);

                        // 4. Sinh mã VietQR động
                        var vietQr = GenerateVietQr(newOrderId, order.TotalAmount);
                        string estTime = isStorePickup
                            ? $"Nhận tại {store.Name} trong 1 giờ (Giữ hàng 48h đến {pickupExpiresAt!.Value.AddHours(7):HH:mm dd/MM/yyyy})"
                            : (model.ShippingMethod == "EXPRESS_2H"
                                ? "Giao hỏa tốc trong 2 giờ nội thành (Hôm nay)"
                                : "Giao tiêu chuẩn 2 - 3 ngày làm việc");

                        var result = new OrderSuccessViewModel
                        {
                            OrderId = newOrderId,
                            OrderDbId = order.Id,
                            RecipientName = order.CustomerName,
                            RecipientPhone = order.CustomerPhone,
                            CustomerEmail = order.CustomerEmail,
                            ShippingAddress = order.ShippingAddress ?? store.Address,
                            ShippingMethod = isStorePickup ? "STORE_PICKUP" : model.ShippingMethod,
                            DeliveryMethod = effectiveDeliveryMethod,
                            PickupStoreId = order.PickupStoreId,
                            PickupStoreName = isStorePickup ? store.Name : null,
                            PickupStoreAddress = isStorePickup ? store.Address : null,
                            PickupStorePhone = isStorePickup ? store.PhoneNumber : null,
                            PickupCode = order.PickupCode,
                            PickupExpiresAt = order.PickupExpiresAt,
                            EstimatedDeliveryTime = estTime,
                            Notes = order.Note,
                            FinalTotal = order.TotalAmount,
                            PaymentMethod = order.PaymentMethod,
                            PaymentStatus = "UNPAID",
                            OrderStatus = order.OrderStatus,
                            CreatedAt = order.CreatedAt,
                            VietQr = vietQr
                        };

                        _logger.LogInformation("Tạo đơn hàng {OrderId} ({DeliveryMethod}) thành công. Đã khóa tạm tồn kho cho {Count} dòng chi tiết.",
                            newOrderId, effectiveDeliveryMethod, allocationPlan.Count);

                        // Dispatch SignalR realtime notifications (Store Isolation cho đơn Click & Collect)
                        var pickupNotifTitle = isStorePickup
                            ? $"🏬 Đơn Click & Collect Mới: #{order.OrderCode}"
                            : $"📦 Đơn Hàng Mới: #{order.OrderCode}";
                        var pickupNotifMsg = isStorePickup
                            ? $"Khách {order.CustomerName} đặt nhận tại quầy {store.Name} ({order.TotalAmount:N0}₫). Đã giữ lô FEFO {batchListText}."
                            : $"Đơn hàng mới từ {order.CustomerName} ({order.TotalAmount:N0}₫) tại chi nhánh.";
                        var targetPosUrl = isStorePickup
                            ? $"/Admin/Pos?storeId={order.StoreId}&openPickup=1"
                            : $"/Admin/Order/Details/{order.Id}";

                        if (_notificationService != null)
                        {
                            try
                            {
                                await _notificationService.CreateAndSendNotificationAsync(new CreateNotificationDto
                                {
                                    Type = isStorePickup ? "CLICK_AND_COLLECT" : "ORDER",
                                    Severity = "INFO",
                                    Title = pickupNotifTitle,
                                    Message = pickupNotifMsg,
                                    StoreId = order.StoreId,
                                    Role = "Cashier",
                                    Url = targetPosUrl
                                });
                            }
                            catch (Exception syncNotifEx)
                            {
                                _logger.LogWarning(syncNotifEx, "Lỗi khi gửi thông báo trực tiếp tới Cashier cho đơn {OrderCode}", order.OrderCode);
                            }
                        }

                        if (_scopeFactory != null)
                        {
                            var oId = order.Id;
                            var oCode = order.OrderCode;
                            var cName = order.CustomerName;
                            var oTotal = order.TotalAmount;
                            var sId = order.StoreId;
                            var uIdVal = order.UserId;
                            var isPickupFlag = isStorePickup;
                            var storeNameVal = store.Name;

                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    using var scope = _scopeFactory.CreateScope();
                                    var notifService = scope.ServiceProvider.GetService<INotificationService>();
                                    if (notifService == null) return;

                                    // 1. Thông báo cho Admin hệ thống
                                    await notifService.CreateAndSendNotificationAsync(new CreateNotificationDto
                                    {
                                        Type = isPickupFlag ? "CLICK_AND_COLLECT" : "ORDER",
                                        Severity = "INFO",
                                        Title = isPickupFlag ? $"🏬 Click & Collect: #{oCode} ({storeNameVal})" : $"🔔 Đơn Hàng Mới: #{oCode}",
                                        Message = isPickupFlag
                                            ? $"Khách {cName} đặt nhận tại {storeNameVal} ({oTotal:N0}đ)."
                                            : $"Đơn hàng mới từ {cName} ({oTotal:N0}đ) trên hệ thống.",
                                        Role = "Admin",
                                        Url = isPickupFlag ? $"/Admin/Pos?storeId={sId}&openPickup=1" : $"/Admin/Order/Details/{oId}"
                                    });

                                    // 2. Thông báo cho Quản lý cửa hàng (StoreManager - Store Isolation)
                                    await notifService.CreateAndSendNotificationAsync(new CreateNotificationDto
                                    {
                                        Type = isPickupFlag ? "CLICK_AND_COLLECT" : "ORDER",
                                        Severity = "INFO",
                                        Title = pickupNotifTitle,
                                        Message = pickupNotifMsg,
                                        StoreId = sId,
                                        Role = "StoreManager",
                                        Url = targetPosUrl
                                    });

                                    // 3. Thông báo cho Khách hàng đặt mua
                                    if (uIdVal.HasValue)
                                    {
                                        await notifService.CreateAndSendNotificationAsync(new CreateNotificationDto
                                        {
                                            Type = "ORDER",
                                            Severity = "SUCCESS",
                                            Title = isPickupFlag ? $"🎫 Vé Nhận Hàng Tại Quầy: #{oCode}" : $"🎉 Đặt Hàng Thành Công: #{oCode}",
                                            Message = isPickupFlag
                                                ? $"Đơn #{oCode} đã được giữ hàng tại {storeNameVal}. Mã PIN nhận hàng của bạn đã sẵn sàng!"
                                                : $"Đơn hàng #{oCode} giá trị {oTotal:N0}₫ đã được ghi nhận thành công.",
                                            UserId = uIdVal.Value,
                                            Url = $"/Customer/OrderDetail/{oId}"
                                        });
                                    }
                                }
                                catch (Exception notifEx)
                                {
                                    _logger.LogWarning(notifEx, "Lỗi khi gửi notification ngầm cho đơn hàng {OrderId}", oId);
                                }
                            });
                        }

                        return (true, "Đặt hàng thành công! Đang chờ xác nhận.", result);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        _logger.LogError(ex, "Lỗi xảy ra trong quá trình tạo đơn hàng.");
                        return (false, "Lỗi hệ thống trong quá trình đặt hàng: " + ex.Message, null);
                    }
                });
            }
            finally
            {
                _orderLock.Release();
            }
        }

        public async Task<(bool Success, string Message)> ConfirmPaymentAndDeductPhysicalStockAsync(
            BankCallbackDto callback, CancellationToken cancellationToken = default)
        {
            string cleanCbId = callback.OrderId.Trim();
            string codeWithoutOb = cleanCbId.StartsWith("OB ") ? cleanCbId[3..].Trim() : (cleanCbId.StartsWith("OB") ? cleanCbId[2..].Trim() : cleanCbId);

            int? parsedOid = int.TryParse(codeWithoutOb, out int tempOid) ? tempOid : null;

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderCode == cleanCbId
                    || o.OrderCode == codeWithoutOb
                    || o.OrderCode.EndsWith(codeWithoutOb)
                    || (parsedOid.HasValue && o.Id == parsedOid.Value)
                    || (!string.IsNullOrEmpty(callback.TransactionRef) && callback.TransactionRef.Contains(o.OrderCode)), cancellationToken);

            if (order == null)
            {
                return (false, "Không tìm thấy đơn hàng.");
            }

            if (order.PaymentStatus == "PAID")
            {
                return (true, "Đơn hàng này đã được thanh toán trước đó.");
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    // 1. Cập nhật trạng thái đơn hàng: Nếu là đơn Click & Collect chưa bàn giao tại quầy thì giữ ở CONFIRMED để chờ thu ngân POS bàn giao
                    order.PaymentStatus = "PAID";
                    if (string.Equals(order.DeliveryMethod, "STORE_PICKUP", StringComparison.OrdinalIgnoreCase) && !order.PickedUpAt.HasValue)
                    {
                        order.OrderStatus = "CONFIRMED";
                    }
                    else
                    {
                        order.OrderStatus = "COMPLETED";
                    }
                    order.UpdatedAt = DateTime.UtcNow;

                    order.AddDomainEvent(new PaymentConfirmedDomainEvent(
                        order.Id,
                        order.OrderCode,
                        order.UserId,
                        order.StoreId,
                        order.TotalAmount,
                        order.PaymentMethod,
                        callback.TransactionRef));

                    // 2. Trừ cứng tồn kho vật lý (Hard Deduction) và giải phóng tồn tạm khóa
                    foreach (var detail in order.OrderDetails)
                    {
                        var inventory = await _context.StoreInventories
                            .FirstOrDefaultAsync(si => si.StoreId == order.StoreId && si.ProductBatchId == detail.ProductBatchId, cancellationToken);

                        if (inventory != null)
                        {
                            inventory.PhysicalQuantity = Math.Max(0, inventory.PhysicalQuantity - detail.Quantity);
                            inventory.ReservedQuantity = Math.Max(0, inventory.ReservedQuantity - detail.Quantity);
                            inventory.LastUpdated = DateTime.UtcNow;

                            // Ghi sổ cái bất biến InventoryLogs
                            var log = new InventoryLog
                            {
                                StoreId = order.StoreId,
                                ProductBatchId = detail.ProductBatchId ?? 0,
                                TransactionType = "ONLINE_SALE",
                                QuantityChanged = -detail.Quantity,
                                QuantityBefore = inventory.PhysicalQuantity + detail.Quantity,
                                QuantityAfter = inventory.PhysicalQuantity,
                                UserId = order.UserId,
                                CreatedAt = DateTime.UtcNow,
                                Note = $"Xuất kho hoàn tất đơn hàng Online {order.OrderCode}"
                            };

                            await _context.InventoryLogs.AddAsync(log, cancellationToken);
                        }
                    }

                    // Ghi lịch sử trạng thái hoàn tất thanh toán & giao hàng
                    await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
                    {
                        OrderId = order.Id,
                        Status = "COMPLETED",
                        Title = "Giao thành công & Hoàn tất thanh toán",
                        Description = $"Đã xác nhận thanh toán ({order.PaymentMethod}) và chốt xuất kho lô FEFO.",
                        ChangedByUserId = order.UserId,
                        ChangedByName = "Hệ thống Thanh toán Tự động",
                        CreatedAt = DateTime.UtcNow
                    }, cancellationToken);

                    // 3. Tự động phát hành Hóa đơn VAT điện tử (Invoices)
                    decimal totalBeforeTax = Math.Round(order.TotalAmount / 1.1m, 2);
                    decimal vatAmount = order.TotalAmount - totalBeforeTax;

                    var invoice = new Invoice
                    {
                        InvoiceCode = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                        OrderId = order.Id,
                        TaxRate = 10.00m,
                        TaxAmount = vatAmount,
                        TotalAmount = order.TotalAmount,
                        InvoiceDate = DateTime.UtcNow,
                        Status = "ISSUED",
                        CreatedAt = DateTime.UtcNow
                    };

                    await _context.Invoices.AddAsync(invoice, cancellationToken);

                    // 4. Bút toán kép Sổ cái tài chính (FinancialLedgers) & Giá vốn hàng bán (COGS) theo Lô FEFO
                    var revenueLedger = new FinancialLedger
                    {
                        LedgerCode = $"LED-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                        EntryDate = DateTime.UtcNow,
                        EntryType = "REVENUE",
                        StoreId = order.StoreId,
                        OrderId = order.Id,
                        CreditAmount = order.TotalAmount,
                        DebitAmount = 0m,
                        Description = $"Doanh thu bán hàng từ đơn hàng {order.OrderCode} qua {order.PaymentMethod}",
                        CreatedBy = order.UserId,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _context.FinancialLedgers.AddAsync(revenueLedger, cancellationToken);

                    decimal cogsAmount = 0m;
                    foreach (var detail in order.OrderDetails)
                    {
                        if (detail.ProductBatchId.HasValue)
                        {
                            var batch = await _context.ProductBatches
                                .AsNoTracking()
                                .FirstOrDefaultAsync(b => b.Id == detail.ProductBatchId.Value, cancellationToken);
                            if (batch != null && batch.ImportCost > 0)
                            {
                                cogsAmount += (batch.ImportCost * detail.Quantity);
                            }
                        }
                    }

                    if (cogsAmount > 0)
                    {
                        var cogsLedger = new FinancialLedger
                        {
                            LedgerCode = $"LED-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                            EntryDate = DateTime.UtcNow,
                            EntryType = "COGS",
                            StoreId = order.StoreId,
                            OrderId = order.Id,
                            DebitAmount = cogsAmount,
                            CreditAmount = 0m,
                            Description = $"Giá vốn hàng bán (COGS) xuất kho theo lô FEFO cho đơn {order.OrderCode}",
                            CreatedBy = order.UserId,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _context.FinancialLedgers.AddAsync(cogsLedger, cancellationToken);
                    }

                    await _context.SaveChangesAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);
                    _logger.LogInformation("Đã chốt trừ tồn kho, phát hành hóa đơn {InvoiceCode} và ghi nhận sổ cái (Doanh thu={Rev}đ, COGS={Cogs}đ) cho đơn {OrderCode}.",
                        invoice.InvoiceCode, revenueLedger.CreditAmount, cogsAmount, order.OrderCode);

                    // Tự động tích điểm cho khách hàng sau khi thanh toán thành công
                    if (_loyaltyService != null)
                    {
                        int? targetUserId = order.UserId;
                        if (!targetUserId.HasValue && !string.IsNullOrWhiteSpace(order.CustomerPhone))
                        {
                            var userByPhone = await _context.Users.AsNoTracking()
                                .FirstOrDefaultAsync(u => u.PhoneNumber == order.CustomerPhone, cancellationToken);
                            if (userByPhone != null)
                            {
                                targetUserId = userByPhone.Id;
                            }
                        }

                        if (targetUserId.HasValue)
                        {
                            try
                            {
                                await _loyaltyService.EarnPointsAsync(targetUserId.Value, order.Id, order.TotalAmount, cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Lỗi khi tích điểm tự động cho User {UserId} từ đơn hàng {OrderCode}.", targetUserId.Value, order.OrderCode);
                            }
                        }
                    }

                    return (true, "Xác nhận thanh toán và chốt kho thành công!");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Lỗi khi chốt trừ tồn kho cho đơn hàng {OrderId}.", callback.OrderId);
                    return (false, "Lỗi hệ thống khi cập nhật trạng thái đơn.");
                }
            });
        }

        public async Task<bool> CancelOrderAndReleaseReserveAsync(string orderId, string reason, CancellationToken cancellationToken = default)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderCode == orderId, cancellationToken);

            if (order == null || order.OrderStatus == "COMPLETED" || order.OrderStatus == "CANCELLED")
            {
                return false;
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    // Giải phóng ReservedQuantity (hoặc hoàn trả PhysicalQuantity/CurrentQuantity nếu đã trừ) đúng lô FEFO
                    foreach (var detail in order.OrderDetails)
                    {
                        if (!detail.ProductBatchId.HasValue) continue;

                        var inventory = await _context.StoreInventories
                            .FirstOrDefaultAsync(si => si.StoreId == order.StoreId && si.ProductBatchId == detail.ProductBatchId, cancellationToken);

                        var batch = await _context.ProductBatches
                            .FirstOrDefaultAsync(b => b.Id == detail.ProductBatchId.Value, cancellationToken);

                        if (inventory != null)
                        {
                            int qtyBefore = inventory.PhysicalQuantity;
                            if (inventory.ReservedQuantity >= detail.Quantity)
                            {
                                inventory.ReservedQuantity -= detail.Quantity;
                            }
                            else if (inventory.ReservedQuantity > 0)
                            {
                                int fromReserve = inventory.ReservedQuantity;
                                int fromPhysical = detail.Quantity - fromReserve;
                                inventory.ReservedQuantity = 0;
                                inventory.PhysicalQuantity += fromPhysical;
                                if (batch != null)
                                {
                                    batch.CurrentQuantity += fromPhysical;
                                }
                            }
                            else
                            {
                                // Trường hợp đơn đã trừ trực tiếp vào PhysicalQuantity / CurrentQuantity
                                inventory.PhysicalQuantity += detail.Quantity;
                                if (batch != null)
                                {
                                    batch.CurrentQuantity += detail.Quantity;
                                }
                            }

                            inventory.LastUpdated = DateTime.UtcNow;

                            await _context.InventoryLogs.AddAsync(new InventoryLog
                            {
                                StoreId = order.StoreId,
                                ProductBatchId = detail.ProductBatchId.Value,
                                TransactionType = "ORDER_CANCEL_RESTORE",
                                QuantityChanged = detail.Quantity,
                                QuantityBefore = qtyBefore,
                                QuantityAfter = inventory.PhysicalQuantity,
                                UserId = order.UserId,
                                CreatedAt = DateTime.UtcNow,
                                Note = $"Hoàn tồn lô FEFO do hủy đơn {order.OrderCode}: {reason}"
                            }, cancellationToken);
                        }
                        else if (batch != null)
                        {
                            batch.CurrentQuantity += detail.Quantity;
                        }
                    }

                    order.OrderStatus = "CANCELLED";
                    order.UpdatedAt = DateTime.UtcNow;

                    await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
                    {
                        OrderId = order.Id,
                        Status = "CANCELLED",
                        Title = "Đã hủy đơn hàng & Hoàn tồn lô FEFO",
                        Description = $"Lý do hủy: {reason}. Toàn bộ số lượng tồn kho đã được hoàn trả về đúng lô FEFO ban đầu.",
                        ChangedByUserId = order.UserId,
                        ChangedByName = "Hệ thống / Quản trị viên",
                        CreatedAt = DateTime.UtcNow
                    }, cancellationToken);

                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Lỗi khi hủy đơn hàng và hoàn tồn tạm.");
                    return false;
                }
            });
        }

        public async Task<List<StorePickupCandidateDto>> GetStorePickupAvailabilityForCartAsync(
            string cartId, string? customerUserId = null, CancellationToken cancellationToken = default)
        {
            var cart = await _cartService.GetCartAsync(cartId, customerUserId, cancellationToken);
            var stores = await _context.Stores
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.IsHeadquarters)
                .ThenBy(s => s.Id)
                .ToListAsync(cancellationToken);

            if (cart == null || !cart.Items.Any())
            {
                return stores.Select(s => new StorePickupCandidateDto
                {
                    StoreId = s.Id,
                    StoreCode = s.Code,
                    StoreName = s.Name,
                    Address = s.Address,
                    PhoneNumber = s.PhoneNumber ?? "1900 8888",
                    HasFullCartStock = false
                }).ToList();
            }

            var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
            var now = DateTime.UtcNow;

            var storeInventories = await _context.StoreInventories
                .AsNoTracking()
                .Include(si => si.ProductBatch)
                .Where(si => productIds.Contains(si.ProductBatch.ProductId)
                             && si.ProductBatch.Status != "EXPIRED"
                             && si.ProductBatch.ExpDate > now
                             && (si.PhysicalQuantity - si.ReservedQuantity) > 0)
                .ToListAsync(cancellationToken);

            var candidates = new List<StorePickupCandidateDto>();

            foreach (var s in stores)
            {
                var missingNames = new List<string>();
                int totalAvailableUnits = 0;
                DateTime? earliestExp = null;
                string? earliestBatchCode = null;

                foreach (var item in cart.Items)
                {
                    var matchingBatches = storeInventories
                        .Where(si => si.StoreId == s.Id && si.ProductBatch.ProductId == item.ProductId)
                        .OrderBy(si => si.ProductBatch.ExpDate)
                        .ToList();

                    int availableForProduct = matchingBatches.Sum(si => Math.Max(0, si.PhysicalQuantity - si.ReservedQuantity));
                    totalAvailableUnits += availableForProduct;

                    if (availableForProduct < item.Quantity)
                    {
                        missingNames.Add(item.ProductName);
                    }
                    else if (matchingBatches.Count > 0)
                    {
                        var firstFefo = matchingBatches[0].ProductBatch;
                        if (!earliestExp.HasValue || firstFefo.ExpDate < earliestExp.Value)
                        {
                            earliestExp = firstFefo.ExpDate;
                            earliestBatchCode = firstFefo.BatchNumber;
                        }
                    }
                }

                candidates.Add(new StorePickupCandidateDto
                {
                    StoreId = s.Id,
                    StoreCode = s.Code,
                    StoreName = s.Name,
                    Address = s.Address,
                    PhoneNumber = s.PhoneNumber ?? "1900 8888",
                    OpeningHours = "08:30 – 22:00 (Mở cửa cả Thứ 7 & Chủ Nhật)",
                    HasFullCartStock = missingNames.Count == 0,
                    TotalAvailableUnitsForCart = totalAvailableUnits,
                    EarliestFefoBatchCode = earliestBatchCode,
                    EarliestFefoExpDate = earliestExp,
                    MissingProductNames = missingNames
                });
            }

            return candidates
                .OrderByDescending(c => c.HasFullCartStock)
                .ThenByDescending(c => c.TotalAvailableUnitsForCart)
                .ToList();
        }

        public async Task<(bool Success, string Message, OrderSuccessViewModel? HandedOverOrder)> HandoverPickupOrderAtPosAsync(
            HandoverPickupOrderRequestDto request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return (false, "Yêu cầu bàn giao không hợp lệ.", null);
            }

            var orderQuery = _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductBatch)
                .Include(o => o.Store)
                .Include(o => o.PickupStore)
                .Include(o => o.Invoice)
                .AsQueryable();

            Order? order = null;
            if (request.OrderId > 0)
            {
                order = await orderQuery.FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);
            }
            if (order == null && !string.IsNullOrWhiteSpace(request.OrderCode))
            {
                var cleanCode = request.OrderCode.Trim();
                order = await orderQuery.FirstOrDefaultAsync(o => o.OrderCode == cleanCode, cancellationToken);
            }

            if (order == null)
            {
                return (false, "Không tìm thấy đơn hàng Click & Collect cần bàn giao.", null);
            }

            if (!string.Equals(order.DeliveryMethod, "STORE_PICKUP", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Đơn hàng này không phải là đơn Nhận tại cửa hàng (Click & Collect).", null);
            }

            // 1. Kiểm tra Store Isolation: Nhân viên cửa hàng khác không được phép xem/bàn giao đơn của cửa hàng này
            int targetStoreId = order.PickupStoreId ?? order.StoreId;
            if (request.CashierStoreId > 0 && request.CashierStoreId != targetStoreId)
            {
                return (false, "Bạn không có quyền bàn giao đơn Click & Collect thuộc chi nhánh Showroom khác (Store Isolation).", null);
            }

            // 2. Chống bàn giao lại đơn đã hoàn tất hoặc đã hủy (đảm bảo tồn kho chỉ trừ 1 lần duy nhất)
            if (order.OrderStatus == "COMPLETED" || order.PickedUpAt.HasValue)
            {
                return (false, $"Đơn hàng #{order.OrderCode} đã được bàn giao trước đó.", null);
            }

            if (order.OrderStatus == "CANCELLED")
            {
                return (false, $"Đơn hàng #{order.OrderCode} đã bị hủy, không thể bàn giao.", null);
            }

            if (order.PickupExpiresAt.HasValue && DateTime.UtcNow > order.PickupExpiresAt.Value)
            {
                return (false, $"Đơn hàng #{order.OrderCode} đã quá thời hạn giữ hàng 48 giờ tại Showroom.", null);
            }

            // 3. Xác thực mã PIN 6 số (PickupCode)
            var inputPin = (request.PickupCode ?? string.Empty).Trim();
            if (inputPin.StartsWith("OB-", StringComparison.OrdinalIgnoreCase))
            {
                inputPin = inputPin[3..].Trim();
            }

            var expectedPin = (order.PickupCode ?? string.Empty).Trim();
            if (expectedPin.StartsWith("OB-", StringComparison.OrdinalIgnoreCase))
            {
                expectedPin = expectedPin[3..].Trim();
            }

            if (string.IsNullOrWhiteSpace(inputPin) || !string.Equals(inputPin, expectedPin, StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Mã PIN nhận hàng 6 số không chính xác! Vui lòng đối chiếu lại Vé QR của khách hàng.", null);
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync<(bool Success, string Message, OrderSuccessViewModel? HandedOverOrder)>(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    bool wasAlreadyPaidOnline = string.Equals(order.PaymentStatus, "PAID", StringComparison.OrdinalIgnoreCase);

                    // 4. Bất biến tồn kho: Chỉ trừ PhysicalQuantity & giải phóng ReservedQuantity nếu đơn chưa trừ cứng trước đó
                    if (!wasAlreadyPaidOnline)
                    {
                        foreach (var detail in order.OrderDetails)
                        {
                            var inventory = await _context.StoreInventories
                                .FirstOrDefaultAsync(si => si.StoreId == targetStoreId && si.ProductBatchId == detail.ProductBatchId, cancellationToken);

                            if (inventory != null)
                            {
                                int qtyBefore = inventory.PhysicalQuantity;
                                inventory.PhysicalQuantity = Math.Max(0, inventory.PhysicalQuantity - detail.Quantity);
                                inventory.ReservedQuantity = Math.Max(0, inventory.ReservedQuantity - detail.Quantity);
                                inventory.LastUpdated = DateTime.UtcNow;

                                await _context.InventoryLogs.AddAsync(new InventoryLog
                                {
                                    StoreId = targetStoreId,
                                    ProductBatchId = detail.ProductBatchId ?? 0,
                                    TransactionType = "CLICK_AND_COLLECT_HANDOVER",
                                    QuantityChanged = -detail.Quantity,
                                    QuantityBefore = qtyBefore,
                                    QuantityAfter = inventory.PhysicalQuantity,
                                    UserId = request.CashierUserId ?? order.UserId,
                                    CreatedAt = DateTime.UtcNow,
                                    Note = $"Xuất kho bàn giao đơn Click & Collect #{order.OrderCode} tại quầy POS"
                                }, cancellationToken);
                            }

                            if (detail.ProductBatchId.HasValue)
                            {
                                var batch = await _context.ProductBatches
                                    .FirstOrDefaultAsync(b => b.Id == detail.ProductBatchId.Value, cancellationToken);
                                if (batch != null)
                                {
                                    batch.CurrentQuantity = Math.Max(0, batch.CurrentQuantity - detail.Quantity);
                                }
                            }
                        }
                    }

                    // 5. Ghi nhận vào Ca làm việc POS (PosShift) đang mở tại Showroom
                    PosShift? activeShift = null;
                    if (request.PosShiftId.HasValue && request.PosShiftId.Value > 0)
                    {
                        activeShift = await _context.PosShifts
                            .FirstOrDefaultAsync(s => s.Id == request.PosShiftId.Value && s.StoreId == targetStoreId, cancellationToken);
                    }
                    activeShift ??= await _context.PosShifts
                        .FirstOrDefaultAsync(s => s.StoreId == targetStoreId && s.Status == "OPEN", cancellationToken);

                    if (activeShift != null)
                    {
                        order.PosShiftId = activeShift.Id;
                        activeShift.TransactionCount += 1;
                        if (!wasAlreadyPaidOnline)
                        {
                            activeShift.CashSalesTotal += order.TotalAmount;
                            activeShift.ExpectedCash += order.TotalAmount;
                        }
                    }

                    // 6. Chuyển trạng thái đơn sang COMPLETED (Đã nhận hàng)
                    order.PaymentStatus = "PAID";
                    order.OrderStatus = "COMPLETED";
                    order.PickedUpAt = DateTime.UtcNow;
                    order.UpdatedAt = DateTime.UtcNow;

                    var storeName = order.PickupStore?.Name ?? order.Store?.Name ?? $"Showroom #{targetStoreId}";
                    var cashierDisplay = !string.IsNullOrWhiteSpace(request.CashierName) ? request.CashierName : "Thu ngân POS";

                    await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
                    {
                        OrderId = order.Id,
                        Status = "COMPLETED",
                        Title = "Đã nhận hàng tại Showroom (Click & Collect)",
                        Description = $"Thu ngân {cashierDisplay} đã xác thực mã PIN 6 số và bàn giao hàng thành công tại {storeName}.",
                        ChangedByUserId = request.CashierUserId,
                        ChangedByName = cashierDisplay,
                        CreatedAt = DateTime.UtcNow
                    }, cancellationToken);

                    // 7. Phát hành Hóa đơn & Bút toán Sổ cái nếu chưa phát hành lúc thanh toán online
                    if (order.Invoice == null)
                    {
                        decimal totalBeforeTax = Math.Round(order.TotalAmount / 1.1m, 2);
                        decimal vatAmount = order.TotalAmount - totalBeforeTax;

                        await _context.Invoices.AddAsync(new Invoice
                        {
                            InvoiceCode = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                            OrderId = order.Id,
                            CashierId = request.CashierUserId,
                            PaymentMethod = order.PaymentMethod,
                            TaxRate = 10.00m,
                            TaxAmount = vatAmount,
                            TotalAmount = order.TotalAmount,
                            InvoiceDate = DateTime.UtcNow,
                            Status = "ISSUED",
                            CreatedAt = DateTime.UtcNow
                        }, cancellationToken);
                    }

                    if (!wasAlreadyPaidOnline)
                    {
                        await _context.FinancialLedgers.AddAsync(new FinancialLedger
                        {
                            LedgerCode = $"LED-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                            EntryDate = DateTime.UtcNow,
                            EntryType = "REVENUE",
                            StoreId = targetStoreId,
                            OrderId = order.Id,
                            CreditAmount = order.TotalAmount,
                            DebitAmount = 0m,
                            Description = $"Doanh thu bàn giao đơn Click & Collect #{order.OrderCode} tại quầy POS ({storeName})",
                            CreatedBy = request.CashierUserId ?? order.UserId,
                            CreatedAt = DateTime.UtcNow
                        }, cancellationToken);
                    }

                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    // 8. Cộng điểm thưởng Loyalty cho khách hàng (nếu chưa cộng lúc thanh toán online)
                    int pointsEarned = 0;
                    if (!wasAlreadyPaidOnline && _loyaltyService != null)
                    {
                        int? targetUserId = order.UserId;
                        if (!targetUserId.HasValue && !string.IsNullOrWhiteSpace(order.CustomerPhone))
                        {
                            var userByPhone = await _context.Users.AsNoTracking()
                                .FirstOrDefaultAsync(u => u.PhoneNumber == order.CustomerPhone, cancellationToken);
                            targetUserId = userByPhone?.Id;
                        }

                        if (targetUserId.HasValue)
                        {
                            try
                            {
                                var earnRes = await _loyaltyService.EarnPointsAsync(targetUserId.Value, order.Id, order.TotalAmount, cancellationToken);
                                pointsEarned = earnRes?.PointsEarned ?? 0;
                            }
                            catch (Exception loyaltyEx)
                            {
                                _logger.LogWarning(loyaltyEx, "Lỗi khi cộng điểm Loyalty cho đơn Click & Collect #{OrderCode}", order.OrderCode);
                            }
                        }
                    }

                    // 9. Đẩy thông báo Realtime SignalR cho khách hàng
                    if (_notificationService != null && order.UserId.HasValue)
                    {
                        try
                        {
                            await _notificationService.CreateAndSendNotificationAsync(new CreateNotificationDto
                            {
                                Type = "ORDER",
                                Severity = "SUCCESS",
                                Title = $"🎉 Đã Nhận Hàng Tại Quầy: #{order.OrderCode}",
                                Message = $"Bạn đã nhận hàng thành công tại {storeName}. Đơn hàng #{order.OrderCode} đã hoàn tất{(pointsEarned > 0 ? $" và cộng +{pointsEarned} điểm thưởng Loyalty" : "")}!",
                                UserId = order.UserId.Value,
                                Url = $"/Customer/OrderDetail/{order.Id}"
                            });
                        }
                        catch (Exception notifEx)
                        {
                            _logger.LogWarning(notifEx, "Lỗi gửi thông báo hoàn tất Click & Collect cho khách {UserId}", order.UserId.Value);
                        }
                    }

                    var summaryVm = new OrderSuccessViewModel
                    {
                        OrderId = order.OrderCode,
                        OrderDbId = order.Id,
                        RecipientName = order.CustomerName,
                        RecipientPhone = order.CustomerPhone,
                        ShippingAddress = order.ShippingAddress ?? storeName,
                        DeliveryMethod = order.DeliveryMethod,
                        PickupStoreId = targetStoreId,
                        PickupStoreName = storeName,
                        PickupCode = order.PickupCode,
                        PickedUpAt = order.PickedUpAt,
                        FinalTotal = order.TotalAmount,
                        PaymentMethod = order.PaymentMethod,
                        PaymentStatus = order.PaymentStatus,
                        OrderStatus = order.OrderStatus,
                        CreatedAt = order.CreatedAt
                    };

                    return (true, $"Xác thực mã PIN thành công! Đã bàn giao đơn #{order.OrderCode} tại {storeName}, ghi nhận vào ca POS và cộng điểm thành viên.", summaryVm);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Lỗi khi bàn giao đơn Click & Collect #{OrderCode} tại POS.", order.OrderCode);
                    return (false, "Lỗi hệ thống khi bàn giao đơn hàng: " + ex.Message, null);
                }
            });
        }

        public async Task<int> ReleaseExpiredPickupOrdersAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var expiredOrders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.DeliveryMethod == "STORE_PICKUP"
                            && o.OrderStatus != "COMPLETED"
                            && o.OrderStatus != "CANCELLED"
                            && o.PickupExpiresAt != null
                            && o.PickupExpiresAt < now)
                .Select(o => o.OrderCode)
                .ToListAsync(cancellationToken);

            int releasedCount = 0;
            foreach (var code in expiredOrders)
            {
                bool cancelled = await CancelOrderAndReleaseReserveAsync(
                    code,
                    "Tự động hủy giữ hàng do quá thời hạn 48 giờ nhận tại Showroom (Click & Collect Expired)",
                    cancellationToken);
                if (cancelled)
                {
                    releasedCount++;
                }
            }

            return releasedCount;
        }

        private static VietQrPaymentDto GenerateVietQr(string orderId, decimal amount)
        {
            string bankId = "MB";
            string accountNo = "0909999888";
            string accountName = "CONG TY TNHH OMNICHANNEL BEAUTY";
            string shortCode = orderId.Contains('-') ? orderId.Split('-')[^1] : orderId;
            string memo = $"OB {shortCode}";

            // Link API VietQR chuẩn Napas 247 tạo ảnh QR động
            string qrUrl = $"https://img.vietqr.io/image/{bankId}-{accountNo}-compact2.png?amount={(long)amount}&addInfo={Uri.EscapeDataString(memo)}&accountName={Uri.EscapeDataString(accountName)}";

            return new VietQrPaymentDto
            {
                BankName = "MBBank (Ngân Hàng Quân Đội)",
                BankAccountNo = accountNo,
                AccountName = "CÔNG TY TNHH OMNICHANNEL BEAUTY",
                Amount = amount,
                OrderInfo = memo,
                QrImageUrl = qrUrl,
                ExpireSeconds = 900 // 15 phút đếm ngược
            };
        }
    }
}
