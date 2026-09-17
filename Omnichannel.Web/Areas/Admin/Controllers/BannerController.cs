using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Cms;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Security;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class BannerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BannerController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [HasPermission("BANNER", "VIEW")]
        public async Task<IActionResult> Index()
        {
            var banners = await _context.Banners
                .AsNoTracking()
                .OrderBy(b => b.Position)
                .ThenBy(b => b.DisplayOrder)
                .Select(b => new BannerItemViewModel
                {
                    BannerId = b.BannerId,
                    Title = b.Title,
                    Position = b.Position,
                    DesktopImageUrl = b.DesktopImageUrl,
                    MobileImageUrl = b.MobileImageUrl,
                    LinkUrl = b.LinkUrl,
                    DisplayOrder = b.DisplayOrder,
                    ValidFrom = b.ValidFrom,
                    ValidTo = b.ValidTo,
                    TrackingClickCount = b.TrackingClickCount,
                    IsActive = b.IsActive
                })
                .ToListAsync();

            return View(banners);
        }

        [HttpGet]
        [HasPermission("BANNER", "CREATE")]
        public IActionResult Create()
        {
            return View(new CreateBannerViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("BANNER", "CREATE")]
        public async Task<IActionResult> Create(CreateBannerViewModel model)
        {
            if (model.ValidTo < model.ValidFrom)
            {
                ModelState.AddModelError("ValidTo", "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var banner = new Banner
            {
                Title = model.Title.Trim(),
                Position = model.Position,
                DesktopImageUrl = model.DesktopImageUrl.Trim(),
                MobileImageUrl = model.MobileImageUrl?.Trim(),
                LinkUrl = model.LinkUrl?.Trim(),
                DisplayOrder = model.DisplayOrder,
                ValidFrom = model.ValidFrom,
                ValidTo = model.ValidTo,
                IsActive = true,
                TrackingClickCount = 0
            };

            await _context.Banners.AddAsync(banner);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã tạo mới Banner '{banner.Title}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("BANNER", "EDIT")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null) return NotFound();

            banner.IsActive = !banner.IsActive;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = banner.IsActive
                ? $"Đã bật hiển thị Banner '{banner.Title}'."
                : $"Đã tạm ẩn Banner '{banner.Title}'.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("BANNER", "DELETE")]
        public async Task<IActionResult> Delete(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null) return NotFound();

            _context.Banners.Remove(banner);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã gỡ bỏ Banner khỏi hệ thống!";
            return RedirectToAction(nameof(Index));
        }
    }
}
