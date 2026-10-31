using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Alerts;
using Omnichannel.Application.Interfaces.Alerts;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services
{
    public class AlertService : IAlertService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AlertService> _logger;

        public AlertService(ApplicationDbContext context, ILogger<AlertService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> CheckAndGenerateAlertsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Bắt đầu chu trình quét và tạo cảnh báo AI tự động...");
            var newAlerts = new List<Alert>();
            var now = DateTime.UtcNow;
            var today = now.Date;

            try
            {
                // =========================================================================
                // 1. 🔴 HẾT HÀNG (stock = 0): "[Tên SP] tại [Chi nhánh] đã HẾT KHO"
                // =========================================================================
                var storeInventories = await _context.StoreInventories
                    .AsNoTracking()
                    .Include(si => si.Store)
                    .Include(si => si.ProductBatch)
                        .ThenInclude(pb => pb.Product)
                    .Where(si => si.ProductBatch.Product.IsActive)
                    .ToListAsync(cancellationToken);

                var productStoreGroups = storeInventories
                    .GroupBy(si => new
                    {
                        si.StoreId,
                        StoreName = si.Store != null ? si.Store.Name : "Kho tổng",
                        ProductId = si.ProductBatch.ProductId,
                        ProductName = si.ProductBatch.Product != null ? si.ProductBatch.Product.Name : "Sản phẩm",
                        ProductSku = si.ProductBatch.Product != null ? si.ProductBatch.Product.Sku : ""
                    })
                    .Select(g => new
                    {
                        g.Key.StoreId,
                        g.Key.StoreName,
                        g.Key.ProductId,
                        g.Key.ProductName,
                        g.Key.ProductSku,
                        TotalStock = g.Sum(x => x.PhysicalQuantity)
                    })
                    .ToList();

                foreach (var group in productStoreGroups)
                {
                    if (group.TotalStock <= 0)
                    {
                        var entityKey = $"OUT_OF_STOCK_P{group.ProductId}_S{group.StoreId}";
                        if (!await HasRecentAlertAsync(entityKey, TimeSpan.FromHours(12), cancellationToken))
                        {
                            newAlerts.Add(new Alert
                            {
                                Type = "OUT_OF_STOCK",
                                Severity = "DANGER",
                                Title = $"🔴 Hết hàng: {group.ProductName}",
                                Message = $"{group.ProductName} tại {group.StoreName} đã HẾT KHO",
                                IsRead = false,
                                CreatedAt = now,
                                StoreId = group.StoreId,
                                ActionUrl = $"/Admin/Warehouse?storeId={group.StoreId}&searchTerm={Uri.EscapeDataString(group.ProductSku)}",
                                EntityKey = entityKey
                            });
                        }
                    }
                    // =====================================================================
                    // 2. 🟡 SẮP HẾT HÀNG (stock <= reorder point): "[Tên SP] tại [Chi nhánh] chỉ còn [SL] — cần nhập thêm"
                    // Ngưỡng reorder point mặc định cho mỹ phẩm là 10
                    // =====================================================================
                    else if (group.TotalStock <= 10)
                    {
                        var entityKey = $"LOW_STOCK_P{group.ProductId}_S{group.StoreId}";
                        if (!await HasRecentAlertAsync(entityKey, TimeSpan.FromHours(12), cancellationToken))
                        {
                            newAlerts.Add(new Alert
                            {
                                Type = "LOW_STOCK",
                                Severity = "WARNING",
                                Title = $"🟡 Sắp hết hàng: {group.ProductName}",
                                Message = $"{group.ProductName} tại {group.StoreName} chỉ còn {group.TotalStock} — cần nhập thêm",
                                IsRead = false,
                                CreatedAt = now,
                                StoreId = group.StoreId,
                                ActionUrl = $"/Admin/Warehouse?storeId={group.StoreId}&searchTerm={Uri.EscapeDataString(group.ProductSku)}",
                                EntityKey = entityKey
                            });
                        }
                    }
                }

                // =========================================================================
                // 3. 🔴 LÔ HÀNG HẾT HẠN TRONG 15 NGÀY:
                // "Lô [Mã lô] của [Tên SP] sẽ hết hạn trong [N] ngày — ĐỀ XUẤT xả kho giảm giá 40%"
                // =========================================================================
                var urgentThreshold = today.AddDays(15);
                var urgentBatches = await _context.ProductBatches
                    .AsNoTracking()
                    .Include(pb => pb.Product)
                    .Where(pb => pb.CurrentQuantity > 0 && pb.ExpDate.Date >= today && pb.ExpDate.Date <= urgentThreshold)
                    .ToListAsync(cancellationToken);

                foreach (var batch in urgentBatches)
                {
                    int daysRemaining = (int)(batch.ExpDate.Date - today).TotalDays;
                    var entityKey = $"EXPIRY_CRIT_B{batch.Id}_{batch.ExpDate:yyyyMMdd}";
                    if (!await HasRecentAlertAsync(entityKey, TimeSpan.FromHours(24), cancellationToken))
                    {
                        newAlerts.Add(new Alert
                        {
                            Type = "EXPIRY_CRITICAL",
                            Severity = "DANGER",
                            Title = $"🔴 Lô cận date cấp bách: {batch.BatchNumber}",
                            Message = $"Lô {batch.BatchNumber} của {batch.Product?.Name ?? "Mỹ phẩm"} sẽ hết hạn trong {daysRemaining} ngày — ĐỀ XUẤT xả kho giảm giá 40%",
                            IsRead = false,
                            CreatedAt = now,
                            ActionUrl = $"/Admin/Warehouse?searchTerm={Uri.EscapeDataString(batch.BatchNumber)}",
                            EntityKey = entityKey
                        });
                    }
                }

                // =========================================================================
                // 4. 🟡 LÔ HÀNG CẬN DATE 60 NGÀY:
                // "Lô [Mã lô] cận date — xem xét chương trình FEFO Sale"
                // =========================================================================
                var warningThreshold = today.AddDays(60);
                var warningBatches = await _context.ProductBatches
                    .AsNoTracking()
                    .Include(pb => pb.Product)
                    .Where(pb => pb.CurrentQuantity > 0 && pb.ExpDate.Date > urgentThreshold && pb.ExpDate.Date <= warningThreshold)
                    .ToListAsync(cancellationToken);

                foreach (var batch in warningBatches)
                {
                    int daysRemaining = (int)(batch.ExpDate.Date - today).TotalDays;
                    var entityKey = $"EXPIRY_WARN_B{batch.Id}_{batch.ExpDate:yyyyMMdd}";
                    if (!await HasRecentAlertAsync(entityKey, TimeSpan.FromHours(24), cancellationToken))
                    {
                        newAlerts.Add(new Alert
                        {
                            Type = "EXPIRY_WARNING",
                            Severity = "WARNING",
                            Title = $"🟡 Lô hàng cận date: {batch.BatchNumber}",
                            Message = $"Lô {batch.BatchNumber} của {batch.Product?.Name ?? "Mỹ phẩm"} cận date ({daysRemaining} ngày) — xem xét chương trình FEFO Sale",
                            IsRead = false,
                            CreatedAt = now,
                            ActionUrl = $"/Admin/Warehouse?searchTerm={Uri.EscapeDataString(batch.BatchNumber)}",
                            EntityKey = entityKey
                        });
                    }
                }

                // =========================================================================
                // 5. 🟢 DOANH THU BẤT THƯỜNG:
                // "Doanh thu hôm nay TĂNG 150% so với trung bình 7 ngày — kiểm tra?"
                // =========================================================================
                var past7Start = today.AddDays(-7);
                var past7Orders = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.CreatedAt >= past7Start && o.CreatedAt < today && (o.OrderStatus == "COMPLETED" || o.PaymentStatus == "PAID"))
                    .ToListAsync(cancellationToken);

                var todayOrders = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.CreatedAt >= today && o.CreatedAt < today.AddDays(1) && (o.OrderStatus == "COMPLETED" || o.PaymentStatus == "PAID"))
                    .ToListAsync(cancellationToken);

                decimal todayRev = todayOrders.Sum(o => o.TotalAmount);
                decimal past7Rev = past7Orders.Sum(o => o.TotalAmount);
                decimal avg7Rev = past7Rev > 0 ? (past7Rev / 7m) : 0m;

                if (todayRev > 0 && avg7Rev > 0 && todayRev >= (1.5m * avg7Rev))
                {
                    int percentInc = (int)Math.Round(((todayRev - avg7Rev) / avg7Rev) * 100);
                    var entityKey = $"REV_ANOMALY_{today:yyyyMMdd}";
                    if (!await HasRecentAlertAsync(entityKey, TimeSpan.FromHours(12), cancellationToken))
                    {
                        newAlerts.Add(new Alert
                        {
                            Type = "REVENUE_ANOMALY",
                            Severity = "SUCCESS",
                            Title = "🟢 Đột biến doanh thu tích cực",
                            Message = $"Doanh thu hôm nay TĂNG {percentInc}% so với trung bình 7 ngày — kiểm tra?",
                            IsRead = false,
                            CreatedAt = now,
                            ActionUrl = "/Admin/Dashboard",
                            EntityKey = entityKey
                        });
                    }
                }

                // =========================================================================
                // 6. 🔵 ĐƠN HÀNG CHỜ XỬ LÝ LÂU:
                // "[N] đơn hàng chờ xác nhận quá 24h"
                // =========================================================================
                var delayThreshold = now.AddHours(-24);
                var delayedPendingOrdersCount = await _context.Orders
                    .AsNoTracking()
                    .CountAsync(o => o.OrderStatus == "PENDING" && o.CreatedAt <= delayThreshold, cancellationToken);

                if (delayedPendingOrdersCount > 0)
                {
                    var entityKey = $"PENDING_DELAY_{now:yyyyMMdd_HH}";
                    if (!await HasRecentAlertAsync(entityKey, TimeSpan.FromHours(6), cancellationToken))
                    {
                        newAlerts.Add(new Alert
                        {
                            Type = "PENDING_ORDERS_DELAY",
                            Severity = "INFO",
                            Title = "🔵 Đơn hàng tồn đọng chờ xác nhận",
                            Message = $"{delayedPendingOrdersCount} đơn hàng chờ xác nhận quá 24h",
                            IsRead = false,
                            CreatedAt = now,
                            ActionUrl = "/Admin/Order?status=PENDING",
                            EntityKey = entityKey
                        });
                    }
                }

                if (newAlerts.Count > 0)
                {
                    await _context.Alerts.AddRangeAsync(newAlerts, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Đã tạo thành công {Count} cảnh báo tự động mới.", newAlerts.Count);
                }
                else
                {
                    _logger.LogInformation("Chu trình kiểm tra hoàn tất: Không có cảnh báo mới phát sinh.");
                }

                return newAlerts.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chạy CheckAndGenerateAlertsAsync");
                return 0;
            }
        }

        public async Task<PagedAlertResultDto> GetAlertsAsync(AlertFilterDto filter, int? currentUserId = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Alerts
                .AsNoTracking()
                .Include(a => a.Store)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.Type))
            {
                query = query.Where(a => a.Type == filter.Type);
            }

            if (!string.IsNullOrEmpty(filter.Severity))
            {
                query = query.Where(a => a.Severity == filter.Severity);
            }

            if (filter.IsRead.HasValue)
            {
                query = query.Where(a => a.IsRead == filter.IsRead.Value);
            }

            if (filter.StoreId.HasValue)
            {
                query = query.Where(a => a.StoreId == filter.StoreId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim();
                query = query.Where(a => a.Title.Contains(term) || a.Message.Contains(term));
            }

            if (filter.FromDate.HasValue)
            {
                query = query.Where(a => a.CreatedAt >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                var toDateEnd = filter.ToDate.Value.Date.AddDays(1);
                query = query.Where(a => a.CreatedAt < toDateEnd);
            }

            // Stats across all alerts
            var allAlerts = await _context.Alerts.AsNoTracking().ToListAsync(cancellationToken);
            var unreadCount = allAlerts.Count(a => !a.IsRead);
            var dangerCount = allAlerts.Count(a => a.Severity == "DANGER");
            var warningCount = allAlerts.Count(a => a.Severity == "WARNING");
            var successCount = allAlerts.Count(a => a.Severity == "SUCCESS");
            var infoCount = allAlerts.Count(a => a.Severity == "INFO");

            var totalItems = await query.CountAsync(cancellationToken);

            var pageNumber = filter.PageNumber <= 0 ? 1 : filter.PageNumber;
            var pageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;

            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AlertDto
                {
                    Id = a.Id,
                    Type = a.Type,
                    Severity = a.Severity,
                    Title = a.Title,
                    Message = a.Message,
                    IsRead = a.IsRead,
                    CreatedAt = a.CreatedAt,
                    ReadAt = a.ReadAt,
                    UserId = a.UserId,
                    StoreId = a.StoreId,
                    StoreName = a.Store != null ? a.Store.Name : "Toàn hệ thống",
                    ActionUrl = a.ActionUrl,
                    EntityKey = a.EntityKey
                })
                .ToListAsync(cancellationToken);

            return new PagedAlertResultDto
            {
                Items = items,
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize,
                UnreadCount = unreadCount,
                DangerCount = dangerCount,
                WarningCount = warningCount,
                SuccessCount = successCount,
                InfoCount = infoCount
            };
        }

        public async Task<List<AlertDto>> GetLatestAlertsAsync(int take = 10, int? currentUserId = null, CancellationToken cancellationToken = default)
        {
            return await _context.Alerts
                .AsNoTracking()
                .Include(a => a.Store)
                .OrderByDescending(a => a.CreatedAt)
                .Take(take)
                .Select(a => new AlertDto
                {
                    Id = a.Id,
                    Type = a.Type,
                    Severity = a.Severity,
                    Title = a.Title,
                    Message = a.Message,
                    IsRead = a.IsRead,
                    CreatedAt = a.CreatedAt,
                    ReadAt = a.ReadAt,
                    UserId = a.UserId,
                    StoreId = a.StoreId,
                    StoreName = a.Store != null ? a.Store.Name : "Toàn hệ thống",
                    ActionUrl = a.ActionUrl,
                    EntityKey = a.EntityKey
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetUnreadCountAsync(int? currentUserId = null, CancellationToken cancellationToken = default)
        {
            return await _context.Alerts
                .AsNoTracking()
                .CountAsync(a => !a.IsRead, cancellationToken);
        }

        public async Task<AlertSummaryDto> GetAlertSummaryAsync(int? currentUserId = null, CancellationToken cancellationToken = default)
        {
            var alerts = await _context.Alerts
                .AsNoTracking()
                .Include(a => a.Store)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(cancellationToken);

            var latest = alerts.Take(5).Select(a => new AlertDto
            {
                Id = a.Id,
                Type = a.Type,
                Severity = a.Severity,
                Title = a.Title,
                Message = a.Message,
                IsRead = a.IsRead,
                CreatedAt = a.CreatedAt,
                ReadAt = a.ReadAt,
                UserId = a.UserId,
                StoreId = a.StoreId,
                StoreName = a.Store != null ? a.Store.Name : "Toàn hệ thống",
                ActionUrl = a.ActionUrl,
                EntityKey = a.EntityKey
            }).ToList();

            return new AlertSummaryDto
            {
                TotalAlerts = alerts.Count,
                UnreadCount = alerts.Count(a => !a.IsRead),
                DangerCount = alerts.Count(a => a.Severity == "DANGER"),
                WarningCount = alerts.Count(a => a.Severity == "WARNING"),
                SuccessCount = alerts.Count(a => a.Severity == "SUCCESS"),
                InfoCount = alerts.Count(a => a.Severity == "INFO"),
                LatestAlerts = latest
            };
        }

        public async Task<bool> MarkAsReadAsync(int alertId, int? currentUserId = null, CancellationToken cancellationToken = default)
        {
            var alert = await _context.Alerts.FindAsync(new object[] { alertId }, cancellationToken);
            if (alert == null) return false;

            if (!alert.IsRead)
            {
                alert.IsRead = true;
                alert.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
            return true;
        }

        public async Task<int> MarkAllAsReadAsync(int? currentUserId = null, CancellationToken cancellationToken = default)
        {
            var unreadAlerts = await _context.Alerts
                .Where(a => !a.IsRead)
                .ToListAsync(cancellationToken);

            if (unreadAlerts.Count == 0) return 0;

            var now = DateTime.UtcNow;
            foreach (var a in unreadAlerts)
            {
                a.IsRead = true;
                a.ReadAt = now;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return unreadAlerts.Count;
        }

        public async Task<bool> DeleteAlertAsync(int alertId, CancellationToken cancellationToken = default)
        {
            var alert = await _context.Alerts.FindAsync(new object[] { alertId }, cancellationToken);
            if (alert == null) return false;

            _context.Alerts.Remove(alert);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<int> ClearAllReadAlertsAsync(CancellationToken cancellationToken = default)
        {
            var readAlerts = await _context.Alerts
                .Where(a => a.IsRead)
                .ToListAsync(cancellationToken);

            if (readAlerts.Count == 0) return 0;

            _context.Alerts.RemoveRange(readAlerts);
            await _context.SaveChangesAsync(cancellationToken);
            return readAlerts.Count;
        }

        private async Task<bool> HasRecentAlertAsync(string entityKey, TimeSpan window, CancellationToken cancellationToken)
        {
            var threshold = DateTime.UtcNow.Subtract(window);
            return await _context.Alerts
                .AnyAsync(a => a.EntityKey == entityKey && a.CreatedAt >= threshold, cancellationToken);
        }
    }
}
