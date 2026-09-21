using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Sales;
using Omnichannel.Application.DTOs.Inventory;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services
{
    public class OrderWorkflowService : IOrderWorkflowService
    {
        private readonly ApplicationDbContext _context;
        private readonly IInventoryAllocationEngine _invEngine;
        private readonly ICartService _cartService;
        private readonly ILogger<OrderWorkflowService> _logger;

        // Đối tượng Semaphore khóa cục bộ chống Race Condition khi đặt cùng sản phẩm
        private static readonly SemaphoreSlim _orderLock = new(1, 1);

        public OrderWorkflowService(
            ApplicationDbContext context,
            IInventoryAllocationEngine invEngine,
            ICartService cartService,
            ILogger<OrderWorkflowService> logger)
        {
            _context = context;
            _invEngine = invEngine;
            _cartService = cartService;
            _logger = logger;
        }

        public async Task<(bool Success, string Message, OrderSuccessViewModel? Result)> CreateOrderWithSoftReserveAsync(
            string cartId, CheckoutViewModel model, string? customerUserId = null, CancellationToken cancellationToken = default)
        {
            var cart = await _cartService.GetCartAsync(cartId, customerUserId, cancellationToken);
            if (!cart.Items.Any())
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

                // 2. Tạo đơn hàng mới
                string newOrderId = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

                int? uId = int.TryParse(customerUserId, out int uid) ? uid : null;
                var order = new Order
                {
                    OrderCode = newOrderId,
                    OrderSource = "WEBSITE", 
                    StoreId = store.Id,
                    UserId = uId,
                    CustomerName = model.RecipientName.Trim(),
                    CustomerPhone = model.RecipientPhone.Trim(),
                    ShippingAddress = model.ShippingAddress.Trim(),
                    SubTotal = cart.SubTotal,
                    DiscountAmount = cart.DiscountAmount,
                    ShippingFee = 0,
                    TotalAmount = cart.TotalAmount,
                    PaymentMethod = model.PaymentMethod.ToString(),
                    PaymentStatus = "UNPAID", 
                    OrderStatus = "PROCESSING",   
                    CreatedAt = DateTime.UtcNow
                };

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
                await transaction.CommitAsync(cancellationToken);

                // Xóa giỏ hàng sau khi đặt thành công
                await _cartService.ClearCartAsync(cartId, customerUserId, cancellationToken);

                // 4. Sinh mã VietQR động
                var vietQr = GenerateVietQr(newOrderId, order.TotalAmount);

                var result = new OrderSuccessViewModel
                {
                    OrderId = newOrderId,
                    RecipientName = order.CustomerName,
                    RecipientPhone = order.CustomerPhone,
                    ShippingAddress = order.ShippingAddress,
                    FinalTotal = order.TotalAmount,
                    PaymentMethod = order.PaymentMethod,
                    PaymentStatus = "UNPAID",
                    OrderStatus = "PROCESSING",
                    CreatedAt = order.CreatedAt,
                    VietQr = vietQr
                };

                _logger.LogInformation("Tạo đơn hàng {OrderId} thành công. Đã khóa tạm tồn kho cho {Count} dòng chi tiết.",
                    newOrderId, allocationPlan.Count);

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
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderCode == callback.OrderId, cancellationToken);

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
                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Đã chốt trừ tồn kho và phát hành hóa đơn {InvoiceCode} cho đơn hàng {OrderCode}.",
                    invoice.InvoiceCode, order.OrderCode);

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
            string accountName = "OMNICHANNEL BEAUTY";
            string content = orderId;

            // Link API VietQR chuẩn tạo ảnh QR động
            string qrUrl = $"https://img.vietqr.io/image/{bankId}-{accountNo}-compact2.png?amount={(long)amount}&addInfo={content}&accountName={Uri.EscapeDataString(accountName)}";

            return new VietQrPaymentDto
            {
                BankName = "MBBank (Ngân Hàng Quân Đội)",
                BankAccountNo = accountNo,
                AccountName = "CÔNG TY TNHH OMNICHANNEL BEAUTY",
                Amount = amount,
                OrderInfo = content,
                QrImageUrl = qrUrl,
                ExpireSeconds = 900 // 15 phút đếm ngược
            };
        }
    }
}
