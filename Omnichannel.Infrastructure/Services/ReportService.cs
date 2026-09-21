using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Reports;
using Omnichannel.Application.Interfaces.Reports;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services
{
    /// <summary>
    /// Dịch vụ tính toán báo cáo doanh thu đa kênh và kiểm soát hạn sử dụng FEFO
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<RevenueDashboardViewModel> GetRevenueDashboardAsync(string? storeId = null, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var today = DateTime.Today;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
            var sevenDaysAgo = today.AddDays(-6);

            // PaymentStatus: "PAID" hoặc mã "2", OrderStatus: "COMPLETED" hoặc mã "5"
            var ordersQuery = _context.Orders
                .AsNoTracking()
                .Where(o => (o.PaymentStatus == "PAID" || o.PaymentStatus == "2") &&
                            (o.OrderStatus == "COMPLETED" || o.OrderStatus == "5"));

            int? parsedStoreId = null;
            if (!string.IsNullOrWhiteSpace(storeId) && int.TryParse(storeId, out var sid))
            {
                parsedStoreId = sid;
                ordersQuery = ordersQuery.Where(o => o.StoreId == sid);
            }

            // 1. Doanh thu & số đơn hôm nay
            var todayOrders = await ordersQuery
                .Where(o => o.CreatedAt >= today)
                .ToListAsync(cancellationToken);

            var todayRevenue = todayOrders.Sum(o => o.TotalAmount);
            var todayOrderCount = todayOrders.Count;

            // 2. Doanh thu & số đơn trong tháng hiện tại
            var monthOrders = await ordersQuery
                .Where(o => o.CreatedAt >= firstDayOfMonth)
                .ToListAsync(cancellationToken);

            var monthRevenue = monthOrders.Sum(o => o.TotalAmount);
            var monthOrderCount = monthOrders.Count;

            // 3. Tổng tích lũy toàn hệ thống
            var allCompletedOrders = await ordersQuery.ToListAsync(cancellationToken);
            var totalRevenue = allCompletedOrders.Sum(o => o.TotalAmount);
            var totalOrderCount = allCompletedOrders.Count;

            // 4. Tổng số khách hàng đăng ký
            var totalCustomers = await _context.Users
                .AsNoTracking()
                .CountAsync(cancellationToken);

            // 5. Biểu đồ doanh thu 7 ngày gần nhất (Daily Revenue Chart)
            var last7DaysQuery = _context.Orders
                .AsNoTracking()
                .Where(o => (o.PaymentStatus == "PAID" || o.PaymentStatus == "2") &&
                            (o.OrderStatus == "COMPLETED" || o.OrderStatus == "5") &&
                            o.CreatedAt >= sevenDaysAgo);

            if (parsedStoreId.HasValue)
            {
                last7DaysQuery = last7DaysQuery.Where(o => o.StoreId == parsedStoreId.Value);
            }

            var last7DaysOrders = await last7DaysQuery.ToListAsync(cancellationToken);

            var dailyRevenues = new List<DailyRevenueDto>();
            for (int i = 0; i < 7; i++)
            {
                var targetDate = sevenDaysAgo.AddDays(i);
                var dayOrders = last7DaysOrders.Where(o => o.CreatedAt.Date == targetDate.Date).ToList();

                dailyRevenues.Add(new DailyRevenueDto
                {
                    Date = targetDate.ToString("dd/MM"),
                    DateLabel = targetDate.ToString("dd/MM/yyyy"),
                    Revenue = dayOrders.Sum(o => o.TotalAmount),
                    OrderCount = dayOrders.Count
                });
            }

            // 6. Top 5 sản phẩm bán chạy nhất
            var topProductsQuery = _context.OrderDetails
                .AsNoTracking()
                .Include(od => od.Product)
                .Include(od => od.Order)
                .Where(od => (od.Order.PaymentStatus == "PAID" || od.Order.PaymentStatus == "2") &&
                             (od.Order.OrderStatus == "COMPLETED" || od.Order.OrderStatus == "5"));

            if (parsedStoreId.HasValue)
            {
                topProductsQuery = topProductsQuery.Where(od => od.Order.StoreId == parsedStoreId.Value);
            }

            var topSellingProducts = await topProductsQuery
                .GroupBy(od => new { od.ProductId, od.Product.Name, od.Product.Sku, od.Product.ThumbnailUrl })
                .Select(g => new TopProductDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name,
                    Sku = g.Key.Sku,
                    ThumbnailUrl = g.Key.ThumbnailUrl,
                    QuantitySold = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.LineTotal)
                })
                .OrderByDescending(x => x.QuantitySold)
                .Take(5)
                .ToListAsync(cancellationToken);

            // 7. Danh sách 10 đơn hàng gần nhất
            var recentOrdersQuery = _context.Orders
                .AsNoTracking();

            if (parsedStoreId.HasValue)
            {
                recentOrdersQuery = recentOrdersQuery.Where(o => o.StoreId == parsedStoreId.Value);
            }

            var recentOrders = await recentOrdersQuery
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .Select(o => new RecentOrderDto
                {
                    OrderId = o.Id,
                    OrderCode = o.OrderCode,
                    CustomerName = o.CustomerName,
                    CustomerPhone = o.CustomerPhone,
                    TotalAmount = o.TotalAmount,
                    OrderStatus = o.OrderStatus,
                    PaymentStatus = o.PaymentStatus,
                    PaymentMethod = o.PaymentMethod,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync(cancellationToken);

            // 8. Cảnh báo lô hàng cận date & quá hạn theo thuật toán FEFO
            var fefoThresholdDate = now.AddDays(60);
            var expiringBatches = await _context.ProductBatches
                .AsNoTracking()
                .Include(pb => pb.Product)
                .Where(pb => pb.CurrentQuantity > 0 && (pb.Status == "NEAR_EXPIRATION" || pb.ExpDate <= fefoThresholdDate))
                .OrderBy(pb => pb.ExpDate)
                .Take(10)
                .Select(pb => new ExpiringBatchDto
                {
                    BatchId = pb.Id,
                    BatchNumber = pb.BatchNumber,
                    ProductId = pb.ProductId,
                    ProductName = pb.Product.Name,
                    ExpDate = pb.ExpDate,
                    DaysRemaining = (int)(pb.ExpDate.Date - now.Date).TotalDays,
                    Quantity = pb.CurrentQuantity,
                    Status = pb.Status
                })
                .ToListAsync(cancellationToken);

            return new RevenueDashboardViewModel
            {
                TodayRevenue = todayRevenue,
                TodayOrderCount = todayOrderCount,
                TodayOrdersCount = todayOrderCount,
                MonthRevenue = monthRevenue,
                MonthOrderCount = monthOrderCount,
                MonthOrdersCount = monthOrderCount,
                TotalRevenue = totalRevenue,
                TotalOrderCount = totalOrderCount,
                TotalOrdersCount = totalOrderCount,
                TotalCustomers = totalCustomers,
                DailyRevenues = dailyRevenues,
                TopSellingProducts = topSellingProducts,
                TopProducts = topSellingProducts,
                RecentOrders = recentOrders,
                ExpiringBatches = expiringBatches,
                FefoAlertBatches = expiringBatches
            };
        }

        public async Task<List<TopProductDto>> GetTopSellingProductsAsync(int top = 5, string? storeId = null, CancellationToken cancellationToken = default)
        {
            var query = _context.OrderDetails
                .AsNoTracking()
                .Include(od => od.Product)
                .Include(od => od.Order)
                .Where(od => (od.Order.PaymentStatus == "PAID" || od.Order.PaymentStatus == "2") &&
                             (od.Order.OrderStatus == "COMPLETED" || od.Order.OrderStatus == "5"));

            if (!string.IsNullOrWhiteSpace(storeId) && int.TryParse(storeId, out var sid))
            {
                query = query.Where(od => od.Order.StoreId == sid);
            }

            return await query
                .GroupBy(od => new { od.ProductId, od.Product.Name, od.Product.Sku, od.Product.ThumbnailUrl })
                .Select(g => new TopProductDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name,
                    Sku = g.Key.Sku,
                    ThumbnailUrl = g.Key.ThumbnailUrl,
                    QuantitySold = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.LineTotal)
                })
                .OrderByDescending(x => x.QuantitySold)
                .Take(top)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<ExpiringBatchDto>> GetFefoExpiryReportAsync(int daysThreshold = 60, string? storeId = null, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var thresholdDate = now.AddDays(daysThreshold);

            var query = _context.ProductBatches
                .AsNoTracking()
                .Include(pb => pb.Product)
                .Where(pb => pb.CurrentQuantity > 0 && (pb.Status == "NEAR_EXPIRATION" || pb.ExpDate <= thresholdDate));

            return await query
                .OrderBy(pb => pb.ExpDate)
                .Select(pb => new ExpiringBatchDto
                {
                    BatchId = pb.Id,
                    BatchNumber = pb.BatchNumber,
                    ProductId = pb.ProductId,
                    ProductName = pb.Product.Name,
                    ExpDate = pb.ExpDate,
                    DaysRemaining = (int)(pb.ExpDate.Date - now.Date).TotalDays,
                    Quantity = pb.CurrentQuantity,
                    Status = pb.Status
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<DailyRevenueDto>> GetRevenueByDateRangeAsync(DateTime fromDate, DateTime toDate, string? storeId = null, CancellationToken cancellationToken = default)
        {
            var ordersQuery = _context.Orders
                .AsNoTracking()
                .Where(o => (o.PaymentStatus == "PAID" || o.PaymentStatus == "2") &&
                            (o.OrderStatus == "COMPLETED" || o.OrderStatus == "5") &&
                            o.CreatedAt >= fromDate && o.CreatedAt <= toDate);

            if (!string.IsNullOrWhiteSpace(storeId) && int.TryParse(storeId, out var sid))
            {
                ordersQuery = ordersQuery.Where(o => o.StoreId == sid);
            }

            var orders = await ordersQuery.ToListAsync(cancellationToken);
            var results = new List<DailyRevenueDto>();

            for (var day = fromDate.Date; day <= toDate.Date; day = day.AddDays(1))
            {
                var currentDayOrders = orders.Where(o => o.CreatedAt.Date == day).ToList();
                results.Add(new DailyRevenueDto
                {
                    Date = day.ToString("dd/MM"),
                    DateLabel = day.ToString("dd/MM/yyyy"),
                    Revenue = currentDayOrders.Sum(o => o.TotalAmount),
                    OrderCount = currentDayOrders.Count
                });
            }

            return results;
        }
    }
}