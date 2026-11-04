using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Omnichannel.Application.DTOs.Catalog;
using Omnichannel.Application.Interfaces.Catalog;

namespace Omnichannel.Web.Controllers
{
    public class CompareController : Controller
    {
        private readonly IProductComparisonService _comparisonService;

        public CompareController(IProductComparisonService comparisonService)
        {
            _comparisonService = comparisonService;
        }

        /// <summary>
        /// Trang Ma Trận So Sánh Mỹ Phẩm Đầy Đủ Đối Chiếu Side-by-Side (GAP-03)
        /// URL: /so-sanh hoặc /Compare?ids=1,6,15
        /// </summary>
        [HttpGet]
        [Route("so-sanh")]
        [Route("so-sanh-my-pham")]
        public async Task<IActionResult> Index(string? ids, CancellationToken ct = default)
        {
            var parsedIds = ParseProductIds(ids);

            var matrix = await _comparisonService.GetDetailedComparisonAsync(parsedIds, ct);

            // Nếu người dùng mới chọn 1 sản phẩm, nạp thêm 4 gợi ý cùng loại để họ dễ dàng thêm vào so sánh
            if (matrix.TotalProducts == 1)
            {
                ViewBag.SuggestedAlternatives = await _comparisonService.GetSuggestedAlternativesAsync(matrix.Products[0].Id, 4, ct);
            }
            else if (matrix.TotalProducts == 0)
            {
                // Fallback: Gợi ý các sản phẩm nổi bật
                ViewBag.SuggestedAlternatives = await _comparisonService.GetSuggestedAlternativesAsync(1, 4, ct);
            }

            return View(matrix);
        }

        /// <summary>
        /// API trả về danh sách tóm tắt các sản phẩm trong Khay so sánh nổi (Drawer)
        /// URL: GET /api/compare/quick-items?ids=1,2,3
        /// </summary>
        [HttpGet]
        [Route("api/compare/quick-items")]
        public async Task<IActionResult> GetQuickItems([FromQuery] string? ids, CancellationToken ct = default)
        {
            var parsedIds = ParseProductIds(ids);
            if (!parsedIds.Any())
            {
                return Ok(new List<CompareItemSummaryDto>());
            }

            var items = await _comparisonService.GetCompareQuickItemsAsync(parsedIds, ct);
            return Ok(items);
        }

        /// <summary>
        /// API trả về toàn bộ dữ liệu ma trận so sánh chi tiết dưới dạng JSON
        /// URL: GET /api/compare/matrix?ids=1,2,3
        /// </summary>
        [HttpGet]
        [Route("api/compare/matrix")]
        public async Task<IActionResult> GetMatrixJson([FromQuery] string? ids, CancellationToken ct = default)
        {
            var parsedIds = ParseProductIds(ids);
            var matrix = await _comparisonService.GetDetailedComparisonAsync(parsedIds, ct);
            return Ok(matrix);
        }

        /// <summary>
        /// API trả về các sản phẩm thay thế cùng loại để gợi ý người dùng bổ sung vào so sánh
        /// URL: GET /api/compare/suggested?productId=1
        /// </summary>
        [HttpGet]
        [Route("api/compare/suggested")]
        public async Task<IActionResult> GetSuggested([FromQuery] int productId, [FromQuery] int limit = 4, CancellationToken ct = default)
        {
            if (productId <= 0) return Ok(new List<CompareItemSummaryDto>());
            var items = await _comparisonService.GetSuggestedAlternativesAsync(productId, limit, ct);
            return Ok(items);
        }

        /// <summary>
        /// Partial View trả về nội dung bảng so sánh để hiển thị trực tiếp trong Quick Compare Modal
        /// URL: GET /compare/modal-partial?ids=1,2,3
        /// </summary>
        [HttpGet]
        [Route("compare/modal-partial")]
        public async Task<IActionResult> GetModalPartial([FromQuery] string? ids, CancellationToken ct = default)
        {
            var parsedIds = ParseProductIds(ids);
            var matrix = await _comparisonService.GetDetailedComparisonAsync(parsedIds, ct);
            return PartialView("_CompareMatrixModalContent", matrix);
        }

        private static List<int> ParseProductIds(string? ids)
        {
            if (string.IsNullOrWhiteSpace(ids)) return new List<int>();

            return ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                      .Select(s => int.TryParse(s.Trim(), out int val) ? val : 0)
                      .Where(val => val > 0)
                      .Distinct()
                      .Take(3)
                      .ToList();
        }
    }
}
