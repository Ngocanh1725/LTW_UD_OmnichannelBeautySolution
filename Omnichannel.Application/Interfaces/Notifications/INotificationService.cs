using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Notifications;

namespace Omnichannel.Application.Interfaces.Notifications
{
    public interface INotificationService
    {
        Task<NotificationDto> CreateAndSendNotificationAsync(CreateNotificationDto dto, CancellationToken cancellationToken = default);

        Task<PagedNotificationResultDto> GetNotificationsAsync(
            NotificationFilterDto filter,
            int? currentUserId = null,
            string? currentRole = null,
            int? currentStoreId = null,
            CancellationToken cancellationToken = default);

        Task<List<NotificationDto>> GetLatestNotificationsAsync(
            int take = 10,
            int? currentUserId = null,
            string? currentRole = null,
            int? currentStoreId = null,
            CancellationToken cancellationToken = default);

        Task<int> GetUnreadCountAsync(
            int? currentUserId = null,
            string? currentRole = null,
            int? currentStoreId = null,
            CancellationToken cancellationToken = default);

        Task<bool> MarkAsReadAsync(int id, int? currentUserId = null, CancellationToken cancellationToken = default);

        Task<int> MarkAllAsReadAsync(
            int? currentUserId = null,
            string? currentRole = null,
            int? currentStoreId = null,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteNotificationAsync(int id, CancellationToken cancellationToken = default);

        Task<int> ClearAllReadAsync(
            int? currentUserId = null,
            string? currentRole = null,
            int? currentStoreId = null,
            CancellationToken cancellationToken = default);
    }
}
