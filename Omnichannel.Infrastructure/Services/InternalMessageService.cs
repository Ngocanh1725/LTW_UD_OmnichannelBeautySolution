using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Messages;
using Omnichannel.Application.DTOs.Notifications;
using Omnichannel.Application.Interfaces.Messages;
using Omnichannel.Application.Interfaces.Notifications;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services
{
    public class InternalMessageService : IInternalMessageService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InternalMessageService> _logger;
        private readonly INotificationService? _notificationService;

        public InternalMessageService(
            ApplicationDbContext context,
            ILogger<InternalMessageService> logger,
            INotificationService? notificationService = null)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<MessageFolderDto> GetInboxMessagesAsync(
            int userId,
            string? search = null,
            int page = 1,
            int pageSize = 15,
            CancellationToken cancellationToken = default)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 15 : pageSize;

            var query = _context.InternalMessages
                .Include(m => m.Sender)
                    .ThenInclude(s => s!.UserRoles)
                        .ThenInclude(ur => ur.Role)
                .Include(m => m.Receiver)
                .Where(m => m.ReceiverId == userId && !m.IsDeletedByReceiver)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(m => m.Subject.Contains(term) ||
                                         m.Body.Contains(term) ||
                                         (m.Sender != null && m.Sender.FullName.Contains(term)));
            }

            int totalCount = await query.CountAsync(cancellationToken);
            int unreadCount = await _context.InternalMessages
                .Where(m => m.ReceiverId == userId && !m.IsRead && !m.IsDeletedByReceiver)
                .CountAsync(cancellationToken);

            var messages = await query
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var items = messages.Select(m => MapToDto(m)).ToList();

            return new MessageFolderDto
            {
                Folder = "inbox",
                UnreadCount = unreadCount,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize,
                Items = items
            };
        }

        public async Task<MessageFolderDto> GetSentMessagesAsync(
            int userId,
            string? search = null,
            int page = 1,
            int pageSize = 15,
            CancellationToken cancellationToken = default)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 15 : pageSize;

            var query = _context.InternalMessages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                    .ThenInclude(r => r!.UserRoles)
                        .ThenInclude(ur => ur.Role)
                .Where(m => m.SenderId == userId && !m.IsDeletedBySender)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(m => m.Subject.Contains(term) ||
                                         m.Body.Contains(term) ||
                                         (m.Receiver != null && m.Receiver.FullName.Contains(term)));
            }

            int totalCount = await query.CountAsync(cancellationToken);
            int unreadCount = await _context.InternalMessages
                .Where(m => m.ReceiverId == userId && !m.IsRead && !m.IsDeletedByReceiver)
                .CountAsync(cancellationToken);

            var sentMessages = await query
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var items = sentMessages.Select(m => MapToDto(m)).ToList();

            return new MessageFolderDto
            {
                Folder = "sent",
                UnreadCount = unreadCount,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize,
                Items = items
            };
        }

        public async Task<InternalMessageDto?> GetMessageDetailsAsync(
            int messageId,
            int currentUserId,
            CancellationToken cancellationToken = default)
        {
            var message = await _context.InternalMessages
                .Include(m => m.Sender)
                    .ThenInclude(s => s!.UserRoles)
                        .ThenInclude(ur => ur.Role)
                .Include(m => m.Receiver)
                    .ThenInclude(r => r!.UserRoles)
                        .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

            if (message == null) return null;

            // Kiểm tra quyền xem: Phải là người gửi hoặc người nhận
            if (message.SenderId != currentUserId && message.ReceiverId != currentUserId)
            {
                return null;
            }

            // Nếu người nhận đang xem và thư chưa đọc -> đánh dấu đã đọc
            if (message.ReceiverId == currentUserId && !message.IsRead)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return MapToDto(message);
        }

        public async Task<InternalMessageDto> SendMessageAsync(
            int senderId,
            ComposeMessageDto dto,
            CancellationToken cancellationToken = default)
        {
            var sender = await _context.Users.FindAsync(new object[] { senderId }, cancellationToken);
            if (sender == null) throw new InvalidOperationException("Người gửi không tồn tại.");

            var receiver = await _context.Users.FindAsync(new object[] { dto.ReceiverId }, cancellationToken);
            if (receiver == null) throw new InvalidOperationException("Người nhận không tồn tại.");

            var message = new InternalMessage
            {
                SenderId = senderId,
                ReceiverId = dto.ReceiverId,
                Subject = dto.Subject?.Trim() ?? "(Không có tiêu đề)",
                Body = dto.Body?.Trim() ?? string.Empty,
                IsRead = false,
                SentAt = DateTime.UtcNow,
                IsDeletedBySender = false,
                IsDeletedByReceiver = false
            };

            await _context.InternalMessages.AddAsync(message, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Đã gửi thư nội bộ #{Id} từ {Sender} tới {Receiver}", message.Id, sender.Username, receiver.Username);

            // Bắn SignalR realtime notification tới người nhận
            if (_notificationService != null)
            {
                try
                {
                    await _notificationService.CreateAndSendNotificationAsync(new CreateNotificationDto
                    {
                        Type = "SYSTEM",
                        Severity = "INFO",
                        Title = $"✉️ Thư nội bộ mới từ {sender.FullName}",
                        Message = message.Subject,
                        UserId = receiver.Id,
                        Url = $"/Admin/Messages/View/{message.Id}"
                    }, cancellationToken);
                }
                catch (Exception notifEx)
                {
                    _logger.LogWarning(notifEx, "Không thể gửi SignalR notification cho thư mới #{Id}", message.Id);
                }
            }

            return MapToDto(message, sender.FullName, sender.AvatarUrl, receiver.FullName, receiver.AvatarUrl);
        }

        public async Task<bool> MarkAsReadAsync(int messageId, int currentUserId, CancellationToken cancellationToken = default)
        {
            var message = await _context.InternalMessages
                .FirstOrDefaultAsync(m => m.Id == messageId && m.ReceiverId == currentUserId, cancellationToken);

            if (message == null) return false;

            if (!message.IsRead)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return true;
        }

        public async Task<bool> DeleteMessageAsync(
            int messageId,
            int currentUserId,
            string folder = "inbox",
            CancellationToken cancellationToken = default)
        {
            var message = await _context.InternalMessages
                .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

            if (message == null) return false;

            if (folder.Equals("sent", StringComparison.OrdinalIgnoreCase) && message.SenderId == currentUserId)
            {
                message.IsDeletedBySender = true;
            }
            else if (message.ReceiverId == currentUserId)
            {
                message.IsDeletedByReceiver = true;
            }
            else
            {
                return false;
            }

            // Nếu cả 2 bên đều đã xóa -> xóa hẳn khỏi DB
            if (message.IsDeletedBySender && message.IsDeletedByReceiver)
            {
                _context.InternalMessages.Remove(message);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<int> GetUnreadMessageCountAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _context.InternalMessages
                .Where(m => m.ReceiverId == userId && !m.IsRead && !m.IsDeletedByReceiver)
                .CountAsync(cancellationToken);
        }

        public async Task<List<UserRecipientDto>> GetReceivableUsersAsync(int currentUserId, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.Store)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => u.IsActive && u.Id != currentUserId)
                .OrderBy(u => u.FullName)
                .Select(u => new UserRecipientDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Username = u.Username,
                    Email = u.Email,
                    StoreName = u.Store != null ? u.Store.Name : "Trụ Sở Chính (HQ)",
                    RoleName = u.UserRoles.Select(ur => ur.Role.Name).FirstOrDefault() ?? "Nhân viên",
                    AvatarUrl = u.AvatarUrl
                })
                .ToListAsync(cancellationToken);
        }

        private static InternalMessageDto MapToDto(
            InternalMessage m,
            string? senderName = null,
            string? senderAvatar = null,
            string? receiverName = null,
            string? receiverAvatar = null)
        {
            return new InternalMessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderName = senderName ?? m.Sender?.FullName ?? "Người dùng",
                SenderAvatar = senderAvatar ?? m.Sender?.AvatarUrl,
                SenderRole = m.Sender?.UserRoles?.Select(ur => ur.Role.Name).FirstOrDefault() ?? "Nhân viên",
                ReceiverId = m.ReceiverId,
                ReceiverName = receiverName ?? m.Receiver?.FullName ?? "Người nhận",
                ReceiverAvatar = receiverAvatar ?? m.Receiver?.AvatarUrl,
                ReceiverRole = m.Receiver?.UserRoles?.Select(ur => ur.Role.Name).FirstOrDefault() ?? "Nhân viên",
                Subject = m.Subject,
                Body = m.Body,
                IsRead = m.IsRead,
                SentAt = m.SentAt,
                ReadAt = m.ReadAt
            };
        }
    }
}
