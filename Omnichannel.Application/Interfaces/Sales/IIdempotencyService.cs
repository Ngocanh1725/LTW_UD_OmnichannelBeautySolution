using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Domain.Entities;

namespace Omnichannel.Application.Interfaces.Sales
{
    public interface IIdempotencyService
    {
        Task<bool> HasBeenProcessedAsync(string idempotencyKey, CancellationToken cancellationToken = default);
        Task<IdempotencyRecord?> GetRecordAsync(string idempotencyKey, CancellationToken cancellationToken = default);
        Task<IdempotencyRecord> RecordSuccessAsync(
            string idempotencyKey,
            string requestPath,
            string? requestPayloadHash = null,
            int statusCode = 200,
            string? responseBodyJson = null,
            CancellationToken cancellationToken = default);
    }
}
