using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Catalog;
using Omnichannel.Application.Interfaces.Catalog;

namespace Omnichannel.Web.Controllers
{
    [ApiController]
    [Route("api/search")]
    public class SearchApiController : ControllerBase
    {
        private readonly ISearchSuggestService _searchSuggestService;
        private readonly ILogger<SearchApiController> _logger;

        public SearchApiController(
            ISearchSuggestService searchSuggestService,
            ILogger<SearchApiController> logger)
        {
            _searchSuggestService = searchSuggestService;
            _logger = logger;
        }

        /// <summary>
        /// API Gợi ý tìm kiếm tức thì theo chuẩn Sephora / Hasaki
        /// GET /api/search/suggest?q=...
        /// </summary>
        [HttpGet("suggest")]
        public async Task<IActionResult> Suggest(
            [FromQuery] string? q,
            [FromQuery] int maxProducts = 6,
            [FromQuery] int maxBrands = 4,
            [FromQuery] int maxCategories = 4,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Giới hạn độ dài và làm sạch truy vấn để chống XSS và injection
                string cleanQuery = SanitizeQuery(q);

                maxProducts = Math.Clamp(maxProducts, 1, 12);
                maxBrands = Math.Clamp(maxBrands, 1, 8);
                maxCategories = Math.Clamp(maxCategories, 1, 8);

                var suggestions = await _searchSuggestService.GetSuggestionsAsync(
                    cleanQuery,
                    maxProducts: maxProducts,
                    maxBrands: maxBrands,
                    maxCategories: maxCategories,
                    cancellationToken: cancellationToken);

                return Ok(suggestions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xử lý API gợi ý tìm kiếm với q='{Query}'", q);
                return StatusCode(500, new { message = "Lỗi khi xử lý tìm kiếm gợi ý", error = ex.Message });
            }
        }

        private static string SanitizeQuery(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var trimmed = input.Trim();

            // Cắt bớt nếu vượt quá 50 ký tự
            if (trimmed.Length > 50)
            {
                trimmed = trimmed.Substring(0, 50);
            }

            // Loại bỏ thẻ HTML / Script tags để chống XSS
            trimmed = Regex.Replace(trimmed, @"<[^>]*>", string.Empty);

            // Loại bỏ các ký tự nguy hại
            trimmed = trimmed.Replace("<", string.Empty).Replace(">", string.Empty);

            return trimmed.Trim();
        }
    }
}

