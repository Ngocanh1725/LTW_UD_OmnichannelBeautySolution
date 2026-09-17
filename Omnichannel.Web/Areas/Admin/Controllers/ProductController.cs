using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Inventory;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Security;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [HasPermission("CATALOG", "VIEW")]
        public async Task<IActionResult> Index(string? keyword = null, int? categoryId = null)
        {
            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string cleanKeyword = keyword.Trim().ToLower();
                query = query.Where(p => p.ProductName.ToLower().Contains(cleanKeyword)
                                      || p.ProductId.ToLower().Contains(cleanKeyword)
                                      || p.Barcode.Contains(cleanKeyword));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new ProductItemViewModel
                {
                    ProductId = p.ProductId,
                    Barcode = p.Barcode,
                    ProductName = p.ProductName,
                    Slug = p.Slug,
                    CategoryName = p.Category.CategoryName,
                    SupplierName = p.Supplier.SupplierName,
                    SellingPrice = p.SellingPrice,
                    CostPrice = p.CostPrice,
                    SafetyStock = p.SafetyStock,
                    Unit = p.Unit,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            ViewBag.Categories = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "CategoryId", "CategoryName", categoryId);
            ViewBag.Keyword = keyword;

            return View(products);
        }

        [HttpGet]
        [HasPermission("CATALOG", "CREATE")]
        public async Task<IActionResult> Create()
        {
            await PopulateDropdownsAsync();
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
                return View(model);
            }

            string cleanSku = model.ProductId.Trim().ToUpperInvariant();
            string cleanBarcode = model.Barcode.Trim();
            string cleanSlug = model.Slug.Trim().ToLowerInvariant();

            // Kiểm tra trùng mã SKU
            if (await _context.Products.AnyAsync(p => p.ProductId == cleanSku))
            {
                ModelState.AddModelError("ProductId", "Mã SKU này đã tồn tại trong danh mục.");
            }

            // Kiểm tra trùng Barcode
            if (await _context.Products.AnyAsync(p => p.Barcode == cleanBarcode))
            {
                ModelState.AddModelError("Barcode", "Mã Barcode này đã được sử dụng.");
            }

            // Kiểm tra trùng Slug
            if (await _context.Products.AnyAsync(p => p.Slug == cleanSlug))
            {
                ModelState.AddModelError("Slug", "Đường dẫn Slug này đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync();
                return View(model);
            }

            var product = new Product
            {
                ProductId = cleanSku,
                CategoryId = model.CategoryId,
                SupplierId = model.SupplierId,
                Barcode = cleanBarcode,
                ProductName = model.ProductName.Trim(),
                Slug = cleanSlug,
                SellingPrice = model.SellingPrice,
                CostPrice = model.CostPrice,
                SafetyStock = model.SafetyStock,
                Unit = model.Unit.Trim(),
                Status = model.Status,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã thêm mới mỹ phẩm '{product.ProductName}' (SKU: {product.ProductId}) thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("CATALOG", "EDIT")]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.Status = product.Status == 1 ? 0 : 1;
            product.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = product.Status == 1
                ? $"Đã mở bán lại sản phẩm {product.ProductName}."
                : $"Đã tạm ngừng kinh doanh sản phẩm {product.ProductName}.";

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdownsAsync()
        {
            var categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync();
            var suppliers = await _context.Suppliers.Where(s => s.Status).OrderBy(s => s.SupplierName).ToListAsync();

            ViewBag.Categories = new SelectList(categories, "CategoryId", "CategoryName");
            ViewBag.Suppliers = new SelectList(suppliers, "SupplierId", "SupplierName");
        }
    }
}