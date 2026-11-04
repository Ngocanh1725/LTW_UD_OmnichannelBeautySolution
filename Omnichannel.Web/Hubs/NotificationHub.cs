using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Notifications;

namespace Omnichannel.Web.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var user = Context.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                // 1. Group theo User ID cụ thể
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
                }

                // 2. Group theo Role
                var roles = user.FindAll(ClaimTypes.Role);
                foreach (var r in roles)
                {
                    if (!string.IsNullOrEmpty(r.Value))
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, $"Role_{r.Value}");
                    }
                }

                // 3. Phân luồng Group "Managers" cho nhân sự quản lý nội bộ
                if (user.IsInRole("Admin") || user.IsInRole("SuperAdmin") || 
                    user.IsInRole("StoreManager") || user.IsInRole("WarehouseStaff"))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, "Managers");
                }

                // 4. Group theo Store / Chi nhánh
                var storeId = user.FindFirst("StoreId")?.Value;
                if (!string.IsNullOrEmpty(storeId))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Store_{storeId}");
                }

                // 5. Group cho tất cả người dùng đã đăng nhập (loại bỏ khách vãng lai chưa xác thực)
                await Groups.AddToGroupAsync(Context.ConnectionId, "AllAuthenticated");

                _logger.LogInformation("[NotificationHub] Client SignalR kết nối thành công: ConnectionId={ConnId}, UserId={UserId}", Context.ConnectionId, userId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogDebug("[NotificationHub] Client SignalR ngắt kết nối: ConnectionId={ConnId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Gửi thông báo tới một User cụ thể
        /// </summary>
        public async Task SendNotification(string userId, string message)
        {
            await Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", new
            {
                title = "Thông báo",
                message = message,
                severity = "INFO"
            });
        }

        /// <summary>
        /// Gửi thông báo tới một Vai trò (Role) cụ thể
        /// </summary>
        public async Task SendToRole(string roleName, string message)
        {
            await Clients.Group($"Role_{roleName}").SendAsync("ReceiveNotification", new
            {
                title = "Thông báo hệ thống",
                message = message,
                severity = "INFO"
            });
        }

        /// <summary>
        /// Gửi thông báo tới toàn bộ người dùng đã xác thực (tránh bắn tới khách vãng lai)
        /// </summary>
        public async Task SendToAll(string message)
        {
            await Clients.Group("AllAuthenticated").SendAsync("ReceiveNotification", new
            {
                title = "Thông báo chung",
                message = message,
                severity = "INFO"
            });
        }

        /// <summary>
        /// Gửi đối tượng thông báo có cấu trúc đầy đủ, tự động phân nhóm chính xác
        /// </summary>
        public async Task SendNotificationPayload(NotificationDto payload)
        {
            if (payload.UserId.HasValue)
            {
                await Clients.Group($"User_{payload.UserId.Value}").SendAsync("ReceiveNotification", payload);
            }
            else if (!string.IsNullOrEmpty(payload.Role))
            {
                await Clients.Group($"Role_{payload.Role}").SendAsync("ReceiveNotification", payload);
            }
            else if (payload.StoreId.HasValue)
            {
                await Clients.Group($"Store_{payload.StoreId.Value}").SendAsync("ReceiveNotification", payload);
            }
            else if (string.Equals(payload.Type, "ALERT", StringComparison.OrdinalIgnoreCase))
            {
                // Cảnh báo nội bộ: Chỉ gửi tới các tài khoản quản lý trong nhóm "Managers"
                await Clients.Group("Managers").SendAsync("ReceiveNotification", payload);
            }
            else
            {
                // Thông báo chung hệ thống: Gửi tới các tài khoản đã đăng nhập
                await Clients.Group("AllAuthenticated").SendAsync("ReceiveNotification", payload);
            }
        }
    }
}
