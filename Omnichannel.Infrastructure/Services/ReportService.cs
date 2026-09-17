using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Reports;
using Omnichannel.Application.Interfaces.Reports;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RevenueDashboardViewModel> GetRevenueDashboardAsync(string? storeId = null, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var today = DateTime.Today;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
            var sevenDaysAgo = today.AddDays(-6);

            var ordersQuery = _context.Orders
                .AsNoTracking()
                .Where(o => o.PaymentStatus == 2 && o.OrderStatus == 5);

            if (!string.IsNullOrEmpty(storeId))
            {
                ordersQuery = ordersQuery.Where(o => o.StoreId == storeId);
            }

            // Doanh thu & số đơn hôm nay
            var todayOrders = await ordersQuery
                .Where(o => o.CreatedAt >= today)
                .ToListAsync(cancellationToken);

            decimal todayRevenue = todayOrders.Sum(o => o.FinalTotal);
            int todayCount = todayOrders.Count;

            // Doanh thu tháng hiện tại
            decimal monthRevenue = await ordersQuery
                .Where(o => o.CreatedAt >= firstDayOfMonth)
                .SumAsync(o => o.FinalTotal, cancellationToken);

            // Tỷ trọng Kênh POS (1) vs Kênh Online (2, 3, 4) trong 30 ngày qua
            var last30DaysOrders = await ordersQuery
                .Where(o => o.CreatedAt >= today.AddDays(-30))
                .ToListAsync(cancellationToken);

            decimal total30Days = last30DaysOrders.Sum(o => o.FinalTotal);
            decimal posTotal = last30DaysOrders.Where(o => o.OrderChannel == 1).Sum(o => o.FinalTotal);
            decimal onlineTotal = total30Days - posTotal;

            decimal posRatio = total30Days > 0 ? Math.Round((posTotal / total30Days) * 100, 1) : 50;
            decimal onlineRatio = total30Days > 0 ? Math.Round((onlineTotal / total30Days) * 100, 1) : 50;

            // Doanh thu 7 ngày gần nhất
            var last7DaysOrders = await ordersQuery
                .Where(o => o.CreatedAt >= sevenDaysAgo)
                .ToListAsync(cancellationToken);

            var revenueLast7Days = new List<DailyRevenuePointDto>();
            for (int i = 6; i >= 0; i--)
            {
                var targetDate = today.AddDays(-i);
                var dayOrders = last7DaysOrders.Where(o => o.CreatedAt.Date == targetDate.Date).ToList();
                revenueLast7Days.Add(new DailyRevenuePointDto
                {
                    DateLabel = targetDate.ToString("dd/MM"),
                    Revenue = dayOrders.Sum(o => o.FinalTotal),
                    OrderCount = dayOrders.Count
                });
            }

            // Top 5 sản phẩm bán chạy nhất
            var topSelling = await _context.OrderDetails
                .AsNoTracking()
                .Include(od => od.Product)
                .Include(od => od.Order)
                .Where(od => od.Order.PaymentStatus == 2)
                .GroupBy(od => new { od.ProductId, od.Product.ProductName })
                .Select(g => new TopProductDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    SoldQuantity = g.Sum(x => x.Quantity),
                    TotalSales = g.Sum(x => x.LineTotal)
                })
                .OrderByDescending(x => x.SoldQuantity)
                .Take(5)
                .ToListAsync(cancellationToken);

            // Cảnh báo các lô hàng cận date nguy cấp (còn dưới 60 ngày)
            var nearExpiryBatches = await _context.StoreInventories
                .AsNoTracking()
                .Include(si => si.Store)
                .Include(si => si.Batch)
                    .ThenInclude(b => b.Product)
                .Where(si => si.Batch.ExpDate > today && (si.Batch.ExpDate - today).Days <= 60 && si.PhysicalQuantity > 0)
                .OrderBy(si => si.Batch.ExpDate)
                .Take(6)
                .Select(si => new NearExpiryProductDto
                {
                    StoreName = si.Store.StoreName,
                    ProductName = si.Batch.Product.ProductName,
                    BatchNumber = si.Batch.BatchNumber,
                    ExpDate = si.Batch.ExpDate,
                    RemainingQuantity = si.PhysicalQuantity
                })
                .ToListAsync(cancellationToken);

            return new RevenueDashboardViewModel
            {
                TodayRevenue = todayRevenue,
                MonthRevenue = monthRevenue,
                TodayOrderCount = todayCount,
                TodayProfitEstimate = Math.Round(todayRevenue * 0.35m, 0), // Ước tính biên lợi nhuận gộp mỹ phẩm 35%
                PosRevenueRatio = posRatio,
                OnlineRevenueRatio = onlineRatio,
                RevenueLast7Days = revenueLast7Days,
                TopSellingProducts = topSelling,
                CriticalExpiringBatches = nearExpiryBatches
            };
        }
    }
}
