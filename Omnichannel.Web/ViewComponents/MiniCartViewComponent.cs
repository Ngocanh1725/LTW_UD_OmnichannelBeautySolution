using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Omnichannel.Application.Interfaces.Sales;

namespace Omnichannel.Web.ViewComponents
{
    public class MiniCartViewComponent : ViewComponent
    {
        private readonly ICartService _cartService;
        private const string CartCookieKey = "OmnichannelBeauty_CartId";

        public MiniCartViewComponent(ICartService cartService)
        {
            _cartService = cartService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            string cartId = Request.Cookies.TryGetValue(CartCookieKey, out string? id) ? id ?? "" : "";
            var cart = await _cartService.GetCartAsync(cartId);
            return View(cart);
        }
    }
}