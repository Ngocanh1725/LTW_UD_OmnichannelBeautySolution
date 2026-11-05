using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.Common;
using Omnichannel.Application.DTOs.BeautyTech;
using Omnichannel.Application.Interfaces.Beauty;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Services.Beauty;
using Omnichannel.Web.Filters;

namespace Omnichannel.Web.Controllers
{
    public class RoutineQuizController : Controller
    {
        private readonly ISkincareConsultantService _consultantService;
        private readonly ISkinAnalysisService _skinAnalysisService;
        private readonly ICartService _cartService;
        private readonly IPromotionEngine _promotionEngine;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RoutineQuizController> _logger;

        private const string CartCookieKey = "CanmakeBeauty_CartId";
        private const string AppliedCouponCookieKey = "CanmakeBeauty_AppliedCoupon";

        public RoutineQuizController(
            ISkincareConsultantService consultantService,
            ISkinAnalysisService skinAnalysisService,
            ICartService cartService,
            IPromotionEngine promotionEngine,
            ApplicationDbContext context,
            ILogger<RoutineQuizController> logger)
        {
            _consultantService = consultantService ?? throw new ArgumentNullException(nameof(consultantService));
            _skinAnalysisService = skinAnalysisService ?? throw new ArgumentNullException(nameof(skinAnalysisService));
            _cartService = cartService ?? throw new ArgumentNullException(nameof(cartService));
            _promotionEngine = promotionEngine ?? throw new ArgumentNullException(nameof(promotionEngine));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("/tu-van-da")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Trắc Nghiệm Da & Tư Vấn Routine 4 Bước Chuẩn Y Khoa | Canmake Beauty";
            ViewData["Description"] = "Khám phá phác đồ chăm sóc sắc đẹp cá nhân hóa chuẩn Sephora & Hasaki. Nhận lộ trình 4 bước và ưu đãi mua trọn bộ tiết kiệm 15%.";
            return View();
        }

        [HttpGet("/phan-tich-da")]
        public IActionResult SkinAnalysis()
        {
            ViewData["Title"] = "Phân Tích Da Bằng Ảnh AI (AI Skin Advisor) | Canmake Beauty";
            ViewData["Description"] = "Chẩn đoán 5 chỉ số làn da (Dầu, Độ ẩm, Mụn, Sắc tố, Nhạy cảm) bằng công nghệ AI Vision chuẩn Olay Skin Advisor & Hasaki Clinic. Không lưu trữ ảnh cá nhân.";
            return View();
        }

        [HttpPost("/api/skin-analysis/analyze")]
        [HttpPost("/phan-tich-da/analyze")]
        public async Task<IActionResult> AnalyzeSkin(IFormFile? image)
        {
            if (image == null || image.Length == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Vui lòng chọn hoặc chụp một bức ảnh selfie để phân tích."
                });
            }

