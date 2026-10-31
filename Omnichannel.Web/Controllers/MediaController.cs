using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Omnichannel.Application.Interfaces.Media;

namespace Omnichannel.Web.Controllers
{
    [ApiController]
    [Route("media")]
    public class MediaController : ControllerBase
    {
        private readonly IMediaService _mediaService;

        public MediaController(IMediaService mediaService)
        {
            _mediaService = mediaService;
        }

        [HttpGet("{id:int}")]
        [ResponseCache(Duration = 31536000, Location = ResponseCacheLocation.Any)] // Cache 1 năm
        public async Task<IActionResult> GetMedia(int id)
        {
            var media = await _mediaService.GetMediaStreamAsync(id);
            if (media == null || media.Data.Length == 0)
            {
                return Redirect("~/images/product-placeholder.svg");
            }

            var etag = new EntityTagHeaderValue($"\"{media.ChecksumHash}\"");

            // Kiểm tra HTTP Header If-None-Match gửi lên từ Browser
            if (Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var clientEtag) &&
                clientEtag.ToString().Contains(media.ChecksumHash))
            {
                return StatusCode(304); // Not Modified: Client dùng cache sẵn có, tiết kiệm 100% băng thông
            }

            Response.Headers[HeaderNames.ETag] = etag.ToString();
            Response.Headers[HeaderNames.CacheControl] = "public, max-age=31536000, immutable";
            Response.Headers[HeaderNames.LastModified] = media.LastModified.ToString("R");

            return File(media.Data, media.ContentType, enableRangeProcessing: true);
        }

        [HttpGet("view/{id:int}")]
        public Task<IActionResult> ViewMedia(int id) => GetMedia(id);

        [HttpGet("placeholder")]
        public IActionResult GetPlaceholder() => Redirect("~/images/product-placeholder.svg");
    }
}
