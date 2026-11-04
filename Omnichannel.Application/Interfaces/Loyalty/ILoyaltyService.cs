using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Loyalty;

namespace Omnichannel.Application.Interfaces.Loyalty
{
    public interface ILoyaltyService
    {
        /// <summary>
        /// Tích điểm tự động dựa trên giá trị đơn hàng và hệ số nhân của hạng thành viên (10.000 VNĐ = 1 điểm cơ sở)
        /// </summary>
        Task<EarnPointsDto> EarnPointsAsync(int userId, int orderId, decimal orderAmount, CancellationToken cancellationToken = default);

        /// <summary>
        /// Đổi điểm thưởng thành tiền giảm giá hoặc voucher (1 điểm = 1.000 VNĐ)
        /// </summary>
        Task<RedeemPointsDto> RedeemPointsAsync(int userId, int points, string description, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy thông tin ví điểm của người dùng theo UserId
        /// </summary>
        Task<LoyaltyWalletDto?> GetWalletAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy hoặc tạo mới ví điểm cho người dùng nếu chưa có (mặc định hạng Silver)
        /// </summary>
        Task<LoyaltyWalletDto> GetOrCreateWalletAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Đánh giá và cập nhật hạng thành viên dựa trên tổng chi tiêu trong 12 tháng gần nhất
        /// </summary>
        Task<TierInfoDto> EvaluateTierAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy danh sách voucher trong kho của người dùng (bao gồm voucher cá nhân và voucher công khai)
        /// </summary>
        Task<List<VoucherDto>> GetUserVouchersAsync(int userId, bool onlyValid = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sinh và cập nhật mã QR định danh thành viên phục vụ quẹt tại quầy POS
        /// </summary>
        Task<string> GenerateMemberQrAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy danh sách ví điểm của tất cả khách hàng kèm bộ lọc tìm kiếm và hạng
        /// </summary>
        Task<List<LoyaltyWalletDto>> GetAllWalletsAsync(string? search = null, int? tierId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Điều chỉnh điểm thủ công cho khách hàng bởi Admin (cộng/trừ kèm lý do)
        /// </summary>
        Task<bool> AdjustPointsAsync(AdjustPointsRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy chi tiết ví điểm của 1 khách hàng kèm toàn bộ lịch sử giao dịch
        /// </summary>
        Task<LoyaltyWalletDto?> GetWalletDetailsAsync(int walletId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy danh sách tất cả các hạng thành viên (Silver, Gold, Diamond...)
        /// </summary>
        Task<List<TierInfoDto>> GetAllTiersAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Cập nhật thông tin cấu hình hạng thành viên (ngưỡng chi tiêu, hệ số, quà sinh nhật)
        /// </summary>
        Task<bool> UpdateTierAsync(TierInfoDto tierDto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Thống kê tổng hợp số lượng thành viên, điểm lưu hành, phân bổ hạng phục vụ Dashboard và biểu đồ Pie Chart
        /// </summary>
        Task<LoyaltySummaryDto> GetLoyaltySummaryAsync(CancellationToken cancellationToken = default);

        // =========================================================================
        // GAMIFICATION LOYALTY (GAP-08 - PHIÊN C7)
        // =========================================================================

        /// <summary>
        /// Lấy trạng thái lịch điểm danh 7 ngày của thành viên theo múi giờ Việt Nam (UTC+7)
        /// </summary>
        Task<DailyCheckInStatusDto> GetDailyCheckInStatusAsync(int userId, System.DateTime? utcNowOverride = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Thực hiện điểm danh hằng ngày (10-10-20-20-30-30-100 điểm), chống điểm danh 2 lần/ngày (UTC+7) và ghi sổ cái PointTransactions
        /// </summary>
        Task<DailyCheckInResultDto> CheckInDailyAsync(int userId, System.DateTime? utcNowOverride = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy thông tin Mã giới thiệu bạn bè và danh sách người đã giới thiệu
        /// </summary>
        Task<ReferralDashboardDto> GetReferralDashboardAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Áp dụng mã giới thiệu bạn bè (chặn tự giới thiệu và chặn giới thiệu vòng lặp A <-> B)
        /// </summary>
        Task<ApplyReferralResultDto> ApplyReferralCodeAsync(int referredUserId, string referralCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Kích hoạt trao thưởng cho cả người giới thiệu và người được giới thiệu khi người mới hoàn tất đơn hàng đầu tiên
        /// </summary>
        Task<ApplyReferralResultDto> ProcessReferralFirstOrderCompletedAsync(int referredUserId, int? orderId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy danh sách Nhiệm vụ làm đẹp (Beauty Missions) kèm trạng thái hoàn thành & nhận thưởng
        /// </summary>
        Task<List<BeautyMissionDto>> GetBeautyMissionsAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Nhận điểm thưởng khi hoàn thành Nhiệm vụ làm đẹp (ghi vào sổ cái PointTransactions)
        /// </summary>
        Task<ClaimMissionResultDto> ClaimMissionRewardAsync(int userId, string missionCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy danh mục đổi điểm lấy Voucher (Omni Rewards Bazaar)
        /// </summary>
        Task<List<RewardCatalogItemDto>> GetRewardsCatalogAsync(int userId, CancellationToken cancellationToken = default);
    }
}
