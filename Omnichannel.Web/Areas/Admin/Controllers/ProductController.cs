using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly ApplicationDbContext _context; // For simple dropdowns, usually this should also be in a service
        private readonly IHubContext<SystemHub> _hubContext;

        public ProductController(IProductService productService, ApplicationDbContext context, IHubContext<SystemHub> hubContext)
        {
            _productService = productService;
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet]
        [HasPermission("CATALOG", "VIEW")]
        public async Task<IActionResult> Index(string? keyword = null, int? categoryId = null)
        {
            var products = await _productService.GetProductsAsync(keyword, categoryId);

            ViewBag.Categories = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "CategoryId", "CategoryName", categoryId);
            ViewBag.Keyword = keyword;

            if (Request.IsHtmx())
            {
                return PartialView("_ProductList", products);
            }

            return View(products);
        }

        [HttpGet]
        [HasPermission("CATALOG", "CREATE")]
        public async Task<IActionResult> Create()
        {
            await PopulateDropdownsAsync();
            
            if (Request.IsHtmx())
            {
                return PartialView("_CreateProductModal", new CreateProductViewModel());
            }
            
            return View(new CreateProductViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("CATALOG", "CREATE")]
        public async Task<IActionResult> Create(CreateProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync();
                if (Request.IsHtmx()) return PartialView("_CreateProductModal", model);
                return View(model);
            }

            if (await _productService.IsSkuExistsAsync(model.ProductId.Trim().ToUpperInvariant()))
            {
                ModelState.AddModelError("ProductId", "Mã SKU này đã tồn tại trong danh mục.");
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
                await PopulateDropdownsAsync();
                if (Request.IsHtmx()) return PartialView("_CreateProductModal", model);
                return View(model);
            }

            var success = await _productService.CreateProductAsync(model);
            
            if (success)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", $"Đã thêm mới mỹ phẩm '{model.ProductName}' thành công!", "success");
                
                if (Request.IsHtmx())
                {
                    Response.Headers.Append("HX-Trigger", "reloadProductTable");
                    return Content(""); // Return empty to close modal or trigger reloads
                }

                TempData["SuccessMessage"] = $"Đã thêm mới mỹ phẩm '{model.ProductName}' thành công!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync();
            if (Request.IsHtmx()) return PartialView("_CreateProductModal", model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("CATALOG", "EDIT")]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var result = await _productService.ToggleProductStatusAsync(id);
            
            if (!result.Success) return NotFound();

            var message = result.NewStatus == 1
                ? $"Đã mở bán lại sản phẩm {result.ProductName}."
                : $"Đã tạm ngừng kinh doanh sản phẩm {result.ProductName}.";

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", message, "success");

            if (Request.IsHtmx())
            {
                Response.Headers.Append("HX-Trigger", "reloadProductTable");
                return Content(""); // Trả về content rỗng, sự kiện reload sẽ gọi lại bảng
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdownsAsync()
        {
            var categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            var suppliers = await _context.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();

            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            ViewBag.Suppliers = new SelectList(suppliers, "Id", "Name");
        }
    }
}