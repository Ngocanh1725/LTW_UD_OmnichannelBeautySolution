using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omnichannel.Application.Interfaces.Reports;
using Omnichannel.Infrastructure.Security;
using Omnichannel.Web.Extensions;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class ReportController : Controller
    {
        private readonly IReportService _reportService;
        private readonly IExportService _exportService;

        public ReportController(IReportService reportService, IExportService exportService)
        {
            _reportService = reportService;
            _exportService = exportService;
        }

        [HttpGet]
        [HasPermission("REPORT_SALES")]
        public async Task<IActionResult> Index(
            string? storeId = null,
            string period = "7days",
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default)
        {
            var sid = User.GetStoreId();
            if (!User.IsSuperAdmin() && sid.HasValue)
            {
                storeId = sid.Value.ToString();
            }

            var vm = await _reportService.GetRevenueDashboardAsync(storeId, period, fromDate, toDate, cancellationToken);
            return View(vm);
        }

        [HttpGet]
        [HasPermission("REPORT_SALES")]
        public async Task<IActionResult> ExportExcel(
            string? storeId = null,
            string period = "7days",
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default)
        {
            var sid = User.GetStoreId();
            if (!User.IsSuperAdmin() && sid.HasValue)
            {
                storeId = sid.Value.ToString();
            }

            var vm = await _reportService.GetRevenueDashboardAsync(storeId, period, fromDate, toDate, cancellationToken);
            string periodLabel = period switch
            {
                "today" => "Hôm nay",
                "30days" => "30 ngày qua",
                "custom" => $"Từ {vm.FromDate:dd/MM/yyyy} đến {vm.ToDate:dd/MM/yyyy}",
                _ => "7 ngày qua"
            };

            var bytes = _exportService.ExportRevenueOrdersToExcel(vm.DetailedOrders, periodLabel, vm.SelectedStoreName);
            string fileName = $"BaoCaoDoanhThu_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet]
        [HasPermission("REPORT_SALES")]
        public async Task<IActionResult> ExportCsv(
            string? storeId = null,
            string period = "7days",
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default)
        {
            var sid = User.GetStoreId();
            if (!User.IsSuperAdmin() && sid.HasValue)
            {
                storeId = sid.Value.ToString();
            }

            var vm = await _reportService.GetRevenueDashboardAsync(storeId, period, fromDate, toDate, cancellationToken);
            var bytes = _exportService.ExportRevenueOrdersToCsv(vm.DetailedOrders);
            string fileName = $"BaoCaoDoanhThu_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(bytes, "text/csv; charset=utf-8", fileName);
        }
    }
}