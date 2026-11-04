using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Catalog;
using Omnichannel.Application.Interfaces.Catalog;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services.Catalog
{
    public class RecommendationService : IRecommendationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RecommendationService> _logger;

        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        public RecommendationService(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<RecommendationService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<FrequentlyBoughtTogetherDto?> GetFrequentlyBoughtTogetherAsync(
            int productId,
            int top = 2,
            CancellationToken cancellationToken = default)
        {
            if (productId <= 0) return null;
            top = Math.Clamp(top, 1, 4);

            string cacheKey = $"FBT_Bundle_v2_{productId}_{top}";
            if (_cache.TryGetValue(cacheKey, out FrequentlyBoughtTogetherDto? cached) && cached != null)
            {
                return cached;
            }

            var currentProduct = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

            if (currentProduct == null || !currentProduct.IsActive)
            {
                return null;
            }

            // 1. THUẬT TOÁN ĐỒNG XUẤT HIỆN (CO-OCCURRENCE) TRÊN ĐƠN HÀNG HOÀN TẤT
            var completedOrders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.OrderStatus == "COMPLETED" || o.OrderStatus == "Completed")
                .Select(o => o.Id)
                .ToListAsync(cancellationToken);

            int totalCompletedOrders = completedOrders.Count;
            var candidateItems = new List<FbtProductItemDto>();
            int bundleOrderCount = 0;
            bool isAlgorithmic = false;

            if (totalCompletedOrders > 0)
            {
                // Danh sách đơn hàng hoàn tất chứa sản phẩm A
                var ordersWithA = await _context.OrderDetails
                    .AsNoTracking()
                    .Where(od => od.ProductId == productId && completedOrders.Contains(od.OrderId))
                    .Select(od => od.OrderId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                int countA = ordersWithA.Count;

                if (countA > 0)
                {
                    // Tìm các sản phẩm khác B xuất hiện trong cùng các đơn hàng này
                    var coOccurrences = await _context.OrderDetails
                        .AsNoTracking()
                        .Where(od => ordersWithA.Contains(od.OrderId) && od.ProductId != productId)
                        .GroupBy(od => od.ProductId)
                        .Select(g => new
                        {
                            ProductId = g.Key,
                            CountAB = g.Select(x => x.OrderId).Distinct().Count()
                        })
                        .OrderByDescending(x => x.CountAB)
                        .Take(10)
                        .ToListAsync(cancellationToken);

                    if (coOccurrences.Any())
                    {
                        var candidateIds = coOccurrences.Select(c => c.ProductId).ToList();
                        var candidateProducts = await _context.Products
                            .AsNoTracking()
                            .Include(p => p.Category)
                            .Include(p => p.Brand)
                            .Where(p => candidateIds.Contains(p.Id) && p.IsActive)
                            .ToDictionaryAsync(p => p.Id, cancellationToken);

                        // Tính CountB trên toàn bộ đơn hoàn tất để tính Lift
                        var countsB = await _context.OrderDetails
                            .AsNoTracking()
                            .Where(od => candidateIds.Contains(od.ProductId) && completedOrders.Contains(od.OrderId))
                            .GroupBy(od => od.ProductId)
                            .Select(g => new
                            {
                                ProductId = g.Key,
                                CountB = g.Select(x => x.OrderId).Distinct().Count()
                            })
                            .ToDictionaryAsync(x => x.ProductId, x => x.CountB, cancellationToken);

                        foreach (var co in coOccurrences)
                        {
                            if (!candidateProducts.TryGetValue(co.ProductId, out var prod)) continue;

                            int countB = countsB.TryGetValue(co.ProductId, out int cb) ? cb : 1;
                            double confidence = (double)co.CountAB / countA;
                            double lift = ((double)co.CountAB * totalCompletedOrders) / ((double)countA * countB);

                            // Lọc các ứng viên có độ tương quan dương (Lift >= 1.0)
                            if (lift >= 1.0 || co.CountAB >= 2)
                            {
                                candidateItems.Add(new FbtProductItemDto
                                {
                                    Id = prod.Id,
                                    Name = prod.Name,
                                    Slug = prod.Slug,
                                    Sku = prod.Sku,
                                    ImageUrl = Data.ProductImageSourceRegistry.ResolveProductImageUrl(prod.ThumbnailUrl, prod.Sku, prod.Slug, prod.Name),
                                    Price = prod.SellingPrice,
                                    SalePrice = (prod.DiscountPrice.HasValue && prod.DiscountPrice.Value > 0 && prod.DiscountPrice.Value < prod.SellingPrice)
                                        ? prod.DiscountPrice.Value
                                        : null,
                                    BrandName = prod.Brand?.Name ?? prod.ManufacturerName ?? "Omni Beauty",
                                    CategoryName = prod.Category?.Name ?? "Mỹ Phẩm",
                                    RoutineStep = prod.RoutineStep,
                                    CoOccurrenceCount = co.CountAB,
                                    Confidence = Math.Round(confidence, 2),
                                    Lift = Math.Round(lift, 2),
                                    RecommendationReason = $"Thường mua cùng ({co.CountAB} đơn hàng, Lift: {lift:F1})",
                                    IsCurrent = false,
                                    IsSelected = true
                                });

                                if (candidateItems.Count >= top)
                                {
                                    break;
                                }
                            }
                        }

                        if (candidateItems.Any())
                        {
                            isAlgorithmic = true;
                            bundleOrderCount = candidateItems.Min(c => c.CoOccurrenceCount);
                        }
                    }
                }
            }

            // 2. FALLBACK: NẾU THIẾU DỮ LIỆU ĐỒNG XUẤT HIỆN -> GỢI Ý CHU TRÌNH SKINCARE SYNERGY
            if (candidateItems.Count < top)
            {
                int needed = top - candidateItems.Count;
                var excludeIds = new HashSet<int>(candidateItems.Select(c => c.Id)) { productId };

                var fallbackProducts = await GetSkincareSynergyProductsAsync(currentProduct, needed, excludeIds, cancellationToken);
                foreach (var p in fallbackProducts)
                {
                    candidateItems.Add(new FbtProductItemDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Slug = p.Slug,
                        Sku = p.Sku,
                        ImageUrl = Data.ProductImageSourceRegistry.ResolveProductImageUrl(p.ThumbnailUrl, p.Sku, p.Slug, p.Name),
                        Price = p.SellingPrice,
                        SalePrice = (p.DiscountPrice.HasValue && p.DiscountPrice.Value > 0 && p.DiscountPrice.Value < p.SellingPrice)
                            ? p.DiscountPrice.Value
                            : null,
                        BrandName = p.Brand?.Name ?? p.ManufacturerName ?? "Omni Beauty",
                        CategoryName = p.Category?.Name ?? "Mỹ Phẩm",
                        RoutineStep = p.RoutineStep,
                        CoOccurrenceCount = 0,
                        Confidence = 0.85,
                        Lift = 1.8,
                        RecommendationReason = GetRoutineReason(currentProduct.RoutineStep, p.RoutineStep),
                        IsCurrent = false,
                        IsSelected = true
                    });
                }
            }

            if (!candidateItems.Any())
            {
                return null;
            }

            // 3. XÂY DỰNG DTO HOÀN CHỈNH
            var currentDto = new FbtProductItemDto
            {
                Id = currentProduct.Id,
                Name = currentProduct.Name,
                Slug = currentProduct.Slug,
                Sku = currentProduct.Sku,
                ImageUrl = Data.ProductImageSourceRegistry.ResolveProductImageUrl(currentProduct.ThumbnailUrl, currentProduct.Sku, currentProduct.Slug, currentProduct.Name),
                Price = currentProduct.SellingPrice,
                SalePrice = (currentProduct.DiscountPrice.HasValue && currentProduct.DiscountPrice.Value > 0 && currentProduct.DiscountPrice.Value < currentProduct.SellingPrice)
                    ? currentProduct.DiscountPrice.Value
                    : null,
                BrandName = currentProduct.Brand?.Name ?? currentProduct.ManufacturerName ?? "Omni Beauty",
                CategoryName = currentProduct.Category?.Name ?? "Mỹ Phẩm",
                RoutineStep = currentProduct.RoutineStep,
                RecommendationReason = "Sản phẩm bạn đang xem",
                IsCurrent = true,
                IsSelected = true
            };

            decimal totalOriginal = currentDto.Price + candidateItems.Sum(c => c.Price);
            decimal totalBundle = (currentDto.SalePrice ?? currentDto.Price) + candidateItems.Sum(c => c.SalePrice ?? c.Price);

            // Ưu đãi combo: Nếu tất cả đều là giá gốc, áp dụng chiết khấu combo 5%
            if (totalBundle == totalOriginal)
            {
                totalBundle = Math.Round(totalOriginal * 0.95m);
            }

            decimal totalDiscount = Math.Max(0m, totalOriginal - totalBundle);

            string explanation = isAlgorithmic && bundleOrderCount > 0
                ? $"Dựa trên {bundleOrderCount} đơn hàng hoàn tất thường xuyên mua kèm các sản phẩm này."
                : "Gợi ý kết hợp chuẩn chu trình chăm sóc da khoa học giúp tối ưu hiệu quả dưỡng chất.";

            var result = new FrequentlyBoughtTogetherDto
            {
                CurrentProduct = currentDto,
                BundleProducts = candidateItems,
                TotalOriginalPrice = totalOriginal,
                TotalBundlePrice = totalBundle,
                TotalDiscount = totalDiscount,
                BundleOrderCount = bundleOrderCount,
                TotalOrdersAnalyzed = totalCompletedOrders,
                Explanation = explanation,
                IsAlgorithmic = isAlgorithmic
            };

            _cache.Set(cacheKey, result, CacheDuration);
            return result;
        }

        public async Task<List<RecentlyViewedProductDto>> GetProductsByIdsAsync(
            IEnumerable<int> productIds,
            CancellationToken cancellationToken = default)
        {
            if (productIds == null) return new List<RecentlyViewedProductDto>();

            var idList = productIds.Where(id => id > 0).Distinct().Take(12).ToList();
            if (!idList.Any()) return new List<RecentlyViewedProductDto>();

            var products = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => idList.Contains(p.Id) && p.IsActive)
                .ToListAsync(cancellationToken);

            var reviewStats = await _context.ProductReviews
                .AsNoTracking()
                .Where(r => idList.Contains(r.ProductId) && r.IsApproved && !r.IsHidden)
                .GroupBy(r => r.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    AvgRating = g.Average(r => r.Rating),
                    TotalReviews = g.Count()
                })
                .ToDictionaryAsync(x => x.ProductId, cancellationToken);

            var dict = products.ToDictionary(p => p.Id);
            var result = new List<RecentlyViewedProductDto>();

            // Duyệt theo đúng thứ tự mảng ID đầu vào để sản phẩm vừa xem mới nhất luôn đứng đầu
            foreach (var id in idList)
            {
                if (dict.TryGetValue(id, out var p))
                {
                    reviewStats.TryGetValue(id, out var stat);
                    decimal? salePrice = (p.DiscountPrice.HasValue && p.DiscountPrice.Value > 0 && p.DiscountPrice.Value < p.SellingPrice)
                        ? p.DiscountPrice.Value
                        : null;

                    result.Add(new RecentlyViewedProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Slug = p.Slug,
                        Sku = p.Sku,
                        ImageUrl = Data.ProductImageSourceRegistry.ResolveProductImageUrl(p.ThumbnailUrl, p.Sku, p.Slug, p.Name),
                        Price = p.SellingPrice,
                        SalePrice = salePrice,
                        BrandName = p.Brand?.Name ?? p.ManufacturerName ?? "Omni Beauty",
                        CategoryName = p.Category?.Name ?? "Mỹ Phẩm",
                        RoutineStep = p.RoutineStep,
                        Rating = stat != null ? Math.Round(stat.AvgRating, 1) : 5.0,
                        ReviewCount = stat != null ? stat.TotalReviews : 0,
                        IsFeatured = p.IsFeatured,
                        InStock = true
                    });
                }
            }

            return result;
        }

        private async Task<List<Product>> GetSkincareSynergyProductsAsync(
            Product current,
            int count,
            HashSet<int> excludeIds,
            CancellationToken cancellationToken)
        {
            var preferredSteps = GetComplementaryRoutineSteps(current.RoutineStep);
            var results = new List<Product>();

            if (preferredSteps.Any())
            {
                foreach (var step in preferredSteps)
                {
                    var match = await _context.Products
                        .AsNoTracking()
                        .Include(p => p.Category)
                        .Include(p => p.Brand)
                        .Where(p => p.RoutineStep == step && !excludeIds.Contains(p.Id) && p.IsActive)
                        .OrderByDescending(p => p.IsFeatured)
                        .ThenByDescending(p => p.Id)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (match != null)
                    {
                        results.Add(match);
                        excludeIds.Add(match.Id);
                        if (results.Count >= count) return results;
                    }
                }
            }

            // Nếu vẫn chưa đủ, lấy sản phẩm cùng Danh mục hoặc cùng Thương hiệu bán chạy
            if (results.Count < count)
            {
                var fallback = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Where(p => (p.CategoryId == current.CategoryId || p.BrandId == current.BrandId) && !excludeIds.Contains(p.Id) && p.IsActive)
                    .OrderByDescending(p => p.IsFeatured)
                    .ThenByDescending(p => p.Id)
                    .Take(count - results.Count)
                    .ToListAsync(cancellationToken);

                results.AddRange(fallback);
            }

            return results;
        }

        private static List<string> GetComplementaryRoutineSteps(string? currentStep)
        {
            return currentStep switch
            {
                "1_Cleanser" => new List<string> { "2_Toner", "4_Moisturizer", "3_Serum" },
                "2_Toner" => new List<string> { "3_Serum", "4_Moisturizer", "1_Cleanser" },
                "3_Serum" => new List<string> { "4_Moisturizer", "2_Toner", "1_Cleanser" },
                "4_Moisturizer" => new List<string> { "3_Serum", "1_Cleanser", "2_Toner" },
                "5_Sunscreen" => new List<string> { "1_Cleanser", "4_Moisturizer", "3_Serum" },
                _ => new List<string> { "2_Toner", "3_Serum", "4_Moisturizer" }
            };
        }

        private static string GetRoutineReason(string? fromStep, string? toStep)
        {
            return (fromStep, toStep) switch
            {
                ("1_Cleanser", "2_Toner") => "Bước 2: Cân bằng ẩm ngay sau khi rửa mặt",
                ("1_Cleanser", "4_Moisturizer") => "Bước 3: Khóa ẩm chuyên sâu phục hồi màng bảo vệ",
                ("2_Toner", "3_Serum") => "Bước 3: Tinh chất thẩm thấu sâu trên nền da ngậm nước",
                ("2_Toner", "4_Moisturizer") => "Bước 4: Khóa dưỡng chất bảo vệ làn da",
                ("3_Serum", "4_Moisturizer") => "Bước 4: Khóa ẩm ngăn ngừa bay hơi tinh chất",
                ("4_Moisturizer", "3_Serum") => "Bước 3: Tăng cường dưỡng chất chuyên sâu trước khi khóa ẩm",
                _ => "Chu trình kết hợp hoàn hảo theo khuyến nghị da liễu"
            };
        }
    }
}
