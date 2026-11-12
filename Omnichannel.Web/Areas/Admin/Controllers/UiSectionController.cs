using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Cms;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Security;
using Omnichannel.Web.ViewComponents;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [HasPermission("THEME_MANAGE")]
    [Route("Admin/[controller]")]
    [Route("admin/ui-section")]
    public class UiSectionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<UiSectionController> _logger;

        public UiSectionController(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<UiSectionController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: /Admin/UiSection or /admin/ui-section
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var sections = await _context.UiSections
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();

            if (!sections.Any())
            {
                // Tự động khởi tạo 6 khối chuẩn nếu CSDL chưa có
                sections = new List<UiSection>
                {
                    new() { SectionCode = "HERO_SLIDER", Title = "Banner Slider", Subtitle = "Bộ sưu tập mỹ phẩm dẫn đầu xu hướng làm đẹp 2026", DisplayOrder = 1, IsVisible = true, LayoutType = "CAROUSEL" },
                    new() { SectionCode = "FEATURED_CATEGORIES", Title = "Danh Mục Nổi Bật", Subtitle = "Khám phá vẻ đẹp thuần khiết theo từng phân nhóm da liễu", DisplayOrder = 2, IsVisible = true, LayoutType = "GRID_6" },
                    new() { SectionCode = "FLASH_SALE", Title = "Deal Chớp Nhoáng (Flash Sale)", Subtitle = "Cơ hội săn mỹ phẩm cao cấp cam kết date mới với giá ưu đãi cực lớn", DisplayOrder = 3, IsVisible = true, LayoutType = "GRID_4" },
                    new() { SectionCode = "BEST_SELLERS", Title = "Sản Phẩm Bán Chạy", Subtitle = "Top mỹ phẩm được các tín đồ làm đẹp tin chọn nhiều nhất", DisplayOrder = 4, IsVisible = true, LayoutType = "GRID_4" },
                    new() { SectionCode = "MID_BANNER", Title = "Banner Giữa Trang", Subtitle = "Chiến dịch ưu đãi nổi bật giữa trang", DisplayOrder = 5, IsVisible = true, LayoutType = "BANNER_FULL" },
                    new() { SectionCode = "BEAUTY_BLOG", Title = "Bài Viết Làm Đẹp", Subtitle = "Cẩm nang làm đẹp khoa học và xu hướng mỹ phẩm mới nhất", DisplayOrder = 6, IsVisible = true, LayoutType = "GRID_3" }
                };

                await _context.UiSections.AddRangeAsync(sections);
                await _context.SaveChangesAsync();
                _cache.Remove(HomeSectionsViewComponent.CacheKey);
                _cache.Remove(HomeSectionsViewComponent.CompositeModelCacheKey);
            }

            return View(sections);
        }

        // GET: /admin/ui-section/detail/{id}
        [HttpGet("detail/{id:int}")]
        public async Task<IActionResult> Detail(int id)
        {
            var section = await _context.UiSections.FindAsync(id);
            if (section == null)
            {
                return Json(new { success = false, message = "Không tìm thấy khối giao diện." });
            }

            UiSectionConfigDto config = new();
            if (!string.IsNullOrWhiteSpace(section.ConfigurationJson))
            {
                try
                {
                    config = JsonSerializer.Deserialize<UiSectionConfigDto>(section.ConfigurationJson) ?? new();
                }
                catch
                {
                    config = new();
                }
            }

            return Json(new
            {
                success = true,
                data = new UiSectionDetailDto
                {
                    Id = section.Id,
                    SectionCode = section.SectionCode,
                    Title = section.Title,
                    Subtitle = section.Subtitle,
                    DisplayOrder = section.DisplayOrder,
                    IsVisible = section.IsVisible,
                    LayoutType = section.LayoutType,
                    Config = config
                }
            });
        }

        // POST: /admin/ui-section/save
        [HttpPost("save")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save([FromForm] UiSectionSaveRequest model, [FromForm] IFormFile? imageFile)
        {
            if (model.Id <= 0)
            {
                return Json(new { success = false, message = "Mã khối không hợp lệ." });
            }

            var section = await _context.UiSections.FindAsync(model.Id);
            if (section == null)
            {
                return Json(new { success = false, message = "Không tìm thấy khối giao diện cần cập nhật." });
            }

            if (string.IsNullOrWhiteSpace(model.Title))
            {
                return Json(new { success = false, message = "Tiêu đề khối không được để trống." });
            }

            // Xử lý upload file ảnh nếu người dùng chọn từ máy tính
            string? targetImageUrl = model.ImageUrl;
            if (imageFile != null && imageFile.Length > 0)
            {
                var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp", ".svg" };
                var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!allowedExts.Contains(ext))
                {
                    return Json(new { success = false, message = "Định dạng file không hợp lệ (hỗ trợ JPG, PNG, WEBP, SVG)." });
                }

                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "Dung lượng ảnh tối đa là 5MB." });
                }

                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "Omnichannel.Web", "wwwroot", "uploads", "cms");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                var fileName = $"uisection_{model.Id}_{Guid.NewGuid():N}{ext}";
                var fullPath = Path.Combine(uploadsDir, fileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
                targetImageUrl = $"/uploads/cms/{fileName}";
            }

            // Đọc cấu hình hiện tại
            UiSectionConfigDto config = new();
            if (!string.IsNullOrWhiteSpace(section.ConfigurationJson))
            {
                try
                {
                    config = JsonSerializer.Deserialize<UiSectionConfigDto>(section.ConfigurationJson) ?? new();
                }
                catch
                {
                    config = new();
                }
            }

            // Cập nhật các trường cấu hình
            config.ImageUrl = targetImageUrl;
            config.MediaAssetId = model.MediaAssetId ?? config.MediaAssetId;
            config.Height = !string.IsNullOrWhiteSpace(model.Height) ? model.Height : (config.Height ?? "md");
            config.ImageFit = !string.IsNullOrWhiteSpace(model.ImageFit) ? model.ImageFit : (config.ImageFit ?? "cover");
            config.ImagePosition = !string.IsNullOrWhiteSpace(model.ImagePosition) ? model.ImagePosition : (config.ImagePosition ?? "center");
            config.ItemCount = model.ItemCount.HasValue && model.ItemCount.Value > 0 ? model.ItemCount.Value : config.ItemCount;
            config.LinkUrl = model.LinkUrl;
            config.ButtonText = model.ButtonText;
            config.BadgeText = model.BadgeText;
            config.BackgroundColor = model.BackgroundColor;

            // Cập nhật thực thể
            section.Title = model.Title.Trim();
            section.Subtitle = model.Subtitle?.Trim();
            section.LayoutType = !string.IsNullOrWhiteSpace(model.LayoutType) ? model.LayoutType : section.LayoutType;
            section.IsVisible = model.IsVisible;
            if (model.DisplayOrder > 0)
            {
                section.DisplayOrder = model.DisplayOrder;
            }
            section.ConfigurationJson = JsonSerializer.Serialize(config);

            await _context.SaveChangesAsync();

            // Xóa cache để làm mới trang chủ tức thì
            _cache.Remove(HomeSectionsViewComponent.CacheKey);
            _cache.Remove(HomeSectionsViewComponent.CompositeModelCacheKey);
            _logger.LogInformation("Khối giao diện '{SectionCode}' (ID: {Id}) đã được cập nhật thành công.", section.SectionCode, section.Id);

            return Json(new
            {
                success = true,
                message = $"Đã cập nhật khối '{section.Title}' thành công!",
                data = new
                {
                    id = section.Id,
                    title = section.Title,
                    subtitle = section.Subtitle,
                    layoutType = section.LayoutType,
                    isVisible = section.IsVisible,
                    displayOrder = section.DisplayOrder,
                    imageUrl = config.ImageUrl,
                    height = config.Height,
                    itemCount = config.ItemCount
                }
            });
        }

        // POST: /Admin/UiSection/ToggleVisibility or /admin/ui-section/toggle-visibility
        [HttpPost("ToggleVisibility")]
        [HttpPost("toggle-visibility")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisibility([FromBody] ToggleVisibilityModel? bodyModel, [FromForm] int? id, [FromForm] bool? isVisible)
        {
            int targetId = bodyModel?.Id ?? id ?? 0;
            bool targetVisible = bodyModel?.IsVisible ?? isVisible ?? false;

            if (targetId <= 0)
            {
                return Json(new { success = false, message = "Mã định danh khối không hợp lệ." });
            }

            var section = await _context.UiSections.FindAsync(targetId);
            if (section == null)
            {
                return Json(new { success = false, message = "Không tìm thấy khối giao diện yêu cầu." });
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                section.IsVisible = targetVisible;
                await _context.SaveChangesAsync();

                // Xóa cache để Storefront làm mới tức thì
                _cache.Remove(HomeSectionsViewComponent.CacheKey);
                _cache.Remove(HomeSectionsViewComponent.CompositeModelCacheKey);
                _logger.LogInformation("Khối giao diện '{SectionCode}' (ID: {Id}) đã được cập nhật hiển thị: {IsVisible}", section.SectionCode, section.Id, section.IsVisible);

                return Json(new
                {
                    success = true,
                    id = section.Id,
                    sectionCode = section.SectionCode,
                    isVisible = section.IsVisible,
                    message = section.IsVisible ? $"Đã bật hiển thị khối '{section.Title}'." : $"Đã ẩn khối '{section.Title}' khỏi Storefront."
                });
            });
        }

        // POST: /admin/ui-section/show-all
        [HttpPost("show-all")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShowAll()
        {
            var sections = await _context.UiSections.ToListAsync();
            foreach (var s in sections)
            {
                s.IsVisible = true;
            }
            await _context.SaveChangesAsync();

            _cache.Remove(HomeSectionsViewComponent.CacheKey);
            _cache.Remove(HomeSectionsViewComponent.CompositeModelCacheKey);

            return Json(new
            {
                success = true,
                message = "Đã bật hiển thị lại tất cả các khối trên Storefront thành công!"
            });
        }

        // POST: /Admin/UiSection/UpdateOrder or /admin/ui-section/update-order
        [HttpPost("UpdateOrder")]
        [HttpPost("update-order")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrder([FromBody] UpdateOrderModel? bodyModel, [FromForm] List<int>? orderedIds)
        {
            var ids = bodyModel?.OrderedIds ?? orderedIds ?? new List<int>();

            if (ids == null || !ids.Any())
            {
                return Json(new { success = false, message = "Danh sách thứ tự khối rỗng." });
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                var allSections = await _context.UiSections.ToListAsync();
                var sectionMap = allSections.ToDictionary(s => s.Id);

                for (int i = 0; i < ids.Count; i++)
                {
                    var sectionId = ids[i];
                    if (sectionMap.TryGetValue(sectionId, out var section))
                    {
                        section.DisplayOrder = i + 1;
                    }
                }

                await _context.SaveChangesAsync();

                // Xóa cache để Storefront làm mới tức thì
                _cache.Remove(HomeSectionsViewComponent.CacheKey);
                _cache.Remove(HomeSectionsViewComponent.CompositeModelCacheKey);
                _logger.LogInformation("Đã cập nhật lại thứ tự hiển thị cho {Count} khối giao diện Storefront.", ids.Count);

                return Json(new
                {
                    success = true,
                    message = "Cập nhật thứ tự bố cục trang chủ thành công! Storefront đã được cập nhật tức thì."
                });
            });
        }
    }

    public class ToggleVisibilityModel
    {
        public int Id { get; set; }
        public bool IsVisible { get; set; }
    }

    public class UpdateOrderModel
    {
        public List<int> OrderedIds { get; set; } = new();
    }
}
