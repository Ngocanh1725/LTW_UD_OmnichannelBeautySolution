using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Omnichannel.Domain.Common;
using Omnichannel.Domain.Entities;

namespace Omnichannel.Infrastructure.Data.Interceptors
{
    /// <summary>
    /// Interceptor tự động đánh chặn quá trình SaveChangesAsync của EF Core để thu thập các Domain Events
    /// từ các Aggregate Root và ghi vào bảng OutboxMessages trong cùng một Transaction (Transactional Outbox Pattern).
    /// </summary>
    public class DomainEventDispatchInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            ProcessDomainEvents(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            ProcessDomainEvents(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private static void ProcessDomainEvents(DbContext? context)
        {
            if (context == null) return;

            var aggregates = context.ChangeTracker
                .Entries<AggregateRoot>()
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();

            if (!aggregates.Any()) return;

            var domainEvents = aggregates.SelectMany(a => a.DomainEvents).ToList();

            // Xóa sự kiện khỏi thực thể để tránh trùng lặp
            aggregates.ForEach(a => a.ClearDomainEvents());

            var outboxSet = context.Set<OutboxMessage>();
            foreach (var domainEvent in domainEvents)
            {
                var outboxMessage = new OutboxMessage
                {
                    EventType = domainEvent.GetType().Name,
                    PayloadJson = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                    CreatedAtUtc = DateTime.UtcNow,
                    RetryCount = 0
                };

                outboxSet.Add(outboxMessage);
            }
        }
    }
}
