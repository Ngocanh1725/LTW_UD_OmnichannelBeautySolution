using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Omnichannel.Application.DTOs.Inventory
{
    public interface IInventoryAllocationEngine
    {
        Task<bool> CheckAvailabilityAsync(string storeId, string sku, int quantity, CancellationToken cancellationToken = default);
        Task<List<AllocatedBatchDto>> AllocateBatchesFEFOAsync(string storeId, string sku, int quantity, CancellationToken cancellationToken = default);
        Task<bool> ReserveStockAsync(string storeId, string sku, int quantity, CancellationToken cancellationToken = default);
        Task<bool> ReserveStockAsync(string storeId, List<AllocatedBatchDto> allocations, CancellationToken cancellationToken = default);
        Task<bool> ReleaseReservedStockAsync(string storeId, List<AllocatedBatchDto> allocations, CancellationToken cancellationToken = default);
        Task<bool> DeductStockAsync(string storeId, List<AllocatedBatchDto> allocations, string reason = "SALE", CancellationToken cancellationToken = default);
    }
}