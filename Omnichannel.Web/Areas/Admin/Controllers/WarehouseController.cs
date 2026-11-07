using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Inventory;
using Omnichannel.Application.Interfaces.Inventory;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Security;
using Omnichannel.Web.Extensions;
using Omnichannel.Application.Interfaces.Notifications;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class WarehouseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IInventoryAllocationEngine _allocationEngine;
        private readonly IStockTransferService _stockTransferService;
        private readonly IInventoryNotificationService? _inventoryNotificationService;

        public WarehouseController(
            ApplicationDbContext context, 
            IInventoryAllocationEngine allocationEngine,
            IStockTransferService stockTransferService,
            IInventoryNotificationService? inventoryNotificationService = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _allocationEngine = allocationEngine ?? throw new ArgumentNullException(nameof(allocationEngine));
            _stockTransferService = stockTransferService ?? throw new ArgumentNullException(nameof(stockTransferService));
            _inventoryNotificationService = inventoryNotificationService;
        }

        [HttpGet]
        [HasPermission("INVENTORY_VIEW")]
        public async Task<IActionResult> Index(string? storeId = null, string? filterStatus = null, string? keyword = null)
        {
            var effectiveStoreId = await GetEffectiveStoreIdAsync();
            var allStores = await _context.Stores.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.Id).ToListAsync();
            var selectedStore = effectiveStoreId.HasValue
                ? allStores.FirstOrDefault(s => s.Id == effectiveStoreId.Value)
                : (!string.IsNullOrEmpty(storeId)
                    ? (allStores.FirstOrDefault(s => s.Id.ToString() == storeId) ?? allStores.FirstOrDefault())
                    : allStores.FirstOrDefault());

            if (selectedStore == null)
            {
                return NotFound("Chưa có kho hàng / chi nhánh nào trong hệ thống.");
            }

            var stores = effectiveStoreId.HasValue
                ? allStores.Where(s => s.Id == effectiveStoreId.Value).ToList()
                : allStores;

            var currentStoreId = selectedStore.Id.ToString();
            var currentStoreName = selectedStore.Name;
            var today = DateTime.UtcNow.Date;

            // Products list for import modal
            var products = await _context.Products.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.Name).Select(p => new { p.Id, p.Sku, p.Name, p.SellingPrice }).ToListAsync();
            ViewBag.ProductList = products.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = $"{p.Sku} - {p.Name} (Giá niêm yết: {p.SellingPrice:N0} đ)"
            }).ToList();

            // Suppliers list for import modal
            var suppliers = await _context.Suppliers.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.Name).Select(s => new { s.Id, s.Code, s.Name }).ToListAsync();
            ViewBag.SupplierList = suppliers.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = $"{s.Code} - {s.Name}"
            }).ToList();

            // Base query for store inventory
            var query = _context.StoreInventories
                .AsNoTracking()
                .Include(si => si.ProductBatch)
                    .ThenInclude(pb => pb.Product)
                .Where(si => si.StoreId == selectedStore.Id);

            // Keyword filter
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(si => si.ProductBatch.Product.Name.ToLower().Contains(kw)
                                       || si.ProductBatch.Product.Sku.ToLower().Contains(kw)
                                       || si.ProductBatch.Product.Barcode.ToLower().Contains(kw)
                                       || si.ProductBatch.BatchNumber.ToLower().Contains(kw));
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(filterStatus))
            {
                switch (filterStatus.ToUpperInvariant())
                {
                    case "EXPIRED":
                        query = query.Where(si => si.ProductBatch.ExpDate.Date < today && si.PhysicalQuantity > 0);
                        break;
                    case "CRITICAL":
                    case "CRITICAL_30":
                        query = query.Where(si => si.ProductBatch.ExpDate.Date <= today.AddDays(30) && si.PhysicalQuantity > 0);
                        break;
                    case "WARNING":
                    case "WARNING_60":
                        query = query.Where(si => si.ProductBatch.ExpDate.Date > today.AddDays(30) && si.ProductBatch.ExpDate.Date <= today.AddDays(60) && si.PhysicalQuantity > 0);
                        break;
                    case "SAFE":
                    case "SAFE_60":
                        query = query.Where(si => si.ProductBatch.ExpDate.Date > today.AddDays(60) && si.PhysicalQuantity > 0);
                        break;
                    case "NEAR_EXPIRY":
                        query = query.Where(si => si.ProductBatch.ExpDate.Date >= today && si.ProductBatch.ExpDate.Date <= today.AddDays(si.ProductBatch.AlertDaysThreshold > 0 ? si.ProductBatch.AlertDaysThreshold : 60) && si.PhysicalQuantity > 0);
                        break;
                    case "LOW_STOCK":
                        query = query.Where(si => si.PhysicalQuantity <= 10);
                        break;
                }
            }

            // FEFO Sorting: ExpDate ascending (lô cận date xếp đầu)
            var inventories = await query
                .OrderBy(si => si.ProductBatch.ExpDate)
                .Take(200)
                .ToListAsync();

            // Calculate KPIs for current store
            var storeInventoriesAll = await _context.StoreInventories
                .AsNoTracking()
                .Include(si => si.ProductBatch)
                .Where(si => si.StoreId == selectedStore.Id)
                .ToListAsync();

            var criticalDate = today.AddDays(30);
            var warningDate = today.AddDays(60);

            var totalExpired = storeInventoriesAll.Count(si => si.PhysicalQuantity > 0 && si.ProductBatch.ExpDate.Date < today);
            var fefoCritical = storeInventoriesAll.Count(si => si.PhysicalQuantity > 0 && si.ProductBatch.ExpDate.Date <= criticalDate);
            var fefoWarning = storeInventoriesAll.Count(si => si.PhysicalQuantity > 0 && si.ProductBatch.ExpDate.Date > criticalDate && si.ProductBatch.ExpDate.Date <= warningDate);
            var fefoSafe = storeInventoriesAll.Count(si => si.PhysicalQuantity > 0 && si.ProductBatch.ExpDate.Date > warningDate);
            var totalNearExpiry = storeInventoriesAll.Count(si => si.PhysicalQuantity > 0 && si.ProductBatch.ExpDate.Date >= today && si.ProductBatch.ExpDate.Date <= warningDate);
            var totalLowStock = storeInventoriesAll.Count(si => si.PhysicalQuantity <= 10);
            var totalValue = storeInventoriesAll.Sum(si => (decimal)si.PhysicalQuantity * (si.ProductBatch.ImportCost > 0 ? si.ProductBatch.ImportCost : si.ProductBatch.Product?.SellingPrice ?? 0));

            // Map to batch items with 3 visual tiers: Red (<=30d), Yellow (<=60d), Green (>60d)
            var batchItems = inventories.Select(si =>
            {
                var threshold = si.ProductBatch.AlertDaysThreshold > 0 ? si.ProductBatch.AlertDaysThreshold : 60;
                var daysRemaining = (int)(si.ProductBatch.ExpDate.Date - today).TotalDays;
                var isExpired = daysRemaining < 0;
                var isCritical = daysRemaining <= 30;
                var isWarning = daysRemaining > 30 && daysRemaining <= 60;
                var initQty = si.ProductBatch.InitialQuantity;
                var currQty = si.ProductBatch.CurrentQuantity;
                var soldRatio = initQty > 0 ? Math.Round((double)(initQty - currQty) / initQty * 100, 1) : 0;

                string badgeClass;
                string badgeText;
                string fefoTier;
                string fefoColor;

                if (isExpired)
                {
                    badgeClass = "badge-fefo-expired";
                    badgeText = "Đã Hết Hạn";
                    fefoTier = "CRITICAL";
                    fefoColor = "danger";
                }
                else if (isCritical)
                {
                    badgeClass = "badge-fefo-expired";
                    badgeText = $"Khẩn Cấp ({daysRemaining} ngày)";
                    fefoTier = "CRITICAL";
                    fefoColor = "danger";
                }
                else if (isWarning || (si.ProductBatch.ClearanceDiscountPercent.HasValue && si.ProductBatch.ClearanceDiscountPercent.Value > 0))
                {
                    badgeClass = "badge-fefo-near";
                    badgeText = si.ProductBatch.ClearanceDiscountPercent.HasValue 
                        ? $"Cận Date Flash Sale -{si.ProductBatch.ClearanceDiscountPercent.Value:0.#}%" 
                        : $"Cảnh Báo ({daysRemaining} ngày)";
                    fefoTier = "WARNING";
                    fefoColor = "warning";
                }
                else if (daysRemaining > 540)
                {
                    badgeClass = "badge-fefo-fresh";
                    badgeText = "Lô Tươi Mới (> 18 Tháng)";
                    fefoTier = "SAFE";
                    fefoColor = "success";
                }
                else
                {
                    badgeClass = "badge-fefo-standard";
                    badgeText = $"An Toàn ({daysRemaining} ngày)";
                    fefoTier = "SAFE";
                    fefoColor = "success";
                }

                return new BatchStockItemViewModel
                {
                    ProductId = si.ProductBatch.ProductId,
                    ProductName = si.ProductBatch.Product?.Name ?? "Mỹ phẩm",
                    ProductSku = si.ProductBatch.Product?.Sku ?? "",
                    Barcode = si.ProductBatch.Product?.Barcode ?? "",
                    ThumbnailUrl = si.ProductBatch.Product?.ThumbnailUrl,
                    BatchId = si.ProductBatchId,
                    BatchNumber = si.ProductBatch.BatchNumber,
                    InitialQuantity = initQty,
                    CurrentQuantity = currQty,
                    PhysicalQuantity = si.PhysicalQuantity,
                    ReservedQuantity = si.ReservedQuantity,
                    AvailableQuantity = si.GetAvailableQuantity(),
                    ImportCost = si.ProductBatch.ImportCost,
                    SellingPrice = si.ProductBatch.Product?.SellingPrice ?? 0,
                    ManufacturingDate = si.ProductBatch.ManufacturingDate,
                    ExpDate = si.ProductBatch.ExpDate,
                    IsExpired = isExpired,
                    IsNearExpiry = isWarning || isCritical,
                    DaysRemaining = daysRemaining,
                    StoreId = si.StoreId.ToString(),
                    StoreName = selectedStore.Name,
                    SoldRatioPercent = Math.Clamp(soldRatio, 0, 100),
                    AlertDaysThreshold = threshold,
                    ClearanceDiscountPercent = si.ProductBatch.ClearanceDiscountPercent,
                    FefoBadgeClass = badgeClass,
                    FefoBadgeText = badgeText,
                    FefoStatusTier = fefoTier,
                    FefoStatusColor = fefoColor
                };
            }).ToList();

            // Recent stock transfers for this store
            var recentTransfers = await _stockTransferService.GetTransfersAsync(new StockTransferFilterDto
            {
                SourceStoreId = selectedStore.Id
            });
            var incomingTransfers = await _stockTransferService.GetTransfersAsync(new StockTransferFilterDto
            {
                DestinationStoreId = selectedStore.Id
            });
            var allRecent = recentTransfers.Concat(incomingTransfers)
                .GroupBy(t => t.Id)
                .Select(g => g.First())
                .OrderByDescending(t => t.RequestedAt)
                .Take(10)
                .ToList();

            // Immutable Inventory Logs for this store
            var logsQuery = _context.InventoryLogs
                .AsNoTracking()
                .Include(l => l.Store)
                .Include(l => l.ProductBatch).ThenInclude(pb => pb.Product)
                .Include(l => l.User)
                .Where(l => l.StoreId == selectedStore.Id);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                logsQuery = logsQuery.Where(l => l.ProductBatch.Product.Name.ToLower().Contains(kw)
                                              || l.ProductBatch.Product.Sku.ToLower().Contains(kw)
                                              || l.ProductBatch.BatchNumber.ToLower().Contains(kw)
                                              || (l.Note != null && l.Note.ToLower().Contains(kw)));
            }

            var recentLogs = await logsQuery
                .OrderByDescending(l => l.CreatedAt)
                .Take(50)
                .Select(l => new InventoryLogItemDto
                {
                    Id = l.Id,
                    StoreId = l.StoreId,
                    StoreName = l.Store.Name,
                    ProductId = l.ProductBatch.ProductId,
                    ProductName = l.ProductBatch.Product.Name,
                    ProductSku = l.ProductBatch.Product.Sku,
                    Barcode = l.ProductBatch.Product.Barcode,
                    BatchId = l.ProductBatchId,
                    BatchNumber = l.ProductBatch.BatchNumber,
                    ExpDate = l.ProductBatch.ExpDate,
                    TransactionType = l.TransactionType,
                    QuantityChanged = l.QuantityChanged,
                    QuantityBefore = l.QuantityBefore,
                    QuantityAfter = l.QuantityAfter,
                    Note = l.Note,
                    PerformerName = l.User != null ? (l.User.FullName ?? l.User.Username) : "Hệ Thống",
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync();

            var vm = new WarehouseOverviewViewModel
            {
                SelectedStoreId = currentStoreId,
                SelectedStoreName = currentStoreName,
                FilterStatus = filterStatus ?? "",
                Keyword = keyword ?? "",
                TotalStockValue = totalValue,
                TotalProducts = inventories.Select(i => i.ProductBatch.ProductId).Distinct().Count(),
                TotalSKUs = products.Count,
                TotalExpiredBatches = totalExpired,
                TotalNearExpiryBatches = totalNearExpiry,
                TotalLowStockBatches = totalLowStock,
                TotalSafeBatches = fefoSafe,
                FefoCriticalCount = fefoCritical,
                FefoWarningCount = fefoWarning,
                FefoSafeCount = fefoSafe,
                AvailableStores = stores.Select(s => new StoreLookupDto
                {
                    StoreId = s.Id.ToString(),
                    StoreName = s.Name,
                    IsCentralWarehouse = s.IsHeadquarters
                }).ToList(),
                BatchStockItems = batchItems,
                RecentTransfers = allRecent,
                InventoryLogs = recentLogs,
                NewBatchImport = new ImportBatchViewModel 
                { 
                    StoreId = currentStoreId,
                    ManufacturingDate = DateTime.Today.AddMonths(-1),
                    ExpDate = DateTime.Today.AddYears(2)
                }
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("INVENTORY_IMPORT")]
        public async Task<IActionResult> ImportBatch(
            [Bind(Prefix = "NewBatchImport")] ImportBatchViewModel? prefixedModel,
            ImportBatchViewModel? flatModel)
        {
            var model = (prefixedModel != null && prefixedModel.ProductId > 0) ? prefixedModel : flatModel;
            if (model == null || model.ProductId <= 0 || model.Quantity <= 0 || string.IsNullOrWhiteSpace(model.BatchNumber))
            {
                TempData["ErrorMessage"] = "Thông tin nhập kho không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction(nameof(Index), new { storeId = model?.StoreId ?? Request.Form["NewBatchImport.StoreId"].ToString() });
            }

            if (!int.TryParse(model.StoreId, out int parsedStoreId))
            {
                TempData["ErrorMessage"] = "Mã kho không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var effectiveStoreId = await GetEffectiveStoreIdAsync();
            if (effectiveStoreId.HasValue && parsedStoreId != effectiveStoreId.Value)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền nhập kho cho chi nhánh khác.";
                return RedirectToAction(nameof(Index), new { storeId = effectiveStoreId.Value });
            }

            // Validation: Hạn sử dụng phải > ngày hiện tại
            if (model.ExpDate.Date <= DateTime.UtcNow.Date)
            {
                TempData["ErrorMessage"] = "Hạn sử dụng lô hàng phải lớn hơn ngày hiện tại!";
                return RedirectToAction(nameof(Index), new { storeId = model.StoreId });
            }

            if (model.ExpDate.Date <= model.ManufacturingDate.Date)
            {
                TempData["ErrorMessage"] = "Hạn sử dụng phải sau ngày sản xuất!";
                return RedirectToAction(nameof(Index), new { storeId = model.StoreId });
            }

            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId);
            var batchNumber = model.BatchNumber.Trim().ToUpperInvariant();

            try
            {
                var product = await _context.Products.FindAsync(model.ProductId);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Sản phẩm không tồn tại trong hệ thống.";
                    return RedirectToAction(nameof(Index), new { storeId = model.StoreId });
                }

                // Kiểm tra lô đã tồn tại chưa
                var batch = await _context.ProductBatches
                    .FirstOrDefaultAsync(pb => pb.ProductId == model.ProductId && pb.BatchNumber == batchNumber);

                if (batch == null)
                {
                    batch = new ProductBatch
                    {
                        ProductId = model.ProductId,
                        BatchNumber = batchNumber,
                        ManufacturingDate = model.ManufacturingDate,
                        ExpDate = model.ExpDate,
                        ImportCost = model.ImportCost > 0 ? model.ImportCost : product.CostPrice,
                        InitialQuantity = model.Quantity,
                        CurrentQuantity = model.Quantity,
                        Status = "ACTIVE",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.ProductBatches.Add(batch);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    batch.CurrentQuantity += model.Quantity;
                    batch.InitialQuantity += model.Quantity;
                    batch.Status = "ACTIVE";
                    if (model.ImportCost > 0) batch.ImportCost = model.ImportCost;
                }

                // Cập nhật tồn kho cửa hàng
                var inv = await _context.StoreInventories
                    .FirstOrDefaultAsync(si => si.StoreId == parsedStoreId && si.ProductBatchId == batch.Id);

                int beforeQty = inv?.PhysicalQuantity ?? 0;
                if (inv == null)
                {
                    inv = new StoreInventory
                    {
                        StoreId = parsedStoreId,
                        ProductBatchId = batch.Id,
                        PhysicalQuantity = model.Quantity,
                        ReservedQuantity = 0,
                        LastUpdated = DateTime.UtcNow
                    };
                    _context.StoreInventories.Add(inv);
                }
                else
                {
                    inv.PhysicalQuantity += model.Quantity;
                    inv.LastUpdated = DateTime.UtcNow;
                }

                // Cập nhật giá vốn bình quân gia quyền (Weighted Average Cost) cho sản phẩm mỹ phẩm
                if (model.ImportCost > 0)
                {
                    var existingTotalQty = await _context.StoreInventories
                        .Where(si => si.ProductBatch.ProductId == product.Id && si.Id != inv.Id)
                        .SumAsync(si => si.PhysicalQuantity);
                    existingTotalQty += beforeQty; // Tồn kho trước đợt nhập này

                    decimal prevTotalVal = (decimal)existingTotalQty * product.CostPrice;
                    decimal newBatchVal = (decimal)model.Quantity * model.ImportCost;
                    int newTotalQty = existingTotalQty + model.Quantity;
                    if (newTotalQty > 0)
                    {
                        product.CostPrice = Math.Round((prevTotalVal + newBatchVal) / newTotalQty, 0);
                        product.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();

                // Tạo InventoryLog ghi sổ cái
                var log = new InventoryLog
                {
                    StoreId = parsedStoreId,
                    ProductBatchId = batch.Id,
                    UserId = userId > 0 ? userId : null,
                    TransactionType = "IMPORT",
                    QuantityChanged = model.Quantity,
                    QuantityBefore = beforeQty,
                    QuantityAfter = inv.PhysicalQuantity,
                    Note = $"Nhập kho lô {batch.BatchNumber} ({product.Name}). HSD: {model.ExpDate:dd/MM/yyyy}. {model.Notes}".Trim(),
                    CreatedAt = DateTime.UtcNow
                };
                _context.InventoryLogs.Add(log);
                await _context.SaveChangesAsync();

                // Kích hoạt thông báo Có hàng lại nếu có khách hàng đang theo dõi (GAP-05)
                if (_inventoryNotificationService != null)
                {
                    try
                    {
                        await _inventoryNotificationService.TriggerBackInStockAlertsAsync(product.Id, model.Quantity);
                    }
                    catch { }
                }

                TempData["SuccessMessage"] = $"Đã nhập kho thành công {model.Quantity} sản phẩm theo Lô {batch.BatchNumber}!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi nhập kho: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { storeId = model.StoreId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("INVENTORY_ADJUST")]
        public async Task<IActionResult> AdjustStock(StockAdjustmentViewModel model)
        {
            if (model == null || model.BatchId <= 0 || !int.TryParse(model.StoreId, out int parsedStoreId))
            {
                TempData["ErrorMessage"] = "Dữ liệu điều chỉnh không hợp lệ.";
                return RedirectToAction(nameof(Index), new { storeId = model?.StoreId });
            }

            var effectiveStoreId = await GetEffectiveStoreIdAsync();
            if (effectiveStoreId.HasValue && parsedStoreId != effectiveStoreId.Value)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền điều chỉnh kho cho chi nhánh khác.";
                return RedirectToAction(nameof(Index), new { storeId = effectiveStoreId.Value });
            }

            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId);

            try
            {
                var inv = await _context.StoreInventories
                    .Include(si => si.ProductBatch)
                        .ThenInclude(pb => pb.Product)
                    .FirstOrDefaultAsync(si => si.StoreId == parsedStoreId && si.ProductBatchId == model.BatchId);

                if (inv == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin tồn kho của lô hàng tại chi nhánh này.";
                    return RedirectToAction(nameof(Index), new { storeId = model.StoreId });
                }

                int beforeQty = inv.PhysicalQuantity;
                int afterQty = beforeQty + model.QuantityChange;
                if (afterQty < 0)
                {
                    TempData["ErrorMessage"] = "Số lượng giảm vượt quá số lượng tồn thực tế trong kho.";
                    return RedirectToAction(nameof(Index), new { storeId = model.StoreId });
                }

                inv.PhysicalQuantity = afterQty;
                inv.LastUpdated = DateTime.UtcNow;

                var batch = inv.ProductBatch;
                batch.CurrentQuantity = Math.Max(0, batch.CurrentQuantity + model.QuantityChange);
                if (batch.CurrentQuantity == 0) batch.Status = "DEPLETED";

                string transType = model.TransactionType == "5" ? "DISCARD_EXPIRED" : "ADJUSTMENT";
                var log = new InventoryLog
                {
                    StoreId = parsedStoreId,
                    ProductBatchId = batch.Id,
                    UserId = userId > 0 ? userId : null,
                    TransactionType = transType,
                    QuantityChanged = model.QuantityChange,
                    QuantityBefore = beforeQty,
                    QuantityAfter = afterQty,
                    Note = $"[Điều chỉnh kiểm kê]: {model.Notes}".Trim(),
                    CreatedAt = DateTime.UtcNow
                };
                _context.InventoryLogs.Add(log);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã cập nhật số lượng tồn kho (Chênh lệch: {model.QuantityChange:+#;-#;0}) và ghi sổ cái kiểm toán thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi điều chỉnh tồn kho: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { storeId = model.StoreId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("INVENTORY_EXPORT")]
        public async Task<IActionResult> DisposeStock(DisposeStockViewModel model)
        {
            if (model == null || model.BatchId <= 0 || model.Quantity <= 0 || !int.TryParse(model.StoreId, out int parsedStoreId))
            {
                TempData["ErrorMessage"] = "Thông tin xuất hủy không hợp lệ.";
                return RedirectToAction(nameof(Index), new { storeId = model?.StoreId });
            }

            var effectiveStoreId = await GetEffectiveStoreIdAsync();
            if (effectiveStoreId.HasValue && parsedStoreId != effectiveStoreId.Value)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xuất hủy kho cho chi nhánh khác.";
                return RedirectToAction(nameof(Index), new { storeId = effectiveStoreId.Value });
            }

            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId);

            try
            {
                var inv = await _context.StoreInventories
                    .Include(si => si.ProductBatch)
                        .ThenInclude(pb => pb.Product)
                    .FirstOrDefaultAsync(si => si.StoreId == parsedStoreId && si.ProductBatchId == model.BatchId);

                if (inv == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin tồn kho của lô hàng tại chi nhánh này.";
                    return RedirectToAction(nameof(Index), new { storeId = model.StoreId });
                }

                int available = inv.GetAvailableQuantity();
                if (model.Quantity > available)
                {
                    TempData["ErrorMessage"] = $"Số lượng xuất hủy ({model.Quantity}) vượt quá số lượng khả dụng ({available}).";
                    return RedirectToAction(nameof(Index), new { storeId = model.StoreId });
                }

                int beforeQty = inv.PhysicalQuantity;
                inv.PhysicalQuantity -= model.Quantity;
                inv.LastUpdated = DateTime.UtcNow;

                var batch = inv.ProductBatch;
                batch.CurrentQuantity = Math.Max(0, batch.CurrentQuantity - model.Quantity);
                if (batch.CurrentQuantity == 0) batch.Status = "DEPLETED";

                string reasonName = model.Reason switch
                {
                    "EXPIRED" => "Hủy hàng hết hạn sử dụng",
                    "DAMAGED" => "Hủy hàng hư hỏng/vỡ bao bì",
                    "RETURN_SUPPLIER" => "Xuất trả nhà cung cấp",
                    _ => "Xuất hủy kiểm kê"
                };

                var log = new InventoryLog
                {
                    StoreId = parsedStoreId,
                    ProductBatchId = batch.Id,
                    UserId = userId > 0 ? userId : null,
                    TransactionType = model.Reason == "EXPIRED" ? "DISCARD_EXPIRED" : "EXPORT_DISPOSE",
                    QuantityChanged = -model.Quantity,
                    QuantityBefore = beforeQty,
                    QuantityAfter = inv.PhysicalQuantity,
                    Note = $"[Xuất hủy/Hao hụt]: {reasonName}. {model.Notes}".Trim(),
                    CreatedAt = DateTime.UtcNow
                };
                _context.InventoryLogs.Add(log);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã xuất hủy thành công {model.Quantity} sản phẩm từ Lô {batch.BatchNumber} ({reasonName})!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi xuất hủy: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { storeId = model.StoreId });
        }

        #region Stock Transfer Endpoints

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("TRANSFER_CREATE")]
        public async Task<IActionResult> CreateTransfer([FromBody] CreateStockTransferDto dto)
        {
            if (dto == null || dto.Items == null || !dto.Items.Any())
            {
                return Json(new { success = false, message = "Vui lòng chọn ít nhất một sản phẩm cần điều chuyển." });
            }

            var effectiveStoreId = await GetEffectiveStoreIdAsync();
            if (effectiveStoreId.HasValue && dto.SourceStoreId != effectiveStoreId.Value)
            {
                return Json(new { success = false, message = "Bạn chỉ có thể tạo phiếu xuất chuyển từ chi nhánh của mình." });
            }

            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId);
            if (userId <= 0) userId = 1;

            try
            {
                var result = await _stockTransferService.CreateTransferRequestAsync(dto, userId);
                return Json(new
                {
                    success = true,
                    message = $"Đã tạo phiếu điều chuyển {result.TransferCode} thành công! Đang chờ phê duyệt.",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("TRANSFER_APPROVE")]
        public async Task<IActionResult> ApproveTransfer(int id, string? note)
        {
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId);
            if (userId <= 0) userId = 1;

            try
            {
                var result = await _stockTransferService.ApproveAndExportAsync(id, userId, note);
                return Json(new
                {
                    success = true,
                    message = $"Đã duyệt và xuất kho theo FEFO thành công cho phiếu {result.TransferCode}! Hàng đang trung chuyển.",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("TRANSFER_RECEIVE")]
        public async Task<IActionResult> ReceiveTransfer([FromBody] ReceiveTransferDto dto)
        {
            if (dto == null)
            {
                return Json(new { success = false, message = "Dữ liệu nhận hàng không hợp lệ." });
            }

            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId);
            if (userId <= 0) userId = 1;

            try
            {
                var result = await _stockTransferService.ReceiveAndImportAsync(dto, userId);
                return Json(new
                {
                    success = true,
                    message = $"Đã xác nhận nhập kho đích thành công cho phiếu {result.TransferCode}! Phiếu đã hoàn tất.",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("TRANSFER_APPROVE")]
        public async Task<IActionResult> CancelTransfer(int id, string reason)
        {
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId);
            if (userId <= 0) userId = 1;

            try
            {
                var ok = await _stockTransferService.CancelTransferAsync(id, userId, reason);
                return Json(new
                {
                    success = ok,
                    message = ok ? "Đã hủy phiếu điều chuyển thành công." : "Không tìm thấy phiếu điều chuyển."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [HasPermission("INVENTORY_VIEW")]
        public async Task<IActionResult> GetTransferDetails(int id)
        {
            var transfer = await _stockTransferService.GetTransferByIdAsync(id);
            if (transfer == null) return NotFound();
            return Json(new { success = true, data = transfer });
        }

        [HttpGet]
        [HasPermission("INVENTORY_VIEW")]
        public async Task<IActionResult> GetInventoryLogs(int? storeId, string? sku, string? batchNumber, string? transactionType)
        {
            var effectiveStoreId = await GetEffectiveStoreIdAsync();
            var targetStoreId = effectiveStoreId ?? storeId;

            var query = _context.InventoryLogs
                .AsNoTracking()
                .Include(l => l.Store)
                .Include(l => l.ProductBatch).ThenInclude(pb => pb.Product)
                .Include(l => l.User)
                .AsQueryable();

            if (targetStoreId.HasValue && targetStoreId.Value > 0)
            {
                query = query.Where(l => l.StoreId == targetStoreId.Value);
            }

            if (!string.IsNullOrWhiteSpace(sku))
            {
                var s = sku.Trim().ToLower();
                query = query.Where(l => l.ProductBatch.Product.Sku.ToLower().Contains(s) || l.ProductBatch.Product.Name.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(batchNumber))
            {
                var b = batchNumber.Trim().ToLower();
                query = query.Where(l => l.ProductBatch.BatchNumber.ToLower().Contains(b));
            }

            if (!string.IsNullOrWhiteSpace(transactionType))
            {
                query = query.Where(l => l.TransactionType == transactionType.Trim().ToUpperInvariant());
            }

            var logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Take(100)
                .Select(l => new InventoryLogItemDto
                {
                    Id = l.Id,
                    StoreId = l.StoreId,
                    StoreName = l.Store.Name,
                    ProductId = l.ProductBatch.ProductId,
                    ProductName = l.ProductBatch.Product.Name,
                    ProductSku = l.ProductBatch.Product.Sku,
                    Barcode = l.ProductBatch.Product.Barcode,
                    BatchId = l.ProductBatchId,
                    BatchNumber = l.ProductBatch.BatchNumber,
                    ExpDate = l.ProductBatch.ExpDate,
                    TransactionType = l.TransactionType,
                    QuantityChanged = l.QuantityChanged,
                    QuantityBefore = l.QuantityBefore,
                    QuantityAfter = l.QuantityAfter,
                    Note = l.Note,
                    PerformerName = l.User != null ? (l.User.FullName ?? l.User.Username) : "Hệ Thống",
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync();

            return Json(new { success = true, data = logs });
        }

        #endregion

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
