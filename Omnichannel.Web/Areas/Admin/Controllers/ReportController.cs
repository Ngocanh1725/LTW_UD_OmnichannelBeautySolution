using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omnichannel.Application.Interfaces.Reports;
using Omnichannel.Infrastructure.Security;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class ReportController : Controller
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet]
        [HasPermission("REPORT_AUDIT", "VIEW_REVENUE")]
        public async Task<IActionResult> Index(string? storeId = null)
        {
            var vm = await _reportService.GetRevenueDashboardAsync(storeId);
            return View(vm);
        }
    }
}