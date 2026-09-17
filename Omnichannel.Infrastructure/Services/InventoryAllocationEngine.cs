using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Inventory;
using Omnichannel.Application.Interfaces.Inventory;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services
{
    public class InventoryAllocationEngine : IInventoryAllocationEngine
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InventoryAllocationEngine> _logger;

        public InventoryAllocationEngine(ApplicationDbContext context, ILogger<InventoryAllocationEngine> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> CheckAvailabilityAsync(string storeId, string sku, int quantity, CancellationToken cancellationToken = default)
        {
            if (quantity <= 0) return false;

            var today = DateTime.Today;

            // Tính tổng tồn khả dụng của các lô chưa hết hạn
            var availableTotal = await _context.StoreInventories
                .AsNoTracking()
                .Where(si => si.StoreId == storeId
                          && si.Batch.ProductId == sku
                          && si.Batch.ExpDate > today)
                .SumAsync(si => (int?)(si.PhysicalQuantity - si.ReservedQuantity), cancellationToken) ?? 0;

            return availableTotal >= quantity;
        }

        public async Task<List<AllocatedBatchDto>> AllocateBatchesFEFOAsync(string storeId, string sku, int quantity, CancellationToken cancellationToken = default)
        {
            if (quantity <= 0) return new List<AllocatedBatchDto>();

            var today = DateTime.Today;

            // Lấy các lô hàng còn tồn khả dụng, ưu tiên hạn dùng gần nhất (ExpDate ASC)
            var availableBatches = await _context.StoreInventories
                .Include(si => si.Batch)
                .Where(si => si.StoreId == storeId
                          && si.Batch.ProductId == sku
                          && si.Batch.ExpDate > today
                          && (si.PhysicalQuantity - si.ReservedQuantity) > 0)
                .OrderBy(si => si.Batch.ExpDate)
                .ToListAsync(cancellationToken);

            int remainingToAllocate = quantity;
            var allocatedList = new List<AllocatedBatchDto>();

            foreach (var inventory in availableBatches)
            {
                int batchAvailable = inventory.PhysicalQuantity - inventory.ReservedQuantity;
                int takeQty = Math.Min(batchAvailable, remainingToAllocate);

                if (takeQty > 0)
                {
                    allocatedList.Add(new AllocatedBatchDto
                    {
                        BatchId = inventory.BatchId,
                        BatchNumber = inventory.Batch.BatchNumber,
                        ExpDate = inventory.Batch.ExpDate,
                        AllocatedQuantity = takeQty,
                        AvailableQuantity = batchAvailable
                    });

                    remainingToAllocate -= takeQty;
                }

                if (remainingToAllocate == 0)
                    break;
            }

            if (remainingToAllocate > 0)
            {
                _logger.LogWarning("Không đủ tồn kho khả dụng theo FEFO cho SKU {Sku} tại Kho {StoreId}. Còn thiếu: {MissingQty}",
                    sku, storeId, remainingToAllocate);
                return new List<AllocatedBatchDto>(); // Không thể phân bổ trọn vẹn
            }

            return allocatedList;
        }

        public async Task<bool> ImportBatchAsync(ImportBatchViewModel model, string staffUserId, CancellationToken cancellationToken = default)
        {
            if (model.ExpDate <= model.ManufacturingDate)
            {
                _logger.LogWarning("Ngày hết hạn phải lớn hơn ngày sản xuất.");
                return false;
            }

            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                string cleanBatchNumber = model.BatchNumber.Trim().ToUpperInvariant();

                // 1. Tìm hoặc tạo mới Lô hàng (ProductBatch)
                var batch = await _context.ProductBatches
                    .FirstOrDefaultAsync(b => b.ProductId == model.ProductId && b.BatchNumber == cleanBatchNumber, cancellationToken);

                if (batch == null)
                {
                    batch = new ProductBatch
                    {
                        ProductId = model.ProductId,
                        BatchNumber = cleanBatchNumber,
                        ManufacturingDate = model.ManufacturingDate.Date,
                        ExpDate = model.ExpDate.Date,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _context.ProductBatches.AddAsync(batch, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                // 2. Cập nhật hoặc tạo mới bản ghi tồn kho tại chi nhánh (StoreInventory)
                var inventory = await _context.StoreInventories
                    .FirstOrDefaultAsync(si => si.StoreId == model.StoreId && si.BatchId == batch.BatchId, cancellationToken);

                if (inventory == null)
                {
                    inventory = new StoreInventory
                    {
                        StoreId = model.StoreId,
                        BatchId = batch.BatchId,
                        PhysicalQuantity = model.Quantity,
                        ReservedQuantity = 0,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _context.StoreInventories.AddAsync(inventory, cancellationToken);
                }
                else
                {
                    inventory.PhysicalQuantity += model.Quantity;
                    inventory.UpdatedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync(cancellationToken);

                // 3. Ghi sổ cái nhật ký kho bất biến (InventoryLogs)
                var log = new InventoryLog
                {
                    StoreId = model.StoreId,
                    BatchId = batch.BatchId,
                    TransactionType = 1, // 1: Nhập NCC
                    ReferenceDocNo = !string.IsNullOrWhiteSpace(model.ReferenceDocNo) ? model.ReferenceDocNo.Trim() : $"PO-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    QuantityChange = model.Quantity,
                    RemainingQuantity = inventory.PhysicalQuantity,
                    CreatedBy = staffUserId,
                    CreatedAt = DateTime.UtcNow,
                    Notes = model.Notes?.Trim() ?? "Nhập kho theo Lô sản xuất"
                };

                await _context.InventoryLogs.AddAsync(log, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Đã nhập kho thành công {Qty} sp SKU {Sku}, Lô {Batch} vào Kho {StoreId}.",
                    model.Quantity, model.ProductId, cleanBatchNumber, model.StoreId);

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Lỗi xảy ra khi thực hiện nhập kho theo Lô.");
                return false;
            }
        }

        public async Task<bool> AdjustStockAsync(StockAdjustmentViewModel model, string staffUserId, CancellationToken cancellationToken = default)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var inventory = await _context.StoreInventories
                    .FirstOrDefaultAsync(si => si.StoreId == model.StoreId && si.BatchId == model.BatchId, cancellationToken);

                if (inventory == null) return false;

                // Kiểm tra tránh âm kho
                if (inventory.PhysicalQuantity + model.QuantityChange < inventory.ReservedQuantity)
                {
                    _logger.LogWarning("Không thể điều chỉnh vì tồn vật lý sẽ nhỏ hơn tồn đã đặt cọc (ReservedQuantity).");
                    return false;
                }

                inventory.PhysicalQuantity += model.QuantityChange;
                inventory.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                // Ghi sổ cái bất biến
                var log = new InventoryLog
                {
                    StoreId = model.StoreId,
                    BatchId = model.BatchId,
                    TransactionType = model.TransactionType,
                    ReferenceDocNo = $"ADJ-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    QuantityChange = model.QuantityChange,
                    RemainingQuantity = inventory.PhysicalQuantity,
                    CreatedBy = staffUserId,
                    CreatedAt = DateTime.UtcNow,
                    Notes = model.Notes.Trim()
                };

                await _context.InventoryLogs.AddAsync(log, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Lỗi xảy ra khi điều chỉnh tồn kho.");
                return false;
            }
        }
    }
}
