using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Inventory;
using Omnichannel.Domain.Entities;

namespace Omnichannel.Application.Interfaces.Catalog
{
    public interface IProductService
    {
        Task<List<ProductItemViewModel>> GetProductsAsync(string? keyword = null, int? categoryId = null);
        Task<ProductListPagedViewModel> GetProductsPagedAsync(ProductFilterRequest filter, CancellationToken cancellationToken = default);
        Task<bool> CreateProductAsync(ProductFormViewModel model);
        Task<ProductFormViewModel?> GetProductForEditAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> UpdateProductAsync(ProductFormViewModel model, CancellationToken cancellationToken = default);
        Task<bool> SoftDeleteProductAsync(int id, CancellationToken cancellationToken = default);
        Task<(bool Success, int NewStatus, string ProductName)> ToggleProductStatusAsync(string id);
        Task<bool> IsSkuExistsAsync(string sku, int? excludeProductId = null);
        Task<bool> IsBarcodeExistsAsync(string barcode, int? excludeProductId = null);
        Task<bool> IsSlugExistsAsync(string slug, int? excludeProductId = null);

        Task<ProductDetailWithVariantsDto?> GetProductDetailAsync(int productId, CancellationToken cancellationToken = default);
        Task<bool> CreateVariantAsync(CreateProductVariantDto dto, CancellationToken cancellationToken = default);
        Task<bool> DeleteVariantAsync(int variantId, CancellationToken cancellationToken = default);
        Task<List<ActiveIngredient>> GetAllIngredientsAsync(CancellationToken cancellationToken = default);
        Task<List<SkinType>> GetAllSkinTypesAsync(CancellationToken cancellationToken = default);
    }
}
