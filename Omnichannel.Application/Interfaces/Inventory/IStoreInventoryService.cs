using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Inventory;

namespace Omnichannel.Application.Interfaces.Inventory
{
    /// <summary>
    /// Service quản lý tra cứu tồn kho đa kênh theo Showroom (O2O) và chiến dịch Flash Sale
    /// </summary>
    public interface IStoreInventoryService
    {
        /// <summary>
        /// Lấy danh sách tồn kho khả dụng của 1 sản phẩm tại tất cả showroom, có thể lọc theo thành phố
        /// </summary>
        Task<List<ShowroomStockDto>> GetProductStockByShowroomsAsync(int productId, string? city = null, CancellationToken ct = default);

        /// <summary>
        /// Lấy danh sách thành phố có chi nhánh/showroom hoạt động
        /// </summary>
        Task<List<string>> GetAvailableCitiesAsync(CancellationToken ct = default);

        /// <summary>
        /// Lấy trọn bộ ViewModel tra cứu tồn kho Showroom cho trang chi tiết sản phẩm
        /// </summary>
        Task<ShowroomAvailabilityViewModel> GetShowroomAvailabilityAsync(int productId, string? city = null, CancellationToken ct = default);

        /// <summary>
        /// Lấy thông tin chiến dịch Flash Sale đang diễn ra kèm danh sách sản phẩm và thời gian đếm ngược
        /// </summary>
        Task<FlashSaleCampaignDto?> GetActiveFlashSaleAsync(CancellationToken ct = default);
    }
}

