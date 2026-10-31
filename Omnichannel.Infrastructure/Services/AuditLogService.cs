using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Security;
using Omnichannel.Application.Interfaces.Security;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(ApplicationDbContext context, ILogger<AuditLogService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task LogAsync(
            string action,
            string entityType,
            string entityId,
            string? oldValues = null,
            string? newValues = null,
            int? userId = null,
            string? userName = null,
            string? ipAddress = null,
            string? userAgent = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    Action = action?.ToUpperInvariant() ?? "UNKNOWN",
                    EntityType = entityType ?? string.Empty,
                    EntityId = entityId ?? string.Empty,
                    OldValues = oldValues,
                    NewValues = newValues,
                    UserId = userId,
                    UserName = userName,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    CreatedAt = DateTime.UtcNow
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi ghi AuditLog cho thao tác {Action} trên {EntityType}:{EntityId}", action, entityType, entityId);
            }
        }

        public async Task<List<AuditLogDto>> GetLogsAsync(
            string? entityType = null,
            int? userId = null,
            DateTime? from = null,
            DateTime? to = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var query = _context.AuditLogs.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(entityType))
            {
                query = query.Where(a => a.EntityType == entityType);
            }

            if (userId.HasValue)
            {
                query = query.Where(a => a.UserId == userId.Value);
            }

            if (from.HasValue)
            {
                query = query.Where(a => a.CreatedAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(a => a.CreatedAt <= to.Value);
            }

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            return await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    UserName = a.UserName,
                    Action = a.Action,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    OldValues = a.OldValues,
                    NewValues = a.NewValues,
                    IpAddress = a.IpAddress,
                    UserAgent = a.UserAgent,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountLogsAsync(
            string? entityType = null,
            int? userId = null,
            DateTime? from = null,
            DateTime? to = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.AuditLogs.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(entityType))
            {
                query = query.Where(a => a.EntityType == entityType);
            }

            if (userId.HasValue)
            {
                query = query.Where(a => a.UserId == userId.Value);
            }

            if (from.HasValue)
            {
                query = query.Where(a => a.CreatedAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(a => a.CreatedAt <= to.Value);
            }

            return await query.CountAsync(cancellationToken);
        }
    }
}
