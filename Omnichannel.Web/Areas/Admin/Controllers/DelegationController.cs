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
    public class DelegationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDelegationService _delegationService;
        private readonly IPermissionService _permissionService;
        private readonly IPasswordHasherService _passwordHasher;
        private readonly IAuditLogService _auditLogService;

        public DelegationController(
            ApplicationDbContext context,
            IDelegationService delegationService,
            IPermissionService permissionService,
            IPasswordHasherService passwordHasher,
            IAuditLogService auditLogService)
        {
            _context = context;
            _delegationService = delegationService;
            _permissionService = permissionService;
            _passwordHasher = passwordHasher;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int actorId = User.GetUserId() ?? 0;
            var actorCtx = await _delegationService.GetContextAsync(actorId);

            if (!actorCtx.IsSuperAdmin && !actorCtx.CanDelegateFurther)
            {
                return RedirectToAction("AccessDenied", "Auth", new
                {
                    area = "Admin",
                    attemptedPath = "/Admin/Delegation",
                    message = "Chỉ Admin Tổng (SuperAdmin) hoặc Quản trị viên có quyền Ủy quyền tiếp (CanDelegateFurther) mới được truy cập trung tâm này."
                });
            }

            var grantableSet = actorCtx.GrantablePermissionIds.ToHashSet();

            var departments = await _context.Departments
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(d => d.IsActive)
                .OrderBy(d => d.SortOrder)
                .ThenBy(d => d.Id)
                .Select(d => new DepartmentTreeItemDto
                {
                    Id = d.Id,
                    Code = d.Code,
                    Name = d.Name,
                    ParentId = d.ParentId,
                    SortOrder = d.SortOrder,
                    IsActive = d.IsActive
                })
                .ToListAsync();

            var deptNameById = departments.ToDictionary(d => d.Id, d => d.Name);

            var stores = await _context.Stores
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.Id)
                .Select(s => new StoreOptionDto
                {
                    Id = s.Id,
                    Code = s.Code,
                    Name = s.Name,
                    IsHeadquarters = s.IsHeadquarters
                })
                .ToListAsync();

            var storeNameById = stores.ToDictionary(s => s.Id, s => s.Name);

            var allPermissions = await _context.Permissions
                .AsNoTracking()
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Code)
                .ToListAsync();

            var permCodeById = allPermissions.ToDictionary(p => p.Id, p => p.Code);

            var moduleGroups = allPermissions
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
                        Description = p.Description,
                        Action = p.Code,
                        IsGrantableByActor = actorCtx.IsSuperAdmin || grantableSet.Contains(p.Id)
                    }).ToList()
                })
                .ToList();

            var scopes = await _context.AdminDelegationScopes
                .AsNoTracking()
                .Include(s => s.AllowedPermissions)
                .Include(s => s.AllowedDepartments)
                .Include(s => s.AllowedStores)
                .ToListAsync();

            var scopeByAdminId = scopes.ToDictionary(s => s.AdminUserId);

            var allStaff = await _context.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(u => u.Department)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => u.UserType >= 2 && !u.IsDeleted)
                .OrderBy(u => u.Id)
                .ToListAsync();

            // Danh sách Admin nhỏ: có AdminDelegationScope hoặc có Role Level từ 30 đến 90
            var subAdminUsers = allStaff
                .Where(u =>
                    scopeByAdminId.ContainsKey(u.Id) ||
                    (u.UserRoles.Any(ur => ur.Role != null && ur.Role.IsActive && ur.Role.Level >= 30 && ur.Role.Level < 100) &&
                     !u.UserRoles.Any(ur => ur.Role != null && ur.Role.Level >= 100)))
                .OrderByDescending(u => scopeByAdminId.ContainsKey(u.Id))
                .ThenBy(u => u.Username)
                .ToList();

            var subAdminCards = new List<SubAdminCardDto>();
            foreach (var u in subAdminUsers)
            {
                scopeByAdminId.TryGetValue(u.Id, out var sc);
                var allowedDeptIds = sc != null
                    ? sc.AllowedDepartments.Select(ad => ad.DepartmentId).Distinct().ToList()
                    : (u.DepartmentId.HasValue ? new List<int> { u.DepartmentId.Value } : new List<int>());

                var allowedStoreIds = sc != null
                    ? sc.AllowedStores.Select(ast => ast.StoreId).Distinct().ToList()
                    : (u.StoreId.HasValue ? new List<int> { u.StoreId.Value } : new List<int>());

                var allowedPermIds = sc != null
                    ? sc.AllowedPermissions.Select(ap => ap.PermissionId).Distinct().ToList()
                    : new List<int>();

                int maxLvl = sc?.MaxGrantLevel ?? 30;
                var deptSet = allowedDeptIds.ToHashSet();

                int managedCount = allStaff.Count(target =>
                    target.Id != u.Id &&
                    ((target.DepartmentId.HasValue && deptSet.Contains(target.DepartmentId.Value)) || target.ManagerId == u.Id) &&
                    !target.UserRoles.Any(ur => ur.Role != null && ur.Role.Level >= 50));

                subAdminCards.Add(new SubAdminCardDto
                {
                    UserId = u.Id,
                    EmployeeCode = u.EmployeeCode,
                    Username = u.Username,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    JobTitle = u.JobTitle,
                    AvatarUrl = u.AvatarUrl,
                    IsActive = u.IsActive,
                    PrimaryDepartmentId = u.DepartmentId,
                    PrimaryDepartmentName = u.Department?.Name ?? "Chưa phân phòng",
                    Roles = u.UserRoles
                        .Where(ur => ur.Role != null && ur.Role.IsActive && ur.Role.Name != "Customer")
                        .OrderByDescending(ur => ur.Role.Level)
                        .Select(ur => new RoleDto
                        {
                            Id = ur.Role.Id,
                            Name = ur.Role.Name,
                            DisplayName = ur.Role.DisplayName ?? ur.Role.Name,
                            Level = ur.Role.Level,
                            Color = ur.Role.Color
                        })
                        .ToList(),
                    HasScope = sc != null,
                    CanCreateUser = sc?.CanCreateUser ?? false,
                    CanEditUser = sc?.CanEditUser ?? false,
                    CanDeleteUser = sc?.CanDeleteUser ?? false,
                    CanResetPassword = sc?.CanResetPassword ?? false,
                    CanAssignRole = sc?.CanAssignRole ?? false,
                    CanDelegateFurther = sc?.CanDelegateFurther ?? false,
                    MaxGrantLevel = maxLvl,
                    AllowedPermissionIds = allowedPermIds,
                    AllowedPermissionCodes = allowedPermIds
                        .Where(id => permCodeById.ContainsKey(id))
                        .Select(id => permCodeById[id])
                        .ToList(),
                    AllowedDepartmentIds = allowedDeptIds,
                    AllowedDepartmentNames = allowedDeptIds
                        .Where(id => deptNameById.ContainsKey(id))
                        .Select(id => deptNameById[id])
                        .ToList(),
                    AllowedStoreIds = allowedStoreIds,
                    AllowedStoreNames = allowedStoreIds
                        .Where(id => storeNameById.ContainsKey(id))
                        .Select(id => storeNameById[id])
                        .ToList(),
                    ManagedStaffCount = managedCount,
                    ScopeUpdatedAt = sc?.UpdatedAt ?? sc?.CreatedAt
                });
            }

            var subAdminRoles = await _context.Roles
                .AsNoTracking()
                .Where(r => r.IsActive && r.Level >= 30 && r.Level < 100)
                .OrderByDescending(r => r.Level)
                .ThenBy(r => r.Id)
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    DisplayName = r.DisplayName ?? r.Name,
                    Level = r.Level,
                    DepartmentId = r.DepartmentId,
                    Color = r.Color
                })
                .ToListAsync();

            var eligibleForPromotion = allStaff
                .Where(u => !u.UserRoles.Any(ur => ur.Role != null && ur.Role.Level >= 100))
                .Select(u => new UserSummaryDto
                {
                    Id = u.Id,
                    EmployeeCode = u.EmployeeCode,
                    Username = u.Username,
                    FullName = u.FullName,
                    Email = u.Email,
                    JobTitle = u.JobTitle,
                    DepartmentId = u.DepartmentId,
                    DepartmentName = u.Department?.Name
                })
                .ToList();

            var vm = new DelegationHubViewModel
            {
                SubAdmins = subAdminCards,
                ModuleGroups = moduleGroups,
                Departments = departments,
                Stores = stores,
                SubAdminRoles = subAdminRoles,
                EligibleStaffForPromotion = eligibleForPromotion,
                ActorContext = actorCtx
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PreviewScopeImpact([FromBody] SaveDelegationScopeRequest req)
        {
            int actorId = User.GetUserId() ?? 0;
            var actorCtx = await _delegationService.GetContextAsync(actorId);
            if (!actorCtx.IsSuperAdmin && !actorCtx.CanDelegateFurther)
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
                return Json(new { success = false, message = "Bạn không có quyền xem trước hoặc thay đổi phạm vi ủy quyền." });
            }

            if (req == null || req.AdminUserId <= 0)
            {
                return Json(new { success = false, message = "Tham số Admin nhỏ không hợp lệ." });
            }

            var existingScope = await _context.AdminDelegationScopes
                .AsNoTracking()
                .Include(s => s.AllowedPermissions)
                .FirstOrDefaultAsync(s => s.AdminUserId == req.AdminUserId);

            if (existingScope == null)
            {
                return Json(new
                {
                    success = true,
                    hasImpact = false,
                    affectedPermissionCount = 0,
                    affectedUserCount = 0,
                    warningMessage = string.Empty
                });
            }

            var newPermSet = (req.PermissionIds ?? new List<int>()).ToHashSet();
            var removedPermIds = existingScope.AllowedPermissions
                .Select(ap => ap.PermissionId)
                .Where(pid => !newPermSet.Contains(pid))
                .Distinct()
                .ToList();

            if (removedPermIds.Count == 0)
            {
                return Json(new
                {
                    success = true,
                    hasImpact = false,
                    affectedPermissionCount = 0,
                    affectedUserCount = 0,
                    warningMessage = string.Empty
                });
            }

            // Kiểm tra các quyền cá nhân mà Admin nhỏ này đã cấp cho cấp dưới nhưng nay bị rút khỏi Scope
            var affectedOverrides = await _context.UserPermissions
                .AsNoTracking()
                .Where(up => up.GrantedByUserId == req.AdminUserId && up.IsGranted && removedPermIds.Contains(up.PermissionId))
                .Select(up => new { up.UserId, up.PermissionId })
                .ToListAsync();

            // Kiểm tra các vai trò mà Admin nhỏ này đã gán cho cấp dưới nhưng vai trò đó chứa quyền bị rút
            var affectedRoleAssignments = await _context.UserRoles
                .AsNoTracking()
                .Include(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                .Where(ur => ur.AssignedByUserId == req.AdminUserId)
                .ToListAsync();

            var affectedUserIds = affectedOverrides.Select(x => x.UserId).ToHashSet();
            var affectedPermIds = affectedOverrides.Select(x => x.PermissionId).ToHashSet();

            foreach (var ur in affectedRoleAssignments)
            {
                var roleRemovedPerms = ur.Role.RolePermissions
                    .Select(rp => rp.PermissionId)
                    .Where(pid => removedPermIds.Contains(pid))
                    .ToList();

                if (roleRemovedPerms.Count > 0)
                {
                    affectedUserIds.Add(ur.UserId);
                    foreach (var p in roleRemovedPerms)
                    {
                        affectedPermIds.Add(p);
                    }
                }
            }

            int xPerms = removedPermIds.Count;
            int yUsers = affectedUserIds.Count;
            bool hasImpact = xPerms > 0;

            string warning = yUsers > 0
                ? $"Bạn đang thu hẹp {xPerms} quyền khỏi Scope. Hệ thống sẽ tự động thu hồi {affectedPermIds.Count} quyền đã cấp cho {yUsers} nhân viên cấp dưới!"
                : $"Bạn đang gỡ {xPerms} quyền khỏi phạm vi ủy quyền của Admin này (hiện chưa có nhân viên cấp dưới nào bị thu hồi quyền dây chuyền).";

            return Json(new
            {
                success = true,
                hasImpact,
                removedScopePermCount = xPerms,
                affectedPermissionCount = affectedPermIds.Count > 0 ? affectedPermIds.Count : xPerms,
                affectedUserCount = yUsers,
                warningMessage = warning
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveScope([FromBody] SaveDelegationScopeRequest req)
        {
            int actorId = User.GetUserId() ?? 0;
            var result = await _delegationService.SaveDelegationScopeAsync(actorId, req);

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
        public async Task<IActionResult> CreateSubAdmin([FromBody] CreateSubAdminRequest req)
        {
            int actorId = User.GetUserId() ?? 0;
            var actorCtx = await _delegationService.GetContextAsync(actorId);
            if (!actorCtx.IsSuperAdmin && !actorCtx.CanDelegateFurther)
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
                return Json(new { success = false, message = "Chỉ Admin Tổng (SuperAdmin) mới có quyền bổ nhiệm Admin nhỏ mới." });
            }

            if (req == null || req.RoleId <= 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn vai trò cấp Quản trị phòng ban (Level 30 - 50)." });
            }

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == req.RoleId && r.IsActive);
            if (role == null || role.Level >= 100 || role.Level <= 0)
            {
                return Json(new { success = false, message = "Vai trò được chọn không hợp lệ cho Admin nhỏ." });
            }

            User? targetUser = null;

            if (string.Equals(req.Mode, "new", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.FullName) || string.IsNullOrWhiteSpace(req.Email))
                {
                    return Json(new { success = false, message = "Vui lòng nhập đầy đủ Username, Họ tên và Email cho Admin nhỏ mới." });
                }

                string cleanUser = req.Username.Trim();
                string cleanEmail = req.Email.Trim().ToLowerInvariant();

                if (await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Username.ToUpper() == cleanUser.ToUpper()))
                {
                    return Json(new { success = false, message = $"Tên đăng nhập '{cleanUser}' đã tồn tại." });
                }

                if (await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email.ToUpper() == cleanEmail.ToUpper()))
                {
                    return Json(new { success = false, message = $"Email '{cleanEmail}' đã tồn tại." });
                }

                var existingCodes = await _context.Users
                    .IgnoreQueryFilters()
                    .Where(u => u.EmployeeCode != null)
                    .Select(u => u.EmployeeCode!)
                    .ToListAsync();
                var codeSet = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);
                int seq = 1;
                while (codeSet.Contains($"NV{seq:D4}")) seq++;

                string rawPassword = string.IsNullOrWhiteSpace(req.Password) ? "Admin@123" : req.Password.Trim();

                targetUser = new User
                {
                    EmployeeCode = $"NV{seq:D4}",
                    Username = cleanUser,
                    NormalizedUsername = cleanUser.ToUpperInvariant(),
                    FullName = req.FullName.Trim(),
                    Email = cleanEmail,
                    PhoneNumber = req.PhoneNumber?.Trim(),
                    PasswordHash = _passwordHasher.HashPassword(rawPassword),
                    UserType = 2,
                    DepartmentId = req.PrimaryDepartmentId ?? role.DepartmentId,
                    ManagerId = actorId,
                    JobTitle = !string.IsNullOrWhiteSpace(req.JobTitle) ? req.JobTitle.Trim() : (role.DisplayName ?? role.Name),
                    HireDate = DateTime.UtcNow.Date,
                    IsActive = true,
                    IsDeleted = false,
                    SecurityStamp = Guid.NewGuid().ToString("N"),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = actorId
                };

                await _context.Users.AddAsync(targetUser);
                await _context.SaveChangesAsync();
            }
            else
            {
                if (!req.ExistingUserId.HasValue || req.ExistingUserId.Value <= 0)
                {
                    return Json(new { success = false, message = "Vui lòng chọn nhân sự hiện có để bổ nhiệm làm Admin nhỏ." });
                }

                targetUser = await _context.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id == req.ExistingUserId.Value && u.UserType >= 2 && !u.IsDeleted);

                if (targetUser == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy hồ sơ nhân sự được chọn." });
                }

                if (req.PrimaryDepartmentId.HasValue)
                {
                    targetUser.DepartmentId = req.PrimaryDepartmentId.Value;
                }
                if (!string.IsNullOrWhiteSpace(req.JobTitle))
                {
                    targetUser.JobTitle = req.JobTitle.Trim();
                }
                targetUser.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // Đảm bảo gán Role cấp 50 cho Admin nhỏ
            bool hasRole = await _context.UserRoles.AnyAsync(ur => ur.UserId == targetUser.Id && ur.RoleId == role.Id);
            if (!hasRole)
            {
                await _context.UserRoles.AddAsync(new UserRole
                {
                    UserId = targetUser.Id,
                    RoleId = role.Id,
                    AssignedByUserId = actorId,
                    AssignedAt = DateTime.UtcNow
                });
                targetUser.SecurityStamp = Guid.NewGuid().ToString("N");
                await _context.SaveChangesAsync();
                await _permissionService.InvalidateUserPermissionCacheAsync(targetUser.Id);
            }

            // Lưu luôn Scope ủy quyền
            var scopeReq = req.Scope ?? new SaveDelegationScopeRequest();
            scopeReq.AdminUserId = targetUser.Id;
            if (scopeReq.MaxGrantLevel <= 0) scopeReq.MaxGrantLevel = role.Level;

            var scopeResult = await _delegationService.SaveDelegationScopeAsync(actorId, scopeReq);
            if (!scopeResult.Succeeded)
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
                return Json(new
                {
                    success = false,
                    message = scopeResult.Message,
                    errorCode = scopeResult.ErrorCode
                });
            }

            return Json(new
            {
                success = true,
                adminUserId = targetUser.Id,
                message = $"Đã bổ nhiệm '{targetUser.FullName} (@{targetUser.Username})' giữ vai trò '{role.DisplayName ?? role.Name}' và kích hoạt phạm vi ủy quyền thành công!"
            });
        }

        [HttpGet]
        [HasPermission("PERMISSION_DELEGATE")]
        public async Task<IActionResult> AuditLog(
            int? actorId,
            string? targetKeyword,
            string? actionType,
            DateTime? fromDate,
            DateTime? toDate,
            int page = 1)
        {
            int currentActorId = User.GetUserId() ?? 0;
            var actorCtx = await _delegationService.GetContextAsync(currentActorId);

            var scopedUsersQuery = _delegationService.ApplyUserScope(
                _context.Users.IgnoreQueryFilters().AsNoTracking().Where(u => u.UserType >= 2),
                actorCtx);

            var scopedUsers = await scopedUsersQuery
                .OrderBy(u => u.FullName)
                .Select(u => new UserSummaryDto
                {
                    Id = u.Id,
                    EmployeeCode = u.EmployeeCode,
                    Username = u.Username,
                    FullName = u.FullName,
                    Email = u.Email,
                    DepartmentName = u.Department != null ? u.Department.Name : null
                })
                .ToListAsync();

            var allowedUserIds = scopedUsers.Select(u => u.Id).Append(currentActorId).ToHashSet();
            var allowedUserIdStrings = allowedUserIds.Select(id => id.ToString()).ToHashSet();

            var securityPrefixes = new[] { "DELEGATION_", "PERMISSION_", "USER_", "ROLE_", "DEPARTMENT_" };
            var securityEntityTypes = new[] { "AdminDelegationScope", "UserPermission", "UserRole", "Role", "User", "Department" };

            var query = _context.AuditLogs
                .AsNoTracking()
                .Where(a =>
                    securityEntityTypes.Contains(a.EntityType) ||
                    a.Action.StartsWith("DELEGATION_") ||
                    a.Action.StartsWith("PERMISSION_") ||
                    a.Action.StartsWith("USER_") ||
                    a.Action.StartsWith("ROLE_") ||
                    a.Action.StartsWith("DEPARTMENT_"));

            // Cô lập phạm vi cho Admin nhỏ: chỉ thấy log do mình thao tác hoặc tác động lên nhân sự trong Scope của mình
            if (!actorCtx.IsSuperAdmin)
            {
                query = query.Where(a =>
                    (a.UserId.HasValue && allowedUserIds.Contains(a.UserId.Value)) ||
                    allowedUserIdStrings.Contains(a.EntityId));
            }

            if (actorId.HasValue && actorId.Value > 0)
            {
                query = query.Where(a => a.UserId == actorId.Value);
            }

            if (!string.IsNullOrWhiteSpace(actionType))
            {
                string act = actionType.Trim().ToUpperInvariant();
                query = query.Where(a => a.Action.ToUpper().Contains(act));
            }

            if (!string.IsNullOrWhiteSpace(targetKeyword))
            {
                string kw = targetKeyword.Trim();
                query = query.Where(a =>
                    a.EntityId.Contains(kw) ||
                    (a.UserName != null && a.UserName.Contains(kw)) ||
                    (a.NewValues != null && a.NewValues.Contains(kw)) ||
                    (a.OldValues != null && a.OldValues.Contains(kw)));
            }

            if (fromDate.HasValue)
            {
                var start = fromDate.Value.Date;
                query = query.Where(a => a.CreatedAt >= start);
            }

            if (toDate.HasValue)
            {
                var end = toDate.Value.Date.AddDays(1);
                query = query.Where(a => a.CreatedAt < end);
            }

            int pageSize = 25;
            if (page < 1) page = 1;
            int totalCount = await query.CountAsync();
            int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));

            var logs = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    UserName = a.UserName ?? "Hệ thống",
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

            var vm = new DelegationAuditLogViewModel
            {
                Logs = logs,
                ScopedActors = scopedUsers,
                FilterActorId = actorId,
                FilterTargetKeyword = targetKeyword,
                FilterActionType = actionType,
                FilterFromDate = fromDate,
                FilterToDate = toDate,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                IsSuperAdmin = actorCtx.IsSuperAdmin
            };

            return View(vm);
        }

        private static string GetModuleDisplayName(string moduleCode) => moduleCode.ToUpperInvariant() switch
        {
            "DASHBOARD" => "Tổng Quan Hệ Thống (Dashboard)",
            "CATALOG" or "PRODUCT" => "Danh Mục & Sản Phẩm (Product)",
            "INVENTORY" => "Quản Lý Kho & Lô Hàng FEFO (Inventory)",
            "POS" => "Bán Lẻ Điểm Bán (POS)",
            "ORDER" => "Quản Lý Đơn Hàng Đa Kênh (Order)",
            "CUSTOMER" or "CRM" => "Khách Hàng & Hồ Sơ CRM (Customer)",
            "CMS" => "Biên Tập Nội Dung & Giao Diện (CMS)",
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
    }
}

