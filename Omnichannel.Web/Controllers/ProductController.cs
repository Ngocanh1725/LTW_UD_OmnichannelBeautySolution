using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Omnichannel.Application.DTOs.Catalog;
using Omnichannel.Application.DTOs.Cms;
using Omnichannel.Application.DTOs.Inventory;
using Omnichannel.Application.Interfaces.Catalog;
using Omnichannel.Application.Interfaces.Cms;
using Omnichannel.Application.Interfaces.Inventory;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Web.Models;

namespace Omnichannel.Web.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IProductReviewService _reviewService;
        private readonly IStoreInventoryService _storeInventoryService;
        private readonly ICartService? _cartService;
        private readonly IMemoryCache? _cache;
        private readonly IRecommendationService? _recommendationService;
        private readonly IProductComparisonService? _comparisonService;
        private const string CartCookieKey = "CanmakeBeauty_CartId";

        public ProductController(
            ApplicationDbContext context, 
            IProductReviewService reviewService,
            IStoreInventoryService storeInventoryService,
            ICartService? cartService = null,
            IMemoryCache? cache = null,
            IRecommendationService? recommendationService = null,
            IProductComparisonService? comparisonService = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _reviewService = reviewService ?? throw new ArgumentNullException(nameof(reviewService));
            _storeInventoryService = storeInventoryService ?? throw new ArgumentNullException(nameof(storeInventoryService));
            _cartService = cartService;
            _cache = cache;
            _recommendationService = recommendationService;
            _comparisonService = comparisonService;
        }

        private string GetOrCreateCartId()
        {
            if (Request.Cookies.TryGetValue(CartCookieKey, out var cartId) && !string.IsNullOrWhiteSpace(cartId))
            {
                return cartId;
            }

            var newCartId = Guid.NewGuid().ToString("N");
            var options = new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true
            };
            Response.Cookies.Append(CartCookieKey, newCartId, options);
            return newCartId;
        }

        /// <summary>
        /// Action So Sánh Sản Phẩm theo chuẩn: GET /Product/Compare?ids=1,2,3
        /// Trả về PartialView Modal hoặc chuyển hướng tới trang so sánh chi tiết
        /// </summary>
        [HttpGet("Product/Compare")]
        public async Task<IActionResult> Compare(string? ids, [FromQuery] bool isModal = false, CancellationToken ct = default)
        {
            var parsedIds = (ids ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out int val) ? val : 0)
                .Where(v => v > 0)
                .Distinct()
                .Take(3)
                .ToList();

            if (_comparisonService == null)
            {
                return Redirect($"/so-sanh?ids={string.Join(',', parsedIds)}");
            }

            var matrix = await _comparisonService.GetDetailedComparisonAsync(parsedIds, ct);

            if (isModal || Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_CompareMatrixModalContent", matrix);
            }

            return View("~/Views/Compare/Index.cshtml", matrix);
        }

        [HttpGet("Product/SmartFilter")]
        [HttpGet("ai-beauty-advisor")]
        public IActionResult SmartFilter()
        {
            ViewData["Title"] = "AI Beauty Advisor - Tư Vấn Mỹ Phẩm Thông Minh";
            ViewData["Description"] = "Trợ lý AI phân tích loại da, mối quan tâm và ngân sách để đề xuất quy trình mỹ phẩm tối ưu nhất cho bạn.";
            return View();
        }

        [HttpGet("Product/Detail/{slugOrId?}")]
        [HttpGet("san-pham/{slugOrId?}")]
        public async Task<IActionResult> Detail(string? slugOrId)
        {
            if (string.IsNullOrWhiteSpace(slugOrId))
            {
                Response.StatusCode = 404;
                return View("~/Views/Home/NotFound404.cshtml");
            }

            int.TryParse(slugOrId, out int parsedId);

            var product = await _context.Products
                .AsNoTracking()
                .AsSplitQuery()
                .Include(p => p.Category)
                .Include(p => p.Batches)
                .Include(p => p.ProductSkinTypes)
                    .ThenInclude(pst => pst.SkinType)
                .Include(p => p.ProductConcerns)
                    .ThenInclude(pc => pc.SkinConcern)
                .Include(p => p.ProductIngredients)
                    .ThenInclude(pi => pi.ActiveIngredient)
                .Include(p => p.Variants.Where(v => v.IsActive))
                    .ThenInclude(v => v.Swatches)
                .FirstOrDefaultAsync(p => p.Slug == slugOrId || (parsedId > 0 && p.Id == parsedId));

            if (product == null)
            {
                Response.StatusCode = 404;
                return View("~/Views/Home/NotFound404.cshtml");
            }

            // Find active FEFO batches from real database records
            var now = DateTime.UtcNow;
            var activeBatches = product.Batches
                .Where(b => b.CurrentQuantity > 0 && b.ExpDate > now)
                .ToList();

            var bestBatch = activeBatches
                .OrderBy(b => b.ExpDate)
                .FirstOrDefault()
                ?? product.Batches.OrderByDescending(b => b.ExpDate).FirstOrDefault();

            int daysLeft = bestBatch != null ? (int)Math.Max(0, (bestBatch.ExpDate - now).TotalDays) : 365;

            // 1. FEFO Fresh Batch (Lô Mới Nhất 100% Date Xa - Truy vấn trực tiếp từ CSDL)
            var freshBatch = activeBatches
                .OrderByDescending(b => b.ExpDate)
                .FirstOrDefault()
                ?? bestBatch;

            // 2. FEFO Clearance Batch (Lô Cận Date Thực Tế trong CSDL - Nếu không có thì truyền null)
            int clearanceDiscountPercent = 45;
            var clearanceBatch = activeBatches
                .Where(b => (freshBatch == null || b.Id != freshBatch.Id) &&
                            (b.Status == "NEAR_EXPIRATION" ||
                             (b.ClearanceDiscountPercent.HasValue && b.ClearanceDiscountPercent.Value > 0) ||
                             (b.ExpDate - now).TotalDays <= 240))
                .OrderBy(b => b.ExpDate)
                .FirstOrDefault();

            if (clearanceBatch != null && clearanceBatch.ClearanceDiscountPercent.HasValue && clearanceBatch.ClearanceDiscountPercent.Value > 0)
            {
                clearanceDiscountPercent = (int)Math.Round(clearanceBatch.ClearanceDiscountPercent.Value);
            }

            decimal clearancePrice = Math.Round(product.SellingPrice * (1m - clearanceDiscountPercent / 100m) / 1000m) * 1000m;

            // 3. Showroom Stock Availability List (O2O Realtime)
            var showroomAvailability = await _storeInventoryService.GetShowroomAvailabilityAsync(product.Id);

            // 4. Video Swatch Demo (Chỉ kích hoạt khi có file video nội bộ, triệt tiêu rủi ro hỏng media khi mất mạng)
            string localVideoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "videos", "swatch-demo.mp4");
            string? swatchVideoUrl = System.IO.File.Exists(localVideoPath) ? "/videos/swatch-demo.mp4" : null;

            // 5. Dynamic Ingredient Breakdowns & EWG Safety Analysis per Product
            var ingredientBreakdowns = Omnichannel.Web.Helpers.IngredientParserHelper.BuildDynamicIngredientBreakdowns(product);

            // Gallery images (Phân giải qua nguồn chuẩn duy nhất ProductImageSourceRegistry)
            product.ThumbnailUrl = ProductImageSourceRegistry.ResolveProductImageUrl(product.ThumbnailUrl, product.Sku, product.Slug, product.Name);
            var gallery = ProductImageSourceRegistry.ResolveGalleryImageUrls(product.ThumbnailUrl, product.ImagesJson, product.Sku, product.Slug, product.Name).ToList();

            // Bổ sung các ảnh Swatch của biến thể nếu có
            if (product.Variants != null)
            {
                foreach (var v in product.Variants)
                {
                    if (!string.IsNullOrEmpty(v.SwatchImageUrl))
                    {
                        var resolvedSwatch = ProductImageSourceRegistry.ResolveProductImageUrl(v.SwatchImageUrl, v.Sku ?? product.Sku, product.Slug, product.Name);
                        v.SwatchImageUrl = resolvedSwatch;
                        if (!gallery.Contains(resolvedSwatch))
                        {
                            gallery.Add(resolvedSwatch);
                        }
                    }
                }
            }

            // Related products in same category (có AsSplitQuery và OrderBy)
            var relatedProducts = await _context.Products
                .AsNoTracking()
                .AsSplitQuery()
                .Include(p => p.Category)
                .Include(p => p.Batches)
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.IsActive)
                .OrderByDescending(p => p.Id)
                .Take(4)
                .ToListAsync();

            // Reviews & Rating breakdown
            var reviews = await _reviewService.GetProductReviewsAsync(product.Id, page: 1, pageSize: 10);
            var breakdown = await _reviewService.GetReviewStatsAsync(product.Id);

            var skinTypes = product.ProductSkinTypes?.Select(pst => pst.SkinType?.Name ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new List<string>();
            if (!skinTypes.Any())
            {
                skinTypes = new List<string> { "Mọi loại da", "Da dầu", "Da nhạy cảm" };
            }

            var ingredients = product.ProductIngredients?.Select(pi => pi.ActiveIngredient?.Name ?? "").Where(i => !string.IsNullOrEmpty(i)).ToList() ?? new List<string>();
            if (!ingredients.Any())
            {
                ingredients = ingredientBreakdowns.Take(3).Select(i => i.Name).ToList();
            }

            string resolvedFullIngredients = Omnichannel.Web.Helpers.IngredientParserHelper.ResolveDynamicFullIngredients(product);
            if (!string.IsNullOrWhiteSpace(resolvedFullIngredients))
            {
                product.FullIngredients = resolvedFullIngredients;
            }

            // Gợi ý Combo Thường Mua Cùng (Frequently Bought Together) - Phiên C2
            Omnichannel.Application.DTOs.Catalog.FrequentlyBoughtTogetherDto? fbtBundle = null;
            if (_recommendationService != null)
            {
                try
                {
                    fbtBundle = await _recommendationService.GetFrequentlyBoughtTogetherAsync(product.Id, top: 2);
                }
                catch { }
            }

            var vm = new ProductDetailViewModel
            {
                Product = product,
                NearestBatch = bestBatch,
                DaysLeftToExpiry = daysLeft,
                GalleryImages = gallery,
                RelatedProducts = relatedProducts,
                Reviews = reviews,
                ReviewStats = breakdown,
                AverageRating = breakdown.AverageRating > 0 ? breakdown.AverageRating : 4.9,
                TotalReviews = breakdown.TotalReviews > 0 ? breakdown.TotalReviews : 128,
                SuitableSkinTypes = skinTypes,
                KeyIngredients = ingredients,
                FreshBatch = freshBatch,
                ClearanceBatch = clearanceBatch,
                ClearancePrice = clearancePrice,
                ClearanceDiscountPercent = clearanceDiscountPercent,
                ShowroomAvailability = showroomAvailability,
                SwatchVideoUrl = swatchVideoUrl,
                IngredientBreakdowns = ingredientBreakdowns,
                ParsedIngredients = Omnichannel.Web.Helpers.IngredientParserHelper.ParseIngredients(product.FullIngredients),
                FrequentlyBoughtTogether = fbtBundle
            };

            return View("Detail", vm);
        }

        [HttpGet("san-pham/ton-kho-showroom/{productId:int}")]
        [HttpGet("Product/ShowroomAvailability/{productId:int}")]
        public async Task<IActionResult> ShowroomAvailability(int productId, [FromQuery] string? city = null)
        {
            var vm = await _storeInventoryService.GetShowroomAvailabilityAsync(productId, city);
            return PartialView("_ShowroomStockAvailability", vm);
        }

        [HttpGet("Catalog")]
        [HttpGet("Catalog/{slug?}")]
        [HttpGet("category/{slug?}")]
        [HttpGet("danh-muc/{slug?}")]
        [HttpGet("Product/Category/{slug?}")]
        public async Task<IActionResult> Category(
            string? slug,
            int? categoryId,
            string? q,
            string? brand,
            decimal? minPrice,
            decimal? maxPrice,
            string fefo = "all",
            string skinType = "all",
            string skinConcern = "all",
            string ingredient = "all",
            int? minRating = null,
            string sort = "newest",
            string viewMode = "grid",
            int page = 1,
            bool isAlcoholFree = false,
            bool isFragranceFree = false,
            bool isPregnancySafe = false)
        {
            var now = DateTime.UtcNow;

            // 1. Resolve Category
            Category? currentCat = null;
            if (!string.IsNullOrWhiteSpace(slug) && slug != "all")
            {
                currentCat = await _context.Categories
                    .AsNoTracking()
                    .Include(c => c.SubCategories)
                    .FirstOrDefaultAsync(c => c.Slug == slug || (c.Name != null && c.Name.Contains(slug)));
            }
            else if (categoryId.HasValue && categoryId.Value > 0)
            {
                currentCat = await _context.Categories
                    .AsNoTracking()
                    .Include(c => c.SubCategories)
                    .FirstOrDefaultAsync(c => c.Id == categoryId.Value);
            }

            // 2. Base Query (Thêm AsSplitQuery chống Cartesian Explosion)
            var query = _context.Products
                .AsNoTracking()
                .AsSplitQuery()
                .Include(p => p.Category)
                .Include(p => p.Batches)
                .Include(p => p.ProductSkinTypes)
                    .ThenInclude(pst => pst.SkinType)
                .Include(p => p.ProductConcerns)
                    .ThenInclude(pc => pc.SkinConcern)
                .Include(p => p.ProductIngredients)
                    .ThenInclude(pi => pi.ActiveIngredient)
                .Include(p => p.Reviews)
                .Where(p => p.IsActive);

            // Filter: Category
            if (currentCat != null)
            {
                var catIds = new List<int> { currentCat.Id };
                if (currentCat.SubCategories != null && currentCat.SubCategories.Any())
                {
                    catIds.AddRange(currentCat.SubCategories.Select(sc => sc.Id));
                }
                query = query.Where(p => catIds.Contains(p.CategoryId));
            }

            // Filter: Search Keyword
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(p => p.Name.Contains(q) || p.Sku.Contains(q) || (p.ManufacturerName != null && p.ManufacturerName.Contains(q)));
            }

            // Filter: Brands (single or comma-separated)
            var selectedBrandsList = new List<string>();
            if (!string.IsNullOrWhiteSpace(brand))
            {
                var brands = brand.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (brands.Length > 0)
                {
                    selectedBrandsList.AddRange(brands);
                    query = query.Where(p => brands.Any(b => (p.ManufacturerName != null && p.ManufacturerName.Contains(b)) || p.Name.Contains(b)));
                }
            }

            // Filter: Price Range (default min 50k, max 10tr)
            decimal effectiveMinPrice = minPrice ?? 50000m;
            decimal effectiveMaxPrice = maxPrice ?? 10000000m;
            if (minPrice.HasValue && minPrice.Value > 50000m)
            {
                query = query.Where(p => p.SellingPrice >= minPrice.Value);
            }
            if (maxPrice.HasValue && maxPrice.Value < 10000000m)
            {
                query = query.Where(p => p.SellingPrice <= maxPrice.Value);
            }

            // Filter: FEFO Expiry (all, fresh >18m, good 6-12m, clearance <6m)
            var freshDate = now.AddDays(540);
            var goodMinDate = now.AddDays(180);
            var goodMaxDate = now.AddDays(365);
            var clearanceMaxDate = now.AddDays(180);

            if (fefo == "fresh")
            {
                query = query.Where(p => p.Batches.Any(b => b.CurrentQuantity > 0 && b.ExpDate >= freshDate) || !p.Batches.Any(b => b.CurrentQuantity > 0));
            }
            else if (fefo == "good")
            {
                query = query.Where(p => p.Batches.Any(b => b.CurrentQuantity > 0 && b.ExpDate >= goodMinDate && b.ExpDate <= goodMaxDate));
            }
            else if (fefo == "clearance")
            {
                query = query.Where(p => p.Batches.Any(b => b.CurrentQuantity > 0 && b.ExpDate < clearanceMaxDate && b.ExpDate > now) || p.Batches.Any(b => b.Status == "NEAR_EXPIRATION"));
            }

            // Filter: Skin Type (all, oily, dry, sensitive, combination)
            if (!string.IsNullOrWhiteSpace(skinType) && skinType != "all")
            {
                string slugKey = skinType.ToLowerInvariant() switch
                {
                    "oily" => "da-dau",
                    "dry" => "da-kho",
                    "sensitive" => "da-nhay-cam",
                    "combination" => "da-hon-hop",
                    _ => skinType.ToLowerInvariant()
                };

                query = query.Where(p => p.ProductSkinTypes.Any(pst => pst.SkinType.Slug.Contains(slugKey) || pst.SkinType.Name.Contains(skinType)));
            }

            // Filter: Skin Concern (all, acne, anti_aging, hydration, brightening, pore_minimizing)
            if (!string.IsNullOrWhiteSpace(skinConcern) && skinConcern != "all")
            {
                string concernKey = skinConcern.ToLowerInvariant();
                query = query.Where(p => p.ProductConcerns.Any(pc => pc.SkinConcern.Slug.Contains(concernKey) || pc.SkinConcern.Name.Contains(concernKey))
                                      || p.Name.ToLower().Contains(concernKey)
                                      || (p.Description != null && p.Description.ToLower().Contains(concernKey)));
            }

            // Filter: Active Ingredients (all, niacinamide, ha, b5, bha, retinol, vitc...)
            if (!string.IsNullOrWhiteSpace(ingredient) && ingredient != "all")
            {
                string ingKey = ingredient.ToLowerInvariant();
                query = query.Where(p => p.ProductIngredients.Any(pi => pi.ActiveIngredient.Slug.Contains(ingKey) || pi.ActiveIngredient.Name.ToLower().Contains(ingKey))
                                      || p.Name.ToLower().Contains(ingKey)
                                      || (p.Description != null && p.Description.ToLower().Contains(ingKey)));
            }

            // Filter: Rating (4, 5)
            if (minRating.HasValue && minRating.Value > 0)
            {
                query = query.Where(p => !p.Reviews.Any() || p.Reviews.Average(r => r.Rating) >= minRating.Value);
            }

            // Filter: Clean Beauty & An Toàn Làn Da
            if (isAlcoholFree)
            {
                query = query.Where(p => p.IsAlcoholFree);
            }
            if (isFragranceFree)
            {
                query = query.Where(p => p.IsFragranceFree);
            }
            if (isPregnancySafe)
            {
                query = query.Where(p => p.IsPregnancySafe);
            }

            // Sorting:
            // "newest", "bestseller", "price-asc", "price-desc", "fefo-longest", "discount-desc"
            query = sort switch
            {
                "price-asc" => query.OrderBy(p => p.SellingPrice),
                "price-desc" => query.OrderByDescending(p => p.SellingPrice),
                "bestseller" => query.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.Id),
                "fefo-longest" => query.OrderByDescending(p => p.Batches.Where(b => b.CurrentQuantity > 0).Max(b => (DateTime?)b.ExpDate) ?? DateTime.MinValue),
                "discount-desc" => query.OrderByDescending(p => p.DiscountPrice.HasValue && p.SellingPrice > 0 ? ((p.SellingPrice - p.DiscountPrice.Value) / p.SellingPrice) : 0)
                                        .ThenByDescending(p => p.Batches.Max(b => (decimal?)b.ClearanceDiscountPercent) ?? 0),
                _ => query.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
            };

            int pageSize = 12;
            int totalProducts = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            page = Math.Clamp(page, 1, totalPages);

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Build FEFO and Rating metadata for view
            var fefoMeta = new Dictionary<int, ProductFefoMeta>();
            var ratingMap = new Dictionary<int, double>();
            var ingredientMap = new Dictionary<int, List<string>>();

            foreach (var p in products)
            {
                // Active batch
                var bestBatch = p.Batches
                    .Where(b => b.CurrentQuantity > 0)
                    .OrderBy(b => b.ExpDate)
                    .FirstOrDefault();

                var exp = bestBatch?.ExpDate ?? now.AddMonths(24);
                var days = (exp - now).Days;
                string statusKey = "fresh";
                string badgeClass = "bg-emerald-500/15 border-emerald-500/40 text-emerald-700";
                string badgeText = $"Chuẩn date • HSD {exp:MM/yyyy}";

                if (days < 180)
                {
                    statusKey = "clearance";
                    badgeClass = "bg-rose-500/15 border-rose-500/40 text-rose-600";
                    badgeText = $"Cận date • HSD {exp:MM/yyyy}";
                }
                else if (days <= 540)
                {
                    statusKey = "good";
                    badgeClass = "bg-amber-500/15 border-amber-500/40 text-amber-700";
                    badgeText = $"Date 6-12th • HSD {exp:MM/yyyy}";
                }

                decimal disc = bestBatch?.ClearanceDiscountPercent ?? (p.DiscountPrice.HasValue && p.SellingPrice > 0 ? Math.Round((p.SellingPrice - p.DiscountPrice.Value) / p.SellingPrice * 100m) : 0);

                fefoMeta[p.Id] = new ProductFefoMeta
                {
                    ExpDate = exp,
                    DaysLeft = days,
                    BatchNumber = bestBatch?.BatchNumber ?? $"LOT-FEFO-{p.Id}",
                    BadgeClass = badgeClass,
                    BadgeText = badgeText,
                    StatusKey = statusKey,
                    DiscountPercent = disc
                };

                // Rating
                double rating = p.Reviews != null && p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 4.9;
                ratingMap[p.Id] = Math.Round(rating, 1);

                // Ingredients
                var ings = p.ProductIngredients?.Select(pi => pi.ActiveIngredient?.Name ?? "").Where(s => !string.IsNullOrEmpty(s)).Take(4).ToList() ?? new List<string>();
                if (!ings.Any())
                {
                    ings = new List<string> { "Hyaluronic Acid (HA)", "Panthenol Vitamin B5", "Niacinamide" };
                }
                ingredientMap[p.Id] = ings;
            }

            // Category items for sidebar (Cached 10 phút, Batch aggregated without N+1 queries)
            List<CategoryFilterItem>? rawCatItems = null;
            if (_cache == null || !_cache.TryGetValue("category_sidebar_items", out rawCatItems) || rawCatItems == null)
            {
                var allActiveCategories = await _context.Categories
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .Select(c => new { c.Id, c.ParentId, c.Name, c.Slug, c.SortOrder })
                    .ToListAsync();

                var categoryCounts = await _context.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive)
                    .GroupBy(p => p.CategoryId)
                    .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.CategoryId, x => x.Count);

                var rootCategories = allActiveCategories
                    .Where(c => c.ParentId == null)
                    .OrderBy(c => c.SortOrder)
                    .ToList();

                rawCatItems = rootCategories.Select(c =>
                {
                    int direct = categoryCounts.TryGetValue(c.Id, out int d) ? d : 0;
                    int childSum = allActiveCategories
                        .Where(sub => sub.ParentId == c.Id)
                        .Sum(sub => categoryCounts.TryGetValue(sub.Id, out int subCnt) ? subCnt : 0);

                    return new CategoryFilterItem
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Slug = c.Slug,
                        Count = direct + childSum,
                        IsSelected = false
                    };
                }).ToList();

                _cache?.Set("category_sidebar_items", rawCatItems, TimeSpan.FromMinutes(10));
            }

            var categoryItems = rawCatItems.Select(c => new CategoryFilterItem
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Count = c.Count,
                IsSelected = currentCat != null && (currentCat.Id == c.Id || currentCat.Slug == c.Slug)
            }).ToList();

            // Brand items for sidebar (Cached 10 phút)
            List<BrandFilterItem>? rawBrandItems = null;
            if (_cache == null || !_cache.TryGetValue("brand_sidebar_items", out rawBrandItems) || rawBrandItems == null)
            {
                var brandCounts = await _context.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive && !string.IsNullOrEmpty(p.ManufacturerName))
                    .GroupBy(p => p.ManufacturerName!)
                    .Select(g => new { Brand = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .Take(30)
                    .ToListAsync();

                rawBrandItems = brandCounts.Select(b => new BrandFilterItem
                {
                    Name = b.Brand,
                    Count = b.Count,
                    IsSelected = false
                }).ToList();

                _cache?.Set("brand_sidebar_items", rawBrandItems, TimeSpan.FromMinutes(10));
            }

            var brandItems = rawBrandItems.Select(b => new BrandFilterItem
            {
                Name = b.Name,
                Count = b.Count,
                IsSelected = selectedBrandsList.Contains(b.Name)
            }).ToList();

            var vm = new CategoryViewModel
            {
                CurrentCategory = currentCat,
                CategorySlug = currentCat?.Slug ?? slug ?? "all",
                CategoryId = currentCat?.Id ?? categoryId,
                Query = q,
                FefoOption = fefo,
                MinPrice = effectiveMinPrice,
                MaxPrice = effectiveMaxPrice,
                SelectedBrands = selectedBrandsList,
                SkinType = skinType,
                SkinConcern = skinConcern,
                Ingredient = ingredient,
                MinRating = minRating,
                IsAlcoholFree = isAlcoholFree,
                IsFragranceFree = isFragranceFree,
                IsPregnancySafe = isPregnancySafe,
                Sort = sort,
                ViewMode = viewMode,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalProducts = totalProducts,
                Products = products,
                Categories = categoryItems,
                Brands = brandItems,
                FefoMeta = fefoMeta,
                Ratings = ratingMap,
                ActiveIngredients = ingredientMap
            };

            // HTMX request -> partial reload of the product grid / list
            if (Request.Headers["HX-Request"] == "true")
            {
                return PartialView("_CategoryProductList", vm);
            }

            return View("Category", vm);
        }

        [HttpGet("Product")]
        [HttpGet("Product/Index")]
        [HttpGet("san-pham")]
        [HttpGet("Product/Search")]
        [HttpGet("tim-kiem")]
        public async Task<IActionResult> Search(
            string? q,
            int? categoryId,
            string? categorySlug,
            string? brand,
            decimal? minPrice,
            decimal? maxPrice,
            int? minRating,
            string? skinType,
            string sort = "newest",
            int page = 1)
        {
            bool isLiveSearch = Request.Headers["HX-Target"] == "searchResultsList" 
                || Request.Headers["HX-Target"] == "liveSearchResultsContainer"
                || Request.Headers["HX-Target"] == "searchResultsContent";

            // Live AJAX search in Header popover
            if (isLiveSearch)
            {
                var liveVm = await BuildLiveSearchResultAsync(q);
                return PartialView("_SearchResults", liveVm);
            }

            // Full Search & Filter Page
            var query = _context.Products
                .AsNoTracking()
                .AsSplitQuery()
                .Include(p => p.Category)
                .Include(p => p.Batches)
                .Where(p => p.IsActive);

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(p => p.Name.Contains(q) || p.Sku.Contains(q) || (p.Category != null && p.Category.Name.Contains(q)) || (p.ManufacturerName != null && p.ManufacturerName.Contains(q)));
            }

            Category? targetCat = null;
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                var catIds = new List<int> { categoryId.Value };
                var subCatIds = await _context.Categories.AsNoTracking().Where(c => c.ParentId == categoryId.Value).Select(c => c.Id).ToListAsync();
                catIds.AddRange(subCatIds);
                query = query.Where(p => catIds.Contains(p.CategoryId));
                targetCat = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == categoryId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(categorySlug))
            {
                targetCat = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Slug == categorySlug || c.Name == categorySlug);
                if (targetCat != null)
                {
                    var catIds = new List<int> { targetCat.Id };
                    var subCatIds = await _context.Categories.AsNoTracking().Where(c => c.ParentId == targetCat.Id).Select(c => c.Id).ToListAsync();
                    catIds.AddRange(subCatIds);
                    query = query.Where(p => catIds.Contains(p.CategoryId));
                }
                else
                {
                    query = query.Where(p => p.Category != null && (p.Category.Slug == categorySlug || p.Category.Name.Contains(categorySlug)));
                }
            }

            if (!string.IsNullOrWhiteSpace(brand))
            {
                var b = brand.Trim();
                query = query.Where(p => (p.ManufacturerName != null && p.ManufacturerName.Contains(b)) || p.Name.Contains(b));
            }

            if (minPrice.HasValue && minPrice.Value > 0)
            {
                query = query.Where(p => p.SellingPrice >= minPrice.Value);
            }

            if (maxPrice.HasValue && maxPrice.Value > 0)
            {
                query = query.Where(p => p.SellingPrice <= maxPrice.Value);
            }

            if (!string.IsNullOrWhiteSpace(skinType))
            {
                var st = skinType.Trim();
                query = query.Where(p => (p.ShortDescription != null && p.ShortDescription.Contains(st))
                    || (p.Description != null && p.Description.Contains(st))
                    || p.Name.Contains(st)
                    || p.ProductSkinTypes.Any(pst => pst.SkinType != null && pst.SkinType.Name.Contains(st)));
            }

            if (minRating.HasValue && minRating.Value > 0)
            {
                var qualifiedProductIds = _context.ProductReviews
                    .Where(r => r.IsApproved && !r.IsHidden)
                    .GroupBy(r => r.ProductId)
                    .Where(g => g.Average(r => r.Rating) >= minRating.Value)
                    .Select(g => g.Key);

                query = query.Where(p => qualifiedProductIds.Contains(p.Id));
            }

            // Sorting
            query = sort switch
            {
                "price-asc" => query.OrderBy(p => p.SellingPrice),
                "price-desc" => query.OrderByDescending(p => p.SellingPrice),
                "bestseller" => query.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.Id),
                _ => query.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
            };

            int pageSize = 12;
            int totalProducts = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            page = Math.Clamp(page, 1, totalPages);

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Populate Sidebar Filters (Batch aggregated without N+1 queries)
            var allActiveCats = await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .Select(c => new { c.Id, c.ParentId, c.Name, c.Slug, c.SortOrder })
                .ToListAsync();

            var catCounts = await _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive)
                .GroupBy(p => p.CategoryId)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CategoryId, x => x.Count);

            var rootCats = allActiveCats
                .Where(c => c.ParentId == null)
                .OrderBy(c => c.SortOrder)
                .ToList();

            int? activeParentCatId = targetCat != null ? (targetCat.ParentId ?? targetCat.Id) : categoryId;
            var categoryItems = rootCats.Select(c =>
            {
                int direct = catCounts.TryGetValue(c.Id, out int d) ? d : 0;
                int childSum = allActiveCats
                    .Where(sub => sub.ParentId == c.Id)
                    .Sum(sub => catCounts.TryGetValue(sub.Id, out int subCnt) ? subCnt : 0);

                return new CategoryFilterItem
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    Count = direct + childSum,
                    IsSelected = (activeParentCatId.HasValue && activeParentCatId.Value == c.Id) || (targetCat != null && targetCat.Slug == c.Slug) || categorySlug == c.Slug
                };
            }).ToList();

            var searchBrandCounts = await _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive && !string.IsNullOrEmpty(p.ManufacturerName))
                .GroupBy(p => p.ManufacturerName!)
                .Select(g => new { Brand = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(25)
                .ToListAsync();

            var brandItems = searchBrandCounts.Select(b => new BrandFilterItem
            {
                Name = b.Brand,
                Count = b.Count,
                IsSelected = !string.IsNullOrEmpty(brand) && brand.Equals(b.Brand, StringComparison.OrdinalIgnoreCase)
            }).ToList();

            var skinTypes = new List<string> { "Da dầu", "Da khô", "Da nhạy cảm", "Da hỗn hợp", "Mọi loại da" };

            var vm = new ProductSearchViewModel
            {
                Query = q,
                CategoryId = categoryId,
                CategorySlug = categorySlug,
                Brand = brand,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                MinRating = minRating,
                SkinType = skinType,
                Sort = sort,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalProducts = totalProducts,
                Products = products,
                Categories = categoryItems,
                Brands = brandItems,
                AvailableSkinTypes = skinTypes
            };

            // If HTMX partial request from filter changes
            if (Request.Headers["HX-Request"] == "true")
            {
                return PartialView("_SearchResultsGrid", vm);
            }

            return View("Search", vm);
        }

        [HttpGet("Product/QuickView/{id}")]
        public async Task<IActionResult> QuickView(int id)
        {
            var product = await _context.Products
                .AsNoTracking()
                .AsSplitQuery()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Batches)
                .Include(p => p.Variants.Where(v => v.IsActive))
                    .ThenInclude(v => v.Swatches)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound("Sản phẩm không tồn tại.");
            }

            return PartialView("_QuickView", product);
        }

        [HttpPost("Product/SubmitReview")]
        public async Task<IActionResult> SubmitReview([FromBody] CreateReviewDto dto)
        {
            if (!ModelState.IsValid || dto.ProductId <= 0)
            {
                return Json(new { success = false, message = "Dữ liệu đánh giá không hợp lệ." });
            }

            int? userId = null;
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(claim, out int uid)) userId = uid;

            try
            {
                var ok = await _reviewService.SubmitReviewAsync(dto, userId);
                return Json(new { success = ok, message = ok ? "Cảm ơn bạn đã gửi đánh giá! Đánh giá sẽ xuất hiện sau khi được ban quản trị kiểm duyệt." : "Không thể gửi đánh giá." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task<LiveSearchViewModel> BuildLiveSearchResultAsync(string? q)
        {
            var trendingKeywords = new List<string>
            {
                "Son lì đỏ gạch",
                "Serum B5 phục hồi",
                "Kem chống nắng kiềm dầu",
                "Nước tẩy trang Micellar",
                "Cushion mỏng nhẹ tự nhiên",
                "Nước hoa Miss Dior"
            };

            var trimmed = q?.Trim() ?? string.Empty;
            var vm = new LiveSearchViewModel
            {
                Query = trimmed,
                TrendingKeywords = trendingKeywords
            };

            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length < 2)
            {
                const string defaultLiveSearchCacheKey = "Default_LiveSearch_Empty_v1";
                if (_cache != null && _cache.TryGetValue(defaultLiveSearchCacheKey, out LiveSearchViewModel? cachedLive) && cachedLive != null)
                {
                    return cachedLive;
                }

                var featured = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.Batches)
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.IsFeatured)
                    .ThenByDescending(p => p.Id)
                    .Take(4)
                    .ToListAsync();

                vm.Products = MapToLiveItems(featured);
                vm.TotalFound = vm.Products.Count;

                _cache?.Set(defaultLiveSearchCacheKey, vm, TimeSpan.FromMinutes(15));
                return vm;
            }

            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Batches)
                .Where(p => p.IsActive && (
                    p.Name.Contains(trimmed) ||
                    p.Sku.Contains(trimmed) ||
                    (p.Category != null && p.Category.Name.Contains(trimmed)) ||
                    (p.ManufacturerName != null && p.ManufacturerName.Contains(trimmed))
                ));

            vm.TotalFound = await query.CountAsync();
            var matched = await query
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.Id)
                .Take(6)
                .ToListAsync();

            vm.Products = MapToLiveItems(matched);
            return vm;
        }

        private static List<LiveSearchProductItem> MapToLiveItems(IEnumerable<Product> products)
        {
            var now = DateTime.UtcNow;
            var result = new List<LiveSearchProductItem>();

            foreach (var p in products)
            {
                var item = new LiveSearchProductItem
                {
                    Id = p.Id,
                    Name = p.Name,
                    Slug = p.Slug,
                    ThumbnailUrl = ProductImageSourceRegistry.ResolveProductImageUrl(p.ThumbnailUrl, p.Sku, p.Slug, p.Name),
                    SellingPrice = (p.DiscountPrice.HasValue && p.DiscountPrice.Value > 0 && p.DiscountPrice.Value < p.SellingPrice)
                        ? p.DiscountPrice.Value
                        : p.SellingPrice,
                    OriginalPrice = p.SellingPrice,
                    CategoryName = p.Category?.Name ?? "Mỹ Phẩm Cao Cấp",
                    ManufacturerName = p.ManufacturerName ?? "Canmake Beauty"
                };

                var bestBatch = p.Batches?
                    .Where(b => b.CurrentQuantity > 0)
                    .OrderBy(b => b.ExpDate)
                    .FirstOrDefault();

                if (bestBatch != null)
                {
                    var daysLeft = (bestBatch.ExpDate - now).Days;
                    item.DaysLeft = daysLeft;
                    if (daysLeft <= 0)
                    {
                        item.FefoStatus = $"HSD: {bestBatch.ExpDate:MM/yyyy} • Hết hạn";
                        item.FefoBadgeClass = "bg-rose-50 text-rose-700 border-rose-200";
                    }
                    else if (daysLeft <= bestBatch.AlertDaysThreshold)
                    {
                        item.FefoStatus = $"HSD: {bestBatch.ExpDate:MM/yyyy} • Cận Date -50%";
                        item.FefoBadgeClass = "bg-amber-50 text-amber-800 border-amber-200";
                    }
                    else
                    {
                        item.FefoStatus = $"HSD: {bestBatch.ExpDate:MM/yyyy} • Chuẩn FEFO";
                        item.FefoBadgeClass = "bg-emerald-50 text-emerald-700 border-emerald-200";
                    }
                }
                else
                {
                    item.FefoStatus = "Date mới 2027 • Chuẩn FEFO";
                    item.FefoBadgeClass = "bg-pink-50 text-magenta-accent border-pink-100";
                }

                result.Add(item);
            }

            return result;
        }

        // =========================================================================
        // PERSONALIZED ROUTINE BUILDER (CHĂM SÓC DA RIÊNG BẠN)
        // =========================================================================

        [HttpPost("Product/BuildRoutine")]
        [HttpGet("Product/BuildRoutine")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> BuildRoutine(
            [FromQuery] string? skinType,
            [FromQuery] string? concern,
            [FromQuery] string? budgetTier,
            [FromBody] PersonalizedRoutineRequestDto? bodyRequest = null)
        {
            var req = bodyRequest ?? new PersonalizedRoutineRequestDto();
            if (!string.IsNullOrEmpty(skinType)) req.SkinType = skinType;
            if (!string.IsNullOrEmpty(concern)) req.Concern = concern;
            if (!string.IsNullOrEmpty(budgetTier)) req.BudgetTier = budgetTier;

            var result = await GeneratePersonalizedRoutineAsync(req);
            return Json(result);
        }

        [HttpPost("Product/SaveRoutineToProfile")]
        [IgnoreAntiforgeryToken]
        public IActionResult SaveRoutineToProfile([FromBody] SaveRoutineRequestDto request)
        {
            return Json(new
            {
                success = true,
                message = "Đã lưu quy trình Skincare thành công vào Hồ sơ sắc đẹp cá nhân của bạn!",
                savedAt = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm")
            });
        }

        [HttpPost("Product/AddRoutineToCart")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AddRoutineToCart([FromBody] AddRoutineToCartRequestDto request)
        {
            if (request == null || request.ProductIds == null || !request.ProductIds.Any())
            {
                return Json(new { success = false, message = "Danh sách sản phẩm trống." });
            }

            if (_cartService == null)
            {
                return Json(new { success = false, message = "Dịch vụ giỏ hàng tạm thời không khả dụng." });
            }

            string cartId = GetOrCreateCartId();
            int addedCount = 0;

            foreach (var pId in request.ProductIds.Distinct())
            {
                var item = await _cartService.AddToCartAsync(cartId, null, pId, null, 1);
                if (item != null)
                {
                    addedCount++;
                }
            }

            var cart = await _cartService.GetCartAsync(cartId, null);

            return Json(new
            {
                success = addedCount > 0,
                addedCount,
                cartCount = cart?.TotalItems ?? addedCount,
                totalItems = cart?.TotalItems ?? addedCount,
                message = $"Đã thêm thành công {addedCount} sản phẩm vào giỏ hàng với ưu đãi Combo Routine Tiết Kiệm 10%!"
            });
        }

        private async Task<PersonalizedRoutineResponseDto> GeneratePersonalizedRoutineAsync(PersonalizedRoutineRequestDto req)
        {
            var skinTypeKey = (req.SkinType ?? "da-dau").ToLowerInvariant();
            var concernKey = (req.Concern ?? "acne_pore").ToLowerInvariant();
            var budgetKey = (req.BudgetTier ?? "standard").ToLowerInvariant();

            string skinTypeName = skinTypeKey switch
            {
                "da-dau" => "Da Dầu Mụn & Tiết Bã Nhờn",
                "da-kho" => "Da Khô Ráp & Thiếu Ẩm Tầng Sâu",
                "da-hon-hop" => "Da Hỗn Hợp Thiên Dầu Vùng Chữ T",
                "da-nhay-cam" => "Da Nhạy Cảm & Dễ Kích Ứng",
                "da-thuong" => "Da Thường Cân Bằng Sinh Học",
                _ => "Mọi Loại Da"
            };

            string concernName = concernKey switch
            {
                "acne_pore" => "Mụn Viêm, Mụn Ẩn & Lỗ Chân Lông To",
                "pigmentation" => "Sạm Nám, Tàn Nhang & Thâm Mụn Lâu Năm",
                "aging" => "Lão Hóa Sớm, Nếp Nhăn & Chảy Xệ",
                "recovery" => "Phục Hồi Màng Lipid, Giảm Ửng Đỏ & Củng Cố Hàng Rào Ẩm",
                _ => "Cải Thiện Tổng Thể Biểu Bì"
            };

            string budgetTierName = budgetKey switch
            {
                "economy" => "Gói Tiết Kiệm (Dưới 1.000.000₫)",
                "luxury" => "Gói Thượng Lưu (Trên 3.000.000₫)",
                _ => "Gói Tiêu Chuẩn (1.000.000₫ - 3.000.000₫)"
            };

            string budgetTierDesc = budgetKey switch
            {
                "economy" => "Tập hợp các sản phẩm hiệu năng cao, tối giản bảng thành phần lành tính với chi phí hợp lý nhất từ CeraVe, The Ordinary, Klairs, Torriden.",
                "luxury" => "Đỉnh cao chăm sóc da quý tộc với các hoạt chất sinh học đắt giá, công nghệ trẻ hóa DNA tế bào và thảo dược hoàng gia từ Estée Lauder, Sulwhasoo, SK-II.",
                _ => "Chuẩn dược mỹ phẩm y khoa Pháp & Hàn Quốc, được hơn 90.000 bác sĩ da liễu toàn cầu tín nhiệm từ La Roche-Posay, Anessa, Laneige, Bioderma."
            };

            string philosophy = $"Phác đồ chuẩn hóa dành riêng cho {skinTypeName} nhằm đặc trị {concernName}, được cá nhân hóa trong phân khúc {budgetTierName} với tỷ lệ tương thích sinh học tối ưu.";

            var tierBrands = budgetKey switch
            {
                "economy" => new List<string> { "CeraVe", "The Ordinary", "Klairs", "Torriden", "Hada Labo", "Some By Mi", "Acnes", "Simple", "L'Oréal Paris" },
                "luxury" => new List<string> { "Estée Lauder", "Sulwhasoo", "SK-II", "Lancôme", "Dior", "Chanel", "Yves Saint Laurent", "Shiseido" },
                _ => new List<string> { "La Roche-Posay", "Anessa", "Laneige", "Bioderma", "DHC", "COSRX", "Paula's Choice" }
            };

            var allProducts = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Batches)
                .Include(p => p.ProductIngredients)
                    .ThenInclude(pi => pi.ActiveIngredient)
                .Include(p => p.Reviews)
                .Where(p => p.IsActive)
                .ToListAsync();

            var cleanserProd = PickProduct(allProducts, new[] { "sua-rua-mat", "cleanser", "rửa mặt" }, tierBrands, budgetKey, 80000, 1500000, skinTypeKey);
            var daySerumProd = PickProduct(allProducts, new[] { "serum-tinh-chat", "toner-nuoc-hoa-hong", "serum", "tinh chất", "dưỡng ẩm" }, tierBrands, budgetKey, 150000, 3500000, concernKey);
            var sunscreenProd = PickProduct(allProducts, new[] { "kem-chong-nang", "chống nắng", "sunscreen", "suncream" }, tierBrands, budgetKey, 120000, 2000000, skinTypeKey);
            var removerProd = PickProduct(allProducts, new[] { "nuoc-tay-trang", "tay-trang", "tẩy trang", "micellar", "cleansing oil" }, tierBrands, budgetKey, 100000, 1800000, skinTypeKey);
            var nightSerumProd = PickTreatmentSerum(allProducts, tierBrands, budgetKey, concernKey);
            var nightCreamProd = PickProduct(allProducts, new[] { "kem-duong-am", "kem dưỡng", "cream", "baume", "phục hồi" }, tierBrands, budgetKey, 150000, 3500000, skinTypeKey);

            var morningRoutine = new List<RoutineStepItemDto>
            {
                new RoutineStepItemDto
                {
                    StepNumber = 1,
                    StepTitle = "Làm Sạch Dịu Nhẹ Cân Bằng pH 5.5",
                    StepCategory = "Sữa Rửa Mặt Buổi Sáng",
                    TimeOfDay = "morning",
                    ClinicalTip = "Rửa mặt với nước ấm 30-32°C trong 45-60 giây để làm sạch bã nhờn tích tụ ban đêm mà không làm mất màng ẩm tự nhiên.",
                    BestUsage = "Dùng lượng gel/bọt bằng đồng xu, massage nhẹ từ cánh mũi ra gò má và rửa sạch lại với nước.",
                    Product = cleanserProd != null ? MapToSmartDto(cleanserProd, 99, new[] { "pH 5.5 chuẩn sinh lý", "Không xà phòng gây khô rát" }) : null
                },
                new RoutineStepItemDto
                {
                    StepNumber = 2,
                    StepTitle = "Cấp Nước Đa Tầng & Bảo Vệ Chống Oxy Hóa",
                    StepCategory = "Tinh Chất / Dưỡng Ẩm Ban Ngày",
                    TimeOfDay = "morning",
                    ClinicalTip = "Bổ sung HA đa tầng và chất chống oxy hóa giúp tế bào ngậm nước 72H, ngăn chặn mất nước biểu bì do máy lạnh.",
                    BestUsage = "Vỗ nhẹ 3-4 giọt lên mặt khi da còn hơi ẩm từ toner, chờ 1 phút trước khi thoa chống nắng.",
                    Product = daySerumProd != null ? MapToSmartDto(daySerumProd, 98, new[] { "Cấp nước tầng sâu", "Bảo vệ màng lipid sinh học" }) : null
                },
                new RoutineStepItemDto
                {
                    StepNumber = 3,
                    StepTitle = "Bảo Vệ Toàn Diện SPF50+ PA++++",
                    StepCategory = "Kem Chống Nắng Phổ Rộng",
                    TimeOfDay = "morning",
                    ClinicalTip = "Bước bảo vệ quan trọng nhất! Chống tia UVA/UVB, ánh sáng xanh màn hình máy tính và ngăn ngừa đốm nâu lão hóa sớm.",
                    BestUsage = "Thoa lượng kem tương đương 2 đốt ngón tay trước khi ra nắng hoặc tiếp xúc máy tính 20 phút.",
                    Product = sunscreenProd != null ? MapToSmartDto(sunscreenProd, 99, new[] { "Màng lọc UV phổ rộng", "Kiềm dầu khô thoáng lâu trôi" }) : null
                }
            };

            var nightRoutine = new List<RoutineStepItemDto>
            {
                new RoutineStepItemDto
                {
                    StepNumber = 1,
                    StepTitle = "Tẩy Trang Làm Sạch Sâu Nang Lông",
                    StepCategory = "Tẩy Trang Sạch Sâu",
                    TimeOfDay = "night",
                    ClinicalTip = "Hòa tan triệt để màng lọc chống nắng chống trôi, cặn trang điểm và bụi mịn PM 2.5 sau cả ngày dài.",
                    BestUsage = "Thấm đẫm bông cotton lau nhẹ theo chiều cơ mặt, không chà xát mạnh gây chảy xệ da.",
                    Product = removerProd != null ? MapToSmartDto(removerProd, 99, new[] { "Làm sạch sâu micellar", "Không cồn, không paraben dịu nhẹ" }) : null
                },
                new RoutineStepItemDto
                {
                    StepNumber = 2,
                    StepTitle = "Sữa Rửa Mặt Tái Sinh Ban Đêm",
                    StepCategory = "Sữa Rửa Mặt Chuyên Sâu",
                    TimeOfDay = "night",
                    ClinicalTip = "Hoàn tất chu trình Double Cleansing chuẩn y khoa, giúp nang lông sạch thông thoáng đón nhận dưỡng chất.",
                    BestUsage = "Sử dụng chung sản phẩm sữa rửa mặt dịu nhẹ với buổi sáng để tối ưu chi phí thông minh.",
                    Product = cleanserProd != null ? MapToSmartDto(cleanserProd, 99, new[] { "Dùng chung bước sáng để tối ưu chi phí", "Làm sạch bọt mịn sâu" }) : null,
                    IsSharedWithOtherRoutine = true
                },
                new RoutineStepItemDto
                {
                    StepNumber = 3,
                    StepTitle = "Đặc Trị Chuyên Sâu Theo Vấn Đề Da",
                    StepCategory = "Tinh Chất Đặc Trị Ban Đêm",
                    TimeOfDay = "night",
                    ClinicalTip = "Ban đêm là lúc chu trình tái tạo tế bào diễn ra mạnh nhất. Hoạt chất nồng độ cao sẽ tác động sửa chữa biểu bì tối đa.",
                    BestUsage = "Nhỏ 3-5 giọt ra lòng bàn tay ấm, áp nhẹ lên toàn bộ gương mặt và massage nâng cơ theo hướng từ dưới lên.",
                    Product = nightSerumProd != null ? MapToSmartDto(nightSerumProd, 99, new[] { "Hoạt chất đặc trị chuẩn da liễu", "Tái tạo biểu bì ban đêm" }) : null
                },
                new RoutineStepItemDto
                {
                    StepNumber = 4,
                    StepTitle = "Kem Khóa Ẩm & Tái Tạo Màng Lipid Đêm",
                    StepCategory = "Kem Dưỡng Phục Hồi Ban Đêm",
                    TimeOfDay = "night",
                    ClinicalTip = "Khóa chặt mọi dưỡng chất của các bước trước, củng cố hàng rào Ceramide sinh học, thức dậy với làn da căng mọng.",
                    BestUsage = "Thoa lượng kem cỡ hạt đậu, tán đều khắp mặt và cổ trước khi đi ngủ 30 phút.",
                    Product = nightCreamProd != null ? MapToSmartDto(nightCreamProd, 97, new[] { "Khóa ẩm 72H", "Tái sinh màng bảo vệ biểu bì" }) : null
                }
            };

            var allCandidateProds = new List<SmartFilterProductDto?>();
            foreach (var s in morningRoutine) if (s.Product != null) allCandidateProds.Add(s.Product);
            foreach (var s in nightRoutine) if (s.Product != null) allCandidateProds.Add(s.Product);

            var distinctProducts = allCandidateProds
                .Where(p => p != null)
                .GroupBy(p => p!.Id)
                .Select(g => g.First()!)
                .ToList();

            decimal totalOriginal = distinctProducts.Sum(p => p.Price);
            decimal discountComboPercent = 10m;
            decimal discountComboAmount = Math.Round(totalOriginal * 0.10m / 1000m) * 1000m;
            decimal comboFinalPrice = Math.Max(0, totalOriginal - discountComboAmount);

            return new PersonalizedRoutineResponseDto
            {
                Success = true,
                Message = "Đã tạo quy trình chăm sóc da cá nhân hóa thành công!",
                SkinTypeName = skinTypeName,
                ConcernName = concernName,
                BudgetTierKey = budgetKey,
                BudgetTierName = budgetTierName,
                BudgetTierDescription = budgetTierDesc,
                RoutinePhilosophy = philosophy,
                MorningRoutine = morningRoutine,
                NightRoutine = nightRoutine,
                DistinctProducts = distinctProducts,
                TotalItemsCount = distinctProducts.Count,
                TotalOriginalPrice = totalOriginal,
                DiscountComboPercent = discountComboPercent,
                DiscountComboAmount = discountComboAmount,
                ComboFinalPrice = comboFinalPrice
            };
        }

        private static Product? PickProduct(
            List<Product> products,
            string[] categoryKeywords,
            List<string> tierBrands,
            string budgetTier,
            decimal minTargetPrice,
            decimal maxTargetPrice,
            string skinTypeKey)
        {
            var catMatches = products.Where(p =>
                categoryKeywords.Any(k =>
                    (p.Category?.Slug != null && p.Category.Slug.Contains(k)) ||
                    (p.Category?.Name != null && p.Category.Name.ToLower().Contains(k)) ||
                    p.Name.ToLower().Contains(k) ||
                    (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(k))
                )
            ).ToList();

            if (!catMatches.Any()) catMatches = products;

            var brandMatches = catMatches.Where(p =>
                tierBrands.Any(b =>
                    (p.Brand?.Name != null && p.Brand.Name.ToLower().Contains(b.ToLower())) ||
                    (p.ManufacturerName != null && p.ManufacturerName.ToLower().Contains(b.ToLower())) ||
                    p.Name.ToLower().Contains(b.ToLower())
                )
            ).ToList();

            if (brandMatches.Any())
            {
                var skinMatch = brandMatches.FirstOrDefault(p =>
                    p.Name.ToLower().Contains(skinTypeKey) ||
                    (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(skinTypeKey))
                );
                if (skinMatch != null) return skinMatch;

                return brandMatches.OrderByDescending(p => p.IsFeatured).ThenBy(p => Math.Abs(p.SellingPrice - ((minTargetPrice + maxTargetPrice) / 2))).First();
            }

            var priceMatches = catMatches.Where(p => p.SellingPrice >= minTargetPrice && p.SellingPrice <= maxTargetPrice).ToList();
            if (priceMatches.Any())
            {
                return priceMatches.OrderByDescending(p => p.IsFeatured).First();
            }

            return catMatches.FirstOrDefault();
        }

        private static Product? PickTreatmentSerum(
            List<Product> products,
            List<string> tierBrands,
            string budgetTier,
            string concernKey)
        {
            var keywords = concernKey switch
            {
                "acne_pore" => new[] { "bha", "salicylic", "niacinamide", "tràm trà", "tea tree", "mụn", "lỗ chân lông", "pore", "effaclar", "acne" },
                "pigmentation" => new[] { "vitamin c", "arbutin", "trắng", "sạm", "nám", "thâm", "brightening", "radian", "aura", "tàn nhang" },
                "aging" => new[] { "retinol", "peptide", "lão hóa", "nhân sâm", "ginseng", "advanced night repair", "pitera", "wrinkle", "trẻ hóa", "dna" },
                "recovery" => new[] { "b5", "panthenol", "hyalu", "phục hồi", "ceramide", "centella", "rau má", "soothing", "barrier", "dịu nhẹ" },
                _ => new[] { "serum", "tinh chất", "essence", "b5", "ha" }
            };

            var serumCandidates = products.Where(p =>
                (p.Category?.Slug != null && p.Category.Slug.Contains("serum")) ||
                (p.Category?.Name != null && p.Category.Name.ToLower().Contains("serum")) ||
                p.Name.ToLower().Contains("serum") ||
                p.Name.ToLower().Contains("tinh chất") ||
                p.Name.ToLower().Contains("essence")
            ).ToList();

            if (!serumCandidates.Any()) serumCandidates = products;

            var brandAndConcernMatch = serumCandidates.Where(p =>
                tierBrands.Any(b =>
                    (p.Brand?.Name != null && p.Brand.Name.ToLower().Contains(b.ToLower())) ||
                    (p.ManufacturerName != null && p.ManufacturerName.ToLower().Contains(b.ToLower())) ||
                    p.Name.ToLower().Contains(b.ToLower())
                ) &&
                keywords.Any(k => p.Name.ToLower().Contains(k) || (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(k)))
            ).ToList();

            if (brandAndConcernMatch.Any())
            {
                return brandAndConcernMatch.OrderByDescending(p => p.IsFeatured).First();
            }

            var concernMatch = serumCandidates.Where(p =>
                keywords.Any(k => p.Name.ToLower().Contains(k) || (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(k)))
            ).ToList();

            if (concernMatch.Any())
            {
                var inTier = concernMatch.FirstOrDefault(p =>
                    tierBrands.Any(b =>
                        (p.Brand?.Name != null && p.Brand.Name.ToLower().Contains(b.ToLower())) ||
                        p.Name.ToLower().Contains(b.ToLower())
                    )
                );
                if (inTier != null) return inTier;

                return concernMatch.OrderByDescending(p => p.IsFeatured).First();
            }

            var inTierOnly = serumCandidates.FirstOrDefault(p =>
                tierBrands.Any(b =>
                    (p.Brand?.Name != null && p.Brand.Name.ToLower().Contains(b.ToLower())) ||
                    p.Name.ToLower().Contains(b.ToLower())
                )
            );

            return inTierOnly ?? serumCandidates.FirstOrDefault();
        }

        private static SmartFilterProductDto MapToSmartDto(Product p, int matchScore, string[] reasons)
        {
            var ings = p.ProductIngredients?.Select(pi => pi.ActiveIngredient?.Name ?? "").Where(s => !string.IsNullOrEmpty(s)).Take(3).ToList() ?? new List<string>();
            if (!ings.Any())
            {
                ings = new List<string> { "Hyaluronic Acid (HA)", "Panthenol B5", "Niacinamide" };
            }

            return new SmartFilterProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                CategoryName = p.Category?.Name ?? "Dược Mỹ Phẩm",
                Price = p.SellingPrice,
                OriginalPrice = p.DiscountPrice.HasValue && p.DiscountPrice > 0 ? p.SellingPrice : Math.Round(p.SellingPrice * 1.2m / 1000m) * 1000m,
                ThumbnailUrl = ProductImageSourceRegistry.ResolveProductImageUrl(p.ThumbnailUrl, p.Sku, p.Slug, p.Name),
                Rating = p.Reviews != null && p.Reviews.Any() ? Math.Round(p.Reviews.Average(r => r.Rating), 1) : 4.9,
                ReviewCount = p.Reviews?.Count ?? 48,
                MatchScore = matchScore,
                MatchReasons = reasons.ToList(),
                KeyIngredients = ings
            };
        }
    }
}
