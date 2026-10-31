using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Sales;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services
{
    public class PosShiftService : IPosShiftService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PosShiftService> _logger;
        private readonly Omnichannel.Application.Interfaces.Notifications.INotificationService? _notificationService;

        public PosShiftService(
            ApplicationDbContext context, 
            ILogger<PosShiftService> logger,
            Omnichannel.Application.Interfaces.Notifications.INotificationService? notificationService = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificationService = notificationService;
        }

        public async Task<PosShiftDetailDto> OpenShiftAsync(OpenShiftDto dto, CancellationToken ct = default)
        {
            var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == dto.StoreId, ct);
            if (store == null)
            {
                throw new InvalidOperationException($"Không tìm thấy chi nhánh với ID {dto.StoreId}.");
            }

            var cashier = await _context.Users.FirstOrDefaultAsync(u => u.Id == dto.CashierId, ct);
            if (cashier == null)
            {
                throw new InvalidOperationException($"Không tìm thấy nhân viên thu ngân với ID {dto.CashierId}.");
            }

            // Quy tắc: Chỉ cho phép duy nhất 1 ca OPEN tại mỗi cửa hàng ở một thời điểm
            var existingOpenShift = await _context.PosShifts
                .FirstOrDefaultAsync(s => s.StoreId == dto.StoreId && s.Status == "OPEN", ct);

            if (existingOpenShift != null)
            {
                throw new InvalidOperationException($"Cửa hàng '{store.Name}' hiện đã có ca làm việc đang mở ({existingOpenShift.ShiftCode}). Vui lòng chốt ca trước khi mở ca mới.");
            }

            var shiftCode = $"SHIFT-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100, 999)}";

            var shift = new PosShift
            {
                ShiftCode = shiftCode,
                StoreId = dto.StoreId,
                CashierId = dto.CashierId,
                OpenedAt = DateTime.UtcNow,
                OpeningBalance = dto.OpeningBalance,
                ExpectedCash = dto.OpeningBalance,
                ActualCash = 0,
                Difference = 0,
                CashSalesTotal = 0,
                CardSalesTotal = 0,
                TransactionCount = 0,
                Status = "OPEN"
            };

            _context.PosShifts.Add(shift);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Đã mở ca bán hàng POS mới: {ShiftCode} tại cửa hàng {StoreId} bởi thu ngân {CashierId}",
                shift.ShiftCode, dto.StoreId, dto.CashierId);

            return new PosShiftDetailDto
            {
                Id = shift.Id,
                ShiftCode = shift.ShiftCode,
                StoreId = shift.StoreId,
                StoreName = store.Name,
                CashierId = shift.CashierId,
                CashierName = cashier.FullName ?? cashier.Username,
                OpenedAt = shift.OpenedAt,
                ClosedAt = null,
                OpeningBalance = shift.OpeningBalance,
                ExpectedCash = shift.ExpectedCash,
                ActualCash = 0,
                Difference = 0,
                CashSalesTotal = 0,
                CardSalesTotal = 0,
                TransactionCount = 0,
                Status = shift.Status,
                Note = shift.Note,
                DrawerTransactions = new List<DrawerTransactionItemDto>()
            };
        }

        public async Task<PosShiftDetailDto> CloseShiftAsync(CloseShiftDto dto, CancellationToken ct = default)
        {
            var shift = await _context.PosShifts
                .Include(s => s.Store)
                .Include(s => s.Cashier)
                .FirstOrDefaultAsync(s => s.Id == dto.ShiftId, ct);

            if (shift == null)
            {
                throw new InvalidOperationException($"Không tìm thấy ca làm việc với ID {dto.ShiftId}.");
            }

            if (shift.Status != "OPEN")
            {
                throw new InvalidOperationException($"Ca làm việc {shift.ShiftCode} đã được đóng trước đó.");
            }

            // 1. Tính toán doanh số từ các đơn hàng POS thuộc ca này
            var shiftOrders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.PosShiftId == shift.Id && o.OrderStatus == "COMPLETED")
                .ToListAsync(ct);

            var cashSales = shiftOrders.Where(o => o.PaymentMethod == "CASH").Sum(o => o.TotalAmount);
            var cardSales = shiftOrders.Where(o => o.PaymentMethod != "CASH").Sum(o => o.TotalAmount);
            var txCount = shiftOrders.Count;

            // 2. Tính toán các giao dịch thu/chi vãng lai trong ca
            var drawerTxs = await _context.PosDrawerTransactions
                .Include(t => t.Performer)
                .Where(t => t.PosShiftId == shift.Id)
                .ToListAsync(ct);

            var cashIn = drawerTxs.Where(t => t.Type == "CASH_IN").Sum(t => t.Amount);
            var cashOut = drawerTxs.Where(t => t.Type == "CASH_OUT" || t.Type == "PETTY_CASH").Sum(t => t.Amount);

            // 3. Tiền mặt kỳ vọng = Tiền đầu ca + Thu tiền mặt bán hàng + Nạp tiền lẻ - Chi tiêu vãng lai
            var expectedCash = shift.OpeningBalance + cashSales + cashIn - cashOut;
            var difference = dto.ActualCash - expectedCash;

            shift.ClosedAt = DateTime.UtcNow;
            shift.ActualCash = dto.ActualCash;
            shift.CashSalesTotal = cashSales;
            shift.CardSalesTotal = cardSales;
            shift.TransactionCount = txCount;
            shift.ExpectedCash = expectedCash;
            shift.Difference = difference;
            shift.Status = "CLOSED";
            shift.Note = dto.Note;

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(ct);
                try
                {
                    await _context.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                }
                catch
                {
                    await transaction.RollbackAsync(ct);
                    throw;
                }
            });

            _logger.LogInformation("Đã chốt ca bán hàng POS {ShiftCode}. Kỳ vọng: {Expected}, Thực tế: {Actual}, Chênh lệch: {Diff}",
                shift.ShiftCode, expectedCash, dto.ActualCash, difference);

            if (_notificationService != null && difference != 0)
            {
                try
                {
                    string sign = difference > 0 ? "+" : "";
                    await _notificationService.CreateAndSendNotificationAsync(new Omnichannel.Application.DTOs.Notifications.CreateNotificationDto
                    {
                        Type = "POS",
                        Severity = "DANGER",
                        Title = $"⚠️ Chênh Lệch Tiền Mặt Ca POS: #{shift.ShiftCode}",
                        Message = $"Ca {shift.ShiftCode} (Thu ngân {shift.Cashier?.FullName ?? "N/A"}) có chênh lệch tiền mặt: {sign}{difference:N0}₫. Lý do: {dto.Note ?? "Không ghi chú"}",
                        StoreId = shift.StoreId,
                        Role = "StoreManager",
                        Url = "/Admin/Pos"
                    }, ct);

                    await _notificationService.CreateAndSendNotificationAsync(new Omnichannel.Application.DTOs.Notifications.CreateNotificationDto
                    {
                        Type = "POS",
                        Severity = "DANGER",
                        Title = $"⚠️ Chênh Lệch Tiền Mặt Ca POS: #{shift.ShiftCode}",
                        Message = $"Chi nhánh #{shift.StoreId}: Ca {shift.ShiftCode} chênh lệch {sign}{difference:N0}₫.",
                        Role = "Admin",
                        Url = "/Admin/Pos"
                    }, ct);
                }
                catch (Exception notifEx)
                {
                    _logger.LogWarning(notifEx, "Không thể gửi notification cảnh báo chênh lệch ca POS {ShiftCode}", shift.ShiftCode);
                }
            }

                return new PosShiftDetailDto
                {
                    Id = shift.Id,
                    ShiftCode = shift.ShiftCode,
                    StoreId = shift.StoreId,
                    StoreName = shift.Store?.Name ?? "Chi nhánh",
                    CashierId = shift.CashierId,
                    CashierName = shift.Cashier?.FullName ?? shift.Cashier?.Username ?? "Thu ngân",
                    OpenedAt = shift.OpenedAt,
                    ClosedAt = shift.ClosedAt,
                    OpeningBalance = shift.OpeningBalance,
                    ExpectedCash = shift.ExpectedCash,
                    ActualCash = shift.ActualCash,
                    Difference = shift.Difference,
                    CashSalesTotal = shift.CashSalesTotal,
                    CardSalesTotal = shift.CardSalesTotal,
                    TransactionCount = shift.TransactionCount,
                    Status = shift.Status,
                    Note = shift.Note,
                    DrawerTransactions = drawerTxs.Select(t => new DrawerTransactionItemDto
                    {
                        Id = t.Id,
                        PosShiftId = t.PosShiftId,
                        Type = t.Type,
                        Amount = t.Amount,
                        Reason = t.Reason,
                        PerformedBy = t.PerformedBy,
                        PerformerName = t.Performer?.FullName ?? "Nhân viên",
                        CreatedAt = t.CreatedAt
                    }).ToList()
                };
        }

        public async Task<PosShiftDetailDto?> GetCurrentOpenShiftAsync(int storeId, int cashierId, CancellationToken ct = default)
        {
            var query = _context.PosShifts
                .Include(s => s.Store)
                .Include(s => s.Cashier)
                .Where(s => s.StoreId == storeId && s.Status == "OPEN");

            if (cashierId > 0)
            {
                query = query.Where(s => s.CashierId == cashierId);
            }

            var shift = await query.FirstOrDefaultAsync(ct);
            if (shift == null) return null;

            // Tính số liệu thời gian thực trong lúc ca đang mở
            var shiftOrders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.PosShiftId == shift.Id && o.OrderStatus == "COMPLETED")
                .ToListAsync(ct);

            var cashSales = shiftOrders.Where(o => o.PaymentMethod == "CASH").Sum(o => o.TotalAmount);
            var cardSales = shiftOrders.Where(o => o.PaymentMethod != "CASH").Sum(o => o.TotalAmount);
            var txCount = shiftOrders.Count;

            var drawerTxs = await _context.PosDrawerTransactions
                .Include(t => t.Performer)
                .Where(t => t.PosShiftId == shift.Id)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(ct);

            var cashIn = drawerTxs.Where(t => t.Type == "CASH_IN").Sum(t => t.Amount);
            var cashOut = drawerTxs.Where(t => t.Type == "CASH_OUT" || t.Type == "PETTY_CASH").Sum(t => t.Amount);
            var currentExpected = shift.OpeningBalance + cashSales + cashIn - cashOut;

            return new PosShiftDetailDto
            {
                Id = shift.Id,
                ShiftCode = shift.ShiftCode,
                StoreId = shift.StoreId,
                StoreName = shift.Store?.Name ?? "Chi nhánh",
                CashierId = shift.CashierId,
                CashierName = shift.Cashier?.FullName ?? shift.Cashier?.Username ?? "Thu ngân",
                OpenedAt = shift.OpenedAt,
                ClosedAt = null,
                OpeningBalance = shift.OpeningBalance,
                ExpectedCash = currentExpected,
                ActualCash = 0,
                Difference = 0,
                CashSalesTotal = cashSales,
                CardSalesTotal = cardSales,
                TransactionCount = txCount,
                Status = shift.Status,
                Note = shift.Note,
                DrawerTransactions = drawerTxs.Select(t => new DrawerTransactionItemDto
                {
                    Id = t.Id,
                    PosShiftId = t.PosShiftId,
                    Type = t.Type,
                    Amount = t.Amount,
                    Reason = t.Reason,
                    PerformedBy = t.PerformedBy,
                    PerformerName = t.Performer?.FullName ?? "Nhân viên",
                    CreatedAt = t.CreatedAt
                }).ToList()
            };
        }

        public async Task<List<PosShiftSummaryDto>> GetShiftHistoryAsync(int storeId, DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
        {
            var query = _context.PosShifts
                .AsNoTracking()
                .Include(s => s.Store)
                .Include(s => s.Cashier)
                .Where(s => s.StoreId == storeId);

            if (from.HasValue)
            {
                query = query.Where(s => s.OpenedAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(s => s.OpenedAt <= to.Value);
            }

            return await query
                .OrderByDescending(s => s.OpenedAt)
                .Select(s => new PosShiftSummaryDto
                {
                    Id = s.Id,
                    ShiftCode = s.ShiftCode,
                    StoreId = s.StoreId,
                    StoreName = s.Store.Name,
                    CashierId = s.CashierId,
                    CashierName = s.Cashier.FullName ?? s.Cashier.Username,
                    OpenedAt = s.OpenedAt,
                    ClosedAt = s.ClosedAt,
                    OpeningBalance = s.OpeningBalance,
                    ExpectedCash = s.ExpectedCash,
                    ActualCash = s.ActualCash,
                    Difference = s.Difference,
                    CashSalesTotal = s.CashSalesTotal,
                    CardSalesTotal = s.CardSalesTotal,
                    TransactionCount = s.TransactionCount,
                    Status = s.Status,
                    Note = s.Note
                })
                .ToListAsync(ct);
        }

        public async Task AddDrawerTransactionAsync(int shiftId, DrawerTransactionDto dto, CancellationToken ct = default)
        {
            var shift = await _context.PosShifts.FirstOrDefaultAsync(s => s.Id == shiftId, ct);
            if (shift == null)
            {
                throw new InvalidOperationException($"Không tìm thấy ca làm việc với ID {shiftId}.");
            }

            if (shift.Status != "OPEN")
            {
                throw new InvalidOperationException($"Không thể thực hiện giao dịch két cho ca làm việc đã đóng ({shift.ShiftCode}).");
            }

            if (dto.Amount <= 0)
            {
                throw new ArgumentException("Số tiền giao dịch két phải lớn hơn 0.", nameof(dto.Amount));
            }

            var performer = await _context.Users.FirstOrDefaultAsync(u => u.Id == dto.PerformedBy, ct);
            if (performer == null)
            {
                throw new InvalidOperationException($"Không tìm thấy nhân viên thực hiện với ID {dto.PerformedBy}.");
            }

            var tx = new PosDrawerTransaction
            {
                PosShiftId = shiftId,
                Type = dto.Type?.ToUpperInvariant() ?? "CASH_OUT",
                Amount = dto.Amount,
                Reason = dto.Reason ?? string.Empty,
                PerformedBy = dto.PerformedBy,
                CreatedAt = DateTime.UtcNow
            };

            _context.PosDrawerTransactions.Add(tx);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Ghi nhận giao dịch két {Type}: {Amount} VND cho ca {ShiftCode}. Lý do: {Reason}",
                tx.Type, tx.Amount, shift.ShiftCode, tx.Reason);
        }
    }
}
