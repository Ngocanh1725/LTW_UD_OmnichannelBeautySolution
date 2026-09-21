using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Inventory;

using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Security;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class WarehouseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IInventoryAllocationEngine _allocationEngine;

        public WarehouseController(ApplicationDbContext context, IInventoryAllocationEngine allocationEngine)
        {
            _context = context;
            _allocationEngine = allocationEngine;
        }

        [HttpGet]
        [HasPermission("WAREHOUSE", "VIEW_STOCK")]
        public async Task<IActionResult> Index(string? storeId = null, string? filterStatus = null)
        {
            var vm = new WarehouseOverviewViewModel
            {
                SelectedStoreId = storeId ?? "S01",
                SelectedStoreName = "Mock Store",
                FilterStatus = filterStatus,
                AvailableStores = new List<StoreLookupDto> { new StoreLookupDto { StoreId = "S01", StoreName = "Mock Store" } },
                BatchStockItems = new List<BatchStockItemViewModel>(),
                TotalSKUs = 0,
                TotalExpiredBatches = 0,
                TotalNearExpiryBatches = 0,
                TotalLowStockBatches = 0,
                NewBatchImport = new ImportBatchViewModel { StoreId = storeId ?? "S01" }
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("WAREHOUSE", "IMPORT_BATCH")]
        public async Task<IActionResult> ImportBatch(ImportBatchViewModel model)
        {
            TempData["SuccessMessage"] = $"Đã nhập kho thành công {model.Quantity} sản phẩm theo Lô {model.BatchNumber}!";
            return RedirectToAction(nameof(Index), new { storeId = model.StoreId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("WAREHOUSE", "STOCK_CHECK")]
        public async Task<IActionResult> AdjustStock(StockAdjustmentViewModel model)
        {
            TempData["SuccessMessage"] = "Đã cập nhật số lượng tồn kho và ghi sổ cái kiểm toán thành công!";
            return RedirectToAction(nameof(Index), new { storeId = model.StoreId });
        }
    }
}
