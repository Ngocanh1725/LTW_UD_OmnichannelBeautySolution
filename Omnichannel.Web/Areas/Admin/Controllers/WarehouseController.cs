using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Inventory;
using Omnichannel.Application.Interfaces.Inventory;
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
            var stores = await _context.Stores
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderByDescending(s => s.IsCentralWarehouse)
                .ThenBy(s => s.StoreName)
                .Select(s => new StoreLookupDto
                {
                    StoreId = s.StoreId,
                    StoreName = s.StoreName,
                    IsCentralWarehouse = s.IsCentralWarehouse
                })
                .ToListAsync();

            var currentStore = !string.IsNullOrEmpty(storeId)
                ? stores.FirstOrDefault(s => s.StoreId == storeId)
                : stores.FirstOrDefault();

            if (currentStore == null)
            {
                return View(new WarehouseOverviewViewModel());
            }

            var query = _context.StoreInventories
                .AsNoTracking()
                .Include(si => si.Store)
                .Include(si => si.Batch)
                    .ThenInclude(b => b.Product)
                .Where(si => si.StoreId == currentStore.StoreId)
                .AsQueryable();

            var today = DateTime.Today;

            var items = await query
                .OrderBy(si => si.Batch.ExpDate) // Luôn sắp xếp theo tiêu chí FEFO
                .Select(si => new BatchStockItemViewModel
                {
                    InventoryId = si.InventoryId,
                    StoreId = si.StoreId,
                    StoreName = si.Store.StoreName,
                    ProductId = si.Batch.ProductId,
                    Barcode = si.Batch.Product.Barcode,
                    ProductName = si.Batch.Product.ProductName,
                    BatchId = si.BatchId,
                    BatchNumber = si.Batch.BatchNumber,
                    ManufacturingDate = si.Batch.ManufacturingDate,
                    ExpDate = si.Batch.ExpDate,
                    PhysicalQuantity = si.PhysicalQuantity,
                    ReservedQuantity = si.ReservedQuantity,
                    SafetyStock = si.Batch.Product.SafetyStock
                })
                .ToListAsync();

            // Đếm thống kê
            int totalExpired = items.Count(i => i.IsExpired);
            int totalNearExpiry = items.Count(i => i.IsNearExpiry);
            int totalLowStock = items.Count(i => i.IsLowStock);

            // Lọc dữ liệu theo tab nếu có
            if (filterStatus == "EXPIRED")
            {
                items = items.Where(i => i.IsExpired).ToList();
            }
            else if (filterStatus == "NEAR_EXPIRY")
            {
                items = items.Where(i => i.IsNearExpiry).ToList();
            }
            else if (filterStatus == "LOW_STOCK")
            {
                items = items.Where(i => i.IsLowStock).ToList();
            }

            // Nạp danh sách sản phẩm cho Modal Nhập Lô
            ViewBag.ProductList = await _context.Products
                .Where(p => p.Status == 1)
                .OrderBy(p => p.ProductName)
                .Select(p => new { p.ProductId, Display = $"{p.ProductName} ({p.ProductId} - {p.Barcode})" })
                .ToListAsync();

            var vm = new WarehouseOverviewViewModel
            {
                SelectedStoreId = currentStore.StoreId,
                SelectedStoreName = currentStore.StoreName,
                FilterStatus = filterStatus,
                AvailableStores = stores,
                BatchStockItems = items,
                TotalSKUs = items.Select(i => i.ProductId).Distinct().Count(),
                TotalExpiredBatches = totalExpired,
                TotalNearExpiryBatches = totalNearExpiry,
                TotalLowStockBatches = totalLowStock,
                NewBatchImport = new ImportBatchViewModel { StoreId = currentStore.StoreId }
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("WAREHOUSE", "IMPORT_BATCH")]
        public async Task<IActionResult> ImportBatch(ImportBatchViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu nhập lô hàng chưa hợp lệ.";
                return RedirectToAction(nameof(Index), new { storeId = model.StoreId });
            }

            var staffId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "USR-SUPERADMIN-0001";
            bool success = await _allocationEngine.ImportBatchAsync(model, staffId);

            if (success)
            {
                TempData["SuccessMessage"] = $"Đã nhập kho thành công {model.Quantity} sản phẩm theo Lô {model.BatchNumber}!";
            }
            else
            {
                TempData["ErrorMessage"] = "Lỗi khi nhập kho theo Lô. Vui lòng kiểm tra ngày HSD > NSX.";
            }

            return RedirectToAction(nameof(Index), new { storeId = model.StoreId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("WAREHOUSE", "STOCK_CHECK")]
        public async Task<IActionResult> AdjustStock(StockAdjustmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do điều chỉnh tồn kho.";
                return RedirectToAction(nameof(Index), new { storeId = model.StoreId });
            }

            var staffId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "USR-SUPERADMIN-0001";
            bool success = await _allocationEngine.AdjustStockAsync(model, staffId);

            if (success)
            {
                TempData["SuccessMessage"] = "Đã cập nhật số lượng tồn kho và ghi sổ cái kiểm toán thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Lỗi điều chỉnh tồn kho. Không được phép để tồn vật lý nhỏ hơn tồn đã đặt cọc.";
            }

            return RedirectToAction(nameof(Index), new { storeId = model.StoreId });
        }
    }
}
