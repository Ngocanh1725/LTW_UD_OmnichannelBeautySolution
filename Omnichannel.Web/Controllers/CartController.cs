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
            var cart = await _cartService.GetCartAsync(cartId, null);
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
            int pId = int.TryParse(req.ProductId, out var id) ? id : 0;
            var c = await _cartService.AddToCartAsync(cartId, null, pId, null, req.Quantity);
            bool success = c != null;

            if (success)
            {
                var cart = await _cartService.GetCartAsync(cartId, null);
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
            int pId = int.TryParse(req.ProductId, out var id) ? id : 0;
            var cart = await _cartService.GetCartAsync(cartId, null);
            var item = cart?.Items.FirstOrDefault(i => i.ProductId == pId);
            if (item != null)
            {
                await _cartService.UpdateQuantityAsync(cartId, null, item.Id, req.Delta);
                cart = await _cartService.GetCartAsync(cartId, null);
            }

            return Json(new
            {
                success = true,
                totalItems = cart?.TotalItems ?? 0,
                subTotal = (cart?.SubTotal ?? 0).ToString("N0") + " đ",
                shippingFee = 0.ToString("N0") + " đ",
                finalTotal = (cart?.TotalAmount ?? 0).ToString("N0") + " đ"
            });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveItem([FromBody] string productId)
        {
            string cartId = GetOrCreateCartId();
            int pId = int.TryParse(productId, out var id) ? id : 0;
            var cart = await _cartService.GetCartAsync(cartId, null);
            var item = cart?.Items.FirstOrDefault(i => i.ProductId == pId);
            if (item != null)
            {
                await _cartService.RemoveFromCartAsync(cartId, null, item.Id);
            }
            return Json(new { success = true });
        }

        [HttpGet]
        public IActionResult GetMiniCart()
        {
            return ViewComponent("MiniCart");
        }
    }
}
