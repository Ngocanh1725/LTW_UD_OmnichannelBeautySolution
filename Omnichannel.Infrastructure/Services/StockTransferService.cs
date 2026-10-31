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

using Omnichannel.Application.Interfaces.Notifications;
using Omnichannel.Application.DTOs.Notifications;
using Omnichannel.Domain.Common;

namespace Omnichannel.Infrastructure.Services
{
    public class StockTransferService : IStockTransferService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StockTransferService> _logger;
        private readonly INotificationService? _notificationService;

        public StockTransferService(
            ApplicationDbContext context,
            ILogger<StockTransferService> logger,
            INotificationService? notificationService = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificationService = notificationService;
        }

        public async Task<StockTransferDto> CreateTransferRequestAsync(
            CreateStockTransferDto dto,
            int createdByUserId,
            CancellationToken cancellationToken = default)
        {
            if (dto.SourceStoreId == dto.DestinationStoreId)
            {
                throw new InvalidOperationException("Kho nguồn và kho đích không được trùng nhau.");
            }

            if (dto.Items == null || !dto.Items.Any())
            {
                throw new InvalidOperationException("Vui lòng chọn ít nhất một sản phẩm cần điều chuyển.");
            }

            var sourceStore = await _context.Stores.AsNoTracking().FirstOrDefaultAsync(s => s.Id == dto.SourceStoreId && s.IsActive, cancellationToken);
            var destStore = await _context.Stores.AsNoTracking().FirstOrDefaultAsync(s => s.Id == dto.DestinationStoreId && s.IsActive, cancellationToken);

            if (sourceStore == null || destStore == null)
            {
                throw new InvalidOperationException("Kho nguồn hoặc kho đích không hợp lệ hoặc đang tạm ngưng hoạt động.");
            }

            string code = $"TRF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            int totalReq = dto.Items.Sum(i => i.RequestedQuantity);

            var transfer = new StockTransfer
            {
                TransferCode = code,
                SourceStoreId = dto.SourceStoreId,
                DestinationStoreId = dto.DestinationStoreId,
                CreatedByUserId = createdByUserId,
                Status = "PENDING",
                TotalRequestedQuantity = totalReq,
                TotalSentQuantity = 0,
                TotalReceivedQuantity = 0,
                Note = dto.Note,
                RequestedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var it in dto.Items)
            {
                if (it.RequestedQuantity <= 0) continue;

                transfer.Details.Add(new StockTransferDetail
                {
                    ProductId = it.ProductId,
                    RequestedQuantity = it.RequestedQuantity,
                    SentQuantity = 0,
                    ReceivedQuantity = 0,
                    Note = it.Note
                });
            }

            await _context.StockTransfers.AddAsync(transfer, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Tạo yêu cầu chuyển kho {Code} từ Kho {Source} tới Kho {Dest} thành công với {Count} sản phẩm.",
                code, sourceStore.Name, destStore.Name, transfer.Details.Count);

            if (_notificationService != null)
            {
                try
                {
                    await _notificationService.CreateAndSendNotificationAsync(new CreateNotificationDto
                    {
                        Type = "WAREHOUSE",
                        Severity = "WARNING",
                        Title = "🔄 Phiếu Chuyển Kho Cần Duyệt",
                        Message = $"Phiếu điều chuyển {transfer.TransferCode} ({sourceStore.Name} ➔ {destStore.Name}) cần phê duyệt.",
                        Role = "SuperAdmin",
                        Url = $"/Admin/StockTransfer"
                    }, cancellationToken);
                }
                catch (Exception notifEx)
                {
                    _logger.LogWarning(notifEx, "Không thể gửi notification cho phiếu chuyển kho {Code}", transfer.TransferCode);
                }
            }

            return (await GetTransferByIdAsync(transfer.Id, cancellationToken))!;
        }

