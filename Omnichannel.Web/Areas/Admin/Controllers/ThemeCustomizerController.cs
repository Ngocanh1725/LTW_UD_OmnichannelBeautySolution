using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Cms;
using Omnichannel.Application.Interfaces.Cms;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Route("admin/customizer")]
    public class ThemeCustomizerController : Controller
    {
        private readonly ILayoutEngineService _layoutEngineService;
        private readonly ILogger<ThemeCustomizerController> _logger;

        public ThemeCustomizerController(
            ILayoutEngineService layoutEngineService,
            ILogger<ThemeCustomizerController> logger)
        {
            _layoutEngineService = layoutEngineService ?? throw new ArgumentNullException(nameof(layoutEngineService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index([FromQuery] string pageKey = "home_page")
        {
            try
            {
                var layout = await _layoutEngineService.GetLayoutAsync(pageKey, isDraft: true);
                ViewBag.PageKey = pageKey;
                return View(layout);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi nạp giao diện Theme Customizer cho page {PageKey}", pageKey);
                TempData["ErrorMessage"] = "Không thể tải cấu hình giao diện. Vui lòng thử lại.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        [HttpPost("save-draft")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDraft([FromBody] SaveDraftLayoutRequest request)
        {
            if (request == null)
            {
                return Json(new { success = false, message = "Dữ liệu bản nháp không hợp lệ." });
            }

            try
            {
                var pageKey = !string.IsNullOrEmpty(request.PageKey) ? request.PageKey : "home_page";
                var ok = await _layoutEngineService.SaveDraftLayoutAsync(pageKey, request);
                return Json(new { success = ok, message = ok ? "Đã lưu bản nháp giao diện." : "Không thể lưu bản nháp." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu bản nháp giao diện");
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpPost("publish")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish([FromForm] string pageKey = "home_page")
        {
            try
            {
                var ok = await _layoutEngineService.PublishLayoutAsync(pageKey);
                return Json(new
                {
                    success = ok,
                    message = ok ? "Xuất bản thành công! Giao diện mới đã được áp dụng toàn hệ thống." : "Xuất bản thất bại."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xuất bản giao diện cho {PageKey}", pageKey);
                return Json(new { success = false, message = $"Lỗi khi xuất bản: {ex.Message}" });
            }
        }
    }
}

