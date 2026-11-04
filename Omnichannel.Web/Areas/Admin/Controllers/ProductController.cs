using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Inventory;
using Omnichannel.Application.Interfaces.Catalog;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Security;
using Omnichannel.Web.Extensions;
using Microsoft.AspNetCore.SignalR;
using Omnichannel.Web.Hubs;

using Microsoft.Extensions.Caching.Memory;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<SystemHub> _hubContext;
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache? _cache;

        public ProductController(
            IProductService productService, 
            ApplicationDbContext context, 
            IHubContext<SystemHub> hubContext,
            IWebHostEnvironment env,
            IMemoryCache? cache = null)
        {
            _productService = productService;
            _context = context;
            _hubContext = hubContext;
            _env = env;
            _cache = cache;
        }

        [HttpGet]
        [HasPermission("PRODUCT_VIEW")]
        public async Task<IActionResult> Index([FromQuery] ProductFilterRequest filter)
        {
            filter ??= new ProductFilterRequest();
            var pagedResult = await _productService.GetProductsPagedAsync(filter);

            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Categories = new SelectList(categories, "Id", "Name", filter.CategoryId);
            ViewBag.CurrentFilter = filter;

            if (Request.IsHtmx())
            {
                return PartialView("_ProductList", pagedResult);
            }

            return View(pagedResult);
        }

        [HttpGet]
        [HasPermission("PRODUCT_VIEW")]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _productService.GetProductDetailAsync(id);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm yêu cầu.";
                return RedirectToAction(nameof(Index));
            }

            return View(product);
        }

        [HttpGet]
        [HasPermission("PRODUCT_CREATE")]
        public async Task<IActionResult> Create()
        {
            await PopulateDropdownsAsync();

            var model = new ProductFormViewModel
            {
                Unit = "Chai",
                IsActive = true,
                CostPrice = 0,
                SellingPrice = 0
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("PRODUCT_CREATE")]
        public async Task<IActionResult> Create(ProductFormViewModel model, IFormFile? thumbnailFile)
        {
            // Handle uploaded thumbnail if provided
            if (thumbnailFile != null && thumbnailFile.Length > 0)
            {
                string uploadedUrl = await SaveImageFileAsync(thumbnailFile);
                if (!string.IsNullOrEmpty(uploadedUrl))
                {
                    model.ThumbnailUrl = uploadedUrl;
                }
            }

            if (await _productService.IsSkuExistsAsync(model.Sku.Trim().ToUpperInvariant()))
            {
                ModelState.AddModelError("Sku", "Mã SKU này đã tồn tại trong danh mục.");
            }

            if (await _productService.IsBarcodeExistsAsync(model.Barcode.Trim()))
            {
                ModelState.AddModelError("Barcode", "Mã Barcode này đã được sử dụng.");
            }

            if (await _productService.IsSlugExistsAsync(model.Slug.Trim().ToLowerInvariant()))
            {
                ModelState.AddModelError("Slug", "Đường dẫn Slug này đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model.SelectedSkinTypeIds, model.SelectedIngredientIds);
                return View(model);
            }

            var success = await _productService.CreateProductAsync(model);
            if (success)
            {
                InvalidateCatalogCache();
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", $"Đã thêm mới mỹ phẩm '{model.ProductName}' thành công!", "success");
                TempData["SuccessMessage"] = $"Đã thêm mới mỹ phẩm '{model.ProductName}' thành công!";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi lưu sản phẩm vào cơ sở dữ liệu.");
            await PopulateDropdownsAsync(model.SelectedSkinTypeIds, model.SelectedIngredientIds);
            return View(model);
        }

        [HttpGet]
        [HasPermission("PRODUCT_EDIT")]
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _productService.GetProductForEditAsync(id);
            if (model == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm để chỉnh sửa.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(model.SelectedSkinTypeIds, model.SelectedIngredientIds);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("PRODUCT_EDIT")]
        public async Task<IActionResult> Edit(int id, ProductFormViewModel model, IFormFile? thumbnailFile)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            // Handle uploaded thumbnail
            if (thumbnailFile != null && thumbnailFile.Length > 0)
            {
                string uploadedUrl = await SaveImageFileAsync(thumbnailFile);
                if (!string.IsNullOrEmpty(uploadedUrl))
                {
                    model.ThumbnailUrl = uploadedUrl;
                }
            }

            if (await _productService.IsSkuExistsAsync(model.Sku.Trim().ToUpperInvariant(), id))
            {
                ModelState.AddModelError("Sku", "Mã SKU này đã tồn tại ở sản phẩm khác.");
            }

            if (await _productService.IsBarcodeExistsAsync(model.Barcode.Trim(), id))
            {
                ModelState.AddModelError("Barcode", "Mã Barcode này đã được sử dụng ở sản phẩm khác.");
            }

            if (await _productService.IsSlugExistsAsync(model.Slug.Trim().ToLowerInvariant(), id))
            {
                ModelState.AddModelError("Slug", "Đường dẫn Slug này đã tồn tại ở sản phẩm khác.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model.SelectedSkinTypeIds, model.SelectedIngredientIds);
                return View(model);
            }

            var success = await _productService.UpdateProductAsync(model);
            if (success)
            {
                InvalidateCatalogCache();
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", $"Đã cập nhật mỹ phẩm '{model.ProductName}' thành công!", "success");
                TempData["SuccessMessage"] = $"Đã cập nhật mỹ phẩm '{model.ProductName}' thành công!";
                return RedirectToAction(nameof(Details), new { id = model.Id });
            }

            ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi cập nhật thông tin sản phẩm.");
            await PopulateDropdownsAsync(model.SelectedSkinTypeIds, model.SelectedIngredientIds);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("PRODUCT_DELETE")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productService.GetProductDetailAsync(id);
            if (product == null)
            {
                if (Request.IsHtmx())
                {
                    return NotFound(new { success = false, message = "Không tìm thấy sản phẩm." });
                }
                return NotFound();
            }

            var success = await _productService.SoftDeleteProductAsync(id);
            if (success)
            {
                InvalidateCatalogCache();
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", $"Đã xóa mềm mỹ phẩm '{product.Name}'!", "warning");

                if (Request.IsHtmx())
                {
                    Response.Headers.Append("HX-Trigger", "reloadProductTable");
                    return Ok(new { success = true, message = $"Đã xóa sản phẩm {product.Name} thành công." });
                }

                TempData["SuccessMessage"] = $"Đã xóa mềm sản phẩm '{product.Name}' thành công.";
                return RedirectToAction(nameof(Index));
            }

            if (Request.IsHtmx())
            {
                return BadRequest(new { success = false, message = "Không thể xóa sản phẩm lúc này." });
            }

            TempData["ErrorMessage"] = "Có lỗi xảy ra khi thực hiện xóa sản phẩm.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("PRODUCT_EDIT")]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var result = await _productService.ToggleProductStatusAsync(id);
            if (!result.Success) return NotFound();

            InvalidateCatalogCache();

            var message = result.NewStatus == 1
                ? $"Đã mở bán lại sản phẩm {result.ProductName}."
                : $"Đã tạm ngừng kinh doanh sản phẩm {result.ProductName}.";

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", message, "success");

            if (Request.IsHtmx())
            {
                Response.Headers.Append("HX-Trigger", "reloadProductTable");
                return Content("");
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        private void InvalidateCatalogCache()
        {
            _cache?.Remove("HomeSections_Composite_Model_v1");
            _cache?.Remove("home_ui_sections");
            _cache?.Remove("category_sidebar_items");
            _cache?.Remove("brand_sidebar_items");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("PRODUCT_CREATE")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "Vui lòng chọn tệp ảnh hợp lệ." });
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                return BadRequest(new { success = false, message = "Định dạng tệp không được hỗ trợ (chỉ nhận JPG, PNG, WEBP, GIF)." });
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { success = false, message = "Dung lượng ảnh tối đa 5MB." });
            }

            string url = await SaveImageFileAsync(file);
            return Json(new { success = true, url });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("PRODUCT_EDIT")]
        public async Task<IActionResult> CreateVariant([FromForm] CreateProductVariantDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Dữ liệu biến thể không hợp lệ." });
            }

            var success = await _productService.CreateVariantAsync(dto);
            if (success)
            {
                TempData["SuccessMessage"] = "Đã thêm mới biến thể thành công.";
                return RedirectToAction(nameof(Details), new { id = dto.ProductId });
            }

            return BadRequest(new { success = false, message = "Không thể thêm biến thể." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("PRODUCT_DELETE")]
        public async Task<IActionResult> DeleteVariant(int id, int productId)
        {
            var success = await _productService.DeleteVariantAsync(id);
            if (success)
            {
                TempData["SuccessMessage"] = "Đã xóa biến thể sản phẩm.";
                return RedirectToAction(nameof(Details), new { id = productId });
            }

            TempData["ErrorMessage"] = "Không thể xóa biến thể.";
            return RedirectToAction(nameof(Details), new { id = productId });
        }

        private async Task<string> SaveImageFileAsync(IFormFile file)
        {
            string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "products");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            string uniqueFileName = $"prod_{Guid.NewGuid():N}{fileExtension}";
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/products/{uniqueFileName}";
        }

        private async Task PopulateDropdownsAsync(List<int>? selectedSkinTypes = null, List<int>? selectedIngredients = null)
        {
            var categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            var suppliers = await _context.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
            var skinTypes = await _productService.GetAllSkinTypesAsync();
            var ingredients = await _productService.GetAllIngredientsAsync();

            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            ViewBag.Suppliers = new SelectList(suppliers, "Id", "Name");
            ViewBag.SkinTypes = skinTypes;
            ViewBag.Ingredients = ingredients;
            ViewBag.SelectedSkinTypeIds = selectedSkinTypes ?? new List<int>();
            ViewBag.SelectedIngredientIds = selectedIngredients ?? new List<int>();
        }
    }
}