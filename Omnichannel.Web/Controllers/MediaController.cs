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
        [HttpGet("GetImage/{id:int}")]
        [ResponseCache(Duration = 31536000, Location = ResponseCacheLocation.Any)] // Cache 1 năm
        public async Task<IActionResult> GetMedia(int id)
        {
            try
            {
                var media = await _mediaService.GetMediaStreamAsync(id);
                if (media == null || media.Data == null || media.Data.Length == 0)
                {
                    return Redirect("/images/product-placeholder.svg");
                }

                var hash = !string.IsNullOrWhiteSpace(media.ChecksumHash)
                    ? media.ChecksumHash
                    : Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(media.Data)).ToLowerInvariant();

                // Kiểm tra HTTP Header If-None-Match gửi lên từ Browser
                if (Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var clientEtag) &&
                    clientEtag.ToString().Contains(hash))
                {
                    return StatusCode(304); // Not Modified: Client dùng cache sẵn có, tiết kiệm 100% băng thông
                }

                var etag = new EntityTagHeaderValue($"\"{hash}\"");
                Response.Headers[HeaderNames.ETag] = etag.ToString();
                Response.Headers[HeaderNames.CacheControl] = "public, max-age=31536000, immutable";

                var lastModifiedUtc = media.LastModified.Kind == DateTimeKind.Utc
                    ? media.LastModified
                    : DateTime.SpecifyKind(media.LastModified, DateTimeKind.Utc);
                Response.Headers[HeaderNames.LastModified] = lastModifiedUtc.ToString("R");

                var contentType = !string.IsNullOrWhiteSpace(media.ContentType)
                    ? media.ContentType
                    : "image/jpeg";

                return File(media.Data, contentType, enableRangeProcessing: true);
            }
            catch
            {
                return Redirect("/images/product-placeholder.svg");
            }
        }

        [HttpGet("view/{id:int}")]
        public Task<IActionResult> ViewMedia(int id) => GetMedia(id);

        [HttpGet("placeholder")]
        public IActionResult GetPlaceholder() => Redirect("/images/product-placeholder.svg");
    }
}
