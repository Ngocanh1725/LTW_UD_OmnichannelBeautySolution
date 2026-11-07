using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Catalog;

namespace Omnichannel.Application.Interfaces.Catalog
{
    public interface IRecommendationService
    {
        /// <summary>
        /// Lấy combo sản phẩm thường được mua cùng (Frequently Bought Together)
        /// Thuật toán đồng xuất hiện (Co-occurrence) trên đơn hàng hoàn tất kết hợp Skincare Routine Synergy Fallback
        /// </summary>
        Task<FrequentlyBoughtTogetherDto?> GetFrequentlyBoughtTogetherAsync(
            int productId, 
            int top = 2, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy danh sách sản phẩm rút gọn theo danh sách ID (dùng cho Sản phẩm đã xem gần đây - Recently Viewed)
        /// Bảo toàn thứ tự danh sách ID đầu vào và loại bỏ sản phẩm không hoạt động
        /// </summary>
        Task<List<RecentlyViewedProductDto>> GetProductsByIdsAsync(
            IEnumerable<int> productIds, 
            CancellationToken cancellationToken = default);
    }
}

