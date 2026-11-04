using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Notifications;
using Omnichannel.Application.Interfaces.Notifications;
using Omnichannel.Web.Hubs;

namespace Omnichannel.Web.Services
{
    /// <summary>
    /// Điều phối thông báo SignalR Realtime tối ưu hóa:
    /// - Cơ chế Throttling / Debounce chống nghẽn đường truyền khi có sự kiện dồn dập.
    /// - Phân luồng Group chính xác (User, Role, Store, Managers, AllAuthenticated).
    /// - Tuyệt đối không gửi tràn lan Clients.All tới khách vãng lai trên Main Thread trình duyệt.
    /// </summary>
    public class SignalRNotificationDispatcher : INotificationDispatcher
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<SignalRNotificationDispatcher> _logger;

        // Bộ nhớ đệm kiểm soát tần suất phát thông báo (Throttling / Debounce cache)
        private static readonly ConcurrentDictionary<string, DateTime> _recentDispatches = new();
        private static readonly TimeSpan ThrottleWindow = TimeSpan.FromSeconds(2.5);

        public SignalRNotificationDispatcher(
            IHubContext<NotificationHub> hubContext,
            ILogger<SignalRNotificationDispatcher> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task DispatchNotificationAsync(NotificationDto notification, CancellationToken cancellationToken = default)
        {
            if (notification == null) return;

            try
            {
                // 1. Áp dụng cơ chế Throttling: Chặn phát trùng lặp các thông báo cùng nội dung trong khoảng 2.5s
                var throttleKey = $"{notification.Type}_{notification.Title}_{notification.Role}_{notification.UserId}_{notification.StoreId}";
                var now = DateTime.UtcNow;

                if (_recentDispatches.TryGetValue(throttleKey, out var lastSent))
                {
                    if (now - lastSent < ThrottleWindow)
                    {
                        _logger.LogDebug("[SignalR Throttled] Bỏ qua thông báo phát dồn dập trong 2.5s: {Key}", throttleKey);
                        return;
                    }
                }

                _recentDispatches[throttleKey] = now;

                // Tự động dọn dẹp cache nếu số lượng phần tử vượt quá 500
                if (_recentDispatches.Count > 500)
                {
                    CleanupStaleThrottleKeys(now);
                }

                // 2. Phân luồng theo nhóm người nhận cụ thể
                if (notification.UserId.HasValue)
                {
                    _logger.LogInformation("Phát thông báo tới User #{UserId}: {Title}", notification.UserId.Value, notification.Title);
                    await _hubContext.Clients.Group($"User_{notification.UserId.Value}")
                        .SendAsync("ReceiveNotification", notification, cancellationToken);
                }
                else if (!string.IsNullOrEmpty(notification.Role))
                {
                    _logger.LogInformation("Phát thông báo tới Role {Role}: {Title}", notification.Role, notification.Title);
                    await _hubContext.Clients.Group($"Role_{notification.Role}")
                        .SendAsync("ReceiveNotification", notification, cancellationToken);
                }
                else if (notification.StoreId.HasValue)
                {
                    _logger.LogInformation("Phát thông báo tới Chi nhánh Store #{StoreId}: {Title}", notification.StoreId.Value, notification.Title);
                    await _hubContext.Clients.Group($"Store_{notification.StoreId.Value}")
                        .SendAsync("ReceiveNotification", notification, cancellationToken);
                }
                else if (string.Equals(notification.Type, "ALERT", StringComparison.OrdinalIgnoreCase))
                {
                    // Cảnh báo nội bộ: Định tuyến riêng về Group "Managers", không gửi tới người dùng ngoài storefront
                    _logger.LogInformation("Phát cảnh báo nội bộ tới nhóm Managers: {Title}", notification.Title);
                    await _hubContext.Clients.Group("Managers")
                        .SendAsync("ReceiveNotification", notification, cancellationToken);
                }
                else
                {
                    // Thông báo toàn hệ thống: Chỉ gửi tới các tài khoản đã xác thực (AllAuthenticated)
                    _logger.LogInformation("Phát thông báo tới AllAuthenticated: {Title}", notification.Title);
                    await _hubContext.Clients.Group("AllAuthenticated")
                        .SendAsync("ReceiveNotification", notification, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("DispatchNotificationAsync đã hủy theo yêu cầu dừng.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi phát thông báo SignalR: {Title}", notification.Title);
            }
        }

        private static void CleanupStaleThrottleKeys(DateTime now)
        {
            foreach (var kvp in _recentDispatches)
            {
                if (now - kvp.Value > ThrottleWindow)
                {
                    _recentDispatches.TryRemove(kvp.Key, out _);
                }
            }
        }
    }
}
