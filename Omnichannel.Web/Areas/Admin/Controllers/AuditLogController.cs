using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Security;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Security;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [HasPermission("SETTINGS_VIEW")]
    public class AuditLogController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? entityType,
            string? actionType,
            string? search,
            DateTime? fromDate,
            DateTime? toDate,
            int page = 1,
            int pageSize = 20)
        {
            var query = _context.AuditLogs.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(entityType))
            {
                query = query.Where(a => a.EntityType == entityType);
            }

            if (!string.IsNullOrWhiteSpace(actionType))
            {
                query = query.Where(a => a.Action == actionType);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(a => 
                    (a.UserName != null && a.UserName.ToLower().Contains(s)) ||
                    a.EntityId.ToLower().Contains(s) ||
                    (a.NewValues != null && a.NewValues.ToLower().Contains(s)) ||
                    (a.OldValues != null && a.OldValues.ToLower().Contains(s)) ||
                    (a.IpAddress != null && a.IpAddress.Contains(s)));
            }

            if (fromDate.HasValue)
            {
                query = query.Where(a => a.CreatedAt >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                var toEndOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(a => a.CreatedAt <= toEndOfDay);
            }

            var totalCount = await query.CountAsync();
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 10, 100);

            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    UserName = a.UserName,
                    Action = a.Action,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    OldValues = a.OldValues,
                    NewValues = a.NewValues,
                    IpAddress = a.IpAddress,
                    UserAgent = a.UserAgent,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            ViewBag.EntityType = entityType;
            ViewBag.ActionType = actionType;
            ViewBag.Search = search;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            // Danh sách EntityTypes & Actions chuẩn hóa cho hệ thống mỹ phẩm, tránh Full Table Scan 90.000+ dòng
            ViewBag.AvailableEntityTypes = new List<string>
            {
                "Alert", "Banner", "Category", "Customer", "Inventory", "Menu", "Order",
                "Product", "ProductBatch", "Promotion", "Role", "Store", "StoreInventory", "User"
            };

            ViewBag.AvailableActions = new List<string>
            {
                "CREATE", "UPDATE", "DELETE", "LOGIN", "STATUS_CHANGE", "IMPORT", "EXPORT", "TRANSFER", "ASSIGN_PERMISSION"
            };

            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> GetDetails(int id)
        {
            var log = await _context.AuditLogs.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            if (log == null) return NotFound();

            return Json(new
            {
                success = true,
                data = new
                {
                    log.Id,
                    log.Action,
                    log.EntityType,
                    log.EntityId,
                    log.UserName,
                    log.UserId,
                    log.IpAddress,
                    log.UserAgent,
                    CreatedAt = log.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"),
                    log.OldValues,
                    log.NewValues
                }
            });
        }
    }
}
