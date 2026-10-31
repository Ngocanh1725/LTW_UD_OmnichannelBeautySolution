using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Omnichannel.Application.DTOs.Settings;
using Omnichannel.Application.Interfaces.Settings;
using Omnichannel.Infrastructure.Security;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly ISettingsService _settingsService;

        public SettingsController(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        [HttpGet]
        [HasPermission("SETTINGS_VIEW")]
        public async Task<IActionResult> Index(string group = "GENERAL", CancellationToken cancellationToken = default)
        {
            var allSettings = await _settingsService.GetAllSettingsAsync(cancellationToken);

            var vm = new SettingsGroupViewModel
            {
                ActiveGroup = !string.IsNullOrWhiteSpace(group) ? group.ToUpperInvariant() : "GENERAL",
                GeneralSettings = allSettings.Where(s => s.SettingGroup == "GENERAL").ToList(),
                SmtpSettings = allSettings.Where(s => s.SettingGroup == "SMTP").ToList(),
                PaymentSettings = allSettings.Where(s => s.SettingGroup == "PAYMENT").ToList(),
                ShippingSettings = allSettings.Where(s => s.SettingGroup == "SHIPPING").ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("SETTINGS_MANAGE")]
        public async Task<IActionResult> SaveGroup(string group, IFormCollection form, CancellationToken cancellationToken = default)
        {
            var dict = new Dictionary<string, string>();
            int? userId = GetCurrentUserId();

            foreach (var key in form.Keys)
            {
                if (key == "__RequestVerificationToken" || key == "group" || key == "ActiveGroup")
                {
                    continue;
                }

                dict[key] = form[key].ToString();
            }

            await _settingsService.UpdateBatchAsync(dict, userId, cancellationToken);
            TempData["SuccessMessage"] = $"Đã cập nhật thành công cấu hình nhóm '{group.ToUpperInvariant()}'!";

            return RedirectToAction(nameof(Index), new { group });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("SETTINGS_MANAGE")]
        public async Task<IActionResult> Create(string key, string value, string group, string dataType, string? description, bool isEncrypted, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                TempData["ErrorMessage"] = "Mã cấu hình (Key) không được để trống!";
                return RedirectToAction(nameof(Index), new { group });
            }

            int? userId = GetCurrentUserId();
            bool success = await _settingsService.SetAsync(
                key: key.Trim().ToUpperInvariant(),
                value: value ?? string.Empty,
                group: !string.IsNullOrWhiteSpace(group) ? group.Trim().ToUpperInvariant() : "GENERAL",
                dataType: !string.IsNullOrWhiteSpace(dataType) ? dataType.Trim().ToUpperInvariant() : "STRING",
                description: description?.Trim(),
                isEncrypted: isEncrypted,
                userId: userId,
                cancellationToken: cancellationToken);

            if (success)
            {
                TempData["SuccessMessage"] = $"Đã thêm mới cấu hình '{key.Trim().ToUpperInvariant()}' thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể thêm cấu hình. Vui lòng thử lại.";
            }

            return RedirectToAction(nameof(Index), new { group });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("SETTINGS_MANAGE")]
        public async Task<IActionResult> ClearCache(string? group = "GENERAL", CancellationToken cancellationToken = default)
        {
            await _settingsService.InvalidateCacheAsync(null, cancellationToken);
            TempData["SuccessMessage"] = "Đã làm mới sạch toàn bộ bộ đệm Cache Cài đặt hệ thống!";
            return RedirectToAction(nameof(Index), new { group });
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claim, out int uid))
            {
                return uid;
            }
            return null;
        }
    }
}
