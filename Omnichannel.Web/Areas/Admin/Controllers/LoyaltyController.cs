using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omnichannel.Application.DTOs.Loyalty;
using Omnichannel.Application.Interfaces.Loyalty;
using Omnichannel.Application.Interfaces.Security;
using Omnichannel.Infrastructure.Security;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class LoyaltyController : Controller
    {
        private readonly ILoyaltyService _loyaltyService;
        private readonly IAuditLogService? _auditLogService;

        public LoyaltyController(ILoyaltyService loyaltyService, IAuditLogService? auditLogService = null)
        {
            _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
            _auditLogService = auditLogService;
        }

        [HttpGet]
        [HasPermission("LOYALTY_VIEW")]
        public async Task<IActionResult> Index(string? search, int? tierId, CancellationToken cancellationToken)
        {
            var summary = await _loyaltyService.GetLoyaltySummaryAsync(cancellationToken);
            var wallets = await _loyaltyService.GetAllWalletsAsync(search, tierId, cancellationToken);
            var tiers = await _loyaltyService.GetAllTiersAsync(cancellationToken);

            ViewBag.Summary = summary;
            ViewBag.Tiers = tiers;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentTierId = tierId;

            return View(wallets);
        }

        [HttpGet]
        [HasPermission("LOYALTY_VIEW")]
        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
        {
            var wallet = await _loyaltyService.GetWalletDetailsAsync(id, cancellationToken);
            if (wallet == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hồ sơ ví điểm thành viên.";
                return RedirectToAction(nameof(Index));
            }

            var tiers = await _loyaltyService.GetAllTiersAsync(cancellationToken);
            ViewBag.Tiers = tiers;

            return View(wallet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("LOYALTY_ADJUST_POINTS")]
        public async Task<IActionResult> AdjustPoints([FromForm] AdjustPointsRequest request, string? returnUrl, CancellationToken cancellationToken)
        {
            if (request.UserId <= 0 || request.Points == 0 || string.IsNullOrWhiteSpace(request.Reason))
            {
                TempData["ErrorMessage"] = "Thông tin điều chỉnh điểm không hợp lệ (số điểm khác 0 và phải có lý do).";
                return RedirectBackOrIndex(returnUrl);
            }

            try
            {
                var success = await _loyaltyService.AdjustPointsAsync(request, cancellationToken);
                if (success)
                {
                    string sign = request.Points > 0 ? $"+{request.Points:N0}" : $"{request.Points:N0}";
                    TempData["SuccessMessage"] = $"Đã điều chỉnh thành công {sign} điểm cho khách hàng #{request.UserId}!";

                    if (_auditLogService != null)
                    {
                        var adminName = User.Identity?.Name ?? "Admin";
                        var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                        int.TryParse(adminIdStr, out var adminId);

                        await _auditLogService.LogAsync(
                            action: "LOYALTY_MANUAL_POINT_ADJUSTMENT",
                            entityType: "CustomerWallet",
                            entityId: request.UserId.ToString(),
                            oldValues: null,
                            newValues: $"Points: {sign}, Reason: {request.Reason}",
                            userId: adminId > 0 ? adminId : null,
                            userName: adminName,
                            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                            userAgent: Request.Headers["User-Agent"].ToString(),
                            cancellationToken: cancellationToken
                        );
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể điều chỉnh điểm. Vui lòng thử lại.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi điều chỉnh điểm: {ex.Message}";
            }

            return RedirectBackOrIndex(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("LOYALTY_TIER_MANAGE")]
        public async Task<IActionResult> UpdateTier([FromForm] TierInfoDto model, CancellationToken cancellationToken)
        {
            if (model.Id <= 0 || string.IsNullOrWhiteSpace(model.TierName))
            {
                TempData["ErrorMessage"] = "Tên hạng thành viên không được để trống.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var success = await _loyaltyService.UpdateTierAsync(model, cancellationToken);
                if (success)
                {
                    TempData["SuccessMessage"] = $"Cập nhật chính sách hạng '{model.TierName}' thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy hạng thành viên để cập nhật.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi cập nhật hạng: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        private IActionResult RedirectBackOrIndex(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
