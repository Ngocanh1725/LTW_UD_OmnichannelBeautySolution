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
        public async Task<IActionResult> Index(int? roleId, int? userId, string? view = null)
        {
            int actorId = User.GetUserId() ?? 0;
            var actorCtx = await _delegationService.GetContextAsync(actorId);

            // Nếu Admin nhỏ cố yêu cầu chế độ ma trận vai trò toàn cục -> chặn 403
            if (!actorCtx.IsSuperAdmin && string.Equals(view, "global-matrix", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("AccessDenied", "Auth", new
                {
                    area = "Admin",
                    attemptedPath = "/Admin/PermissionManagement?view=global-matrix",
                    message = "Chỉ Admin Tổng (SuperAdmin) mới có quyền truy cập Ma trận Vai trò toàn cục."
                });
            }

            var grantablePermSet = actorCtx.GrantablePermissionIds.ToHashSet();
            var assignableRoleIds = await _delegationService.GetAssignableRoleIdsAsync(actorId);
            var assignableRoleSet = assignableRoleIds.ToHashSet();

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

            var assignableRoles = roles.Where(r => actorCtx.IsSuperAdmin || assignableRoleSet.Contains(r.Id)).ToList();

            var scopedUsersQuery = _delegationService.ApplyUserScope(
                _context.Users
                    .AsNoTracking()
                    .Include(u => u.Department)
                    .Include(u => u.Store)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .Where(u => u.IsActive && !u.IsDeleted && u.UserType >= 2),
                actorCtx);

            var users = await scopedUsersQuery
                .Where(u => actorCtx.IsSuperAdmin || u.Id != actorId)
                .OrderBy(u => u.Id)
                .Select(u => new UserSummaryDto
                {
                    Id = u.Id,
                    EmployeeCode = u.EmployeeCode,
                    Username = u.Username,
                    FullName = u.FullName,
                    Email = u.Email,
                    StoreName = u.Store != null ? u.Store.Name : "Toàn hệ thống",
                    DepartmentId = u.DepartmentId,
                    DepartmentName = u.Department != null ? u.Department.Name : "Chưa phân phòng",
                    JobTitle = u.JobTitle,
                    RoleNames = u.UserRoles
                        .Where(ur => ur.Role != null && ur.Role.IsActive && ur.Role.Name != "Customer")
                        .Select(ur => ur.Role.DisplayName ?? ur.Role.Name)
                        .ToList(),
                    RoleIds = u.UserRoles
                        .Where(ur => ur.Role != null && ur.Role.IsActive && ur.Role.Name != "Customer")
                        .Select(ur => ur.RoleId)
                        .ToList()
                })
                .ToListAsync();

            var selectedRoleId = roleId ?? roles.FirstOrDefault()?.Id ?? 0;
            var selectedUserId = userId ?? users.FirstOrDefault()?.Id ?? 0;

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
                AssignableRoles = assignableRoles,
                Users = users,
                ModuleGroups = moduleGroups,
                SelectedRoleId = selectedRoleId,
                SelectedUserId = selectedUserId,
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("PERMISSION_MANAGE")]
        public async Task<IActionResult> SaveRoleMatrix([FromBody] RolePermissionUpdateDto dto)
        {
            int actorId = User.GetUserId() ?? 0;
            if (!await _delegationService.IsSuperAdminAsync(actorId))
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("PERMISSION_MANAGE")]
        public async Task<IActionResult> SaveBulkRoleMatrix([FromBody] BulkRolePermissionUpdateDto dto)
        {
            int actorId = User.GetUserId() ?? 0;
            if (!await _delegationService.IsSuperAdminAsync(actorId))
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
                return Json(new
                {
                    success = false,
                    message = "Chỉ Quản trị viên tối cao (SuperAdmin) mới có quyền lưu Ma trận Vai trò toàn cục."
                });
            }

            if (dto?.Roles == null || dto.Roles.Count == 0)
            {
                return Json(new { success = false, message = "Không có dữ liệu ma trận vai trò để lưu." });
            }

            var editableRoles = await _context.Roles
                .Where(r => r.IsActive && r.Level > 0 && r.Level < 100 && r.Name != "SuperAdmin" && r.Name != "Customer")
                .ToDictionaryAsync(r => r.Id);

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var updatedRoleNames = new List<string>();
                    foreach (var item in dto.Roles)
                    {
                        if (!editableRoles.TryGetValue(item.RoleId, out var role))
                            continue;

                        var existing = await _context.RolePermissions.Where(rp => rp.RoleId == role.Id).ToListAsync();
                        _context.RolePermissions.RemoveRange(existing);

                        var distinctPermIds = (item.PermissionIds ?? new List<int>()).Distinct().ToList();
                        foreach (var pid in distinctPermIds)
                        {
                            await _context.RolePermissions.AddAsync(new RolePermission
                            {
                                RoleId = role.Id,
                                PermissionId = pid
                            });
                        }

                        updatedRoleNames.Add($"{role.Name}({distinctPermIds.Count})");
                    }

                    // Làm mới SecurityStamp toàn bộ nhân sự nội bộ (trừ SuperAdmin)
                    var staffUsers = await _context.Users
                        .IgnoreQueryFilters()
                        .Where(u => u.UserType >= 2)
                        .ToListAsync();
                    foreach (var u in staffUsers)
                    {
                        u.SecurityStamp = Guid.NewGuid().ToString("N");
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    foreach (var roleIdKey in editableRoles.Keys)
                    {
                        await _permissionService.InvalidateRolePermissionCacheAsync(roleIdKey);
                    }

                    await _auditLogService.LogAsync(
                        action: "PERMISSION_BULK_MATRIX_UPDATE",
                        entityType: "RolePermissionMatrix",
                        entityId: "ALL_ROLES",
                        oldValues: null,
                        newValues: $"Lưu đồng loạt ma trận phân quyền cho {updatedRoleNames.Count} vai trò: {string.Join(", ", updatedRoleNames)}",
                        userId: actorId,
                        userName: User.Identity?.Name,
                        ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                        userAgent: Request.Headers["User-Agent"].ToString());

                    return Json(new
                    {
                        success = true,
                        message = $"Đã lưu Ma trận Phân quyền cho {updatedRoleNames.Count} vai trò và đồng bộ Cache L1 toàn hệ thống!"
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = $"Lỗi lưu ma trận: {ex.Message}" });
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("PERMISSION_DELEGATE")]
        public async Task<IActionResult> ClearAllCache()
        {
            var allRoles = await _context.Roles.AsNoTracking().Select(r => r.Id).ToListAsync();
            foreach (var rId in allRoles)
            {
                await _permissionService.InvalidateRolePermissionCacheAsync(rId);
            }
            return Json(new { success = true, message = "Đã làm sạch bộ nhớ đệm phân quyền L1 (IMemoryCache) cho toàn bộ người dùng!" });
        }

        [HttpGet]
        [HasPermission("PERMISSION_DELEGATE")]
        public async Task<IActionResult> GetUserOverrides(int userId)
        {
            int actorId = User.GetUserId() ?? 0;
            var actorCtx = await _delegationService.GetContextAsync(actorId);

            if (!actorCtx.IsSuperAdmin && actorId != userId && !await _delegationService.CanManageUserAsync(actorId, userId))
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
                return Json(new
                {
                    success = false,
                    message = "Từ chối truy cập (403): Nhân sự này không nằm trong phạm vi phòng ban hoặc cấp bậc quản lý của bạn.",
                    errorCode = "TARGET_OUT_OF_SCOPE"
                });
            }

            var grantableSet = actorCtx.GrantablePermissionIds.ToHashSet();
            var breakdown = await _permissionService.GetUserPermissionBreakdownAsync(userId);

            foreach (var item in breakdown)
            {
                item.ModuleDisplayName = GetModuleDisplayName(item.Module);
                item.CanActorGrant = actorCtx.IsSuperAdmin || grantableSet.Contains(item.PermissionId);
            }

            var currentRoleIds = await _context.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == userId && ur.Role.IsActive && ur.Role.Name != "Customer")
                .Select(ur => ur.RoleId)
                .ToListAsync();

            var assignableRoleIds = await _delegationService.GetAssignableRoleIdsAsync(actorId);

            return Json(new
            {
                success = true,
                userId,
                currentRoleIds,
                assignableRoleIds,
                canAssignRole = actorCtx.IsSuperAdmin || actorCtx.CanAssignRole,
                breakdown
            });
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

            // Khi Thu hồi quyền (Deny = false), bắt buộc phải có lý do ghi chú
            if (dto.OverrideStatus.HasValue && dto.OverrideStatus.Value == false && string.IsNullOrWhiteSpace(dto.Reason))
            {
                return Json(new
                {
                    success = false,
                    message = "Vui lòng nhập lý do bắt buộc khi Thu hồi (Deny) quyền của nhân sự.",
                    errorCode = "REASON_REQUIRED"
                });
            }

            int actorId = User.GetUserId() ?? 0;
            var result = !dto.OverrideStatus.HasValue
                ? await _delegationService.RemoveUserPermissionOverrideAsync(actorId, dto.UserId, dto.PermissionId)
                : await _delegationService.GrantUserPermissionAsync(actorId, dto.UserId, dto.PermissionId, dto.OverrideStatus.Value, dto.Reason);

            if (!result.Succeeded)
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
            }

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
        public async Task<IActionResult> ResetUserOverrides([FromBody] ResetUserOverridesDto dto)
        {
            if (dto == null || dto.UserId <= 0)
            {
                return Json(new { success = false, message = "Tham số người dùng không hợp lệ." });
            }

            int actorId = User.GetUserId() ?? 0;
            var actorCtx = await _delegationService.GetContextAsync(actorId);

            if (!actorCtx.IsSuperAdmin && !await _delegationService.CanManageUserAsync(actorId, dto.UserId))
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
                return Json(new
                {
                    success = false,
                    message = "Từ chối truy cập (403): Nhân sự này nằm ngoài phạm vi quản lý của bạn.",
                    errorCode = "TARGET_OUT_OF_SCOPE"
                });
            }

            var grantableSet = actorCtx.GrantablePermissionIds.ToHashSet();
            var overrides = await _context.UserPermissions
                .Where(up => up.UserId == dto.UserId)
                .ToListAsync();

            var removable = actorCtx.IsSuperAdmin
                ? overrides
                : overrides.Where(up => grantableSet.Contains(up.PermissionId)).ToList();

            if (removable.Count == 0)
            {
                return Json(new { success = true, message = "Nhân sự này hiện không có quyền đặc cách (Override) nào trong phạm vi của bạn." });
            }

            _context.UserPermissions.RemoveRange(removable);

            var targetUser = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == dto.UserId);
            if (targetUser != null)
            {
                targetUser.SecurityStamp = Guid.NewGuid().ToString("N");
                targetUser.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            await _permissionService.InvalidateUserPermissionCacheAsync(dto.UserId);

            await _auditLogService.LogAsync(
                action: "DELEGATION_RESET_USER_OVERRIDES",
                entityType: "UserPermission",
                entityId: dto.UserId.ToString(),
                oldValues: $"RemovedCount={removable.Count}",
                newValues: "Khôi phục quyền mặc định theo vai trò",
                userId: actorId,
                userName: actorCtx.ActorUsername,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                userAgent: Request.Headers["User-Agent"].ToString());

            return Json(new
            {
                success = true,
                message = $"Đã khôi phục mặc định theo vai trò (xóa {removable.Count} quyền đặc cách cá nhân) thành công!"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("PERMISSION_DELEGATE")]
        public async Task<IActionResult> SaveDelegationScope([FromBody] SaveDelegationScopeRequest request)
        {
            int actorId = User.GetUserId() ?? 0;
            var result = await _delegationService.SaveDelegationScopeAsync(actorId, request);
            if (!result.Succeeded)
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
            }
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
        public async Task<IActionResult> AssignUserRoles([FromBody] AssignUserRolesRequest req)
        {
            if (req == null || req.UserId <= 0)
            {
                return Json(new { success = false, message = "Tham số gán vai trò không hợp lệ." });
            }

            int actorId = User.GetUserId() ?? 0;
            var result = await _delegationService.AssignRolesAsync(actorId, req.UserId, req.RoleIds ?? new List<int>());
            if (!result.Succeeded)
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
            }
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

        public static string GetModuleDisplayName(string moduleCode) => moduleCode.ToUpperInvariant() switch
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
