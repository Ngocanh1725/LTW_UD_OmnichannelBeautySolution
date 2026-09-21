using System.Collections.Generic;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Inventory;

namespace Omnichannel.Application.Interfaces.Catalog
{
    public interface IProductService
    {
        Task<List<ProductItemViewModel>> GetProductsAsync(string? keyword = null, int? categoryId = null);
        Task<bool> CreateProductAsync(CreateProductViewModel model);
        Task<(bool Success, int NewStatus, string ProductName)> ToggleProductStatusAsync(string id);
        Task<bool> IsSkuExistsAsync(string sku);
        Task<bool> IsBarcodeExistsAsync(string barcode);
        Task<bool> IsSlugExistsAsync(string slug);
    }
}
