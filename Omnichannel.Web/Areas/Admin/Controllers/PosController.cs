using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Sales;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Security;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class PosController : Controller
    {
        private readonly IPosService _posService;
        private readonly ApplicationDbContext _context;

        public PosController(IPosService posService, ApplicationDbContext context)
        {
            _posService = posService;
            _context = context;
        }

        [HttpGet]
        [HasPermission("POS", "VIEW")]
        public async Task<IActionResult> Index(string? storeId = null)
        {
            var stores = await _context.Stores
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderByDescending(s => s.IsCentralWarehouse)
                .ToListAsync();

            var currentStore = !string.IsNullOrEmpty(storeId)
                ? stores.Find(s => s.StoreId == storeId)
                : stores.Find(s => !s.IsCentralWarehouse) ?? stores[0];

            ViewBag.Stores = stores;
            ViewBag.CurrentStore = currentStore;
            ViewBag.CashierName = User.FindFirstValue(ClaimTypes.GivenName) ?? User.Identity?.Name ?? "Thu Ngân Quầy";

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SearchProducts(string storeId, string keyword)
        {
            var results = await _posService.SearchProductsAsync(storeId, keyword);
            return Json(results);
        }

        [HttpGet]
        public async Task<IActionResult> ScanBarcode(string storeId, string barcode)
        {
            var result = await _posService.GetProductByBarcodeAsync(storeId, barcode);
            if (result == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm hoặc lô hàng đã hết hạn sử dụng." });
            }
            return Json(new { success = true, product = result });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("POS", "CREATE_ORDER")]
        public async Task<IActionResult> Checkout([FromBody] PosCheckoutRequest request)
        {
            var cashierId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "USR-SUPERADMIN-0001";
            var result = await _posService.ProcessPosCheckoutAsync(request, cashierId);
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> PrintReceipt(string orderId)
        {
            var receipt = await _posService.GetReceiptForPrintAsync(orderId);
            if (receipt == null) return NotFound();

            return View(receipt);
        }
    }
}
