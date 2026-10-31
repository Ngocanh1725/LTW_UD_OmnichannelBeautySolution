using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Catalog;
using Omnichannel.Application.Interfaces.Catalog;

namespace Omnichannel.Web.Controllers
{
    [ApiController]
    [Route("api/products/smart-filter")]
    public class SmartFilterApiController : ControllerBase
    {
        private readonly ISmartFilterService _smartFilterService;
        private readonly ILogger<SmartFilterApiController> _logger;

        public SmartFilterApiController(
            ISmartFilterService smartFilterService,
            ILogger<SmartFilterApiController> logger)
        {
            _smartFilterService = smartFilterService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Filter(
            [FromQuery] string? skinType,
            [FromQuery] string? concerns,
            [FromQuery] decimal? budget,
            [FromQuery] string? budgetBracket,
            [FromQuery] int limit = 12)
        {
            try
            {
                var concernList = new List<string>();
                if (!string.IsNullOrWhiteSpace(concerns))
                {
                    concernList = concerns.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                }

                var criteria = new SmartFilterCriteriaDto
                {
                    SkinType = skinType,
                    Concerns = concernList,
                    Budget = budget,
                    BudgetBracket = budgetBracket,
                    Limit = limit > 0 ? limit : 12
                };

                var results = await _smartFilterService.GetSmartRecommendationsAsync(criteria);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình gợi ý sản phẩm thông minh SmartFilter.");
                return StatusCode(500, new { success = false, message = "Lỗi khi xử lý tìm kiếm sản phẩm thông minh." });
            }
        }

        [HttpGet("metadata")]
        public async Task<IActionResult> GetMetadata()
        {
            try
            {
                var skinTypes = await _smartFilterService.GetAvailableSkinTypesAsync();
                var concerns = await _smartFilterService.GetAvailableSkinConcernsAsync();

                return Ok(new
                {
                    success = true,
                    skinTypes,
                    concerns
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy metadata SmartFilter.");
                return StatusCode(500, new { success = false, message = "Lỗi khi tải metadata." });
            }
        }
    }
}
