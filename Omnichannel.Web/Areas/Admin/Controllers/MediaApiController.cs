using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Omnichannel.Infrastructure.Services.Cms;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Route("admin/api/media")]
    public class MediaApiController : Controller
    {
        private readonly MediaStorageService _mediaStorageService;
        private readonly ILogger<MediaApiController> _logger;

        public MediaApiController(
            MediaStorageService mediaStorageService,
            ILogger<MediaApiController> logger)
        {
            _mediaStorageService = mediaStorageService ?? throw new ArgumentNullException(nameof(mediaStorageService));
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
        public async Task<IActionResult> Upload(IFormFile file)
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

                var asset = await _mediaStorageService.SaveUploadedFileAsync(file, staffId);
                return Json(new
                {
                    success = true,
                    id = asset.Id,
                    fileName = asset.FileName,
                    publicUrl = !string.IsNullOrEmpty(asset.PublicUrl) ? asset.PublicUrl : $"/media/{asset.Id}",
                    message = "Tải ảnh thành công."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải lên file ảnh {FileName}", file.FileName);
                return Json(new { success = false, message = $"Lỗi khi tải ảnh: {ex.Message}" });
            }
        }
    }
}

