using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Omnichannel.Domain.Entities;

namespace Omnichannel.Infrastructure.Data.Interceptors
{
    public class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            CaptureAndAddAuditLogs(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            CaptureAndAddAuditLogs(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void CaptureAndAddAuditLogs(DbContext? context)
        {
            if (context == null) return;

            var httpContext = _httpContextAccessor?.HttpContext;
            int? currentUserId = null;
            string? currentUserName = null;
            string? staffCode = null;
            string? ipAddress = null;
            string? userAgent = null;

            if (httpContext != null)
            {
                var uidClaim = httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(uidClaim, out var uid)) currentUserId = uid;

                currentUserName = httpContext.User?.Identity?.Name;
                staffCode = httpContext.User?.FindFirst("StaffCode")?.Value ?? currentUserName ?? "SYSTEM";
                ipAddress = httpContext.Connection?.RemoteIpAddress?.ToString();
                userAgent = httpContext.Request?.Headers["User-Agent"].ToString();
            }

            var entries = context.ChangeTracker.Entries()
                .Where(e => !(e.Entity is AuditLog) &&
                            e.State != EntityState.Detached &&
                            e.State != EntityState.Unchanged)
                .ToList();

            if (entries.Count == 0) return;

            var auditLogsToAdd = new List<AuditLog>();

            foreach (var entry in entries)
            {
                var entityType = entry.Entity.GetType().Name;
                if (entityType.EndsWith("Proxy") || entityType == "AuditLog") continue;

                string action;
                var oldValues = new Dictionary<string, object?>();
                var newValues = new Dictionary<string, object?>();
                string recordId = "0";

                switch (entry.State)
                {
                    case EntityState.Added:
                        action = "CREATE";
                        foreach (var prop in entry.Properties)
                        {
                            if (prop.Metadata.IsPrimaryKey())
                            {
                                recordId = prop.CurrentValue?.ToString() ?? "0";
                            }
                            newValues[prop.Metadata.Name] = prop.CurrentValue;
                        }
                        break;

                    case EntityState.Deleted:
                        action = "DELETE";
                        foreach (var prop in entry.Properties)
                        {
                            if (prop.Metadata.IsPrimaryKey())
                            {
                                recordId = prop.OriginalValue?.ToString() ?? "0";
                            }
                            oldValues[prop.Metadata.Name] = prop.OriginalValue;
                        }
                        break;

                    case EntityState.Modified:
                        action = "UPDATE";
                        bool hasRealChanges = false;
                        foreach (var prop in entry.Properties)
                        {
                            if (prop.Metadata.IsPrimaryKey())
                            {
                                recordId = prop.CurrentValue?.ToString() ?? "0";
                            }
                            if (prop.IsModified)
                            {
                                oldValues[prop.Metadata.Name] = prop.OriginalValue;
                                newValues[prop.Metadata.Name] = prop.CurrentValue;
                                hasRealChanges = true;
                            }
                        }
                        if (!hasRealChanges) continue;
                        break;

                    default:
                        continue;
                }

                string? oldValuesJson = oldValues.Count > 0 ? JsonSerializer.Serialize(oldValues) : null;
                string? newValuesJson = newValues.Count > 0 ? JsonSerializer.Serialize(newValues) : null;

                var auditLog = new AuditLog
                {
                    UserId = currentUserId,
                    UserName = currentUserName ?? "SYSTEM",
                    StaffCode = staffCode ?? "SYSTEM",
                    Action = action,
                    EntityType = entityType,
                    EntityName = entityType,
                    EntityId = recordId,
                    RecordId = recordId,
                    OldValues = oldValuesJson,
                    OldValuesJson = oldValuesJson,
                    NewValues = newValuesJson,
                    NewValuesJson = newValuesJson,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    CreatedAt = DateTime.UtcNow
                };

                auditLogsToAdd.Add(auditLog);
            }

            if (auditLogsToAdd.Count > 0)
            {
                context.Set<AuditLog>().AddRange(auditLogsToAdd);
            }
        }
    }
}

