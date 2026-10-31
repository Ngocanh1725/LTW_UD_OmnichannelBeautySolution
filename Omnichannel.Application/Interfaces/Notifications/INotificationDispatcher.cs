using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Notifications;

namespace Omnichannel.Application.Interfaces.Notifications
{
    public interface INotificationDispatcher
    {
        Task DispatchNotificationAsync(NotificationDto notification, CancellationToken cancellationToken = default);
    }
}
