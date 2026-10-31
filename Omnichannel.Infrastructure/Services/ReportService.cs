using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Finance;
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

        public async Task<RevenueDashboardViewModel> GetRevenueDashboardAsync(
            string? storeId = null,
            string period = "7days",
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var today = DateTime.Today;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

            // Xác định khoảng thời gian lọc (filterFrom, filterTo)
            DateTime filterFrom;
            DateTime filterTo;
            string periodNorm = (period ?? "7days").Trim().ToLowerInvariant();

            switch (periodNorm)
            {
                case "today":
                    filterFrom = today;
                    filterTo = today.AddDays(1).AddTicks(-1);
                    break;
                case "30days":
                    filterFrom = today.AddDays(-29);
                    filterTo = today.AddDays(1).AddTicks(-1);
                    break;
                case "custom":
                    filterFrom = fromDate?.Date ?? today.AddDays(-6);
                    filterTo = (toDate?.Date ?? today).AddDays(1).AddTicks(-1);
                    break;
                case "7days":
                default:
                    periodNorm = "7days";
                    filterFrom = today.AddDays(-6);
                    filterTo = today.AddDays(1).AddTicks(-1);
                    break;
            }

            // Danh sách các chi nhánh để lọc
            var allStores = await _context.Stores
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync(cancellationToken);

            var storeSelects = allStores.Select(s => new StoreSelectDto { Id = s.Id, Name = s.Name }).ToList();
            string selectedStoreName = "Toàn hệ thống";

            int? parsedStoreId = null;
            if (!string.IsNullOrWhiteSpace(storeId) && int.TryParse(storeId, out var sid))
            {
                parsedStoreId = sid;
                var found = allStores.FirstOrDefault(s => s.Id == sid);
                if (found != null)
                {
                    selectedStoreName = found.Name;
                }
            }

            // PaymentStatus: "PAID" hoặc mã "2", OrderStatus: "COMPLETED" hoặc mã "5"
            var baseOrdersQuery = _context.Orders
                .AsNoTracking()
                .Where(o => (o.PaymentStatus == "PAID" || o.PaymentStatus == "2") &&
                            (o.OrderStatus == "COMPLETED" || o.OrderStatus == "5"));

            if (parsedStoreId.HasValue)
            {
                baseOrdersQuery = baseOrdersQuery.Where(o => o.StoreId == parsedStoreId.Value);
            }

            // 1. Doanh thu & số đơn hôm nay
            var todayOrders = await baseOrdersQuery
                .Where(o => o.CreatedAt >= today)
                .ToListAsync(cancellationToken);

            var todayRevenue = todayOrders.Sum(o => o.TotalAmount);
            var todayOrderCount = todayOrders.Count;

            // 2. Doanh thu & số đơn trong tháng hiện tại
            var monthOrders = await baseOrdersQuery
                .Where(o => o.CreatedAt >= firstDayOfMonth)
                .ToListAsync(cancellationToken);

            var monthRevenue = monthOrders.Sum(o => o.TotalAmount);
            var monthOrderCount = monthOrders.Count;

            // 3. Tổng tích lũy
            var allCompletedOrders = await baseOrdersQuery.ToListAsync(cancellationToken);
            var totalRevenue = allCompletedOrders.Sum(o => o.TotalAmount);
            var totalOrderCount = allCompletedOrders.Count;

            // 4. Tổng số khách hàng
            var totalCustomers = await _context.Users.AsNoTracking().CountAsync(cancellationToken);

            // 5. Đơn hàng trong kỳ lọc (periodOrders)
            var periodOrders = await baseOrdersQuery
                .Where(o => o.CreatedAt >= filterFrom && o.CreatedAt <= filterTo)
                .Include(o => o.Store)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync(cancellationToken);

            decimal filteredRevenue = periodOrders.Sum(o => o.TotalAmount);
            int filteredOrderCount = periodOrders.Count;
            decimal aov = filteredOrderCount > 0 ? Math.Round(filteredRevenue / filteredOrderCount, 0) : 0m;
            decimal estimatedGross = Math.Round(filteredRevenue * 0.35m, 0);

            // 6. Biểu đồ đường (Line Chart): Theo Ngày (DailyRevenues)
            var dailyRevenues = new List<DailyRevenueDto>();
            int totalDays = (int)(filterTo.Date - filterFrom.Date).TotalDays + 1;
            for (int i = 0; i < totalDays && i <= 90; i++)
            {
                var targetDate = filterFrom.Date.AddDays(i);
                var dayOrders = periodOrders.Where(o => o.CreatedAt.Date == targetDate).ToList();

                dailyRevenues.Add(new DailyRevenueDto
                {
                    Date = targetDate.ToString("dd/MM"),
                    DateLabel = targetDate.ToString("dd/MM/yyyy"),
                    Revenue = dayOrders.Sum(o => o.TotalAmount),
                    OrderCount = dayOrders.Count
                });
            }

            // 7. Biểu đồ đường (Line Chart): Theo Tuần (WeeklyRevenues) - 8 tuần gần nhất
            var weeklyRevenues = new List<WeeklyRevenueDto>();
            var eightWeeksAgo = today.AddDays(-55);
            var weeksOrders = await baseOrdersQuery
                .Where(o => o.CreatedAt >= eightWeeksAgo)
                .ToListAsync(cancellationToken);

            for (int w = 7; w >= 0; w--)
            {
                var wStart = today.AddDays(-7 * w - (int)today.DayOfWeek + 1);
                var wEnd = wStart.AddDays(6).AddDays(1).AddTicks(-1);
                var wOrders = weeksOrders.Where(o => o.CreatedAt >= wStart && o.CreatedAt <= wEnd).ToList();

                weeklyRevenues.Add(new WeeklyRevenueDto
                {
                    WeekLabel = $"{wStart:dd/MM} - {wStart.AddDays(6):dd/MM}",
                    Revenue = wOrders.Sum(o => o.TotalAmount),
                    OrderCount = wOrders.Count
                });
            }

            // 8. Biểu đồ đường (Line Chart): Theo Tháng (MonthlyRevenues) - 6 tháng gần nhất
            var monthlyRevenues = new List<MonthlyRevenueDto>();
            for (int m = 5; m >= 0; m--)
            {
                var mTarget = today.AddMonths(-m);
                var mStart = new DateTime(mTarget.Year, mTarget.Month, 1);
                var mEnd = mStart.AddMonths(1).AddTicks(-1);

                var mOrders = allCompletedOrders.Where(o => o.CreatedAt >= mStart && o.CreatedAt <= mEnd).ToList();
                monthlyRevenues.Add(new MonthlyRevenueDto
                {
                    MonthLabel = $"Tháng {mTarget.Month:D2}/{mTarget.Year}",
                    Revenue = mOrders.Sum(o => o.TotalAmount),
                    OrderCount = mOrders.Count
                });
            }

            // 9. Biểu đồ Bar: So sánh doanh thu giữa các chi nhánh trong kỳ
            var storeDistributions = new List<StoreRevenueDistributionDto>();
            var periodOrdersAllStoresQuery = _context.Orders
                .AsNoTracking()
                .Where(o => (o.PaymentStatus == "PAID" || o.PaymentStatus == "2") &&
                            (o.OrderStatus == "COMPLETED" || o.OrderStatus == "5") &&
                            o.CreatedAt >= filterFrom && o.CreatedAt <= filterTo);

            var periodOrdersAllStores = await periodOrdersAllStoresQuery.ToListAsync(cancellationToken);

            foreach (var st in allStores)
            {
                var stOrders = periodOrdersAllStores.Where(o => o.StoreId == st.Id).ToList();
                storeDistributions.Add(new StoreRevenueDistributionDto
                {
                    StoreId = st.Id,
                    StoreCode = st.Code,
                    StoreName = st.Name,
                    Revenue = stOrders.Sum(o => o.TotalAmount),
                    OrderCount = stOrders.Count
                });
            }

            // 10. Biểu đồ Doughnut: Tỷ trọng kênh bán (POS vs Website)
            var channelDistributions = new List<ChannelRevenueDistributionDto>();
            var posOrders = periodOrders.Where(o => o.OrderSource != "WEBSITE").ToList();
            var webOrders = periodOrders.Where(o => o.OrderSource == "WEBSITE").ToList();

            decimal posRev = posOrders.Sum(o => o.TotalAmount);
            decimal webRev = webOrders.Sum(o => o.TotalAmount);
            decimal sumRev = posRev + webRev;

            channelDistributions.Add(new ChannelRevenueDistributionDto
            {
                ChannelKey = "POS",
                ChannelName = "Điểm bán lẻ tại quầy (POS)",
                Revenue = posRev,
                OrderCount = posOrders.Count,
                Percentage = sumRev > 0 ? Math.Round((posRev / sumRev) * 100, 1) : 0m
            });

            channelDistributions.Add(new ChannelRevenueDistributionDto
            {
                ChannelKey = "WEBSITE",
                ChannelName = "Website thương mại điện tử",
                Revenue = webRev,
                OrderCount = webOrders.Count,
                Percentage = sumRev > 0 ? Math.Round((webRev / sumRev) * 100, 1) : 0m
            });

            // 11. Biểu đồ Area / Bar: Top danh mục bán chạy trong kỳ
            var orderIdsInPeriod = periodOrders.Select(o => o.Id).ToList();
            var topCategoriesQuery = _context.OrderDetails
                .AsNoTracking()
                .Include(od => od.Product)
                    .ThenInclude(p => p.Category)
                .Where(od => orderIdsInPeriod.Contains(od.OrderId) && od.Product != null && od.Product.Category != null);

            var categoryGroups = await topCategoriesQuery
                .GroupBy(od => new { od.Product.CategoryId, od.Product.Category.Name })
                .Select(g => new
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.Name,
                    Revenue = g.Sum(x => x.LineTotal),
                    QuantitySold = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(6)
                .ToListAsync(cancellationToken);

            decimal catSumRevenue = categoryGroups.Sum(c => c.Revenue);
            var topCategories = categoryGroups.Select(c => new CategoryRevenueDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                Revenue = c.Revenue,
                QuantitySold = c.QuantitySold,
                Percentage = catSumRevenue > 0 ? Math.Round((c.Revenue / catSumRevenue) * 100, 1) : 0m
            }).ToList();

            // 12. Danh sách đơn hàng chi tiết (phục vụ bảng xem & Export)
            var detailedOrders = periodOrders.Take(150).Select(o => new OrderReportItemDto
            {
                OrderId = o.Id,
                OrderCode = o.OrderCode,
                CreatedAt = o.CreatedAt,
                StoreName = o.Store?.Name ?? $"Chi nhánh #{o.StoreId}",
                OrderSource = o.OrderSource,
                CustomerName = string.IsNullOrWhiteSpace(o.CustomerName) ? "Khách vãng lai" : o.CustomerName,
                CustomerPhone = o.CustomerPhone ?? "",
                PaymentMethod = o.PaymentMethod,
                OrderStatus = o.OrderStatus,
                PaymentStatus = o.PaymentStatus,
                TotalAmount = o.TotalAmount
            }).ToList();

            // 13. Top 5 sản phẩm bán chạy nhất
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

            // 14. Danh sách 10 đơn hàng gần nhất
            var recentOrders = periodOrders.Take(10).Select(o => new RecentOrderDto
            {
                OrderId = o.Id,
                OrderCode = o.OrderCode,
                CustomerName = o.CustomerName,
                CustomerPhone = o.CustomerPhone,
                TotalAmount = o.TotalAmount,
                OrderStatus = o.OrderStatus,
                PaymentStatus = o.PaymentStatus,
                PaymentMethod = o.PaymentMethod,
                OrderSource = o.OrderSource,
                CreatedAt = o.CreatedAt
            }).ToList();

            // 15. Cảnh báo lô hàng cận date & quá hạn theo thuật toán FEFO
            var fefoThresholdDate = now.AddDays(60);
            var rawExpiringBatches = await _context.ProductBatches
                .AsNoTracking()
                .Include(pb => pb.Product)
                .Where(pb => pb.CurrentQuantity > 0 && (pb.Status == "NEAR_EXPIRATION" || pb.ExpDate <= fefoThresholdDate))
                .OrderBy(pb => pb.ExpDate)
                .Take(10)
                .ToListAsync(cancellationToken);

            var expiringBatches = rawExpiringBatches.Select(pb => new ExpiringBatchDto
            {
                BatchId = pb.Id,
                BatchNumber = pb.BatchNumber,
                ProductId = pb.ProductId,
                ProductName = pb.Product?.Name ?? "N/A",
                ExpDate = pb.ExpDate,
                DaysRemaining = (int)(pb.ExpDate.Date - now.Date).TotalDays,
                Quantity = pb.CurrentQuantity,
                Status = pb.Status
            }).ToList();

            return new RevenueDashboardViewModel
            {
                Period = periodNorm,
                FromDate = filterFrom,
                ToDate = filterTo,
                SelectedStoreId = storeId,
                SelectedStoreName = selectedStoreName,
                Stores = storeSelects,

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

                FilteredRevenue = filteredRevenue,
                FilteredOrderCount = filteredOrderCount,
                AverageOrderValue = aov,
                EstimatedGrossProfit = estimatedGross,

                DailyRevenues = dailyRevenues,
                WeeklyRevenues = weeklyRevenues,
                MonthlyRevenues = monthlyRevenues,
                StoreDistributions = storeDistributions,
                ChannelDistributions = channelDistributions,
                TopCategories = topCategories,
                DetailedOrders = detailedOrders,

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

        public async Task<ProfitLossReportDto> GetProfitLossAsync(int? storeId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
        {
            var stores = await _context.Stores.AsNoTracking().Where(s => s.IsActive).ToListAsync(cancellationToken);
            string? storeName = null;
            if (storeId.HasValue)
            {
                storeName = stores.FirstOrDefault(s => s.Id == storeId.Value)?.Name;
            }

            // 1. Truy vấn các đơn hàng hoàn tất trong khoảng thời gian
            var ordersQuery = _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductBatch)
                .Where(o => (o.PaymentStatus == "PAID" || o.PaymentStatus == "2") &&
                            (o.OrderStatus == "COMPLETED" || o.OrderStatus == "5") &&
                            o.CreatedAt >= from && o.CreatedAt <= to);

            if (storeId.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.StoreId == storeId.Value);
            }

            var orders = await ordersQuery.ToListAsync(cancellationToken);

            // 2. Tính giá vốn bình quân gia quyền (Weighted Average Cost - WAC) cho từng sản phẩm
            var productIds = orders.SelectMany(o => o.OrderDetails).Select(od => od.ProductId).Distinct().ToList();
            var productBatches = await _context.ProductBatches
                .AsNoTracking()
                .Where(pb => productIds.Contains(pb.ProductId))
                .ToListAsync(cancellationToken);

            var productWacMap = new Dictionary<int, decimal>();
            foreach (var pid in productIds)
            {
                var batches = productBatches.Where(pb => pb.ProductId == pid).ToList();
                var totalQty = batches.Sum(b => b.InitialQuantity > 0 ? b.InitialQuantity : b.CurrentQuantity);
                var totalCost = batches.Sum(b => (b.InitialQuantity > 0 ? b.InitialQuantity : b.CurrentQuantity) * b.ImportCost);

                if (totalQty > 0)
                {
                    productWacMap[pid] = Math.Round(totalCost / totalQty, 2);
                }
                else
                {
                    productWacMap[pid] = batches.FirstOrDefault()?.ImportCost ?? 0m;
                }
            }

            // 3. Tính Doanh thu và COGS
            decimal totalRevenue = orders.Sum(o => o.TotalAmount);
            decimal totalCogs = 0m;

            foreach (var order in orders)
            {
                foreach (var detail in order.OrderDetails)
                {
                    decimal unitCost;
                    if (detail.ProductBatch != null && detail.ProductBatch.ImportCost > 0)
                    {
                        unitCost = detail.ProductBatch.ImportCost;
                    }
                    else if (productWacMap.TryGetValue(detail.ProductId, out var wac) && wac > 0)
                    {
                        unitCost = wac;
                    }
                    else
                    {
                        unitCost = Math.Round(detail.UnitPrice * 0.6m, 2); // Ước tính biên an toàn 60%
                    }

                    totalCogs += detail.Quantity * unitCost;
                }
            }

            decimal grossProfit = totalRevenue - totalCogs;
            decimal grossMarginPercent = totalRevenue > 0 ? Math.Round((grossProfit / totalRevenue) * 100, 2) : 0m;

            // 4. Chi phí vận hành (Operating Expenses từ Sổ cái)
            var ledgerQuery = _context.FinancialLedgers
                .AsNoTracking()
                .Where(l => l.EntryType == "EXPENSE" && l.EntryDate >= from && l.EntryDate <= to);

            if (storeId.HasValue)
            {
                ledgerQuery = ledgerQuery.Where(l => l.StoreId == storeId.Value);
            }

            decimal operatingExpenses = await ledgerQuery.SumAsync(l => l.DebitAmount, cancellationToken);
            decimal netProfit = grossProfit - operatingExpenses;
            decimal netMarginPercent = totalRevenue > 0 ? Math.Round((netProfit / totalRevenue) * 100, 2) : 0m;

            // 5. Phân bổ chi tiết theo từng chi nhánh
            var storeBreakdowns = new List<StoreProfitLossItemDto>();
            foreach (var st in stores)
            {
                var stOrders = orders.Where(o => o.StoreId == st.Id).ToList();
                decimal stRev = stOrders.Sum(o => o.TotalAmount);
                decimal stCogs = 0m;

                foreach (var o in stOrders)
                {
                    foreach (var d in o.OrderDetails)
                    {
                        decimal cost = d.ProductBatch?.ImportCost ?? (productWacMap.TryGetValue(d.ProductId, out var w) ? w : d.UnitPrice * 0.6m);
                        stCogs += d.Quantity * cost;
                    }
                }

                decimal stGross = stRev - stCogs;
                var stExp = await _context.FinancialLedgers
                    .AsNoTracking()
                    .Where(l => l.StoreId == st.Id && l.EntryType == "EXPENSE" && l.EntryDate >= from && l.EntryDate <= to)
                    .SumAsync(l => l.DebitAmount, cancellationToken);

                decimal stNet = stGross - stExp;
                decimal area = st.Area > 0 ? st.Area : 120m;
                decimal salesPerSqft = Math.Round(stRev / area, 2);

                storeBreakdowns.Add(new StoreProfitLossItemDto
                {
                    StoreId = st.Id,
                    StoreName = st.Name,
                    Area = area,
                    Revenue = stRev,
                    Cogs = stCogs,
                    GrossProfit = stGross,
                    OperatingExpenses = stExp,
                    NetProfit = stNet,
                    SalesPerSqft = salesPerSqft
                });
            }

            // 6. Tính xu hướng P&L 6 tháng gần nhất (Monthly Trends cho Biểu Đồ Stacked Bar)
            var monthlyTrends = new List<MonthlyProfitLossDto>();
            for (int m = 5; m >= 0; m--)
            {
                var mDate = to.AddMonths(-m);
                var mStart = new DateTime(mDate.Year, mDate.Month, 1);
                var mEnd = mStart.AddMonths(1).AddTicks(-1);

                var mOrders = await _context.Orders
                    .AsNoTracking()
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.ProductBatch)
                    .Where(o => (o.PaymentStatus == "PAID" || o.PaymentStatus == "2") &&
                                (o.OrderStatus == "COMPLETED" || o.OrderStatus == "5") &&
                                o.CreatedAt >= mStart && o.CreatedAt <= mEnd &&
                                (!storeId.HasValue || o.StoreId == storeId.Value))
                    .ToListAsync(cancellationToken);

                decimal mRev = mOrders.Sum(o => o.TotalAmount);
                decimal mCogs = 0m;
                foreach (var o in mOrders)
                {
                    foreach (var d in o.OrderDetails)
                    {
                        decimal cost = d.ProductBatch?.ImportCost ?? (productWacMap.TryGetValue(d.ProductId, out var w) ? w : d.UnitPrice * 0.6m);
                        mCogs += d.Quantity * cost;
                    }
                }

                decimal mGross = mRev - mCogs;

                var mExp = await _context.FinancialLedgers
                    .AsNoTracking()
                    .Where(l => l.EntryType == "EXPENSE" && l.EntryDate >= mStart && l.EntryDate <= mEnd &&
                                (!storeId.HasValue || l.StoreId == storeId.Value))
                    .SumAsync(l => l.DebitAmount, cancellationToken);

                decimal mNet = mGross - mExp;

                monthlyTrends.Add(new MonthlyProfitLossDto
                {
                    MonthLabel = $"T{mDate.Month:D2}/{mDate.Year}",
                    Revenue = mRev,
                    CostOfGoodsSold = mCogs,
                    OperatingExpenses = mExp,
                    GrossProfit = mGross,
                    NetProfit = mNet
                });
            }

            return new ProfitLossReportDto
            {
                StoreId = storeId,
                StoreName = storeName,
                FromDate = from,
                ToDate = to,
                Revenue = totalRevenue,
                CostOfGoodsSold = totalCogs,
                GrossProfit = grossProfit,
                GrossMarginPercent = grossMarginPercent,
                OperatingExpenses = operatingExpenses,
                NetProfit = netProfit,
                NetMarginPercent = netMarginPercent,
                BreakdownByStore = storeBreakdowns,
                MonthlyTrends = monthlyTrends
            };
        }

        public async Task<InventoryTurnoverDto> GetInventoryTurnoverAsync(int? storeId, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var oneYearAgo = now.AddDays(-365);

            // 1. Tính tổng COGS 12 tháng gần nhất
            var ordersQuery = _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductBatch)
                .Where(o => (o.PaymentStatus == "PAID" || o.PaymentStatus == "2") &&
                            (o.OrderStatus == "COMPLETED" || o.OrderStatus == "5") &&
                            o.CreatedAt >= oneYearAgo);

            if (storeId.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.StoreId == storeId.Value);
            }

            var orders = await ordersQuery.ToListAsync(cancellationToken);
            decimal periodCogs = 0m;
            foreach (var o in orders)
            {
                foreach (var d in o.OrderDetails)
                {
                    periodCogs += d.Quantity * (d.ProductBatch?.ImportCost ?? 0m);
                }
            }

            // 2. Tính giá trị hàng tồn kho hiện tại (Average Inventory Value)
            var inventoryQuery = _context.StoreInventories
                .AsNoTracking()
                .Include(si => si.ProductBatch)
                .AsQueryable();

            if (storeId.HasValue)
            {
                inventoryQuery = inventoryQuery.Where(si => si.StoreId == storeId.Value);
            }

            var inventories = await inventoryQuery.ToListAsync(cancellationToken);
            decimal averageInventoryValue = inventories.Sum(si => si.PhysicalQuantity * (si.ProductBatch?.ImportCost ?? 0m));

            decimal turnoverRatio = averageInventoryValue > 0 ? Math.Round(periodCogs / averageInventoryValue, 2) : 0m;
            decimal dio = turnoverRatio > 0 ? Math.Round(365m / turnoverRatio, 1) : 0m;

            string? storeName = null;
            if (storeId.HasValue)
            {
                storeName = (await _context.Stores.FindAsync(new object[] { storeId.Value }, cancellationToken))?.Name;
            }

            return new InventoryTurnoverDto
            {
                StoreId = storeId,
                StoreName = storeName,
                PeriodCogs = periodCogs,
                AverageInventoryValue = averageInventoryValue,
                TurnoverRatio = turnoverRatio,
                DaysSalesOfInventory = dio
            };
        }

        public async Task<SalesPerSqftDto> GetSalesPerSqftAsync(int storeId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
        {
            var store = await _context.Stores.FindAsync(new object[] { storeId }, cancellationToken);
            if (store == null)
            {
                return new SalesPerSqftDto { StoreId = storeId, StoreName = "Không tìm thấy cửa hàng" };
            }

            var fromDate = from ?? DateTime.UtcNow.AddMonths(-1);
            var toDate = to ?? DateTime.UtcNow;

            var orders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.StoreId == storeId &&
                            (o.PaymentStatus == "PAID" || o.PaymentStatus == "2") &&
                            (o.OrderStatus == "COMPLETED" || o.OrderStatus == "5") &&
                            o.CreatedAt >= fromDate && o.CreatedAt <= toDate)
                .ToListAsync(cancellationToken);

            decimal revenue = orders.Sum(o => o.TotalAmount);
            decimal area = store.Area > 0 ? store.Area : 120m;
            decimal salesPerSqm = Math.Round(revenue / area, 2);

            return new SalesPerSqftDto
            {
                StoreId = storeId,
                StoreName = store.Name,
                AreaSquareMeters = area,
                TotalRevenue = revenue,
                SalesPerSquareMeter = salesPerSqm,
                TotalOrders = orders.Count
            };
        }
    }
}