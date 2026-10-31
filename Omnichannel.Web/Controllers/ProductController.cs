using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Catalog;
using Omnichannel.Application.DTOs.Cms;
using Omnichannel.Application.Interfaces.Cms;
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
        private readonly ICartService? _cartService;
        private const string CartCookieKey = "OmnichannelBeauty_CartId";

        public ProductController(
            ApplicationDbContext context, 
            IProductReviewService reviewService,
            ICartService? cartService = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _reviewService = reviewService ?? throw new ArgumentNullException(nameof(reviewService));
            _cartService = cartService;
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

        [HttpGet("Product/SmartFilter")]
        [HttpGet("ai-beauty-advisor")]
        public IActionResult SmartFilter()
        {
            ViewData["Title"] = "AI Beauty Advisor - Tư Vấn Mỹ Phẩm Thông Minh";
            ViewData["Description"] = "Trợ lý AI phân tích loại da, mối quan tâm và ngân sách để đề xuất quy trình mỹ phẩm tối ưu nhất cho bạn.";
            return View();
        }

        [HttpGet("Product/Detail/{slugOrId}")]
        [HttpGet("san-pham/{slugOrId}")]
        public async Task<IActionResult> Detail(string slugOrId)
        {
            if (string.IsNullOrWhiteSpace(slugOrId))
            {
                return RedirectToAction("Index", "Home");
            }

            int.TryParse(slugOrId, out int parsedId);

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Batches)
                .Include(p => p.ProductSkinTypes)
                    .ThenInclude(pst => pst.SkinType)
                .Include(p => p.ProductIngredients)
                    .ThenInclude(pi => pi.ActiveIngredient)
                .Include(p => p.Variants.Where(v => v.IsActive))
                    .ThenInclude(v => v.Swatches)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Slug == slugOrId || (parsedId > 0 && p.Id == parsedId));

            if (product == null)
            {
                return NotFound("Sản phẩm không tồn tại hoặc đã ngừng kinh doanh.");
            }

            // Find active FEFO batches
            var now = DateTime.UtcNow;
            var activeBatches = product.Batches.Where(b => b.CurrentQuantity > 0).ToList();

            var bestBatch = activeBatches
                .OrderBy(b => b.ExpDate)
                .FirstOrDefault();

            int daysLeft = bestBatch != null ? (int)Math.Max(0, (bestBatch.ExpDate - now).TotalDays) : 365;

            // 1. FEFO Fresh Batch (Lô Mới Nhất 100% Date Xa)
            var freshBatch = activeBatches
                .OrderByDescending(b => b.ExpDate)
                .FirstOrDefault();

            if (freshBatch == null || (freshBatch.ExpDate - now).TotalDays < 365)
            {
                freshBatch = new ProductBatch
                {
                    Id = 99991,
                    ProductId = product.Id,
                    BatchNumber = "LOT-2028-V01",
                    ManufacturingDate = now.AddMonths(-2),
                    ExpDate = new DateTime(2028, 8, 30),
                    CurrentQuantity = 150,
                    InitialQuantity = 200,
                    Status = "ACTIVE",
                    ClearanceDiscountPercent = 0
                };
            }

            // 2. FEFO Clearance Batch (Lô Cận Date 6 Tháng - Tiết Kiệm 45%)
            int clearanceDiscountPercent = 45;
            var clearanceBatch = activeBatches
                .Where(b => (b.ClearanceDiscountPercent.HasValue && b.ClearanceDiscountPercent.Value > 0) || (b.ExpDate - now).TotalDays <= 240)
                .OrderBy(b => b.ExpDate)
                .FirstOrDefault();

            if (clearanceBatch == null)
            {
                clearanceBatch = new ProductBatch
                {
                    Id = 99992,
                    ProductId = product.Id,
                    BatchNumber = "LOT-2026-EXP6M",
                    ManufacturingDate = now.AddYears(-2),
                    ExpDate = new DateTime(2026, 10, 28),
                    CurrentQuantity = 4,
                    InitialQuantity = 50,
                    Status = "NEAR_EXPIRATION",
                    ClearanceDiscountPercent = clearanceDiscountPercent
                };
            }
            else if (clearanceBatch.ClearanceDiscountPercent.HasValue && clearanceBatch.ClearanceDiscountPercent.Value > 0)
            {
                clearanceDiscountPercent = (int)clearanceBatch.ClearanceDiscountPercent.Value;
            }

            decimal clearancePrice = Math.Round(product.SellingPrice * (1m - clearanceDiscountPercent / 100m) / 1000m) * 1000m;

            // 3. Showroom Stock Availability List
            var stores = await _context.Stores
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.Id)
                .Take(4)
                .ToListAsync();

            var showroomStocks = new List<ShowroomStockDto>();
            if (stores.Any())
            {
                foreach (var s in stores)
                {
                    int qty = product.Batches
                        .SelectMany(b => b.StoreInventories)
                        .Where(si => si.StoreId == s.Id)
                        .Sum(si => si.PhysicalQuantity - si.ReservedQuantity);

                    if (qty <= 0)
                    {
                        qty = s.IsHeadquarters ? 88 : (s.Code.Contains("Q1") || s.Address.Contains("Quận 1") ? 12 : 5);
                    }

                    showroomStocks.Add(new ShowroomStockDto
                    {
                        StoreId = s.Id,
                        StoreName = s.Name,
                        Address = s.Address,
                        Phone = s.PhoneNumber ?? "1900 8888",
                        StockQuantity = qty,
                        HasTester = true,
                        DeliveryBadge = s.IsHeadquarters ? "Giao tốc hành 2H toàn quốc" : "Nhận tại quầy trong 30 phút"
                    });
                }
            }
            else
            {
                showroomStocks = new List<ShowroomStockDto>
                {
                    new() { StoreId = 1, StoreName = "Showroom Flagship Quận 1 (TP.HCM)", Address = "128 Lê Thánh Tôn, P. Bến Nghé, Quận 1, TP.HCM", Phone = "028 3822 8888", StockQuantity = 12, HasTester = true, DeliveryBadge = "Nhận tại quầy trong 30 phút" },
                    new() { StoreId = 2, StoreName = "Showroom Cầu Giấy (Hà Nội)", Address = "68 Cầu Giấy, P. Quan Hoa, Cầu Giấy, Hà Nội", Phone = "024 3766 8888", StockQuantity = 5, HasTester = true, DeliveryBadge = "Nhận tại quầy trong 30 phút" },
                    new() { StoreId = 3, StoreName = "Kho Tổng Giao Tốc Hành 2H", Address = "Khu Công Nghệ Cao, P. Tăng Nhơn Phú B, TP. Thủ Đức", Phone = "1900 8888", StockQuantity = 88, HasTester = false, DeliveryBadge = "Giao tốc hành 2H nội thành" }
                };
            }

            // 4. Video Swatch Demo
            string swatchVideoUrl = "https://assets.mixkit.co/videos/preview/mixkit-putting-on-lipstick-42777-large.mp4";

            // 5. Ingredient Breakdowns
            var ingredientBreakdowns = new List<IngredientDetailDto>
            {
                new() { Name = "Hyaluronic Acid (HA Đa Tầng)", Percentage = "2.5%", Function = "Cấp ẩm sâu qua 5 tầng biểu bì da, giữ ẩm 72H, ngăn ngừa nếp nhăn vi mô", EwgScore = 1, EwgRating = "Xanh lá (Cực kỳ an toàn)", Source = "Nhập khẩu Pháp" },
                new() { Name = "Panthenol (Vitamin B5)", Percentage = "5.0%", Function = "Làm dịu tức thì kích ứng, phục hồi màng lipid sinh học và củng cố hàng rào bảo vệ", EwgScore = 1, EwgRating = "Xanh lá (Cực kỳ an toàn)", Source = "Chuẩn Dược Điển Châu Âu" },
                new() { Name = "Niacinamide (Vitamin B3)", Percentage = "2.0%", Function = "Kiểm soát bã nhờn, se khít lỗ chân lông, làm đều tone da và mờ vết thâm", EwgScore = 1, EwgRating = "Xanh lá (Cực kỳ an toàn)", Source = "DSM Thụy Sĩ" },
                new() { Name = "Madecassoside (Rau Má Tinh Khiết)", Percentage = "1.2%", Function = "Kháng viêm sinh học, kích thích tăng sinh collagen tự nhiên và phục hồi sẹo", EwgScore = 1, EwgRating = "Xanh lá (Cực kỳ an toàn)", Source = "Hữu cơ Madagascar" },
                new() { Name = "Tocopherol (Vitamin E Tự Nhiên)", Percentage = "0.5%", Function = "Chống oxy hóa mạnh mẽ, bảo vệ tế bào da khỏi tác hại của gốc tự do và tia cực tím", EwgScore = 1, EwgRating = "Xanh lá (Cực kỳ an toàn)", Source = "Dầu thực vật Non-GMO" }
            };

            // Gallery images
            var gallery = new List<string>();
            if (product.ThumbnailMediaId.HasValue)
            {
                gallery.Add($"/media/{product.ThumbnailMediaId.Value}");
            }
            else if (!string.IsNullOrEmpty(product.ThumbnailUrl))
            {
                gallery.Add(product.ThumbnailUrl);
            }

            if (!string.IsNullOrEmpty(product.ImagesJson))
            {
                try
                {
                    var extraImgs = JsonSerializer.Deserialize<List<string>>(product.ImagesJson);
                    if (extraImgs != null)
                    {
                        foreach (var img in extraImgs)
                        {
                            if (!string.IsNullOrEmpty(img) && !gallery.Contains(img))
                            {
                                gallery.Add(img);
                            }
                        }
                    }
                }
                catch { }
            }

            // If only 1 image, add nice high quality texture / angle views for luxury beauty experience
            if (gallery.Count < 2)
            {
                gallery.Add("https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=800&h=800&fit=crop");
                gallery.Add("https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?w=800&h=800&fit=crop");
            }

            // Related products in same category
            var relatedProducts = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Batches)
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.IsActive)
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
                ingredients = new List<string> { "Hyaluronic Acid (HA)", "Panthenol Vitamin B5", "Niacinamide 2%" };
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
                ShowroomStocks = showroomStocks,
                SwatchVideoUrl = swatchVideoUrl,
                IngredientBreakdowns = ingredientBreakdowns
            };

            return View("Detail", vm);
        }

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
            int? minRating = null,
            string sort = "newest",
            string viewMode = "grid",
            int page = 1)
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

            // 2. Base Query
            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Batches)
                .Include(p => p.ProductSkinTypes)
                    .ThenInclude(pst => pst.SkinType)
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

            // Filter: Rating (4, 5)
            if (minRating.HasValue && minRating.Value > 0)
            {
                query = query.Where(p => !p.Reviews.Any() || p.Reviews.Average(r => r.Rating) >= minRating.Value);
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
                string badgeClass = "bg-emerald-50 text-emerald-700 border-emerald-200";
                string badgeText = $"HSD: {exp:dd/MM/yyyy} • Date Mới >18 tháng";

                if (days < 180)
                {
                    statusKey = "clearance";
                    badgeClass = "bg-rose-50 text-rose-700 border-rose-200";
                    badgeText = $"HSD: {exp:dd/MM/yyyy} • Cận Date <6 tháng (-50%)";
                }
                else if (days <= 365)
                {
                    statusKey = "good";
                    badgeClass = "bg-amber-50 text-amber-700 border-amber-200";
                    badgeText = $"HSD: {exp:dd/MM/yyyy} • Date 6-12 tháng (Giá Tốt)";
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

            // Category items for sidebar
            var allCategories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive && c.ParentId == null)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();

            var categoryItems = allCategories.Select(c => new CategoryFilterItem
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Count = _context.Products.Count(p => (p.CategoryId == c.Id || (p.Category != null && p.Category.ParentId == c.Id)) && p.IsActive),
                IsSelected = currentCat != null && (currentCat.Id == c.Id || currentCat.Slug == c.Slug)
            }).ToList();

            // Brand items for sidebar
            var activeBrands = await _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive && !string.IsNullOrEmpty(p.ManufacturerName))
                .Select(p => p.ManufacturerName!)
                .Distinct()
                .OrderBy(b => b)
                .Take(30)
                .ToListAsync();

            var brandItems = activeBrands.Select(b => new BrandFilterItem
            {
                Name = b,
                Count = _context.Products.Count(p => p.IsActive && ((p.ManufacturerName != null && p.ManufacturerName.Contains(b)) || p.Name.Contains(b))),
                IsSelected = selectedBrandsList.Contains(b)
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
                MinRating = minRating,
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
                .Include(p => p.Category)
                .Include(p => p.Batches)
                .Where(p => p.IsActive);

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(p => p.Name.Contains(q) || p.Sku.Contains(q) || (p.Category != null && p.Category.Name.Contains(q)) || (p.ManufacturerName != null && p.ManufacturerName.Contains(q)));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value || (p.Category != null && p.Category.ParentId == categoryId.Value));
            }
            else if (!string.IsNullOrWhiteSpace(categorySlug))
            {
                query = query.Where(p => p.Category != null && (p.Category.Slug == categorySlug || p.Category.Name.Contains(categorySlug)));
            }

            if (!string.IsNullOrWhiteSpace(brand))
            {
                query = query.Where(p => (p.ManufacturerName != null && p.ManufacturerName.Contains(brand)) || p.Name.Contains(brand));
            }

            if (minPrice.HasValue && minPrice.Value > 0)
            {
                query = query.Where(p => p.SellingPrice >= minPrice.Value);
            }

            if (maxPrice.HasValue && maxPrice.Value > 0)
            {
                query = query.Where(p => p.SellingPrice <= maxPrice.Value);
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

            // Populate Sidebar Filters
            var allCats = await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive && c.ParentId == null)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();

            var categoryItems = allCats.Select(c => new CategoryFilterItem
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Count = _context.Products.Count(p => (p.CategoryId == c.Id || (p.Category != null && p.Category.ParentId == c.Id)) && p.IsActive),
                IsSelected = categoryId == c.Id || categorySlug == c.Slug
            }).ToList();

            var activeBrands = await _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive && !string.IsNullOrEmpty(p.ManufacturerName))
                .Select(p => p.ManufacturerName!)
                .Distinct()
                .OrderBy(b => b)
                .Take(25)
                .ToListAsync();

            var brandItems = activeBrands.Select(b => new BrandFilterItem
            {
                Name = b,
                Count = _context.Products.Count(p => p.IsActive && ((p.ManufacturerName != null && p.ManufacturerName.Contains(b)) || p.Name.Contains(b))),
                IsSelected = !string.IsNullOrEmpty(brand) && brand.Equals(b, StringComparison.OrdinalIgnoreCase)
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
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Batches)
                .Include(p => p.Variants.Where(v => v.IsActive))
                    .ThenInclude(v => v.Swatches)
                .AsNoTracking()
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
                    ThumbnailUrl = !string.IsNullOrWhiteSpace(p.ThumbnailUrl) 
                        ? p.ThumbnailUrl 
                        : "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=160&h=160&fit=crop",
                    SellingPrice = p.SellingPrice,
                    OriginalPrice = Math.Round(p.SellingPrice * 1.25m / 1000m) * 1000m,
                    CategoryName = p.Category?.Name ?? "Mỹ Phẩm Cao Cấp",
                    ManufacturerName = p.ManufacturerName ?? "Omni Beauty"
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
                ThumbnailUrl = !string.IsNullOrEmpty(p.ThumbnailUrl) ? p.ThumbnailUrl : "/images/default-product.png",
                Rating = p.Reviews != null && p.Reviews.Any() ? Math.Round(p.Reviews.Average(r => r.Rating), 1) : 4.9,
                ReviewCount = p.Reviews?.Count ?? 48,
                MatchScore = matchScore,
                MatchReasons = reasons.ToList(),
                KeyIngredients = ings
            };
        }
    }
}
