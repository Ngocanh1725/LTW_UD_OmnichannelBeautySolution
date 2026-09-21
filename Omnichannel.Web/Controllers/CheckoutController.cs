using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Sales;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Web.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IOrderWorkflowService _orderWorkflow;
        private readonly ApplicationDbContext _context;

        private const string CartCookieKey = "OmnichannelBeauty_CartId";

        public CheckoutController(
            ICartService cartService,
            IOrderWorkflowService orderWorkflow,
            ApplicationDbContext context)
        {
            _cartService = cartService;
            _orderWorkflow = orderWorkflow;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!Request.Cookies.TryGetValue(CartCookieKey, out string? cartId) || string.IsNullOrEmpty(cartId))
            {
                return RedirectToAction("Index", "Cart");
            }

            var cartDto = await _cartService.GetCartAsync(cartId, null);
            if (!cartDto.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            var cartVm = new CartViewModel
            {
                Id = cartDto.Id,
                SessionId = cartDto.SessionId,
                UserId = cartDto.UserId,
                SubTotal = cartDto.SubTotal,
                DiscountAmount = cartDto.DiscountAmount,
                TotalAmount = cartDto.TotalAmount,
                TotalItems = cartDto.TotalItems,
                Items = cartDto.Items,
                CartItems = cartDto.CartItems,
                ShippingFee = cartDto.TotalAmount >= 500000 ? 0 : 30000,
                FinalTotal = cartDto.TotalAmount + (cartDto.TotalAmount >= 500000 ? 0 : 30000)
            };

            var vm = new CheckoutViewModel
            {
                Cart = cartVm,
                RecipientName = User.FindFirstValue(ClaimTypes.GivenName) ?? "",
                RecipientPhone = ""
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            if (!Request.Cookies.TryGetValue(CartCookieKey, out string? cartId) || string.IsNullOrEmpty(cartId))
            {
                return RedirectToAction("Index", "Cart");
            }

            if (!ModelState.IsValid)
            {
                var cDto = await _cartService.GetCartAsync(cartId, null);
                model.Cart = new CartViewModel
                {
                    Id = cDto.Id, SessionId = cDto.SessionId, UserId = cDto.UserId,
                    SubTotal = cDto.SubTotal, DiscountAmount = cDto.DiscountAmount,
                    TotalAmount = cDto.TotalAmount, TotalItems = cDto.TotalItems,
                    Items = cDto.Items, CartItems = cDto.CartItems,
                    ShippingFee = cDto.TotalAmount >= 500000 ? 0 : 30000,
                    FinalTotal = cDto.TotalAmount + (cDto.TotalAmount >= 500000 ? 0 : 30000)
                };
                return View("Index", model);
            }

            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var (success, message, result) = await _orderWorkflow.CreateOrderWithSoftReserveAsync(cartId, model, userId);

            if (!success || result == null)
            {
                TempData["ErrorMessage"] = message;
                var cDto2 = await _cartService.GetCartAsync(cartId, null);
                model.Cart = new CartViewModel
                {
                    Id = cDto2.Id, SessionId = cDto2.SessionId, UserId = cDto2.UserId,
                    SubTotal = cDto2.SubTotal, DiscountAmount = cDto2.DiscountAmount,
                    TotalAmount = cDto2.TotalAmount, TotalItems = cDto2.TotalItems,
                    Items = cDto2.Items, CartItems = cDto2.CartItems,
                    ShippingFee = cDto2.TotalAmount >= 500000 ? 0 : 30000,
                    FinalTotal = cDto2.TotalAmount + (cDto2.TotalAmount >= 500000 ? 0 : 30000)
                };
                return View("Index", model);
            }

            return RedirectToAction(nameof(Success), new { orderId = result.OrderId });
        }

        [HttpGet]
        public async Task<IActionResult> Success(string orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderCode == orderId);

            if (order == null) return NotFound();

            // Sinh mã QR nếu chọn VietQR
            VietQrPaymentDto? vietQr = null;
            if (order.PaymentMethod == "VNPAY" || order.PaymentMethod == "VIETQR")
            {
                string bankId = "MB";
                string accountNo = "0909999888";
                string accountName = "OMNICHANNEL BEAUTY";
                string qrUrl = $"https://img.vietqr.io/image/{bankId}-{accountNo}-compact2.png?amount={(long)order.TotalAmount}&addInfo={order.OrderCode}&accountName={accountName}";

                vietQr = new VietQrPaymentDto
                {
                    BankName = "MBBank (Ngân Hàng Quân Đội)",
                    BankAccountNo = accountNo,
                    AccountName = "CÔNG TY TNHH OMNICHANNEL BEAUTY",
                    Amount = order.TotalAmount,
                    OrderInfo = order.OrderCode,
                    QrImageUrl = qrUrl,
                    ExpireSeconds = 900
                };
            }

            var vm = new OrderSuccessViewModel
            {
                OrderId = order.OrderCode,
                RecipientName = order.CustomerName,
                RecipientPhone = order.CustomerPhone,
                ShippingAddress = order.ShippingAddress,
                FinalTotal = order.TotalAmount,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,
                OrderStatus = order.OrderStatus,
                CreatedAt = order.CreatedAt,
                VietQr = vietQr
            };

            return View(vm);
        }

        // Webhook mô phỏng Ngân hàng hoặc Thu ngân xác nhận thanh toán
        [HttpPost]
        public async Task<IActionResult> MockBankWebhook([FromBody] BankCallbackDto callback)
        {
            var (success, message) = await _orderWorkflow.ConfirmPaymentAndDeductPhysicalStockAsync(callback);
            return Json(new { success, message });
        }

        // Endpoint kiểm tra trạng thái đơn hàng (Polling từ trang Success)
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