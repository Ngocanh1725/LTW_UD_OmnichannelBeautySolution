using System.Collections.Generic;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Catalog;

namespace Omnichannel.Application.Interfaces.Catalog
{
    public interface ISmartFilterService
    {
        Task<SmartFilterResultDto> GetSmartRecommendationsAsync(SmartFilterCriteriaDto criteria);
        Task<List<SkinTypeDto>> GetAvailableSkinTypesAsync();
        Task<List<SkinConcernDto>> GetAvailableSkinConcernsAsync();
        Task<ProductAttributesViewModel> GetProductAttributesOverviewAsync(string? search = null, int? categoryId = null, int? skinTypeId = null, int? concernId = null);
        Task<bool> UpdateProductAttributesAsync(int productId, List<int> skinTypeIds, List<int> concernIds);
        Task<int> BulkUpdateAttributesAsync(BulkUpdateAttributesDto dto);
    }
}
