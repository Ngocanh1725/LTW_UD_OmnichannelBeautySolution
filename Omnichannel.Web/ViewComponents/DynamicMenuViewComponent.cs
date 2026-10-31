using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Omnichannel.Application.DTOs.Cms;
using Omnichannel.Application.Interfaces.Cms;

namespace Omnichannel.Web.ViewComponents
{
    public class DynamicMenuViewComponent : ViewComponent
    {
        private readonly IMenuEngineService _menuService;

        public DynamicMenuViewComponent(IMenuEngineService menuService)
        {
            _menuService = menuService;
        }

        public async Task<IViewComponentResult> InvokeAsync(string menuCode = "HEADER_MAIN")
        {
            var menuTree = await _menuService.GetHierarchyTreeAsync(menuCode, onlyActive: true);
            ViewBag.MenuCode = menuCode;
            return View(menuTree ?? new List<MenuItemDto>());
        }
    }
}