            if (image.Length > MockSkinAnalysisService.MaxImageSizeBytes)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Dung lượng ảnh vượt quá giới hạn tối đa 5MB. Vui lòng chọn ảnh nhỏ hơn 5MB."
                });
            }

            try
            {
                // Đọc ảnh vào bộ nhớ RAM (MemoryStream) để phân tích tức thì, TUYỆT ĐỐI KHÔNG lưu xuống ổ đĩa
                byte[] imageBytes;
                using (var ms = new MemoryStream((int)image.Length))
                {
                    await image.CopyToAsync(ms, HttpContext.RequestAborted);
                    imageBytes = ms.ToArray();
                }

                var result = await _skinAnalysisService.AnalyzeSkinImageAsync(
                    imageBytes,
                    image.FileName,
                    image.ContentType,
                    HttpContext.RequestAborted);

                // Giải phóng tham chiếu mảng byte ngay lập tức sau khi phân tích xong
                Array.Clear(imageBytes, 0, imageBytes.Length);

                if (!result.Success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message
                    });
                }

                return Json(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi phân tích ảnh da tại /api/skin-analysis/analyze");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi trong quá trình phân tích ảnh: " + ex.Message
                });
            }
        }

        [HttpPost("/api/quiz/recommend")]

        public async Task<IActionResult> Recommend([FromBody] SkinQuizRequestDto request)
        {
            if (request == null)
            {
                return Json(new { success = false, message = "Dữ liệu yêu cầu không hợp lệ." });
            }

            try
            {
                var result = await _consultantService.GenerateRoutineAsync(request, HttpContext.RequestAborted);
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong quá trình tạo gợi ý routine cá nhân hóa.");
                return Json(new { success = false, message = "Có lỗi xảy ra khi phân tích: " + ex.Message });
            }
        }

        [HttpPost("/api/quiz/add-routine-to-cart")]

        [BlockAdminShopping]
        public async Task<IActionResult> AddRoutineToCart([FromBody] AddRoutineToCartRequestDto request)
        {
            if (request == null || request.ProductIds == null || !request.ProductIds.Any())
            {
                return Json(new { success = false, message = "Danh sách sản phẩm routine trống." });
            }

            string cartId = GetOrCreateCartId();
            _logger.LogInformation("AddRoutineToCart: CartId={CartId}, ProductCount={Count}", cartId, request.ProductIds.Count);

            try
            {
                // Bọc tác vụ ghi dữ liệu phức tạp trong ExecutionStrategy theo Guardrail
                var strategy = _context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    // 1. Thêm lần lượt từng sản phẩm trong bộ 4 bước vào giỏ hàng
                    foreach (var productId in request.ProductIds.Distinct())
                    {
                        if (productId > 0)
                        {
                            await _cartService.AddToCartAsync(cartId, null, productId, null, 1);
                        }
                    }

                    // 2. Kích hoạt mã ưu đãi combo COMBO15
                    string couponCode = !string.IsNullOrWhiteSpace(request.CouponCode) ? request.CouponCode.Trim().ToUpperInvariant() : "COMBO15";
                    Response.Cookies.Append(AppliedCouponCookieKey, couponCode, new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddDays(2),
                        HttpOnly = true,
                        SameSite = SameSiteMode.Lax
                    });

                    // 3. Lấy giỏ hàng mới nhất và tính toán khuyến mãi
                    var cart = await _cartService.GetCartAsync(cartId, null);
                    if (cart == null)
                    {
                        return Json(new { success = false, message = "Không thể tải giỏ hàng sau khi thêm combo." });
                    }

                    var promoResult = await _promotionEngine.EvaluateCartAsync(cart.Items, couponCode, cart.UserId);
                    decimal totalDiscount = promoResult.TotalDiscount;
                    decimal subTotal = cart.Items.Sum(i => i.Quantity * i.UnitPrice);
                    decimal finalTotal = Math.Max(0, subTotal - totalDiscount);

                    return Json(new
                    {
                        success = true,
                        message = $"✨ Đã thêm trọn bộ {request.ProductIds.Count} bước vào giỏ hàng! Đã áp dụng mã {couponCode} giảm 15% (-{totalDiscount:N0}₫).",
                        cartCount = cart.Items.Sum(i => i.Quantity),
                        totalItems = cart.Items.Sum(i => i.Quantity),
                        subTotal = $"{subTotal:N0} ₫",
                        discountAmount = totalDiscount,
                        discountFormatted = $"{totalDiscount:N0} ₫",
                        finalTotal = $"{finalTotal:N0} ₫",
                        cartTotalFormatted = $"{finalTotal:N0} ₫",
                        items = cart.Items.Select(i => new
                        {
                            id = i.ProductId,
                            title = i.ProductName,
                            price = i.UnitPrice,
                            priceFormatted = $"{i.UnitPrice:N0} ₫",
                            quantity = i.Quantity,
                            imageUrl = i.ProductImageUrl
                        })
                    });
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi thêm trọn bộ routine vào giỏ hàng cho CartId={CartId}", cartId);
                return Json(new { success = false, message = "Không thể thêm combo vào giỏ hàng: " + ex.Message });
            }
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
    }
}
