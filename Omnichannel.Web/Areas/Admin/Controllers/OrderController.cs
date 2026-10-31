using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Sales;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Security;
using Omnichannel.Web.Extensions;
using Omnichannel.Web.Hubs;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IOrderManagementService _orderService;
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<SystemHub> _hubContext;

        public OrderController(
            IOrderManagementService orderService,
            ApplicationDbContext context,
            IHubContext<SystemHub> hubContext)
        {
            _orderService = orderService;
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet]
        [HasPermission("ORDER_VIEW")]
        public async Task<IActionResult> Index([FromQuery] AdminOrderFilterRequest filter)
        {
            filter ??= new AdminOrderFilterRequest();
            var effectiveStoreId = await GetEffectiveStoreIdAsync();
            if (effectiveStoreId.HasValue)
            {
                filter.StoreId = effectiveStoreId.Value;
            }

            var pagedResult = await _orderService.GetOrdersPagedAsync(filter);

            var storesQuery = _context.Stores.AsNoTracking().Where(s => s.IsActive);
            if (effectiveStoreId.HasValue)
            {
                storesQuery = storesQuery.Where(s => s.Id == effectiveStoreId.Value);
            }
            var stores = await storesQuery.OrderBy(s => s.Name).ToListAsync();

            ViewBag.Stores = new SelectList(stores, "Id", "Name", filter.StoreId);
            ViewBag.CurrentFilter = filter;
            ViewBag.IsStoreScoped = effectiveStoreId.HasValue;

            if (Request.IsHtmx())
            {
                return PartialView("_OrderList", pagedResult);
            }

            return View(pagedResult);
        }

        [HttpGet]
        [HasPermission("ORDER_VIEW")]
        public async Task<IActionResult> Details(int id)
        {
            var effectiveStoreId = await GetEffectiveStoreIdAsync();
            var order = await _orderService.GetOrderDetailAsync(id);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng yêu cầu.";
                return RedirectToAction(nameof(Index));
            }

            if (effectiveStoreId.HasValue && order.StoreId != effectiveStoreId.Value)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập đơn hàng của chi nhánh khác.";
                return RedirectToAction(nameof(Index));
            }

            return View(order);
        }

        [HttpGet]
        [ActionName("Detail")]
        [HasPermission("ORDER_VIEW")]
        public async Task<IActionResult> Detail(int id)
        {
            return await Details(id);
        }

        [HttpGet]
        [HasPermission("ORDER_VIEW")]
        public async Task<IActionResult> PrintShippingLabel(int id)
        {
            var effectiveStoreId = await GetEffectiveStoreIdAsync();
            var order = await _orderService.GetOrderDetailAsync(id);
            if (order == null)
            {
                return NotFound("Không tìm thấy đơn hàng để in phiếu vận đơn.");
            }

            if (effectiveStoreId.HasValue && order.StoreId != effectiveStoreId.Value)
            {
                return Forbid();
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("ORDER_EDIT")]
        public async Task<IActionResult> UpdateStatus(int id, string status, string? note = null, string? reason = null)
        {
            var effectiveStoreId = await GetEffectiveStoreIdAsync();
            if (effectiveStoreId.HasValue)
            {
                var existingOrder = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
                if (existingOrder == null || existingOrder.StoreId != effectiveStoreId.Value)
                {
                    var deniedMsg = "Bạn không có quyền cập nhật đơn hàng của chi nhánh khác.";
                    if (Request.IsHtmx()) return BadRequest(new { success = false, message = deniedMsg });
                    TempData["ErrorMessage"] = deniedMsg;
                    return RedirectToAction(nameof(Index));
                }
            }

            var (success, message) = await _orderService.UpdateOrderStatusAsync(id, status, note, reason);

            if (success)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", message, "success");
            }
            else
            {
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", message, "error");
            }

            if (Request.IsHtmx())
            {
                if (success)
                {
                    Response.Headers.Append("HX-Trigger", "reloadOrderTable");
                    return Ok(new { success = true, message });
                }
                return BadRequest(new { success = false, message });
            }

            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        [HasPermission("ORDER_VIEW")]
        public async Task<IActionResult> Print(int id)
        {
            var effectiveStoreId = await GetEffectiveStoreIdAsync();
            var order = await _orderService.GetOrderDetailAsync(id);
            if (order == null)
            {
                return NotFound("Không tìm thấy đơn hàng để in hóa đơn.");
            }

            if (effectiveStoreId.HasValue && order.StoreId != effectiveStoreId.Value)
            {
                return Forbid();
            }

            return View(order);
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
