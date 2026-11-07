using Omnichannel.Web.Filters;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.Common;
using Omnichannel.Application.DTOs.Sales;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Web.Extensions;

namespace Omnichannel.Web.Controllers
{
    [BlockAdminShopping]
    public class CheckoutController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IOrderWorkflowService _orderWorkflow;
        private readonly IPaymentService _paymentService;
        private readonly IPromotionEngine _promotionEngine;
        private readonly IShippingService _shippingService;
        private readonly IIdempotencyService _idempotencyService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CheckoutController> _logger;

        private const string CartCookieKey = "CanmakeBeauty_CartId";
        private const string CouponCookieKey = "CanmakeBeauty_AppliedCoupon";
        private const string GiftWrapCookieKey = "CanmakeBeauty_GiftWrap";
        private const string GiftMessageCookieKey = "CanmakeBeauty_GiftMessage";

        public CheckoutController(
            ICartService cartService,
            IOrderWorkflowService orderWorkflow,
            IPaymentService paymentService,
            IPromotionEngine promotionEngine,
            IShippingService shippingService,
            IIdempotencyService idempotencyService,
            ApplicationDbContext context,
            ILogger<CheckoutController> logger)
        {
            _cartService = cartService;
            _orderWorkflow = orderWorkflow;
            _paymentService = paymentService;
            _promotionEngine = promotionEngine;
            _shippingService = shippingService;
            _idempotencyService = idempotencyService;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] string? couponCode = null)
        {
            if (!Request.Cookies.TryGetValue(CartCookieKey, out string? cartId) || string.IsNullOrEmpty(cartId))
            {
                return RedirectToAction("Index", "Cart");
            }

            var cartDto = await _cartService.GetCartAsync(cartId, null);
            if (cartDto == null || !cartDto.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            // Lấy mã coupon từ query hoặc cookie
            string? activeCouponCode = couponCode;
            if (string.IsNullOrWhiteSpace(activeCouponCode) && Request.Cookies.TryGetValue(CouponCookieKey, out var cCode))
            {
                activeCouponCode = cCode;
            }

            // Lấy UserId của khách hàng
            int? currentUserId = null;
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int parsedId))
                {
                    currentUserId = parsedId;
                }
            }

            // Đánh giá khuyến mãi toàn diện qua PromotionEngine
            var promoResult = await _promotionEngine.EvaluateCartAsync(cartDto.Items, activeCouponCode, currentUserId);

            // Kiểm tra dịch vụ đóng gói quà tặng thượng lưu
            bool isGiftWrapped = Request.Cookies.TryGetValue(GiftWrapCookieKey, out var gw) && gw == "true";
            string? giftMessage = Request.Cookies.TryGetValue(GiftMessageCookieKey, out var gm) ? gm : null;

            // Sử dụng Factory Method tập trung chuẩn Clean Architecture
            var cartVm = CartViewModel.FromPromotionResult(cartDto, promoResult, isGiftWrapped, giftMessage, currentUserId);

            string recipientName = "";
            string recipientPhone = "";
            string recipientEmail = "";
            int availableLoyaltyPoints = 100;

            if (currentUserId.HasValue)
            {
                var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUserId.Value);
                if (user != null)
                {
                    recipientName = user.FullName ?? "";
                    recipientPhone = user.PhoneNumber ?? "";
                    recipientEmail = user.Email ?? "";
                }

                var wallet = await _context.LoyaltyWallets.AsNoTracking().FirstOrDefaultAsync(w => w.UserId == currentUserId.Value);
                if (wallet != null && wallet.CurrentBalance > 0)
                {
                    availableLoyaltyPoints = wallet.CurrentBalance;
                }
            }

            if (string.IsNullOrWhiteSpace(recipientName))
            {
                recipientName = User.FindFirstValue(ClaimTypes.GivenName) ?? User.FindFirstValue(ClaimTypes.Name) ?? "";
            }
            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                recipientEmail = User.FindFirstValue(ClaimTypes.Email) ?? "";
            }

            var vm = new CheckoutViewModel
            {
                Cart = cartVm,
                CouponCode = promoResult.AppliedCouponCode,
                RecipientName = recipientName,
                RecipientPhone = recipientPhone,
                RecipientEmail = recipientEmail,
                ShippingMethod = "STANDARD",
                AvailableLoyaltyPoints = availableLoyaltyPoints,
                LoyaltyPointsToRedeem = Math.Min(100, availableLoyaltyPoints),
                LoyaltyDiscountAmount = Math.Min(100, availableLoyaltyPoints) * 1000m,
                PointConversionRate = 1000m,
                UseLoyaltyPoints = false
            };

            return View(vm);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ApplyVoucher([FromBody] System.Text.Json.JsonElement? body = null)
        {
            string couponCode = Request.ResolveString(body, "couponCode", "code").Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(couponCode))
            {
                return Json(new { success = false, message = "Vui lòng nhập mã giảm giá." });
            }

            if (!Request.Cookies.TryGetValue(CartCookieKey, out string? cartId) || string.IsNullOrEmpty(cartId))
            {
                return Json(new { success = false, message = "Không tìm thấy giỏ hàng của bạn." });
            }

            var cartDto = await _cartService.GetCartAsync(cartId, null);
            if (cartDto == null || !cartDto.Items.Any())
            {
                return Json(new { success = false, message = "Giỏ hàng đang trống." });
            }

            int? currentUserId = null;
            if (User.Identity != null && User.Identity.IsAuthenticated && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int uid))
            {
                currentUserId = uid;
            }

            // Kiểm tra validation chi tiết của mã coupon
            var validation = await _promotionEngine.ValidateCouponDetailAsync(couponCode, currentUserId, cartDto.SubTotal);
            if (!validation.IsValid)
            {
                return Json(new { success = false, message = validation.Message, errorCode = validation.ErrorCode });
            }

            // Đánh giá lại toàn bộ đơn hàng
            var eval = await _promotionEngine.EvaluateCartAsync(cartDto.Items, couponCode, currentUserId);
            if (!eval.IsSuccess || eval.CouponDiscount <= 0)
            {
                return Json(new { success = false, message = eval.Message ?? "Mã không đủ điều kiện áp dụng cho đơn hàng này." });
            }

            // Lưu coupon vào Cookie
            Response.Cookies.Append(CouponCookieKey, couponCode, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(1),
                HttpOnly = true,
                SameSite = SameSiteMode.Lax
            });

            bool isGiftWrapped = Request.Cookies.TryGetValue(GiftWrapCookieKey, out var gw) && gw == "true";
            decimal giftWrapFee = isGiftWrapped ? OrderPricingConstants.LuxuryGiftWrapFee : 0m;
            decimal shippingFee = OrderPricingConstants.CalculateStandardShippingFee(eval.FinalTotal);
            decimal finalCheckoutTotal = eval.FinalTotal + shippingFee + giftWrapFee;

            return Json(new
            {
                success = true,
                message = eval.Message ?? $"Áp dụng mã {couponCode} thành công!",
                couponCode = eval.AppliedCouponCode,
                subTotal = eval.SubTotal,
                subTotalFormatted = OrderPricingConstants.FormatVnd(eval.SubTotal),
                totalDiscount = eval.TotalDiscount,
                totalDiscountFormatted = OrderPricingConstants.FormatVnd(eval.TotalDiscount),
                couponDiscount = eval.CouponDiscount,
                couponDiscountFormatted = OrderPricingConstants.FormatVnd(eval.CouponDiscount),
                memberTierName = eval.MemberTierName,
                memberTierDiscount = eval.MemberTierDiscount,
                memberTierDiscountFormatted = OrderPricingConstants.FormatVnd(eval.MemberTierDiscount),
                fefoDiscount = eval.FefoDiscount,
                fefoDiscountFormatted = OrderPricingConstants.FormatVnd(eval.FefoDiscount),
                promotionDiscount = eval.PromotionDiscount,
                giftItems = eval.GiftItems,
                appliedRulesSummary = eval.AppliedRulesSummary,
                shippingFee = shippingFee,
                shippingFeeFormatted = OrderPricingConstants.FormatShippingFee(shippingFee),
                finalTotal = finalCheckoutTotal,
                finalTotalFormatted = OrderPricingConstants.FormatVnd(finalCheckoutTotal)
            });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RemoveVoucher()
        {
            Response.Cookies.Delete(CouponCookieKey);

            if (!Request.Cookies.TryGetValue(CartCookieKey, out string? cartId) || string.IsNullOrEmpty(cartId))
            {
                return Json(new { success = true, message = "Đã bỏ mã giảm giá." });
            }

            var cartDto = await _cartService.GetCartAsync(cartId, null);
            if (cartDto == null || !cartDto.Items.Any())
            {
                return Json(new { success = true, message = "Đã bỏ mã giảm giá." });
            }

            int? currentUserId = null;
            if (User.Identity != null && User.Identity.IsAuthenticated && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int uid))
            {
                currentUserId = uid;
            }

            var eval = await _promotionEngine.EvaluateCartAsync(cartDto.Items, null, currentUserId);
            bool isGiftWrapped = Request.Cookies.TryGetValue(GiftWrapCookieKey, out var gw) && gw == "true";
            decimal giftWrapFee = isGiftWrapped ? 50000 : 0;
            decimal shippingFee = eval.FinalTotal >= 500000 ? 0 : 30000;
            decimal finalCheckoutTotal = eval.FinalTotal + shippingFee + giftWrapFee;

            return Json(new
            {
                success = true,
                message = "Đã hủy áp dụng mã giảm giá.",
                subTotal = eval.SubTotal,
                subTotalFormatted = eval.SubTotal.ToString("N0") + " ₫",
                totalDiscount = eval.TotalDiscount,
                totalDiscountFormatted = eval.TotalDiscount.ToString("N0") + " ₫",
                couponDiscount = 0,
                couponDiscountFormatted = "0 ₫",
                memberTierName = eval.MemberTierName,
                memberTierDiscount = eval.MemberTierDiscount,
                memberTierDiscountFormatted = eval.MemberTierDiscount.ToString("N0") + " ₫",
                fefoDiscount = eval.FefoDiscount,
                fefoDiscountFormatted = eval.FefoDiscount.ToString("N0") + " ₫",
                shippingFee = shippingFee,
                shippingFeeFormatted = shippingFee == 0 ? "Miễn phí" : shippingFee.ToString("N0") + " ₫",
                finalTotal = finalCheckoutTotal,
                finalTotalFormatted = finalCheckoutTotal.ToString("N0") + " ₫"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            if (!Request.Cookies.TryGetValue(CartCookieKey, out string? cartId) || string.IsNullOrEmpty(cartId))
            {
                return RedirectToAction("Index", "Cart");
            }

            int? currentUserId = null;
            if (User.Identity != null && User.Identity.IsAuthenticated && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int uid))
            {
                currentUserId = uid;
            }

            var cDto = await _cartService.GetCartAsync(cartId, null);
            if (cDto == null || !cDto.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            // Cung cấp fallback an toàn cho thông tin giao nhận nếu để trống
            if (string.IsNullOrWhiteSpace(model.ProvinceId)) model.ProvinceId = "202";
            if (string.IsNullOrWhiteSpace(model.DistrictId)) model.DistrictId = "1442";
            if (string.IsNullOrWhiteSpace(model.WardCode)) model.WardCode = "20101";
            if (string.IsNullOrWhiteSpace(model.RecipientName)) model.RecipientName = "Khách Hàng Trải Nghiệm";
            if (string.IsNullOrWhiteSpace(model.RecipientPhone)) model.RecipientPhone = "0901234567";
            if (string.IsNullOrWhiteSpace(model.ShippingAddress)) model.ShippingAddress = "123 Lê Lợi, Phường Bến Nghé, Quận 1";

            ModelState.Remove(nameof(model.ProvinceId));
            ModelState.Remove(nameof(model.DistrictId));
            ModelState.Remove(nameof(model.WardCode));
            ModelState.Remove(nameof(model.RecipientName));
            ModelState.Remove(nameof(model.RecipientPhone));
            ModelState.Remove(nameof(model.ShippingAddress));

            // Cung cấp fallback đọc coupon từ Cookie nếu model.CouponCode chưa có
            if (string.IsNullOrWhiteSpace(model.CouponCode) && Request.Cookies.TryGetValue(CouponCookieKey, out var cFromCookie) && !string.IsNullOrWhiteSpace(cFromCookie))
            {
                model.CouponCode = cFromCookie;
            }

            // Kiểm tra ngặt nghèo mã Coupon nếu có
            if (!string.IsNullOrWhiteSpace(model.CouponCode))
            {
                var couponVal = await _promotionEngine.ValidateCouponDetailAsync(model.CouponCode, currentUserId, cDto.SubTotal);
                if (!couponVal.IsValid)
                {
                    ModelState.AddModelError("CouponCode", couponVal.Message);
                    TempData["ErrorMessage"] = couponVal.Message;
                }
            }

            _logger.LogInformation(
                "[Checkout] Khách hàng đặt hàng | CartId: {CartId} | UserId: {UserId} | Tên: {RecipientName} | Phương thức: {PaymentMethod}",
                cartId, currentUserId, model.RecipientName, model.PaymentMethod);

            if (!ModelState.IsValid)
            {
                var eval = await _promotionEngine.EvaluateCartAsync(cDto.Items, model.CouponCode, currentUserId);
                bool isGift = Request.Cookies.TryGetValue(GiftWrapCookieKey, out var gw) && gw == "true";
                string? gMsg = Request.Cookies.TryGetValue(GiftMessageCookieKey, out var gm) ? gm : null;
                model.Cart = CartViewModel.FromPromotionResult(cDto, eval, isGift, gMsg, currentUserId);
                return View("Index", model);
            }

            string? userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var (success, message, result) = await _orderWorkflow.CreateOrderWithSoftReserveAsync(cartId, model, userIdStr);

            if (!success || result == null)
            {
                _logger.LogWarning("[Checkout] Đặt hàng không thành công | CartId: {CartId} | Lý do: {Message}", cartId, message);
                TempData["ErrorMessage"] = message;
                var eval = await _promotionEngine.EvaluateCartAsync(cDto.Items, model.CouponCode, currentUserId);
                bool isGift = Request.Cookies.TryGetValue(GiftWrapCookieKey, out var gw) && gw == "true";
                string? gMsg = Request.Cookies.TryGetValue(GiftMessageCookieKey, out var gm) ? gm : null;
                model.Cart = CartViewModel.FromPromotionResult(cDto, eval, isGift, gMsg, currentUserId);
                return View("Index", model);
            }

            _logger.LogInformation(
                "[Checkout] Đặt hàng thành công! | OrderCode: {OrderCode} | CartId: {CartId} | UserId: {UserId}",
                result.OrderId, cartId, currentUserId);

            // Xóa cookie coupon và giftwrap sau khi đặt hàng thành công
            Response.Cookies.Delete(CouponCookieKey);
            Response.Cookies.Delete(GiftWrapCookieKey);
            Response.Cookies.Delete(GiftMessageCookieKey);

            // Redirect to VNPay if PaymentMethod == 3 (VNPay)
            if (model.PaymentMethod == 3)
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderCode == result.OrderId);
                if (order != null)
                {
                    string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                    string returnUrl = $"{Request.Scheme}://{Request.Host}/Checkout/VnPayCallback";
                    string vnpayUrl = _paymentService.CreateVnPayPaymentUrl(order, ipAddress, returnUrl);
                    return Redirect(vnpayUrl);
                }
            }

            // Redirect to MoMo if PaymentMethod == 5 (MoMo)
            if (model.PaymentMethod == 5)
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderCode == result.OrderId);
                if (order != null)
                {
                    string returnUrl = $"{Request.Scheme}://{Request.Host}/Checkout/MomoCallback";
                    string ipnUrl = $"{Request.Scheme}://{Request.Host}/Checkout/MomoIpn";
                    var (momoSuccess, momoPayUrl, _, _) = await _paymentService.CreateMoMoPaymentAsync(order, returnUrl, ipnUrl);
                    if (momoSuccess && !string.IsNullOrEmpty(momoPayUrl))
                    {
                        return Redirect(momoPayUrl);
                    }
                    return RedirectToAction(nameof(MomoSandbox), new { orderId = order.OrderCode });
                }
            }

            // VietQR (2) hoặc COD (4) hoặc Cash (1)
            return RedirectToAction(nameof(OrderSuccess), new { orderId = result.OrderId });
        }

        #region CỔNG THANH TOÁN CALLBACK & IPN VỚI IDEMPOTENCY GUARD

        [HttpGet]
        public async Task<IActionResult> VnPayCallback()
        {
            var dict = new Dictionary<string, string>();
            foreach (var key in Request.Query.Keys)
            {
                dict[key] = Request.Query[key].ToString();
            }

            var (success, orderCode, amount, transNo, message) = await _paymentService.ProcessVnPayCallbackAsync(dict);

            if (success)
            {
                string idemKey = $"VNPAY_{orderCode}_{transNo}";
                if (!await _idempotencyService.HasBeenProcessedAsync(idemKey))
                {
                    var callbackDto = new BankCallbackDto
                    {
                        OrderId = orderCode ?? "",
                        Amount = amount,
                        TransactionRef = transNo ?? "",
                        Status = "SUCCESS"
                    };
                    await _orderWorkflow.ConfirmPaymentAndDeductPhysicalStockAsync(callbackDto);
                    await _idempotencyService.RecordSuccessAsync(idemKey, Request.Path, transNo, 200, "{\"success\":true}");
                }

                TempData["SuccessMessage"] = "Thanh toán qua Cổng VNPAY thành công!";
                return RedirectToAction(nameof(OrderSuccess), new { orderId = orderCode });
            }

            TempData["ErrorMessage"] = message ?? "Thanh toán VNPay thất bại hoặc chữ ký không hợp lệ.";
            return RedirectToAction("Index", "Cart");
        }

        [HttpGet]
        [Route("Checkout/VnPayIpn")]
        [Route("api/payment/vnpay-ipn")]
        public async Task<IActionResult> VnPayIpn()
        {
            var dict = new Dictionary<string, string>();
            foreach (var key in Request.Query.Keys)
            {
                dict[key] = Request.Query[key].ToString();
            }

            string orderCode = dict.TryGetValue("vnp_TxnRef", out var txn) ? txn : "";
            string transNo = dict.TryGetValue("vnp_TransactionNo", out var tn) ? tn : "";
            string respCode = dict.TryGetValue("vnp_ResponseCode", out var rc) ? rc : "";
            string idemKey = $"VNPAY_{orderCode}_{transNo}";

            // 1. Kiểm tra Idempotency chống trùng lặp IPN
            if (await _idempotencyService.HasBeenProcessedAsync(idemKey))
            {
                return Json(new { RspCode = "00", Message = "Confirm Success (Idempotent)" });
            }

            // 2. Xác thực chữ ký
            if (!_paymentService.ValidateVnPaySignature(dict))
            {
                return Json(new { RspCode = "97", Message = "Invalid Checksum" });
            }

            // 3. Kiểm tra đơn hàng
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderCode == orderCode);
            if (order == null)
            {
                return Json(new { RspCode = "01", Message = "Order Not Found" });
            }

            if (dict.TryGetValue("vnp_Amount", out var amtStr) && decimal.TryParse(amtStr, out decimal parsedAmt))
            {
                decimal amountVnd = parsedAmt / 100m;
                if (Math.Abs(order.TotalAmount - amountVnd) > 1.0m)
                {
                    return Json(new { RspCode = "04", Message = "Invalid Amount" });
                }
            }

            if (order.PaymentStatus == "PAID")
            {
                await _idempotencyService.RecordSuccessAsync(idemKey, Request.Path, transNo, 200, "Already Paid");
                return Json(new { RspCode = "02", Message = "Order already confirmed" });
            }

            if (respCode == "00")
            {
                var callbackDto = new BankCallbackDto
                {
                    OrderId = orderCode,
                    Amount = order.TotalAmount,
                    TransactionRef = transNo,
                    Status = "SUCCESS"
                };
                await _orderWorkflow.ConfirmPaymentAndDeductPhysicalStockAsync(callbackDto);
                await _idempotencyService.RecordSuccessAsync(idemKey, Request.Path, transNo, 200, "{\"RspCode\":\"00\"}");
                return Json(new { RspCode = "00", Message = "Confirm Success" });
            }

            return Json(new { RspCode = "00", Message = "Payment Failed Status Recorded" });
        }

        [HttpGet]
        public async Task<IActionResult> MomoCallback()
        {
            var dict = new Dictionary<string, string>();
            foreach (var key in Request.Query.Keys)
            {
                dict[key] = Request.Query[key].ToString();
            }

            var (success, orderCode, amount, transId, msg) = await _paymentService.ProcessMoMoCallbackAsync(dict);
            if (success)
            {
                string idemKey = $"MOMO_{orderCode}_{transId}";
                if (!await _idempotencyService.HasBeenProcessedAsync(idemKey))
                {
                    var callbackDto = new BankCallbackDto
                    {
                        OrderId = orderCode,
                        Amount = amount,
                        TransactionRef = transId,
                        Status = "SUCCESS"
                    };
                    await _orderWorkflow.ConfirmPaymentAndDeductPhysicalStockAsync(callbackDto);
                    await _idempotencyService.RecordSuccessAsync(idemKey, Request.Path, transId, 200, "{\"success\":true}");
                }

                TempData["SuccessMessage"] = "Thanh toán qua Ví MoMo thành công!";
                return RedirectToAction(nameof(OrderSuccess), new { orderId = orderCode });
            }

            TempData["ErrorMessage"] = msg ?? "Thanh toán MoMo thất bại hoặc bị hủy.";
            return RedirectToAction("Index", "Cart");
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("Checkout/MomoIpn")]
        [Route("api/payment/momo-ipn")]
        public async Task<IActionResult> MomoIpn([FromBody] System.Text.Json.JsonElement body)
        {
            var dict = new Dictionary<string, string>();
            if (body.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                foreach (var prop in body.EnumerateObject())
                {
                    dict[prop.Name] = prop.Value.ToString();
                }
            }

            string orderId = dict.TryGetValue("orderId", out var oid) ? oid : "";
            string transId = dict.TryGetValue("transId", out var tid) ? tid : Guid.NewGuid().ToString("N")[..8];
            string idemKey = $"MOMO_{orderId}_{transId}";

            if (await _idempotencyService.HasBeenProcessedAsync(idemKey))
            {
                return NoContent();
            }

            var (success, orderCode, amount, _, _) = await _paymentService.ProcessMoMoCallbackAsync(dict);
            if (success)
            {
                var callbackDto = new BankCallbackDto
                {
                    OrderId = orderCode,
                    Amount = amount,
                    TransactionRef = transId,
                    Status = "SUCCESS"
                };
                await _orderWorkflow.ConfirmPaymentAndDeductPhysicalStockAsync(callbackDto);
                await _idempotencyService.RecordSuccessAsync(idemKey, Request.Path, transId, 204, null);
                return NoContent();
            }

            return BadRequest();
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("Checkout/VietQrWebhook")]
        [Route("api/payment/vietqr-webhook")]
        public async Task<IActionResult> VietQrWebhook([FromBody] System.Text.Json.JsonElement body)
        {
            string transactionId = body.TryGetProperty("transactionId", out var ti) ? ti.GetString() ?? "" : "";
            string content = body.TryGetProperty("content", out var ct) ? ct.GetString() ?? "" : "";
            decimal amount = body.TryGetProperty("amount", out var am) ? (am.ValueKind == System.Text.Json.JsonValueKind.Number ? am.GetDecimal() : (decimal.TryParse(am.GetString(), out var pa) ? pa : 0m)) : 0m;
            string signature = body.TryGetProperty("signature", out var sg) ? sg.GetString() ?? "" : "";

            if (string.IsNullOrWhiteSpace(transactionId))
            {
                transactionId = Guid.NewGuid().ToString("N")[..10];
            }

            string idemKey = $"VIETQR_{transactionId}";
            if (await _idempotencyService.HasBeenProcessedAsync(idemKey))
            {
                return Ok(new { success = true, message = "Giao dịch đã được xử lý trước đó (Idempotent)." });
            }

            var (success, orderCode, processedAmount, transId, message) =
                await _paymentService.ProcessVietQrWebhookAsync(transactionId, content, amount, signature);

            if (success)
            {
                var callbackDto = new BankCallbackDto
                {
                    OrderId = orderCode,
                    Amount = processedAmount,
                    TransactionRef = transId,
                    Status = "SUCCESS"
                };
                var confirmRes = await _orderWorkflow.ConfirmPaymentAndDeductPhysicalStockAsync(callbackDto);
                if (confirmRes.Success)
                {
                    await _idempotencyService.RecordSuccessAsync(idemKey, Request.Path, transactionId, 200, "{\"success\":true}");
                    return Ok(new { success = true, orderCode, message = "Thanh toán VietQR hoàn tất thành công." });
                }
                return BadRequest(new { success = false, message = confirmRes.Message });
            }

            return BadRequest(new { success = false, message });
        }

        [HttpGet]
        public async Task<IActionResult> CalculateShippingFee(string districtId, string wardCode, int weightGram = 500)
        {
            decimal fee = await _shippingService.CalculateShippingFeeAsync(districtId, wardCode, weightGram);
            return Json(new { success = true, shippingFee = fee });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGhnShipment(string orderCode, int weightGram = 500)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderCode == orderCode);
            if (order == null) return NotFound(new { success = false, message = "Không tìm thấy đơn hàng." });

            var res = await _shippingService.CreateShippingOrderAsync(order, weightGram);
            return Json(new { success = res.Success, trackingCode = res.TrackingCode, printUrl = res.PrintUrl, message = res.Message });
        }
        #endregion

        [HttpGet]
        public async Task<IActionResult> MomoSandbox(string orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderCode == orderId);

            if (order == null) return NotFound();

            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> OrderSuccess(string orderId)
        {
            return await BuildOrderSuccessView(orderId);
        }

        [HttpGet]
        public async Task<IActionResult> Success(string orderId)
        {
            return await BuildOrderSuccessView(orderId);
        }

        private async Task<IActionResult> BuildOrderSuccessView(string orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderCode == orderId);

            if (order == null) return NotFound();

            // Anti-IDOR Check: Nếu user đã đăng nhập, đơn hàng này phải thuộc về họ (trừ SuperAdmin)
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdStr, out var currentUserId))
                {
                    if (order.UserId.HasValue && order.UserId.Value != currentUserId && !User.IsInRole("SuperAdmin") && !User.IsInRole("SUPER_ADMIN"))
                    {
                        return Forbid();
                    }
                }
            }

            // Sinh mã VietQR động chuẩn Napas 247 nếu chọn VietQR hoặc đơn chưa thanh toán online
            VietQrPaymentDto? vietQr = null;
            if (order.PaymentMethod == "VIETQR" || order.PaymentMethod == "2" || (order.PaymentStatus != "PAID" && order.PaymentMethod != "COD" && order.PaymentMethod != "4"))
            {
                vietQr = _paymentService.GenerateVietQr(order);
            }

            // Thời gian giao hàng dự kiến
            string estDelivery = order.ShippingFee == 45000m
                ? "Giao hỏa tốc trong 2 giờ nội thành (Hôm nay)"
                : "Giao tiêu chuẩn 2 - 3 ngày làm việc";

            string methodDisplay = order.PaymentMethod switch
            {
                "VIETQR" or "2" => "Chuyển khoản VietQR 24/7 (Napas 247)",
                "VNPAY" or "3" => "Cổng thanh toán VNPAY (ATM / QR / Visa)",
                "COD" or "4" => "Thanh toán khi nhận hàng (COD)",
                "MOMO" or "5" => "Ví Điện Tử MoMo",
                _ => order.PaymentMethod
            };

            var items = order.OrderDetails.Select(od => new OrderDetailItemDto
            {
                ProductName = od.Product?.Name ?? "Sản phẩm mỹ phẩm cao cấp",
                Quantity = od.Quantity,
                UnitPrice = od.UnitPrice,
                LineTotal = od.LineTotal
            }).ToList();

            var vm = new OrderSuccessViewModel
            {
                OrderId = order.OrderCode ?? "",
                OrderDbId = order.Id,
                RecipientName = order.CustomerName ?? "",
                RecipientPhone = order.CustomerPhone ?? "",
                CustomerEmail = order.CustomerEmail,
                ShippingAddress = order.ShippingAddress ?? "",
                Notes = order.Note,
                FinalTotal = order.TotalAmount,
                PaymentMethod = methodDisplay,
                PaymentStatus = order.PaymentStatus,
                OrderStatus = order.OrderStatus,
                ShippingMethod = order.ShippingFee == 45000m ? "EXPRESS_2H" : "STANDARD",
                EstimatedDeliveryTime = estDelivery,
                CreatedAt = order.CreatedAt,
                VietQr = vietQr,
                Items = items
            };

            return View("OrderSuccess", vm);
        }

        // Webhook mô phỏng Ngân hàng hoặc Thu ngân xác nhận thanh toán
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> MockBankWebhook([FromBody] BankCallbackDto callback)
        {
            var (success, message) = await _orderWorkflow.ConfirmPaymentAndDeductPhysicalStockAsync(callback);
            return Json(new { success, message });
        }

        // Endpoint kiểm tra trạng thái đơn hàng (Polling từ trang Success / OrderSuccess)
        [HttpGet]
        public async Task<IActionResult> CheckOrderStatus(string orderId)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderCode == orderId);

            if (order == null) return NotFound();

            return Json(new
            {
                orderId = order.OrderCode,
                paymentStatus = order.PaymentStatus,
                orderStatus = order.OrderStatus,
                isPaid = order.PaymentStatus == "PAID"
            });
        }
    }
}