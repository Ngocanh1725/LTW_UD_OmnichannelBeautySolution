using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Messages;

namespace Omnichannel.Application.Interfaces.Messages
{
    public interface IInternalMessageService
    {
        Task<MessageFolderDto> GetInboxMessagesAsync(int userId, string? search = null, int page = 1, int pageSize = 15, CancellationToken cancellationToken = default);
        Task<MessageFolderDto> GetSentMessagesAsync(int userId, string? search = null, int page = 1, int pageSize = 15, CancellationToken cancellationToken = default);
        Task<InternalMessageDto?> GetMessageDetailsAsync(int messageId, int currentUserId, CancellationToken cancellationToken = default);
        Task<InternalMessageDto> SendMessageAsync(int senderId, ComposeMessageDto dto, CancellationToken cancellationToken = default);
        Task<bool> MarkAsReadAsync(int messageId, int currentUserId, CancellationToken cancellationToken = default);
        Task<bool> DeleteMessageAsync(int messageId, int currentUserId, string folder = "inbox", CancellationToken cancellationToken = default);
        Task<int> GetUnreadMessageCountAsync(int userId, CancellationToken cancellationToken = default);
        Task<List<UserRecipientDto>> GetReceivableUsersAsync(int currentUserId, CancellationToken cancellationToken = default);
    }
}
