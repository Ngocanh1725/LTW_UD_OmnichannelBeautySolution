using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Omnichannel.Application.Interfaces.Cms;

namespace Omnichannel.Web.ViewComponents
{
    public class HeaderMenuViewComponent : ViewComponent
    {
        private readonly IMenuEngineService _menuService;

        public HeaderMenuViewComponent(IMenuEngineService menuService)
        {
            _menuService = menuService;
        }

        public async Task<IViewComponentResult> InvokeAsync(string menuCode = "HEADER_MAIN")
        {
            var menuTree = await _menuService.GetHierarchyTreeAsync(menuCode, onlyActive: true);
            return View(menuTree);
        }
    }
}
