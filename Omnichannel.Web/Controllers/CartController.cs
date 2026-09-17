using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Omnichannel.Application.DTOs.Sales;
using Omnichannel.Application.Interfaces.Sales;

namespace Omnichannel.Web.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private const string CartCookieKey = "OmnichannelBeauty_CartId";

        public CartController(ICartService cartService)
        {
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

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            string cartId = GetOrCreateCartId();
            var cart = await _cartService.GetCartAsync(cartId);
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest req)
        {
            if (string.IsNullOrEmpty(req.ProductId))
            {
                return Json(new { success = false, message = "Mã sản phẩm không hợp lệ." });
            }

            string cartId = GetOrCreateCartId();
            bool success = await _cartService.AddToCartAsync(cartId, req.ProductId, req.Quantity);

            if (success)
            {
                var cart = await _cartService.GetCartAsync(cartId);
                return Json(new
                {
                    success = true,
                    message = "Đã thêm sản phẩm vào giỏ hàng!",
                    totalItems = cart.TotalItems,
                    subTotal = cart.SubTotal.ToString("N0") + " đ"
                });
            }

            return Json(new { success = false, message = "Không thể thêm vào giỏ hàng." });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateCartItemRequest req)
        {
            string cartId = GetOrCreateCartId();
            await _cartService.UpdateQuantityAsync(cartId, req.ProductId, req.Delta);
            var cart = await _cartService.GetCartAsync(cartId);

            return Json(new
            {
                success = true,
                totalItems = cart.TotalItems,
                subTotal = cart.SubTotal.ToString("N0") + " đ",
                shippingFee = cart.ShippingFee.ToString("N0") + " đ",
                finalTotal = cart.FinalTotal.ToString("N0") + " đ"
            });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveItem([FromBody] string productId)
        {
            string cartId = GetOrCreateCartId();
            await _cartService.RemoveItemAsync(cartId, productId);
            return Json(new { success = true });
        }

        [HttpGet]
        public IActionResult GetMiniCart()
        {
            return ViewComponent("MiniCart");
        }
    }
}
