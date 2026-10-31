using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omnichannel.Application.DTOs.Cms;
using Omnichannel.Application.Interfaces.Cms;
using Omnichannel.Infrastructure.Security;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [HasPermission("THEME_MANAGE")]
    public class ThemeConfigController : Controller
    {
        private readonly IThemeService _themeService;

        public ThemeConfigController(IThemeService themeService)
        {
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var theme = await _themeService.GetCurrentThemeAsync();
            return View(theme);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ThemeConfigDto model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu nhập vào chưa hợp lệ, vui lòng kiểm tra lại.";
                return View(model);
            }

            var success = await _themeService.UpdateThemeAsync(model);
            if (success)
            {
                TempData["SuccessMessage"] = "Cập nhật cấu hình Theme và bảng màu thành công! Giao diện Storefront đã được làm mới tức thì.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Có lỗi xảy ra trong quá trình lưu cấu hình Theme vào CSDL.";
            return View(model);
        }
    }
}
