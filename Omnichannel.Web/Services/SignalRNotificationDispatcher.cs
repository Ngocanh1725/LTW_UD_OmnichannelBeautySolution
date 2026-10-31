using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Notifications;
using Omnichannel.Application.Interfaces.Notifications;
using Omnichannel.Web.Hubs;

namespace Omnichannel.Web.Services
{
    public class SignalRNotificationDispatcher : INotificationDispatcher
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<SignalRNotificationDispatcher> _logger;

        public SignalRNotificationDispatcher(
            IHubContext<NotificationHub> hubContext,
            ILogger<SignalRNotificationDispatcher> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task DispatchNotificationAsync(NotificationDto notification, CancellationToken cancellationToken = default)
        {
            try
            {
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
                else
                {
                    _logger.LogInformation("Phát thông báo Broadcast tới toàn bộ hệ thống: {Title}", notification.Title);
                    await _hubContext.Clients.All
                        .SendAsync("ReceiveNotification", notification, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi phát thông báo SignalR: {Title}", notification.Title);
            }
        }
    }
}
