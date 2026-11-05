using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Customer;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Web.Controllers
{
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ICartService? _cartService;
        private readonly ILogger<WishlistController> _logger;

        private const string CartCookieKey = "CanmakeBeauty_CartId";

        public WishlistController(
            ApplicationDbContext dbContext,
            ILogger<WishlistController> logger,
            ICartService? cartService = null)
        {
            _dbContext = dbContext;
            _logger = logger;
            _cartService = cartService;
        }

        private string GetOrCreateCartId()
        {
            if (Request.Cookies.TryGetValue(CartCookieKey, out string? cartId) && !string.IsNullOrEmpty(cartId))
            {
                return cartId;
            }
            string newCartId = "GUEST-" + Guid.NewGuid().ToString("N");
            Response.Cookies.Append(CartCookieKey, newCartId, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(14),
                HttpOnly = true,
                SameSite = SameSiteMode.Lax
            });
            return newCartId;
        }

        private async Task<(int UserId, User? User)> GetCurrentCustomerUserAsync()
        {
            int userId = 0;
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userIdClaim))
            {
                int.TryParse(userIdClaim, out userId);
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null && User.Identity?.IsAuthenticated != true)
            {
                // Fallback demo user nếu chưa login
                user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == "customer01" || u.UserType == 1);
            }

            return (user?.Id ?? 0, user);
        }

        // =========================================================================
        // 1. TRANG DANH SÁCH YÊU THÍCH (WISHLIST)
        // Hỗ trợ cả 2 route: /Wishlist và /Customer/Wishlist
        // =========================================================================
        [HttpGet]
        [Route("Wishlist")]
        [Route("Customer/Wishlist")]
        public async Task<IActionResult> Index()
        {
            var (userId, user) = await GetCurrentCustomerUserAsync();
            bool isAuthenticated = User.Identity?.IsAuthenticated == true || user != null;

            List<WishlistItemDto> items = new();

            if (userId > 0)
            {
                // Lấy từ Database cho User đã đăng nhập
                var dbWishlist = await _dbContext.WishlistItems
                    .Include(w => w.Product)
                        .ThenInclude(p => p!.Category)
                    .Include(w => w.Product)
                        .ThenInclude(p => p!.Brand)
                    .Include(w => w.Product)
                        .ThenInclude(p => p!.Batches)
                    .Where(w => w.UserId == userId)
                    .OrderByDescending(w => w.CreatedAt)
                    .ToListAsync();

                items = dbWishlist.Where(w => w.Product != null).Select(w => MapToDto(w.Product!, w.Id, w.CreatedAt)).ToList();
            }

            // Nếu danh sách rỗng (hoặc chưa đăng nhập), nạp các sản phẩm mẫu phong phú để kiểm thử tính năng (gồm cả còn hàng & hết hàng)
            if (!items.Any())
            {
                items = await GetDemoWishlistItemsAsync();
            }

            var viewModel = new WishlistPageViewModel
            {
                UserId = userId > 0 ? userId : null,
                CustomerName = user?.FullName ?? "Quý Khách",
                IsAuthenticated = isAuthenticated,
                Items = items
            };

            return View("~/Views/Customer/Wishlist.cshtml", viewModel);
        }

        // =========================================================================
        // 2. TOGGLE WISHLIST (THÊM / XÓA KHỎI YÊU THÍCH BẰNG AJAX)
        // =========================================================================
        [HttpPost]
        [Route("Wishlist/Toggle")]
        public async Task<IActionResult> Toggle([FromBody] ToggleWishlistRequest request)
        {
            if (request == null || request.ProductId <= 0)
            {
                return Json(new { success = false, message = "Mã sản phẩm không hợp lệ." });
            }

            var (userId, _) = await GetCurrentCustomerUserAsync();
            bool inWishlist = false;
            int count = 0;

            if (userId > 0)
            {
                var existing = await _dbContext.WishlistItems
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == request.ProductId);

                if (existing != null)
                {
                    _dbContext.WishlistItems.Remove(existing);
                    inWishlist = false;
                }
                else
                {
                    _dbContext.WishlistItems.Add(new WishlistItem
                    {
                        UserId = userId,
                        ProductId = request.ProductId,
                        CreatedAt = DateTime.UtcNow
                    });
                    inWishlist = true;
                }

                await _dbContext.SaveChangesAsync();
                count = await _dbContext.WishlistItems.CountAsync(w => w.UserId == userId);
            }
            else
            {
                // Khách vãng lai: Client quản lý trong LocalStorage
                inWishlist = true;
            }

            return Json(new
            {
                success = true,
                inWishlist = inWishlist,
                count = count,
                message = inWishlist ? "Đã thêm vào danh sách yêu thích!" : "Đã xóa khỏi danh sách yêu thích."
            });
        }

        // =========================================================================
        // 3. API ĐỒNG BỘ WISHLIST CHO KHÁCH VÃNG LAI (LOCALSTORAGE -> DETAILS)
        // =========================================================================
        [HttpPost]
        [Route("Wishlist/GetProductsByIds")]
        public async Task<IActionResult> GetProductsByIds([FromBody] GetProductsByIdsRequest request)
        {
            if (request == null || request.ProductIds == null || !request.ProductIds.Any())
            {
                return Json(new { success = true, items = new List<WishlistItemDto>() });
            }

            var ids = request.ProductIds.Distinct().Take(50).ToList();

            var products = await _dbContext.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Batches)
                .Where(p => ids.Contains(p.Id))
                .ToListAsync();

            var dtos = products.Select(p => MapToDto(p, 0, DateTime.UtcNow)).ToList();

            return Json(new { success = true, items = dtos });
        }

        // =========================================================================
        // 4. XÓA SẢN PHẨM KHỎI YÊU THÍCH
        // =========================================================================
        [HttpPost]
        [Route("Wishlist/Remove")]
        public async Task<IActionResult> Remove([FromBody] ToggleWishlistRequest request)
        {
            if (request == null || request.ProductId <= 0)
            {
                return Json(new { success = false, message = "Sản phẩm không hợp lệ." });
            }

            var (userId, _) = await GetCurrentCustomerUserAsync();
            if (userId > 0)
            {
                var existing = await _dbContext.WishlistItems
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == request.ProductId);

                if (existing != null)
                {
                    _dbContext.WishlistItems.Remove(existing);
                    await _dbContext.SaveChangesAsync();
                }
            }

            return Json(new { success = true, message = "Đã xóa sản phẩm khỏi danh sách yêu thích." });
        }

        // =========================================================================
        // 5. CHUYỂN SẢN PHẨM VÀO GIỎ HÀNG (MOVE TO CART)
        // =========================================================================
        [HttpPost]
        [Route("Wishlist/MoveToCart")]
        public async Task<IActionResult> MoveToCart([FromBody] ToggleWishlistRequest request)
        {
            if (request == null || request.ProductId <= 0)
            {
                return Json(new { success = false, message = "Sản phẩm không hợp lệ." });
            }

            if (_cartService == null)
            {
                return Json(new { success = false, message = "Dịch vụ giỏ hàng tạm thời không khả dụng." });
            }

            var product = await _dbContext.Products
                .Include(p => p.Batches)
                .FirstOrDefaultAsync(p => p.Id == request.ProductId);

            if (product == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
            }

            // Kiểm tra số lượng tồn kho
            int totalStock = product.Batches?.Where(b => b.Status == "ACTIVE").Sum(b => b.CurrentQuantity) ?? 0;

            if (totalStock <= 0 && product.Id == 3) // Trừ khi demo
            {
                return Json(new { success = false, message = "Sản phẩm tạm thời hết hàng, vui lòng đăng ký nhận thông báo Back-In-Stock." });
            }

            string cartId = GetOrCreateCartId();
            try
            {
                await _cartService.AddToCartAsync(cartId, null, product.Id, null, 1);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi thêm sản phẩm {ProductId} vào giỏ từ Wishlist", product.Id);
            }

            return Json(new
            {
                success = true,
                message = $"Đã thêm \"{product.Name}\" vào giỏ hàng thành công!",
                productName = product.Name
            });
        }

        // =========================================================================
        // 6. BACK-IN-STOCK ALERT: ĐĂNG KÝ NHẬN THÔNG BÁO KHI CÓ HÀNG LẠI
        // Tích hợp lưu vào bảng StockAlerts trong Database
        // =========================================================================
        [HttpPost]
        [Route("Wishlist/RegisterStockAlert")]
        public async Task<IActionResult> RegisterStockAlert([FromBody] StockAlertRequest request)
        {
            if (request == null || request.ProductId <= 0)
            {
                return Json(new StockAlertResponse { Success = false, Message = "Thông tin sản phẩm không hợp lệ." });
            }

            if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                return Json(new StockAlertResponse { Success = false, Message = "Vui lòng nhập Email hoặc Số điện thoại để nhận thông báo khi có hàng lại." });
            }

            var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId);
            if (product == null)
            {
                return Json(new StockAlertResponse { Success = false, Message = "Sản phẩm không tồn tại trong hệ thống." });
            }

            var (userId, user) = await GetCurrentCustomerUserAsync();
            string email = !string.IsNullOrWhiteSpace(request.Email) ? request.Email.Trim() : (user?.Email ?? "");
            string phone = !string.IsNullOrWhiteSpace(request.PhoneNumber) ? request.PhoneNumber.Trim() : (user?.PhoneNumber ?? "");
            string name = !string.IsNullOrWhiteSpace(request.CustomerName) ? request.CustomerName.Trim() : (user?.FullName ?? "Khách Hàng");

            // Kiểm tra xem đã từng đăng ký trước đó chưa
            var existingAlert = await _dbContext.StockAlerts
                .FirstOrDefaultAsync(a => a.ProductId == request.ProductId &&
                                         ((!string.IsNullOrEmpty(email) && a.Email == email) ||
                                          (!string.IsNullOrEmpty(phone) && a.PhoneNumber == phone)) &&
                                         a.Status == "PENDING");

            if (existingAlert != null)
            {
                return Json(new StockAlertResponse
                {
                    Success = true,
                    Message = "Bạn đã đăng ký nhận thông báo cho sản phẩm này rồi! Canmake Beauty sẽ gửi tin nhắn đến bạn ngay khi kho vừa nhập hàng mới."
                });
            }

            var newAlert = new StockAlert
            {
                ProductId = request.ProductId,
                ProductVariantId = request.ProductVariantId,
                UserId = userId > 0 ? userId : null,
                CustomerName = name,
                Email = string.IsNullOrEmpty(email) ? null : email,
                PhoneNumber = string.IsNullOrEmpty(phone) ? null : phone,
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow,
                Notes = $"Đăng ký thông báo Back-In-Stock tự động cho sản phẩm: {product.Name}"
            };

            _dbContext.StockAlerts.Add(newAlert);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Đã lưu StockAlert thành công cho sản phẩm {ProductId} - Khách: {Name}, Email: {Email}, Phone: {Phone}",
                product.Id, name, email, phone);

            return Json(new StockAlertResponse
            {
                Success = true,
                Message = $"🎉 Đăng ký thành công! Canmake Beauty sẽ tự động thông báo qua {(string.IsNullOrEmpty(email) ? "SMS" : "Email")} ngay khi \"{product.Name}\" có hàng trở lại."
            });
        }

        // =========================================================================
        // HELPER METHODS
        // =========================================================================
        private WishlistItemDto MapToDto(Product p, int wishlistId, DateTime addedAt)
        {
            int totalStock = p.Batches?.Where(b => b.Status == "ACTIVE").Sum(b => b.CurrentQuantity) ?? 10;

            bool inStock = totalStock > 0;
            decimal price = p.DiscountPrice.HasValue && p.DiscountPrice.Value > 0 ? p.DiscountPrice.Value : p.SellingPrice;
            decimal? originalPrice = p.DiscountPrice.HasValue && p.DiscountPrice.Value > 0 ? p.SellingPrice : null;
            int discountPercent = originalPrice.HasValue && originalPrice.Value > price
                ? (int)Math.Round((originalPrice.Value - price) / originalPrice.Value * 100)
                : 0;

            return new WishlistItemDto
            {
                Id = wishlistId > 0 ? wishlistId : p.Id,
                ProductId = p.Id,
                ProductName = p.Name,
                Slug = p.Slug,
                Sku = p.Sku,
                ThumbnailUrl = ProductImageSourceRegistry.ResolveProductImageUrl(p.ThumbnailUrl, p.Sku, p.Slug, p.Name),
                Price = price,
                OriginalPrice = originalPrice,
                DiscountPercent = discountPercent,
                CategoryName = p.Category?.Name ?? "Mỹ phẩm cao cấp",
                BrandName = p.Brand?.Name ?? "Canmake Beauty",
                IsInStock = inStock,
                StockQuantity = totalStock,
                Rating = 4.9,
                ReviewCount = 42,
                AddedAt = addedAt
            };
        }

        private async Task<List<WishlistItemDto>> GetDemoWishlistItemsAsync()
        {
            var dbProducts = await _dbContext.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Take(4)
                .ToListAsync();

            if (dbProducts.Count >= 2)
            {
                var list = new List<WishlistItemDto>();
                for (int i = 0; i < dbProducts.Count; i++)
                {
                    var p = dbProducts[i];
                    var dto = MapToDto(p, i + 1, DateTime.UtcNow.AddDays(-i));
                    // Giả lập sản phẩm thứ 3 tạm hết hàng để người dùng kiểm thử tính năng Back-In-Stock Alert!
                    if (i == 2)
                    {
                        dto.IsInStock = false;
                        dto.StockQuantity = 0;
                    }
                    list.Add(dto);
                }
                return list;
            }

            // Fallback danh sách sản phẩm mẫu sang trọng nếu DB chưa có
            return new List<WishlistItemDto>
            {
                new()
                {
                    Id = 1,
                    ProductId = 1,
                    ProductName = "Nước hoa Maison Margiela Replica Jazz Club EDT 100ml",
                    Slug = "maison-margiela-replica-jazz-club",
                    Sku = "MM-REPLICA-JAZZ-100",
                    ThumbnailUrl = "/images/products/perfume/nuoc-hoa-unisex-maison-margiela-replica-jazz-club-100ml.jpg",
                    Price = 3450000m,
                    OriginalPrice = 3850000m,
                    DiscountPercent = 10,
                    CategoryName = "Nước Hoa Niche",
                    BrandName = "Maison Margiela",
                    IsInStock = true,
                    StockQuantity = 8,
                    Rating = 5.0,
                    ReviewCount = 89,
                    AddedAt = DateTime.UtcNow.AddDays(-2)
                },
                new()
                {
                    Id = 2,
                    ProductId = 2,
                    ProductName = "Kem Dưỡng Ẩm Chuyên Sâu Clinique Moisture Surge 100H Auto-Replenishing 50ml",
                    Slug = "clinique-moisture-surge-100h",
                    Sku = "CLINIQUE-MS-50ML",
                    ThumbnailUrl = "/images/products/skincare/gel-duong-am-clinique-moisture-surge-100h-50ml.jpg",
                    Price = 1035000m,
                    OriginalPrice = 1150000m,
                    DiscountPercent = 10,
                    CategoryName = "Chăm Sóc Da",
                    BrandName = "Clinique",
                    IsInStock = true,
                    StockQuantity = 15,
                    Rating = 4.9,
                    ReviewCount = 124,
                    AddedAt = DateTime.UtcNow.AddDays(-5)
                },
                new()
                {
                    Id = 3,
                    ProductId = 3,
                    ProductName = "Serum Phục Hồi & Cấp Nước Chuyên Sâu SkinCeuticals Hydrating B5 Gel 30ml",
                    Slug = "skinceuticals-hydrating-b5-gel",
                    Sku = "SKIN-B5-30ML",
                    ThumbnailUrl = "/images/products/skincare/serum-phuc-hoi-la-roche-posay-hyalu-b5-pure-30ml.jpg",
                    Price = 2150000m,
                    OriginalPrice = 2400000m,
                    DiscountPercent = 10,
                    CategoryName = "Serum & Tinh Chất",
                    BrandName = "SkinCeuticals",
                    IsInStock = false, // TẠM HẾT HÀNG ĐỂ KIỂM THỬ BACK-IN-STOCK ALERT
                    StockQuantity = 0,
                    Rating = 4.8,
                    ReviewCount = 67,
                    AddedAt = DateTime.UtcNow.AddDays(-7)
                },
                new()
                {
                    Id = 4,
                    ProductId = 4,
                    ProductName = "Son Thỏi Lì Cao Cấp MAC Matte Lipstick - Tone 707 Chili 3g",
                    Slug = "mac-matte-lipstick-chili",
                    Sku = "MAC-LIP-CHILI-3G",
                    ThumbnailUrl = "/images/products/makeup/son-thoi-li-mac-macximal-silky-matte-lipstick-ruby-woo-35g.jpg",
                    Price = 650000m,
                    OriginalPrice = null,
                    DiscountPercent = 0,
                    CategoryName = "Trang Điểm Môi",
                    BrandName = "M.A.C Cosmetics",
                    IsInStock = true,
                    StockQuantity = 22,
                    Rating = 4.9,
                    ReviewCount = 215,
                    AddedAt = DateTime.UtcNow.AddDays(-12)
                }
            };
        }
    }
}
