using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Finance;
using Omnichannel.Application.Interfaces.Finance;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services
{
    public class FinanceService : IFinanceService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FinanceService> _logger;

        public FinanceService(ApplicationDbContext context, ILogger<FinanceService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<FinancialLedgerDto> RecordLedgerEntryAsync(CreateLedgerEntryDto dto, int? userId = null, CancellationToken cancellationToken = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                string code = $"LED-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

                var entry = new FinancialLedger
                {
                    LedgerCode = code,
                    EntryDate = DateTime.UtcNow,
                    EntryType = dto.EntryType.Trim().ToUpperInvariant(),
                    StoreId = dto.StoreId,
                    OrderId = dto.OrderId,
                    SupplierId = dto.SupplierId,
                    DebitAmount = dto.DebitAmount,
                    CreditAmount = dto.CreditAmount,
                    Description = dto.Description?.Trim(),
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.FinancialLedgers.AddAsync(entry, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Ghi nhận Sổ cái thành công: {Code}, Loại: {Type}, Nợ: {Debit}, Có: {Credit}",
                    code, entry.EntryType, entry.DebitAmount, entry.CreditAmount);

                var store = await _context.Stores.FindAsync(new object[] { dto.StoreId }, cancellationToken);
                return new FinancialLedgerDto
                {
                    Id = entry.Id,
                    LedgerCode = entry.LedgerCode,
                    EntryDate = entry.EntryDate,
                    EntryType = entry.EntryType,
                    StoreId = entry.StoreId,
                    StoreName = store?.Name ?? string.Empty,
                    OrderId = entry.OrderId,
                    SupplierId = entry.SupplierId,
                    DebitAmount = entry.DebitAmount,
                    CreditAmount = entry.CreditAmount,
                    Description = entry.Description,
                    CreatedBy = entry.CreatedBy,
                    CreatedAt = entry.CreatedAt
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Lỗi khi ghi nhận Sổ cái tài chính");
                throw;
            }
        }

        public async Task<List<FinancialLedgerDto>> GetLedgerEntriesAsync(int? storeId = null, string? entryType = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
        {
            var query = _context.FinancialLedgers
                .AsNoTracking()
                .Include(l => l.Store)
                .Include(l => l.Order)
                .Include(l => l.Supplier)
                .Include(l => l.CreatedByUser)
                .AsQueryable();

            if (storeId.HasValue)
            {
                query = query.Where(l => l.StoreId == storeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(entryType))
            {
                string normType = entryType.Trim().ToUpperInvariant();
                query = query.Where(l => l.EntryType == normType);
            }

            if (from.HasValue)
            {
                query = query.Where(l => l.EntryDate >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(l => l.EntryDate <= to.Value);
            }

            var list = await query
                .OrderByDescending(l => l.EntryDate)
                .ThenByDescending(l => l.Id)
                .Take(500)
                .ToListAsync(cancellationToken);

            return list.Select(l => new FinancialLedgerDto
            {
                Id = l.Id,
                LedgerCode = l.LedgerCode,
                EntryDate = l.EntryDate,
                EntryType = l.EntryType,
                StoreId = l.StoreId,
                StoreName = l.Store?.Name ?? string.Empty,
                OrderId = l.OrderId,
                OrderCode = l.Order?.OrderCode,
                SupplierId = l.SupplierId,
                SupplierName = l.Supplier?.Name,
                DebitAmount = l.DebitAmount,
                CreditAmount = l.CreditAmount,
                Description = l.Description,
                CreatedBy = l.CreatedBy,
                CreatedByName = l.CreatedByUser?.FullName ?? l.CreatedByUser?.Username,
                CreatedAt = l.CreatedAt
            }).ToList();
        }

        public async Task<SupplierDebtDto> CreateSupplierDebtAsync(CreateSupplierDebtDto dto, CancellationToken cancellationToken = default)
        {
            var supplier = await _context.Suppliers.FindAsync(new object[] { dto.SupplierId }, cancellationToken);
            if (supplier == null)
            {
                throw new InvalidOperationException($"Không tìm thấy Nhà cung cấp với Id = {dto.SupplierId}");
            }

            var debt = new SupplierDebt
            {
                SupplierId = dto.SupplierId,
                PurchaseOrderCode = dto.PurchaseOrderCode.Trim().ToUpperInvariant(),
                DebtAmount = dto.DebtAmount,
                PaidAmount = 0m,
                RemainingAmount = dto.DebtAmount,
                DueDate = dto.DueDate,
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };

            await _context.SupplierDebts.AddAsync(debt, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Đã ghi nhận công nợ mới #{Id} cho NCC {Supplier}: {Amount} VNĐ",
                debt.Id, supplier.Name, debt.DebtAmount);

            return new SupplierDebtDto
            {
                Id = debt.Id,
                SupplierId = debt.SupplierId,
                SupplierName = supplier.Name,
                PurchaseOrderCode = debt.PurchaseOrderCode,
                DebtAmount = debt.DebtAmount,
                PaidAmount = debt.PaidAmount,
                RemainingAmount = debt.RemainingAmount,
                DueDate = debt.DueDate,
                Status = debt.Status,
                CreatedAt = debt.CreatedAt
            };
        }

        public async Task<bool> RecordSupplierPaymentAsync(RecordSupplierPaymentDto dto, int? userId = null, CancellationToken cancellationToken = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var debt = await _context.SupplierDebts
                    .Include(d => d.Supplier)
                    .FirstOrDefaultAsync(d => d.Id == dto.DebtId, cancellationToken);

                if (debt == null)
                {
                    _logger.LogWarning("Không tìm thấy công nợ #{DebtId}", dto.DebtId);
                    return false;
                }

                if (dto.PaymentAmount <= 0)
                {
                    return false;
                }

                // Cập nhật công nợ
                debt.PaidAmount += dto.PaymentAmount;
                debt.RemainingAmount = Math.Max(0, debt.DebtAmount - debt.PaidAmount);
                debt.Status = debt.RemainingAmount == 0 ? "PAID" : "PARTIAL";
                debt.UpdatedAt = DateTime.UtcNow;

                // Tự động ghi nhận Sổ cái giao dịch SUPPLIER_PAYMENT
                string code = $"LED-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
                var ledgerEntry = new FinancialLedger
                {
                    LedgerCode = code,
                    EntryDate = DateTime.UtcNow,
                    EntryType = "SUPPLIER_PAYMENT",
                    StoreId = dto.StoreId,
                    SupplierId = debt.SupplierId,
                    DebitAmount = dto.PaymentAmount, // Chi trả làm giảm nợ
                    CreditAmount = 0m,
                    Description = $"Thanh toán công nợ đơn nhập #{debt.PurchaseOrderCode} - NCC: {debt.Supplier?.Name}. {(string.IsNullOrWhiteSpace(dto.Note) ? "" : "Ghi chú: " + dto.Note.Trim())}".Trim(),
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.FinancialLedgers.AddAsync(ledgerEntry, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Thanh toán công nợ #{DebtId} thành công: {PaidAmount} VNĐ. Còn nợ: {Remaining} VNĐ",
                    debt.Id, dto.PaymentAmount, debt.RemainingAmount);

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Lỗi khi ghi nhận thanh toán công nợ");
                throw;
            }
        }

        public async Task<List<SupplierDebtDto>> GetSupplierDebtsAsync(int? supplierId = null, string? status = null, CancellationToken cancellationToken = default)
        {
            var query = _context.SupplierDebts
                .AsNoTracking()
                .Include(d => d.Supplier)
                .AsQueryable();

            if (supplierId.HasValue)
            {
                query = query.Where(d => d.SupplierId == supplierId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                string normStatus = status.Trim().ToUpperInvariant();
                query = query.Where(d => d.Status == normStatus);
            }

            var list = await query
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync(cancellationToken);

            return list.Select(d => new SupplierDebtDto
            {
                Id = d.Id,
                SupplierId = d.SupplierId,
                SupplierName = d.Supplier?.Name ?? string.Empty,
                PurchaseOrderCode = d.PurchaseOrderCode,
                DebtAmount = d.DebtAmount,
                PaidAmount = d.PaidAmount,
                RemainingAmount = d.RemainingAmount,
                DueDate = d.DueDate,
                Status = d.Status,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            }).ToList();
        }

        public async Task<CodReconciliationDto> CreateCodReconciliationAsync(CreateCodReconciliationDto dto, CancellationToken cancellationToken = default)
        {
            string code = $"COD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            decimal diff = dto.TotalCodAmount - dto.ReceivedAmount;
            string status = Math.Abs(diff) < 0.01m ? "MATCHED" : "DISPUTED";

            var recon = new CodReconciliation
            {
                ReconciliationCode = code,
                ShippingPartner = dto.ShippingPartner.Trim().ToUpperInvariant(),
                ReconciliationDate = dto.ReconciliationDate,
                TotalOrders = dto.TotalOrders,
                TotalCodAmount = dto.TotalCodAmount,
                ReceivedAmount = dto.ReceivedAmount,
                Difference = diff,
                Status = status,
                Note = dto.Note?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _context.CodReconciliations.AddAsync(recon, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Tạo đợt đối soát COD {Code} cho đối tác {Partner}: Lệch {Diff} VNĐ, Trạng thái: {Status}",
                code, recon.ShippingPartner, recon.Difference, recon.Status);

            return new CodReconciliationDto
            {
                Id = recon.Id,
                ReconciliationCode = recon.ReconciliationCode,
                ShippingPartner = recon.ShippingPartner,
                ReconciliationDate = recon.ReconciliationDate,
                TotalOrders = recon.TotalOrders,
                TotalCodAmount = recon.TotalCodAmount,
                ReceivedAmount = recon.ReceivedAmount,
                Difference = recon.Difference,
                Status = recon.Status,
                Note = recon.Note,
                CreatedAt = recon.CreatedAt
            };
        }

        public async Task<bool> ConfirmCodReconciliationAsync(int reconciliationId, string status, string? note = null, int? userId = null, CancellationToken cancellationToken = default)
        {
            var recon = await _context.CodReconciliations.FindAsync(new object[] { reconciliationId }, cancellationToken);
            if (recon == null) return false;

            recon.Status = status.Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(note))
            {
                recon.Note = string.IsNullOrEmpty(recon.Note) ? note.Trim() : $"{recon.Note} | {note.Trim()}";
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Xác nhận đợt đối soát COD #{Id}: Trạng thái mới {Status}", reconciliationId, recon.Status);
            return true;
        }

        public async Task<List<CodReconciliationDto>> GetCodReconciliationsAsync(string? partner = null, string? status = null, CancellationToken cancellationToken = default)
        {
            var query = _context.CodReconciliations.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(partner))
            {
                string normPartner = partner.Trim().ToUpperInvariant();
                query = query.Where(r => r.ShippingPartner == normPartner);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                string normStatus = status.Trim().ToUpperInvariant();
                query = query.Where(r => r.Status == normStatus);
            }

            var list = await query.OrderByDescending(r => r.ReconciliationDate).ToListAsync(cancellationToken);

            return list.Select(r => new CodReconciliationDto
            {
                Id = r.Id,
                ReconciliationCode = r.ReconciliationCode,
                ShippingPartner = r.ShippingPartner,
                ReconciliationDate = r.ReconciliationDate,
                TotalOrders = r.TotalOrders,
                TotalCodAmount = r.TotalCodAmount,
                ReceivedAmount = r.ReceivedAmount,
                Difference = r.Difference,
                Status = r.Status,
                Note = r.Note,
                CreatedAt = r.CreatedAt
            }).ToList();
        }

        public async Task<(FinancialLedgerDto RevenueEntry, FinancialLedgerDto? CogsEntry)> RecordOrderPaymentAndCogsAsync(
            int orderId, int? userId = null, CancellationToken cancellationToken = default)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .Include(o => o.Store)
                .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

            if (order == null) throw new InvalidOperationException($"Không tìm thấy đơn hàng với Id = {orderId}");

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // 1. Ghi nhận doanh thu (Credit) nếu chưa có
                var existingRev = await _context.FinancialLedgers
                    .FirstOrDefaultAsync(l => l.OrderId == orderId && l.EntryType == "REVENUE", cancellationToken);

                FinancialLedger revEntry;
                if (existingRev == null)
                {
                    revEntry = new FinancialLedger
                    {
                        LedgerCode = $"LED-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                        EntryDate = DateTime.UtcNow,
                        EntryType = "REVENUE",
                        StoreId = order.StoreId,
                        OrderId = order.Id,
                        CreditAmount = order.TotalAmount,
                        DebitAmount = 0m,
                        Description = $"Doanh thu bán hàng từ đơn hàng {order.OrderCode} qua {order.PaymentMethod}",
                        CreatedBy = userId ?? order.UserId,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _context.FinancialLedgers.AddAsync(revEntry, cancellationToken);
                }
                else
                {
                    revEntry = existingRev;
                }

                // 2. Tính toán chính xác Giá vốn hàng bán (COGS) theo giá nhập ImportCost của từng lô ProductBatch
                decimal cogsAmount = 0m;
                foreach (var detail in order.OrderDetails)
                {
                    if (detail.ProductBatchId.HasValue)
                    {
                        var batch = await _context.ProductBatches
                            .AsNoTracking()
                            .FirstOrDefaultAsync(b => b.Id == detail.ProductBatchId.Value, cancellationToken);
                        if (batch != null && batch.ImportCost > 0)
                        {
                            cogsAmount += (batch.ImportCost * detail.Quantity);
                        }
                    }
                }

                FinancialLedger? cogsEntry = null;
                if (cogsAmount > 0)
                {
                    var existingCogs = await _context.FinancialLedgers
                        .FirstOrDefaultAsync(l => l.OrderId == orderId && l.EntryType == "COGS", cancellationToken);

                    if (existingCogs == null)
                    {
                        cogsEntry = new FinancialLedger
                        {
                            LedgerCode = $"LED-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                            EntryDate = DateTime.UtcNow,
                            EntryType = "COGS",
                            StoreId = order.StoreId,
                            OrderId = order.Id,
                            DebitAmount = cogsAmount,
                            CreditAmount = 0m,
                            Description = $"Giá vốn hàng bán (COGS) xuất kho theo lô FEFO cho đơn {order.OrderCode}",
                            CreatedBy = userId ?? order.UserId,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _context.FinancialLedgers.AddAsync(cogsEntry, cancellationToken);
                    }
                    else
                    {
                        cogsEntry = existingCogs;
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Ghi nhận Sổ cái cho đơn {OrderCode}: Doanh thu = {Revenue}đ, Giá vốn COGS = {Cogs}đ",
                    order.OrderCode, revEntry.CreditAmount, cogsAmount);

                var revDto = new FinancialLedgerDto
                {
                    Id = revEntry.Id,
                    LedgerCode = revEntry.LedgerCode,
                    EntryDate = revEntry.EntryDate,
                    EntryType = revEntry.EntryType,
                    StoreId = revEntry.StoreId,
                    StoreName = order.Store?.Name ?? string.Empty,
                    OrderId = revEntry.OrderId,
                    OrderCode = order.OrderCode,
                    DebitAmount = revEntry.DebitAmount,
                    CreditAmount = revEntry.CreditAmount,
                    Description = revEntry.Description,
                    CreatedBy = revEntry.CreatedBy,
                    CreatedAt = revEntry.CreatedAt
                };

                FinancialLedgerDto? cogsDto = null;
                if (cogsEntry != null)
                {
                    cogsDto = new FinancialLedgerDto
                    {
                        Id = cogsEntry.Id,
                        LedgerCode = cogsEntry.LedgerCode,
                        EntryDate = cogsEntry.EntryDate,
                        EntryType = cogsEntry.EntryType,
                        StoreId = cogsEntry.StoreId,
                        StoreName = order.Store?.Name ?? string.Empty,
                        OrderId = cogsEntry.OrderId,
                        OrderCode = order.OrderCode,
                        DebitAmount = cogsEntry.DebitAmount,
                        CreditAmount = cogsEntry.CreditAmount,
                        Description = cogsEntry.Description,
                        CreatedBy = cogsEntry.CreatedBy,
                        CreatedAt = cogsEntry.CreatedAt
                    };
                }

                return (revDto, cogsDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Lỗi khi ghi nhận doanh thu và giá vốn COGS cho đơn hàng {OrderId}", orderId);
                throw;
            }
        }

        public async Task<decimal> CalculateOrderCogsAsync(int orderId, CancellationToken cancellationToken = default)
        {
            var details = await _context.OrderDetails
                .Where(od => od.OrderId == orderId)
                .ToListAsync(cancellationToken);

            decimal totalCogs = 0m;
            foreach (var d in details)
            {
                if (d.ProductBatchId.HasValue)
                {
                    var batch = await _context.ProductBatches
                        .AsNoTracking()
                        .FirstOrDefaultAsync(b => b.Id == d.ProductBatchId.Value, cancellationToken);
                    if (batch != null && batch.ImportCost > 0)
                    {
                        totalCogs += (batch.ImportCost * d.Quantity);
                    }
                }
            }
            return totalCogs;
        }

        public async Task<SupplierDebtDto> RecordPurchaseOrderReceiptAsync(
            string purchaseOrderCode, int supplierId, int storeId, decimal totalAmount, DateTime? dueDate = null, int? userId = null, CancellationToken cancellationToken = default)
        {
            var supplier = await _context.Suppliers.FindAsync(new object[] { supplierId }, cancellationToken);
            if (supplier == null) throw new InvalidOperationException($"Không tìm thấy Nhà cung cấp #{supplierId}");

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // 1. Ghi sổ cái Chi phí nhập hàng (Debit)
                string ledgerCode = $"LED-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
                var ledgerEntry = new FinancialLedger
                {
                    LedgerCode = ledgerCode,
                    EntryDate = DateTime.UtcNow,
                    EntryType = "PURCHASE_RECEIPT",
                    StoreId = storeId,
                    SupplierId = supplierId,
                    DebitAmount = totalAmount, // Chi phí mua hàng
                    CreditAmount = 0m,
                    Description = $"Chi phí nhập kho đơn mua hàng #{purchaseOrderCode.Trim().ToUpperInvariant()} - NCC: {supplier.Name}",
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.FinancialLedgers.AddAsync(ledgerEntry, cancellationToken);

                // 2. Tạo công nợ Nhà cung cấp
                var debt = new SupplierDebt
                {
                    SupplierId = supplierId,
                    PurchaseOrderCode = purchaseOrderCode.Trim().ToUpperInvariant(),
                    DebtAmount = totalAmount,
                    PaidAmount = 0m,
                    RemainingAmount = totalAmount,
                    DueDate = dueDate ?? DateTime.UtcNow.AddDays(30),
                    Status = "PENDING",
                    CreatedAt = DateTime.UtcNow
                };
                await _context.SupplierDebts.AddAsync(debt, cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Nhập kho PO {PO}: Đã ghi Sổ cái Debit {Amount}đ và tạo công nợ NCC #{DebtId}",
                    purchaseOrderCode, totalAmount, debt.Id);

                return new SupplierDebtDto
                {
                    Id = debt.Id,
                    SupplierId = debt.SupplierId,
                    SupplierName = supplier.Name,
                    PurchaseOrderCode = debt.PurchaseOrderCode,
                    DebtAmount = debt.DebtAmount,
                    PaidAmount = debt.PaidAmount,
                    RemainingAmount = debt.RemainingAmount,
                    DueDate = debt.DueDate,
                    Status = debt.Status,
                    CreatedAt = debt.CreatedAt
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Lỗi khi ghi nhận nhập kho PO {PO}", purchaseOrderCode);
                throw;
            }
        }

        public async Task<List<SupplierDebtDto>> GetOverdueSupplierDebtsAsync(DateTime? asOfDate = null, CancellationToken cancellationToken = default)
        {
            var cutoff = (asOfDate ?? DateTime.UtcNow).Date;
            var list = await _context.SupplierDebts
                .AsNoTracking()
                .Include(d => d.Supplier)
                .Where(d => d.Status != "PAID" && d.DueDate.HasValue && d.DueDate.Value.Date < cutoff)
                .OrderBy(d => d.DueDate)
                .ToListAsync(cancellationToken);

            return list.Select(d => new SupplierDebtDto
            {
                Id = d.Id,
                SupplierId = d.SupplierId,
                SupplierName = d.Supplier?.Name ?? string.Empty,
                PurchaseOrderCode = d.PurchaseOrderCode,
                DebtAmount = d.DebtAmount,
                PaidAmount = d.PaidAmount,
                RemainingAmount = d.RemainingAmount,
                DueDate = d.DueDate,
                Status = d.Status,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            }).ToList();
        }

        public async Task<CodReconciliationDto> ReconcileCodOrdersAsync(
            string shippingPartner, List<string> orderCodes, decimal actualReceivedAmount, string? note = null, CancellationToken cancellationToken = default)
        {
            if (orderCodes == null || !orderCodes.Any())
            {
                throw new ArgumentException("Danh sách mã đơn đối soát COD không được để trống.", nameof(orderCodes));
            }

            var orders = await _context.Orders
                .AsNoTracking()
                .Where(o => orderCodes.Contains(o.OrderCode))
                .ToListAsync(cancellationToken);

            decimal expectedCod = orders.Sum(o => o.TotalAmount);
            decimal diff = expectedCod - actualReceivedAmount;
            string status = Math.Abs(diff) < 0.01m ? "MATCHED" : "DISPUTED";
            string code = $"COD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

            var recon = new CodReconciliation
            {
                ReconciliationCode = code,
                ShippingPartner = (shippingPartner ?? "GHN").Trim().ToUpperInvariant(),
                ReconciliationDate = DateTime.UtcNow,
                TotalOrders = orders.Count,
                TotalCodAmount = expectedCod,
                ReceivedAmount = actualReceivedAmount,
                Difference = diff,
                Status = status,
                Note = !string.IsNullOrWhiteSpace(note) ? note.Trim() : $"Đối soát {orders.Count} đơn COD qua {shippingPartner}",
                CreatedAt = DateTime.UtcNow
            };

            await _context.CodReconciliations.AddAsync(recon, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Đối soát COD {Code}: Tổng kỳ vọng={Expected}đ, Thực nhận={Received}đ, Lệch={Diff}đ -> {Status}",
                code, expectedCod, actualReceivedAmount, diff, status);

            return new CodReconciliationDto
            {
                Id = recon.Id,
                ReconciliationCode = recon.ReconciliationCode,
                ShippingPartner = recon.ShippingPartner,
                ReconciliationDate = recon.ReconciliationDate,
                TotalOrders = recon.TotalOrders,
                TotalCodAmount = recon.TotalCodAmount,
                ReceivedAmount = recon.ReceivedAmount,
                Difference = recon.Difference,
                Status = recon.Status,
                Note = recon.Note,
                CreatedAt = recon.CreatedAt
            };
        }
    }
}
