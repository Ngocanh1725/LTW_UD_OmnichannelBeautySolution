using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Security;
using Omnichannel.Application.Interfaces.Security;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Security;
using Omnichannel.Web.Extensions;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class DepartmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPermissionService _permissionService;
        private readonly IDelegationService _delegationService;
        private readonly IAuditLogService _auditLogService;

        public DepartmentController(
            ApplicationDbContext context,
            IPermissionService permissionService,
            IDelegationService delegationService,
            IAuditLogService auditLogService)
        {
            _context = context;
            _permissionService = permissionService;
            _delegationService = delegationService;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        [HasPermission("DEPARTMENT_VIEW")]
        public async Task<IActionResult> Index()
        {
            int actorId = User.GetUserId() ?? 0;
            bool isSuper = await _delegationService.IsSuperAdminAsync(actorId);
            var effectivePerms = actorId > 0
                ? await _permissionService.GetEffectivePermissionsAsync(actorId)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            bool canManage = isSuper || effectivePerms.Contains("DEPARTMENT_MANAGE");

            var departments = await _context.Departments
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(d => d.HeadUser)
                .Include(d => d.Parent)
                .OrderBy(d => d.SortOrder)
                .ThenBy(d => d.Id)
                .ToListAsync();

            var staffCounts = await _context.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(u => u.UserType >= 2 && !u.IsDeleted && u.DepartmentId != null)
                .GroupBy(u => u.DepartmentId!.Value)
                .Select(g => new { DeptId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DeptId, x => x.Count);

            var flatItems = departments.Select(d => new DepartmentTreeItemDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name,
                Description = d.Description,
                ParentId = d.ParentId,
                ParentName = d.Parent?.Name,
                HeadUserId = d.HeadUserId,
                HeadUserName = d.HeadUser?.Username,
                HeadFullName = d.HeadUser?.FullName,
                HeadJobTitle = d.HeadUser?.JobTitle,
                HeadAvatarUrl = d.HeadUser?.AvatarUrl,
                IsActive = d.IsActive,
                SortOrder = d.SortOrder,
                StaffCount = staffCounts.TryGetValue(d.Id, out int c) ? c : 0
            }).ToList();

            var lookup = flatItems.ToDictionary(x => x.Id);
            var roots = new List<DepartmentTreeItemDto>();

            foreach (var item in flatItems)
            {
                if (item.ParentId.HasValue && lookup.TryGetValue(item.ParentId.Value, out var parentNode) && item.ParentId.Value != item.Id)
                {
                    parentNode.Children.Add(item);
                }
                else
                {
                    roots.Add(item);
                }
            }

            int ComputeTotalStaff(DepartmentTreeItemDto node)
            {
                int sum = node.StaffCount;
                foreach (var child in node.Children)
                {
                    sum += ComputeTotalStaff(child);
                }
                node.TotalTreeStaffCount = sum;
                return sum;
            }

            foreach (var root in roots)
            {
                ComputeTotalStaff(root);
            }

            var eligibleHeads = await _context.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(u => u.UserType >= 2 && u.IsActive && !u.IsDeleted)
                .OrderBy(u => u.FullName)
                .Select(u => new UserSummaryDto
                {
                    Id = u.Id,
                    EmployeeCode = u.EmployeeCode,
                    Username = u.Username,
                    FullName = u.FullName,
                    Email = u.Email,
                    JobTitle = u.JobTitle,
                    DepartmentId = u.DepartmentId,
                    DepartmentName = u.Department != null ? u.Department.Name : null
                })
                .ToListAsync();

            var vm = new DepartmentIndexViewModel
            {
                TreeRoots = roots,
                FlatDepartments = flatItems,
                EligibleHeads = eligibleHeads,
                CanManage = canManage,
                TotalDepartments = flatItems.Count,
                ActiveDepartments = flatItems.Count(d => d.IsActive),
                TotalStaffCount = staffCounts.Values.Sum()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("DEPARTMENT_MANAGE")]
        public async Task<IActionResult> Save([FromBody] SaveDepartmentRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Code) || string.IsNullOrWhiteSpace(req.Name))
            {
                return Json(new { success = false, message = "Mã phòng ban và Tên phòng ban không được để trống." });
            }

            string cleanCode = req.Code.Trim().ToUpperInvariant().Replace(" ", "_");
            string cleanName = req.Name.Trim();

            if (req.Id > 0 && req.ParentId.HasValue && req.ParentId.Value == req.Id)
            {
                return Json(new { success = false, message = "Phòng ban không thể nhận chính nó làm phòng ban cha." });
            }

            bool codeExists = await _context.Departments
                .IgnoreQueryFilters()
                .AnyAsync(d => d.Code.ToUpper() == cleanCode && d.Id != req.Id);

            if (codeExists)
            {
                return Json(new { success = false, message = $"Mã phòng ban '{cleanCode}' đã tồn tại trong hệ thống." });
            }

            // Kiểm tra vòng lặp cây phòng ban nếu chỉnh sửa ParentId
            if (req.Id > 0 && req.ParentId.HasValue)
            {
                var allParentLinks = await _context.Departments
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Select(d => new { d.Id, d.ParentId })
                    .ToDictionaryAsync(d => d.Id, d => d.ParentId);

                int? cursor = req.ParentId;
                var visited = new HashSet<int> { req.Id };
                while (cursor.HasValue)
                {
                    if (!visited.Add(cursor.Value))
                    {
                        return Json(new { success = false, message = "Không thể chọn phòng ban con làm phòng ban cha (vòng lặp phân cấp)." });
                    }
                    cursor = allParentLinks.TryGetValue(cursor.Value, out var nextParent) ? nextParent : null;
                }
            }

            int actorId = User.GetUserId() ?? 0;
            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string ua = Request.Headers["User-Agent"].ToString();

            if (req.Id <= 0)
            {
                var dept = new Department
                {
                    Code = cleanCode,
                    Name = cleanName,
                    Description = req.Description?.Trim(),
                    ParentId = req.ParentId,
                    HeadUserId = req.HeadUserId,
                    SortOrder = req.SortOrder <= 0 ? 10 : req.SortOrder,
                    IsActive = req.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Departments.AddAsync(dept);
                await _context.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    action: "DEPARTMENT_CREATE",
                    entityType: "Department",
                    entityId: dept.Id.ToString(),
                    oldValues: null,
                    newValues: $"Tạo mới phòng ban '{dept.Name}' ({dept.Code}), ParentId={dept.ParentId}, HeadUserId={dept.HeadUserId}.",
                    userId: actorId,
                    userName: User.Identity?.Name,
                    ipAddress: ip,
                    userAgent: ua);

                return Json(new { success = true, message = $"Đã tạo mới phòng ban '{dept.Name}' thành công!" });
            }
            else
            {
                var dept = await _context.Departments
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(d => d.Id == req.Id);

                if (dept == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phòng ban cần cập nhật." });
                }

                string oldSummary = $"Name={dept.Name}, Code={dept.Code}, ParentId={dept.ParentId}, HeadUserId={dept.HeadUserId}, IsActive={dept.IsActive}";

                if (!string.Equals(dept.Code, "BAN_GIAM_DOC", StringComparison.OrdinalIgnoreCase))
                {
                    dept.Code = cleanCode;
                }
                dept.Name = cleanName;
                dept.Description = req.Description?.Trim();
                dept.ParentId = req.ParentId;
                dept.HeadUserId = req.HeadUserId;
                dept.SortOrder = req.SortOrder;
                dept.IsActive = req.IsActive;
                dept.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditLogService.LogAsync(
                    action: "DEPARTMENT_UPDATE",
                    entityType: "Department",
                    entityId: dept.Id.ToString(),
                    oldValues: oldSummary,
                    newValues: $"Name={dept.Name}, Code={dept.Code}, ParentId={dept.ParentId}, HeadUserId={dept.HeadUserId}, IsActive={dept.IsActive}",
                    userId: actorId,
                    userName: User.Identity?.Name,
                    ipAddress: ip,
                    userAgent: ua);

                return Json(new { success = true, message = $"Đã cập nhật thông tin phòng ban '{dept.Name}' thành công!" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("DEPARTMENT_MANAGE")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var dept = await _context.Departments
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dept == null)
            {
                return Json(new { success = false, message = "Phòng ban không tồn tại." });
            }

            if (string.Equals(dept.Code, "BAN_GIAM_DOC", StringComparison.OrdinalIgnoreCase) && dept.IsActive)
            {
                return Json(new { success = false, message = "Không thể tạm ẩn phòng ban gốc 'Ban Giám Đốc & Quản Trị Tối Cao'." });
            }

            dept.IsActive = !dept.IsActive;
            dept.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "DEPARTMENT_TOGGLE_STATUS",
                entityType: "Department",
                entityId: dept.Id.ToString(),
                oldValues: (!dept.IsActive).ToString(),
                newValues: dept.IsActive.ToString(),
                userId: User.GetUserId(),
                userName: User.Identity?.Name,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                userAgent: Request.Headers["User-Agent"].ToString());

            return Json(new
            {
                success = true,
                isActive = dept.IsActive,
                message = dept.IsActive
                    ? $"Đã kích hoạt hiển thị phòng ban '{dept.Name}'."
                    : $"Đã tạm ẩn phòng ban '{dept.Name}'."
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("DEPARTMENT_MANAGE")]
        public async Task<IActionResult> Delete(int id)
        {
            var dept = await _context.Departments
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dept == null)
            {
                return Json(new { success = false, message = "Phòng ban không tồn tại." });
            }

            if (string.Equals(dept.Code, "BAN_GIAM_DOC", StringComparison.OrdinalIgnoreCase))
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
                return Json(new { success = false, message = "Không được phép xóa phòng ban gốc 'Ban Giám Đốc & Quản Trị Tối Cao'." });
            }

            int staffCount = await _context.Users
                .IgnoreQueryFilters()
                .CountAsync(u => u.DepartmentId == id && !u.IsDeleted);

            if (staffCount > 0)
            {
                return Json(new
                {
                    success = false,
                    message = $"Không thể xóa phòng ban '{dept.Name}' vì đang có {staffCount} nhân sự trực thuộc. Vui lòng điều chuyển nhân sự sang phòng ban khác trước."
                });
            }

            bool hasChildren = await _context.Departments
                .IgnoreQueryFilters()
                .AnyAsync(d => d.ParentId == id);

            if (hasChildren)
            {
                return Json(new
                {
                    success = false,
                    message = $"Không thể xóa phòng ban '{dept.Name}' vì đang có phòng ban cấp dưới trực thuộc."
                });
            }

            // Gỡ liên kết Role.DepartmentId và AdminDelegationDepartment trước khi xóa
            var linkedRoles = await _context.Roles.Where(r => r.DepartmentId == id).ToListAsync();
            foreach (var r in linkedRoles)
            {
                r.DepartmentId = null;
            }

            var linkedScopes = await _context.AdminDelegationDepartments.Where(ad => ad.DepartmentId == id).ToListAsync();
            if (linkedScopes.Count > 0)
            {
                _context.AdminDelegationDepartments.RemoveRange(linkedScopes);
            }

            _context.Departments.Remove(dept);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(
                action: "DEPARTMENT_DELETE",
                entityType: "Department",
                entityId: id.ToString(),
                oldValues: $"{dept.Code} - {dept.Name}",
                newValues: null,
                userId: User.GetUserId(),
                userName: User.Identity?.Name,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                userAgent: Request.Headers["User-Agent"].ToString());

            return Json(new { success = true, message = $"Đã xóa phòng ban '{dept.Name}' thành công." });
        }
    }
}

