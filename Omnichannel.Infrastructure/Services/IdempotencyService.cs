using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services
{
    public class IdempotencyService : IIdempotencyService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IdempotencyService> _logger;

        public IdempotencyService(ApplicationDbContext context, ILogger<IdempotencyService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> HasBeenProcessedAsync(string idempotencyKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey)) return false;

            return await _context.IdempotencyRecords
                .AsNoTracking()
                .AnyAsync(r => r.IdempotencyKey == idempotencyKey, cancellationToken);
        }

        public async Task<IdempotencyRecord?> GetRecordAsync(string idempotencyKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey)) return null;

            return await _context.IdempotencyRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey, cancellationToken);
        }

        public async Task<IdempotencyRecord> RecordSuccessAsync(
            string idempotencyKey,
            string requestPath,
            string? requestPayloadHash = null,
            int statusCode = 200,
            string? responseBodyJson = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                throw new ArgumentException("Idempotency key cannot be empty.", nameof(idempotencyKey));
            }

            var existing = await _context.IdempotencyRecords
                .FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey, cancellationToken);

            if (existing != null)
            {
                _logger.LogInformation("Idempotency record {Key} already exists. Returning existing record.", idempotencyKey);
                return existing;
            }

            var record = new IdempotencyRecord
            {
                IdempotencyKey = idempotencyKey.Trim(),
                RequestPath = requestPath ?? string.Empty,
                RequestPayloadHash = requestPayloadHash ?? string.Empty,
                ResponseStatusCode = statusCode,
                ResponseBodyJson = responseBodyJson,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(48)
            };

            await _context.IdempotencyRecords.AddAsync(record, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Saved new idempotency record: Key={Key}, Path={Path}, Status={Status}",
                record.IdempotencyKey, record.RequestPath, record.ResponseStatusCode);

            return record;
        }
    }
}
