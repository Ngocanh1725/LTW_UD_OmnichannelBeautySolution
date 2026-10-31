using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Omnichannel.Application.DTOs.Sales;
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
            CartDto? cart = null;
            if (!string.IsNullOrEmpty(cartId))
            {
                cart = await _cartService.GetCartAsync(cartId, null);
            }

            var vm = CartViewModel.FromDto(cart);
            return View(vm);
        }
    }
}