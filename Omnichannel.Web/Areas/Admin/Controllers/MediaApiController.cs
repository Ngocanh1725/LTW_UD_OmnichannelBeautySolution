using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Services.Cms;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Route("admin/api/media")]
    public class MediaApiController : Controller
    {
        private readonly MediaStorageService _mediaStorageService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MediaApiController> _logger;

        public MediaApiController(
            MediaStorageService mediaStorageService,
            ApplicationDbContext context,
            ILogger<MediaApiController> logger)
        {
            _mediaStorageService = mediaStorageService ?? throw new ArgumentNullException(nameof(mediaStorageService));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetList([FromQuery] string? search, [FromQuery] string? category, [FromQuery] int page = 1, [FromQuery] int pageSize = 24)
        {
            try
            {
                var (items, total) = await _mediaStorageService.GetPagedMediaAsync(search, category, page, pageSize);
                return Json(new
                {
                    success = true,
                    total,
                    page,
                    pageSize,
                    items = items.Select(m => new
                    {
                        id = m.Id,
                        fileName = m.FileName,
                        publicUrl = !string.IsNullOrEmpty(m.PublicUrl) ? m.PublicUrl : $"/media/{m.Id}",
                        category = m.Category,
                        fileSize = m.FileSize,
                        createdAt = m.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách Media");
                return Json(new { success = false, message = "Lỗi máy chủ khi lấy danh sách ảnh." });
            }
        }

        [HttpPost("upload")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file, [FromForm] string? category)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn file ảnh để tải lên." });
            }

            try
            {
                int? staffId = null;
                var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(claim, out var parsedId)) staffId = parsedId;

                var asset = await _mediaStorageService.SaveUploadedFileAsync(file, staffId, category);
                return Json(new
                {
                    success = true,
                    id = asset.Id,
                    fileName = asset.FileName,
                    publicUrl = !string.IsNullOrEmpty(asset.PublicUrl) ? asset.PublicUrl : $"/media/{asset.Id}",
                    category = asset.Category,
                    message = "Tải ảnh thành công."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải lên file ảnh {FileName}", file.FileName);
                return Json(new { success = false, message = $"Lỗi khi tải ảnh: {ex.Message}" });
            }
        }

        [HttpPost("delete/{id:int}")]
        [HttpDelete("{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var asset = await _context.MediaAssets.FindAsync(id);
                if (asset == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy ảnh cần xóa." });
                }

                // Gỡ bỏ liên kết an toàn khỏi các bảng tham chiếu
                var banners = await _context.Banners.Where(b => b.DesktopMediaId == id || b.MobileMediaId == id).ToListAsync();
                foreach (var b in banners)
                {
                    if (b.DesktopMediaId == id) b.DesktopMediaId = null;
                    if (b.MobileMediaId == id) b.MobileMediaId = null;
                }

                var products = await _context.Products.Where(p => p.ThumbnailMediaId == id).ToListAsync();
                foreach (var p in products)
                {
                    p.ThumbnailMediaId = null;
                }

                var uiSections = await _context.UiSections.Where(s => s.BackgroundMediaId == id).ToListAsync();
                foreach (var s in uiSections)
                {
                    s.BackgroundMediaId = null;
                }

                var sectionItems = await _context.SectionItems.Where(si => si.MediaAssetId == id).ToListAsync();
                foreach (var si in sectionItems)
                {
                    si.MediaAssetId = null;
                }

                // Xóa tệp vật lý nếu nằm trong thư mục uploads
                if (!string.IsNullOrEmpty(asset.PublicUrl) && asset.PublicUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
                {
                    var cleanPath = asset.PublicUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "Omnichannel.Web", "wwwroot", cleanPath);
                    if (System.IO.File.Exists(fullPath))
                    {
                        try { System.IO.File.Delete(fullPath); } catch { }
                    }
                }

                _context.MediaAssets.Remove(asset);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Đã xóa MediaAsset {Id}: {FileName}", id, asset.FileName);

                return Json(new
                {
                    success = true,
                    message = $"Đã xóa ảnh '{asset.FileName}' khỏi thư viện thành công."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa MediaAsset {Id}", id);
                return Json(new { success = false, message = $"Lỗi khi xóa ảnh: {ex.Message}" });
            }
        }
    }
}
