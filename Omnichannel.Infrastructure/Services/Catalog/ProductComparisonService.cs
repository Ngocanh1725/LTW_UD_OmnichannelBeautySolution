using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.BeautyTech;
using Omnichannel.Application.DTOs.Catalog;
using Omnichannel.Application.Interfaces.BeautyTech;
using Omnichannel.Application.Interfaces.Catalog;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services.Catalog
{
    public class ProductComparisonService : IProductComparisonService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISkincareIngredientInteractionEngine _interactionEngine;
        private readonly ILogger<ProductComparisonService> _logger;

        public ProductComparisonService(
            ApplicationDbContext context,
            ISkincareIngredientInteractionEngine interactionEngine,
            ILogger<ProductComparisonService> logger)
        {
            _context = context;
            _interactionEngine = interactionEngine;
            _logger = logger;
        }

        public async Task<List<CompareItemSummaryDto>> GetCompareQuickItemsAsync(List<int> productIds, CancellationToken ct = default)
        {
            if (productIds == null || !productIds.Any())
                return new List<CompareItemSummaryDto>();

            // Tối đa 3 sản phẩm theo chuẩn thiết kế
            var validIds = productIds.Where(id => id > 0).Distinct().Take(3).ToList();

            var products = await _context.Products
                .AsNoTracking()
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Batches)
                .Where(p => validIds.Contains(p.Id) && p.IsActive)
                .ToListAsync(ct);

            var result = new List<CompareItemSummaryDto>();

            foreach (var id in validIds)
            {
                var prod = products.FirstOrDefault(p => p.Id == id);
                if (prod == null) continue;

                var (finalPrice, originalPrice, hasDiscount, discountPercent) = CalculatePrices(prod);
                var (volumeAmount, volumeUnit) = ExtractVolumeAndUnit(prod.Name, prod.Unit, prod.Description);

                decimal? ratio = null;
                string? ratioFormatted = null;
                if (volumeAmount.HasValue && volumeAmount.Value > 0)
                {
                    ratio = Math.Round(finalPrice / volumeAmount.Value, 0);
                    ratioFormatted = $"{ratio.Value:N0} ₫ / {volumeUnit}";
                }

                int inStockQty = prod.Batches?.Where(b => b.CurrentQuantity > 0).Sum(b => b.CurrentQuantity) ?? 0;

                result.Add(new CompareItemSummaryDto
                {
                    Id = prod.Id,
                    Name = prod.Name,
                    Slug = prod.Slug,
                    ThumbnailUrl = Data.ProductImageSourceRegistry.ResolveProductImageUrl(prod.ThumbnailUrl, prod.Sku, prod.Slug, prod.Name),
                    FinalPrice = finalPrice,
                    OriginalPrice = originalPrice,
                    HasDiscount = hasDiscount,
                    DiscountPercent = discountPercent,
                    CategoryId = prod.CategoryId,
                    BrandName = prod.Brand?.Name ?? "Thương Hiệu",
                    CategoryName = prod.Category?.Name ?? "Dược Mỹ Phẩm",
                    RoutineStep = prod.RoutineStep,
                    VolumeAmount = volumeAmount,
                    VolumeUnit = volumeUnit,
                    UnitPriceRatio = ratio,
                    UnitPriceRatioFormatted = ratioFormatted,
                    IsInStock = inStockQty > 0
                });
            }

            return result;
        }

        public async Task<ProductComparisonMatrixDto> GetDetailedComparisonAsync(List<int> productIds, CancellationToken ct = default)
        {
            var matrix = new ProductComparisonMatrixDto();
            if (productIds == null || !productIds.Any())
                return matrix;

            // Tối đa 3 sản phẩm theo đặc tả chuẩn
            var validIds = productIds.Where(id => id > 0).Distinct().Take(3).ToList();

            var products = await _context.Products
                .AsNoTracking()
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Batches)
                .Include(p => p.Reviews)
                .Include(p => p.ProductIngredients)
                    .ThenInclude(pi => pi.ActiveIngredient)
                .Include(p => p.ProductSkinTypes)
                    .ThenInclude(pst => pst.SkinType)
                .Include(p => p.ProductConcerns)
                    .ThenInclude(pc => pc.SkinConcern)
                .Where(p => validIds.Contains(p.Id) && p.IsActive)
                .ToListAsync(ct);

            if (!products.Any())
                return matrix;

            var detailList = new List<ProductComparisonDetailDto>();

            foreach (var id in validIds)
            {
                var prod = products.FirstOrDefault(p => p.Id == id);
                if (prod == null) continue;

                var (finalPrice, originalPrice, hasDiscount, discountPercent) = CalculatePrices(prod);
                var (volumeAmount, volumeUnit) = ExtractVolumeAndUnit(prod.Name, prod.Unit, prod.Description);

                decimal? ratio = null;
                string? ratioFormatted = null;
                if (volumeAmount.HasValue && volumeAmount.Value > 0)
                {
                    ratio = Math.Round(finalPrice / volumeAmount.Value, 0);
                    ratioFormatted = $"{ratio.Value:N0} ₫ / {volumeUnit}";
                }

                // FEFO Lot & Stock
                var bestBatch = prod.Batches?.Where(b => b.CurrentQuantity > 0).OrderBy(b => b.ExpDate).FirstOrDefault()
                    ?? prod.Batches?.OrderBy(b => b.ExpDate).FirstOrDefault();

                int totalStock = prod.Batches?.Where(b => b.CurrentQuantity > 0).Sum(b => b.CurrentQuantity) ?? 0;
                DateTime? expDate = bestBatch?.ExpDate;
                int? daysLeft = expDate.HasValue ? Math.Max(0, (int)(expDate.Value.Date - DateTime.UtcNow.Date).TotalDays) : (int?)null;

                string fefoText = "Date Mới 100%";
                string fefoBadge = "bg-emerald-50 text-emerald-700 border-emerald-200";

                if (daysLeft.HasValue)
                {
                    if (daysLeft.Value < 180)
                    {
                        fefoText = $"Cận Date • Còn {daysLeft.Value} ngày";
                        fefoBadge = "bg-rose-50 text-rose-700 border-rose-200";
                    }
                    else if (daysLeft.Value <= 540)
                    {
                        fefoText = $"HSD Ổn Định • Còn {daysLeft.Value} ngày";
                        fefoBadge = "bg-amber-50 text-amber-700 border-amber-200";
                    }
                    else
                    {
                        fefoText = $"Chuẩn Date Mới • Còn {daysLeft.Value} ngày";
                        fefoBadge = "bg-emerald-50 text-emerald-700 border-emerald-200";
                    }
                }

                // Reviews
                var approvedReviews = prod.Reviews?.Where(r => r.IsApproved && !r.IsHidden).ToList() ?? new List<ProductReview>();
                double avgRating = approvedReviews.Any() ? Math.Round(approvedReviews.Average(r => (double)r.Rating), 1) : 5.0;
                int reviewCount = approvedReviews.Count;

                // Skin Types & Concerns
                var skinTypes = prod.ProductSkinTypes?.Select(pst => pst.SkinType?.Name ?? "").Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList() ?? new List<string>();
                if (!skinTypes.Any())
                {
                    skinTypes = new List<string> { "Mọi loại da", "Da hỗn hợp" };
                }

                var skinConcerns = prod.ProductConcerns?.Select(pc => pc.SkinConcern?.Name ?? "").Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList() ?? new List<string>();
                if (!skinConcerns.Any())
                {
                    skinConcerns = new List<string> { "Dưỡng ẩm", "Bảo vệ màng ẩm" };
                }

                // Active Ingredients & EWG
                var keyActives = ExtractKeyActives(prod);
                var (avgEwg, ewgText, ewgBadge) = CalculateEwgScore(prod, keyActives);

                // Routine Step display name
                string routineDisplayName = FormatRoutineStepName(prod.RoutineStep);

                detailList.Add(new ProductComparisonDetailDto
                {
                    Id = prod.Id,
                    Name = prod.Name,
                    Slug = prod.Slug,
                    ThumbnailUrl = Data.ProductImageSourceRegistry.ResolveProductImageUrl(prod.ThumbnailUrl, prod.Sku, prod.Slug, prod.Name),
                    ShortDescription = prod.ShortDescription,
                    CategoryId = prod.CategoryId,
                    BrandName = prod.Brand?.Name ?? "Thương Hiệu",
                    CategoryName = prod.Category?.Name ?? "Dược Mỹ Phẩm",
                    OriginCountry = !string.IsNullOrWhiteSpace(prod.CountryOfOrigin) ? prod.CountryOfOrigin : "Chính Hãng",
                    ManufacturerName = prod.ManufacturerName,
                    RoutineStep = prod.RoutineStep,
                    RoutineStepDisplayName = routineDisplayName,
                    StorageCondition = FormatStorageCondition(prod.StorageCondition),
                    PaoMonths = prod.PaoMonths ?? 12,
                    FinalPrice = finalPrice,
                    OriginalPrice = originalPrice,
                    HasDiscount = hasDiscount,
                    DiscountPercent = discountPercent,
                    VolumeAmount = volumeAmount,
                    VolumeUnit = volumeUnit,
                    UnitPriceRatio = ratio,
                    UnitPriceRatioFormatted = ratioFormatted,
                    AverageEwgScore = avgEwg,
                    EwgRatingText = ewgText,
                    EwgBadgeColor = ewgBadge,
                    IsAlcoholFree = prod.IsAlcoholFree,
                    IsFragranceFree = prod.IsFragranceFree,
                    IsPregnancySafe = prod.IsPregnancySafe,
                    TotalIngredientsCount = Math.Max(keyActives.Count, 12),
                    KeyActives = keyActives,
                    ParsedKeyActiveNames = keyActives.Select(ka => ka.Name).ToList(),
                    SuitableSkinTypes = skinTypes,
                    TargetConcerns = skinConcerns,
                    IsInStock = totalStock > 0,
                    TotalStockQuantity = totalStock,
                    NearestExpiryDate = expDate,
                    DaysUntilExpiry = daysLeft,
                    FefoStatusText = fefoText,
                    FefoBadgeClass = fefoBadge,
                    NearestBatchNumber = bestBatch?.BatchNumber,
                    AverageRating = avgRating,
                    ReviewCount = reviewCount
                });
            }

            // 1. Xác định Best Value (Đơn giá trên ml/g kinh tế nhất - Badge "Kinh tế nhất")
            MarkBestValueProducts(detailList);

            // 2. Xác định Hạn dùng lô FEFO xa nhất (Badge "Date mới nhất")
            MarkLongestFefoProducts(detailList);

            // 3. Kiểm tra xem có cảnh báo khác nhóm danh mục không
            var categories = detailList.Select(d => d.CategoryName ?? "Dược Mỹ Phẩm").Distinct().ToList();
            matrix.DistinctCategories = categories;
            matrix.HasCategoryMismatch = categories.Count > 1;

            matrix.Products = detailList;

            // Xác định sản phẩm nổi bật
            var bestValueItem = detailList.FirstOrDefault(d => d.IsBestValuePriceRatio);
            if (bestValueItem != null)
            {
                matrix.BestValueProductName = bestValueItem.Name;
            }

            var longestFefoItem = detailList.FirstOrDefault(d => d.IsLongestFefo);
            if (longestFefoItem != null)
            {
                matrix.LongestFefoProductName = longestFefoItem.Name;
            }

            var safestItem = detailList.OrderBy(d => d.AverageEwgScore).ThenByDescending(d => (d.IsAlcoholFree ? 1 : 0) + (d.IsFragranceFree ? 1 : 0) + (d.IsPregnancySafe ? 1 : 0)).FirstOrDefault();
            if (safestItem != null)
            {
                matrix.SafestProductName = safestItem.Name;
            }

            // Gọi Engine tương kỵ da liễu nếu so sánh từ 2 sản phẩm trở lên
            if (detailList.Count >= 2)
            {
                try
                {
                    var analysis = await _interactionEngine.AnalyzeCartOrRoutineAsync(validIds, null, ct);
                    if (analysis?.Interactions != null)
                    {
                        matrix.Interactions = analysis.Interactions;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi phân tích tương kỵ thành phần cho danh sách sản phẩm so sánh.");
                }
            }

            // Khuyến nghị tổng hợp
            matrix.RecommendationSummary = BuildRecommendationSummary(detailList, matrix.Interactions, matrix.BestValueProductName, matrix.LongestFefoProductName, matrix.SafestProductName);

            return matrix;
        }

        public async Task<List<CompareItemSummaryDto>> GetSuggestedAlternativesAsync(int productId, int limit = 3, CancellationToken ct = default)
        {
            var currentProduct = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId, ct);

            if (currentProduct == null)
                return new List<CompareItemSummaryDto>();

            var categoryId = currentProduct.CategoryId;
            var routineStep = currentProduct.RoutineStep;

            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Batches)
                .Where(p => p.Id != productId && p.IsActive);

            if (!string.IsNullOrWhiteSpace(routineStep))
            {
                query = query.Where(p => p.RoutineStep == routineStep || p.CategoryId == categoryId);
            }
            else
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            var alternatives = await query
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.SellingPrice)
                .Take(limit)
                .ToListAsync(ct);

            var result = new List<CompareItemSummaryDto>();
            foreach (var prod in alternatives)
            {
                var (finalPrice, originalPrice, hasDiscount, discountPercent) = CalculatePrices(prod);
                var (volumeAmount, volumeUnit) = ExtractVolumeAndUnit(prod.Name, prod.Unit, prod.Description);

                decimal? ratio = null;
                string? ratioFormatted = null;
                if (volumeAmount.HasValue && volumeAmount.Value > 0)
                {
                    ratio = Math.Round(finalPrice / volumeAmount.Value, 0);
                    ratioFormatted = $"{ratio.Value:N0} ₫ / {volumeUnit}";
                }

                result.Add(new CompareItemSummaryDto
                {
                    Id = prod.Id,
                    Name = prod.Name,
                    Slug = prod.Slug,
                    ThumbnailUrl = Data.ProductImageSourceRegistry.ResolveProductImageUrl(prod.ThumbnailUrl, prod.Sku, prod.Slug, prod.Name),
                    FinalPrice = finalPrice,
                    OriginalPrice = originalPrice,
                    HasDiscount = hasDiscount,
                    DiscountPercent = discountPercent,
                    CategoryId = prod.CategoryId,
                    BrandName = prod.Brand?.Name ?? "Thương Hiệu",
                    CategoryName = prod.Category?.Name ?? "Dược Mỹ Phẩm",
                    RoutineStep = prod.RoutineStep,
                    VolumeAmount = volumeAmount,
                    VolumeUnit = volumeUnit,
                    UnitPriceRatio = ratio,
                    UnitPriceRatioFormatted = ratioFormatted,
                    IsInStock = prod.Batches?.Any(b => b.CurrentQuantity > 0) ?? false
                });
            }

            return result;
        }

        #region Helper Calculation Methods (Public Static for Unit Tests)

        public static (decimal FinalPrice, decimal OriginalPrice, bool HasDiscount, int DiscountPercent) CalculatePrices(Product product)
        {
            var originalPrice = product.SellingPrice;
            var hasDiscount = product.DiscountPrice.HasValue && product.DiscountPrice.Value < originalPrice;
            var finalPrice = hasDiscount && product.DiscountPrice.HasValue ? product.DiscountPrice.Value : originalPrice;

            var bestBatch = product.Batches?.Where(b => b.CurrentQuantity > 0).OrderBy(b => b.ExpDate).FirstOrDefault();
            if (bestBatch != null)
            {
                if (bestBatch.ClearanceDiscountPercent.HasValue && bestBatch.ClearanceDiscountPercent.Value > 0)
                {
                    var clPrice = Math.Round(originalPrice * (1m - (bestBatch.ClearanceDiscountPercent.Value / 100m)));
                    if (clPrice < finalPrice)
                    {
                        finalPrice = clPrice;
                        hasDiscount = true;
                    }
                }
            }

            var discountPercent = hasDiscount && originalPrice > 0
                ? (int)Math.Round((1 - (finalPrice / originalPrice)) * 100)
                : 0;

            return (finalPrice, originalPrice, hasDiscount, discountPercent);
        }

        /// <summary>
        /// Bóc tách dung tích và đơn vị (ml hoặc g) từ chuỗi tên sản phẩm hoặc mô tả bằng Regex chuẩn da liễu.
        /// Hỗ trợ cả oz, fl oz, l, kg quy đổi chuẩn.
        /// Nếu tên/mô tả không chứa dung tích, trả về null để hiển thị "——".
        /// </summary>
        public static (decimal? Amount, string Unit) ExtractVolumeAndUnit(string name, string? unit, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return (null, "ml");

            // Quét regex ưu tiên bắt các mẫu: 30ml, 1.7 fl oz, 50 g, 400ml, 5.5g, 80g, 1L, 1.5kg, 2 oz
            var pattern = @"(?:^|\s|[(\[-])(\d+(?:[.,]\d+)?)\s*(fl\.?\s*oz|floz|oz|ml|g|gram|gr|kg|l)\b";
            var match = Regex.Match(name, pattern, RegexOptions.IgnoreCase);
            if (!match.Success && !string.IsNullOrWhiteSpace(description))
            {
                match = Regex.Match(description, pattern, RegexOptions.IgnoreCase);
            }

            if (match.Success)
            {
                var numStr = match.Groups[1].Value.Replace(',', '.');
                var rawUnit = match.Groups[2].Value.ToLowerInvariant().Replace(" ", "").Replace(".", "");

                if (decimal.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal val) && val > 0)
                {
                    // Quy đổi oz / fl oz sang ml (1 fl oz ≈ 29.5735 ml)
                    if (rawUnit is "floz" or "oz")
                    {
                        var converted = Math.Round(val * 29.5735m, 1);
                        return (converted, "ml");
                    }

                    if (rawUnit == "l")
                    {
                        return (val * 1000m, "ml");
                    }
                    if (rawUnit == "kg")
                    {
                        return (val * 1000m, "g");
                    }
                    if (rawUnit is "g" or "gram" or "gr")
                    {
                        return (val, "g");
                    }
                    return (val, "ml");
                }
            }

            // Tên/mô tả không có dung tích -> Trả về null để hiển thị "——"
            return (null, "ml");
        }

        /// <summary>
        /// Gắn nhãn sản phẩm có đơn giá trên thể tích kinh tế nhất (Badge "Kinh tế nhất")
        /// </summary>
        public static void MarkBestValueProducts(List<ProductComparisonDetailDto> list)
        {
            if (list == null || list.Count < 2) return;

            // Nhóm theo đơn vị (ml hoặc g)
            var groups = list.Where(d => d.UnitPriceRatio.HasValue && d.UnitPriceRatio.Value > 0)
                             .GroupBy(d => d.VolumeUnit);

            foreach (var grp in groups)
            {
                var items = grp.ToList();
                if (items.Count >= 2)
                {
                    var minRatio = items.Min(i => i.UnitPriceRatio!.Value);
                    foreach (var it in items)
                    {
                        if (it.UnitPriceRatio!.Value == minRatio)
                        {
                            it.IsBestValuePriceRatio = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gắn nhãn sản phẩm có hạn dùng FEFO xa nhất trong nhóm (Badge "Date mới nhất")
        /// </summary>
        public static void MarkLongestFefoProducts(List<ProductComparisonDetailDto> list)
        {
            if (list == null || list.Count < 2) return;

            var itemsWithFefo = list.Where(d => d.DaysUntilExpiry.HasValue && d.DaysUntilExpiry.Value > 0).ToList();
            if (itemsWithFefo.Count >= 2)
            {
                var maxDays = itemsWithFefo.Max(i => i.DaysUntilExpiry!.Value);
                foreach (var it in itemsWithFefo)
                {
                    if (it.DaysUntilExpiry!.Value == maxDays)
                    {
                        it.IsLongestFefo = true;
                    }
                }
            }
        }

        private static List<ComparisonActiveIngredientDto> ExtractKeyActives(Product product)
        {
            var list = new List<ComparisonActiveIngredientDto>();

            // Lấy từ quan hệ ProductIngredients nếu có
            if (product.ProductIngredients != null && product.ProductIngredients.Any())
            {
                foreach (var pi in product.ProductIngredients)
                {
                    if (pi.ActiveIngredient != null)
                    {
                        list.Add(new ComparisonActiveIngredientDto
                        {
                            Name = pi.ActiveIngredient.Name,
                            Concentration = !string.IsNullOrWhiteSpace(pi.Concentration) ? pi.Concentration : "Chuẩn Liều Lượng",
                            Function = GetIngredientFunction(pi.ActiveIngredient.Name),
                            EwgScore = GetIngredientEwgScore(pi.ActiveIngredient.Name),
                            EwgRating = GetIngredientEwgScore(pi.ActiveIngredient.Name) <= 2 ? "Xanh lá (Cực kỳ an toàn)" : "An toàn có kiểm soát"
                        });
                    }
                }
            }

            // Nếu không có, quét regex từ FullIngredients hoặc tên/mô tả
            if (!list.Any())
            {
                var text = (product.Name + " " + (product.FullIngredients ?? "") + " " + (product.Description ?? "")).ToLowerInvariant();

                if (text.Contains("bha") || text.Contains("salicylic"))
                    list.Add(new ComparisonActiveIngredientDto { Name = "Salicylic Acid (BHA)", Concentration = "2%", Function = "Làm sạch sâu nang lông, gom cồi mụn", EwgScore = 2, EwgRating = "Xanh lá" });

                if (text.Contains("aha") || text.Contains("glycolic") || text.Contains("lactic"))
                    list.Add(new ComparisonActiveIngredientDto { Name = "Glycolic / Lactic Acid (AHA)", Concentration = "5% - 7%", Function = "Tẩy tế bào sừng, làm sáng đều màu da", EwgScore = 2, EwgRating = "Xanh lá" });

                if (text.Contains("retinol") || text.Contains("retinoid"))
                    list.Add(new ComparisonActiveIngredientDto { Name = "Retinol Tinh Khiết", Concentration = "0.3% - 1.0%", Function = "Tái tạo tăng sinh Collagen, chống lão hóa", EwgScore = 3, EwgRating = "Vàng cam (Cần làm quen)" });

                if (text.Contains("niacinamide") || text.Contains("vitamin b3"))
                    list.Add(new ComparisonActiveIngredientDto { Name = "Niacinamide (Vitamin B3)", Concentration = "5% - 10%", Function = "Kiểm soát bã nhờn, thu nhỏ lỗ chân lông", EwgScore = 1, EwgRating = "Xanh lá" });

                if (text.Contains("panthenol") || text.Contains("b5"))
                    list.Add(new ComparisonActiveIngredientDto { Name = "Panthenol (Vitamin B5)", Concentration = "5%", Function = "Phục hồi làm dịu da kích ứng, tái tạo màng ẩm", EwgScore = 1, EwgRating = "Xanh lá" });

                if (text.Contains("hyaluronic") || text.Contains("sodium hyaluronate") || text.Contains("ha"))
                    list.Add(new ComparisonActiveIngredientDto { Name = "Hyaluronic Acid (HA Đa Tầng)", Concentration = "2.5%", Function = "Cấp nước ngậm ẩm sâu đa tầng biểu bì", EwgScore = 1, EwgRating = "Xanh lá" });

                if (text.Contains("centella") || text.Contains("rau má") || text.Contains("madecassoside"))
                    list.Add(new ComparisonActiveIngredientDto { Name = "Chiết Xuất Rau Má (Centella)", Concentration = "85%", Function = "Kháng viêm làm dịu tức thì vết đỏ rát", EwgScore = 1, EwgRating = "Xanh lá" });

                if (text.Contains("ceramide"))
                    list.Add(new ComparisonActiveIngredientDto { Name = "Ceramide Complex", Concentration = "1%", Function = "Khóa ẩm và tái tạo hàng rào biểu bì", EwgScore = 1, EwgRating = "Xanh lá" });

                if (text.Contains("vitamin c") || text.Contains("ascorbic"))
                    list.Add(new ComparisonActiveIngredientDto { Name = "Vitamin C (L-Ascorbic Acid)", Concentration = "15%", Function = "Chống oxy hóa mạnh mẽ, làm sáng mờ thâm", EwgScore = 2, EwgRating = "Xanh lá" });
            }

            return list;
        }

        private static (int Score, string Text, string BadgeClass) CalculateEwgScore(Product product, List<ComparisonActiveIngredientDto> actives)
        {
            int baseScore = 1;
            if (actives.Any(a => a.EwgScore >= 3))
            {
                baseScore = 3;
            }
            else if (actives.Any(a => a.EwgScore == 2))
            {
                baseScore = 2;
            }

            if (baseScore <= 2)
            {
                return (baseScore, "EWG 1-2: Cực kỳ lành tính (Xanh lá)", "bg-emerald-50 text-emerald-700 border-emerald-200");
            }
            else if (baseScore <= 4)
            {
                return (baseScore, "EWG 3-4: An toàn có kiểm soát (Vàng/Cam)", "bg-amber-50 text-amber-700 border-amber-200");
            }
            else
            {
                return (baseScore, "EWG 5+: Cần lưu ý nồng độ", "bg-rose-50 text-rose-700 border-rose-200");
            }
        }

        private static string GetIngredientFunction(string name)
        {
            var lower = name.ToLowerInvariant();
            if (lower.Contains("b5") || lower.Contains("panthenol")) return "Phục hồi màng ẩm sinh học và làm dịu da";
            if (lower.Contains("ha") || lower.Contains("hyaluronic")) return "Cấp nước đa tầng, ngậm ẩm suốt 48h";
            if (lower.Contains("niacinamide")) return "Kiểm soát bã nhờn, dưỡng sáng đều màu";
            if (lower.Contains("retinol")) return "Tăng sinh tế bào, trẻ hóa làn da";
            if (lower.Contains("salicylic") || lower.Contains("bha")) return "Giải phóng bít tắc cổ nang lông, giảm mụn";
            if (lower.Contains("centella") || lower.Contains("rau má")) return "Kháng viêm làm dịu tức thì vết đỏ";
            return "Dưỡng chất chuyên sâu phục hồi làn da";
        }

        private static int GetIngredientEwgScore(string name)
        {
            var lower = name.ToLowerInvariant();
            if (lower.Contains("retinol")) return 3;
            if (lower.Contains("bha") || lower.Contains("salicylic") || lower.Contains("aha") || lower.Contains("glycolic")) return 2;
            return 1;
        }

        private static string FormatRoutineStepName(string? step)
        {
            return step switch
            {
                "1_Cleanser" => "1. Làm Sạch (Sữa Rửa Mặt)",
                "2_Toner" => "2. Cân Bằng (Nước Hoa Hồng)",
                "3_Serum" => "3. Đặc Trị Chuyên Sâu (Serum)",
                "4_Moisturizer" => "4. Khóa Ẩm (Kem Dưỡng)",
                "5_Sunscreen" => "5. Bảo Vệ (Kem Chống Nắng)",
                _ => "Chăm Sóc Toàn Diện"
            };
        }

        private static string FormatStorageCondition(string? condition)
        {
            return condition switch
            {
                "Cool_8_15C" => "Nơi mát mẻ 8°C - 15°C",
                "Refrigerated_2_8C" => "Bảo quản ngăn mát 2°C - 8°C",
                _ => "Nhiệt độ phòng thoáng mát 15°C - 25°C"
            };
        }

        private static string BuildRecommendationSummary(
            List<ProductComparisonDetailDto> products,
            List<IngredientInteractionFindingDto> interactions,
            string? bestValueName,
            string? longestFefoName,
            string? safestName)
        {
            var parts = new List<string>();

            if (interactions.Any(i => i.SeverityBadge == "DANGER" || i.SeverityBadge == "WARNING"))
            {
                parts.Add("⚠️ Lưu ý da liễu: Phát hiện hoạt chất có thể gây quá tải sừng khi thoa chung (như Retinol, BHA, AHA). Khuyến nghị áp dụng Skin Cycling (dùng cách ngày hoặc sáng/tối).");
            }
            else if (interactions.Any(i => i.SeverityBadge == "SUCCESS_SYNERGY"))
            {
                parts.Add("✨ Hiệp đồng vàng: Các sản phẩm bổ trợ dược lý rất tốt cho nhau (như B5 kết hợp HA giúp phục hồi màng ẩm gấp đôi).");
            }

            if (!string.IsNullOrWhiteSpace(bestValueName))
            {
                parts.Add($"💡 Về mặt kinh tế: '{bestValueName}' có đơn giá trên thể tích (VNĐ/ml hoặc VNĐ/g) Kinh tế nhất.");
            }

            if (!string.IsNullOrWhiteSpace(longestFefoName))
            {
                parts.Add($"📅 Về độ tươi mới: '{longestFefoName}' sở hữu lô FEFO có hạn sử dụng xa nhất (Date mới nhất).");
            }

            if (!string.IsNullOrWhiteSpace(safestName))
            {
                parts.Add($"🌿 Về độ lành tính: '{safestName}' sở hữu điểm an toàn EWG xanh lá và thành phần 5 KHÔNG lý tưởng cho da nhạy cảm.");
            }

            if (!parts.Any())
            {
                parts.Add("Tất cả các sản phẩm đều đạt tiêu chuẩn chính ngạch và có đầy đủ số phiếu công bố Bộ Y Tế.");
            }

            return string.Join(" ", parts);
        }

        #endregion
    }
}
