using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Omnichannel.Web.Hubs
{
    /// <summary>
    /// Hub quản lý các sự kiện hệ thống, đơn hàng và cảnh báo nội bộ.
    /// Tự động phân luồng quản trị viên vào Group "Managers" để không ảnh hưởng đến khách hàng ngoài storefront.
    /// </summary>
    public class SystemHub : Hub
    {
        private readonly ILogger<SystemHub> _logger;

        public SystemHub(ILogger<SystemHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var user = Context.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var role = user.FindFirst(ClaimTypes.Role)?.Value;
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Tự động gán người dùng có quyền quản trị vào Group "Managers"
                if (user.IsInRole("Admin") || user.IsInRole("SuperAdmin") || 
                    user.IsInRole("StoreManager") || user.IsInRole("WarehouseStaff"))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, "Managers");
                    _logger.LogInformation("[SystemHub] Đã gán kết nối {ConnId} (User {UserId}, Role {Role}) vào nhóm Managers.", 
                        Context.ConnectionId, userId, role);
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogDebug("[SystemHub] Kết nối {ConnId} đã ngắt.", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Cho phép trang Admin đăng ký vào nhóm nhận thông báo quản trị (Managers)
        /// </summary>
        public async Task JoinManagersGroup()
        {
            var user = Context.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Managers");
            }
        }

        /// <summary>
        /// Gửi thông báo tới quản trị viên nội bộ (thay vì phát tán toàn bộ Clients.All)
        /// </summary>
        public async Task SendNotification(string message, string type = "success")
        {
            await Clients.Group("Managers").SendAsync("ReceiveNotification", message, type);
        }
    }
}
