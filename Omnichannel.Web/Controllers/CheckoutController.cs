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

            var cart = await _cartService.GetCartAsync(cartId);
            if (!cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            var vm = new CheckoutViewModel
            {
                Cart = cart,
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
                model.Cart = await _cartService.GetCartAsync(cartId);
                return View("Index", model);
            }

            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var (success, message, result) = await _orderWorkflow.CreateOrderWithSoftReserveAsync(cartId, model, userId);

            if (!success || result == null)
            {
                TempData["ErrorMessage"] = message;
                model.Cart = await _cartService.GetCartAsync(cartId);
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
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return NotFound();

            // Sinh mã QR nếu chọn VietQR
            VietQrPaymentDto? vietQr = null;
            if (order.PaymentMethod == 2)
            {
                string bankId = "MB";
                string accountNo = "0909999888";
                string accountName = "OMNICHANNEL BEAUTY";
                string qrUrl = $"https://img.vietqr.io/image/{bankId}-{accountNo}-compact2.png?amount={(long)order.FinalTotal}&addInfo={order.OrderId}&accountName={accountName}";

                vietQr = new VietQrPaymentDto
                {
                    BankName = "MBBank (Ngân Hàng Quân Đội)",
                    BankAccountNo = accountNo,
                    AccountName = "CÔNG TY TNHH OMNICHANNEL BEAUTY",
                    Amount = order.FinalTotal,
                    OrderInfo = order.OrderId,
                    QrImageUrl = qrUrl,
                    ExpireSeconds = 900
                };
            }

            var vm = new OrderSuccessViewModel
            {
                OrderId = order.OrderId,
                RecipientName = order.RecipientName,
                RecipientPhone = order.RecipientPhone,
                ShippingAddress = order.ShippingAddress,
                FinalTotal = order.FinalTotal,
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
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return NotFound();

            return Json(new
            {
                orderId = order.OrderId,
                paymentStatus = order.PaymentStatus,
                orderStatus = order.OrderStatus,
                isPaid = order.PaymentStatus == 2
            });
        }
    }
}