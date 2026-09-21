using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Inventory;
using Omnichannel.Application.Interfaces.Catalog;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services.Catalog
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;

        public ProductService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ProductItemViewModel>> GetProductsAsync(string? keyword = null, int? categoryId = null)
        {
            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string cleanKeyword = keyword.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(cleanKeyword)
                                      || p.Sku.ToLower().Contains(cleanKeyword)
                                      || p.Barcode.Contains(cleanKeyword));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new ProductItemViewModel
                {
                    ProductId = p.Sku,
                    Barcode = p.Barcode,
                    ProductName = p.Name,
                    Slug = p.Slug,
                    CategoryName = p.Category.Name,
                    SupplierName = p.Supplier != null ? p.Supplier.Name : "",
                    SellingPrice = p.SellingPrice,
                    CostPrice = p.CostPrice,
                    SafetyStock = 0,
                    Unit = p.Unit,
                    Status = p.IsActive ? 1 : 0,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<bool> CreateProductAsync(CreateProductViewModel model)
        {
            string cleanSku = model.ProductId.Trim().ToUpperInvariant();
            string cleanBarcode = model.Barcode.Trim();
            string cleanSlug = model.Slug.Trim().ToLowerInvariant();

            var product = new Product
            {
                Sku = cleanSku,
                CategoryId = model.CategoryId,
                SupplierId = int.TryParse(model.SupplierId, out int sid) ? sid : null,
                Barcode = cleanBarcode,
                Name = model.ProductName.Trim(),
                Slug = cleanSlug,
                SellingPrice = model.SellingPrice,
                CostPrice = model.CostPrice,
                Unit = model.Unit.Trim(),
                IsActive = model.Status == 1,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Products.AddAsync(product);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<(bool Success, int NewStatus, string ProductName)> ToggleProductStatusAsync(string id)
        {
            int newStatus = 0;
            string productName = string.Empty;

            int idInt = int.TryParse(id, out int parsed) ? parsed : 0;
            var product = await _context.Products.FindAsync(idInt);
            if (product == null) return (false, newStatus, productName);

            product.IsActive = !product.IsActive;
            product.UpdatedAt = DateTime.UtcNow;
            
            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            newStatus = product.IsActive ? 1 : 0;
            productName = product.Name;
            return (true, newStatus, productName);
        }

        public async Task<bool> IsSkuExistsAsync(string sku)
        {
            return await _context.Products.AnyAsync(p => p.Sku == sku);
        }

        public async Task<bool> IsBarcodeExistsAsync(string barcode)
        {
            return await _context.Products.AnyAsync(p => p.Barcode == barcode);
        }

        public async Task<bool> IsSlugExistsAsync(string slug)
        {
            return await _context.Products.AnyAsync(p => p.Slug == slug);
        }
    }
}
