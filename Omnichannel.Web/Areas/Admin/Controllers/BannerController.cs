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

using Microsoft.Extensions.Caching.Memory;
using Omnichannel.Web.ViewComponents;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class BannerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache? _cache;

        public BannerController(ApplicationDbContext context, IMemoryCache? cache = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _cache = cache;
        }

        private static int MapPositionStringToInt(string? position)
        {
            return position switch
            {
                "HERO_HOME" or "1" => 1,
                "POPUP" or "2" => 2,
                "CATEGORY_TOP" or "3" => 3,
                "FOOTER" or "4" => 4,
                _ => 1
            };
        }

        private static string MapPositionIntToString(int position)
        {
            return position switch
            {
                1 => "HERO_HOME",
                2 => "POPUP",
                3 => "CATEGORY_TOP",
                4 => "FOOTER",
                _ => "HERO_HOME"
            };
        }

        [HttpGet]
        [HasPermission("CMS_BANNER_MANAGE")]
        public async Task<IActionResult> Index()
        {
            var banners = await _context.Banners
                .AsNoTracking()
                .OrderBy(b => b.Position)
                .ThenBy(b => b.DisplayOrder)
                .Select(b => new BannerItemViewModel
                {
                    BannerId = b.Id,
                    Title = b.Title,
                    Position = MapPositionStringToInt(b.Position),
                    DesktopImageUrl = !string.IsNullOrEmpty(b.DesktopImageUrl) ? b.DesktopImageUrl : b.ImageUrl,
                    MobileImageUrl = b.MobileImageUrl,
                    LinkUrl = b.LinkUrl,
                    DisplayOrder = b.DisplayOrder,
                    ValidFrom = b.ValidFrom ?? DateTime.UtcNow,
                    ValidTo = b.ValidTo ?? DateTime.UtcNow.AddMonths(1),
                    TrackingClickCount = b.TrackingClickCount,
                    IsActive = b.IsActive
                })
                .ToListAsync();

            return View(banners);
        }

        [HttpGet]
        [HasPermission("CMS_BANNER_MANAGE")]
        public IActionResult Create()
        {
            return View(new CreateBannerViewModel
            {
                ValidFrom = DateTime.UtcNow,
                ValidTo = DateTime.UtcNow.AddMonths(1),
                DisplayOrder = 1
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("CMS_BANNER_MANAGE")]
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

            string desktopUrl = model.DesktopMediaId.HasValue ? $"/media/{model.DesktopMediaId}" : (model.DesktopImageUrl?.Trim() ?? string.Empty);
            string? mobileUrl = model.MobileMediaId.HasValue ? $"/media/{model.MobileMediaId}" : model.MobileImageUrl?.Trim();

            var banner = new Banner
            {
                Title = model.Title.Trim(),
                Position = MapPositionIntToString(model.Position),
                ImageUrl = desktopUrl,
                DesktopImageUrl = desktopUrl,
                DesktopMediaId = model.DesktopMediaId,
                MobileImageUrl = mobileUrl,
                MobileMediaId = model.MobileMediaId,
                LinkUrl = model.LinkUrl?.Trim(),
                DisplayOrder = model.DisplayOrder,
                SortOrder = model.DisplayOrder,
                ValidFrom = model.ValidFrom,
                ValidTo = model.ValidTo,
                IsActive = true,
                TrackingClickCount = 0
            };

            await _context.Banners.AddAsync(banner);
            await _context.SaveChangesAsync();
            InvalidateBannerCache();

            TempData["SuccessMessage"] = $"Đã tạo mới Banner '{banner.Title}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [HasPermission("CMS_BANNER_MANAGE")]
        public async Task<IActionResult> Edit(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null) return NotFound();

            var vm = new EditBannerViewModel
            {
                BannerId = banner.Id,
                Title = banner.Title,
                Position = MapPositionStringToInt(banner.Position),
                DesktopImageUrl = !string.IsNullOrEmpty(banner.DesktopImageUrl) ? banner.DesktopImageUrl : banner.ImageUrl,
                DesktopMediaId = banner.DesktopMediaId,
                MobileImageUrl = banner.MobileImageUrl,
                MobileMediaId = banner.MobileMediaId,
                LinkUrl = banner.LinkUrl,
                DisplayOrder = banner.DisplayOrder,
                ValidFrom = banner.ValidFrom ?? DateTime.UtcNow,
                ValidTo = banner.ValidTo ?? DateTime.UtcNow.AddMonths(1),
                IsActive = banner.IsActive
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("CMS_BANNER_MANAGE")]
        public async Task<IActionResult> Edit(EditBannerViewModel model)
        {
            if (model.ValidTo < model.ValidFrom)
            {
                ModelState.AddModelError("ValidTo", "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var banner = await _context.Banners.FindAsync(model.BannerId);
            if (banner == null) return NotFound();

            banner.Title = model.Title.Trim();
            banner.Position = MapPositionIntToString(model.Position);
            if (model.DesktopMediaId.HasValue)
            {
                banner.DesktopMediaId = model.DesktopMediaId;
                banner.DesktopImageUrl = $"/media/{model.DesktopMediaId}";
                banner.ImageUrl = $"/media/{model.DesktopMediaId}";
            }
            else if (!string.IsNullOrWhiteSpace(model.DesktopImageUrl))
            {
                banner.ImageUrl = model.DesktopImageUrl.Trim();
                banner.DesktopImageUrl = model.DesktopImageUrl.Trim();
            }

            if (model.MobileMediaId.HasValue)
            {
                banner.MobileMediaId = model.MobileMediaId;
                banner.MobileImageUrl = $"/media/{model.MobileMediaId}";
            }
            else if (!string.IsNullOrWhiteSpace(model.MobileImageUrl))
            {
                banner.MobileImageUrl = model.MobileImageUrl.Trim();
            }

            banner.LinkUrl = model.LinkUrl?.Trim();
            banner.DisplayOrder = model.DisplayOrder;
            banner.SortOrder = model.DisplayOrder;
            banner.ValidFrom = model.ValidFrom;
            banner.ValidTo = model.ValidTo;
            banner.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            InvalidateBannerCache();

            TempData["SuccessMessage"] = $"Đã cập nhật Banner '{banner.Title}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("CMS_BANNER_MANAGE")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null) return NotFound();

            banner.IsActive = !banner.IsActive;
            await _context.SaveChangesAsync();
            InvalidateBannerCache();

            TempData["SuccessMessage"] = banner.IsActive
                ? $"Đã bật hiển thị Banner '{banner.Title}'."
                : $"Đã tạm ẩn Banner '{banner.Title}'.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("CMS_BANNER_MANAGE")]
        public async Task<IActionResult> Delete(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null) return NotFound();

            _context.Banners.Remove(banner);
            await _context.SaveChangesAsync();
            InvalidateBannerCache();

            TempData["SuccessMessage"] = "Đã gỡ bỏ Banner khỏi hệ thống!";
            return RedirectToAction(nameof(Index));
        }

        private void InvalidateBannerCache()
        {
            _cache?.Remove(HomeSectionsViewComponent.CompositeModelCacheKey);
            _cache?.Remove(HomeSectionsViewComponent.CacheKey);
            _cache?.Remove("home_hero_banners");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("CMS_BANNER_MANAGE")]
        public async Task<IActionResult> UploadImage(Microsoft.AspNetCore.Http.IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "Vui lòng chọn tệp ảnh hợp lệ." });
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var ext = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                return BadRequest(new { success = false, message = "Định dạng không được hỗ trợ (chỉ nhận JPG, PNG, WEBP, GIF)." });
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { success = false, message = "Dung lượng ảnh tối đa 5MB." });
            }

            var uploadsFolder = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "uploads", "banners");
            if (!System.IO.Directory.Exists(uploadsFolder))
            {
                System.IO.Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid():N}_{System.IO.Path.GetFileName(file.FileName)}";
            var filePath = System.IO.Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var url = $"/uploads/banners/{uniqueFileName}";
            return Json(new { success = true, url });
        }
    }
}
