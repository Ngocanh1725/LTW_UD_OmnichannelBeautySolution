using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Omnichannel.Application.DTOs.Media;
using Omnichannel.Application.Interfaces.Media;
using Omnichannel.Infrastructure.Security;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Route("admin/media")]
    public class MediaController : Controller
    {
        private readonly IMediaService _mediaService;

        public MediaController(IMediaService mediaService)
        {
            _mediaService = mediaService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index([FromQuery] MediaFilterRequest filter)
        {
            var (items, totalCount) = await _mediaService.GetPagedListAsync(filter);
            ViewBag.Filter = filter;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize);
            return View(items);
        }

        [HttpGet("api/list")]
        public async Task<IActionResult> GetMediaApi([FromQuery] MediaFilterRequest filter)
        {
            var (items, totalCount) = await _mediaService.GetPagedListAsync(filter);
            return Json(new { success = true, items, totalCount });
        }

        [HttpPost("api/upload")]
        public async Task<IActionResult> UploadApi([FromForm] List<IFormFile> files, [FromForm] string category = "PRODUCT")
        {
            if (files == null || files.Count == 0)
                return Json(new { success = false, message = "Vui lòng chọn ít nhất một file ảnh." });

            var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (int?)null;
            var uploaded = await _mediaService.UploadBatchAsync(files, category, userId);

            return Json(new { success = true, count = uploaded.Count, items = uploaded });
        }

        [HttpPost("api/delete/{id:int}")]
        public async Task<IActionResult> DeleteApi(int id)
        {
            var result = await _mediaService.DeleteAsync(id);
            return Json(new { success = result });
        }

        [HttpPost("api/update-alt/{id:int}")]
        public async Task<IActionResult> UpdateAltApi(int id, [FromBody] string altText)
        {
            var result = await _mediaService.UpdateAltTextAsync(id, altText);
            return Json(new { success = result });
        }
    }
}
