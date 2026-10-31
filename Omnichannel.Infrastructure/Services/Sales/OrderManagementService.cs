using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Sales;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

using Omnichannel.Application.Interfaces.Notifications;
using Omnichannel.Application.DTOs.Notifications;

namespace Omnichannel.Infrastructure.Services.Sales
{
    public class OrderManagementService : IOrderManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly IOrderWorkflowService _workflowService;
        private readonly INotificationService? _notificationService;
        private readonly ILogger<OrderManagementService> _logger;

        public OrderManagementService(
            ApplicationDbContext context,
            IOrderWorkflowService workflowService,
            ILogger<OrderManagementService> logger,
            INotificationService? notificationService = null)
        {
            _context = context;
            _workflowService = workflowService;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<AdminOrderPagedListViewModel> GetOrdersPagedAsync(AdminOrderFilterRequest filter, CancellationToken cancellationToken = default)
        {
            filter ??= new AdminOrderFilterRequest();

            // Overall Tab counts for rapid badge displays
            int countAll = await _context.Orders.CountAsync(cancellationToken);
            int countPending = await _context.Orders.CountAsync(o => o.OrderStatus == "PENDING", cancellationToken);
            int countConfirmed = await _context.Orders.CountAsync(o => o.OrderStatus == "CONFIRMED" || o.OrderStatus == "ALLOCATED", cancellationToken);
            int countShipping = await _context.Orders.CountAsync(o => o.OrderStatus == "PROCESSING" || o.OrderStatus == "SHIPPING", cancellationToken);
            int countCompleted = await _context.Orders.CountAsync(o => o.OrderStatus == "COMPLETED", cancellationToken);
            int countCancelled = await _context.Orders.CountAsync(o => o.OrderStatus == "CANCELLED", cancellationToken);

            var query = _context.Orders
                .AsNoTracking()
                .Include(o => o.Store)
                .Include(o => o.OrderDetails)
                .AsQueryable();

            // 1. Keyword search (OrderCode, CustomerName, CustomerPhone)
            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                string cleanKw = filter.Keyword.Trim().ToLower();
                query = query.Where(o => o.OrderCode.ToLower().Contains(cleanKw)
                                      || o.CustomerName.ToLower().Contains(cleanKw)
                                      || o.CustomerPhone.Contains(cleanKw));
            }

            // 2. Status filter
            if (!string.IsNullOrWhiteSpace(filter.Status) && filter.Status != "all")
            {
                string statusUpper = filter.Status.Trim().ToUpperInvariant();
                if (statusUpper == "CONFIRMED")
                {
                    query = query.Where(o => o.OrderStatus == "CONFIRMED" || o.OrderStatus == "ALLOCATED");
                }
                else if (statusUpper == "SHIPPING" || statusUpper == "PROCESSING")
                {
                    query = query.Where(o => o.OrderStatus == "SHIPPING" || o.OrderStatus == "PROCESSING");
                }
                else
                {
                    query = query.Where(o => o.OrderStatus == statusUpper);
                }
            }

            // 3. Sales channel filter (POS, WEBSITE)
            if (!string.IsNullOrWhiteSpace(filter.OrderSource) && filter.OrderSource != "all")
            {
                string sourceUpper = filter.OrderSource.Trim().ToUpperInvariant();
                query = query.Where(o => o.OrderSource == sourceUpper);
            }

            // 4. Store / Branch filter
            if (filter.StoreId.HasValue && filter.StoreId.Value > 0)
            {
                query = query.Where(o => o.StoreId == filter.StoreId.Value);
            }

            // 5. Date filter
            if (filter.FromDate.HasValue)
            {
                query = query.Where(o => o.CreatedAt >= filter.FromDate.Value.Date);
            }
            if (filter.ToDate.HasValue)
            {
                var endOfDay = filter.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(o => o.CreatedAt <= endOfDay);
            }

            int totalItems = await query.CountAsync(cancellationToken);
            int pageSize = filter.PageSize > 0 ? filter.PageSize : 10;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            int currentPage = Math.Max(1, Math.Min(filter.Page, Math.Max(1, totalPages)));

            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new AdminOrderItemViewModel
                {
                    Id = o.Id,
                    OrderCode = o.OrderCode,
                    UserId = o.UserId,
                    CustomerName = o.CustomerName,
                    CustomerPhone = o.CustomerPhone,
                    CustomerEmail = o.CustomerEmail,
                    ShippingAddress = o.ShippingAddress,
                    OrderSource = o.OrderSource,
                    OrderStatus = o.OrderStatus,
                    PaymentStatus = o.PaymentStatus,
                    PaymentMethod = o.PaymentMethod,
                    SubTotal = o.SubTotal,
                    DiscountAmount = o.DiscountAmount,
                    ShippingFee = o.ShippingFee,
                    TotalAmount = o.TotalAmount,
                    ItemCount = o.OrderDetails.Sum(d => d.Quantity),
                    StoreId = o.StoreId,
                    StoreName = o.Store != null ? o.Store.Name : "Tổng Kho Điều Phối",
                    CreatedAt = o.CreatedAt,
                    UpdatedAt = o.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            return new AdminOrderPagedListViewModel
            {
                Items = items,
                Filter = filter,
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = currentPage,
                PageSize = pageSize,
                CountAll = countAll,
                CountPending = countPending,
                CountConfirmed = countConfirmed,
                CountShipping = countShipping,
                CountCompleted = countCompleted,
                CountCancelled = countCancelled
            };
        }

        public async Task<AdminOrderDetailViewModel?> GetOrderDetailAsync(int id, CancellationToken cancellationToken = default)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Store)
                .Include(o => o.Invoice)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductBatch)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

            if (order == null) return null;

            // 1. Phân tích hồ sơ khách hàng & Hạng thẻ VIP
            string vipTier = "Khách Hàng Mới";
            string vipBadgeColor = "slate";
            decimal totalSpent = 0;
            int totalOrders = 0;
            int loyaltyPoints = 0;
            DateTime? memberSince = null;
            var recentOrderList = new List<CustomerOrderHistoryBriefDto>();

            var custOrdersQuery = _context.Orders.AsNoTracking()
                .Where(o => o.OrderStatus != "CANCELLED" && (
                    (order.UserId.HasValue && o.UserId == order.UserId.Value) ||
                    (!string.IsNullOrEmpty(order.CustomerPhone) && o.CustomerPhone == order.CustomerPhone)
                ));

            totalOrders = await custOrdersQuery.CountAsync(cancellationToken);
            totalSpent = await custOrdersQuery.SumAsync(o => (decimal?)o.TotalAmount, cancellationToken) ?? 0;

            recentOrderList = await custOrdersQuery
                .Where(o => o.Id != order.Id)
                .OrderByDescending(o => o.CreatedAt)
                .Take(4)
                .Select(o => new CustomerOrderHistoryBriefDto
                {
                    OrderId = o.Id,
                    OrderCode = o.OrderCode,
                    CreatedAt = o.CreatedAt,
                    TotalAmount = o.TotalAmount,
                    OrderStatus = o.OrderStatus,
                    PaymentStatus = o.PaymentStatus,
                    OrderSource = o.OrderSource
                })
                .ToListAsync(cancellationToken);

            if (order.UserId.HasValue)
            {
                var wallet = await _context.LoyaltyWallets.AsNoTracking()
                    .Include(w => w.CurrentTier)
                    .FirstOrDefaultAsync(w => w.UserId == order.UserId.Value, cancellationToken);

                if (wallet != null)
                {
                    vipTier = wallet.CurrentTier?.TierName ?? "Hội Viên";
                    loyaltyPoints = wallet.CurrentBalance;
                    memberSince = wallet.MemberSince;
                    vipBadgeColor = vipTier.ToLower() switch
                    {
                        var t when t.Contains("diamond") || t.Contains("kim cương") => "purple",
                        var t when t.Contains("gold") || t.Contains("vàng") => "amber",
                        var t when t.Contains("silver") || t.Contains("bạc") => "slate",
                        _ => "rose"
                    };
                }
            }

            if (vipTier == "Khách Hàng Mới" && totalSpent > 0)
            {
                if (totalSpent >= 20000000) { vipTier = "VIP Kim Cương (Diamond)"; vipBadgeColor = "purple"; }
                else if (totalSpent >= 10000000) { vipTier = "VIP Vàng (Gold Member)"; vipBadgeColor = "amber"; }
                else if (totalSpent >= 3000000) { vipTier = "VIP Bạc (Silver Member)"; vipBadgeColor = "slate"; }
                else { vipTier = "Thành Viên Thân Thiết"; vipBadgeColor = "rose"; }
                loyaltyPoints = (int)(totalSpent / 10000);
            }

            // 2. Danh sách sản phẩm kèm Lô hàng FEFO (Chỉ định tự động lô có hạn dùng gần nhất trước)
            var now = DateTime.UtcNow;
            var items = new List<AdminOrderDetailItemViewModel>();

            foreach (var od in order.OrderDetails)
            {
                var batch = od.ProductBatch;
                string fefoReason = "Lô hàng chỉ định xuất kho";

                if (batch == null)
                {
                    batch = await _context.ProductBatches
                        .AsNoTracking()
                        .Where(b => b.ProductId == od.ProductId && b.CurrentQuantity > 0)
                        .OrderBy(b => b.ExpDate)
                        .FirstOrDefaultAsync(cancellationToken);

                    fefoReason = "Tự động chỉ định theo thuật toán FEFO (Ưu tiên xuất lô cận hạn nhất)";
                }
                else
                {
                    fefoReason = "Đã xuất/khóa lô theo quy tắc FEFO chuẩn Dược Mỹ Phẩm";
                }

                int daysLeft = batch != null ? (int)Math.Max(0, (batch.ExpDate.Date - now.Date).TotalDays) : 0;
                string batchStatus = "CHUẨN HẠN";
                if (daysLeft <= 30) batchStatus = "CẬN HẠN ĐỎ (≤30 Ngày)";
                else if (daysLeft <= 90) batchStatus = "CẢNH BÁO VÀNG (≤90 Ngày)";

                items.Add(new AdminOrderDetailItemViewModel
                {
                    Id = od.Id,
                    ProductId = od.ProductId,
                    ProductName = od.Product?.Name ?? "Mỹ Phẩm Cao Cấp",
                    ProductSku = od.Product?.Sku ?? string.Empty,
                    ThumbnailUrl = od.Product?.ThumbnailUrl,
                    Unit = od.Product?.Unit ?? "Chai",
                    ProductBatchId = batch?.Id ?? od.ProductBatchId,
                    BatchNumber = batch?.BatchNumber ?? "LOT-FEFO-AUTO",
                    ManufacturingDate = batch != null && batch.ManufacturingDate != default ? batch.ManufacturingDate : (batch?.ExpDate.AddYears(-3) ?? now.AddYears(-2)),
                    ExpDate = batch?.ExpDate,
                    DaysRemaining = daysLeft,
                    BatchStatus = batchStatus,
                    FefoAssignedReason = fefoReason,
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                    DiscountRate = od.DiscountRate,
                    LineTotal = od.LineTotal
                });
            }

            // 3. Tiến trình Workflow 4 Bước Chuyển Trạng Thái
            var timeline = new List<AdminOrderStatusHistoryDto>();

            // Step 1: Placed
            timeline.Add(new AdminOrderStatusHistoryDto
            {
                Status = "PENDING",
                Title = "Đặt Hàng Thành Công",
                Description = $"Đơn hàng được khởi tạo từ kênh {order.OrderSource}",
                Timestamp = order.CreatedAt,
                IsCompleted = true,
                IsCurrent = order.OrderStatus == "PENDING"
            });

            // Step 2: Confirmed (Duyệt Đơn)
            bool isConfirmedOrBeyond = order.OrderStatus != "PENDING" && order.OrderStatus != "CANCELLED";
            timeline.Add(new AdminOrderStatusHistoryDto
            {
                Status = "CONFIRMED",
                Title = "Duyệt Đơn & Khóa Lô FEFO",
                Description = order.Store != null ? $"Showroom tiếp nhận: {order.Store.Name}" : "Đã duyệt đơn & Phân bổ lô FEFO gần nhất",
                Timestamp = isConfirmedOrBeyond ? (order.UpdatedAt ?? order.CreatedAt) : order.CreatedAt,
                IsCompleted = isConfirmedOrBeyond,
                IsCurrent = order.OrderStatus == "CONFIRMED"
            });

            // Step 3: Packing / Processing (Đang Đóng Gói)
            bool isProcessingOrBeyond = order.OrderStatus == "PROCESSING" || order.OrderStatus == "SHIPPING" || order.OrderStatus == "COMPLETED";
            timeline.Add(new AdminOrderStatusHistoryDto
            {
                Status = "PROCESSING",
                Title = "Đang Đóng Gói Niêm Phong",
                Description = "Soạn hàng mỹ phẩm, in phiếu vận đơn A6 & đóng gói chống sốc",
                Timestamp = isProcessingOrBeyond ? (order.UpdatedAt ?? order.CreatedAt) : order.CreatedAt,
                IsCompleted = isProcessingOrBeyond,
                IsCurrent = order.OrderStatus == "PROCESSING"
            });

            // Step 4: Shipping (Bàn Giao Vận Chuyển)
            bool isShippingOrBeyond = order.OrderStatus == "SHIPPING" || order.OrderStatus == "COMPLETED";
            timeline.Add(new AdminOrderStatusHistoryDto
            {
                Status = "SHIPPING",
                Title = "Bàn Giao Vận Chuyển",
                Description = !string.IsNullOrWhiteSpace(order.ShippingAddress) ? $"Địa chỉ giao: {order.ShippingAddress}" : "Bàn giao bưu tá lấy hàng",
                Timestamp = isShippingOrBeyond ? (order.UpdatedAt ?? order.CreatedAt) : order.CreatedAt,
                IsCompleted = isShippingOrBeyond,
                IsCurrent = order.OrderStatus == "SHIPPING"
            });

            // Step 5: Completed (Hoàn Tất)
            bool isCompleted = order.OrderStatus == "COMPLETED";
            timeline.Add(new AdminOrderStatusHistoryDto
            {
                Status = "COMPLETED",
                Title = "Giao Thành Công & Hoàn Tất",
                Description = order.PaymentStatus == "PAID" ? "Đã giao tận tay khách • Thu đủ tiền & Xuất hóa đơn" : "Hoàn tất đơn hàng",
                Timestamp = isCompleted ? (order.UpdatedAt ?? order.CreatedAt) : order.CreatedAt,
                IsCompleted = isCompleted,
                IsCurrent = isCompleted
            });

            return new AdminOrderDetailViewModel
            {
                Id = order.Id,
                OrderCode = order.OrderCode,
                UserId = order.UserId,
                StoreId = order.StoreId,
                StoreName = order.Store?.Name ?? "Tổng Kho Điều Phối",
                StoreAddress = order.Store?.Address ?? string.Empty,
                StorePhone = order.Store?.PhoneNumber,
                OrderSource = order.OrderSource,
                OrderStatus = order.OrderStatus,
                PaymentStatus = order.PaymentStatus,
                PaymentMethod = order.PaymentMethod,
                SubTotal = order.SubTotal,
                DiscountAmount = order.DiscountAmount,
                ShippingFee = order.ShippingFee,
                TotalAmount = order.TotalAmount,
                CustomerName = order.CustomerName,
                CustomerPhone = order.CustomerPhone,
                CustomerEmail = order.CustomerEmail,
                ShippingAddress = order.ShippingAddress,
                Note = order.Note,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                InvoiceCode = order.Invoice?.InvoiceCode,
                InvoiceDate = order.Invoice?.InvoiceDate,
                CustomerVipTier = vipTier,
                CustomerVipBadgeColor = vipBadgeColor,
                CustomerTotalSpent = totalSpent,
                CustomerTotalOrders = totalOrders,
                CustomerLoyaltyPoints = loyaltyPoints,
                CustomerMemberSince = memberSince,
                CustomerRecentOrders = recentOrderList,
                Items = items,
                Timeline = timeline
            };
        }


        public async Task<(bool Success, string Message)> UpdateOrderStatusAsync(
            int id, string newStatus, string? note = null, string? reason = null, CancellationToken cancellationToken = default)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

            if (order == null)
            {
                return (false, "Không tìm thấy đơn hàng.");
            }

            string targetStatus = newStatus.Trim().ToUpperInvariant();

            // Already in target status
            if (order.OrderStatus == targetStatus)
            {
                return (true, $"Đơn hàng đã ở trạng thái {targetStatus}.");
            }

            // Cannot modify finalized orders
            if (order.OrderStatus == "COMPLETED" && targetStatus != "CANCELLED")
            {
                return (false, "Đơn hàng đã hoàn thành, không thể chuyển đổi trạng thái khác.");
            }

            if (order.OrderStatus == "CANCELLED")
            {
                return (false, "Đơn hàng đã hủy trước đó, không thể khôi phục.");
            }

            // Case: CANCEL
            if (targetStatus == "CANCELLED")
            {
                string cancelReason = !string.IsNullOrWhiteSpace(reason) ? reason : (!string.IsNullOrWhiteSpace(note) ? note : "Quản trị viên hủy đơn");
                var cancelResult = await _workflowService.CancelOrderAndReleaseReserveAsync(order.OrderCode, cancelReason, cancellationToken);
                if (cancelResult)
                {
                    order.Note = string.IsNullOrWhiteSpace(order.Note) ? $"Lý do hủy: {cancelReason}" : $"{order.Note} | Lý do hủy: {cancelReason}";
                    await _context.SaveChangesAsync(cancellationToken);
                    return (true, "Đã hủy đơn hàng và hoàn tồn tạm khóa thành công.");
                }
                return (false, "Không thể hủy đơn hàng vào lúc này.");
            }

            // Case: COMPLETE
            if (targetStatus == "COMPLETED")
            {
                order.OrderStatus = "COMPLETED";
                order.PaymentStatus = "PAID";
                order.UpdatedAt = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(note))
                {
                    order.Note = string.IsNullOrWhiteSpace(order.Note) ? note : $"{order.Note} | {note}";
                }

                // Check if Invoice exists, if not generate one
                var existingInvoice = await _context.Invoices.FirstOrDefaultAsync(i => i.OrderId == order.Id, cancellationToken);
                if (existingInvoice == null)
                {
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
                }

                await _context.SaveChangesAsync(cancellationToken);
                await NotifyCustomerOnOrderStatusChangeAsync(order, targetStatus, cancellationToken);
                return (true, "Đã chuyển đơn hàng sang trạng thái HOÀN THÀNH và ghi nhận thanh toán.");
            }

            // Tự động phân bổ lô hàng FEFO (First-Expired, First-Out) khi duyệt đơn hoặc đóng gói
            if ((targetStatus == "CONFIRMED" || targetStatus == "PROCESSING") && order.OrderDetails != null)
            {
                foreach (var od in order.OrderDetails.Where(d => d.ProductBatchId == null))
                {
                    var fefoBatch = await _context.ProductBatches
                        .Where(b => b.ProductId == od.ProductId && b.CurrentQuantity > 0)
                        .OrderBy(b => b.ExpDate)
                        .FirstOrDefaultAsync(cancellationToken);
                    if (fefoBatch != null)
                    {
                        od.ProductBatchId = fefoBatch.Id;
                    }
                }
            }

            // Case: CONFIRMED or SHIPPING / PROCESSING
            order.OrderStatus = targetStatus;
            order.UpdatedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(note))
            {
                order.Note = string.IsNullOrWhiteSpace(order.Note) ? note : $"{order.Note} | {note}";
            }

            await _context.SaveChangesAsync(cancellationToken);
            await NotifyCustomerOnOrderStatusChangeAsync(order, targetStatus, cancellationToken);
            return (true, $"Đã cập nhật trạng thái đơn hàng thành {targetStatus}.");
        }

        private async Task NotifyCustomerOnOrderStatusChangeAsync(Order order, string targetStatus, CancellationToken cancellationToken)
        {
            if (_notificationService != null && order.UserId.HasValue)
            {
                try
                {
                    string statusText = targetStatus switch
                    {
                        "CONFIRMED" => "Đã xác nhận",
                        "PROCESSING" => "Đang chuẩn bị hàng",
                        "SHIPPING" => "Đang giao hàng",
                        "COMPLETED" => "Đã giao hàng thành công",
                        "CANCELLED" => "Đã hủy",
                        _ => targetStatus
                    };

                    await _notificationService.CreateAndSendNotificationAsync(new CreateNotificationDto
                    {
                        Type = "ORDER",
                        Severity = targetStatus == "COMPLETED" ? "SUCCESS" : (targetStatus == "CANCELLED" ? "DANGER" : "INFO"),
                        Title = $"📋 Đơn Hàng #{order.OrderCode}",
                        Message = $"Đơn hàng #{order.OrderCode} của bạn đã chuyển sang: {statusText}.",
                        UserId = order.UserId.Value,
                        Url = "/Account/Orders"
                    }, cancellationToken);
                }
                catch (Exception notifEx)
                {
                    _logger.LogWarning(notifEx, "Không thể gửi notification cho khách hàng #{UserId}", order.UserId);
                }
            }
        }

        public async Task<Invoice?> GetInvoiceForOrderAsync(int orderId, CancellationToken cancellationToken = default)
        {
            return await _context.Invoices
                .AsNoTracking()
                .Include(i => i.Order)
                    .ThenInclude(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                .Include(i => i.Order)
                    .ThenInclude(o => o.Store)
                .FirstOrDefaultAsync(i => i.OrderId == orderId, cancellationToken);
        }
    }
}
