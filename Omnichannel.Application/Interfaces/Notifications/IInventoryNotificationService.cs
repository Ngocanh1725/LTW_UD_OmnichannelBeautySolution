using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Notifications;

namespace Omnichannel.Application.Interfaces.Notifications
{
    /// <summary>
    /// Quản lý đăng ký thông báo Có hàng lại (Back-in-Stock) & Giảm giá (Price Drop) - GAP-05
    /// </summary>
    public interface IInventoryNotificationService
    {
        /// <summary>
        /// Khách hàng đăng ký nhận thông báo khi có hàng lại
        /// </summary>
        Task<SubscriptionResultDto> SubscribeBackInStockAsync(SubscribeBackInStockRequest request, int? userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Khách hàng đăng ký nhận thông báo khi giá giảm đến hoặc dưới mức TargetPrice
        /// </summary>
        Task<SubscriptionResultDto> SubscribePriceDropAsync(SubscribePriceDropRequest request, int? userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy danh sách sản phẩm đang theo dõi của khách hàng
        /// </summary>
        Task<List<CustomerSubscriptionDto>> GetCustomerSubscriptionsAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Hủy theo dõi một đăng ký
        /// </summary>
        Task<bool> UnsubscribeAsync(int subscriptionId, int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Kích hoạt gửi thông báo có hàng lại khi nhập kho hoặc kiểm kê kho (tồn chuyển từ 0 -> > 0)
        /// </summary>
        Task<int> TriggerBackInStockAlertsAsync(int productId, int newQuantity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Kích hoạt gửi thông báo giảm giá khi giá bán sản phẩm được cập nhật hoặc áp dụng khuyến mãi
        /// </summary>
        Task<int> TriggerPriceDropAlertsAsync(int productId, decimal newPrice, decimal? oldPrice = null, CancellationToken cancellationToken = default);
    }
}

