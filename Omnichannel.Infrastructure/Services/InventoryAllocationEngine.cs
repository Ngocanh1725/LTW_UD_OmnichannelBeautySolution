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

        public Task<List<AllocatedBatchDto>> AllocateBatchesFEFOAsync(string storeId, string sku, int quantity, CancellationToken cancellationToken = default)
        {
            return AllocateBatchesFEFOAsync(storeId, sku, quantity, allowNearExpiration: false, cancellationToken);
        }

        public async Task<List<AllocatedBatchDto>> AllocateBatchesFEFOAsync(
            string storeId, 
            string sku, 
            int quantity, 
            bool allowNearExpiration, 
            CancellationToken cancellationToken = default)
        {
            if (quantity <= 0 || !int.TryParse(storeId, out var parsedStoreId) || parsedStoreId <= 0)
            {
                return new List<AllocatedBatchDto>();
            }

            var today = DateTime.Today;
            int.TryParse(sku, out var parsedProductId);

            var baseQuery = _context.StoreInventories
                .Include(si => si.ProductBatch)
                    .ThenInclude(b => b.Product)
                .Where(si => si.StoreId == parsedStoreId && si.ProductBatch.ExpDate > today);

            if (parsedProductId > 0)
            {
                baseQuery = baseQuery.Where(si => si.ProductBatch.ProductId == parsedProductId
                                               || si.ProductBatch.Product.Sku == sku
                                               || si.ProductBatch.Product.Barcode == sku);
            }
            else
            {
                baseQuery = baseQuery.Where(si => si.ProductBatch.Product.Sku == sku
                                               || si.ProductBatch.Product.Barcode == sku);
            }

            // FEFO: Ưu tiên lấy từ các lô an toàn (≥ 60 ngày) trừ khi đơn cho phép hàng cận date
            List<StoreInventory> candidateBatches;
            var safeThresholdDate = today.AddDays(60);

            if (!allowNearExpiration)
            {
                // Tìm các lô an toàn trước
                candidateBatches = await baseQuery
                    .Where(si => si.ProductBatch.ExpDate >= safeThresholdDate)
                    .OrderBy(si => si.ProductBatch.ExpDate)
                    .ToListAsync(cancellationToken);

                // Nếu các lô an toàn không đủ nhưng còn lô cận date hợp lệ, bổ sung thêm nếu cần
                int safeAvailable = candidateBatches.Sum(si => Math.Max(0, si.PhysicalQuantity - si.ReservedQuantity));
                if (safeAvailable < quantity)
                {
                    var nearExpiryBatches = await baseQuery
                        .Where(si => si.ProductBatch.ExpDate < safeThresholdDate)
                        .OrderBy(si => si.ProductBatch.ExpDate)
                        .ToListAsync(cancellationToken);

                    candidateBatches.AddRange(nearExpiryBatches);
                }
            }
            else
            {
                // Cho phép xuất toàn bộ theo hạn dùng tăng dần
                candidateBatches = await baseQuery
                    .OrderBy(si => si.ProductBatch.ExpDate)
                    .ToListAsync(cancellationToken);
            }

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

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                foreach (var alloc in allocations)
                {
                    var inv = await _context.StoreInventories
                        .FirstOrDefaultAsync(si => si.StoreId == parsedStoreId && si.ProductBatchId == alloc.BatchId, cancellationToken);

                    if (inv == null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return false;
                    }

                    int available = inv.PhysicalQuantity - inv.ReservedQuantity;
                    if (available < alloc.AllocatedQuantity)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return false;
                    }

                    inv.ReservedQuantity += alloc.AllocatedQuantity;
                    inv.LastUpdated = DateTime.UtcNow;
                }

                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }

        public async Task<bool> ReleaseReservedStockAsync(string storeId, List<AllocatedBatchDto> allocations, CancellationToken cancellationToken = default)
        {
            if (!int.TryParse(storeId, out var parsedStoreId) || allocations == null || !allocations.Any())
            {
                return false;
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                foreach (var alloc in allocations)
                {
                    var inv = await _context.StoreInventories
                        .FirstOrDefaultAsync(si => si.StoreId == parsedStoreId && si.ProductBatchId == alloc.BatchId, cancellationToken);

                    if (inv != null)
                    {
                        inv.ReservedQuantity = Math.Max(0, inv.ReservedQuantity - alloc.AllocatedQuantity);
                        inv.LastUpdated = DateTime.UtcNow;
                    }
                }

                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }

        public async Task<bool> DeductStockAsync(string storeId, List<AllocatedBatchDto> allocations, string reason = "SALE", CancellationToken cancellationToken = default)
        {
            if (!int.TryParse(storeId, out var parsedStoreId) || allocations == null || !allocations.Any())
            {
                return false;
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                foreach (var alloc in allocations)
                {
                    var inv = await _context.StoreInventories
                        .Include(si => si.ProductBatch)
                        .FirstOrDefaultAsync(si => si.StoreId == parsedStoreId && si.ProductBatchId == alloc.BatchId, cancellationToken);

                    if (inv != null)
                    {
                        int beforeQty = inv.PhysicalQuantity;
                        inv.PhysicalQuantity = Math.Max(0, inv.PhysicalQuantity - alloc.AllocatedQuantity);
                        inv.ReservedQuantity = Math.Max(0, inv.ReservedQuantity - alloc.AllocatedQuantity);
                        inv.LastUpdated = DateTime.UtcNow;

                        if (inv.ProductBatch != null)
                        {
                            inv.ProductBatch.CurrentQuantity = Math.Max(0, inv.ProductBatch.CurrentQuantity - alloc.AllocatedQuantity);
                            if (inv.ProductBatch.IsNearExpiration(60))
                            {
                                inv.ProductBatch.Status = "NEAR_EXPIRATION";
                                inv.ProductBatch.AddDomainEvent(new Omnichannel.Domain.Common.BatchNearExpiryDomainEvent(
                                    inv.ProductBatch.Id,
                                    inv.ProductBatch.BatchNumber,
                                    inv.ProductBatch.ProductId,
                                    Math.Max(0, (int)(inv.ProductBatch.ExpDate.Date - DateTime.UtcNow.Date).TotalDays),
                                    inv.ProductBatch.CurrentQuantity));
                            }
                            else if (inv.ProductBatch.CurrentQuantity == 0)
                            {
                                inv.ProductBatch.Status = "DEPLETED";
                            }
                        }

                        _context.InventoryLogs.Add(new InventoryLog
                        {
                            StoreId = parsedStoreId,
                            ProductBatchId = alloc.BatchId,
                            TransactionType = string.IsNullOrWhiteSpace(reason) ? "ONLINE_SALE" : reason,
                            QuantityChanged = -alloc.AllocatedQuantity,
                            QuantityBefore = beforeQty,
                            QuantityAfter = inv.PhysicalQuantity,
                            Note = $"Xuất kho bán hàng lô {alloc.BatchNumber} (HSD: {alloc.ExpDate:dd/MM/yyyy}). Lý do: {reason}",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }
    }
}