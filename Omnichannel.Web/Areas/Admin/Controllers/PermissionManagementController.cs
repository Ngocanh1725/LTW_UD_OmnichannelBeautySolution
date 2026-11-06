using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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
    [HasPermission("PERMISSION_DELEGATE")]
    public class PermissionManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPermissionService _permissionService;
        private readonly IDelegationService _delegationService;
        private readonly IAuditLogService _auditLogService;

        public PermissionManagementController(
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
        [HasPermission("PERMISSION_DELEGATE")]
        public async Task<IActionResult> Index(int? roleId)
        {
            int actorId = User.GetUserId() ?? 0;
            var actorCtx = await _delegationService.GetContextAsync(actorId);
            var grantablePermSet = actorCtx.GrantablePermissionIds.ToHashSet();

            // Loại bỏ hoàn toàn Role Customer khỏi danh sách quản trị nhân sự
            var roles = await _context.Roles
                .AsNoTracking()
                .Include(r => r.Department)
                .Where(r => r.IsActive && r.Level > 0 && r.Name != "Customer" && r.Name != "CUSTOMER")
                .OrderByDescending(r => r.Level)
                .ThenBy(r => r.Id)
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    DisplayName = r.DisplayName ?? r.Name,
                    Description = r.Description,
                    Level = r.Level,
                    DepartmentId = r.DepartmentId,
                    DepartmentName = r.Department != null ? r.Department.Name : null,
                    IsSystem = r.IsSystem,
                    Color = r.Color
                })
                .ToListAsync();

            var scopedUsersQuery = _delegationService.ApplyUserScope(
                _context.Users.AsNoTracking().Where(u => u.IsActive),
                actorCtx);

            var users = await scopedUsersQuery
                .OrderBy(u => u.Id)
                .Select(u => new UserSummaryDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    FullName = u.FullName,
                    Email = u.Email,
                    StoreName = u.Store != null ? u.Store.Name : "Toàn hệ thống",
                    DepartmentId = u.DepartmentId,
                    DepartmentName = u.Department != null ? u.Department.Name : "Chưa phân phòng",
                    JobTitle = u.JobTitle
                })
                .ToListAsync();

            var selectedRoleId = roleId ?? roles.FirstOrDefault()?.Id ?? 0;

            var permissions = await _context.Permissions
                .AsNoTracking()
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Code)
                .ToListAsync();

            var moduleGroups = permissions
                .GroupBy(p => p.Module)
                .Select(g => new ModulePermissionGroupDto
                {
                    ModuleCode = g.Key,
                    ModuleName = GetModuleDisplayName(g.Key),
                    Permissions = g.Select(p => new PermissionItemDto
                    {
                        Id = p.Id,
                        Code = p.Code,
                        Name = p.Name,
                        Action = ExtractAction(p.Code),
                        Description = p.Description,
                        IsGrantableByActor = actorCtx.IsSuperAdmin || grantablePermSet.Contains(p.Id)
                    }).ToList()
                })
                .ToList();

            var activeRolePermIds = await _context.RolePermissions
                .AsNoTracking()
                .Where(rp => rp.RoleId == selectedRoleId)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            var allRolePermissions = await _context.RolePermissions.AsNoTracking().ToListAsync();
            var rolePermissionMap = allRolePermissions
                .GroupBy(rp => rp.RoleId)
                .ToDictionary(g => g.Key, g => g.Select(rp => rp.PermissionId).ToHashSet());
            ViewBag.RolePermissionMap = rolePermissionMap;
            ViewBag.IsSuperAdmin = actorCtx.IsSuperAdmin;

            var vm = new PermissionMatrixViewModel
            {
                Roles = roles,
                Users = users,
                ModuleGroups = moduleGroups,
                SelectedRoleId = selectedRoleId,
                ActivePermissionIdsForRole = activeRolePermIds,
                ActorContext = actorCtx
            };

            return View(vm);
        }

        [HttpGet]
        [HasPermission("PERMISSION_DELEGATE")]
        public async Task<IActionResult> GetRolePermissions(int roleId)
        {
            var permissionIds = await _context.RolePermissions
                .AsNoTracking()
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            return Json(new { success = true, roleId, permissionIds });
        }

        /// <summary>
        /// Lưu ma trận Role × Permission toàn cục: CHỈ SuperAdmin mới được phép thực thi.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("PERMISSION_MANAGE")]
        public async Task<IActionResult> SaveRoleMatrix([FromBody] RolePermissionUpdateDto dto)
        {
            int actorId = User.GetUserId() ?? 0;
            if (!await _delegationService.IsSuperAdminAsync(actorId))
            {
                return Json(new
                {
                    success = false,
                    message = "Chỉ Quản trị viên tối cao (SuperAdmin) mới có quyền thay đổi Ma trận Vai trò toàn cục."
                });
            }

            if (dto == null || dto.RoleId <= 0)
            {
                return Json(new { success = false, message = "Dữ liệu yêu cầu không hợp lệ." });
            }

            var role = await _context.Roles.FindAsync(dto.RoleId);
            if (role == null)
            {
                return Json(new { success = false, message = "Vai trò không tồn tại trong hệ thống." });
            }

            if (string.Equals(role.Name, "Customer", StringComparison.OrdinalIgnoreCase) || role.Level <= 0)
            {
                return Json(new { success = false, message = "Không thể gán quyền quản trị nội bộ cho vai trò Khách hàng (Customer)." });
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var existing = await _context.RolePermissions.Where(rp => rp.RoleId == dto.RoleId).ToListAsync();
                    _context.RolePermissions.RemoveRange(existing);

                    if (dto.PermissionIds != null && dto.PermissionIds.Any())
                    {
                        var newMappings = dto.PermissionIds.Distinct().Select(pid => new RolePermission
                        {
                            RoleId = dto.RoleId,
                            PermissionId = pid
                        });
                        await _context.RolePermissions.AddRangeAsync(newMappings);
                    }

                    // Cập nhật SecurityStamp cho toàn bộ user thuộc Role này để buộc làm mới claims
                    var roleUserIds = await _context.UserRoles
                        .Where(ur => ur.RoleId == dto.RoleId)
                        .Select(ur => ur.UserId)
                        .ToListAsync();

                    if (roleUserIds.Count > 0)
                    {
                        var affectedUsers = await _context.Users
                            .IgnoreQueryFilters()
                            .Where(u => roleUserIds.Contains(u.Id))
                            .ToListAsync();

                        foreach (var u in affectedUsers)
                        {
                            u.SecurityStamp = Guid.NewGuid().ToString("N");
                            u.UpdatedAt = DateTime.UtcNow;
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    await _permissionService.InvalidateRolePermissionCacheAsync(dto.RoleId);

                    string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                    string userAgent = Request.Headers["User-Agent"].ToString();
                    await _auditLogService.LogAsync(
                        action: "PERMISSION_ROLE_UPDATE",
                        entityType: "Role",
                        entityId: role.Id.ToString(),
                        oldValues: null,
                        newValues: $"Cập nhật phân quyền cho vai trò '{role.Name}'. Số quyền gán: {dto.PermissionIds?.Count ?? 0}.",
                        userId: actorId,
                        userName: User.Identity?.Name,
                        ipAddress: ipAddress,
                        userAgent: userAgent
                    );

                    return Json(new { success = true, message = $"Cập nhật ma trận phân quyền cho vai trò '{role.DisplayName ?? role.Name}' thành công!" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = $"Lỗi khi lưu phân quyền: {ex.Message}" });
                }
            });
        }

        [HttpGet]
        [HasPermission("PERMISSION_DELEGATE")]
        public async Task<IActionResult> GetUserOverrides(int userId)
        {
            int actorId = User.GetUserId() ?? 0;
            var actorCtx = await _delegationService.GetContextAsync(actorId);

            if (!actorCtx.IsSuperAdmin && actorId != userId && !await _delegationService.CanManageUserAsync(actorId, userId))
            {
                return Json(new { success = false, message = "Nhân sự này không nằm trong phạm vi quản lý của bạn." });
            }

            var grantableSet = actorCtx.GrantablePermissionIds.ToHashSet();
            var breakdown = await _permissionService.GetUserPermissionBreakdownAsync(userId);

            foreach (var item in breakdown)
            {
                item.CanActorGrant = actorCtx.IsSuperAdmin || grantableSet.Contains(item.PermissionId);
            }

            return Json(new { success = true, userId, breakdown });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("PERMISSION_DELEGATE")]
        public async Task<IActionResult> SaveUserOverride([FromBody] UserOverrideDto dto)
        {
            if (dto == null || dto.UserId <= 0 || dto.PermissionId <= 0)
            {
                return Json(new { success = false, message = "Tham số đặc cách phân quyền không hợp lệ." });
            }

            int actorId = User.GetUserId() ?? 0;
            var result = !dto.OverrideStatus.HasValue
                ? await _delegationService.RemoveUserPermissionOverrideAsync(actorId, dto.UserId, dto.PermissionId)
                : await _delegationService.GrantUserPermissionAsync(actorId, dto.UserId, dto.PermissionId, dto.OverrideStatus.Value, dto.Reason);

            return Json(new
            {
                success = result.Succeeded,
                message = result.Message,
                errorCode = result.ErrorCode
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("PERMISSION_DELEGATE")]
        public async Task<IActionResult> SaveDelegationScope([FromBody] SaveDelegationScopeRequest request)
        {
            int actorId = User.GetUserId() ?? 0;
            var result = await _delegationService.SaveDelegationScopeAsync(actorId, request);
            return Json(new
            {
                success = result.Succeeded,
                message = result.Message,
                errorCode = result.ErrorCode
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("USER_ASSIGN_ROLE")]
        public async Task<IActionResult> AssignUserRoles(int userId, [FromBody] List<int> roleIds)
        {
            int actorId = User.GetUserId() ?? 0;
            var result = await _delegationService.AssignRolesAsync(actorId, userId, roleIds);
            return Json(new
            {
                success = result.Succeeded,
                message = result.Message,
                errorCode = result.ErrorCode
            });
        }

        #region Helpers
        private static string ExtractAction(string code)
        {
            var parts = code.Split(new[] { '_', ':' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 1 ? parts[^1].ToUpperInvariant() : "ACCESS";
        }

        private static string GetModuleDisplayName(string moduleCode) => moduleCode.ToUpperInvariant() switch
        {
            "DASHBOARD" => "Tổng Quan Hệ Thống (Dashboard)",
            "CATALOG" or "PRODUCT" => "Danh Mục & Sản Phẩm (Product)",
            "INVENTORY" => "Quản Lý Kho & Lô Hàng FEFO (Inventory)",
            "POS" => "Bán Lẻ Điểm Bán (POS)",
            "ORDER" => "Quản Lý Đơn Hàng Đa Kênh (Order)",
            "CUSTOMER" or "CRM" => "Khách Hàng & Hồ Sơ CRM (Customer)",
            "CMS" => "Biên Tập Nội Dung & Banner (CMS)",
            "REPORT" => "Báo Cáo & Thống Kê (Report)",
            "FINANCE" => "Tài Chính & Kế Toán Sổ Cái (Finance)",
            "PROMOTION" => "Khuyến Mãi & Voucher (Promotion)",
            "LOYALTY" => "Ví Điểm & Hạng Thành Viên (Loyalty)",
            "STOCK_TRANSFER" or "TRANSFER" => "Điều Chuyển Kho Liên Chi Nhánh (Stock Transfer)",
            "SETTINGS" => "Cài Đặt Hệ Thống (Settings)",
            "SECURITY" => "Định Danh & Phân Quyền Bảo Mật (Security)",
            "HR" => "Quản Trị Nhân Sự & Phòng Ban (HR)",
            _ => moduleCode
        };
        #endregion
    }
}
