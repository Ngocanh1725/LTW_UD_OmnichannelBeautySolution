using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Cms;
using Omnichannel.Application.Interfaces.Cms;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Security;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class MenuController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMenuEngineService _menuService;

        public MenuController(ApplicationDbContext context, IMenuEngineService menuService)
        {
            _context = context;
            _menuService = menuService;
        }

        [HttpGet]
        [HasPermission("MENU", "VIEW")]
        public async Task<IActionResult> Index(int? menuId = null)
        {
            var menus = await _context.Menus
                .AsNoTracking()
                .OrderBy(m => m.Position)
                .Select(m => new MenuLookupDto
                {
                    MenuId = m.MenuId,
                    MenuCode = m.MenuCode,
                    MenuName = m.MenuName,
                    Position = m.Position
                })
                .ToListAsync();

            var currentMenu = menuId.HasValue
                ? menus.FirstOrDefault(m => m.MenuId == menuId.Value)
                : menus.FirstOrDefault();

            if (currentMenu == null)
            {
                return View(new MenuManagementViewModel());
            }

            var tree = await _menuService.GetHierarchyTreeAsync(currentMenu.MenuCode, onlyActive: false);

            var vm = new MenuManagementViewModel
            {
                SelectedMenuId = currentMenu.MenuId,
                SelectedMenuCode = currentMenu.MenuCode,
                SelectedMenuName = currentMenu.MenuName,
                AvailableMenus = menus,
                MenuTree = tree,
                NewItem = new CreateMenuItemViewModel { MenuId = currentMenu.MenuId }
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("MENU", "CREATE")]
        public async Task<IActionResult> CreateItem(CreateMenuItemViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu nhập vào chưa hợp lệ.";
                return RedirectToAction(nameof(Index), new { menuId = model.MenuId });
            }

            bool success = await _menuService.AddMenuItemAsync(model);
            if (success)
            {
                TempData["SuccessMessage"] = $"Đã thêm thành công nút '{model.Title}' vào cây Menu!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể thêm nút Menu. Vui lòng thử lại.";
            }

            return RedirectToAction(nameof(Index), new { menuId = model.MenuId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("MENU", "DELETE")]
        public async Task<IActionResult> DeleteItem(int menuItemId, int menuId)
        {
            bool success = await _menuService.DeleteMenuItemAsync(menuItemId);
            if (success)
            {
                TempData["SuccessMessage"] = "Đã xóa nút Menu và các nhánh con liên quan!";
            }
            else
            {
                TempData["ErrorMessage"] = "Lỗi khi xóa nút Menu.";
            }

            return RedirectToAction(nameof(Index), new { menuId = menuId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("MENU", "EVICT_CACHE")]
        public async Task<IActionResult> EvictCache(string menuCode, int menuId)
        {
            await _menuService.InvalidateMenuCacheAsync(menuCode);
            TempData["SuccessMessage"] = $"Đã xóa sạch bộ đệm Cache của Menu '{menuCode}'. Website Storefront đã được đồng bộ!";
            return RedirectToAction(nameof(Index), new { menuId = menuId });
        }

        [HttpGet]
        public async Task<IActionResult> GetTargetsByType(int targetType)
        {
            return targetType switch
            {
                1 => Json(await _context.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.CategoryName)
                    .Select(c => new TargetLookupItemDto { Id = c.CategoryId.ToString(), Name = c.CategoryName, Slug = c.Slug })
                    .ToListAsync()),

                2 => Json(await _context.Products
                    .Where(p => p.Status == 1)
                    .OrderBy(p => p.ProductName)
                    .Take(100)
                    .Select(p => new TargetLookupItemDto { Id = p.ProductId, Name = $"{p.ProductName} ({p.ProductId})", Slug = p.Slug })
                    .ToListAsync()),

                3 => Json(await _context.Posts
                    .Where(p => p.Status == 3)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(50)
                    .Select(p => new TargetLookupItemDto { Id = p.PostId.ToString(), Name = p.Title, Slug = p.Slug })
                    .ToListAsync()),

                4 => Json(await _context.StaticPages
                    .OrderBy(sp => sp.Title)
                    .Select(sp => new TargetLookupItemDto { Id = sp.PageId.ToString(), Name = sp.Title, Slug = sp.Slug })
                    .ToListAsync()),

                _ => Json(new object[] { })
            };
        }
    }
}