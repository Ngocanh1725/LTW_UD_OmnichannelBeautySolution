using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omnichannel.Application.DTOs.Notifications;
using Omnichannel.Application.Interfaces.Notifications;
using Omnichannel.Web.Extensions;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        [Route("Admin/Notifications")]
        [Route("Admin/Notification")]
        [Route("Admin/Notifications/Index")]
        public async Task<IActionResult> Index([FromQuery] NotificationFilterDto? filter)
        {
            filter ??= new NotificationFilterDto();
            if (string.IsNullOrEmpty(filter.Tab)) filter.Tab = "ALL";
            if (filter.PageNumber <= 0) filter.PageNumber = 1;
            if (filter.PageSize <= 0) filter.PageSize = 10;

            int? userId = User.GetUserId();
            string? role = User.FindFirst(ClaimTypes.Role)?.Value;
            int? storeId = User.GetStoreId();

            var result = await _notificationService.GetNotificationsAsync(filter, userId, role, storeId);
            ViewBag.Filter = filter;

            return View(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/Notifications/MarkAsRead/{id:int}")]
        public async Task<IActionResult> MarkAsRead(int id, string? returnUrl = null)
        {
            int? userId = User.GetUserId();
            await _notificationService.MarkAsReadAsync(id, userId);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/Notifications/MarkAllAsRead")]
        public async Task<IActionResult> MarkAllAsRead(string? returnUrl = null)
        {
            int? userId = User.GetUserId();
            string? role = User.FindFirst(ClaimTypes.Role)?.Value;
            int? storeId = User.GetStoreId();

            var count = await _notificationService.MarkAllAsReadAsync(userId, role, storeId);
            TempData["SuccessMessage"] = $"Đã đánh dấu {count} thông báo là đã đọc.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/Notifications/Delete/{id:int}")]
        public async Task<IActionResult> Delete(int id, string? returnUrl = null)
        {
            await _notificationService.DeleteNotificationAsync(id);
            TempData["SuccessMessage"] = "Đã xóa thông báo thành công.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/Notifications/ClearAllRead")]
        public async Task<IActionResult> ClearAllRead(string? returnUrl = null)
        {
            int? userId = User.GetUserId();
            string? role = User.FindFirst(ClaimTypes.Role)?.Value;
            int? storeId = User.GetStoreId();

            var count = await _notificationService.ClearAllReadAsync(userId, role, storeId);
            TempData["SuccessMessage"] = $"Đã dọn dẹp {count} thông báo đã đọc.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
