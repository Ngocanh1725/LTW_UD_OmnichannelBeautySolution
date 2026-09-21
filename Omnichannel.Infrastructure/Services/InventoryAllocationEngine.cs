using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Inventory;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services
{
    public class InventoryAllocationEngine : IInventoryAllocationEngine
    {
        private readonly ApplicationDbContext _context;

        public InventoryAllocationEngine(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<bool> CheckAvailabilityAsync(string storeId, string sku, int quantity, CancellationToken cancellationToken = default)
        {
            if (quantity <= 0 || !int.TryParse(storeId, out var parsedStoreId) || parsedStoreId <= 0)
            {
                return false;
            }

            var today = DateTime.Today;
            int.TryParse(sku, out var parsedProductId);

            // Sửa si.Batch -> si.ProductBatch
            var query = _context.StoreInventories
                .AsNoTracking()
                .Include(si => si.ProductBatch)
                    .ThenInclude(b => b.Product)
                .Where(si => si.StoreId == parsedStoreId && si.ProductBatch.ExpDate > today);

            if (parsedProductId > 0)
            {
                query = query.Where(si => si.ProductBatch.ProductId == parsedProductId
                                       || si.ProductBatch.Product.Sku == sku
                                       || si.ProductBatch.Product.Barcode == sku);
            }
            else
            {
                query = query.Where(si => si.ProductBatch.Product.Sku == sku
                                       || si.ProductBatch.Product.Barcode == sku);
            }

            var availableTotal = await query.SumAsync(
                si => (int?)(si.PhysicalQuantity - si.ReservedQuantity),
                cancellationToken) ?? 0;

            return availableTotal >= quantity;
        }

        public async Task<List<AllocatedBatchDto>> AllocateBatchesFEFOAsync(string storeId, string sku, int quantity, CancellationToken cancellationToken = default)
        {
            if (quantity <= 0 || !int.TryParse(storeId, out var parsedStoreId) || parsedStoreId <= 0)
            {
                return new List<AllocatedBatchDto>();
            }

            var today = DateTime.Today;
            int.TryParse(sku, out var parsedProductId);

            // Sửa si.Batch -> si.ProductBatch
            var query = _context.StoreInventories
                .Include(si => si.ProductBatch)
                    .ThenInclude(b => b.Product)
                .Where(si => si.StoreId == parsedStoreId && si.ProductBatch.ExpDate > today);

            if (parsedProductId > 0)
            {
                query = query.Where(si => si.ProductBatch.ProductId == parsedProductId
                                       || si.ProductBatch.Product.Sku == sku
                                       || si.ProductBatch.Product.Barcode == sku);
            }
            else
            {
                query = query.Where(si => si.ProductBatch.Product.Sku == sku
                                       || si.ProductBatch.Product.Barcode == sku);
            }

            // FEFO: Ưu tiên lấy từ các lô có ngày hết hạn gần nhất trước
            var candidateBatches = await query
                .OrderBy(si => si.ProductBatch.ExpDate)
                .ToListAsync(cancellationToken);

            var allocations = new List<AllocatedBatchDto>();
            var remainingNeeded = quantity;

            foreach (var inv in candidateBatches)
            {
                var availableInBatch = inv.PhysicalQuantity - inv.ReservedQuantity;
                if (availableInBatch <= 0) continue;

                var take = Math.Min(remainingNeeded, availableInBatch);

                allocations.Add(new AllocatedBatchDto
                {
                    BatchId = inv.ProductBatch.Id,
                    BatchCode = inv.ProductBatch.BatchNumber,
                    BatchNumber = inv.ProductBatch.BatchNumber,
                    ProductId = inv.ProductBatch.ProductId,
                    ProductName = inv.ProductBatch.Product?.Name ?? "Mỹ phẩm",
                    Sku = inv.ProductBatch.Product?.Sku ?? sku,
                    ExpDate = inv.ProductBatch.ExpDate,
                    AllocatedQuantity = take,
                    UnitPrice = inv.ProductBatch.Product?.SellingPrice ?? 0
                });

                remainingNeeded -= take;
                if (remainingNeeded <= 0) break;
            }

            return allocations;
        }

        public async Task<bool> ReserveStockAsync(string storeId, string sku, int quantity, CancellationToken cancellationToken = default)
        {
            var allocations = await AllocateBatchesFEFOAsync(storeId, sku, quantity, cancellationToken);
            if (!allocations.Any() || allocations.Sum(a => a.AllocatedQuantity) < quantity)
            {
                return false;
            }

            return await ReserveStockAsync(storeId, allocations, cancellationToken);
        }

        public async Task<bool> ReserveStockAsync(string storeId, List<AllocatedBatchDto> allocations, CancellationToken cancellationToken = default)
        {
            if (!int.TryParse(storeId, out var parsedStoreId) || allocations == null || !allocations.Any())
            {
                return false;
            }

            foreach (var alloc in allocations)
            {
                var inv = await _context.StoreInventories
                    .FirstOrDefaultAsync(si => si.StoreId == parsedStoreId && si.ProductBatchId == alloc.BatchId, cancellationToken);

                if (inv != null)
                {
                    inv.ReservedQuantity += alloc.AllocatedQuantity;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> ReleaseReservedStockAsync(string storeId, List<AllocatedBatchDto> allocations, CancellationToken cancellationToken = default)
        {
            if (!int.TryParse(storeId, out var parsedStoreId) || allocations == null || !allocations.Any())
            {
                return false;
            }

            foreach (var alloc in allocations)
            {
                var inv = await _context.StoreInventories
                    .FirstOrDefaultAsync(si => si.StoreId == parsedStoreId && si.ProductBatchId == alloc.BatchId, cancellationToken);

                if (inv != null)
                {
                    inv.ReservedQuantity = Math.Max(0, inv.ReservedQuantity - alloc.AllocatedQuantity);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> DeductStockAsync(string storeId, List<AllocatedBatchDto> allocations, string reason = "SALE", CancellationToken cancellationToken = default)
        {
            if (!int.TryParse(storeId, out var parsedStoreId) || allocations == null || !allocations.Any())
            {
                return false;
            }

            foreach (var alloc in allocations)
            {
                var inv = await _context.StoreInventories
                    .FirstOrDefaultAsync(si => si.StoreId == parsedStoreId && si.ProductBatchId == alloc.BatchId, cancellationToken);

                if (inv != null)
                {
                    inv.PhysicalQuantity = Math.Max(0, inv.PhysicalQuantity - alloc.AllocatedQuantity);
                    inv.ReservedQuantity = Math.Max(0, inv.ReservedQuantity - alloc.AllocatedQuantity);

                    _context.InventoryLogs.Add(new InventoryLog
                    {
                        StoreId = parsedStoreId,
                        ProductBatchId = alloc.BatchId,
                        TransactionType = "ADJUSTMENT",
                        QuantityChanged = -alloc.AllocatedQuantity,
                        QuantityBefore = inv.PhysicalQuantity + alloc.AllocatedQuantity,
                        QuantityAfter = inv.PhysicalQuantity,
                        Note = reason,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}