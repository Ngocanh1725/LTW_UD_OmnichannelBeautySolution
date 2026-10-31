using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omnichannel.Application.DTOs.Notifications;
using Omnichannel.Application.Interfaces.Notifications;
using Omnichannel.Web.Extensions;

namespace Omnichannel.Web.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationApiController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationApiController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            int? userId = User.GetUserId();
            string? role = User.FindFirst(ClaimTypes.Role)?.Value;
            int? storeId = User.GetStoreId();

            var count = await _notificationService.GetUnreadCountAsync(userId, role, storeId);
            return Ok(new
            {
                success = true,
                count
            });
        }

        [HttpGet("latest")]
        public async Task<IActionResult> GetLatest([FromQuery] int take = 10)
        {
            take = Math.Clamp(take, 1, 50);
            int? userId = User.GetUserId();
            string? role = User.FindFirst(ClaimTypes.Role)?.Value;
            int? storeId = User.GetStoreId();

            var notifications = await _notificationService.GetLatestNotificationsAsync(take, userId, role, storeId);
            var unreadCount = await _notificationService.GetUnreadCountAsync(userId, role, storeId);

            return Ok(new
            {
                success = true,
                unreadCount,
                notifications
            });
        }

        [HttpPost("mark-read/{id:int}")]
        public async Task<IActionResult> MarkRead(int id)
        {
            int? userId = User.GetUserId();
            var success = await _notificationService.MarkAsReadAsync(id, userId);

            string? role = User.FindFirst(ClaimTypes.Role)?.Value;
            int? storeId = User.GetStoreId();
            var unreadCount = await _notificationService.GetUnreadCountAsync(userId, role, storeId);

            return Ok(new
            {
                success,
                unreadCount
            });
        }

        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllRead()
        {
            int? userId = User.GetUserId();
            string? role = User.FindFirst(ClaimTypes.Role)?.Value;
            int? storeId = User.GetStoreId();

            var updatedCount = await _notificationService.MarkAllAsReadAsync(userId, role, storeId);
            return Ok(new
            {
                success = true,
                updatedCount,
                unreadCount = 0
            });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _notificationService.DeleteNotificationAsync(id);
            int? userId = User.GetUserId();
            string? role = User.FindFirst(ClaimTypes.Role)?.Value;
            int? storeId = User.GetStoreId();
            var unreadCount = await _notificationService.GetUnreadCountAsync(userId, role, storeId);

            return Ok(new
            {
                success,
                unreadCount
            });
        }
    }
}
