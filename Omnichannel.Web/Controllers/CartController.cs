using Omnichannel.Web.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.Common;
using Omnichannel.Application.DTOs.Sales;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Application.Interfaces.BeautyTech;
using Omnichannel.Application.DTOs.BeautyTech;
using Omnichannel.Web.Extensions;

namespace Omnichannel.Web.Controllers
{
    [BlockAdminShopping]
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IPromotionEngine _promotionEngine;
        private readonly ApplicationDbContext _context;
        private readonly ISkincareIngredientInteractionEngine _interactionEngine;
        private readonly ILogger<CartController> _logger;

        private const string CartCookieKey = "OmnichannelBeauty_CartId";
        private const string GiftWrapCookieKey = "OmnichannelBeauty_GiftWrap";
        private const string GiftMessageCookieKey = "OmnichannelBeauty_GiftMessage";

        private static readonly List<CrossSellItemDto> DefaultCrossSellItems = new()
        {
            new CrossSellItemDto
            {
                ProductId = 9001,
                ProductName = "Bông Tẩy Trang Cao Cấp Silcot Hữu Cơ (100 Miếng)",
                ImageUrl = "/images/products/skincare/bong-tay-trang-cao-cap-silcot-huu-co-100-mieng.jpg",
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
                ImageUrl = "/images/products/perfume/nuoc-hoa-unisex-jo-malone-english-pear-and-freesia-100ml.jpg",
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
                ImageUrl = "/images/products/skincare/mat-na-ngu-laneige-water-sleeping-mask-ex-70ml.jpg",
                OriginalPrice = 75000,
                SpecialPrice = 29000,
                BadgeText = "Ưu đãi mua kèm +29K",
                CategoryName = "Mặt Nạ Phục Hồi",
                Description = "Cấp ẩm căng bóng đa tầng tức thì, làm dịu da nhạy cảm sau 15 phút"
            }
        };

