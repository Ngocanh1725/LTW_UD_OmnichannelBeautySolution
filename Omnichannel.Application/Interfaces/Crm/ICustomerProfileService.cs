using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Crm;

namespace Omnichannel.Application.Interfaces.Crm
{
    public interface ICustomerProfileService
    {
        /// <summary>
        /// Lấy hồ sơ sắc đẹp của khách hàng theo UserId
        /// </summary>
        Task<CustomerProfileDto?> GetProfileByUserIdAsync(int userId, CancellationToken ct = default);

        /// <summary>
        /// Lấy hồ sơ sắc đẹp của khách hàng theo ProfileId
        /// </summary>
        Task<CustomerProfileDto?> GetProfileByIdAsync(int profileId, CancellationToken ct = default);

        /// <summary>
        /// Tạo mới hoặc cập nhật hồ sơ sắc đẹp (loại da, tiền sử kích ứng, thương hiệu yêu thích)
        /// </summary>
        Task<CustomerProfileDto> UpsertProfileAsync(CreateCustomerProfileDto dto, CancellationToken ct = default);

        /// <summary>
        /// Thêm nhật ký theo dõi da định kỳ (Skin History) bởi Beauty Advisor
        /// </summary>
        Task<SkinHistoryDto> AddSkinHistoryAsync(int profileId, AddSkinHistoryDto dto, int? recordedBy = null, CancellationToken ct = default);

        /// <summary>
        /// Lấy toàn bộ lịch sử biến chuyển làn da của khách hàng
        /// </summary>
        Task<List<SkinHistoryDto>> GetSkinHistoriesAsync(int profileId, CancellationToken ct = default);

        /// <summary>
        /// Tính toán chu kỳ hao hụt sản phẩm và lập lịch dự báo mua lại (Repurchase Schedule)
        /// </summary>
        Task<RepurchaseAlertDto> CalculateRepurchaseScheduleAsync(int userId, int productId, decimal volume, decimal dailyUsage, CancellationToken ct = default);

        /// <summary>
        /// Lấy danh sách khách hàng có sản phẩm sắp hết cần nhắc mua lại trong ngưỡng ngày quy định
        /// </summary>
        Task<List<RepurchaseAlertDto>> GetRepurchaseAlertsAsync(int daysThreshold = 7, CancellationToken ct = default);
    }
}
