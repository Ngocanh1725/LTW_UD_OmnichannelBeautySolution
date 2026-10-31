using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omnichannel.Application.DTOs.Alerts;
using Omnichannel.Application.Interfaces.Alerts;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class AlertController : Controller
    {
        private readonly IAlertService _alertService;

        public AlertController(IAlertService alertService)
        {
            _alertService = alertService;
        }

        [HttpGet]
        [Route("Admin/Alerts")]
        [Route("Admin/Alert")]
        [Route("Admin/Alerts/Index")]
        public async Task<IActionResult> Index([FromQuery] AlertFilterDto? filter)
        {
            filter ??= new AlertFilterDto();
            if (filter.PageNumber <= 0) filter.PageNumber = 1;
            if (filter.PageSize <= 0) filter.PageSize = 10;

            var result = await _alertService.GetAlertsAsync(filter);
            ViewBag.Filter = filter;
            return View(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/Alerts/MarkAsRead/{id:int}")]
        public async Task<IActionResult> MarkAsRead(int id, string? returnUrl = null)
        {
            await _alertService.MarkAsReadAsync(id);
            TempData["SuccessMessage"] = "Đã đánh dấu cảnh báo là đã đọc.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/Alerts/MarkAllAsRead")]
        public async Task<IActionResult> MarkAllAsRead(string? returnUrl = null)
        {
            var count = await _alertService.MarkAllAsReadAsync();
            TempData["SuccessMessage"] = $"Đã đánh dấu {count} cảnh báo là đã đọc.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/Alerts/Delete/{id:int}")]
        public async Task<IActionResult> Delete(int id, string? returnUrl = null)
        {
            await _alertService.DeleteAlertAsync(id);
            TempData["SuccessMessage"] = "Đã xóa cảnh báo thành công.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/Alerts/ClearAllRead")]
        public async Task<IActionResult> ClearAllRead(string? returnUrl = null)
        {
            var count = await _alertService.ClearAllReadAlertsAsync();
            TempData["SuccessMessage"] = $"Đã dọn dẹp {count} cảnh báo đã đọc.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/Alerts/TriggerCheck")]
        public async Task<IActionResult> TriggerCheck(string? returnUrl = null)
        {
            var newCount = await _alertService.CheckAndGenerateAlertsAsync();
            TempData["SuccessMessage"] = newCount > 0
                ? $"AI đã quét xong và phát hiện {newCount} cảnh báo mới!"
                : "AI đã quét xong. Không có cảnh báo mới nào phát sinh!";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
