using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Catalog;
using Omnichannel.Application.Interfaces.Catalog;

namespace Omnichannel.Web.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductApiController : ControllerBase
    {
        private readonly IRecommendationService _recommendationService;
        private readonly ILogger<ProductApiController> _logger;

        public ProductApiController(
            IRecommendationService recommendationService,
            ILogger<ProductApiController> logger)
        {
            _recommendationService = recommendationService ?? throw new ArgumentNullException(nameof(recommendationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Lấy thông tin rút gọn danh sách sản phẩm theo IDs (phục vụ Recently Viewed Products)
        /// GET /api/products/by-ids?ids=1,2,3
        /// </summary>
        [HttpGet("by-ids")]
        public async Task<IActionResult> GetByIds([FromQuery] string? ids, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ids))
                {
                    return Ok(new List<RecentlyViewedProductDto>());
                }

                var parsedIds = ids.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s.Trim(), out int id) ? id : 0)
                    .Where(id => id > 0)
                    .Distinct()
                    .Take(12)
                    .ToList();

                if (!parsedIds.Any())
                {
                    return Ok(new List<RecentlyViewedProductDto>());
                }

                var products = await _recommendationService.GetProductsByIdsAsync(parsedIds, cancellationToken);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách sản phẩm theo IDs: {Ids}", ids);
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách sản phẩm", error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy combo Thường được mua cùng (Frequently Bought Together) cho PDP
        /// GET /api/products/{id}/frequently-bought-together?top=2
        /// </summary>
        [HttpGet("{id:int}/frequently-bought-together")]
        public async Task<IActionResult> GetFrequentlyBoughtTogether(int id, [FromQuery] int top = 2, CancellationToken cancellationToken = default)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "Mã sản phẩm không hợp lệ." });
                }

                top = Math.Clamp(top, 1, 4);
                var bundle = await _recommendationService.GetFrequentlyBoughtTogetherAsync(id, top, cancellationToken);
                if (bundle == null)
                {
                    return NotFound(new { message = "Không tìm thấy gợi ý combo cho sản phẩm này." });
                }

                return Ok(bundle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy combo FBT cho sản phẩm Id={Id}", id);
                return StatusCode(500, new { message = "Lỗi khi phân tích gợi ý combo", error = ex.Message });
            }
        }
    }
}

