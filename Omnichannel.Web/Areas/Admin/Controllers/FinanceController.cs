using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Finance;
using Omnichannel.Application.Interfaces.Finance;
using Omnichannel.Application.Interfaces.Reports;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Security;
using Omnichannel.Web.Extensions;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class FinanceController : Controller
    {
        private readonly IFinanceService _financeService;
        private readonly IReportService _reportService;
        private readonly IExportService _exportService;
        private readonly ApplicationDbContext _context;

        public FinanceController(
            IFinanceService financeService,
            IReportService reportService,
            IExportService exportService,
            ApplicationDbContext context)
        {
            _financeService = financeService;
            _reportService = reportService;
            _exportService = exportService;
            _context = context;
        }

        [HttpGet]
        [HasPermission("FINANCE_PNL_VIEW")]
        public IActionResult Index() => RedirectToAction(nameof(ProfitLoss));

        #region 1. Báo cáo P&L (Profit & Loss)
        [HttpGet]
        [HasPermission("FINANCE_PNL_VIEW")]
        public async Task<IActionResult> ProfitLoss(int? storeId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken)
        {
            var effectiveStoreId = await GetEffectiveStoreIdAsync();
            if (effectiveStoreId.HasValue) storeId = effectiveStoreId.Value;

            var now = DateTime.UtcNow;
            var from = fromDate ?? new DateTime(now.Year, now.Month, 1);
            var to = toDate ?? now;

            var report = await _reportService.GetProfitLossAsync(storeId, from, to, cancellationToken);
            var turnover = await _reportService.GetInventoryTurnoverAsync(storeId, cancellationToken);

            ViewBag.Turnover = turnover;
            var storesQuery = _context.Stores.AsNoTracking().Where(s => s.IsActive);
            if (effectiveStoreId.HasValue) storesQuery = storesQuery.Where(s => s.Id == effectiveStoreId.Value);
            ViewBag.Stores = await storesQuery.OrderBy(s => s.Name).ToListAsync(cancellationToken);
            ViewBag.SelectedStoreId = storeId;
            ViewBag.FromDate = from.ToString("yyyy-MM-dd");
            ViewBag.ToDate = to.ToString("yyyy-MM-dd");

            return View(report);
        }

        [HttpGet]
        [HasPermission("FINANCE_PNL_VIEW")]
        public async Task<IActionResult> ExportProfitLossExcel(int? storeId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken)
        {
            var effectiveStoreId = await GetEffectiveStoreIdAsync();
            if (effectiveStoreId.HasValue) storeId = effectiveStoreId.Value;

            var now = DateTime.UtcNow;
            var from = fromDate ?? new DateTime(now.Year, now.Month, 1);
            var to = toDate ?? now;

            var report = await _reportService.GetProfitLossAsync(storeId, from, to, cancellationToken);
            var bytes = _exportService.ExportProfitLossToExcel(report);
            string fileName = $"BaoCao_PnL_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet]
        [HasPermission("FINANCE_PNL_VIEW")]
        public async Task<IActionResult> ExportProfitLossPdf(int? storeId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken)
        {
            var effectiveStoreId = await GetEffectiveStoreIdAsync();
            if (effectiveStoreId.HasValue) storeId = effectiveStoreId.Value;

            var now = DateTime.UtcNow;
            var from = fromDate ?? new DateTime(now.Year, now.Month, 1);
            var to = toDate ?? now;

            var report = await _reportService.GetProfitLossAsync(storeId, from, to, cancellationToken);
            var bytes = _exportService.ExportProfitLossToPdf(report);
            string fileName = $"BaoCao_PnL_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(bytes, "application/pdf", fileName);
        }
        #endregion

        #region 2. Sổ cái tài chính (Financial Ledger)
        [HttpGet]
        [HasPermission("FINANCE_LEDGER_VIEW")]
        public async Task<IActionResult> Ledger(int? storeId, string? entryType, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken)
        {
            var effectiveStoreId = await GetEffectiveStoreIdAsync();
            if (effectiveStoreId.HasValue) storeId = effectiveStoreId.Value;

            var entries = await _financeService.GetLedgerEntriesAsync(storeId, entryType, fromDate, toDate, cancellationToken);

            // Tính số dư lũy kế theo thứ tự thời gian
            decimal running = 0m;
            var sorted = entries.OrderBy(e => e.EntryDate).ToList();
            foreach (var entry in sorted)
            {
                if (entry.Direction == "THU")
                {
                    running += entry.Amount;
                }
                else
                {
                    running -= entry.Amount;
                }
                entry.RunningBalance = running;
            }

            // Hiển thị mới nhất lên đầu
            var displayEntries = sorted.OrderByDescending(e => e.EntryDate).ToList();

            decimal totalThu = displayEntries.Where(e => e.Direction == "THU").Sum(e => e.Amount);
            decimal totalChi = displayEntries.Where(e => e.Direction == "CHI").Sum(e => e.Amount);
            decimal netBalance = totalThu - totalChi;

            ViewBag.TotalThu = totalThu;
            ViewBag.TotalChi = totalChi;
            ViewBag.NetBalance = netBalance;

            var storesQuery = _context.Stores.AsNoTracking().Where(s => s.IsActive);
            if (effectiveStoreId.HasValue) storesQuery = storesQuery.Where(s => s.Id == effectiveStoreId.Value);
            ViewBag.Stores = await storesQuery.OrderBy(s => s.Name).ToListAsync(cancellationToken);
            ViewBag.SelectedStoreId = storeId;
            ViewBag.SelectedEntryType = entryType;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(displayEntries);
        }

        [HttpGet]
        [HasPermission("FINANCE_LEDGER_VIEW")]
        public async Task<IActionResult> ExportLedgerExcel(int? storeId, string? entryType, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken)
        {
            var effectiveStoreId = await GetEffectiveStoreIdAsync();
            if (effectiveStoreId.HasValue) storeId = effectiveStoreId.Value;

            var entries = await _financeService.GetLedgerEntriesAsync(storeId, entryType, fromDate, toDate, cancellationToken);

            // Tính số dư lũy kế
            decimal running = 0m;
            var sorted = entries.OrderBy(e => e.EntryDate).ToList();
            foreach (var entry in sorted)
            {
                if (entry.Direction == "THU")
                {
                    running += entry.Amount;
                }
                else
                {
                    running -= entry.Amount;
                }
                entry.RunningBalance = running;
            }

            string storeLabel = "Toàn hệ thống";
            if (storeId.HasValue)
            {
                var store = await _context.Stores.FindAsync(new object[] { storeId.Value }, cancellationToken);
                if (store != null) storeLabel = store.Name;
            }

            string periodLabel = (fromDate.HasValue || toDate.HasValue)
                ? $"Từ {fromDate:dd/MM/yyyy} đến {toDate:dd/MM/yyyy}"
                : "Toàn bộ thời gian";

            var bytes = _exportService.ExportLedgerToExcel(sorted, storeLabel, periodLabel);
            string fileName = $"SoCai_ThuChi_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("FINANCE_LEDGER_VIEW")]
        public async Task<IActionResult> CreateLedgerEntry(CreateLedgerEntryDto dto, CancellationToken cancellationToken)
        {
            var effectiveStoreId = await GetEffectiveStoreIdAsync();
            if (effectiveStoreId.HasValue && dto.StoreId != effectiveStoreId.Value)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền tạo bút toán sổ cái cho chi nhánh khác.";
                return RedirectToAction(nameof(Ledger), new { storeId = effectiveStoreId.Value });
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu nhập sổ cái không hợp lệ.";
                return RedirectToAction(nameof(Ledger), new { storeId = dto.StoreId });
            }

            try
            {
                int? userId = GetCurrentUserId();
                await _financeService.RecordLedgerEntryAsync(dto, userId, cancellationToken);
                TempData["SuccessMessage"] = $"Đã ghi nhận bút toán sổ cái thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi ghi nhận sổ cái: " + ex.Message;
            }

            return RedirectToAction(nameof(Ledger), new { storeId = dto.StoreId });
        }
        #endregion

        #region 3. Công nợ Nhà cung cấp (Supplier Debt)
        [HttpGet]
        [HasPermission("FINANCE_SUPPLIER_DEBT")]
        public async Task<IActionResult> SupplierDebts(int? supplierId, string? status, CancellationToken cancellationToken)
        {
            var debts = await _financeService.GetSupplierDebtsAsync(supplierId, status, cancellationToken);

            ViewBag.Suppliers = await _context.Suppliers.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync(cancellationToken);
            ViewBag.Stores = await _context.Stores.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync(cancellationToken);
            ViewBag.SelectedSupplierId = supplierId;
            ViewBag.SelectedStatus = status;

            return View(debts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("FINANCE_SUPPLIER_DEBT")]
        public async Task<IActionResult> CreateSupplierDebt(CreateSupplierDebtDto dto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu công nợ không hợp lệ.";
                return RedirectToAction(nameof(SupplierDebts));
            }

            try
            {
                await _financeService.CreateSupplierDebtAsync(dto, cancellationToken);
                TempData["SuccessMessage"] = "Đã khởi tạo công nợ nhà cung cấp thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
            }

            return RedirectToAction(nameof(SupplierDebts));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("FINANCE_SUPPLIER_DEBT")]
        public async Task<IActionResult> PaySupplierDebt(RecordSupplierPaymentDto dto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Thông tin thanh toán công nợ chưa hợp lệ.";
                return RedirectToAction(nameof(SupplierDebts));
            }

            try
            {
                int? userId = GetCurrentUserId();
                bool success = await _financeService.RecordSupplierPaymentAsync(dto, userId, cancellationToken);
                if (success)
                {
                    TempData["SuccessMessage"] = "Đã ghi nhận thanh toán công nợ và tự động lưu sổ cái tài chính!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể thanh toán công nợ.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
            }

            return RedirectToAction(nameof(SupplierDebts));
        }
        #endregion

        #region 4. Đối soát COD (COD Reconciliation)
        [HttpGet]
        [HasPermission("FINANCE_COD_RECONCILE")]
        public async Task<IActionResult> CodReconciliations(string? partner, string? status, CancellationToken cancellationToken)
        {
            var recons = await _financeService.GetCodReconciliationsAsync(partner, status, cancellationToken);

            ViewBag.SelectedPartner = partner;
            ViewBag.SelectedStatus = status;

            return View(recons);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("FINANCE_COD_RECONCILE")]
        public async Task<IActionResult> CreateCodReconciliation(CreateCodReconciliationDto dto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu đối soát COD chưa hợp lệ.";
                return RedirectToAction(nameof(CodReconciliations));
            }

            try
            {
                await _financeService.CreateCodReconciliationAsync(dto, cancellationToken);
                TempData["SuccessMessage"] = "Đã tạo phiên đối soát tiền thu hộ COD thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
            }

            return RedirectToAction(nameof(CodReconciliations));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("FINANCE_COD_RECONCILE")]
        public async Task<IActionResult> ConfirmCodReconciliation(int reconciliationId, string status, string? note, CancellationToken cancellationToken)
        {
            int? userId = GetCurrentUserId();
            await _financeService.ConfirmCodReconciliationAsync(reconciliationId, status, note, userId, cancellationToken);
            TempData["SuccessMessage"] = "Đã cập nhật trạng thái phiên đối soát COD!";
            return RedirectToAction(nameof(CodReconciliations));
        }
        #endregion

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claim, out int uid))
            {
                return uid;
            }
            return null;
        }

        private async Task<int?> GetEffectiveStoreIdAsync()
        {
            if (User.IsSuperAdmin())
            {
                return null;
            }

            var sid = User.GetStoreId();
            if (sid.HasValue) return sid;

            var uid = User.GetUserId();
            if (uid.HasValue)
            {
                var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == uid.Value);
                if (user?.StoreId != null) return user.StoreId;
            }
            return null;
        }
    }
}