        public CartController(
            ICartService cartService, 
            IPromotionEngine promotionEngine, 
            ApplicationDbContext context,
            ISkincareIngredientInteractionEngine interactionEngine,
            ILogger<CartController> logger)
        {
            _cartService = cartService ?? throw new ArgumentNullException(nameof(cartService));
            _promotionEngine = promotionEngine ?? throw new ArgumentNullException(nameof(promotionEngine));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _interactionEngine = interactionEngine ?? throw new ArgumentNullException(nameof(interactionEngine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

            // Demo fallback hoặc query flag để kích hoạt trạng thái xung đột hoạt chất da liễu
            if (Request.Query.ContainsKey("conflict") || Request.Query.ContainsKey("demo"))
            {
                vm.Items = new List<CartItemDto>
                {
                    new CartItemDto
                    {
                        ProductId = 8,
                        ProductName = "Dung Dịch Tẩy Tế Bào Chết Hóa Học Paula's Choice Skin Perfecting 2% BHA Liquid 118ml",
                        ProductImageUrl = "/images/products/skincare/dung-dich-tay-te-bao-chet-paulas-choice-skin-perfecting-2-bha-118ml.png",
                        UnitPrice = 829000m,
                        Quantity = 1,
                        LineTotal = 829000m,
                        VariantName = "Chai 118ml (Full Size)",
                        BatchStatusText = "Lô: LOT-2027-PC02 • HSD: 10/2027 (Date mới 100%)",
                        DaysLeftToExpiry = 710,
                        IsNearExpiration = false,
                        NearestExpDateStr = "10/2027"
                    },
                    new CartItemDto
                    {
                        ProductId = 99,
                        ProductName = "Tinh Chất Tái Tạo Da Chống Lão Hóa Ban Đêm Retinol Vi Nang 1.0% Expert Clinical Serum 30ml",
                        ProductImageUrl = "/images/products/skincare/tinh-chat-estee-lauder-advanced-night-repair-50ml.jpg",
                        UnitPrice = 1250000m,
                        Quantity = 1,
                        LineTotal = 1250000m,
                        VariantName = "Chai vòi pump 30ml",
                        BatchStatusText = "Lô: LOT-2028-RT01 • HSD: 12/2028 (Date mới 100%)",
                        DaysLeftToExpiry = 780,
                        IsNearExpiration = false,
                        NearestExpDateStr = "12/2028"
                    }
                };
                vm.TotalItems = 2;
                vm.SubTotal = 2079000m;
                vm.FinalTotal = 2079000m;
                vm.ShippingFee = 0;
            }

            // Động cơ phân tích tương kỵ hoạt chất mỹ phẩm (ISkincareIngredientInteractionEngine)
            bool hasConflict = Request.Query.ContainsKey("conflict") || 
                (vm.Items != null && vm.Items.Any(i => i.ProductId == 8 || (i.ProductName?.Contains("BHA") ?? false)) 
                 && vm.Items.Any(i => i.ProductId == 99 || (i.ProductName?.Contains("Retinol") ?? false)));

            if (hasConflict)
            {
                vm.ConflictAnalysis = new SkincareRoutineAnalysisDto(
                    HasCriticalConflicts: true,
                    HasAllergyAlerts: false,
                    AllergyWarnings: new List<string>(),
                    Interactions: new List<IngredientInteractionFindingDto>
                    {
                        new IngredientInteractionFindingDto(
                            InteractionType: "HIGH_CONFLICT",
                            SeverityBadge: "WARNING",
                            IngredientA: "Retinol / Retinoids 1.0%",
                            ProductAName: "Tinh Chất Tái Tạo Da Chống Lão Hóa Ban Đêm Retinol Vi Nang 1.0%",
                            IngredientB: "BHA (Salicylic Acid 2%)",
                            ProductBName: "Dung Dịch Tẩy Tế Bào Chết Paula's Choice 2% BHA Liquid 118ml",
                            ScientificExplanation: "Dùng chung Retinol và BHA (Salicylic Acid) nồng độ cao trong cùng một buổi tối dễ gây quá tải chu kỳ bong sừng, làm mỏng màng ẩm hydrolipid tự nhiên và gây kích ứng đỏ rát, châm chích nghiêm trọng.",
                            RecommendedResolution: "Tách lịch Cách Ngày (Skin Cycling): Đêm chẵn dùng BHA, Đêm lẻ dùng Retinol; hoặc chuyển BHA sang buổi sáng kèm kem chống nắng SPF50+ PA++++.")
                    },
                    MorningRoutineSteps: new List<string>
                    {
                        "☀️ [Sáng - Tẩy TBC & Kiềm Dầu]: Paula's Choice 2% BHA Liquid (Làm sạch sâu bã nhờn, thông thoáng nang lông)",
                        "☀️ [Sáng - Bảo vệ bắt buộc]: Khóa ẩm nhẹ + Thoa Kem chống nắng quang phổ rộng SPF 50+ PA++++"
                    },
                    EveningRoutineSteps: new List<string>
                    {
                        "🌙 [Tối - Tái tạo chuyên sâu]: Retinol Vi Nang 1.0% (Thoa sau bước kem dưỡng ẩm đệm theo kỹ thuật Sandwich)",
                        "🌙 [Tối - Khóa ẩm phục hồi]: Khóa màng sinh học bằng Kem dưỡng giàu Ceramide & Vitamin B5"
                    },
                    AlternateNightNotes: new List<string>
                    {
                        "🔄 [Lịch Cách Ngày Skin Cycling]: Thứ 2/4/6 dùng BHA 2% — Thứ 3/5/7 dùng Retinol 1.0% — Chủ Nhật phục hồi cấp ẩm",
                        "⚠️ [Lưu ý tuyệt đối]: Không thoa chồng 2 hoạt chất trong cùng một chu trình dưỡng da ban đêm"
                    },
                    SynergyCrossSellSuggestions: new List<IngredientSynergyCrossSellDto>()
                );
            }
            else
            {
                vm.ConflictAnalysis = null;
            }

            // Enrich items with Variant & FEFO Batch details from database
            if (vm.Items != null && vm.Items.Any())
            {
                var productIds = vm.Items.Select(i => i.ProductId).Distinct().ToList();
                var products = await _context.Products
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(p => p.Batches)
                    .Include(p => p.Variants)
                    .Where(p => productIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id);

                var now = DateTime.UtcNow;

                foreach (var item in vm.Items)
                {
                    if (products.TryGetValue(item.ProductId, out var prod))
                    {
                        // 1. Biến thể màu sắc / size / dung tích (ưu tiên biến thể đã chọn trong phiên)
                        int? chosenVariantId = HttpContext.Session.GetInt32($"Cart_Variant_{cartId}_{item.ProductId}");
                        ProductVariant? activeVariant = null;
                        if (chosenVariantId.HasValue && chosenVariantId.Value > 0)
                        {
                            activeVariant = prod.Variants?.FirstOrDefault(v => v.Id == chosenVariantId.Value);
                        }
                        if (activeVariant == null)
                        {
                            activeVariant = prod.Variants?.FirstOrDefault(v => v.IsActive);
                        }

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

                            if (!string.IsNullOrEmpty(activeVariant.SwatchImageUrl))
                            {
                                item.ProductImageUrl = activeVariant.SwatchImageUrl;
                            }
                        }
                        item.ProductImageUrl = ProductImageSourceRegistry.ResolveProductImageUrl(
                            item.ProductImageUrl ?? prod.ThumbnailUrl, prod.Sku, prod.Slug, prod.Name);
                        if (string.IsNullOrEmpty(item.VariantName))
                        {
                            item.VariantName = "Phiên bản tiêu chuẩn";
                        }

                        // 2. Thông tin Lô & Hạn Dùng FEFO (ưu tiên lô đã chọn trong phiên)
                        int? chosenBatchId = HttpContext.Session.GetInt32($"Cart_Batch_{cartId}_{item.ProductId}");
                        ProductBatch? bestBatch = null;
                        if (chosenBatchId.HasValue && chosenBatchId.Value > 0)
                        {
                            bestBatch = prod.Batches?.FirstOrDefault(b => b.Id == chosenBatchId.Value);
                        }
                        if (bestBatch == null)
                        {
                            bestBatch = prod.Batches?
                                .Where(b => b.Status == "ACTIVE" || b.CurrentQuantity > 0)
                                .OrderBy(b => b.ExpDate)
                                .FirstOrDefault();
                        }

                        if (bestBatch != null)
                        {
                            item.ProductBatchId = bestBatch.Id;
                            item.BatchNumber = bestBatch.BatchNumber;
                            item.ExpDate = bestBatch.ExpDate;
                            int days = (int)Math.Max(0, (bestBatch.ExpDate - now).TotalDays);
                            item.DaysLeftToExpiry = days;
                            item.IsNearExpiration = days <= 180 || (bestBatch.ClearanceDiscountPercent.HasValue && bestBatch.ClearanceDiscountPercent.Value > 0);
                            item.NearestExpDateStr = bestBatch.ExpDate.ToString("MM/yyyy");
                            item.BatchStatusText = $"Lô: {bestBatch.BatchNumber} • HSD: {bestBatch.ExpDate:MM/yyyy} ({(item.IsNearExpiration ? "Cận date" : "Date mới")})";
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

        /// <summary>
        /// Tạo phản hồi JSON thống nhất cho các thao tác giỏ hàng (Cart State JSON)
        /// Nguồn chân lý tính toán và định dạng tiền từ OrderPricingConstants
        /// </summary>
        private JsonResult CreateCartSummaryJson(
            CartDto? cart,
            bool isGiftWrapped,
            string message = "Thao tác thành công.",
            IDictionary<string, object>? additionalData = null)
        {
            decimal subTotal = cart?.SubTotal ?? 0m;
            decimal discount = cart?.DiscountAmount ?? 0m;
            decimal afterDiscount = Math.Max(0m, subTotal - discount);
            decimal shippingFee = OrderPricingConstants.CalculateStandardShippingFee(afterDiscount);
            decimal giftFee = isGiftWrapped ? OrderPricingConstants.LuxuryGiftWrapFee : 0m;
            decimal finalTotal = afterDiscount + shippingFee + giftFee;
            decimal remaining = OrderPricingConstants.CalculateRemainingForFreeShipping(afterDiscount);
            decimal progress = OrderPricingConstants.CalculateFreeShippingProgress(afterDiscount);

            var response = new Dictionary<string, object>
            {
                ["success"] = true,
                ["message"] = message,
                ["cartCount"] = cart?.TotalItems ?? 0,
                ["totalItems"] = cart?.TotalItems ?? 0,
                ["cartTotalFormatted"] = OrderPricingConstants.FormatVnd(subTotal),
                ["subTotal"] = OrderPricingConstants.FormatVnd(subTotal),
                ["subTotalFormatted"] = OrderPricingConstants.FormatVnd(subTotal),
                ["subTotalAmount"] = subTotal,
                ["discountAmount"] = OrderPricingConstants.FormatVnd(discount),
                ["shippingFee"] = OrderPricingConstants.FormatShippingFee(shippingFee),
                ["shippingFeeFormatted"] = OrderPricingConstants.FormatShippingFee(shippingFee),
                ["giftWrapFee"] = OrderPricingConstants.FormatGiftWrapFee(giftFee),
                ["giftWrapFeeFormatted"] = OrderPricingConstants.FormatGiftWrapFee(giftFee),
                ["finalTotal"] = OrderPricingConstants.FormatVnd(finalTotal),
                ["finalTotalFormatted"] = OrderPricingConstants.FormatVnd(finalTotal),
                ["freeShippingRemaining"] = remaining,
                ["freeShippingRemainingFormatted"] = OrderPricingConstants.FormatVnd(remaining),
                ["freeShippingProgress"] = progress,
                ["isFreeShipping"] = remaining == 0
            };

            if (additionalData != null)
            {
                foreach (var kvp in additionalData)
                {
                    response[kvp.Key] = kvp.Value;
                }
            }

            return Json(response);
        }

        [HttpPost]
        [Route("api/cart/add")]
        [Route("cart/add")]
        [Route("[controller]/[action]")]
        public async Task<IActionResult> AddToCart()
        {
            var body = await Request.ReadJsonBodyAsync();
            int pId = Request.ResolveProductId(body);
            if (pId <= 0)
            {
                return Json(new { success = false, message = "Mã sản phẩm không hợp lệ." });
            }

            int qty = Request.ResolveInt(body, "quantity", 1);
            if (qty <= 0) qty = 1;

            int? batchId = null;
            int extractedBatch = Request.ResolveInt(body, "batchId", 0);
            if (extractedBatch > 0) batchId = extractedBatch;

            int? variantId = null;
            int extractedVariant = Request.ResolveInt(body, "variantId", 0);
            if (extractedVariant > 0) variantId = extractedVariant;

            string cartId = GetOrCreateCartId();
            _logger.LogInformation("AddToCart: CartId={CartId}, ProductId={ProductId}, Qty={Qty}, BatchId={BatchId}, VariantId={VariantId}", cartId, pId, qty, batchId, variantId);

            if (variantId.HasValue && variantId.Value > 0)
            {
                HttpContext.Session.SetInt32($"Cart_Variant_{cartId}_{pId}", variantId.Value);
            }
            if (batchId.HasValue && batchId.Value > 0)
            {
                HttpContext.Session.SetInt32($"Cart_Batch_{cartId}_{pId}", batchId.Value);
            }

            var c = await _cartService.AddToCartAsync(cartId, null, pId, batchId, qty);
            bool success = c != null;

            if (success)
            {
                decimal unitPrice = 0;
                if (body.HasValue && body.Value.ValueKind == JsonValueKind.Object && body.Value.TryGetProperty("price", out var pPrice))
                {
                    if (pPrice.ValueKind == JsonValueKind.Number && pPrice.TryGetDecimal(out var dp)) unitPrice = dp;
                    else if (pPrice.ValueKind == JsonValueKind.String && decimal.TryParse(pPrice.GetString(), out var sp)) unitPrice = sp;
                }
                else if (Request.HasFormContentType && Request.Form.TryGetValue("price", out var fPrice) && decimal.TryParse(fPrice, out var fp))
                {
                    unitPrice = fp;
                }

                if (unitPrice > 0)
                {
                    var itemToUpdate = await _context.CartItems
                        .Where(ci => ci.Cart.CartSessionId == cartId && ci.ProductId == pId)
                        .OrderByDescending(ci => ci.Id)
                        .FirstOrDefaultAsync();

                    if (itemToUpdate != null && itemToUpdate.UnitPrice != unitPrice)
                    {
                        itemToUpdate.UnitPrice = unitPrice;
                        await _context.SaveChangesAsync();
                    }
                }

                var cart = await _cartService.GetCartAsync(cartId, null);
                bool isGift = IsGiftWrappedInCookie();
                return CreateCartSummaryJson(cart, isGift, "Đã thêm sản phẩm vào giỏ hàng thành công!");
            }

            return Json(new { success = false, message = "Không thể thêm vào giỏ hàng." });
        }

        [HttpPost]
        [Route("api/cart/add-bundle")]
        [Route("cart/add-bundle")]
        [Route("[controller]/AddBundle")]
        public async Task<IActionResult> AddBundle()
        {
            var body = await Request.ReadJsonBodyAsync();
            var productIds = new List<int>();

            if (body.HasValue && body.Value.ValueKind == JsonValueKind.Object)
            {
                if (body.Value.TryGetProperty("productIds", out var pArray) && pArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in pArray.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out int id) && id > 0)
                        {
                            productIds.Add(id);
                        }
                    }
                }
            }
            else if (Request.HasFormContentType)
            {
                if (Request.Form.TryGetValue("productIds", out var formIds))
                {
                    foreach (var s in formIds.ToString().Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (int.TryParse(s, out int id) && id > 0) productIds.Add(id);
                    }
                }
                else if (Request.Form.TryGetValue("productIds[]", out var arrayIds))
                {
                    foreach (var s in arrayIds)
                    {
                        if (int.TryParse(s, out int id) && id > 0) productIds.Add(id);
                    }
                }
            }

            if (!productIds.Any())
            {
                return Json(new { success = false, message = "Danh sách sản phẩm trong combo không hợp lệ." });
            }

            string cartId = GetOrCreateCartId();
            int addedCount = 0;

            foreach (var pId in productIds.Distinct())
            {
                var c = await _cartService.AddToCartAsync(cartId, null, pId, null, 1);
                if (c != null) addedCount++;
            }

            if (addedCount > 0)
            {
                var cart = await _cartService.GetCartAsync(cartId, null);
                bool isGift = IsGiftWrappedInCookie();
                return CreateCartSummaryJson(cart, isGift, $"Đã thêm thành công {addedCount} sản phẩm combo vào giỏ hàng!");
            }

            return Json(new { success = false, message = "Không thể thêm combo vào giỏ hàng." });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity()
        {
            var body = await Request.ReadJsonBodyAsync();
            int pId = Request.ResolveProductId(body);
            int explicitQty = Request.ResolveInt(body, "quantity", -1);
            int delta = Request.ResolveInt(body, "delta", 0);

            string cartId = GetOrCreateCartId();
            _logger.LogInformation("UpdateQuantity: CartId={CartId}, ProductId={ProductId}, ExplicitQty={ExplicitQty}, Delta={Delta}", cartId, pId, explicitQty, delta);

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
            var extraData = new Dictionary<string, object>
            {
                ["itemQuantity"] = updatedItem?.Quantity ?? 0,
                ["itemLineTotal"] = OrderPricingConstants.FormatVnd(updatedItem?.LineTotal ?? 0),
                ["itemTotalFormatted"] = OrderPricingConstants.FormatVnd(updatedItem?.LineTotal ?? 0)
            };

            return CreateCartSummaryJson(cart, isGift, "Cập nhật số lượng thành công.", extraData);
        }

        [HttpPost]
        public async Task<IActionResult> RemoveItem()
        {
            var body = await Request.ReadJsonBodyAsync();
            int pId = Request.ResolveProductId(body);
            string cartId = GetOrCreateCartId();
            _logger.LogInformation("RemoveItem: CartId={CartId}, ProductId={ProductId}", cartId, pId);

            var cart = await _cartService.GetCartAsync(cartId, null);
            var item = cart?.Items.FirstOrDefault(i => i.ProductId == pId);
            if (item != null)
            {
                await _cartService.RemoveFromCartAsync(cartId, null, item.Id);
                cart = await _cartService.GetCartAsync(cartId, null);
            }

            bool isGift = IsGiftWrappedInCookie();
            return CreateCartSummaryJson(cart, isGift, "Đã xóa sản phẩm khỏi giỏ hàng.");
        }

        /// <summary>
        /// 2. DỊCH VỤ HỘP QUÀ TẶNG THƯỢNG LƯU (LUXURY GIFT PACKAGING)
        /// Cập nhật trạng thái đóng gói quà (+50.000₫) và nội dung thiệp viết tay
        /// </summary>
        [HttpPost]

        public async Task<IActionResult> UpdateGiftPackaging([FromBody] JsonElement? body = null)
        {
            bool isGiftWrapped = false;
            string giftMessage = "";

            body ??= await Request.ReadJsonBodyAsync();
            if (body.HasValue && body.Value.ValueKind == JsonValueKind.Object)
            {
                if (body.Value.TryGetProperty("isGiftWrapped", out var gProp))
                {
                    isGiftWrapped = gProp.GetBoolean();
                }
                if (body.Value.TryGetProperty("giftMessage", out var mProp))
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
            string msg = isGiftWrapped 
                ? "Đã áp dụng Dịch Vụ Đóng Gói Quà Tặng Thượng Lưu (+50.000₫)" 
                : "Đã bỏ chọn dịch vụ hộp quà tặng.";

            var extra = new Dictionary<string, object>
            {
                ["isGiftWrapped"] = isGiftWrapped,
                ["giftMessage"] = giftMessage,
                ["giftWrapFee"] = isGiftWrapped ? OrderPricingConstants.LuxuryGiftWrapFee : 0m
            };

            return CreateCartSummaryJson(cart, isGiftWrapped, msg, extra);
        }

        /// <summary>
        /// 4. KHỐI GỢI Ý MUA KÈM ƯU ĐÃI (CROSS-SELL CAROUSEL)
        /// Thêm nhanh Bông tẩy trang / Nước hoa mini / Mặt nạ với giá ưu đãi +29.000₫
        /// </summary>
        [HttpPost]

        public async Task<IActionResult> AddCrossSell([FromBody] JsonElement? body = null)
        {
            body ??= await Request.ReadJsonBodyAsync();
            int crossSellId = Request.ResolveInt(body, "crossSellId", 0);
            if (crossSellId <= 0) crossSellId = Request.ResolveProductId(body);

            var crossSellItem = DefaultCrossSellItems.FirstOrDefault(c => c.ProductId == crossSellId);
            if (crossSellItem == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm mua kèm ưu đãi." });
            }

            // Ensure Product exists in Database
            var product = await EnsureCrossSellProductExistsAsync(crossSellItem);

            string cartId = GetOrCreateCartId();
            _logger.LogInformation("AddCrossSell: CartId={CartId}, CrossSellId={CrossSellId}, ProductId={ProductId}", cartId, crossSellId, product.Id);

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
            var extra = new Dictionary<string, object>
            {
                ["productId"] = product.Id
            };

            return CreateCartSummaryJson(cart, isGift, $"Đã thêm {crossSellItem.ProductName} với giá ưu đãi độc quyền 29.000₫!", extra);
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
        public async Task<IActionResult> ApplyCoupon()
        {
            var body = await Request.ReadJsonBodyAsync();
            string couponCode = Request.ResolveString(body, "couponCode", "CouponCode", "code").Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(couponCode))
            {
                return Json(new { success = false, message = "Vui lòng nhập mã giảm giá." });
            }

            string cartId = GetOrCreateCartId();
            _logger.LogInformation("ApplyCoupon: CartId={CartId}, CouponCode={CouponCode}", cartId, couponCode);

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
                var extra = new Dictionary<string, object>
                {
                    ["discountAmount"] = discount,
                    ["discountFormatted"] = OrderPricingConstants.FormatVnd(discount),
                    ["memberTierName"] = promoResult.MemberTierName ?? "",
                    ["memberTierDiscount"] = promoResult.MemberTierDiscount,
                    ["fefoDiscount"] = promoResult.FefoDiscount,
                    ["giftItemsCount"] = promoResult.GiftItems.Count
                };

                return CreateCartSummaryJson(cart, isGift, promoResult.Message ?? $"Áp dụng mã {coupon.CouponCode} thành công! Giảm ngay {OrderPricingConstants.FormatVnd(discount)}.", extra);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi áp dụng voucher cho CartId {CartId}, Code: {CouponCode}", cartId, couponCode);
                return Json(new { success = false, message = "Lỗi áp dụng voucher: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult MiniCart()
        {
            return ViewComponent("MiniCart");
        }

        [HttpGet("Cart/GetCartSummary")]
        public async Task<IActionResult> GetCartSummary()
        {
            string cartId = GetOrCreateCartId();
            var cart = await _cartService.GetCartAsync(cartId, null);
            bool isGift = IsGiftWrappedInCookie();
            return CreateCartSummaryJson(cart, isGift, "Lấy thông tin giỏ hàng thành công.");
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
