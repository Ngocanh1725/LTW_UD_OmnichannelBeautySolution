using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Sales;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Web.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IPromotionEngine _promotionEngine;
        private readonly ApplicationDbContext _context;

        private const string CartCookieKey = "OmnichannelBeauty_CartId";
        private const string GiftWrapCookieKey = "OmnichannelBeauty_GiftWrap";
        private const string GiftMessageCookieKey = "OmnichannelBeauty_GiftMessage";
        private const decimal FreeShippingThreshold = 500000m;
        private const decimal GiftPackagingFee = 50000m;
        private const decimal StandardShippingFee = 30000m;

        private static readonly List<CrossSellItemDto> DefaultCrossSellItems = new()
        {
            new CrossSellItemDto
            {
                ProductId = 9001,
                ProductName = "Bông Tẩy Trang Cao Cấp Silcot Hữu Cơ (100 Miếng)",
                ImageUrl = "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=400&h=400&fit=crop",
                OriginalPrice = 65000,
                SpecialPrice = 29000,
                BadgeText = "Ưu đãi mua kèm +29K",
                CategoryName = "Phụ Kiện Skincare",
                Description = "100% sợi bông tự nhiên dập viền không xơ, siêu êm dịu cho da"
            },
            new CrossSellItemDto
            {
                ProductId = 9002,
                ProductName = "Nước Hoa Mini Bỏ Túi Jo Malone English Pear (5ml)",
                ImageUrl = "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=400&h=400&fit=crop",
                OriginalPrice = 185000,
                SpecialPrice = 29000,
                BadgeText = "Ưu đãi mua kèm +29K",
                CategoryName = "Nước Hoa Cao Cấp",
                Description = "Hương thơm quý phái thanh lịch, giữ mùi 6-8 tiếng tiện mang theo"
            },
            new CrossSellItemDto
            {
                ProductId = 9003,
                ProductName = "Mặt Nạ Dưỡng Da Sinh Học Hyaluronic Acid Bio-Cellulose",
                ImageUrl = "https://images.unsplash.com/photo-1567928815116-f75471158609?w=400&h=400&fit=crop",
                OriginalPrice = 75000,
                SpecialPrice = 29000,
                BadgeText = "Ưu đãi mua kèm +29K",
                CategoryName = "Mặt Nạ Phục Hồi",
                Description = "Cấp ẩm căng bóng đa tầng tức thì, làm dịu da nhạy cảm sau 15 phút"
            }
        };

        public CartController(ICartService cartService, IPromotionEngine promotionEngine, ApplicationDbContext context)
        {
            _cartService = cartService ?? throw new ArgumentNullException(nameof(cartService));
            _promotionEngine = promotionEngine ?? throw new ArgumentNullException(nameof(promotionEngine));
            _context = context ?? throw new ArgumentNullException(nameof(context));
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

        private bool IsGiftWrappedInCookie()
        {
            return Request.Cookies.TryGetValue(GiftWrapCookieKey, out var val) && val == "true";
        }

        private string? GetGiftMessageFromCookie()
        {
            return Request.Cookies.TryGetValue(GiftMessageCookieKey, out var msg) ? msg : null;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            string cartId = GetOrCreateCartId();
            var cart = await _cartService.GetCartAsync(cartId, null);

            // Read Gift Wrapping options from cookie
            bool isGiftWrapped = IsGiftWrappedInCookie();
            string? giftMessage = GetGiftMessageFromCookie();

            var vm = CartViewModel.FromDto(cart, isGiftWrapped, giftMessage);

            // Enrich items with Variant & FEFO Batch details from database
            if (vm.Items.Any())
            {
                var productIds = vm.Items.Select(i => i.ProductId).Distinct().ToList();
                var products = await _context.Products
                    .Include(p => p.Batches)
                    .Include(p => p.Variants)
                    .Where(p => productIds.Contains(p.Id))
                    .AsNoTracking()
                    .ToDictionaryAsync(p => p.Id);

                var now = DateTime.UtcNow;

                foreach (var item in vm.Items)
                {
                    if (products.TryGetValue(item.ProductId, out var prod))
                    {
                        // 1. Biến thể màu sắc / size / dung tích
                        var activeVariant = prod.Variants?.FirstOrDefault(v => v.IsActive);
                        if (activeVariant != null)
                        {
                            var parts = new List<string>();
                            if (!string.IsNullOrWhiteSpace(activeVariant.VariantName)) parts.Add(activeVariant.VariantName);
                            else
                            {
                                if (!string.IsNullOrWhiteSpace(activeVariant.ColorName)) parts.Add(activeVariant.ColorName);
                                if (!string.IsNullOrWhiteSpace(activeVariant.Volume)) parts.Add(activeVariant.Volume);
                            }
                            item.VariantName = parts.Any() ? string.Join(" • ", parts) : activeVariant.Sku;
                        }
                        if (string.IsNullOrEmpty(item.VariantName))
                        {
                            item.VariantName = "Phiên bản tiêu chuẩn";
                        }

                        // 2. Thông tin Lô & Hạn Dùng FEFO
                        var bestBatch = prod.Batches?
                            .Where(b => b.Status == "ACTIVE" || b.CurrentQuantity > 0)
                            .OrderBy(b => b.ExpDate)
                            .FirstOrDefault();

                        if (bestBatch != null)
                        {
                            item.ProductBatchId = bestBatch.Id;
                            item.BatchNumber = bestBatch.BatchNumber;
                            item.ExpDate = bestBatch.ExpDate;
                            int days = (int)Math.Max(0, (bestBatch.ExpDate - now).TotalDays);
                            item.DaysLeftToExpiry = days;
                            item.IsNearExpiration = days <= 180;
                            item.NearestExpDateStr = bestBatch.ExpDate.ToString("MM/yyyy");
                            item.BatchStatusText = $"Lô: {bestBatch.BatchNumber} • HSD: {bestBatch.ExpDate:MM/yyyy} (Còn {days} ngày)";
                        }
                        else
                        {
                            item.BatchNumber = "LOT-2028-V01";
                            item.NearestExpDateStr = "12/2028";
                            item.DaysLeftToExpiry = 780;
                            item.BatchStatusText = "Lô: LOT-2028-V01 • HSD: 12/2028 (Chuẩn Date Xa 100%)";
                        }
                    }
                }
            }

            // Set Cross-sell items
            vm.CrossSellItems = DefaultCrossSellItems;

            return View(vm);
        }

        private static int ExtractProductId(JsonElement body)
        {
            if (body.ValueKind == JsonValueKind.Number && body.TryGetInt32(out var num))
                return num;
            if (body.ValueKind == JsonValueKind.String && int.TryParse(body.GetString(), out var parsedStr))
                return parsedStr;
            if (body.ValueKind == JsonValueKind.Object)
            {
                if (body.TryGetProperty("productId", out var p1) || body.TryGetProperty("ProductId", out p1))
                {
                    if (p1.ValueKind == JsonValueKind.Number && p1.TryGetInt32(out var n)) return n;
                    if (p1.ValueKind == JsonValueKind.String && int.TryParse(p1.GetString(), out var s)) return s;
                }
                if (body.TryGetProperty("id", out var p2) || body.TryGetProperty("Id", out p2))
                {
                    if (p2.ValueKind == JsonValueKind.Number && p2.TryGetInt32(out var n2)) return n2;
                    if (p2.ValueKind == JsonValueKind.String && int.TryParse(p2.GetString(), out var s2)) return s2;
                }
            }
            return 0;
        }

        private static int ExtractInt(JsonElement body, string propName, int defaultValue = 0)
        {
            if (body.ValueKind == JsonValueKind.Object)
            {
                if (body.TryGetProperty(propName, out var p) || body.TryGetProperty(char.ToUpper(propName[0]) + propName[1..], out p))
                {
                    if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var num)) return num;
                    if (p.ValueKind == JsonValueKind.String && int.TryParse(p.GetString(), out var s)) return s;
                }
            }
            return defaultValue;
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AddToCart([FromBody] JsonElement body)
        {
            int pId = ExtractProductId(body);
            if (pId <= 0)
            {
                return Json(new { success = false, message = "Mã sản phẩm không hợp lệ." });
            }

            int qty = ExtractInt(body, "quantity", 1);
            if (qty <= 0) qty = 1;

            int? batchId = null;
            int extractedBatch = ExtractInt(body, "batchId", 0);
            if (extractedBatch > 0) batchId = extractedBatch;

            string cartId = GetOrCreateCartId();
            var c = await _cartService.AddToCartAsync(cartId, null, pId, batchId, qty);
            bool success = c != null;

            if (success)
            {
                var cart = await _cartService.GetCartAsync(cartId, null);
                bool isGift = IsGiftWrappedInCookie();
                decimal subTotal = cart?.SubTotal ?? 0;
                decimal discount = cart?.DiscountAmount ?? 0;
                decimal afterDiscount = Math.Max(0, subTotal - discount);
                decimal remaining = Math.Max(0, FreeShippingThreshold - afterDiscount);
                decimal progress = afterDiscount >= FreeShippingThreshold ? 100 : (FreeShippingThreshold > 0 ? (afterDiscount / FreeShippingThreshold * 100) : 100);
                decimal shippingFee = (afterDiscount >= FreeShippingThreshold || afterDiscount == 0) ? 0 : StandardShippingFee;
                decimal giftFee = isGift ? GiftPackagingFee : 0;
                decimal finalTotal = afterDiscount + shippingFee + giftFee;

                return Json(new
                {
                    success = true,
                    message = "Đã thêm sản phẩm vào giỏ hàng thành công!",
                    cartCount = cart?.TotalItems ?? 0,
                    totalItems = cart?.TotalItems ?? 0,
                    cartTotalFormatted = subTotal.ToString("N0") + " ₫",
                    subTotal = subTotal.ToString("N0") + " ₫",
                    subTotalAmount = subTotal,
                    discountAmount = discount.ToString("N0") + " ₫",
                    shippingFee = shippingFee == 0 ? "Miễn phí" : shippingFee.ToString("N0") + " ₫",
                    giftWrapFee = giftFee > 0 ? giftFee.ToString("N0") + " ₫" : "0 ₫",
                    finalTotal = finalTotal.ToString("N0") + " ₫",
                    freeShippingRemaining = remaining,
                    freeShippingRemainingFormatted = remaining.ToString("N0") + " ₫",
                    freeShippingProgress = Math.Round(progress, 1),
                    isFreeShipping = remaining == 0
                });
            }

            return Json(new { success = false, message = "Không thể thêm vào giỏ hàng." });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UpdateQuantity([FromBody] JsonElement body)
        {
            int pId = ExtractProductId(body);
            int explicitQty = ExtractInt(body, "quantity", -1);
            int delta = ExtractInt(body, "delta", 0);

            string cartId = GetOrCreateCartId();
            var cart = await _cartService.GetCartAsync(cartId, null);
            var item = cart?.Items.FirstOrDefault(i => i.ProductId == pId);
            if (item != null)
            {
                int newQty = explicitQty >= 0 ? explicitQty : item.Quantity + delta;
                if (newQty <= 0)
                {
                    await _cartService.RemoveFromCartAsync(cartId, null, item.Id);
                }
                else
                {
                    await _cartService.UpdateQuantityAsync(cartId, null, item.Id, newQty);
                }
                cart = await _cartService.GetCartAsync(cartId, null);
            }

            var updatedItem = cart?.Items.FirstOrDefault(i => i.ProductId == pId);
            bool isGift = IsGiftWrappedInCookie();
            decimal subTotal = cart?.SubTotal ?? 0;
            decimal discount = cart?.DiscountAmount ?? 0;
            decimal afterDiscount = Math.Max(0, subTotal - discount);
            decimal remaining = Math.Max(0, FreeShippingThreshold - afterDiscount);
            decimal progress = afterDiscount >= FreeShippingThreshold ? 100 : (FreeShippingThreshold > 0 ? (afterDiscount / FreeShippingThreshold * 100) : 100);
            decimal shippingFee = (afterDiscount >= FreeShippingThreshold || afterDiscount == 0) ? 0 : StandardShippingFee;
            decimal giftFee = isGift ? GiftPackagingFee : 0;
            decimal finalTotal = afterDiscount + shippingFee + giftFee;

            return Json(new
            {
                success = true,
                totalItems = cart?.TotalItems ?? 0,
                itemQuantity = updatedItem?.Quantity ?? 0,
                itemLineTotal = (updatedItem?.LineTotal ?? 0).ToString("N0") + " ₫",
                itemTotalFormatted = (updatedItem?.LineTotal ?? 0).ToString("N0") + " ₫",
                subTotal = subTotal.ToString("N0") + " ₫",
                subTotalFormatted = subTotal.ToString("N0") + " ₫",
                discountAmount = discount.ToString("N0") + " ₫",
                shippingFee = shippingFee == 0 ? "Miễn phí" : shippingFee.ToString("N0") + " ₫",
                giftWrapFee = giftFee > 0 ? giftFee.ToString("N0") + " ₫" : "0 ₫",
                finalTotal = finalTotal.ToString("N0") + " ₫",
                freeShippingRemaining = remaining,
                freeShippingRemainingFormatted = remaining.ToString("N0") + " ₫",
                freeShippingProgress = Math.Round(progress, 1),
                isFreeShipping = remaining == 0
            });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RemoveItem([FromBody] JsonElement body)
        {
            int pId = ExtractProductId(body);
            string cartId = GetOrCreateCartId();
            var cart = await _cartService.GetCartAsync(cartId, null);
            var item = cart?.Items.FirstOrDefault(i => i.ProductId == pId);
            if (item != null)
            {
                await _cartService.RemoveFromCartAsync(cartId, null, item.Id);
                cart = await _cartService.GetCartAsync(cartId, null);
            }

            bool isGift = IsGiftWrappedInCookie();
            decimal subTotal = cart?.SubTotal ?? 0;
            decimal discount = cart?.DiscountAmount ?? 0;
            decimal afterDiscount = Math.Max(0, subTotal - discount);
            decimal remaining = Math.Max(0, FreeShippingThreshold - afterDiscount);
            decimal progress = afterDiscount >= FreeShippingThreshold ? 100 : (FreeShippingThreshold > 0 ? (afterDiscount / FreeShippingThreshold * 100) : 100);
            decimal shippingFee = (afterDiscount >= FreeShippingThreshold || afterDiscount == 0) ? 0 : StandardShippingFee;
            decimal giftFee = isGift ? GiftPackagingFee : 0;
            decimal finalTotal = afterDiscount + shippingFee + giftFee;

            return Json(new 
            { 
                success = true,
                message = "Đã xóa sản phẩm khỏi giỏ hàng.",
                totalItems = cart?.TotalItems ?? 0,
                subTotal = subTotal.ToString("N0") + " ₫",
                subTotalFormatted = subTotal.ToString("N0") + " ₫",
                shippingFee = shippingFee == 0 ? "Miễn phí" : shippingFee.ToString("N0") + " ₫",
                giftWrapFee = giftFee > 0 ? giftFee.ToString("N0") + " ₫" : "0 ₫",
                finalTotal = finalTotal.ToString("N0") + " ₫",
                freeShippingRemaining = remaining,
                freeShippingRemainingFormatted = remaining.ToString("N0") + " ₫",
                freeShippingProgress = Math.Round(progress, 1),
                isFreeShipping = remaining == 0
            });
        }

        /// <summary>
        /// 2. DỊCH VỤ HỘP QUÀ TẶNG THƯỢNG LƯU (LUXURY GIFT PACKAGING)
        /// Cập nhật trạng thái đóng gói quà (+50.000₫) và nội dung thiệp viết tay
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UpdateGiftPackaging([FromBody] JsonElement body)
        {
            bool isGiftWrapped = false;
            string giftMessage = "";

            if (body.ValueKind == JsonValueKind.Object)
            {
                if (body.TryGetProperty("isGiftWrapped", out var gProp))
                {
                    isGiftWrapped = gProp.GetBoolean();
                }
                if (body.TryGetProperty("giftMessage", out var mProp))
                {
                    giftMessage = mProp.GetString() ?? "";
                    if (giftMessage.Length > 200) giftMessage = giftMessage[..200];
                }
            }

            // Save to Cookies
            Response.Cookies.Append(GiftWrapCookieKey, isGiftWrapped ? "true" : "false", new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                HttpOnly = false,
                SameSite = SameSiteMode.Lax
            });

            Response.Cookies.Append(GiftMessageCookieKey, giftMessage, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                HttpOnly = false,
                SameSite = SameSiteMode.Lax
            });

            string cartId = GetOrCreateCartId();
            var cart = await _cartService.GetCartAsync(cartId, null);

            decimal subTotal = cart?.SubTotal ?? 0;
            decimal discount = cart?.DiscountAmount ?? 0;
            decimal afterDiscount = Math.Max(0, subTotal - discount);
            decimal shippingFee = (afterDiscount >= FreeShippingThreshold || afterDiscount == 0) ? 0 : StandardShippingFee;
            decimal giftFee = isGiftWrapped ? GiftPackagingFee : 0;
            decimal finalTotal = afterDiscount + shippingFee + giftFee;

            return Json(new
            {
                success = true,
                isGiftWrapped,
                giftMessage,
                giftWrapFee = giftFee,
                giftWrapFeeFormatted = giftFee > 0 ? "+50.000 ₫" : "0 ₫",
                subTotalFormatted = subTotal.ToString("N0") + " ₫",
                shippingFeeFormatted = shippingFee == 0 ? "Miễn phí" : shippingFee.ToString("N0") + " ₫",
                finalTotalFormatted = finalTotal.ToString("N0") + " ₫",
                message = isGiftWrapped 
                    ? "Đã áp dụng Dịch Vụ Đóng Gói Quà Tặng Thượng Lưu (+50.000₫)" 
                    : "Đã bỏ chọn dịch vụ hộp quà tặng."
            });
        }

        /// <summary>
        /// 4. KHỐI GỢI Ý MUA KÈM ƯU ĐÃI (CROSS-SELL CAROUSEL)
        /// Thêm nhanh Bông tẩy trang / Nước hoa mini / Mặt nạ với giá ưu đãi +29.000₫
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AddCrossSell([FromBody] JsonElement body)
        {
            int crossSellId = ExtractInt(body, "crossSellId", 0);
            if (crossSellId <= 0) crossSellId = ExtractProductId(body);

            var crossSellItem = DefaultCrossSellItems.FirstOrDefault(c => c.ProductId == crossSellId);
            if (crossSellItem == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm mua kèm ưu đãi." });
            }

            // Ensure Product exists in Database
            var product = await EnsureCrossSellProductExistsAsync(crossSellItem);

            string cartId = GetOrCreateCartId();
            var cartEntity = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.CartSessionId == cartId);

            if (cartEntity == null)
            {
                cartEntity = new Cart
                {
                    CartSessionId = cartId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cartEntity);
                await _context.SaveChangesAsync();
            }

            var existingItem = cartEntity.CartItems.FirstOrDefault(ci => ci.ProductId == product.Id);
            if (existingItem != null)
            {
                existingItem.Quantity += 1;
                existingItem.UnitPrice = crossSellItem.SpecialPrice; // 29.000₫
            }
            else
            {
                cartEntity.CartItems.Add(new CartItem
                {
                    CartId = cartEntity.Id,
                    ProductId = product.Id,
                    Quantity = 1,
                    UnitPrice = crossSellItem.SpecialPrice, // 29.000₫
                    CreatedAt = DateTime.UtcNow
                });
            }

            cartEntity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var cart = await _cartService.GetCartAsync(cartId, null);
            bool isGift = IsGiftWrappedInCookie();
            decimal subTotal = cart?.SubTotal ?? 0;
            decimal discount = cart?.DiscountAmount ?? 0;
            decimal afterDiscount = Math.Max(0, subTotal - discount);
            decimal remaining = Math.Max(0, FreeShippingThreshold - afterDiscount);
            decimal progress = afterDiscount >= FreeShippingThreshold ? 100 : (FreeShippingThreshold > 0 ? (afterDiscount / FreeShippingThreshold * 100) : 100);
            decimal shippingFee = (afterDiscount >= FreeShippingThreshold || afterDiscount == 0) ? 0 : StandardShippingFee;
            decimal giftFee = isGift ? GiftPackagingFee : 0;
            decimal finalTotal = afterDiscount + shippingFee + giftFee;

            return Json(new
            {
                success = true,
                message = $"Đã thêm {crossSellItem.ProductName} với giá ưu đãi độc quyền 29.000₫!",
                productId = product.Id,
                totalItems = cart?.TotalItems ?? 0,
                subTotal = subTotal.ToString("N0") + " ₫",
                subTotalFormatted = subTotal.ToString("N0") + " ₫",
                shippingFee = shippingFee == 0 ? "Miễn phí" : shippingFee.ToString("N0") + " ₫",
                giftWrapFee = giftFee > 0 ? giftFee.ToString("N0") + " ₫" : "0 ₫",
                finalTotal = finalTotal.ToString("N0") + " ₫",
                freeShippingRemaining = remaining,
                freeShippingRemainingFormatted = remaining.ToString("N0") + " ₫",
                freeShippingProgress = Math.Round(progress, 1),
                isFreeShipping = remaining == 0
            });
        }

        private async Task<Product> EnsureCrossSellProductExistsAsync(CrossSellItemDto item)
        {
            var sku = "SKU-CROSS-" + item.ProductId;
            var prod = await _context.Products
                .Include(p => p.Batches)
                .FirstOrDefaultAsync(p => p.Sku == sku || p.Name == item.ProductName);

            if (prod == null)
            {
                var cat = await _context.Categories.FirstOrDefaultAsync();
                var brand = await _context.Brands.FirstOrDefaultAsync();

                prod = new Product
                {
                    Name = item.ProductName,
                    Slug = "cross-sell-" + item.ProductId,
                    Sku = sku,
                    SellingPrice = item.SpecialPrice,
                    CostPrice = 15000,
                    DiscountPrice = item.OriginalPrice,
                    ThumbnailUrl = item.ImageUrl,
                    CategoryId = cat?.Id ?? 1,
                    BrandId = brand?.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Products.Add(prod);
                await _context.SaveChangesAsync();

                var batch = new ProductBatch
                {
                    ProductId = prod.Id,
                    BatchNumber = "LOT-2028-CR" + item.ProductId,
                    ManufacturingDate = DateTime.UtcNow.AddMonths(-1),
                    ExpDate = DateTime.UtcNow.AddYears(2),
                    CurrentQuantity = 500,
                    InitialQuantity = 500,
                    Status = "ACTIVE"
                };

                _context.ProductBatches.Add(batch);
                await _context.SaveChangesAsync();
            }

            return prod;
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ApplyCoupon([FromBody] JsonElement body)
        {
            string couponCode = "";
            if (body.ValueKind == JsonValueKind.String)
            {
                couponCode = body.GetString() ?? "";
            }
            else if (body.ValueKind == JsonValueKind.Object && body.TryGetProperty("couponCode", out var prop))
            {
                couponCode = prop.GetString() ?? "";
            }

            couponCode = couponCode.Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(couponCode))
            {
                return Json(new { success = false, message = "Vui lòng nhập mã giảm giá." });
            }

            string cartId = GetOrCreateCartId();
            var cart = await _cartService.GetCartAsync(cartId, null);
            if (cart == null || !cart.Items.Any())
            {
                return Json(new { success = false, message = "Giỏ hàng đang trống." });
            }

            try
            {
                var coupon = await _promotionEngine.GetCouponByCodeAsync(couponCode);
                if (coupon == null || !coupon.IsActive)
                {
                    return Json(new { success = false, message = "Mã voucher không tồn tại hoặc đã hết lượt dùng." });
                }

                var promoResult = await _promotionEngine.EvaluateCartAsync(cart.Items, couponCode, cart.UserId);
                if (!promoResult.IsSuccess || (promoResult.CouponDiscount <= 0 && promoResult.TotalDiscount <= 0))
                {
                    return Json(new { success = false, message = promoResult.Message ?? "Giỏ hàng chưa thỏa mãn điều kiện áp dụng mã voucher này." });
                }

                Response.Cookies.Append("OmnichannelBeauty_AppliedCoupon", coupon.CouponCode, new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(1),
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax
                });

                bool isGift = IsGiftWrappedInCookie();
                decimal discount = promoResult.TotalDiscount;
                decimal subTotal = cart.SubTotal;
                decimal afterDiscount = Math.Max(0, subTotal - discount);
                decimal shippingFee = (afterDiscount >= FreeShippingThreshold || afterDiscount == 0) ? 0 : StandardShippingFee;
                decimal giftFee = isGift ? GiftPackagingFee : 0;
                decimal finalTotal = afterDiscount + shippingFee + giftFee;

                return Json(new
                {
                    success = true,
                    message = promoResult.Message ?? $"Áp dụng mã {coupon.CouponCode} thành công! Giảm ngay {discount:N0} ₫.",
                    discountAmount = discount,
                    discountFormatted = discount.ToString("N0") + " ₫",
                    subTotalFormatted = subTotal.ToString("N0") + " ₫",
                    shippingFeeFormatted = shippingFee == 0 ? "Miễn phí" : shippingFee.ToString("N0") + " ₫",
                    giftWrapFeeFormatted = giftFee > 0 ? giftFee.ToString("N0") + " ₫" : "0 ₫",
                    finalTotalFormatted = finalTotal.ToString("N0") + " ₫",
                    memberTierName = promoResult.MemberTierName,
                    memberTierDiscount = promoResult.MemberTierDiscount,
                    fefoDiscount = promoResult.FefoDiscount,
                    giftItemsCount = promoResult.GiftItems.Count
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi áp dụng voucher: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult MiniCart()
        {
            return ViewComponent("MiniCart");
        }

        [HttpGet("Cart/QuickView/{id}")]
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
    }
}
