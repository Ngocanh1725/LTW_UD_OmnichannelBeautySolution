using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Notifications;
using Omnichannel.Application.Interfaces.Notifications;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;
        private readonly INotificationDispatcher? _dispatcher;

        public NotificationService(
            ApplicationDbContext context,
            ILogger<NotificationService> logger,
            INotificationDispatcher? dispatcher = null)
        {
            _context = context;
            _logger = logger;
            _dispatcher = dispatcher;
        }

        public async Task<NotificationDto> CreateAndSendNotificationAsync(CreateNotificationDto dto, CancellationToken cancellationToken = default)
        {
            var notification = new Notification
            {
                UserId = dto.UserId,
                Role = dto.Role,
                StoreId = dto.StoreId,
                Type = string.IsNullOrWhiteSpace(dto.Type) ? "SYSTEM" : dto.Type.ToUpperInvariant(),
                Severity = string.IsNullOrWhiteSpace(dto.Severity) ? "INFO" : dto.Severity.ToUpperInvariant(),
                Title = dto.Title,
                Message = dto.Message,
                Url = dto.Url,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Notifications.AddAsync(notification, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            string? storeName = null;
            if (dto.StoreId.HasValue)
            {
                storeName = await _context.Stores
                    .Where(s => s.Id == dto.StoreId.Value)
                    .Select(s => s.Name)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            var resultDto = MapToDto(notification, storeName);

            // Dispatch SignalR realtime notification
            if (_dispatcher != null)
            {
                await _dispatcher.DispatchNotificationAsync(resultDto, cancellationToken);
            }

            return resultDto;
        }

        public async Task<PagedNotificationResultDto> GetNotificationsAsync(
            NotificationFilterDto filter,
            int? currentUserId = null,
            string? currentRole = null,
            int? currentStoreId = null,
            CancellationToken cancellationToken = default)
        {
            var query = BuildTargetedQuery(currentUserId, currentRole, currentStoreId);

            // Calculate category counts
            var allMatching = await query.AsNoTracking().ToListAsync(cancellationToken);
            int totalCount = allMatching.Count;
            int unreadCount = allMatching.Count(n => !n.IsRead);
            int orderCount = allMatching.Count(n => n.Type == "ORDER");
            int warehouseCount = allMatching.Count(n => n.Type == "WAREHOUSE");
            int systemCount = allMatching.Count(n => n.Type == "SYSTEM" || n.Type == "ALERT");

            // Apply Tab filter
            switch (filter.Tab?.ToUpperInvariant())
            {
                case "UNREAD":
                    query = query.Where(n => !n.IsRead);
                    break;
                case "ORDER":
                    query = query.Where(n => n.Type == "ORDER");
                    break;
                case "WAREHOUSE":
                    query = query.Where(n => n.Type == "WAREHOUSE");
                    break;
                case "SYSTEM":
                    query = query.Where(n => n.Type == "SYSTEM" || n.Type == "ALERT");
                    break;
            }

            if (!string.IsNullOrEmpty(filter.Type))
            {
                query = query.Where(n => n.Type == filter.Type);
            }

            if (!string.IsNullOrEmpty(filter.Severity))
            {
                query = query.Where(n => n.Severity == filter.Severity);
            }

            if (filter.IsRead.HasValue)
            {
                query = query.Where(n => n.IsRead == filter.IsRead.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim();
                query = query.Where(n => n.Title.Contains(term) || n.Message.Contains(term));
            }

            int filteredTotal = await query.CountAsync(cancellationToken);

            int pageNumber = filter.PageNumber <= 0 ? 1 : filter.PageNumber;
            int pageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;

            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    UserId = n.UserId,
                    Role = n.Role,
                    StoreId = n.StoreId,
                    StoreName = n.Store != null ? n.Store.Name : null,
                    Type = n.Type,
                    Severity = n.Severity,
                    Title = n.Title,
                    Message = n.Message,
                    Url = n.Url,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    ReadAt = n.ReadAt
                })
                .ToListAsync(cancellationToken);

            return new PagedNotificationResultDto
            {
                Items = items,
                TotalItems = filteredTotal,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                UnreadCount = unreadCount,
                OrderCount = orderCount,
                WarehouseCount = warehouseCount,
                SystemCount = systemCount
            };
        }

        public async Task<List<NotificationDto>> GetLatestNotificationsAsync(
            int take = 10,
            int? currentUserId = null,
            string? currentRole = null,
            int? currentStoreId = null,
            CancellationToken cancellationToken = default)
        {
            var query = BuildTargetedQuery(currentUserId, currentRole, currentStoreId);

            return await query
                .AsNoTracking()
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    UserId = n.UserId,
                    Role = n.Role,
                    StoreId = n.StoreId,
                    StoreName = n.Store != null ? n.Store.Name : null,
                    Type = n.Type,
                    Severity = n.Severity,
                    Title = n.Title,
                    Message = n.Message,
                    Url = n.Url,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    ReadAt = n.ReadAt
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetUnreadCountAsync(
            int? currentUserId = null,
            string? currentRole = null,
            int? currentStoreId = null,
            CancellationToken cancellationToken = default)
        {
            var query = BuildTargetedQuery(currentUserId, currentRole, currentStoreId);
            return await query.CountAsync(n => !n.IsRead, cancellationToken);
        }

        public async Task<bool> MarkAsReadAsync(int id, int? currentUserId = null, CancellationToken cancellationToken = default)
        {
            var notification = await _context.Notifications.FindAsync(new object[] { id }, cancellationToken);
            if (notification == null) return false;

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
            return true;
        }

        public async Task<int> MarkAllAsReadAsync(
            int? currentUserId = null,
            string? currentRole = null,
            int? currentStoreId = null,
            CancellationToken cancellationToken = default)
        {
            var query = BuildTargetedQuery(currentUserId, currentRole, currentStoreId)
                .Where(n => !n.IsRead);

            var unreadList = await query.ToListAsync(cancellationToken);
            if (!unreadList.Any()) return 0;

            var now = DateTime.UtcNow;
            foreach (var n in unreadList)
            {
                n.IsRead = true;
                n.ReadAt = now;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return unreadList.Count;
        }

        public async Task<bool> DeleteNotificationAsync(int id, CancellationToken cancellationToken = default)
        {
            var notification = await _context.Notifications.FindAsync(new object[] { id }, cancellationToken);
            if (notification == null) return false;

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<int> ClearAllReadAsync(
            int? currentUserId = null,
            string? currentRole = null,
            int? currentStoreId = null,
            CancellationToken cancellationToken = default)
        {
            var query = BuildTargetedQuery(currentUserId, currentRole, currentStoreId)
                .Where(n => n.IsRead);

            var readList = await query.ToListAsync(cancellationToken);
            if (!readList.Any()) return 0;

            _context.Notifications.RemoveRange(readList);
            await _context.SaveChangesAsync(cancellationToken);
            return readList.Count;
        }

        private IQueryable<Notification> BuildTargetedQuery(int? currentUserId, string? currentRole, int? currentStoreId)
        {
            var query = _context.Notifications
                .Include(n => n.Store)
                .AsQueryable();

            // Nếu là SuperAdmin: có thể xem toàn bộ thông báo hệ thống
            if (string.Equals(currentRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(currentRole, "SUPER_ADMIN", StringComparison.OrdinalIgnoreCase))
            {
                return query;
            }

            // Nếu không phải SuperAdmin: Lọc theo đích danh User, Store + Role, hoặc Broadcast
            query = query.Where(n =>
                // 1. Gửi đích danh cho user này
                (n.UserId.HasValue && currentUserId.HasValue && n.UserId == currentUserId.Value) ||
                // 2. Không chỉ định userId cụ thể:
                (!n.UserId.HasValue && (
                    // 2a. Broadcast toàn hệ thống (không role, không store)
                    (n.Role == null && n.StoreId == null) ||
                    // 2b. Có StoreId: user phải cùng store (và nếu có Role thì phải khớp Role)
                    (n.StoreId.HasValue && currentStoreId.HasValue && n.StoreId == currentStoreId.Value &&
                        (n.Role == null || (!string.IsNullOrEmpty(currentRole) && n.Role == currentRole))) ||
                    // 2c. Không có StoreId nhưng có Role: user phải cùng Role
                    (!n.StoreId.HasValue && n.Role != null && !string.IsNullOrEmpty(currentRole) && n.Role == currentRole)
                ))
            );

            return query;
        }

        private static NotificationDto MapToDto(Notification n, string? storeName)
        {
            return new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Role = n.Role,
                StoreId = n.StoreId,
                StoreName = storeName,
                Type = n.Type,
                Severity = n.Severity,
                Title = n.Title,
                Message = n.Message,
                Url = n.Url,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                ReadAt = n.ReadAt
            };
        }
    }
}