        public async Task<StockTransferDto> ApproveAndExportAsync(
            int transferId,
            int approvedByUserId,
            string? note = null,
            CancellationToken cancellationToken = default)
        {
            var transfer = await _context.StockTransfers
                .Include(t => t.Details)
                    .ThenInclude(d => d.Product)
                .Include(t => t.SourceStore)
                .Include(t => t.DestinationStore)
                .FirstOrDefaultAsync(t => t.Id == transferId, cancellationToken);

            if (transfer == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy phiếu điều chuyển #{transferId}.");
            }

            if (transfer.Status != "PENDING")
            {
                throw new InvalidOperationException($"Chỉ phiếu ở trạng thái PENDING mới có thể phê duyệt xuất kho (Trạng thái hiện tại: {transfer.Status}).");
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            return (await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var initialDetails = transfer.Details.ToList();
                    int totalSentAll = 0;

                    foreach (var detail in initialDetails)
                    {
                        int neededQty = detail.RequestedQuantity;

                        // Tìm các lô khả dụng của sản phẩm tại kho nguồn theo thuật toán FEFO (Lô cận date xuất trước)
                        var availableInventories = await _context.StoreInventories
                            .Include(si => si.ProductBatch)
                            .Where(si => si.StoreId == transfer.SourceStoreId 
                                      && si.ProductBatch.ProductId == detail.ProductId
                                      && si.ProductBatch.Status == "ACTIVE"
                                      && si.PhysicalQuantity > si.ReservedQuantity)
                            .OrderBy(si => si.ProductBatch.ExpDate) // FEFO
                            .ToListAsync(cancellationToken);

                        int totalAvail = availableInventories.Sum(si => si.PhysicalQuantity - si.ReservedQuantity);
                        if (totalAvail < neededQty)
                        {
                            throw new InvalidOperationException(
                                $"Kho xuất '{transfer.SourceStore.Name}' không đủ tồn kho khả dụng cho sản phẩm '{detail.Product?.Name}' (Cần {neededQty}, khả dụng {totalAvail}).");
                        }

                        bool isFirstBatch = true;
                        foreach (var inv in availableInventories)
                        {
                            if (neededQty <= 0) break;

                            int availableInBatch = inv.PhysicalQuantity - inv.ReservedQuantity;
                            int takeQty = Math.Min(neededQty, availableInBatch);

                            int beforeQty = inv.PhysicalQuantity;
                            inv.PhysicalQuantity -= takeQty;
                            inv.LastUpdated = DateTime.UtcNow;

                            // Ghi sổ cái nhật ký xuất kho điều chuyển
                            var outLog = new InventoryLog
                            {
                                StoreId = transfer.SourceStoreId,
                                ProductBatchId = inv.ProductBatchId,
                                TransactionType = "TRANSFER_OUT",
                                QuantityChanged = -takeQty,
                                QuantityBefore = beforeQty,
                                QuantityAfter = inv.PhysicalQuantity,
                                UserId = approvedByUserId,
                                CreatedAt = DateTime.UtcNow,
                                Note = $"Xuất chuyển kho {transfer.TransferCode} tới {transfer.DestinationStore.Name} (Hạn dùng: {inv.ProductBatch.ExpDate:dd/MM/yyyy})"
                            };
                            await _context.InventoryLogs.AddAsync(outLog, cancellationToken);

                            if (isFirstBatch)
                            {
                                detail.ProductBatchId = inv.ProductBatchId;
                                detail.SentQuantity = takeQty;
                                isFirstBatch = false;
                            }
                            else
                            {
                                // Tách thêm dòng chi tiết nếu phân bổ từ nhiều lô khác nhau
                                var additionalDetail = new StockTransferDetail
                                {
                                    StockTransferId = transfer.Id,
                                    ProductId = detail.ProductId,
                                    ProductBatchId = inv.ProductBatchId,
                                    RequestedQuantity = 0,
                                    SentQuantity = takeQty,
                                    ReceivedQuantity = 0,
                                    Note = $"Tách lô FEFO bổ sung (Lô: {inv.ProductBatch.BatchNumber})"
                                };
                                await _context.StockTransferDetails.AddAsync(additionalDetail, cancellationToken);
                            }

                            totalSentAll += takeQty;
                            neededQty -= takeQty;
                        }
                    }

                    transfer.Status = "IN_TRANSIT";
                    transfer.ApprovedByUserId = approvedByUserId;
                    transfer.ApprovedAt = DateTime.UtcNow;
                    transfer.TotalSentQuantity = totalSentAll;
                    if (!string.IsNullOrWhiteSpace(note))
                    {
                        transfer.Note = string.IsNullOrWhiteSpace(transfer.Note) ? $"[Duyệt xuất]: {note}" : $"{transfer.Note} | [Duyệt xuất]: {note}";
                    }
                    transfer.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation("Phê duyệt và xuất kho thành công phiếu {Code}. Đang trung chuyển {Total} sản phẩm.",
                        transfer.TransferCode, totalSentAll);

                    return (await GetTransferByIdAsync(transfer.Id, cancellationToken))!;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Lỗi phê duyệt xuất kho phiếu điều chuyển #{TransferId}", transferId);
                    throw;
                }
            }))!;
        }

        public async Task<StockTransferDto> ReceiveAndImportAsync(
            ReceiveTransferDto dto,
            int receivedByUserId,
            CancellationToken cancellationToken = default)
        {
            var transfer = await _context.StockTransfers
                .Include(t => t.Details)
                    .ThenInclude(d => d.Product)
                .Include(t => t.Details)
                    .ThenInclude(d => d.ProductBatch)
                .Include(t => t.SourceStore)
                .Include(t => t.DestinationStore)
                .FirstOrDefaultAsync(t => t.Id == dto.TransferId, cancellationToken);

            if (transfer == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy phiếu điều chuyển #{dto.TransferId}.");
            }

            if (transfer.Status != "IN_TRANSIT")
            {
                throw new InvalidOperationException($"Chỉ phiếu ở trạng thái IN_TRANSIT mới có thể xác nhận nhập kho (Trạng thái hiện tại: {transfer.Status}).");
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            return (await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    int totalReceivedAll = 0;

                    foreach (var detail in transfer.Details)
                    {
                        var receivedItem = dto.Items.FirstOrDefault(i => i.DetailId == detail.Id);
                        int actualReceived = receivedItem != null ? receivedItem.ReceivedQuantity : detail.SentQuantity;

                        if (actualReceived < 0) actualReceived = 0;
                        if (actualReceived > detail.SentQuantity) actualReceived = detail.SentQuantity;

                        detail.ReceivedQuantity = actualReceived;

                        if (actualReceived < detail.SentQuantity)
                        {
                            detail.ShortageReason = receivedItem?.ShortageReason ?? "Chênh lệch thiếu hụt khi nhận hàng thực tế";
                        }

                        totalReceivedAll += actualReceived;

                        // Tăng tồn kho vật lý tại kho đích
                        if (actualReceived > 0 && detail.ProductBatchId.HasValue)
                        {
                            var destInventory = await _context.StoreInventories
                                .FirstOrDefaultAsync(si => si.StoreId == transfer.DestinationStoreId && si.ProductBatchId == detail.ProductBatchId.Value, cancellationToken);

                            if (destInventory == null)
                            {
                                destInventory = new StoreInventory
                                {
                                    StoreId = transfer.DestinationStoreId,
                                    ProductBatchId = detail.ProductBatchId.Value,
                                    PhysicalQuantity = 0,
                                    ReservedQuantity = 0,
                                    LastUpdated = DateTime.UtcNow
                                };
                                await _context.StoreInventories.AddAsync(destInventory, cancellationToken);
                            }

                            int beforeQty = destInventory.PhysicalQuantity;
                            destInventory.PhysicalQuantity += actualReceived;
                            destInventory.LastUpdated = DateTime.UtcNow;

                            // Ghi sổ cái nhập kho điều chuyển
                            var inLog = new InventoryLog
                            {
                                StoreId = transfer.DestinationStoreId,
                                ProductBatchId = detail.ProductBatchId.Value,
                                TransactionType = "TRANSFER_IN",
                                QuantityChanged = actualReceived,
                                QuantityBefore = beforeQty,
                                QuantityAfter = destInventory.PhysicalQuantity,
                                UserId = receivedByUserId,
                                CreatedAt = DateTime.UtcNow,
                                Note = $"Nhập chuyển kho {transfer.TransferCode} từ {transfer.SourceStore.Name} (Thực nhận {actualReceived}/{detail.SentQuantity})"
                            };
                            await _context.InventoryLogs.AddAsync(inLog, cancellationToken);
                        }
                    }

                    transfer.Status = "RECEIVED";
                    transfer.ReceivedByUserId = receivedByUserId;
                    transfer.ReceivedAt = DateTime.UtcNow;
                    transfer.TotalReceivedQuantity = totalReceivedAll;
                    if (!string.IsNullOrWhiteSpace(dto.Note))
                    {
                        transfer.Note = string.IsNullOrWhiteSpace(transfer.Note) ? $"[Nhập kho]: {dto.Note}" : $"{transfer.Note} | [Nhập kho]: {dto.Note}";
                    }
                    transfer.UpdatedAt = DateTime.UtcNow;

                    transfer.AddDomainEvent(new StockTransferCompletedDomainEvent(
                        transfer.Id,
                        transfer.TransferCode,
                        transfer.SourceStoreId,
                        transfer.DestinationStoreId,
                        totalReceivedAll));

                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation("Kho đích {Dest} đã hoàn tất nhập kho phiếu {Code}. Thực nhận {Received}/{Sent}.",
                        transfer.DestinationStore.Name, transfer.TransferCode, totalReceivedAll, transfer.TotalSentQuantity);

                    return (await GetTransferByIdAsync(transfer.Id, cancellationToken))!;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Lỗi xác nhận nhập kho phiếu điều chuyển #{TransferId}", dto.TransferId);
                    throw;
                }
            }))!;
        }

        public async Task<bool> CancelTransferAsync(
            int transferId,
            int userId,
            string reason,
            CancellationToken cancellationToken = default)
        {
            var transfer = await _context.StockTransfers.FirstOrDefaultAsync(t => t.Id == transferId, cancellationToken);
            if (transfer == null) return false;

            if (transfer.Status != "PENDING")
            {
                throw new InvalidOperationException($"Chỉ có thể hủy phiếu khi còn ở trạng thái PENDING. Trạng thái hiện tại: {transfer.Status}");
            }

            transfer.Status = "CANCELLED";
            transfer.CancellationReason = reason;
            transfer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Phiếu điều chuyển {Code} đã bị hủy bởi User {UserId}. Lý do: {Reason}",
                transfer.TransferCode, userId, reason);

            return true;
        }

        public async Task<StockTransferDto?> GetTransferByIdAsync(int transferId, CancellationToken cancellationToken = default)
        {
            var t = await _context.StockTransfers
                .AsNoTracking()
                .Include(x => x.SourceStore)
                .Include(x => x.DestinationStore)
                .Include(x => x.CreatedByUser)
                .Include(x => x.ApprovedByUser)
                .Include(x => x.ReceivedByUser)
                .Include(x => x.Details)
                    .ThenInclude(d => d.Product)
                .Include(x => x.Details)
                    .ThenInclude(d => d.ProductBatch)
                .FirstOrDefaultAsync(x => x.Id == transferId, cancellationToken);

            return t != null ? MapToDto(t) : null;
        }

        public async Task<StockTransferDto?> GetTransferByCodeAsync(string transferCode, CancellationToken cancellationToken = default)
        {
            var t = await _context.StockTransfers
                .AsNoTracking()
                .Include(x => x.SourceStore)
                .Include(x => x.DestinationStore)
                .Include(x => x.CreatedByUser)
                .Include(x => x.ApprovedByUser)
                .Include(x => x.ReceivedByUser)
                .Include(x => x.Details)
                    .ThenInclude(d => d.Product)
                .Include(x => x.Details)
                    .ThenInclude(d => d.ProductBatch)
                .FirstOrDefaultAsync(x => x.TransferCode == transferCode, cancellationToken);

            return t != null ? MapToDto(t) : null;
        }

        public async Task<List<StockTransferDto>> GetTransfersAsync(StockTransferFilterDto? filter = null, CancellationToken cancellationToken = default)
        {
            var query = _context.StockTransfers
                .AsNoTracking()
                .Include(x => x.SourceStore)
                .Include(x => x.DestinationStore)
                .Include(x => x.CreatedByUser)
                .Include(x => x.ApprovedByUser)
                .Include(x => x.ReceivedByUser)
                .Include(x => x.Details)
                    .ThenInclude(d => d.Product)
                .Include(x => x.Details)
                    .ThenInclude(d => d.ProductBatch)
                .AsQueryable();

            if (filter != null)
            {
                if (!string.IsNullOrWhiteSpace(filter.Status))
                {
                    query = query.Where(t => t.Status == filter.Status);
                }
                if (filter.SourceStoreId.HasValue)
                {
                    query = query.Where(t => t.SourceStoreId == filter.SourceStoreId.Value);
                }
                if (filter.DestinationStoreId.HasValue)
                {
                    query = query.Where(t => t.DestinationStoreId == filter.DestinationStoreId.Value);
                }
                if (filter.FromDate.HasValue)
                {
                    query = query.Where(t => t.RequestedAt >= filter.FromDate.Value);
                }
                if (filter.ToDate.HasValue)
                {
                    query = query.Where(t => t.RequestedAt <= filter.ToDate.Value);
                }
                if (!string.IsNullOrWhiteSpace(filter.Keyword))
                {
                    var kw = filter.Keyword.Trim();
                    query = query.Where(t => t.TransferCode.Contains(kw) || (t.Note != null && t.Note.Contains(kw)));
                }
            }

            var transfers = await query
                .OrderByDescending(t => t.RequestedAt)
                .ToListAsync(cancellationToken);

            return transfers.Select(MapToDto).ToList();
        }

        private static StockTransferDto MapToDto(StockTransfer t)
        {
            return new StockTransferDto
            {
                Id = t.Id,
                TransferCode = t.TransferCode,
                SourceStoreId = t.SourceStoreId,
                SourceStoreName = t.SourceStore?.Name ?? $"Kho #{t.SourceStoreId}",
                DestinationStoreId = t.DestinationStoreId,
                DestinationStoreName = t.DestinationStore?.Name ?? $"Kho #{t.DestinationStoreId}",
                CreatedByUserId = t.CreatedByUserId,
                CreatedByUserName = t.CreatedByUser?.FullName ?? t.CreatedByUser?.Username ?? "N/A",
                ApprovedByUserId = t.ApprovedByUserId,
                ApprovedByUserName = t.ApprovedByUser?.FullName ?? t.ApprovedByUser?.Username,
                ReceivedByUserId = t.ReceivedByUserId,
                ReceivedByUserName = t.ReceivedByUser?.FullName ?? t.ReceivedByUser?.Username,
                Status = t.Status,
                TotalRequestedQuantity = t.TotalRequestedQuantity,
                TotalSentQuantity = t.TotalSentQuantity,
                TotalReceivedQuantity = t.TotalReceivedQuantity,
                Note = t.Note,
                CancellationReason = t.CancellationReason,
                RequestedAt = t.RequestedAt,
                ApprovedAt = t.ApprovedAt,
                ReceivedAt = t.ReceivedAt,
                Details = t.Details.Select(d => new StockTransferDetailDto
                {
                    Id = d.Id,
                    StockTransferId = d.StockTransferId,
                    ProductId = d.ProductId,
                    ProductName = d.Product?.Name ?? "Sản phẩm",
                    ProductSku = d.Product?.Sku,
                    ProductBarcode = d.Product?.Barcode,
                    ProductThumbnailUrl = d.Product?.ThumbnailUrl,
                    ProductBatchId = d.ProductBatchId,
                    BatchNumber = d.ProductBatch?.BatchNumber,
                    ExpDate = d.ProductBatch?.ExpDate,
                    RequestedQuantity = d.RequestedQuantity,
                    SentQuantity = d.SentQuantity,
                    ReceivedQuantity = d.ReceivedQuantity,
                    ShortageReason = d.ShortageReason,
                    Note = d.Note
                }).ToList()
            };
        }
    }
}
