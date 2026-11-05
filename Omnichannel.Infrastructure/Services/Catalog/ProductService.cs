using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Inventory;
using Omnichannel.Application.Interfaces.Catalog;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Application.Interfaces.Notifications;

namespace Omnichannel.Infrastructure.Services.Catalog
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;
        private readonly IInventoryNotificationService? _inventoryNotificationService;

        public ProductService(
            ApplicationDbContext context,
            IInventoryNotificationService? inventoryNotificationService = null)
        {
            _context = context;
            _inventoryNotificationService = inventoryNotificationService;
        }

        public async Task<List<ProductItemViewModel>> GetProductsAsync(string? keyword = null, int? categoryId = null)
        {
            var query = _context.Products
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string cleanKeyword = keyword.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(cleanKeyword)
                                      || p.Sku.ToLower().Contains(cleanKeyword)
                                      || p.Barcode.Contains(cleanKeyword));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new ProductItemViewModel
                {
                    Id = p.Id,
                    ProductId = p.Sku,
                    Sku = p.Sku,
                    Barcode = p.Barcode,
                    ProductName = p.Name,
                    Slug = p.Slug,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : string.Empty,
                    SupplierName = p.Supplier != null ? p.Supplier.Name : string.Empty,
                    ThumbnailUrl = p.ThumbnailUrl,
                    SellingPrice = p.SellingPrice,
                    DiscountPrice = p.DiscountPrice,
                    CostPrice = p.CostPrice,
                    TotalStock = p.Batches.Sum(b => b.CurrentQuantity),
                    SafetyStock = 10,
                    Unit = p.Unit,
                    Status = p.IsActive ? 1 : 0,
                    HasVariants = p.HasVariants,
                    VariantCount = p.Variants.Count,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<ProductListPagedViewModel> GetProductsPagedAsync(ProductFilterRequest filter, CancellationToken cancellationToken = default)
        {
            filter ??= new ProductFilterRequest();

            var baseQuery = _context.Products
                .AsNoTracking()
                .AsQueryable();

            // Overall stats before keyword/stock filtering (for top KPI pills)
            var statusCounts = await _context.Products
                .AsNoTracking()
                .GroupBy(p => p.IsActive)
                .Select(g => new { IsActive = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.IsActive, x => x.Count, cancellationToken);

            int totalActive = statusCounts.TryGetValue(true, out int act) ? act : 0;
            int totalInactive = statusCounts.TryGetValue(false, out int inact) ? inact : 0;
            int totalOutOfStock = await _context.Products.CountAsync(p => !p.Batches.Any() || p.Batches.Sum(b => b.CurrentQuantity) <= 0, cancellationToken);
            int totalInStock = await _context.Products.CountAsync(p => p.Batches.Sum(b => b.CurrentQuantity) > 0, cancellationToken);

            var query = baseQuery;

            // 1. Keyword search (Name, Sku, Barcode)
            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                string cleanKeyword = filter.Keyword.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(cleanKeyword)
                                      || p.Sku.ToLower().Contains(cleanKeyword)
                                      || p.Barcode.Contains(cleanKeyword));
            }

            // 2. Category filter
            if (filter.CategoryId.HasValue && filter.CategoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
            }

            // 3. Status filter (Active/Inactive)
            if (filter.Status.HasValue)
            {
                bool isActiveTarget = filter.Status.Value == 1;
                query = query.Where(p => p.IsActive == isActiveTarget);
            }

            // 4. Price range filter
            if (filter.MinPrice.HasValue && filter.MinPrice.Value > 0)
            {
                query = query.Where(p => p.SellingPrice >= filter.MinPrice.Value);
            }
            if (filter.MaxPrice.HasValue && filter.MaxPrice.Value > 0)
            {
                query = query.Where(p => p.SellingPrice <= filter.MaxPrice.Value);
            }

            // 5. Stock status filter (in_stock, out_of_stock, low_stock)
            if (!string.IsNullOrWhiteSpace(filter.StockStatus) && filter.StockStatus != "all")
            {
                if (filter.StockStatus == "in_stock")
                {
                    query = query.Where(p => p.Batches.Sum(b => b.CurrentQuantity) > 0);
                }
                else if (filter.StockStatus == "out_of_stock")
                {
                    query = query.Where(p => !p.Batches.Any() || p.Batches.Sum(b => b.CurrentQuantity) <= 0);
                }
                else if (filter.StockStatus == "low_stock")
                {
                    query = query.Where(p => p.Batches.Sum(b => b.CurrentQuantity) > 0 && p.Batches.Sum(b => b.CurrentQuantity) <= 10);
                }
            }

            int totalItems = await query.CountAsync(cancellationToken);
            int pageSize = filter.PageSize > 0 ? filter.PageSize : 10;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            int currentPage = Math.Max(1, Math.Min(filter.Page, Math.Max(1, totalPages)));

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductItemViewModel
                {
                    Id = p.Id,
                    ProductId = p.Sku,
                    Sku = p.Sku,
                    Barcode = p.Barcode,
                    ProductName = p.Name,
                    Slug = p.Slug,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : string.Empty,
                    SupplierName = p.Supplier != null ? p.Supplier.Name : string.Empty,
                    ThumbnailUrl = p.ThumbnailUrl,
                    SellingPrice = p.SellingPrice,
                    DiscountPrice = p.DiscountPrice,
                    CostPrice = p.CostPrice,
                    TotalStock = p.Batches.Sum(b => b.CurrentQuantity),
                    SafetyStock = 10,
                    Unit = p.Unit,
                    Status = p.IsActive ? 1 : 0,
                    HasVariants = p.HasVariants,
                    VariantCount = p.Variants.Count,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return new ProductListPagedViewModel
            {
                Items = items,
                Filter = filter,
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = currentPage,
                PageSize = pageSize,
                TotalActive = totalActive,
                TotalInactive = totalInactive,
                TotalOutOfStock = totalOutOfStock,
                TotalInStock = totalInStock
            };
        }

        public async Task<bool> CreateProductAsync(ProductFormViewModel model)
        {
            string cleanSku = model.Sku.Trim().ToUpperInvariant();
            string cleanBarcode = model.Barcode.Trim();
            string cleanSlug = model.Slug.Trim().ToLowerInvariant();

            var product = new Product
            {
                Sku = cleanSku,
                Barcode = cleanBarcode,
                Name = model.ProductName.Trim(),
                Slug = cleanSlug,
                CategoryId = model.CategoryId,
                SupplierId = model.SupplierId,
                ShortDescription = model.ShortDescription?.Trim(),
                Description = model.Description?.Trim(),
                CostPrice = model.CostPrice,
                SellingPrice = model.SellingPrice,
                DiscountPrice = model.DiscountPrice,
                ThumbnailUrl = model.ThumbnailUrl?.Trim(),
                ThumbnailMediaId = model.ThumbnailMediaId,
                Unit = string.IsNullOrWhiteSpace(model.Unit) ? "Chai" : model.Unit.Trim(),
                IsActive = model.IsActive,
                IsFeatured = model.IsFeatured,
                ProductType = string.IsNullOrWhiteSpace(model.ProductType) ? "STANDARD" : model.ProductType.Trim(),
                CosmeticNotificationNumber = model.CosmeticNotificationNumber?.Trim(),
                ManufacturerName = model.ManufacturerName?.Trim(),
                CountryOfOrigin = model.CountryOfOrigin?.Trim(),
                ImporterName = model.ImporterName?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();

            // Link Skin Types
            if (model.SelectedSkinTypeIds != null && model.SelectedSkinTypeIds.Any())
            {
                foreach (var skinTypeId in model.SelectedSkinTypeIds.Distinct())
                {
                    _context.ProductSkinTypes.Add(new ProductSkinType
                    {
                        ProductId = product.Id,
                        SkinTypeId = skinTypeId
                    });
                }
            }

            // Link Ingredients
            if (model.SelectedIngredientIds != null && model.SelectedIngredientIds.Any())
            {
                foreach (var ingredientId in model.SelectedIngredientIds.Distinct())
                {
                    _context.ProductIngredients.Add(new ProductIngredient
                    {
                        ProductId = product.Id,
                        ActiveIngredientId = ingredientId
                    });
                }
            }

            if ((model.SelectedSkinTypeIds?.Any() ?? false) || (model.SelectedIngredientIds?.Any() ?? false))
            {
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<ProductFormViewModel?> GetProductForEditAsync(int id, CancellationToken cancellationToken = default)
        {
            var product = await _context.Products
                .AsNoTracking()
                .AsSplitQuery()
                .Include(p => p.ProductSkinTypes)
                .Include(p => p.ProductIngredients)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (product == null) return null;

            var firstVariant = product.Variants.FirstOrDefault();

            return new ProductFormViewModel
            {
                Id = product.Id,
                Sku = product.Sku,
                Barcode = product.Barcode,
                ProductName = product.Name,
                Slug = product.Slug,
                CategoryId = product.CategoryId,
                SupplierId = product.SupplierId,
                ShortDescription = product.ShortDescription,
                Description = product.Description,
                CostPrice = product.CostPrice,
                SellingPrice = product.SellingPrice,
                DiscountPrice = product.DiscountPrice,
                ThumbnailUrl = Data.ProductImageSourceRegistry.ResolveProductImageUrl(product.ThumbnailUrl, product.Sku, product.Slug, product.Name),
                ThumbnailMediaId = product.ThumbnailMediaId,
                Unit = product.Unit,
                IsActive = product.IsActive,
                IsFeatured = product.IsFeatured,
                ProductType = product.ProductType,
                CosmeticNotificationNumber = product.CosmeticNotificationNumber,
                ManufacturerName = product.ManufacturerName,
                CountryOfOrigin = product.CountryOfOrigin,
                ImporterName = product.ImporterName,
                Volume = firstVariant?.Volume,
                Formulation = firstVariant?.Formulation,
                SelectedSkinTypeIds = product.ProductSkinTypes.Select(pst => pst.SkinTypeId).ToList(),
                SelectedIngredientIds = product.ProductIngredients.Select(pi => pi.ActiveIngredientId).ToList()
            };
        }

        public async Task<bool> UpdateProductAsync(ProductFormViewModel model, CancellationToken cancellationToken = default)
        {
            var product = await _context.Products
                .AsSplitQuery()
                .Include(p => p.ProductSkinTypes)
                .Include(p => p.ProductIngredients)
                .FirstOrDefaultAsync(p => p.Id == model.Id, cancellationToken);

            if (product == null) return false;

            product.Sku = model.Sku.Trim().ToUpperInvariant();
            product.Barcode = model.Barcode.Trim();
            product.Name = model.ProductName.Trim();
            product.Slug = model.Slug.Trim().ToLowerInvariant();
            product.CategoryId = model.CategoryId;
            product.SupplierId = model.SupplierId;
            product.ShortDescription = model.ShortDescription?.Trim();
            product.Description = model.Description?.Trim();
            decimal oldEffectivePrice = (product.DiscountPrice.HasValue && product.DiscountPrice.Value > 0)
                ? product.DiscountPrice.Value
                : product.SellingPrice;

            product.CostPrice = model.CostPrice;
            product.SellingPrice = model.SellingPrice;
            product.DiscountPrice = model.DiscountPrice;
            if (!string.IsNullOrWhiteSpace(model.ThumbnailUrl))
            {
                product.ThumbnailUrl = model.ThumbnailUrl.Trim();
            }
            product.ThumbnailMediaId = model.ThumbnailMediaId;
            product.Unit = string.IsNullOrWhiteSpace(model.Unit) ? "Chai" : model.Unit.Trim();
            product.IsActive = model.IsActive;
            product.IsFeatured = model.IsFeatured;
            product.ProductType = string.IsNullOrWhiteSpace(model.ProductType) ? "STANDARD" : model.ProductType.Trim();
            product.CosmeticNotificationNumber = model.CosmeticNotificationNumber?.Trim();
            product.ManufacturerName = model.ManufacturerName?.Trim();
            product.CountryOfOrigin = model.CountryOfOrigin?.Trim();
            product.ImporterName = model.ImporterName?.Trim();
            product.UpdatedAt = DateTime.UtcNow;

            // Sync Skin Types
            _context.ProductSkinTypes.RemoveRange(product.ProductSkinTypes);
            if (model.SelectedSkinTypeIds != null && model.SelectedSkinTypeIds.Any())
            {
                foreach (var skinTypeId in model.SelectedSkinTypeIds.Distinct())
                {
                    _context.ProductSkinTypes.Add(new ProductSkinType
                    {
                        ProductId = product.Id,
                        SkinTypeId = skinTypeId
                    });
                }
            }

            // Sync Ingredients
            _context.ProductIngredients.RemoveRange(product.ProductIngredients);
            if (model.SelectedIngredientIds != null && model.SelectedIngredientIds.Any())
            {
                foreach (var ingredientId in model.SelectedIngredientIds.Distinct())
                {
                    _context.ProductIngredients.Add(new ProductIngredient
                    {
                        ProductId = product.Id,
                        ActiveIngredientId = ingredientId
                    });
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Kích hoạt thông báo giảm giá nếu giá mới thấp hơn giá cũ (GAP-05)
            decimal newEffectivePrice = (product.DiscountPrice.HasValue && product.DiscountPrice.Value > 0)
                ? product.DiscountPrice.Value
                : product.SellingPrice;

            if (_inventoryNotificationService != null && newEffectivePrice < oldEffectivePrice)
            {
                try
                {
                    await _inventoryNotificationService.TriggerPriceDropAlertsAsync(product.Id, newEffectivePrice, oldEffectivePrice, cancellationToken);
                }
                catch { }
            }

            return true;
        }

        public async Task<bool> SoftDeleteProductAsync(int id, CancellationToken cancellationToken = default)
        {
            var product = await _context.Products.FindAsync(new object[] { id }, cancellationToken);
            if (product == null) return false;

            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<(bool Success, int NewStatus, string ProductName)> ToggleProductStatusAsync(string id)
        {
            int newStatus = 0;
            string productName = string.Empty;

            Product? product = null;
            if (int.TryParse(id, out int idInt))
            {
                product = await _context.Products.FindAsync(idInt);
            }
            if (product == null)
            {
                product = await _context.Products.FirstOrDefaultAsync(p => p.Sku == id);
            }

            if (product == null) return (false, newStatus, productName);

            product.IsActive = !product.IsActive;
            product.UpdatedAt = DateTime.UtcNow;
            
            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            newStatus = product.IsActive ? 1 : 0;
            productName = product.Name;
            return (true, newStatus, productName);
        }

        public async Task<bool> IsSkuExistsAsync(string sku, int? excludeProductId = null)
        {
            string cleanSku = sku.Trim().ToUpperInvariant();
            return await _context.Products.AnyAsync(p => p.Sku == cleanSku && (!excludeProductId.HasValue || p.Id != excludeProductId.Value));
        }

        public async Task<bool> IsBarcodeExistsAsync(string barcode, int? excludeProductId = null)
        {
            string cleanBarcode = barcode.Trim();
            return await _context.Products.AnyAsync(p => p.Barcode == cleanBarcode && (!excludeProductId.HasValue || p.Id != excludeProductId.Value));
        }

        public async Task<bool> IsSlugExistsAsync(string slug, int? excludeProductId = null)
        {
            string cleanSlug = slug.Trim().ToLowerInvariant();
            return await _context.Products.AnyAsync(p => p.Slug == cleanSlug && (!excludeProductId.HasValue || p.Id != excludeProductId.Value));
        }

        public async Task<ProductDetailWithVariantsDto?> GetProductDetailAsync(int productId, CancellationToken cancellationToken = default)
        {
            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Include(p => p.Batches.OrderBy(b => b.ExpDate))
                .Include(p => p.Variants.OrderBy(v => v.SortOrder))
                    .ThenInclude(v => v.Swatches.OrderBy(s => s.SortOrder))
                .Include(p => p.ProductIngredients)
                    .ThenInclude(pi => pi.ActiveIngredient)
                .Include(p => p.ProductSkinTypes)
                    .ThenInclude(pst => pst.SkinType)
                .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

            if (product == null) return null;

            return new ProductDetailWithVariantsDto
            {
                Id = product.Id,
                Sku = product.Sku,
                Barcode = product.Barcode,
                Name = product.Name,
                Slug = product.Slug,
                ShortDescription = product.ShortDescription,
                Description = product.Description,
                CostPrice = product.CostPrice,
                SellingPrice = product.SellingPrice,
                DiscountPrice = product.DiscountPrice,
                ThumbnailUrl = Data.ProductImageSourceRegistry.ResolveProductImageUrl(product.ThumbnailUrl, product.Sku, product.Slug, product.Name),
                ImagesJson = product.ImagesJson,
                Unit = product.Unit,
                IsActive = product.IsActive,
                IsFeatured = product.IsFeatured,
                HasVariants = product.HasVariants,
                ProductType = product.ProductType,
                CosmeticNotificationNumber = product.CosmeticNotificationNumber,
                ManufacturerName = product.ManufacturerName,
                CountryOfOrigin = product.CountryOfOrigin,
                ImporterName = product.ImporterName,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name ?? string.Empty,
                SupplierId = product.SupplierId,
                SupplierName = product.Supplier?.Name,
                Variants = product.Variants.Select(v => new ProductVariantItemDto
                {
                    Id = v.Id,
                    ProductId = v.ProductId,
                    Sku = v.Sku,
                    Barcode = v.Barcode,
                    VariantName = v.VariantName,
                    ColorName = v.ColorName,
                    ColorHex = v.ColorHex,
                    Volume = v.Volume,
                    Formulation = v.Formulation,
                    CostPrice = v.CostPrice,
                    SellingPrice = v.SellingPrice,
                    DiscountPrice = v.DiscountPrice,
                    Weight = v.Weight,
                    IsActive = v.IsActive,
                    SortOrder = v.SortOrder,
                    Swatches = v.Swatches.Select(s => new SwatchDto
                    {
                        Id = s.Id,
                        ProductVariantId = s.ProductVariantId,
                        SwatchImageUrl = s.SwatchImageUrl,
                        SwatchOnSkinImageUrl = s.SwatchOnSkinImageUrl,
                        SortOrder = s.SortOrder
                    }).ToList()
                }).ToList(),
                Ingredients = product.ProductIngredients.Select(pi => new IngredientDto
                {
                    Id = pi.ActiveIngredientId,
                    Name = pi.ActiveIngredient?.Name ?? string.Empty,
                    Slug = pi.ActiveIngredient?.Slug ?? string.Empty,
                    Concentration = pi.Concentration,
                    Description = pi.ActiveIngredient?.Description
                }).ToList(),
                SkinTypes = product.ProductSkinTypes.Select(pst => new SkinTypeDto
                {
                    Id = pst.SkinTypeId,
                    Name = pst.SkinType?.Name ?? string.Empty,
                    Slug = pst.SkinType?.Slug ?? string.Empty
                }).ToList(),
                Batches = product.Batches.Select(b => new ProductBatchItemDto
                {
                    Id = b.Id,
                    BatchNumber = b.BatchNumber,
                    ManufacturingDate = b.ManufacturingDate,
                    ExpDate = b.ExpDate,
                    ImportCost = b.ImportCost,
                    InitialQuantity = b.InitialQuantity,
                    CurrentQuantity = b.CurrentQuantity,
                    Status = b.Status
                }).ToList()
            };
        }

        public async Task<bool> CreateVariantAsync(CreateProductVariantDto dto, CancellationToken cancellationToken = default)
        {
            var product = await _context.Products.FindAsync(new object[] { dto.ProductId }, cancellationToken);
            if (product == null) return false;

            var variant = new ProductVariant
            {
                ProductId = dto.ProductId,
                Sku = dto.Sku.Trim().ToUpperInvariant(),
                Barcode = dto.Barcode.Trim(),
                VariantName = dto.VariantName.Trim(),
                ColorName = dto.ColorName?.Trim(),
                ColorHex = dto.ColorHex?.Trim(),
                Volume = dto.Volume?.Trim(),
                Formulation = dto.Formulation?.Trim(),
                CostPrice = dto.CostPrice,
                SellingPrice = dto.SellingPrice,
                DiscountPrice = dto.DiscountPrice,
                Weight = dto.Weight,
                IsActive = true,
                SortOrder = dto.SortOrder,
                CreatedAt = DateTime.UtcNow
            };

            if (!string.IsNullOrWhiteSpace(dto.SwatchImageUrl))
            {
                variant.Swatches.Add(new ProductSwatch
                {
                    SwatchImageUrl = dto.SwatchImageUrl.Trim(),
                    SwatchOnSkinImageUrl = dto.SwatchOnSkinImageUrl?.Trim(),
                    SortOrder = 0
                });
            }

            product.HasVariants = true;
            _context.ProductVariants.Add(variant);
            return await _context.SaveChangesAsync(cancellationToken) > 0;
        }

        public async Task<bool> DeleteVariantAsync(int variantId, CancellationToken cancellationToken = default)
        {
            var variant = await _context.ProductVariants
                .Include(v => v.Swatches)
                .FirstOrDefaultAsync(v => v.Id == variantId, cancellationToken);

            if (variant == null) return false;

            int productId = variant.ProductId;
            _context.ProductSwatches.RemoveRange(variant.Swatches);
            _context.ProductVariants.Remove(variant);
            await _context.SaveChangesAsync(cancellationToken);

            bool anyRemaining = await _context.ProductVariants.AnyAsync(v => v.ProductId == productId, cancellationToken);
            if (!anyRemaining)
            {
                var product = await _context.Products.FindAsync(new object[] { productId }, cancellationToken);
                if (product != null)
                {
                    product.HasVariants = false;
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            return true;
        }

        public async Task<List<ActiveIngredient>> GetAllIngredientsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.ActiveIngredients
                .AsNoTracking()
                .Where(i => i.IsActive)
                .OrderBy(i => i.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SkinType>> GetAllSkinTypesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SkinTypes
                .AsNoTracking()
                .OrderBy(s => s.Id)
                .ToListAsync(cancellationToken);
        }
    }
}
