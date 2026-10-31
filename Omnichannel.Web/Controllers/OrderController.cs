using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Web.Extensions;

namespace Omnichannel.Web.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> History()
        {
            var userId = User.GetUserId();
            if (!userId.HasValue)
            {
                return Challenge();
            }

            // Chống IDOR: Chỉ lấy đơn hàng thuộc sở hữu của User hiện tại
            var orders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Where(o => o.UserId == userId.Value)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.GetUserId();
            if (!userId.HasValue)
            {
                return Challenge();
            }

            var order = await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Include(o => o.Store)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound("Không tìm thấy đơn hàng yêu cầu.");
            }

            // Chống IDOR: Khách hàng A tuyệt đối KHÔNG xem được đơn hàng của khách hàng B
            if (order.UserId != userId.Value && !User.IsSuperAdmin())
            {
                return Forbid();
            }

            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> Tracking(string? orderCode)
        {
            if (string.IsNullOrWhiteSpace(orderCode))
            {
                return View(null);
            }

            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode.Trim());

            if (order == null)
            {
                ViewBag.ErrorMessage = $"Không tìm thấy đơn hàng với mã '{orderCode}'.";
                return View(null);
            }

            // Chống IDOR: Nếu đơn hàng đã gắn với một User và User đang đăng nhập không phải là chủ sở hữu
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userId = User.GetUserId();
                if (order.UserId.HasValue && order.UserId.Value != userId && !User.IsSuperAdmin())
                {
                    return Forbid();
                }
            }

            return View(order);
        }
    }
}
