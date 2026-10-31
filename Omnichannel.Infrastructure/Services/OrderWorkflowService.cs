using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Sales;
using Omnichannel.Application.DTOs.Inventory;
using Omnichannel.Application.Interfaces.Loyalty;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

using Omnichannel.Application.Interfaces.Notifications;
using Omnichannel.Application.DTOs.Notifications;
using Omnichannel.Domain.Common;

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
        private readonly ILogger<OrderWorkflowService> _logger;

        // Đối tượng Semaphore khóa cục bộ chống Race Condition khi đặt cùng sản phẩm
        private static readonly SemaphoreSlim _orderLock = new(1, 1);

        public OrderWorkflowService(
            ApplicationDbContext context,
            IInventoryAllocationEngine invEngine,
            ICartService cartService,
            ILogger<OrderWorkflowService> logger,
            ILoyaltyService? loyaltyService = null,
            IPromotionEngine? promotionEngine = null,
            INotificationService? notificationService = null)
        {
            _context = context;
            _invEngine = invEngine;
            _cartService = cartService;
            _logger = logger;
            _loyaltyService = loyaltyService;
            _promotionEngine = promotionEngine;
            _notificationService = notificationService;
        }

        public async Task<(bool Success, string Message, OrderSuccessViewModel? Result)> CreateOrderWithSoftReserveAsync(
            string cartId, CheckoutViewModel model, string? customerUserId = null, CancellationToken cancellationToken = default)
        {
            var cart = await _cartService.GetCartAsync(cartId, customerUserId, cancellationToken);
            if (cart == null || !cart.Items.Any())
            {
                return (false, "Giỏ hàng của bạn đang trống.", null);
            }

            // Mặc định lấy từ Tổng kho Điều Phối hoặc Kho đầu tiên còn hoạt động
            var store = await _context.Stores
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderByDescending(s => s.IsHeadquarters)
                .FirstOrDefaultAsync(cancellationToken);

            if (store == null)
            {
                return (false, "Không tìm thấy kho hàng hợp lệ để xuất bán.", null);
            }

            await _orderLock.WaitAsync(cancellationToken);
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Danh sách lưu trữ kế hoạch phân bổ lô trước khi ghi vào CSDL
                var allocationPlan = new List<(int ProductId, decimal Price, int BatchId, int Qty)>();

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
                        allocationPlan.Add((item.ProductId, item.UnitPrice, batch.BatchId, batch.AllocatedQuantity));
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

                // Tính phí giao hàng theo phương thức vận chuyển: Tiêu chuẩn (30k/Freeship) hoặc Tốc hành 2H (45k)
                decimal shippingFee = 0m;
                if (model.ShippingMethod == "EXPRESS_2H")
                {
                    shippingFee = 45000m;
                }
                else
                {
                    shippingFee = (Math.Max(0, subTotal - discountAmount) >= 500000) ? 0m : 30000m;
                }

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

                var order = new Order
                {
                    OrderCode = newOrderId,
                    OrderSource = "WEBSITE", 
                    StoreId = store.Id,
                    UserId = uId,
                    CustomerName = model.RecipientName.Trim(),
                    CustomerPhone = model.RecipientPhone.Trim(),
                    CustomerEmail = string.IsNullOrWhiteSpace(model.RecipientEmail) ? null : model.RecipientEmail.Trim(),
                    ShippingAddress = model.ShippingAddress.Trim(),
                    Note = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim(),
                    SubTotal = subTotal,
                    DiscountAmount = discountAmount,
                    ShippingFee = shippingFee,
                    TotalAmount = totalAmount,
                    PaymentMethod = paymentMethodCode,
                    PaymentStatus = "UNPAID", 
                    OrderStatus = "PROCESSING",   
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
                string estTime = model.ShippingMethod == "EXPRESS_2H"
                    ? "Giao hỏa tốc trong 2 giờ nội thành (Hôm nay)"
                    : "Giao tiêu chuẩn 2 - 3 ngày làm việc";

                var result = new OrderSuccessViewModel
                {
                    OrderId = newOrderId,
                    OrderDbId = order.Id,
                    RecipientName = order.CustomerName,
                    RecipientPhone = order.CustomerPhone,
                    CustomerEmail = order.CustomerEmail,
                    ShippingAddress = order.ShippingAddress,
                    ShippingMethod = model.ShippingMethod,
                    EstimatedDeliveryTime = estTime,
                    Notes = order.Note,
                    FinalTotal = order.TotalAmount,
                    PaymentMethod = order.PaymentMethod,
                    PaymentStatus = "UNPAID",
                    OrderStatus = "PROCESSING",
                    CreatedAt = order.CreatedAt,
                    VietQr = vietQr
                };

                _logger.LogInformation("Tạo đơn hàng {OrderId} thành công. Đã khóa tạm tồn kho cho {Count} dòng chi tiết.",
                    newOrderId, allocationPlan.Count);

                // Trigger SignalR realtime notifications cho Admin, StoreManager, Cashier và Khách hàng
                if (_notificationService != null)
                {
                    try
                    {
                        // 1. Thông báo cho Admin hệ thống
                        await _notificationService.CreateAndSendNotificationAsync(new CreateNotificationDto
                        {
                            Type = "ORDER",
                            Severity = "INFO",
                            Title = $"🔔 Đơn Hàng Mới: #{order.OrderCode}",
                            Message = $"Đơn hàng mới từ {order.CustomerName} ({order.TotalAmount:N0}đ) trên hệ thống.",
                            Role = "Admin",
                            Url = $"/Admin/Order/Details/{order.Id}"
                        }, cancellationToken);

                        // 2. Thông báo cho Quản lý cửa hàng (StoreManager)
                        await _notificationService.CreateAndSendNotificationAsync(new CreateNotificationDto
                        {
                            Type = "ORDER",
                            Severity = "INFO",
                            Title = $"📦 Đơn Hàng Mới: #{order.OrderCode}",
                            Message = $"Đơn hàng mới từ {order.CustomerName} ({order.TotalAmount:N0}đ) tại chi nhánh.",
                            StoreId = order.StoreId,
                            Role = "StoreManager",
                            Url = $"/Admin/Order/Details/{order.Id}"
                        }, cancellationToken);

                        // 3. Thông báo cho Thu ngân (Cashier)
                        await _notificationService.CreateAndSendNotificationAsync(new CreateNotificationDto
                        {
                            Type = "ORDER",
                            Severity = "INFO",
                            Title = $"📦 Đơn Hàng Mới: #{order.OrderCode}",
                            Message = $"Đơn hàng mới từ {order.CustomerName} ({order.TotalAmount:N0}đ) tại chi nhánh.",
                            StoreId = order.StoreId,
                            Role = "Cashier",
                            Url = $"/Admin/Order/Details/{order.Id}"
                        }, cancellationToken);

                        // 4. Thông báo cho Khách hàng đặt mua (Storefront real-time notification)
                        if (order.UserId.HasValue)
                        {
                            await _notificationService.CreateAndSendNotificationAsync(new CreateNotificationDto
                            {
                                Type = "ORDER",
                                Severity = "SUCCESS",
                                Title = $"🎉 Đặt Hàng Thành Công: #{order.OrderCode}",
                                Message = $"Đơn hàng #{order.OrderCode} giá trị {order.TotalAmount:N0}₫ đã được ghi nhận thành công.",
                                UserId = order.UserId.Value,
                                Url = "/Customer/Orders"
                            }, cancellationToken);
                        }
                    }
                    catch (Exception notifEx)
                    {
                        _logger.LogWarning(notifEx, "Không thể gửi notification cho đơn hàng mới {OrderId}", order.Id);
                    }
                }

                return (true, "Đặt hàng thành công! Đang chờ thanh toán.", result);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình tạo đơn hàng.");
                return (false, "Lỗi hệ thống trong quá trình đặt hàng. Vui lòng thử lại.", null);
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

            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // 1. Cập nhật trạng thái đơn hàng
                order.PaymentStatus = "PAID"; 
                order.OrderStatus = "COMPLETED";   
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
                        inventory.PhysicalQuantity -= detail.Quantity;
                        inventory.ReservedQuantity -= detail.Quantity;
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

            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Giải phóng ReservedQuantity nếu đơn đang ở trạng thái đã khóa tồn
                if (order.OrderStatus == "PROCESSING")
                {
                    foreach (var detail in order.OrderDetails)
                    {
                        var inventory = await _context.StoreInventories
                            .FirstOrDefaultAsync(si => si.StoreId == order.StoreId && si.ProductBatchId == detail.ProductBatchId, cancellationToken);

                        if (inventory != null)
                        {
                            inventory.ReservedQuantity = Math.Max(0, inventory.ReservedQuantity - detail.Quantity);
                            inventory.LastUpdated = DateTime.UtcNow;
                        }
                    }
                }

                order.OrderStatus = "CANCELLED"; 
                order.UpdatedAt = DateTime.UtcNow;

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
