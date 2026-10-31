using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omnichannel.Application.DTOs.Catalog;
using Omnichannel.Application.Interfaces.Catalog;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class ProductAttributeController : Controller
    {
        private readonly ISmartFilterService _smartFilterService;

        public ProductAttributeController(ISmartFilterService smartFilterService)
        {
            _smartFilterService = smartFilterService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, int? categoryId, int? skinTypeId, int? concernId)
        {
            if (!User.IsInRole("SuperAdmin") && !User.IsInRole("SUPER_ADMIN") && 
                !User.HasClaim("Permission", "PRODUCT_VIEW") &&
                !User.HasClaim("Permission", "PRODUCT_MANAGE"))
            {
                return Forbid();
            }

            var viewModel = await _smartFilterService.GetProductAttributesOverviewAsync(search, categoryId, skinTypeId, concernId);
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.SkinTypeId = skinTypeId;
            ViewBag.ConcernId = concernId;

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSingle(int productId, List<int> skinTypeIds, List<int> concernIds)
        {
            if (!User.IsInRole("SuperAdmin") && !User.IsInRole("SUPER_ADMIN") && 
                !User.HasClaim("Permission", "PRODUCT_MANAGE"))
            {
                return Forbid();
            }

            var success = await _smartFilterService.UpdateProductAttributesAsync(productId, skinTypeIds, concernIds);
            if (success)
            {
                TempData["SuccessMessage"] = "Đã cập nhật thuộc tính sản phẩm thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm để cập nhật.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUpdate(BulkUpdateAttributesDto dto)
        {
            if (!User.IsInRole("SuperAdmin") && !User.IsInRole("SUPER_ADMIN") && 
                !User.HasClaim("Permission", "PRODUCT_MANAGE"))
            {
                return Forbid();
            }

            if (dto == null || dto.ProductIds == null || dto.ProductIds.Count == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một sản phẩm để cập nhật hàng loạt.";
                return RedirectToAction(nameof(Index));
            }

            var updatedCount = await _smartFilterService.BulkUpdateAttributesAsync(dto);
            TempData["SuccessMessage"] = $"Đã cập nhật thành công thuộc tính cho {updatedCount} sản phẩm được chọn!";

            return RedirectToAction(nameof(Index));
        }
    }
}
