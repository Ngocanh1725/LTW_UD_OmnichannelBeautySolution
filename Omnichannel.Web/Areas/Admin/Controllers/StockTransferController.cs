using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Inventory;
using Omnichannel.Application.Interfaces.Inventory;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Security;
using Omnichannel.Web.Extensions;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class StockTransferController : Controller
    {
        private readonly IStockTransferService _transferService;
        private readonly ApplicationDbContext _context;

        public StockTransferController(IStockTransferService transferService, ApplicationDbContext context)
        {
            _transferService = transferService;
            _context = context;
        }

        [HttpGet]
        [HasPermission("TRANSFER_CREATE")]
        public async Task<IActionResult> Index([FromQuery] StockTransferFilterDto filter, CancellationToken cancellationToken)
        {
            var effectiveStoreId = await GetEffectiveStoreIdAsync();
            if (effectiveStoreId.HasValue)
            {
                filter.SourceStoreId ??= effectiveStoreId.Value;
            }

            var transfers = await _transferService.GetTransfersAsync(filter, cancellationToken);
            var storesQuery = _context.Stores.AsNoTracking().Where(s => s.IsActive);
            if (effectiveStoreId.HasValue) storesQuery = storesQuery.Where(s => s.Id == effectiveStoreId.Value);
            var stores = await storesQuery.OrderBy(s => s.Name).ToListAsync(cancellationToken);

            ViewBag.Stores = stores;
            ViewBag.Filter = filter;

            return View(transfers);
        }

        [HttpGet]
        [HasPermission("TRANSFER_CREATE")]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var stores = await _context.Stores
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync(cancellationToken);

            var products = await _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .Select(p => new { p.Id, p.Name, p.Sku, p.Barcode })
                .ToListAsync(cancellationToken);

            ViewBag.Stores = stores;
            ViewBag.Products = products;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("TRANSFER_CREATE")]
        public async Task<IActionResult> Create([FromBody] CreateStockTransferDto dto, CancellationToken cancellationToken)
        {
            var effectiveStoreId = await GetEffectiveStoreIdAsync();
            if (effectiveStoreId.HasValue && dto.SourceStoreId != effectiveStoreId.Value)
            {
                return Json(new { success = false, message = "Bạn chỉ có thể tạo phiếu xuất chuyển từ chi nhánh của mình." });
            }

            if (dto.SourceStoreId == dto.DestinationStoreId)
            {
                return Json(new { success = false, message = "Kho nguồn và kho đích không được trùng nhau." });
            }

            if (dto.Items == null || !dto.Items.Any())
            {
                return Json(new { success = false, message = "Vui lòng chọn ít nhất một sản phẩm cần chuyển." });
            }

            int userId = GetCurrentUserId();

            try
            {
                var result = await _transferService.CreateTransferRequestAsync(dto, userId, cancellationToken);
                return Json(new { success = true, redirectUrl = Url.Action(nameof(Details), new { id = result.Id }) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [HasPermission("TRANSFER_CREATE")]
        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
        {
            var transfer = await _transferService.GetTransferByIdAsync(id, cancellationToken);
            if (transfer == null)
            {
                return NotFound();
            }

            return View(transfer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("TRANSFER_APPROVE")]
        public async Task<IActionResult> Approve(int id, string? note, CancellationToken cancellationToken)
        {
            int userId = GetCurrentUserId();
            try
            {
                var result = await _transferService.ApproveAndExportAsync(id, userId, note, cancellationToken);
                TempData["SuccessMessage"] = $"Đã duyệt xuất kho theo FEFO cho phiếu {result.TransferCode}. Đang trung chuyển {result.TotalSentQuantity} sản phẩm.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi duyệt xuất kho: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("TRANSFER_RECEIVE")]
        public async Task<IActionResult> Receive([FromBody] ReceiveTransferDto dto, CancellationToken cancellationToken)
        {
            int userId = GetCurrentUserId();
            try
            {
                var result = await _transferService.ReceiveAndImportAsync(dto, userId, cancellationToken);
                return Json(new { success = true, message = $"Xác nhận nhập kho thành công cho phiếu {result.TransferCode}!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("TRANSFER_APPROVE")]
        public async Task<IActionResult> Cancel(int id, string reason, CancellationToken cancellationToken)
        {
            int userId = GetCurrentUserId();
            try
            {
                var success = await _transferService.CancelTransferAsync(id, userId, reason, cancellationToken);
                if (success)
                {
                    TempData["SuccessMessage"] = "Đã hủy phiếu yêu cầu chuyển kho.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi hủy phiếu: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        private int GetCurrentUserId()
        {
            var val = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(val) && int.TryParse(val, out var uid))
            {
                return uid;
            }
            // Fallback SuperAdmin ID 1
            return 1;
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
