using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Catalog;
using Omnichannel.Application.Interfaces.Catalog;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services.Catalog
{
    public class SearchSuggestService : ISearchSuggestService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SearchSuggestService> _logger;

        private const string CatalogCacheKey = "SearchSuggest_CatalogSnapshot_v2";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        private static readonly string[] DefaultHotKeywords = new[]
        {
            "Serum B5 phục hồi",
            "Kem chống nắng kiềm dầu",
            "Son lì đỏ gạch",
            "Nước tẩy trang Micellar",
            "Retinol tái tạo da",
            "Toner hoa hồng cấp ẩm",
            "Kem dưỡng ẩm Ceramide",
            "Sữa rửa mặt BHA"
        };

        public SearchSuggestService(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<SearchSuggestService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<SearchSuggestionDto> GetSuggestionsAsync(
            string? query,
            int maxProducts = 6,
            int maxBrands = 4,
            int maxCategories = 4,
            CancellationToken cancellationToken = default)
        {
            var rawQuery = query?.Trim() ?? string.Empty;
            var normQuery = RemoveDiacritics(rawQuery);

            var snapshot = await GetOrLoadCatalogSnapshotAsync(cancellationToken);

            var result = new SearchSuggestionDto
            {
                Query = rawQuery,
                NormalizedQuery = normQuery
            };

            // Trường hợp truy vấn rỗng hoặc < 2 ký tự: trả về từ khóa hot & sản phẩm/thương hiệu nổi bật
            if (string.IsNullOrWhiteSpace(normQuery) || normQuery.Length < 2)
            {
                result.HotKeywords = DefaultHotKeywords.Take(5).ToList();
                result.Products = snapshot.Products
                    .Where(p => p.IsFeatured)
                    .OrderByDescending(p => p.Id)
                    .Take(maxProducts)
                    .Select(MapToDto)
                    .ToList();

                result.Brands = snapshot.Brands
                    .Take(maxBrands)
                    .Select(MapBrandToDto)
                    .ToList();

                result.Categories = snapshot.Categories
                    .Take(maxCategories)
                    .Select(MapCategoryToDto)
                    .ToList();

                result.TotalMatches = result.Products.Count;
                return result;
            }

            // 1. TÌM KIẾM & XẾP HẠNG SẢN PHẨM
            var scoredProducts = new List<(CachedProduct Product, int Score)>();
            foreach (var p in snapshot.Products)
            {
                int score = CalculateProductScore(p, normQuery);
                if (score > 0)
                {
                    scoredProducts.Add((p, score));
                }
            }

            result.TotalMatches = scoredProducts.Count;

            result.Products = scoredProducts
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Product.IsFeatured)
                .ThenByDescending(x => x.Product.Id)
                .Take(maxProducts)
                .Select(x =>
                {
                    var dto = MapToDto(x.Product);
                    dto.MatchScore = x.Score;
                    return dto;
                })
                .ToList();

            // 2. TÌM KIẾM THƯƠNG HIỆU
            result.Brands = snapshot.Brands
                .Where(b => b.NormalizedName.Contains(normQuery))
                .OrderByDescending(b => b.NormalizedName.StartsWith(normQuery))
                .ThenByDescending(b => b.ProductCount)
                .Take(maxBrands)
                .Select(MapBrandToDto)
                .ToList();

            // 3. TÌM KIẾM DANH MỤC
            result.Categories = snapshot.Categories
                .Where(c => c.NormalizedName.Contains(normQuery))
                .OrderByDescending(c => c.NormalizedName.StartsWith(normQuery))
                .ThenByDescending(c => c.ProductCount)
                .Take(maxCategories)
                .Select(MapCategoryToDto)
                .ToList();

            // 4. TỪ KHÓA GỢI Ý / HOT KEYWORDS
            var matchedKeywords = DefaultHotKeywords
                .Where(k => RemoveDiacritics(k).Contains(normQuery))
                .ToList();

            if (matchedKeywords.Count < 5)
            {
                // Thêm các từ khóa hot chưa có vào để đủ 5
                foreach (var kw in DefaultHotKeywords)
                {
                    if (!matchedKeywords.Contains(kw))
                    {
                        matchedKeywords.Add(kw);
                        if (matchedKeywords.Count >= 5) break;
                    }
                }
            }
            result.HotKeywords = matchedKeywords.Take(5).ToList();

            return result;
        }

        private static int CalculateProductScore(CachedProduct p, string query)
        {
            int score = 0;

            // a. Khớp Tên sản phẩm (Trọng số cao nhất)
            if (p.NormalizedName.StartsWith(query))
            {
                score += 100;
            }
            else if (HasWordStartingWith(p.NormalizedName, query))
            {
                score += 80;
            }
            else if (p.NormalizedName.Contains(query))
            {
                score += 50;
            }

            // b. Khớp Thương hiệu
            if (!string.IsNullOrEmpty(p.NormalizedBrand))
            {
                if (p.NormalizedBrand.StartsWith(query)) score += 45;
                else if (p.NormalizedBrand.Contains(query)) score += 30;
            }

            // c. Khớp SKU / Mã hàng
            if (!string.IsNullOrEmpty(p.NormalizedSku) && p.NormalizedSku.Contains(query))
            {
                score += 40;
            }

            // d. Khớp Danh mục
            if (!string.IsNullOrEmpty(p.NormalizedCategory))
            {
                if (p.NormalizedCategory.StartsWith(query)) score += 35;
                else if (p.NormalizedCategory.Contains(query)) score += 20;
            }

            // e. Khớp Thành phần hoạt chất (FullIngredients)
            if (!string.IsNullOrEmpty(p.NormalizedIngredients) && p.NormalizedIngredients.Contains(query))
            {
                score += 25;
            }

            // f. Điểm thưởng ưu tiên
            if (score > 0)
            {
                if (p.IsFeatured) score += 10;
                if (p.SalePrice.HasValue && p.SalePrice.Value < p.Price) score += 5;
            }

            return score;
        }

        private static bool HasWordStartingWith(string text, string prefix)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(prefix)) return false;
            var words = text.Split(new[] { ' ', '-', '/', ',', '.', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
            return words.Any(w => w.StartsWith(prefix, StringComparison.Ordinal));
        }

        public string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var cleaned = text.Trim();

            // Xử lý ký tự đặc thù tiếng Việt đ/Đ
            cleaned = cleaned.Replace('đ', 'd').Replace('Đ', 'D');

            // Chuẩn hóa FormD để bóc tách dấu thanh
            var normalizedString = cleaned.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            var result = stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();

            // Rút gọn khoảng trắng thừa
            return Regex.Replace(result, @"\s+", " ").Trim();
        }

        private async Task<CatalogSnapshot> GetOrLoadCatalogSnapshotAsync(CancellationToken cancellationToken)
        {
            if (_cache.TryGetValue(CatalogCacheKey, out CatalogSnapshot? cached) && cached != null)
            {
                return cached;
            }

            var productsRaw = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => p.IsActive)
                .ToListAsync(cancellationToken);

            var cachedProducts = productsRaw.Select(p =>
            {
                var brandName = p.Brand?.Name ?? p.ManufacturerName ?? "Omni Beauty";
                var catName = p.Category?.Name ?? "Mỹ Phẩm";
                decimal price = p.SellingPrice;
                decimal? salePrice = (p.DiscountPrice.HasValue && p.DiscountPrice.Value > 0 && p.DiscountPrice.Value < p.SellingPrice)
                    ? p.DiscountPrice.Value
                    : null;

                return new CachedProduct
                {
                    Id = p.Id,
                    Name = p.Name,
                    NormalizedName = RemoveDiacritics(p.Name),
                    Slug = p.Slug,
                    Sku = p.Sku,
                    NormalizedSku = RemoveDiacritics(p.Sku),
                    ImageUrl = !string.IsNullOrWhiteSpace(p.ThumbnailUrl)
                        ? p.ThumbnailUrl
                        : "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=160&h=160&fit=crop",
                    Price = price,
                    SalePrice = salePrice,
                    BrandName = brandName,
                    NormalizedBrand = RemoveDiacritics(brandName),
                    CategoryName = catName,
                    NormalizedCategory = RemoveDiacritics(catName),
                    RoutineStep = p.RoutineStep,
                    IsFeatured = p.IsFeatured,
                    FullIngredients = p.FullIngredients,
                    NormalizedIngredients = RemoveDiacritics(p.FullIngredients ?? string.Empty)
                };
            }).ToList();

            var brandsRaw = await _context.Brands
                .AsNoTracking()
                .Where(b => b.IsActive)
                .ToListAsync(cancellationToken);

            var brandProductCounts = cachedProducts
                .GroupBy(p => p.BrandName.ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.Count());

            var cachedBrands = brandsRaw.Select(b => new CachedBrand
            {
                Id = b.Id,
                Name = b.Name,
                NormalizedName = RemoveDiacritics(b.Name),
                Slug = b.Slug,
                LogoUrl = b.LogoUrl,
                ProductCount = brandProductCounts.TryGetValue(b.Name.ToLowerInvariant(), out int cnt) ? cnt : 0
            }).OrderByDescending(b => b.ProductCount).ToList();

            var categoriesRaw = await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .ToListAsync(cancellationToken);

            var catProductCounts = cachedProducts
                .GroupBy(p => p.CategoryName.ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.Count());

            var cachedCategories = categoriesRaw.Select(c => new CachedCategory
            {
                Id = c.Id,
                Name = c.Name,
                NormalizedName = RemoveDiacritics(c.Name),
                Slug = c.Slug,
                ProductCount = catProductCounts.TryGetValue(c.Name.ToLowerInvariant(), out int cnt) ? cnt : 0
            }).OrderByDescending(c => c.ProductCount).ToList();

            var snapshot = new CatalogSnapshot
            {
                Products = cachedProducts,
                Brands = cachedBrands,
                Categories = cachedCategories
            };

            _cache.Set(CatalogCacheKey, snapshot, CacheDuration);
            _logger.LogInformation("Đã khởi tạo/làm mới bộ nhớ đệm Search Suggest Catalog ({Products} SP, {Brands} Brands, {Cats} Categories).",
                cachedProducts.Count, cachedBrands.Count, cachedCategories.Count);

            return snapshot;
        }

        private static SearchProductSuggestionDto MapToDto(CachedProduct p)
        {
            return new SearchProductSuggestionDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                Sku = p.Sku,
                ImageUrl = p.ImageUrl,
                Price = p.Price,
                SalePrice = p.SalePrice,
                BrandName = p.BrandName,
                CategoryName = p.CategoryName,
                RoutineStep = p.RoutineStep,
                IsFeatured = p.IsFeatured
            };
        }

        private static SearchBrandSuggestionDto MapBrandToDto(CachedBrand b)
        {
            return new SearchBrandSuggestionDto
            {
                Id = b.Id,
                Name = b.Name,
                Slug = b.Slug,
                LogoUrl = b.LogoUrl,
                ProductCount = b.ProductCount
            };
        }

        private static SearchCategorySuggestionDto MapCategoryToDto(CachedCategory c)
        {
            return new SearchCategorySuggestionDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                ProductCount = c.ProductCount
            };
        }

        private class CatalogSnapshot
        {
            public List<CachedProduct> Products { get; set; } = new();
            public List<CachedBrand> Brands { get; set; } = new();
            public List<CachedCategory> Categories { get; set; } = new();
        }

        private class CachedProduct
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string NormalizedName { get; set; } = string.Empty;
            public string Slug { get; set; } = string.Empty;
            public string? Sku { get; set; }
            public string NormalizedSku { get; set; } = string.Empty;
            public string? ImageUrl { get; set; }
            public decimal Price { get; set; }
            public decimal? SalePrice { get; set; }
            public string BrandName { get; set; } = string.Empty;
            public string NormalizedBrand { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
            public string NormalizedCategory { get; set; } = string.Empty;
            public string? RoutineStep { get; set; }
            public bool IsFeatured { get; set; }
            public string? FullIngredients { get; set; }
            public string NormalizedIngredients { get; set; } = string.Empty;
        }

        private class CachedBrand
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string NormalizedName { get; set; } = string.Empty;
            public string Slug { get; set; } = string.Empty;
            public string? LogoUrl { get; set; }
            public int ProductCount { get; set; }
        }

        private class CachedCategory
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string NormalizedName { get; set; } = string.Empty;
            public string Slug { get; set; } = string.Empty;
            public int ProductCount { get; set; }
        }
    }
}

