using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Customer;
using Omnichannel.Application.DTOs.Notifications;
using Omnichannel.Application.DTOs.Sales;
using Omnichannel.Application.Interfaces.Notifications;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services.Sales
{
    public class OrderManagementService : IOrderManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly IOrderWorkflowService _workflowService;
        private readonly INotificationService? _notificationService;
        private readonly IShippingService? _shippingService;
        private readonly ILogger<OrderManagementService> _logger;

        public OrderManagementService(
            ApplicationDbContext context,
            IOrderWorkflowService workflowService,
            ILogger<OrderManagementService> logger,
            INotificationService? notificationService = null,
            IShippingService? shippingService = null)
        {
            _context = context;
            _workflowService = workflowService;
            _logger = logger;
            _notificationService = notificationService;
            _shippingService = shippingService;
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
                .Include(o => o.ReturnRequests)
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
                    UpdatedAt = o.UpdatedAt,
                    HasPendingReturnRequest = o.ReturnRequests.Any(r => r.Status == "PENDING")
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
                .Include(o => o.StatusHistories)
                .Include(o => o.ReturnRequests)
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

            // 2. Danh sách sản phẩm kèm Lô hàng FEFO
            var now = DateTime.UtcNow;
            var items = new List<AdminOrderDetailItemViewModel>();

            foreach (var od in order.OrderDetails)
            {
                var batch = od.ProductBatch;
                string fefoReason;

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

            var histories = order.StatusHistories
                .OrderBy(h => h.CreatedAt)
                .ThenBy(h => h.Id)
                .ToList();

            DateTime? GetHistoryTime(params string[] statuses)
            {
                var match = histories.LastOrDefault(h => statuses.Contains(h.Status));
                return match?.CreatedAt;
            }

            string? latestTracking = histories.LastOrDefault(h => !string.IsNullOrWhiteSpace(h.TrackingCode))?.TrackingCode
                ?? GenerateDeterministicTrackingCode(order);
            string? latestCarrier = histories.LastOrDefault(h => !string.IsNullOrWhiteSpace(h.CarrierName))?.CarrierName
                ?? (order.ShippingFee == 45000m ? "Giao Hàng Hỏa Tốc 2H (Omni VIP Express)" : "Giao Hàng Nhanh (GHN Express)");

            // 3. Tiến trình Workflow
            var timeline = new List<AdminOrderStatusHistoryDto>();

            timeline.Add(new AdminOrderStatusHistoryDto
            {
                Status = "PENDING",
                Title = "Đặt Hàng Thành Công",
                Description = $"Đơn hàng được khởi tạo từ kênh {order.OrderSource}",
                Timestamp = GetHistoryTime("PENDING") ?? order.CreatedAt,
                IsCompleted = true,
                IsCurrent = order.OrderStatus == "PENDING"
            });

            bool isConfirmedOrBeyond = order.OrderStatus != "PENDING" && order.OrderStatus != "CANCELLED";
            timeline.Add(new AdminOrderStatusHistoryDto
            {
                Status = "CONFIRMED",
                Title = "Duyệt Đơn & Khóa Lô FEFO",
                Description = order.Store != null ? $"Showroom tiếp nhận: {order.Store.Name}" : "Đã duyệt đơn & Phân bổ lô FEFO gần nhất",
                Timestamp = GetHistoryTime("CONFIRMED") ?? (isConfirmedOrBeyond ? (order.UpdatedAt ?? order.CreatedAt.AddMinutes(15)) : order.CreatedAt),
                IsCompleted = isConfirmedOrBeyond,
                IsCurrent = order.OrderStatus == "CONFIRMED"
            });

            bool isProcessingOrBeyond = order.OrderStatus == "PROCESSING" || order.OrderStatus == "SHIPPING" || order.OrderStatus == "COMPLETED";
            timeline.Add(new AdminOrderStatusHistoryDto
            {
                Status = "PROCESSING",
                Title = "Đang Đóng Gói Niêm Phong",
                Description = "Soạn hàng mỹ phẩm theo lô FEFO, in phiếu vận đơn A6 & đóng gói chống sốc",
                Timestamp = GetHistoryTime("PROCESSING") ?? (isProcessingOrBeyond ? (order.UpdatedAt ?? order.CreatedAt.AddHours(1)) : order.CreatedAt),
                IsCompleted = isProcessingOrBeyond,
                IsCurrent = order.OrderStatus == "PROCESSING"
            });

            bool isShippingOrBeyond = order.OrderStatus == "SHIPPING" || order.OrderStatus == "COMPLETED";
            timeline.Add(new AdminOrderStatusHistoryDto
            {
                Status = "SHIPPING",
                Title = "Bàn Giao Vận Chuyển",
                Description = $"Mã vận đơn: {latestTracking} ({latestCarrier})",
                Timestamp = GetHistoryTime("HANDED_OVER", "SHIPPING") ?? (isShippingOrBeyond ? (order.UpdatedAt ?? order.CreatedAt.AddHours(3)) : order.CreatedAt),
                IsCompleted = isShippingOrBeyond,
                IsCurrent = order.OrderStatus == "SHIPPING"
            });

            bool isCompleted = order.OrderStatus == "COMPLETED";
            timeline.Add(new AdminOrderStatusHistoryDto
            {
                Status = "COMPLETED",
                Title = "Giao Thành Công & Hoàn Tất",
                Description = order.PaymentStatus == "PAID" ? "Đã giao tận tay khách • Thu đủ tiền & Xuất hóa đơn" : "Hoàn tất đơn hàng",
                Timestamp = GetHistoryTime("COMPLETED") ?? (isCompleted ? (order.UpdatedAt ?? order.CreatedAt.AddDays(1)) : order.CreatedAt),
                IsCompleted = isCompleted,
                IsCurrent = isCompleted
            });

            var statusHistoryDtos = histories
                .OrderByDescending(h => h.CreatedAt)
                .ThenByDescending(h => h.Id)
                .Select(h => new OrderStatusHistoryItemDto
                {
                    Id = h.Id,
                    Status = h.Status,
                    Title = h.Title,
                    Description = h.Description,
                    TrackingCode = h.TrackingCode,
                    CarrierName = h.CarrierName,
                    ChangedByName = h.ChangedByName,
                    CreatedAt = h.CreatedAt
                })
                .ToList();

            var returnRequestDtos = order.ReturnRequests
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => MapReturnRequestToViewModel(r, order.OrderCode))
                .ToList();

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
                TrackingCode = latestTracking,
                CarrierName = latestCarrier,
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
                Timeline = timeline,
                StatusHistories = statusHistoryDtos,
                ReturnRequests = returnRequestDtos
            };
        }

        public async Task<(bool Success, string Message)> UpdateOrderStatusAsync(
            int id, string newStatus, string? note = null, string? reason = null, CancellationToken cancellationToken = default)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductBatch)
                .Include(o => o.StatusHistories)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

            if (order == null)
            {
                return (false, "Không tìm thấy đơn hàng.");
            }

            string targetStatus = newStatus.Trim().ToUpperInvariant();

            if (order.OrderStatus == targetStatus)
            {
                return (true, $"Đơn hàng đã ở trạng thái {targetStatus}.");
            }

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
                if (order.OrderStatus == "COMPLETED")
                {
                    return (false, "Đơn hàng đã giao thành công, không thể hủy đơn (vui lòng dùng quy trình Đổi/Trả hàng).");
                }

                string cancelReason = !string.IsNullOrWhiteSpace(reason) ? reason : (!string.IsNullOrWhiteSpace(note) ? note : "Quản trị viên hủy đơn");
                var cancelResult = await _workflowService.CancelOrderAndReleaseReserveAsync(order.OrderCode, cancelReason, cancellationToken);
                if (cancelResult)
                {
                    var cancelledOrder = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
                    if (cancelledOrder != null)
                    {
                        cancelledOrder.Note = string.IsNullOrWhiteSpace(cancelledOrder.Note)
                            ? $"Lý do hủy: {cancelReason}"
                            : $"{cancelledOrder.Note} | Lý do hủy: {cancelReason}";
                        await _context.SaveChangesAsync(cancellationToken);
                        await NotifyCustomerOnOrderStatusChangeAsync(cancelledOrder, "CANCELLED", cancellationToken);
                    }
                    return (true, "Đã hủy đơn hàng và hoàn tồn kho đúng lô FEFO thành công.");
                }
                return (false, "Không thể hủy đơn hàng vào lúc này.");
            }

            // Tự động phân bổ lô hàng FEFO (First-Expired, First-Out) khi duyệt đơn hoặc đóng gói
            var assignedBatchSummaries = new List<string>();
            if (order.OrderDetails != null)
            {
                foreach (var od in order.OrderDetails)
                {
                    if (od.ProductBatchId == null)
                    {
                        var fefoBatch = await _context.ProductBatches
                            .Where(b => b.ProductId == od.ProductId && b.CurrentQuantity > 0)
                            .OrderBy(b => b.ExpDate)
                            .FirstOrDefaultAsync(cancellationToken);
                        if (fefoBatch != null)
                        {
                            od.ProductBatchId = fefoBatch.Id;
                            od.ProductBatch = fefoBatch;
                        }
                    }

                    var bCode = od.ProductBatch?.BatchNumber ?? $"LOT-FEFO-{od.ProductId}";
                    var bExp = od.ProductBatch?.ExpDate.ToString("MM/yyyy") ?? "12/2027";
                    assignedBatchSummaries.Add($"{bCode} (HSD: {bExp})");
                }
            }

            var now = DateTime.UtcNow;

            // Đảm bảo có mốc PENDING trong lịch sử nếu đơn cũ chưa có
            if (!order.StatusHistories.Any(h => h.Status == "PENDING"))
            {
                await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
                {
                    OrderId = order.Id,
                    Status = "PENDING",
                    Title = "Đặt hàng thành công",
                    Description = $"Đơn hàng #{order.OrderCode} được tiếp nhận từ kênh {order.OrderSource}.",
                    ChangedByName = order.CustomerName,
                    CreatedAt = order.CreatedAt
                }, cancellationToken);
            }

            // Case: COMPLETE
            if (targetStatus == "COMPLETED")
            {
                order.OrderStatus = "COMPLETED";
                order.PaymentStatus = "PAID";
                order.UpdatedAt = now;
                if (!string.IsNullOrWhiteSpace(note))
                {
                    order.Note = string.IsNullOrWhiteSpace(order.Note) ? note : $"{order.Note} | {note}";
                }

                // Nếu kho còn giữ ReservedQuantity thì chốt trừ PhysicalQuantity
                if (order.OrderDetails != null)
                {
                    foreach (var detail in order.OrderDetails)
                    {
                        if (!detail.ProductBatchId.HasValue) continue;
                        var inv = await _context.StoreInventories
                            .FirstOrDefaultAsync(si => si.StoreId == order.StoreId && si.ProductBatchId == detail.ProductBatchId.Value, cancellationToken);
                        if (inv != null && inv.ReservedQuantity >= detail.Quantity)
                        {
                            inv.ReservedQuantity -= detail.Quantity;
                            inv.PhysicalQuantity = Math.Max(0, inv.PhysicalQuantity - detail.Quantity);
                            inv.LastUpdated = now;
                        }
                    }
                }

                // Check if Invoice exists, if not generate one
                var existingInvoice = await _context.Invoices.FirstOrDefaultAsync(i => i.OrderId == order.Id, cancellationToken);
                if (existingInvoice == null)
                {
                    decimal totalBeforeTax = Math.Round(order.TotalAmount / 1.1m, 2);
                    decimal vatAmount = order.TotalAmount - totalBeforeTax;

                    var invoice = new Invoice
                    {
                        InvoiceCode = $"INV-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                        OrderId = order.Id,
                        TaxRate = 10.00m,
                        TaxAmount = vatAmount,
                        TotalAmount = order.TotalAmount,
                        InvoiceDate = now,
                        Status = "ISSUED",
                        CreatedAt = now
                    };
                    await _context.Invoices.AddAsync(invoice, cancellationToken);
                }

                await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
                {
                    OrderId = order.Id,
                    Status = "COMPLETED",
                    Title = "Giao thành công",
                    Description = !string.IsNullOrWhiteSpace(note)
                        ? note
                        : "Đơn hàng đã được giao tận tay khách hàng thành công. Đã quyết toán thanh toán & kích hoạt bảo hành đổi trả 7 ngày.",
                    ChangedByName = "Quản trị viên / Bưu tá GHN",
                    CreatedAt = now
                }, cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);
                await NotifyCustomerOnOrderStatusChangeAsync(order, targetStatus, cancellationToken);
                return (true, "Đã chuyển đơn hàng sang trạng thái HOÀN THÀNH và ghi nhận lịch sử giao hàng.");
            }

            // Case: CONFIRMED, PROCESSING, or SHIPPING
            order.OrderStatus = targetStatus;
            order.UpdatedAt = now;
            if (!string.IsNullOrWhiteSpace(note))
            {
                order.Note = string.IsNullOrWhiteSpace(order.Note) ? note : $"{order.Note} | {note}";
            }

            if (targetStatus == "CONFIRMED")
            {
                await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
                {
                    OrderId = order.Id,
                    Status = "CONFIRMED",
                    Title = "Đã xác nhận đơn hàng",
                    Description = !string.IsNullOrWhiteSpace(note)
                        ? note
                        : $"Hệ thống & CSKH đã xác nhận đơn hàng #{order.OrderCode}, giữ chỗ lô FEFO: {string.Join(", ", assignedBatchSummaries.Distinct())}.",
                    ChangedByName = "Quản trị viên Omni Beauty",
                    CreatedAt = now
                }, cancellationToken);
            }
            else if (targetStatus == "PROCESSING")
            {
                await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
                {
                    OrderId = order.Id,
                    Status = "PROCESSING",
                    Title = "Đóng gói (Lô FEFO đã cấp phát)",
                    Description = !string.IsNullOrWhiteSpace(note)
                        ? note
                        : $"Đã xuất kho & đóng gói chống sốc theo chuẩn FEFO. Lô cấp phát: {string.Join(", ", assignedBatchSummaries.Distinct())}.",
                    ChangedByName = "Bộ phận Kho FEFO",
                    CreatedAt = now
                }, cancellationToken);
            }
            else if (targetStatus == "SHIPPING")
            {
                string trackingCode = GenerateDeterministicTrackingCode(order);
                string carrierName = order.ShippingFee == 45000m ? "Giao Hàng Hỏa Tốc 2H (Omni VIP Express)" : "Giao Hàng Nhanh (GHN Express)";

                if (_shippingService != null)
                {
                    try
                    {
                        var shipRes = await _shippingService.CreateShippingOrderAsync(order, 500);
                        if (shipRes.Success && !string.IsNullOrWhiteSpace(shipRes.TrackingCode))
                        {
                            trackingCode = shipRes.TrackingCode;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Fallback mã vận đơn nội bộ cho đơn {OrderCode}", order.OrderCode);
                    }
                }

                // Ghi mốc 4 (Bàn giao vận chuyển) và mốc 5 (Đang giao)
                if (!order.StatusHistories.Any(h => h.Status == "HANDED_OVER"))
                {
                    await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
                    {
                        OrderId = order.Id,
                        Status = "HANDED_OVER",
                        Title = "Bàn giao vận chuyển",
                        Description = $"Kiện hàng đã bàn giao cho {carrierName}. Mã vận đơn: {trackingCode}.",
                        TrackingCode = trackingCode,
                        CarrierName = carrierName,
                        ChangedByName = "Bộ phận Điều phối Kho",
                        CreatedAt = now.AddSeconds(-5)
                    }, cancellationToken);
                }

                await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
                {
                    OrderId = order.Id,
                    Status = "SHIPPING",
                    Title = "Đang giao hàng",
                    Description = !string.IsNullOrWhiteSpace(note)
                        ? $"{note} (Mã vận đơn: {trackingCode})"
                        : $"Bưu tá {carrierName} đang trên tuyến giao kiện hàng ({trackingCode}) đến địa chỉ: {order.ShippingAddress}.",
                    TrackingCode = trackingCode,
                    CarrierName = carrierName,
                    ChangedByName = carrierName,
                    CreatedAt = now
                }, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
            await NotifyCustomerOnOrderStatusChangeAsync(order, targetStatus, cancellationToken);
            return (true, $"Đã cập nhật trạng thái đơn hàng thành {targetStatus} và ghi nhận vào lịch sử.");
        }

        // =========================================================================
        // GAP-07 (PHIÊN C6): TIMELINE 6 BƯỚC, TRA CỨU VÃNG LAI, HỦY ĐƠN & ĐỔI TRẢ
        // =========================================================================

        public async Task<(bool Allowed, string? ErrorMessage, CustomerOrderDetailViewModel? OrderDetail)> GetCustomerOrderDetailAsync(
            int orderId, string? orderCode, int? currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default)
        {
            Order? order = null;
            var baseQuery = _context.Orders
                .AsNoTracking()
                .Include(o => o.Store)
                .Include(o => o.Invoice)
                .Include(o => o.StatusHistories)
                .Include(o => o.ReturnRequests)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.ProductBatch);

            if (orderId > 0)
            {
                order = await baseQuery.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(orderCode))
            {
                string cleanCode = orderCode.Trim();
                order = await baseQuery.FirstOrDefaultAsync(o => o.OrderCode == cleanCode, cancellationToken);
            }

            if (order == null)
            {
                return (false, "Không tìm thấy đơn hàng yêu cầu.", null);
            }

            // Kiểm tra quyền sở hữu nghiêm ngặt (Anti-IDOR): Khách A không xem được đơn Khách B
            if (!isAdmin)
            {
                if (!currentUserId.HasValue || currentUserId.Value <= 0)
                {
                    return (false, "Vui lòng đăng nhập để xem chi tiết đơn hàng của bạn.", null);
                }

                if (order.UserId.HasValue)
                {
                    if (order.UserId.Value != currentUserId.Value)
                    {
                        return (false, "Bạn không có quyền truy cập đơn hàng của khách hàng khác.", null);
                    }
                }
                else
                {
                    var currentUser = await _context.Users.AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == currentUserId.Value, cancellationToken);

                    bool phoneMatches = currentUser != null
                        && !string.IsNullOrWhiteSpace(currentUser.PhoneNumber)
                        && NormalizePhone(currentUser.PhoneNumber) == NormalizePhone(order.CustomerPhone);

                    bool emailMatches = currentUser != null
                        && !string.IsNullOrWhiteSpace(currentUser.Email)
                        && !string.IsNullOrWhiteSpace(order.CustomerEmail)
                        && string.Equals(currentUser.Email.Trim(), order.CustomerEmail.Trim(), StringComparison.OrdinalIgnoreCase);

                    if (!phoneMatches && !emailMatches)
                    {
                        return (false, "Bạn không có quyền truy cập đơn hàng của khách hàng khác.", null);
                    }
                }
            }

            var vm = MapToCustomerOrderDetailViewModel(order, isGuestLookup: false);
            return (true, null, vm);
        }

        public async Task<GuestOrderLookupViewModel> LookupGuestOrderAsync(
            string orderCode, string phoneNumber, CancellationToken cancellationToken = default)
        {
            var result = new GuestOrderLookupViewModel
            {
                OrderCode = orderCode?.Trim() ?? string.Empty,
                PhoneNumber = phoneNumber?.Trim() ?? string.Empty,
                HasSearched = true
            };

            if (string.IsNullOrWhiteSpace(result.OrderCode) || string.IsNullOrWhiteSpace(result.PhoneNumber))
            {
                result.ErrorMessage = "Vui lòng nhập đầy đủ Mã đơn hàng và Số điện thoại nhận hàng.";
                return result;
            }

            string cleanCode = result.OrderCode.Trim().ToUpperInvariant();
            string cleanInputPhone = NormalizePhone(result.PhoneNumber);

            if (cleanInputPhone.Length < 8)
            {
                result.ErrorMessage = "Số điện thoại tra cứu không hợp lệ.";
                return result;
            }

            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Store)
                .Include(o => o.Invoice)
                .Include(o => o.StatusHistories)
                .Include(o => o.ReturnRequests)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.ProductBatch)
                .FirstOrDefaultAsync(o => o.OrderCode.ToUpper() == cleanCode, cancellationToken);

            // Bảo mật: Nếu không tồn tại mã đơn HOẶC sai số điện thoại -> Trả về cùng 1 thông báo chung, tuyệt đối không lộ thông tin
            if (order == null || NormalizePhone(order.CustomerPhone) != cleanInputPhone)
            {
                result.ErrorMessage = "Không tìm thấy thông tin đơn hàng khớp với Mã đơn hàng và Số điện thoại đã nhập. Vui lòng kiểm tra lại!";
                result.OrderDetail = null;
                return result;
            }

            result.OrderDetail = MapToCustomerOrderDetailViewModel(order, isGuestLookup: true);
            return result;
        }

        public async Task<(bool Success, string Message)> CancelOrderByCustomerAsync(
            int orderId, int currentUserId, string reason, string? note = null, CancellationToken cancellationToken = default)
        {
            if (currentUserId <= 0)
            {
                return (false, "Vui lòng đăng nhập để thực hiện hủy đơn hàng.");
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return (false, "Vui lòng chọn lý do hủy đơn hàng.");
            }

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

            if (order == null)
            {
                return (false, "Không tìm thấy đơn hàng.");
            }

            // Kiểm tra quyền sở hữu (Anti-IDOR): Khách A không hủy được đơn Khách B
            if (order.UserId.HasValue)
            {
                if (order.UserId.Value != currentUserId)
                {
                    return (false, "Bạn không có quyền hủy đơn hàng của khách hàng khác.");
                }
            }
            else
            {
                var currentUser = await _context.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);
                if (currentUser == null || NormalizePhone(currentUser.PhoneNumber) != NormalizePhone(order.CustomerPhone))
                {
                    return (false, "Bạn không có quyền hủy đơn hàng của khách hàng khác.");
                }
            }

            // Chỉ cho phép khách tự hủy khi đơn ở trạng thái Chờ xác nhận (PENDING)
            if (order.OrderStatus != "PENDING")
            {
                string currentStatusVn = order.OrderStatus switch
                {
                    "COMPLETED" => "Đã giao thành công",
                    "SHIPPING" => "Đang giao hàng",
                    "PROCESSING" => "Đang đóng gói",
                    "CONFIRMED" => "Đã xác nhận",
                    "CANCELLED" => "Đã hủy",
                    _ => order.OrderStatus
                };
                return (false, $"Không thể hủy đơn hàng đang ở trạng thái '{currentStatusVn}'. Khách hàng chỉ có thể tự hủy khi đơn đang Chờ xác nhận.");
            }

            string fullReason = string.IsNullOrWhiteSpace(note)
                ? $"Khách hàng yêu cầu hủy: {reason.Trim()}"
                : $"Khách hàng yêu cầu hủy: {reason.Trim()} ({note.Trim()})";

            var cancelled = await _workflowService.CancelOrderAndReleaseReserveAsync(order.OrderCode, fullReason, cancellationToken);
            if (!cancelled)
            {
                return (false, "Không thể hủy đơn hàng vào lúc này. Vui lòng thử lại sau.");
            }

            var updatedOrder = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
            if (updatedOrder != null)
            {
                updatedOrder.Note = string.IsNullOrWhiteSpace(updatedOrder.Note)
                    ? fullReason
                    : $"{updatedOrder.Note} | {fullReason}";
                await _context.SaveChangesAsync(cancellationToken);
            }

            // Gửi thông báo realtime cho Admin
            if (_notificationService != null)
            {
                try
                {
                    await _notificationService.CreateAndSendNotificationAsync(new CreateNotificationDto
                    {
                        Type = "ORDER",
                        Severity = "WARNING",
                        Title = $"⚠️ Khách Hủy Đơn: #{order.OrderCode}",
                        Message = $"Khách hàng {order.CustomerName} vừa hủy đơn #{order.OrderCode}. {fullReason}. Đã hoàn tồn lô FEFO.",
                        Role = "Admin",
                        Url = $"/Admin/Order/Details/{order.Id}"
                    }, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi gửi thông báo hủy đơn cho Admin");
                }
            }

            return (true, $"Đã hủy đơn hàng #{order.OrderCode} và hoàn trả tồn kho lô FEFO thành công.");
        }

        public async Task<(bool Success, string Message, OrderReturnRequestViewModel? ReturnRequest)> SubmitReturnRequestAsync(
            int orderId, int currentUserId, string reason, string? description, List<string>? evidenceUrls = null, CancellationToken cancellationToken = default)
        {
            if (currentUserId <= 0)
            {
                return (false, "Vui lòng đăng nhập để gửi yêu cầu đổi/trả hàng.", null);
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return (false, "Vui lòng chọn lý do đổi/trả hàng.", null);
            }

            var order = await _context.Orders
                .Include(o => o.StatusHistories)
                .Include(o => o.ReturnRequests)
                .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

            if (order == null)
            {
                return (false, "Không tìm thấy đơn hàng.", null);
            }

            // Kiểm tra quyền sở hữu
            if (order.UserId.HasValue)
            {
                if (order.UserId.Value != currentUserId)
                {
                    return (false, "Bạn không có quyền gửi yêu cầu đổi/trả cho đơn hàng của khách hàng khác.", null);
                }
            }
            else
            {
                var currentUser = await _context.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);
                if (currentUser == null || NormalizePhone(currentUser.PhoneNumber) != NormalizePhone(order.CustomerPhone))
                {
                    return (false, "Bạn không có quyền gửi yêu cầu đổi/trả cho đơn hàng của khách hàng khác.", null);
                }
            }

            if (order.OrderStatus != "COMPLETED")
            {
                return (false, "Chỉ những đơn hàng Đã giao thành công mới có thể gửi yêu cầu đổi/trả.", null);
            }

            // Kiểm tra thời hạn 7 ngày kể từ khi giao hàng
            var deliveredAt = order.StatusHistories
                .Where(h => h.Status == "COMPLETED")
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => (DateTime?)h.CreatedAt)
                .FirstOrDefault() ?? order.UpdatedAt ?? order.CreatedAt;

            double daysElapsed = (DateTime.UtcNow - deliveredAt).TotalDays;
            if (daysElapsed > 7.0)
            {
                return (false, "Đơn hàng đã quá thời hạn 7 ngày kể từ khi giao thành công nên không đủ điều kiện đổi/trả online.", null);
            }

            if (order.ReturnRequests.Any(r => r.Status == "PENDING" || r.Status == "APPROVED"))
            {
                return (false, "Đơn hàng này đã có yêu cầu đổi/trả đang chờ xử lý hoặc đã được chấp thuận.", null);
            }

            var cleanUrls = (evidenceUrls ?? new List<string>())
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Take(3)
                .ToList();

            var now = DateTime.UtcNow;
            var reqEntity = new OrderReturnRequest
            {
                OrderId = order.Id,
                UserId = currentUserId,
                RequestCode = $"RET-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}",
                Reason = reason.Trim(),
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                EvidenceUrlsJson = cleanUrls.Any() ? JsonSerializer.Serialize(cleanUrls) : null,
                Status = "PENDING",
                CreatedAt = now
            };

            await _context.OrderReturnRequests.AddAsync(reqEntity, cancellationToken);

            await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = "RETURN_REQUESTED",
                Title = "Yêu cầu Đổi/Trả hàng",
                Description = $"Khách hàng gửi yêu cầu đổi/trả ({reqEntity.RequestCode}): {reqEntity.Reason}.",
                ChangedByUserId = currentUserId,
                ChangedByName = order.CustomerName,
                CreatedAt = now
            }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            if (_notificationService != null)
            {
                try
                {
                    await _notificationService.CreateAndSendNotificationAsync(new CreateNotificationDto
                    {
                        Type = "ORDER",
                        Severity = "WARNING",
                        Title = $"🔄 Yêu Cầu Đổi/Trả Mới: #{order.OrderCode}",
                        Message = $"Khách hàng {order.CustomerName} gửi yêu cầu đổi/trả ({reqEntity.RequestCode}) cho đơn #{order.OrderCode}. Lý do: {reqEntity.Reason}",
                        Role = "Admin",
                        Url = $"/Admin/Order/Details/{order.Id}"
                    }, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không thể gửi thông báo yêu cầu đổi trả cho Admin");
                }
            }

            return (true, $"Đã gửi yêu cầu đổi/trả hàng ({reqEntity.RequestCode}) thành công! Bộ phận CSKH Omni Beauty sẽ phản hồi trong vòng 24 giờ.", MapReturnRequestToViewModel(reqEntity, order.OrderCode));
        }

        public async Task<(bool Success, string Message)> ProcessReturnRequestAsync(
            int returnRequestId, bool isApproved, string? adminNote, int? adminUserId = null, CancellationToken cancellationToken = default)
        {
            var returnReq = await _context.OrderReturnRequests
                .Include(r => r.Order)
                    .ThenInclude(o => o.OrderDetails)
                .FirstOrDefaultAsync(r => r.Id == returnRequestId, cancellationToken);

            if (returnReq == null || returnReq.Order == null)
            {
                return (false, "Không tìm thấy yêu cầu đổi/trả hàng.");
            }

            if (returnReq.Status != "PENDING")
            {
                return (false, $"Yêu cầu đổi/trả {returnReq.RequestCode} đã được xử lý trước đó ({returnReq.Status}).");
            }

            var order = returnReq.Order;
            var now = DateTime.UtcNow;

            var strategy = _context.Database.CreateExecutionStrategy();
            var result = await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    returnReq.Status = isApproved ? "APPROVED" : "REJECTED";
                    returnReq.AdminNote = string.IsNullOrWhiteSpace(adminNote)
                        ? (isApproved ? "Chấp thuận đổi/trả & hoàn tiền theo chính sách 7 ngày Omni Beauty." : "Yêu cầu chưa đủ điều kiện đổi/trả theo quy định.")
                        : adminNote.Trim();
                    returnReq.ProcessedByUserId = adminUserId;
                    returnReq.ProcessedAt = now;

                    if (isApproved)
                    {
                        order.PaymentStatus = "REFUNDED";
                        order.UpdatedAt = now;

                        // Hoàn lại tồn kho vào đúng lô FEFO khi nhận hàng trả về
                        foreach (var detail in order.OrderDetails)
                        {
                            if (!detail.ProductBatchId.HasValue) continue;

                            var inv = await _context.StoreInventories
                                .FirstOrDefaultAsync(si => si.StoreId == order.StoreId && si.ProductBatchId == detail.ProductBatchId.Value, cancellationToken);
                            var batch = await _context.ProductBatches
                                .FirstOrDefaultAsync(b => b.Id == detail.ProductBatchId.Value, cancellationToken);

                            if (inv != null)
                            {
                                int before = inv.PhysicalQuantity;
                                inv.PhysicalQuantity += detail.Quantity;
                                inv.LastUpdated = now;

                                await _context.InventoryLogs.AddAsync(new InventoryLog
                                {
                                    StoreId = order.StoreId,
                                    ProductBatchId = detail.ProductBatchId.Value,
                                    TransactionType = "RETURN_APPROVED_RESTORE",
                                    QuantityChanged = detail.Quantity,
                                    QuantityBefore = before,
                                    QuantityAfter = inv.PhysicalQuantity,
                                    UserId = adminUserId,
                                    CreatedAt = now,
                                    Note = $"Nhập hoàn kho lô FEFO từ yêu cầu đổi trả {returnReq.RequestCode} của đơn {order.OrderCode}"
                                }, cancellationToken);
                            }

                            if (batch != null)
                            {
                                batch.CurrentQuantity += detail.Quantity;
                            }
                        }

                        await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
                        {
                            OrderId = order.Id,
                            Status = "RETURN_APPROVED",
                            Title = "Đã duyệt Đổi/Trả & Hoàn tiền",
                            Description = $"Yêu cầu {returnReq.RequestCode} đã được CHẤP THUẬN. Phản hồi: {returnReq.AdminNote}",
                            ChangedByUserId = adminUserId,
                            ChangedByName = "Quản trị viên Omni Beauty",
                            CreatedAt = now
                        }, cancellationToken);
                    }
                    else
                    {
                        order.UpdatedAt = now;
                        await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
                        {
                            OrderId = order.Id,
                            Status = "RETURN_REJECTED",
                            Title = "Từ chối yêu cầu Đổi/Trả",
                            Description = $"Yêu cầu {returnReq.RequestCode} đã bị TỪ CHỐI. Lý do: {returnReq.AdminNote}",
                            ChangedByUserId = adminUserId,
                            ChangedByName = "Quản trị viên Omni Beauty",
                            CreatedAt = now
                        }, cancellationToken);
                    }

                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return (true, isApproved
                        ? $"Đã DUYỆT yêu cầu đổi/trả {returnReq.RequestCode} và thông báo realtime cho khách hàng."
                        : $"Đã TỪ CHỐI yêu cầu đổi/trả {returnReq.RequestCode} và thông báo realtime cho khách hàng.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Lỗi khi xử lý yêu cầu đổi trả {RequestId}", returnRequestId);
                    return (false, "Lỗi hệ thống khi xử lý yêu cầu đổi/trả.");
                }
            });

            // Gửi thông báo realtime qua SignalR cho khách hàng
            int? targetCustomerId = returnReq.UserId ?? order.UserId;
            if (result.Item1 && _notificationService != null && targetCustomerId.HasValue)
            {
                try
                {
                    await _notificationService.CreateAndSendNotificationAsync(new CreateNotificationDto
                    {
                        Type = "ORDER",
                        Severity = isApproved ? "SUCCESS" : "DANGER",
                        Title = isApproved
                            ? $"✅ Đã Duyệt Đổi/Trả Đơn #{order.OrderCode}"
                            : $"❌ Phản Hồi Đổi/Trả Đơn #{order.OrderCode}",
                        Message = isApproved
                            ? $"Yêu cầu đổi/trả {returnReq.RequestCode} của đơn #{order.OrderCode} đã được CHẤP THUẬN. Ghi chú: {returnReq.AdminNote}"
                            : $"Yêu cầu đổi/trả {returnReq.RequestCode} của đơn #{order.OrderCode} không được duyệt. Lý do: {returnReq.AdminNote}",
                        UserId = targetCustomerId.Value,
                        Url = $"/Customer/OrderDetail/{order.Id}"
                    }, cancellationToken);
                }
                catch (Exception notifEx)
                {
                    _logger.LogWarning(notifEx, "Không thể gửi notification kết quả đổi trả cho khách #{UserId}", targetCustomerId);
                }
            }

            return result;
        }

        private CustomerOrderDetailViewModel MapToCustomerOrderDetailViewModel(Order o, bool isGuestLookup)
        {
            var histories = (o.StatusHistories ?? new List<OrderStatusHistory>())
                .OrderBy(h => h.CreatedAt)
                .ThenBy(h => h.Id)
                .ToList();

            var items = (o.OrderDetails ?? new List<OrderDetail>()).Select(d => new CustomerOrderItemDto
            {
                ProductId = d.ProductId,
                ProductName = d.Product?.Name ?? "Mỹ phẩm cao cấp",
                Sku = d.Product?.Sku ?? "OMNI-SKU",
                ThumbnailUrl = d.Product?.ThumbnailUrl ?? "/images/branding/default-product.png",
                VariantName = d.Product?.Unit ?? "Tiêu chuẩn",
                ProductBatchId = d.ProductBatchId,
                FefoBatchNumber = d.ProductBatch?.BatchNumber ?? $"LOT-FEFO-{d.ProductId:D3}",
                FefoExpiryDate = d.ProductBatch?.ExpDate ?? DateTime.UtcNow.AddMonths(24),
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                DiscountRate = d.DiscountRate,
                LineTotal = d.LineTotal
            }).ToList();

            var fefoBatchesSummary = items
                .Select(i => $"Lô {i.FefoBatchNumber} • HSD: {(i.FefoExpiryDate?.ToString("dd/MM/yyyy") ?? "Hạn xa")} — {i.ProductName} (x{i.Quantity})")
                .ToList();

            string trackingCode = histories.LastOrDefault(h => !string.IsNullOrWhiteSpace(h.TrackingCode))?.TrackingCode
                ?? GenerateDeterministicTrackingCode(o);
            string carrierName = histories.LastOrDefault(h => !string.IsNullOrWhiteSpace(h.CarrierName))?.CarrierName
                ?? (o.ShippingFee == 45000m ? "Giao Hàng Hỏa Tốc 2H (Omni VIP Express)" : "Giao Hàng Nhanh (GHN Express)");

            // Mapping OrderStatus -> 6-step index:
            // 1: Đặt hàng (PENDING)
            // 2: Xác nhận (CONFIRMED)
            // 3: Đóng gói - Lô FEFO đã cấp phát (PROCESSING)
            // 4: Bàn giao vận chuyển (HANDED_OVER)
            // 5: Đang giao (SHIPPING)
            // 6: Giao thành công (COMPLETED)
            int currentStep = o.OrderStatus switch
            {
                "PENDING" => 1,
                "CONFIRMED" or "ALLOCATED" => 2,
                "PROCESSING" => 3,
                "HANDED_OVER" => 4,
                "SHIPPING" => 5,
                "COMPLETED" => 6,
                _ => 1
            };

            var timelineEvents = BuildSixStepTimelineEvents(o, currentStep, histories, fefoBatchesSummary, trackingCode, carrierName);

            var statusHistoryDtos = histories
                .OrderByDescending(h => h.CreatedAt)
                .ThenByDescending(h => h.Id)
                .Select(h => new OrderStatusHistoryItemDto
                {
                    Id = h.Id,
                    Status = h.Status,
                    Title = h.Title,
                    Description = h.Description,
                    TrackingCode = h.TrackingCode,
                    CarrierName = h.CarrierName,
                    ChangedByName = h.ChangedByName,
                    CreatedAt = h.CreatedAt
                })
                .ToList();

            var latestReturn = (o.ReturnRequests ?? new List<OrderReturnRequest>())
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefault();

            DateTime? deliveredAt = histories.LastOrDefault(h => h.Status == "COMPLETED")?.CreatedAt
                ?? (o.OrderStatus == "COMPLETED" ? (o.UpdatedAt ?? o.CreatedAt) : null);

            double daysSinceDelivered = deliveredAt.HasValue ? (DateTime.UtcNow - deliveredAt.Value).TotalDays : 999;
            bool canRequestReturn = !isGuestLookup
                && o.OrderStatus == "COMPLETED"
                && daysSinceDelivered <= 7.0
                && (latestReturn == null || latestReturn.Status == "REJECTED");

            int returnDaysRemaining = o.OrderStatus == "COMPLETED" && daysSinceDelivered <= 7.0
                ? Math.Max(1, (int)Math.Ceiling(7.0 - daysSinceDelivered))
                : 0;

            string displayPhone = isGuestLookup ? MaskPhone(o.CustomerPhone) : o.CustomerPhone;
            string displayAddress = isGuestLookup
                ? MaskAddress(o.ShippingAddress ?? "Nhận tại Showroom Omni Beauty")
                : (o.ShippingAddress ?? "Nhận tại Showroom Omni Beauty");

            var cancelHistory = histories.LastOrDefault(h => h.Status == "CANCELLED");

            return new CustomerOrderDetailViewModel
            {
                Id = o.Id,
                OrderCode = o.OrderCode,
                UserId = o.UserId,
                OrderDate = o.CreatedAt,
                UpdatedAt = o.UpdatedAt,
                OrderStatus = o.OrderStatus,
                OrderStatusDisplayName = o.OrderStatus switch
                {
                    "PENDING" => "Chờ xác nhận",
                    "CONFIRMED" or "ALLOCATED" => "Đã xác nhận",
                    "PROCESSING" => "Đóng gói (Chuẩn FEFO)",
                    "HANDED_OVER" => "Đã bàn giao vận chuyển",
                    "SHIPPING" => "Đang giao hàng",
                    "COMPLETED" => "Giao thành công",
                    "CANCELLED" => "Đã hủy",
                    _ => o.OrderStatus
                },
                OrderStatusBadgeClass = o.OrderStatus switch
                {
                    "PENDING" => "bg-amber-100 text-amber-800 border-amber-200",
                    "CONFIRMED" or "ALLOCATED" => "bg-blue-100 text-blue-800 border-blue-200",
                    "PROCESSING" => "bg-indigo-100 text-indigo-800 border-indigo-200",
                    "HANDED_OVER" or "SHIPPING" => "bg-purple-100 text-purple-800 border-purple-200",
                    "COMPLETED" => "bg-emerald-100 text-emerald-800 border-emerald-200",
                    "CANCELLED" => "bg-rose-100 text-rose-800 border-rose-200",
                    _ => "bg-slate-100 text-slate-600 border-slate-200"
                },
                PaymentMethod = o.PaymentMethod,
                PaymentStatus = o.PaymentStatus,
                OrderNote = o.Note,
                TimelineStep = currentStep,
                TimelineEvents = timelineEvents,
                StatusHistories = statusHistoryDtos,
                RecipientName = o.CustomerName,
                RecipientPhone = displayPhone,
                ShippingAddress = displayAddress,
                CarrierName = carrierName,
                TrackingCode = trackingCode,
                Items = items,
                SubTotal = o.SubTotal > 0 ? o.SubTotal : items.Sum(i => i.LineTotal),
                ShippingFee = o.ShippingFee,
                DiscountAmount = o.DiscountAmount,
                AppliedVoucherCode = o.DiscountAmount > 0 ? "OMNI-VIP" : null,
                TotalAmount = o.TotalAmount,
                LoyaltyPointsEarned = (int)Math.Round(o.TotalAmount / 10000m * 1.5m),
                HasVatRequested = o.Invoice != null,
                VatInfo = o.Invoice != null ? new VatInvoiceRequestDto
                {
                    OrderId = o.Id,
                    TaxCode = o.Invoice.CustomerTaxCode ?? "",
                    CompanyName = o.Invoice.CompanyName ?? "",
                    RequestedAt = o.Invoice.InvoiceDate
                } : null,
                CanCancel = !isGuestLookup && o.OrderStatus == "PENDING",
                CancelReason = cancelHistory?.Description ?? o.Note,
                CanRequestReturn = canRequestReturn,
                ReturnDaysRemaining = returnDaysRemaining,
                DeliveredAt = deliveredAt,
                ActiveReturnRequest = latestReturn != null ? MapReturnRequestToViewModel(latestReturn, o.OrderCode) : null,
                IsGuestLookupView = isGuestLookup
            };
        }

        private static List<OrderTimelineEventDto> BuildSixStepTimelineEvents(
            Order o,
            int currentStep,
            List<OrderStatusHistory> histories,
            List<string> fefoBatchesSummary,
            string trackingCode,
            string carrierName)
        {
            DateTime? FindHistoryTime(params string[] statuses)
            {
                var found = histories.LastOrDefault(h => statuses.Contains(h.Status));
                return found?.CreatedAt;
            }

            OrderStatusHistory? FindHistory(params string[] statuses)
            {
                return histories.LastOrDefault(h => statuses.Contains(h.Status));
            }

            if (o.OrderStatus == "CANCELLED")
            {
                var placedTime = FindHistoryTime("PENDING") ?? o.CreatedAt;
                var cancelHist = FindHistory("CANCELLED");
                var cancelTime = cancelHist?.CreatedAt ?? o.UpdatedAt ?? o.CreatedAt.AddMinutes(15);

                return new List<OrderTimelineEventDto>
                {
                    new()
                    {
                        StepIndex = 1,
                        StatusCode = "PENDING",
                        Title = "Đặt hàng",
                        Description = $"Đơn hàng #{o.OrderCode} đã được khởi tạo trên hệ thống.",
                        Timestamp = placedTime,
                        FormattedTime = placedTime.ToString("dd/MM/yyyy HH:mm:ss"),
                        ChangedByName = o.CustomerName,
                        IsCompleted = true,
                        IsCurrent = false
                    },
                    new()
                    {
                        StepIndex = 2,
                        StatusCode = "CANCELLED",
                        Title = "Đã hủy đơn hàng & Hoàn tồn FEFO",
                        Description = cancelHist?.Description ?? (!string.IsNullOrWhiteSpace(o.Note) ? o.Note : "Đơn hàng đã hủy và hoàn trả tồn kho về đúng lô FEFO."),
                        Timestamp = cancelTime,
                        FormattedTime = cancelTime.ToString("dd/MM/yyyy HH:mm:ss"),
                        ChangedByName = cancelHist?.ChangedByName ?? "Khách hàng / Quản trị viên",
                        FefoBatchesSummary = fefoBatchesSummary,
                        IsCompleted = true,
                        IsCurrent = true,
                        IsAlertStep = true
                    }
                };
            }

            var t1 = FindHistoryTime("PENDING") ?? o.CreatedAt;
            var t2 = FindHistoryTime("CONFIRMED") ?? (currentStep >= 2 ? t1.AddMinutes(15) : null);
            var t3 = FindHistoryTime("PROCESSING") ?? (currentStep >= 3 ? (t2 ?? t1).AddMinutes(45) : null);
            var t4 = FindHistoryTime("HANDED_OVER") ?? FindHistoryTime("SHIPPING") ?? (currentStep >= 4 ? (t3 ?? t1).AddHours(2) : null);
            var t5 = FindHistoryTime("SHIPPING", "DELIVERING") ?? (currentStep >= 5 ? (t4 ?? t1).AddMinutes(30) : null);
            var t6 = FindHistoryTime("COMPLETED") ?? (currentStep >= 6 ? (o.UpdatedAt ?? (t5 ?? t1).AddHours(6)) : null);

            var h1 = FindHistory("PENDING");
            var h2 = FindHistory("CONFIRMED");
            var h3 = FindHistory("PROCESSING");
            var h4 = FindHistory("HANDED_OVER");
            var h5 = FindHistory("SHIPPING", "DELIVERING");
            var h6 = FindHistory("COMPLETED");

            return new List<OrderTimelineEventDto>
            {
                new()
                {
                    StepIndex = 1,
                    StatusCode = "PENDING",
                    Title = "Đặt hàng",
                    Description = h1?.Description ?? $"Hệ thống Omni Beauty đã tiếp nhận đơn hàng #{o.OrderCode}.",
                    Timestamp = t1,
                    FormattedTime = t1.ToString("dd/MM/yyyy HH:mm:ss"),
                    ChangedByName = h1?.ChangedByName ?? o.CustomerName,
                    IsCompleted = currentStep >= 1,
                    IsCurrent = currentStep == 1
                },
                new()
                {
                    StepIndex = 2,
                    StatusCode = "CONFIRMED",
                    Title = "Xác nhận",
                    Description = h2?.Description ?? "Đã xác nhận thông tin đơn hàng, địa chỉ nhận và giữ chỗ lô hàng.",
                    Timestamp = t2,
                    FormattedTime = currentStep >= 2 ? t2?.ToString("dd/MM/yyyy HH:mm:ss") : null,
                    ChangedByName = h2?.ChangedByName ?? "CSKH Omni Beauty",
                    IsCompleted = currentStep >= 2,
                    IsCurrent = currentStep == 2
                },
                new()
                {
                    StepIndex = 3,
                    StatusCode = "PROCESSING",
                    Title = "Đóng gói (Lô FEFO đã cấp phát)",
                    Description = h3?.Description ?? "Kho chuẩn GSP đã cấp phát lô mỹ phẩm theo nguyên tắc FEFO và đóng gói chống sốc.",
                    Timestamp = t3,
                    FormattedTime = currentStep >= 3 ? t3?.ToString("dd/MM/yyyy HH:mm:ss") : null,
                    ChangedByName = h3?.ChangedByName ?? "Kho Điều Phối FEFO",
                    FefoBatchesSummary = fefoBatchesSummary,
                    IsCompleted = currentStep >= 3,
                    IsCurrent = currentStep == 3
                },
                new()
                {
                    StepIndex = 4,
                    StatusCode = "HANDED_OVER",
                    Title = "Bàn giao vận chuyển",
                    Description = h4?.Description ?? $"Đã xuất kho và bàn giao kiện hàng cho đối tác {carrierName} (Mã vận đơn: {trackingCode}).",
                    Timestamp = t4,
                    FormattedTime = currentStep >= 4 ? t4?.ToString("dd/MM/yyyy HH:mm:ss") : null,
                    ChangedByName = h4?.ChangedByName ?? "Bộ phận Điều phối Kho",
                    TrackingCode = trackingCode,
                    IsCompleted = currentStep >= 4,
                    IsCurrent = currentStep == 4
                },
                new()
                {
                    StepIndex = 5,
                    StatusCode = "SHIPPING",
                    Title = "Đang giao",
                    Description = h5?.Description ?? $"Bưu tá {carrierName} đang trên tuyến giao kiện hàng #{trackingCode} đến người nhận.",
                    Timestamp = t5,
                    FormattedTime = currentStep >= 5 ? t5?.ToString("dd/MM/yyyy HH:mm:ss") : null,
                    ChangedByName = h5?.ChangedByName ?? carrierName,
                    TrackingCode = trackingCode,
                    IsCompleted = currentStep >= 5,
                    IsCurrent = currentStep == 5
                },
                new()
                {
                    StepIndex = 6,
                    StatusCode = "COMPLETED",
                    Title = "Giao thành công",
                    Description = h6?.Description ?? "Kiện hàng đã giao thành công đến tay quý khách. Điểm thưởng Beauty Club đã được cộng vào ví!",
                    Timestamp = t6,
                    FormattedTime = currentStep >= 6 ? t6?.ToString("dd/MM/yyyy HH:mm:ss") : null,
                    ChangedByName = h6?.ChangedByName ?? "Hoàn tất giao nhận",
                    IsCompleted = currentStep >= 6,
                    IsCurrent = currentStep == 6
                }
            };
        }

        private static OrderReturnRequestViewModel MapReturnRequestToViewModel(OrderReturnRequest r, string orderCode)
        {
            var urls = new List<string>();
            if (!string.IsNullOrWhiteSpace(r.EvidenceUrlsJson))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<List<string>>(r.EvidenceUrlsJson);
                    if (parsed != null) urls = parsed;
                }
                catch
                {
                    // Ignore malformed JSON
                }
            }

            return new OrderReturnRequestViewModel
            {
                Id = r.Id,
                OrderId = r.OrderId,
                OrderCode = orderCode,
                RequestCode = r.RequestCode,
                Reason = r.Reason,
                Description = r.Description,
                EvidenceUrls = urls,
                Status = r.Status,
                AdminNote = r.AdminNote,
                CreatedAt = r.CreatedAt,
                ProcessedAt = r.ProcessedAt
            };
        }

        private static string GenerateDeterministicTrackingCode(Order order)
        {
            string suffix = !string.IsNullOrWhiteSpace(order.OrderCode) && order.OrderCode.Length >= 6
                ? order.OrderCode[^6..].Replace("-", "").ToUpperInvariant()
                : $"{order.Id:D6}";
            return $"GHN{order.CreatedAt:yyMMdd}{suffix}";
        }

        public static string NormalizePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return string.Empty;
            string digits = Regex.Replace(phone, @"[^\d]", "");
            if (digits.StartsWith("84") && digits.Length >= 10)
            {
                digits = "0" + digits[2..];
            }
            return digits;
        }

        private static string MaskPhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone) || phone.Length < 7) return "***";
            string clean = phone.Trim();
            return $"{clean[..3]}****{clean[^3..]}";
        }

        private static string MaskAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address) || address.Length <= 15) return address;
            var parts = address.Split(',');
            if (parts.Length >= 2)
            {
                return $"***, {string.Join(",", parts.Skip(1)).Trim()}";
            }
            return address;
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
                        "PROCESSING" => "Đang đóng gói (Lô FEFO đã cấp phát)",
                        "SHIPPING" => "Đã bàn giao vận chuyển & Đang giao hàng",
                        "COMPLETED" => "Đã giao hàng thành công",
                        "CANCELLED" => "Đã hủy & Hoàn tồn FEFO",
                        _ => targetStatus
                    };

                    await _notificationService.CreateAndSendNotificationAsync(new CreateNotificationDto
                    {
                        Type = "ORDER",
                        Severity = targetStatus == "COMPLETED" ? "SUCCESS" : (targetStatus == "CANCELLED" ? "DANGER" : "INFO"),
                        Title = $"📋 Đơn Hàng #{order.OrderCode}",
                        Message = $"Đơn hàng #{order.OrderCode} của bạn đã chuyển sang: {statusText}.",
                        UserId = order.UserId.Value,
                        Url = $"/Customer/OrderDetail/{order.Id}"
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
