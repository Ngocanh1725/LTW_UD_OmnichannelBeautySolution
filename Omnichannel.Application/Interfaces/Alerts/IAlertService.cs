using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Alerts;

namespace Omnichannel.Application.Interfaces.Alerts
{
    public interface IAlertService
    {
        /// <summary>
        /// Quét toàn bộ hệ thống và tự động phát hiện, sinh cảnh báo cho 6 loại:
        /// 1. Hết hàng (stock = 0)
        /// 2. Sắp hết hàng (stock <= reorder point)
        /// 3. Lô hàng hết hạn trong 15 ngày (Đề xuất xả kho giảm giá 40%)
        /// 4. Lô hàng cận date 60 ngày (FEFO Sale)
        /// 5. Doanh thu hôm nay tăng đột biến so với TB 7 ngày
        /// 6. Đơn hàng chờ xử lý quá 24h
        /// </summary>
        Task<int> CheckAndGenerateAlertsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy danh sách cảnh báo có phân trang và bộ lọc (type, severity, read status, date)
        /// </summary>
        Task<PagedAlertResultDto> GetAlertsAsync(AlertFilterDto filter, int? currentUserId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy các cảnh báo mới nhất phục vụ Notification Bell Dropdown
        /// </summary>
        Task<List<AlertDto>> GetLatestAlertsAsync(int take = 10, int? currentUserId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy số lượng cảnh báo chưa đọc
        /// </summary>
        Task<int> GetUnreadCountAsync(int? currentUserId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy tóm tắt thống kê các cảnh báo (unread, danger, warning, success, info)
        /// </summary>
        Task<AlertSummaryDto> GetAlertSummaryAsync(int? currentUserId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Đánh dấu một cảnh báo là đã đọc
        /// </summary>
        Task<bool> MarkAsReadAsync(int alertId, int? currentUserId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Đánh dấu tất cả cảnh báo là đã đọc
        /// </summary>
        Task<int> MarkAllAsReadAsync(int? currentUserId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Xóa bỏ hoặc dismiss một cảnh báo
        /// </summary>
        Task<bool> DeleteAlertAsync(int alertId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Xóa sạch các cảnh báo đã đọc
        /// </summary>
        Task<int> ClearAllReadAlertsAsync(CancellationToken cancellationToken = default);
    }
}
