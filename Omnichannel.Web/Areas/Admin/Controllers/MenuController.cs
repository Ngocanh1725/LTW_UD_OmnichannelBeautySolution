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
    [Route("Admin/[controller]")]
    [Route("Admin/MenuManagement")]
    [Route("Admin/MenuManagement/Index")]
    [Authorize]
    public class MenuController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMenuEngineService _menuService;
//Hiển thị trang quản trị cây menu
        public MenuController(ApplicationDbContext context, IMenuEngineService menuService)
        {
            _context = context;
            _menuService = menuService;
        }

        [HttpGet]
        [Route("")]
        [Route("Index")]

        public async Task<IActionResult> Index(int? menuId = null)
        {
            var menus = await _context.Menus
                .AsNoTracking()
                .OrderBy(m => m.Position)
                .Select(m => new MenuLookupDto
                {
                    MenuId = m.Id,
                    MenuCode = m.Code,
                    MenuName = m.Title,
                    Position = m.Position == "TOP" ? 1 : 2
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
        [HasPermission("CMS_MENU_MANAGE")]
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
        [HasPermission("CMS_MENU_MANAGE")]
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
        [HasPermission("CMS_MENU_MANAGE")]
        public async Task<IActionResult> EditItem(EditMenuItemViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu sửa nút menu chưa hợp lệ.";
                return RedirectToAction(nameof(Index), new { menuId = model.MenuId });
            }

            bool success = await _menuService.UpdateMenuItemAsync(model);
            if (success)
            {
                TempData["SuccessMessage"] = $"Đã cập nhật nút Menu '{model.Title}' thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể cập nhật nút Menu.";
            }

            return RedirectToAction(nameof(Index), new { menuId = model.MenuId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("CMS_MENU_MANAGE")]
        public async Task<IActionResult> Reorder([FromBody] ReorderMenuRequest request)
        {
            if (request == null || request.MenuId <= 0 || !request.Items.Any())
            {
                return Json(new { success = false, message = "Dữ liệu sắp xếp không hợp lệ." });
            }

            bool success = await _menuService.ReorderMenuItemsAsync(request);
            return Json(new { success, message = success ? "Đã lưu thứ tự menu mới!" : "Lỗi khi lưu thứ tự menu." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("CMS_MENU_MANAGE")]
        public async Task<IActionResult> EvictCache(string menuCode, int menuId)
        {
            await _menuService.InvalidateMenuCacheAsync(menuCode);
            TempData["SuccessMessage"] = $"Đã xóa sạch bộ đệm Cache của Menu '{menuCode}'. Website Storefront đã được đồng bộ!";
            return RedirectToAction(nameof(Index), new { menuId = menuId });
        }

        [HttpGet]
        [Route("GetTargetsByType")]

        public async Task<IActionResult> GetTargetsByType(int targetType)
        {
            return targetType switch
            {
                1 => Json(await _context.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .Select(c => new TargetLookupItemDto { Id = c.Id.ToString(), Name = c.Name, Slug = c.Slug })
                    .ToListAsync()),

                2 => Json(await _context.Products
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Name)
                    .Take(100)
                    .Select(p => new TargetLookupItemDto { Id = p.Id.ToString(), Name = $"{p.Name} ({p.Id})", Slug = p.Slug })
                    .ToListAsync()),

                3 => Json(await _context.Posts
                    .Where(p => p.IsPublished)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(50)
                    .Select(p => new TargetLookupItemDto { Id = p.Id.ToString(), Name = p.Title, Slug = p.Slug })
                    .ToListAsync()),

                4 => Json(await _context.StaticPages
                    .OrderBy(sp => sp.Title)
                    .Select(sp => new TargetLookupItemDto { Id = sp.Id.ToString(), Name = sp.Title, Slug = sp.Slug })
                    .ToListAsync()),

                _ => Json(new object[] { })
            };
        }
    }
}