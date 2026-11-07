using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Catalog;

namespace Omnichannel.Application.Interfaces.Catalog
{
    public interface IProductComparisonService
    {
        /// <summary>
        /// Lấy thông tin tóm tắt của các sản phẩm đang nằm trong Khay so sánh (Floating Drawer)
        /// </summary>
        Task<List<CompareItemSummaryDto>> GetCompareQuickItemsAsync(List<int> productIds, CancellationToken ct = default);

        /// <summary>
        /// Phân tích ma trận so sánh chi tiết Side-by-Side bao gồm: Đơn giá/ml, Điểm EWG, Hoạt chất, FEFO, Tương kỵ da liễu
        /// </summary>
        Task<ProductComparisonMatrixDto> GetDetailedComparisonAsync(List<int> productIds, CancellationToken ct = default);

        /// <summary>
        /// Gợi ý các sản phẩm cùng danh mục hoặc cùng chu trình để so sánh khi người dùng chỉ mới chọn 1 sản phẩm
        /// </summary>
        Task<List<CompareItemSummaryDto>> GetSuggestedAlternativesAsync(int productId, int limit = 4, CancellationToken ct = default);
    }
}

