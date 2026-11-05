using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Omnichannel.Application.DTOs.Cms;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Security;
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

        private static string ResolveBannerDisplayUrl(Banner b)
        {
            if (!string.IsNullOrWhiteSpace(b.DesktopImageUrl))
                return b.DesktopImageUrl;
            if (b.DesktopMediaId.HasValue)
                return $"/media/{b.DesktopMediaId.Value}";
            if (!string.IsNullOrWhiteSpace(b.ImageUrl))
                return b.ImageUrl;
            return "/images/banners/hero-banner-summer-beauty.jpg";
        }

        [HttpGet]
        [HasPermission("CMS_BANNER_MANAGE")]
        public async Task<IActionResult> Index()
        {
            var rawBanners = await _context.Banners
                .AsNoTracking()
                .OrderBy(b => b.Position)
                .ThenBy(b => b.DisplayOrder)
                .ToListAsync();

            var banners = rawBanners.Select(b => new BannerItemViewModel
            {
                BannerId = b.Id,
                Title = b.Title,
                Subtitle = b.Subtitle,
                ButtonText = b.ButtonText,
                Position = MapPositionStringToInt(b.Position),
                DesktopImageUrl = ResolveBannerDisplayUrl(b),
                MobileImageUrl = !string.IsNullOrWhiteSpace(b.MobileImageUrl)
                    ? b.MobileImageUrl
                    : (b.MobileMediaId.HasValue ? $"/media/{b.MobileMediaId.Value}" : null),
                LinkUrl = b.LinkUrl,
                DisplayOrder = b.DisplayOrder,
                ValidFrom = b.ValidFrom ?? DateTime.UtcNow,
                ValidTo = b.ValidTo ?? DateTime.UtcNow.AddMonths(1),
                TrackingClickCount = b.TrackingClickCount,
                IsActive = b.IsActive
            }).ToList();

            return View(banners);
        }

        [HttpGet]
        [HasPermission("CMS_BANNER_MANAGE")]
        public IActionResult Create()
        {
            return View(new CreateBannerViewModel
            {
                ValidFrom = DateTime.UtcNow,
                ValidTo = DateTime.UtcNow.AddMonths(3),
                DisplayOrder = 1,
                ButtonText = "Khám Phá Ngay",
                DesktopImageUrl = "/images/banners/hero-banner-summer-beauty.jpg"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("CMS_BANNER_MANAGE")]
        public async Task<IActionResult> Create(
            CreateBannerViewModel model,
            IFormFile? desktopFile = null,
            IFormFile? mobileFile = null)
        {
            if (model.ValidTo < model.ValidFrom)
            {
                ModelState.AddModelError("ValidTo", "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string desktopUrl = model.DesktopImageUrl?.Trim() ?? string.Empty;
            int? desktopMediaId = model.DesktopMediaId;

            if (desktopFile != null && desktopFile.Length > 0)
            {
                var (savedUrl, savedMediaId) = await SaveUploadedBannerFileAsync(desktopFile);
                if (!string.IsNullOrEmpty(savedUrl))
                {
                    desktopUrl = savedUrl;
                    desktopMediaId = savedMediaId;
                }
            }
            else if (!string.IsNullOrWhiteSpace(desktopUrl) && (!desktopMediaId.HasValue || desktopUrl != $"/media/{desktopMediaId.Value}"))
            {
                desktopMediaId = null;
            }
            else if (desktopMediaId.HasValue)
            {
                desktopUrl = $"/media/{desktopMediaId.Value}";
            }

            if (string.IsNullOrWhiteSpace(desktopUrl))
            {
                desktopUrl = "/images/banners/hero-banner-summer-beauty.jpg";
            }

            string? mobileUrl = model.MobileImageUrl?.Trim();
            int? mobileMediaId = model.MobileMediaId;

            if (mobileFile != null && mobileFile.Length > 0)
            {
                var (savedMobUrl, savedMobId) = await SaveUploadedBannerFileAsync(mobileFile);
                if (!string.IsNullOrEmpty(savedMobUrl))
                {
                    mobileUrl = savedMobUrl;
                    mobileMediaId = savedMobId;
                }
            }
            else if (!string.IsNullOrWhiteSpace(mobileUrl) && (!mobileMediaId.HasValue || mobileUrl != $"/media/{mobileMediaId.Value}"))
            {
                mobileMediaId = null;
            }
            else if (mobileMediaId.HasValue)
            {
                mobileUrl = $"/media/{mobileMediaId.Value}";
            }

            var banner = new Banner
            {
                Title = model.Title.Trim(),
                Subtitle = string.IsNullOrWhiteSpace(model.Subtitle) ? null : model.Subtitle.Trim(),
                ButtonText = string.IsNullOrWhiteSpace(model.ButtonText) ? "Khám Phá Ngay" : model.ButtonText.Trim(),
                Position = MapPositionIntToString(model.Position),
                ImageUrl = desktopUrl,
                DesktopImageUrl = desktopUrl,
                DesktopMediaId = desktopMediaId,
                MobileImageUrl = mobileUrl,
                MobileMediaId = mobileMediaId,
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
                Subtitle = banner.Subtitle,
                ButtonText = banner.ButtonText ?? "Khám Phá Ngay",
                Position = MapPositionStringToInt(banner.Position),
                DesktopImageUrl = ResolveBannerDisplayUrl(banner),
                DesktopMediaId = banner.DesktopMediaId,
                MobileImageUrl = !string.IsNullOrWhiteSpace(banner.MobileImageUrl)
                    ? banner.MobileImageUrl
                    : (banner.MobileMediaId.HasValue ? $"/media/{banner.MobileMediaId.Value}" : null),
                MobileMediaId = banner.MobileMediaId,
                LinkUrl = banner.LinkUrl,
                DisplayOrder = banner.DisplayOrder,
                ValidFrom = banner.ValidFrom ?? DateTime.UtcNow,
                ValidTo = banner.ValidTo ?? DateTime.UtcNow.AddMonths(3),
                IsActive = banner.IsActive
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("CMS_BANNER_MANAGE")]
        public async Task<IActionResult> Edit(
            EditBannerViewModel model,
            IFormFile? desktopFile = null,
            IFormFile? mobileFile = null)
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
            banner.Subtitle = string.IsNullOrWhiteSpace(model.Subtitle) ? null : model.Subtitle.Trim();
            banner.ButtonText = string.IsNullOrWhiteSpace(model.ButtonText) ? "Khám Phá Ngay" : model.ButtonText.Trim();
            banner.Position = MapPositionIntToString(model.Position);

            // Xử lý ảnh Desktop: ưu tiên file tải lên trực tiếp -> URL nhập tay -> MediaId từ thư viện
            if (desktopFile != null && desktopFile.Length > 0)
            {
                var (savedUrl, savedMediaId) = await SaveUploadedBannerFileAsync(desktopFile);
                if (!string.IsNullOrEmpty(savedUrl))
                {
                    banner.DesktopImageUrl = savedUrl;
                    banner.ImageUrl = savedUrl;
                    banner.DesktopMediaId = savedMediaId;
                }
            }
            else if (!string.IsNullOrWhiteSpace(model.DesktopImageUrl))
            {
                var trimmedDesktop = model.DesktopImageUrl.Trim();
                banner.DesktopImageUrl = trimmedDesktop;
                banner.ImageUrl = trimmedDesktop;

                if (model.DesktopMediaId.HasValue && trimmedDesktop == $"/media/{model.DesktopMediaId.Value}")
                {
                    banner.DesktopMediaId = model.DesktopMediaId;
                }
                else
                {
                    banner.DesktopMediaId = null;
                }
            }
            else if (model.DesktopMediaId.HasValue)
            {
                banner.DesktopMediaId = model.DesktopMediaId;
                banner.DesktopImageUrl = $"/media/{model.DesktopMediaId.Value}";
                banner.ImageUrl = $"/media/{model.DesktopMediaId.Value}";
            }

            // Xử lý ảnh Mobile
            if (mobileFile != null && mobileFile.Length > 0)
            {
                var (savedMobUrl, savedMobId) = await SaveUploadedBannerFileAsync(mobileFile);
                if (!string.IsNullOrEmpty(savedMobUrl))
                {
                    banner.MobileImageUrl = savedMobUrl;
                    banner.MobileMediaId = savedMobId;
                }
            }
            else if (!string.IsNullOrWhiteSpace(model.MobileImageUrl))
            {
                var trimmedMob = model.MobileImageUrl.Trim();
                banner.MobileImageUrl = trimmedMob;
                if (model.MobileMediaId.HasValue && trimmedMob == $"/media/{model.MobileMediaId.Value}")
                {
                    banner.MobileMediaId = model.MobileMediaId;
                }
                else
                {
                    banner.MobileMediaId = null;
                }
            }
            else if (model.MobileMediaId.HasValue)
            {
                banner.MobileMediaId = model.MobileMediaId;
                banner.MobileImageUrl = $"/media/{model.MobileMediaId.Value}";
            }
            else
            {
                banner.MobileImageUrl = null;
                banner.MobileMediaId = null;
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
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "Vui lòng chọn tệp ảnh hợp lệ." });
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                return BadRequest(new { success = false, message = "Định dạng không được hỗ trợ (chỉ nhận JPG, PNG, WEBP, GIF, SVG)." });
            }

            if (file.Length > 10 * 1024 * 1024)
            {
                return BadRequest(new { success = false, message = "Dung lượng ảnh tối đa 10MB." });
            }

            var (url, mediaId) = await SaveUploadedBannerFileAsync(file);
            InvalidateBannerCache();

            return Json(new { success = true, url, mediaId });
        }

        private async Task<(string Url, int? MediaId)> SaveUploadedBannerFileAsync(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg" };
            if (!allowedExtensions.Contains(ext))
            {
                return (string.Empty, null);
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "banners");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var safeOriginalName = Path.GetFileNameWithoutExtension(file.FileName);
            safeOriginalName = System.Text.RegularExpressions.Regex.Replace(safeOriginalName, @"[^a-zA-Z0-9_\-]", "-");
            var uniqueFileName = $"banner_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..6]}_{safeOriginalName}{ext}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                bytes = ms.ToArray();
            }

            await System.IO.File.WriteAllBytesAsync(filePath, bytes);
            var publicUrl = $"/uploads/banners/{uniqueFileName}";

            int? savedMediaId = null;
            try
            {
                var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
                var mediaAsset = new MediaAsset
                {
                    FileName = uniqueFileName,
                    ContentType = file.ContentType ?? "image/jpeg",
                    MimeType = file.ContentType ?? "image/jpeg",
                    Data = bytes,
                    FileSize = bytes.Length,
                    FileSizeBytes = bytes.Length,
                    ChecksumHash = hash,
                    Category = "BANNER",
                    PublicUrl = publicUrl,
                    StoragePath = publicUrl,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.MediaAssets.AddAsync(mediaAsset);
                await _context.SaveChangesAsync();
                savedMediaId = mediaAsset.Id;
            }
            catch
            {
                // Nếu lưu MediaAssets gặp lỗi (ví dụ InMemory DB trong Unit Test), vẫn trả về URL file tĩnh bình thường
            }

            return (publicUrl, savedMediaId);
        }
    }
}
