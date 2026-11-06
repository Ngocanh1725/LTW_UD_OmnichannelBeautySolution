using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.Common;
using Omnichannel.Application.DTOs.Security;
using Omnichannel.Application.Interfaces.Security;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Security
{
    public class DelegationService : IDelegationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPermissionService _permissionService;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public DelegationService(
            ApplicationDbContext context,
            IPermissionService permissionService,
            IAuditLogService auditLogService,
            IHttpContextAccessor? httpContextAccessor = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> IsSuperAdminAsync(int actorId)
        {
            if (actorId <= 0) return false;

            var user = await _context.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == actorId);

            if (user == null || !user.IsActive || user.IsDeleted || user.UserType < 2)
            {
                return false;
            }

            return user.UserRoles.Any(ur =>
                ur.Role != null &&
                ur.Role.IsActive &&
                (string.Equals(ur.Role.Name, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(ur.Role.Name, "SUPER_ADMIN", StringComparison.OrdinalIgnoreCase) ||
                 ur.Role.Level >= 100));
        }

        public async Task<DelegationContextDto> GetContextAsync(int actorId)
        {
            var result = new DelegationContextDto
            {
                ActorId = actorId
            };

            if (actorId <= 0)
            {
                return result;
            }

            var actor = await _context.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.DelegationScope)
                    .ThenInclude(s => s!.AllowedPermissions)
                .Include(u => u.DelegationScope)
                    .ThenInclude(s => s!.AllowedDepartments)
                .Include(u => u.DelegationScope)
                    .ThenInclude(s => s!.AllowedStores)
                .FirstOrDefaultAsync(u => u.Id == actorId);

            if (actor == null || !actor.IsActive || actor.IsDeleted || actor.UserType < 2)
            {
                return result;
            }

            result.ActorUsername = actor.Username;
            result.EffectivePermissionCodes = await _permissionService.GetEffectivePermissionsAsync(actorId);

            var activeStaffRoles = actor.UserRoles
                .Where(ur => ur.Role != null &&
                             ur.Role.IsActive &&
                             !string.Equals(ur.Role.Name, "Customer", StringComparison.OrdinalIgnoreCase))
                .Select(ur => ur.Role)
                .ToList();

            bool isSuperAdmin = activeStaffRoles.Any(r =>
                string.Equals(r.Name, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(r.Name, "SUPER_ADMIN", StringComparison.OrdinalIgnoreCase) ||
                r.Level >= 100);

            result.IsSuperAdmin = isSuperAdmin;

            if (isSuperAdmin)
            {
                result.HighestRoleLevel = 100;
                result.MaxGrantLevel = 100;
                result.CanCreateUser = true;
                result.CanEditUser = true;
                result.CanDeleteUser = true;
                result.CanResetPassword = true;
                result.CanAssignRole = true;
                result.CanDelegateFurther = true;

                result.GrantablePermissionIds = await _context.Permissions
                    .AsNoTracking()
                    .OrderBy(p => p.Id)
                    .Select(p => p.Id)
                    .ToListAsync();

                result.ManagedDepartmentIds = await _context.Departments
                    .AsNoTracking()
                    .OrderBy(d => d.Id)
                    .Select(d => d.Id)
                    .ToListAsync();

                result.ManagedStoreIds = await _context.Stores
                    .AsNoTracking()
                    .OrderBy(s => s.Id)
                    .Select(s => s.Id)
                    .ToListAsync();

                return result;
            }

            result.HighestRoleLevel = activeStaffRoles
                .Select(r => r.Level)
                .DefaultIfEmpty(0)
                .Max();

            var scope = actor.DelegationScope;
            if (scope == null)
            {
                // Nếu nhân sự chưa được cấp AdminDelegationScope nhưng là Trưởng phòng (HeadUserId)
                var headDeptIds = await _context.Departments
                    .AsNoTracking()
                    .Where(d => d.HeadUserId == actorId)
                    .Select(d => d.Id)
                    .ToListAsync();

                if (actor.DepartmentId.HasValue && !headDeptIds.Contains(actor.DepartmentId.Value))
                {
                    headDeptIds.Add(actor.DepartmentId.Value);
                }

                result.ManagedDepartmentIds = headDeptIds;
                if (actor.StoreId.HasValue)
                {
                    result.ManagedStoreIds = new List<int> { actor.StoreId.Value };
                }
                return result;
            }

            result.CanCreateUser = scope.CanCreateUser;
            result.CanEditUser = scope.CanEditUser;
            result.CanDeleteUser = scope.CanDeleteUser;
            result.CanResetPassword = scope.CanResetPassword;
            result.CanAssignRole = scope.CanAssignRole;
            result.CanDelegateFurther = scope.CanDelegateFurther;
            result.MaxGrantLevel = Math.Min(scope.MaxGrantLevel, result.HighestRoleLevel);

            // Quyền được phép cấp = (Quyền hiệu lực của actor) ∩ (AdminDelegationPermission)
            var allPermissions = await _context.Permissions
                .AsNoTracking()
                .Select(p => new { p.Id, p.Code })
                .ToListAsync();

            var effectivePermissionIds = allPermissions
                .Where(p => result.EffectivePermissionCodes.Contains(p.Code) ||
                            result.EffectivePermissionCodes.Contains(p.Code.Replace("_", ":")))
                .Select(p => p.Id)
                .ToHashSet();

            var scopePermissionIds = scope.AllowedPermissions
                .Select(ap => ap.PermissionId)
                .ToHashSet();

            result.GrantablePermissionIds = effectivePermissionIds
                .Intersect(scopePermissionIds)
                .OrderBy(id => id)
                .ToList();

            // Tập phòng ban được quản lý
            var managedDeptSet = scope.AllowedDepartments
                .Select(ad => ad.DepartmentId)
                .ToHashSet();

            if (managedDeptSet.Count == 0 && actor.DepartmentId.HasValue)
            {
                managedDeptSet.Add(actor.DepartmentId.Value);
            }

            // Mở rộng thêm các phòng ban con trực thuộc các phòng ban được giao (trừ gốc BAN_GIAM_DOC = 1)
            if (managedDeptSet.Count > 0)
            {
                var nonRootParents = managedDeptSet.Where(id => id != 1).ToList();
                if (nonRootParents.Count > 0)
                {
                    var childDeptIds = await _context.Departments
                        .AsNoTracking()
                        .Where(d => d.ParentId.HasValue && nonRootParents.Contains(d.ParentId.Value))
                        .Select(d => d.Id)
                        .ToListAsync();

                    foreach (var cid in childDeptIds)
                    {
                        managedDeptSet.Add(cid);
                    }
                }
            }

            result.ManagedDepartmentIds = managedDeptSet.OrderBy(id => id).ToList();

            // Tập chi nhánh được quản lý
            var managedStoreSet = scope.AllowedStores
                .Select(s => s.StoreId)
                .ToHashSet();

            if (managedStoreSet.Count == 0 && actor.StoreId.HasValue)
            {
                managedStoreSet.Add(actor.StoreId.Value);
            }

            result.ManagedStoreIds = managedStoreSet.OrderBy(id => id).ToList();

            return result;
        }

        public async Task<IReadOnlyList<int>> GetGrantablePermissionIdsAsync(int actorId)
        {
            var ctx = await GetContextAsync(actorId);
            return ctx.GrantablePermissionIds;
        }

        public async Task<IReadOnlyList<int>> GetAssignableRoleIdsAsync(int actorId)
        {
            var ctx = await GetContextAsync(actorId);

            // Role Customer tuyệt đối không bao giờ nằm trong danh sách Role gán cho nhân sự
            var activeStaffRoles = await _context.Roles
                .AsNoTracking()
                .Include(r => r.RolePermissions)
                .Where(r => r.IsActive &&
                            r.Level > 0 &&
                            r.Name != "Customer" &&
                            r.Name != "CUSTOMER")
                .OrderByDescending(r => r.Level)
                .ThenBy(r => r.Id)
                .ToListAsync();

            if (ctx.IsSuperAdmin)
            {
                return activeStaffRoles.Select(r => r.Id).ToList();
            }

            if (!ctx.CanAssignRole || ctx.MaxGrantLevel <= 0 || ctx.HighestRoleLevel <= 0)
            {
                return Array.Empty<int>();
            }

            var grantablePermSet = ctx.GrantablePermissionIds.ToHashSet();
            var managedDeptSet = ctx.ManagedDepartmentIds.ToHashSet();

            var assignableIds = new List<int>();
            foreach (var role in activeStaffRoles)
            {
                if (string.Equals(role.Name, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(role.Name, "SUPER_ADMIN", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Role phải có Level < MaxGrantLevel và Level < Level của Actor
                if (role.Level >= ctx.MaxGrantLevel || role.Level >= ctx.HighestRoleLevel)
                {
                    continue;
                }

                // Nếu Role gắn với 1 phòng ban cụ thể thì phòng ban đó phải nằm trong phạm vi quản lý
                if (role.DepartmentId.HasValue && managedDeptSet.Count > 0 && !managedDeptSet.Contains(role.DepartmentId.Value))
                {
                    continue;
                }

                // Mọi permission của Role phải là tập con của GrantablePermissionIds
                bool allPermsInScope = role.RolePermissions.All(rp => grantablePermSet.Contains(rp.PermissionId));
                if (!allPermsInScope)
                {
                    continue;
                }

                assignableIds.Add(role.Id);
            }

            return assignableIds;
        }

        public async Task<bool> CanManageUserAsync(int actorId, int targetUserId)
        {
            var check = await ValidateManageUserInternalAsync(actorId, targetUserId);
            return check.Succeeded;
        }

        private async Task<OperationResult> ValidateManageUserInternalAsync(int actorId, int targetUserId)
        {
            if (actorId <= 0 || targetUserId <= 0)
            {
                return OperationResult.Fail("Thông tin định danh người dùng không hợp lệ.", "INVALID_USER_ID");
            }

            if (actorId == targetUserId)
            {
                return OperationResult.Fail("Bạn không thể tự cấp quyền hoặc chỉnh sửa phân quyền của chính mình.", "SELF_MODIFICATION_FORBIDDEN");
            }

            var ctx = await GetContextAsync(actorId);
            if (ctx.HighestRoleLevel <= 0)
            {
                return OperationResult.Fail("Tài khoản của bạn không có thẩm quyền quản trị.", "ACTOR_UNAUTHORIZED");
            }

            var target = await _context.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == targetUserId);

            if (target == null)
            {
                return OperationResult.Fail("Không tìm thấy tài khoản nhân sự mục tiêu.", "TARGET_NOT_FOUND");
            }

            if (target.UserType < 2)
            {
                return OperationResult.Fail("Không thể thực hiện phân quyền nội bộ trên tài khoản Khách hàng.", "TARGET_IS_CUSTOMER");
            }

            if (ctx.IsSuperAdmin)
            {
                return OperationResult.Ok();
            }

            bool isTargetSuperAdmin = target.UserRoles.Any(ur =>
                ur.Role != null &&
                ur.Role.IsActive &&
                (string.Equals(ur.Role.Name, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(ur.Role.Name, "SUPER_ADMIN", StringComparison.OrdinalIgnoreCase) ||
                 ur.Role.Level >= 100));

            if (isTargetSuperAdmin)
            {
                return OperationResult.Fail("Bạn không có quyền tác động lên tài khoản Quản trị viên tối cao (SuperAdmin).", "TARGET_IS_SUPERADMIN");
            }

            int targetLevel = target.UserRoles
                .Where(ur => ur.Role != null &&
                             ur.Role.IsActive &&
                             !string.Equals(ur.Role.Name, "Customer", StringComparison.OrdinalIgnoreCase))
                .Select(ur => ur.Role.Level)
                .DefaultIfEmpty(0)
                .Max();

            if (targetLevel >= ctx.HighestRoleLevel)
            {
                return OperationResult.Fail("Không thể chỉnh sửa tài khoản có cấp bậc bằng hoặc cao hơn bạn.", "TARGET_LEVEL_TOO_HIGH");
            }

            bool inDepartmentScope =
                (target.DepartmentId.HasValue && ctx.ManagedDepartmentIds.Contains(target.DepartmentId.Value)) ||
                target.ManagerId == actorId;

            if (!inDepartmentScope)
            {
                return OperationResult.Fail("Nhân sự này không thuộc phạm vi phòng ban bạn được giao quản lý.", "OUT_OF_DEPARTMENT_SCOPE");
            }

            if (ctx.ManagedStoreIds.Count > 0 && target.StoreId.HasValue && !ctx.ManagedStoreIds.Contains(target.StoreId.Value))
            {
                return OperationResult.Fail("Nhân sự này không thuộc phạm vi chi nhánh bạn được giao quản lý.", "OUT_OF_STORE_SCOPE");
            }

            return OperationResult.Ok();
        }

        public IQueryable<User> ApplyUserScope(IQueryable<User> query, DelegationContextDto ctx)
        {
            if (ctx == null || ctx.HighestRoleLevel <= 0)
            {
                return query.Where(_ => false);
            }

            // Luôn chỉ lọc nhân sự nội bộ (UserType = 2)
            var staffQuery = query.Where(u => u.UserType == 2);

            if (ctx.IsSuperAdmin)
            {
                return staffQuery;
            }

            var deptIds = ctx.ManagedDepartmentIds.ToList();
            var storeIds = ctx.ManagedStoreIds.ToList();
            int actorId = ctx.ActorId;
            int actorLevel = ctx.HighestRoleLevel;

            // Admin nhỏ không nhìn thấy SuperAdmin trong phạm vi quản lý cấp dưới,
            // chỉ thấy nhân sự thuộc phòng ban được giao (hoặc do mình quản lý trực tiếp) và chính mình
            staffQuery = staffQuery.Where(u =>
                !u.UserRoles.Any(ur =>
                    ur.Role.IsActive &&
                    (ur.Role.Name == "SuperAdmin" || ur.Role.Name == "SUPER_ADMIN" || ur.Role.Level >= 100)));

            if (deptIds.Count > 0)
            {
                staffQuery = staffQuery.Where(u =>
                    u.Id == actorId ||
                    u.ManagerId == actorId ||
                    (u.DepartmentId.HasValue && deptIds.Contains(u.DepartmentId.Value)));
            }
            else
            {
                staffQuery = staffQuery.Where(u => u.Id == actorId || u.ManagerId == actorId);
            }

            if (storeIds.Count > 0)
            {
                staffQuery = staffQuery.Where(u =>
                    u.Id == actorId ||
                    !u.StoreId.HasValue ||
                    storeIds.Contains(u.StoreId.Value));
            }

            return staffQuery;
        }

        public async Task<OperationResult> GrantUserPermissionAsync(
            int actorId,
            int targetUserId,
            int permissionId,
            bool isGranted,
            string? reason = null)
        {
            var manageCheck = await ValidateManageUserInternalAsync(actorId, targetUserId);
            if (!manageCheck.Succeeded)
            {
                return manageCheck;
            }

            var ctx = await GetContextAsync(actorId);
            if (!ctx.IsSuperAdmin && !ctx.GrantablePermissionIds.Contains(permissionId))
            {
                return OperationResult.Fail("Bạn không thể cấp quyền vượt phạm vi được giao.", "PERMISSION_OUT_OF_SCOPE");
            }

            var perm = await _context.Permissions.AsNoTracking().FirstOrDefaultAsync(p => p.Id == permissionId);
            if (perm == null)
            {
                return OperationResult.Fail("Mã quyền không tồn tại trong hệ thống.", "PERMISSION_NOT_FOUND");
            }

            return await ExecuteInTransactionAsync(async () =>
            {
                var targetUser = await _context.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id == targetUserId);

                if (targetUser == null)
                {
                    return OperationResult.Fail("Không tìm thấy tài khoản nhân sự mục tiêu.", "TARGET_NOT_FOUND");
                }

                var existingOverride = await _context.UserPermissions
                    .FirstOrDefaultAsync(up => up.UserId == targetUserId && up.PermissionId == permissionId);

                var oldJson = existingOverride != null
                    ? JsonSerializer.Serialize(new
                    {
                        existingOverride.UserId,
                        existingOverride.PermissionId,
                        PermissionCode = perm.Code,
                        existingOverride.IsGranted,
                        existingOverride.GrantedByUserId,
                        existingOverride.Reason
                    })
                    : null;

                if (existingOverride != null)
                {
                    existingOverride.IsGranted = isGranted;
                    existingOverride.GrantedByUserId = actorId;
                    existingOverride.GrantedAt = DateTime.UtcNow;
                    existingOverride.Reason = reason;
                }
                else
                {
                    await _context.UserPermissions.AddAsync(new UserPermission
                    {
                        UserId = targetUserId,
                        PermissionId = permissionId,
                        IsGranted = isGranted,
                        GrantedByUserId = actorId,
                        GrantedAt = DateTime.UtcNow,
                        Reason = reason
                    });
                }

                targetUser.SecurityStamp = Guid.NewGuid().ToString("N");
                targetUser.UpdatedAt = DateTime.UtcNow;
                targetUser.UpdatedBy = actorId;

                await _context.SaveChangesAsync();
                await _permissionService.InvalidateUserPermissionCacheAsync(targetUserId);

                var newJson = JsonSerializer.Serialize(new
                {
                    ActorId = actorId,
                    TargetUserId = targetUserId,
                    PermissionId = permissionId,
                    PermissionCode = perm.Code,
                    IsGranted = isGranted,
                    Reason = reason
                });

                await WriteAuditAsync(
                    action: isGranted ? "GRANT_PERMISSION" : "REVOKE_PERMISSION",
                    entityType: "UserPermission",
                    entityId: $"User:{targetUserId}-Perm:{permissionId}",
                    oldValues: oldJson,
                    newValues: newJson,
                    actorId: actorId,
                    actorUsername: ctx.ActorUsername);

                return OperationResult.Ok(isGranted
                    ? $"Đã cấp quyền '{perm.Name}' ({perm.Code}) cho nhân sự {targetUser.FullName}."
                    : $"Đã thu hồi/chặn quyền '{perm.Name}' ({perm.Code}) đối với nhân sự {targetUser.FullName}.");
            });
        }

        public async Task<OperationResult> RemoveUserPermissionOverrideAsync(
            int actorId,
            int targetUserId,
            int permissionId)
        {
            var manageCheck = await ValidateManageUserInternalAsync(actorId, targetUserId);
            if (!manageCheck.Succeeded)
            {
                return manageCheck;
            }

            var ctx = await GetContextAsync(actorId);
            if (!ctx.IsSuperAdmin && !ctx.GrantablePermissionIds.Contains(permissionId))
            {
                return OperationResult.Fail("Bạn không thể điều chỉnh quyền vượt phạm vi được giao.", "PERMISSION_OUT_OF_SCOPE");
            }

            return await ExecuteInTransactionAsync(async () =>
            {
                var targetUser = await _context.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id == targetUserId);

                if (targetUser == null)
                {
                    return OperationResult.Fail("Không tìm thấy tài khoản nhân sự mục tiêu.", "TARGET_NOT_FOUND");
                }

                var existingOverride = await _context.UserPermissions
                    .Include(up => up.Permission)
                    .FirstOrDefaultAsync(up => up.UserId == targetUserId && up.PermissionId == permissionId);

                if (existingOverride == null)
                {
                    return OperationResult.Ok("Quyền này đang ở trạng thái mặc định theo Vai trò.");
                }

                var oldJson = JsonSerializer.Serialize(new
                {
                    ActorId = actorId,
                    TargetUserId = targetUserId,
                    PermissionId = permissionId,
                    PermissionCode = existingOverride.Permission?.Code,
                    existingOverride.IsGranted,
                    existingOverride.GrantedByUserId,
                    existingOverride.Reason
                });

                _context.UserPermissions.Remove(existingOverride);

                targetUser.SecurityStamp = Guid.NewGuid().ToString("N");
                targetUser.UpdatedAt = DateTime.UtcNow;
                targetUser.UpdatedBy = actorId;

                await _context.SaveChangesAsync();
                await _permissionService.InvalidateUserPermissionCacheAsync(targetUserId);

                var newJson = JsonSerializer.Serialize(new
                {
                    ActorId = actorId,
                    TargetUserId = targetUserId,
                    PermissionId = permissionId,
                    OverrideResetToRoleDefault = true
                });

                await WriteAuditAsync(
                    action: "REVOKE_PERMISSION",
                    entityType: "UserPermission",
                    entityId: $"User:{targetUserId}-Perm:{permissionId}",
                    oldValues: oldJson,
                    newValues: newJson,
                    actorId: actorId,
                    actorUsername: ctx.ActorUsername);

                return OperationResult.Ok("Đã xóa đặc cách quyền cá nhân và khôi phục quyền mặc định theo Vai trò.");
            });
        }

        public async Task<OperationResult> AssignRolesAsync(
            int actorId,
            int targetUserId,
            IEnumerable<int> roleIds)
        {
            var manageCheck = await ValidateManageUserInternalAsync(actorId, targetUserId);
            if (!manageCheck.Succeeded)
            {
                return manageCheck;
            }

            var ctx = await GetContextAsync(actorId);
            if (!ctx.IsSuperAdmin && !ctx.CanAssignRole)
            {
                return OperationResult.Fail("Bạn chưa được cấp quyền gán vai trò cho nhân sự.", "ROLE_ASSIGN_FORBIDDEN");
            }

            var requestedRoleIds = (roleIds ?? Enumerable.Empty<int>()).Distinct().ToList();
            if (requestedRoleIds.Count == 0)
            {
                return OperationResult.Fail("Vui lòng chọn ít nhất một vai trò cho nhân sự.", "EMPTY_ROLES");
            }

            var requestedRoles = await _context.Roles
                .AsNoTracking()
                .Include(r => r.RolePermissions)
                .Where(r => requestedRoleIds.Contains(r.Id) && r.IsActive)
                .ToListAsync();

            if (requestedRoles.Count != requestedRoleIds.Count)
            {
                return OperationResult.Fail("Một hoặc nhiều vai trò được chọn không tồn tại hoặc đã ngừng hoạt động.", "INVALID_ROLE");
            }

            // Chặn tuyệt đối gán Role Customer cho nhân sự nội bộ
            if (requestedRoles.Any(r => r.Level <= 0 || string.Equals(r.Name, "Customer", StringComparison.OrdinalIgnoreCase)))
            {
                return OperationResult.Fail("Không được phép gán vai trò Khách hàng (Customer) cho nhân sự nội bộ.", "CUSTOMER_ROLE_FORBIDDEN_FOR_STAFF");
            }

            if (!ctx.IsSuperAdmin)
            {
                if (requestedRoles.Any(r => r.Level >= ctx.MaxGrantLevel || r.Level >= ctx.HighestRoleLevel))
                {
                    return OperationResult.Fail(
                        $"Bạn không thể gán vai trò có cấp bậc bằng hoặc vượt quá giới hạn cho phép (MaxGrantLevel = {ctx.MaxGrantLevel}).",
                        "ROLE_LEVEL_EXCEEDS_MAX_GRANT_LEVEL");
                }

                var assignableIds = (await GetAssignableRoleIdsAsync(actorId)).ToHashSet();
                if (requestedRoles.Any(r => !assignableIds.Contains(r.Id)))
                {
                    return OperationResult.Fail(
                        "Bạn không thể gán vai trò có chứa các quyền nằm ngoài phạm vi được ủy quyền.",
                        "ROLE_OUT_OF_DELEGATION_SCOPE");
                }
            }

            return await ExecuteInTransactionAsync(async () =>
            {
                var targetUser = await _context.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id == targetUserId);

                if (targetUser == null)
                {
                    return OperationResult.Fail("Không tìm thấy tài khoản nhân sự mục tiêu.", "TARGET_NOT_FOUND");
                }

                var existingUserRoles = await _context.UserRoles
                    .Include(ur => ur.Role)
                    .Where(ur => ur.UserId == targetUserId)
                    .ToListAsync();

                var oldJson = JsonSerializer.Serialize(new
                {
                    TargetUserId = targetUserId,
                    OldRoleIds = existingUserRoles.Select(ur => ur.RoleId).ToList(),
                    OldRoleNames = existingUserRoles.Select(ur => ur.Role?.Name).ToList()
                });

                _context.UserRoles.RemoveRange(existingUserRoles);

                var newUserRoles = requestedRoleIds.Select(rid => new UserRole
                {
                    UserId = targetUserId,
                    RoleId = rid,
                    AssignedByUserId = actorId,
                    AssignedAt = DateTime.UtcNow
                }).ToList();

                await _context.UserRoles.AddRangeAsync(newUserRoles);

                targetUser.SecurityStamp = Guid.NewGuid().ToString("N");
                targetUser.UpdatedAt = DateTime.UtcNow;
                targetUser.UpdatedBy = actorId;

                await _context.SaveChangesAsync();
                await _permissionService.InvalidateUserPermissionCacheAsync(targetUserId);

                var newJson = JsonSerializer.Serialize(new
                {
                    ActorId = actorId,
                    TargetUserId = targetUserId,
                    NewRoleIds = requestedRoleIds,
                    NewRoleNames = requestedRoles.Select(r => r.Name).ToList()
                });

                await WriteAuditAsync(
                    action: "ASSIGN_ROLE",
                    entityType: "UserRole",
                    entityId: targetUserId.ToString(),
                    oldValues: oldJson,
                    newValues: newJson,
                    actorId: actorId,
                    actorUsername: ctx.ActorUsername);

                return OperationResult.Ok($"Đã cập nhật vai trò ({string.Join(", ", requestedRoles.Select(r => r.DisplayName ?? r.Name))}) cho nhân sự {targetUser.FullName}.");
            });
        }

        public async Task<OperationResult> SaveDelegationScopeAsync(
            int actorId,
            SaveDelegationScopeRequest req)
        {
            if (req == null || req.AdminUserId <= 0)
            {
                return OperationResult.Fail("Dữ liệu cấu hình phạm vi ủy quyền không hợp lệ.", "INVALID_REQUEST");
            }

            if (actorId == req.AdminUserId)
            {
                return OperationResult.Fail("Bạn không thể tự cấu hình phạm vi ủy quyền của chính mình.", "SELF_DELEGATION_FORBIDDEN");
            }

            var ctx = await GetContextAsync(actorId);
            if (!ctx.IsSuperAdmin && !ctx.CanDelegateFurther)
            {
                return OperationResult.Fail("Chỉ Quản trị viên tối cao (SuperAdmin) hoặc Quản trị viên có quyền ủy quyền tiếp mới được thiết lập phạm vi ủy quyền.", "DELEGATION_FORBIDDEN");
            }

            var targetCheck = await ValidateManageUserInternalAsync(actorId, req.AdminUserId);
            if (!targetCheck.Succeeded)
            {
                return targetCheck;
            }

            var distinctPermIds = (req.PermissionIds ?? new List<int>()).Distinct().ToList();
            var distinctDeptIds = (req.DepartmentIds ?? new List<int>()).Distinct().ToList();
            var distinctStoreIds = (req.StoreIds ?? new List<int>()).Distinct().ToList();

            if (!ctx.IsSuperAdmin)
            {
                if (req.MaxGrantLevel >= ctx.MaxGrantLevel || req.MaxGrantLevel >= ctx.HighestRoleLevel)
                {
                    return OperationResult.Fail("Trần cấp bậc ủy quyền (MaxGrantLevel) phải nhỏ hơn cấp bậc giới hạn của bạn.", "MAX_GRANT_LEVEL_EXCEEDED");
                }

                var grantableSet = ctx.GrantablePermissionIds.ToHashSet();
                if (distinctPermIds.Any(pid => !grantableSet.Contains(pid)))
                {
                    return OperationResult.Fail("Bạn không thể ủy quyền các mã quyền nằm ngoài phạm vi bạn được giao.", "PERMISSION_OUT_OF_SCOPE");
                }

                var managedDeptSet = ctx.ManagedDepartmentIds.ToHashSet();
                if (distinctDeptIds.Any(did => !managedDeptSet.Contains(did)))
                {
                    return OperationResult.Fail("Bạn không thể giao quyền quản lý phòng ban nằm ngoài phạm vi của bạn.", "DEPARTMENT_OUT_OF_SCOPE");
                }

                if ((req.CanCreateUser && !ctx.CanCreateUser) ||
                    (req.CanEditUser && !ctx.CanEditUser) ||
                    (req.CanDeleteUser && !ctx.CanDeleteUser) ||
                    (req.CanResetPassword && !ctx.CanResetPassword) ||
                    (req.CanAssignRole && !ctx.CanAssignRole) ||
                    (req.CanDelegateFurther && !ctx.CanDelegateFurther))
                {
                    return OperationResult.Fail("Bạn không thể cấp cờ thao tác quản trị nhân sự mà chính bạn không sở hữu.", "CAPABILITY_OUT_OF_SCOPE");
                }
            }

            var saveResult = await ExecuteInTransactionAsync(async () =>
            {
                var existingScope = await _context.AdminDelegationScopes
                    .Include(s => s.AllowedPermissions)
                    .Include(s => s.AllowedDepartments)
                    .Include(s => s.AllowedStores)
                    .FirstOrDefaultAsync(s => s.AdminUserId == req.AdminUserId);

                var oldJson = existingScope != null
                    ? JsonSerializer.Serialize(new
                    {
                        existingScope.AdminUserId,
                        existingScope.CanCreateUser,
                        existingScope.CanEditUser,
                        existingScope.CanDeleteUser,
                        existingScope.CanResetPassword,
                        existingScope.CanAssignRole,
                        existingScope.CanDelegateFurther,
                        existingScope.MaxGrantLevel,
                        PermissionIds = existingScope.AllowedPermissions.Select(p => p.PermissionId).OrderBy(x => x).ToList(),
                        DepartmentIds = existingScope.AllowedDepartments.Select(d => d.DepartmentId).OrderBy(x => x).ToList(),
                        StoreIds = existingScope.AllowedStores.Select(st => st.StoreId).OrderBy(x => x).ToList()
                    })
                    : null;

                if (existingScope == null)
                {
                    existingScope = new AdminDelegationScope
                    {
                        AdminUserId = req.AdminUserId,
                        CanCreateUser = req.CanCreateUser,
                        CanEditUser = req.CanEditUser,
                        CanDeleteUser = req.CanDeleteUser,
                        CanResetPassword = req.CanResetPassword,
                        CanAssignRole = req.CanAssignRole,
                        CanDelegateFurther = req.CanDelegateFurther,
                        MaxGrantLevel = req.MaxGrantLevel,
                        GrantedByUserId = actorId,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _context.AdminDelegationScopes.AddAsync(existingScope);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    existingScope.CanCreateUser = req.CanCreateUser;
                    existingScope.CanEditUser = req.CanEditUser;
                    existingScope.CanDeleteUser = req.CanDeleteUser;
                    existingScope.CanResetPassword = req.CanResetPassword;
                    existingScope.CanAssignRole = req.CanAssignRole;
                    existingScope.CanDelegateFurther = req.CanDelegateFurther;
                    existingScope.MaxGrantLevel = req.MaxGrantLevel;
                    existingScope.GrantedByUserId = actorId;
                    existingScope.UpdatedAt = DateTime.UtcNow;

                    _context.AdminDelegationPermissions.RemoveRange(existingScope.AllowedPermissions);
                    _context.AdminDelegationDepartments.RemoveRange(existingScope.AllowedDepartments);
                    _context.AdminDelegationStores.RemoveRange(existingScope.AllowedStores);
                }

                if (distinctPermIds.Count > 0)
                {
                    await _context.AdminDelegationPermissions.AddRangeAsync(
                        distinctPermIds.Select(pid => new AdminDelegationPermission
                        {
                            AdminDelegationScopeId = existingScope.Id,
                            PermissionId = pid
                        }));
                }

                if (distinctDeptIds.Count > 0)
                {
                    await _context.AdminDelegationDepartments.AddRangeAsync(
                        distinctDeptIds.Select(did => new AdminDelegationDepartment
                        {
                            AdminDelegationScopeId = existingScope.Id,
                            DepartmentId = did
                        }));
                }

                if (distinctStoreIds.Count > 0)
                {
                    await _context.AdminDelegationStores.AddRangeAsync(
                        distinctStoreIds.Select(sid => new AdminDelegationStore
                        {
                            AdminDelegationScopeId = existingScope.Id,
                            StoreId = sid
                        }));
                }

                var adminUser = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == req.AdminUserId);
                if (adminUser != null)
                {
                    adminUser.SecurityStamp = Guid.NewGuid().ToString("N");
                    adminUser.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                await _permissionService.InvalidateUserPermissionCacheAsync(req.AdminUserId);

                var newJson = JsonSerializer.Serialize(new
                {
                    ActorId = actorId,
                    TargetUserId = req.AdminUserId,
                    req.CanCreateUser,
                    req.CanEditUser,
                    req.CanDeleteUser,
                    req.CanResetPassword,
                    req.CanAssignRole,
                    req.CanDelegateFurther,
                    req.MaxGrantLevel,
                    PermissionIds = distinctPermIds.OrderBy(x => x).ToList(),
                    DepartmentIds = distinctDeptIds.OrderBy(x => x).ToList(),
                    StoreIds = distinctStoreIds.OrderBy(x => x).ToList()
                });

                await WriteAuditAsync(
                    action: "UPDATE_SCOPE",
                    entityType: "AdminDelegationScope",
                    entityId: existingScope.Id.ToString(),
                    oldValues: oldJson,
                    newValues: newJson,
                    actorId: actorId,
                    actorUsername: ctx.ActorUsername);

                return OperationResult.Ok("Đã lưu cấu hình phạm vi ủy quyền thành công.");
            });

            if (!saveResult.Succeeded)
            {
                return saveResult;
            }

            // Tự động thu hồi dây chuyền (Cascade Revoke) các quyền mà Admin nhỏ này đã cấp vượt quá Scope mới
            await CascadeRevokeAsync(req.AdminUserId);

            return saveResult;
        }

        public async Task<OperationResult> CascadeRevokeAsync(int adminUserId)
        {
            if (adminUserId <= 0)
            {
                return OperationResult.Fail("Mã quản trị viên không hợp lệ.", "INVALID_ADMIN_ID");
            }

            var adminCtx = await GetContextAsync(adminUserId);
            var allowedPermSet = adminCtx.GrantablePermissionIds.ToHashSet();
            var allowedDeptSet = adminCtx.ManagedDepartmentIds.ToHashSet();
            int maxGrantLevel = adminCtx.MaxGrantLevel;

            return await ExecuteInTransactionAsync(async () =>
            {
                // 1. Tìm tất cả UserPermission (Grant) mà adminUserId đã cấp cho cấp dưới
                var grantedOverrides = await _context.UserPermissions
                    .Include(up => up.User)
                    .Include(up => up.Permission)
                    .Where(up => up.GrantedByUserId == adminUserId && up.IsGranted)
                    .ToListAsync();

                var overridesToRevoke = new List<UserPermission>();
                var affectedUserIds = new HashSet<int>();

                foreach (var ovr in grantedOverrides)
                {
                    bool permOutOfScope = !allowedPermSet.Contains(ovr.PermissionId);
                    bool deptOutOfScope = allowedDeptSet.Count > 0 &&
                                          ovr.User != null &&
                                          ovr.User.DepartmentId.HasValue &&
                                          !allowedDeptSet.Contains(ovr.User.DepartmentId.Value) &&
                                          ovr.User.ManagerId != adminUserId;

                    if (permOutOfScope || deptOutOfScope)
                    {
                        overridesToRevoke.Add(ovr);
                        affectedUserIds.Add(ovr.UserId);
                    }
                }

                // 2. Tìm tất cả UserRole mà adminUserId đã gán nhưng vượt quá MaxGrantLevel hoặc ngoài phạm vi quyền mới
                var assignedRoles = await _context.UserRoles
                    .Include(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                    .Where(ur => ur.AssignedByUserId == adminUserId)
                    .ToListAsync();

                var rolesToRevoke = new List<UserRole>();
                foreach (var ur in assignedRoles)
                {
                    if (ur.Role == null) continue;
                    bool levelExceeded = !adminCtx.CanAssignRole || ur.Role.Level >= maxGrantLevel || ur.Role.Level >= adminCtx.HighestRoleLevel;
                    bool rolePermsExceeded = ur.Role.RolePermissions.Any(rp => !allowedPermSet.Contains(rp.PermissionId));

                    if (levelExceeded || rolePermsExceeded)
                    {
                        rolesToRevoke.Add(ur);
                        affectedUserIds.Add(ur.UserId);
                    }
                }

                if (overridesToRevoke.Count == 0 && rolesToRevoke.Count == 0)
                {
                    return OperationResult.Ok("Không có quyền cấp dưới nào vượt quá phạm vi ủy quyền mới.");
                }

                var auditDetails = JsonSerializer.Serialize(new
                {
                    AdminUserId = adminUserId,
                    AffectedUserIds = affectedUserIds.OrderBy(id => id).ToList(),
                    RevokedOverrides = overridesToRevoke.Select(o => new
                    {
                        o.UserId,
                        o.PermissionId,
                        PermissionCode = o.Permission?.Code
                    }).ToList(),
                    RevokedRoles = rolesToRevoke.Select(r => new
                    {
                        r.UserId,
                        r.RoleId,
                        RoleName = r.Role?.Name
                    }).ToList()
                });

                if (overridesToRevoke.Count > 0)
                {
                    _context.UserPermissions.RemoveRange(overridesToRevoke);
                }

                if (rolesToRevoke.Count > 0)
                {
                    _context.UserRoles.RemoveRange(rolesToRevoke);
                }

                var affectedUsers = await _context.Users
                    .IgnoreQueryFilters()
                    .Where(u => affectedUserIds.Contains(u.Id))
                    .ToListAsync();

                foreach (var u in affectedUsers)
                {
                    u.SecurityStamp = Guid.NewGuid().ToString("N");
                    u.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                foreach (var uid in affectedUserIds)
                {
                    await _permissionService.InvalidateUserPermissionCacheAsync(uid);
                }

                await WriteAuditAsync(
                    action: "CASCADE_REVOKE",
                    entityType: "AdminDelegationScope",
                    entityId: adminUserId.ToString(),
                    oldValues: auditDetails,
                    newValues: $"Đã tự động thu hồi {overridesToRevoke.Count} quyền đặc cách và {rolesToRevoke.Count} vai trò vượt phạm vi ủy quyền mới của Admin #{adminUserId}.",
                    actorId: adminUserId,
                    actorUsername: adminCtx.ActorUsername);

                return OperationResult.Ok(
                    $"Đã thu hồi dây chuyền {overridesToRevoke.Count} quyền đặc cách và {rolesToRevoke.Count} vai trò trên {affectedUserIds.Count} nhân viên cấp dưới.");
            });
        }

        private async Task<OperationResult> ExecuteInTransactionAsync(Func<Task<OperationResult>> operation)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                if (_context.Database.IsRelational() && _context.Database.CurrentTransaction == null)
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        var result = await operation();
                        if (result.Succeeded)
                        {
                            await transaction.CommitAsync();
                        }
                        else
                        {
                            await transaction.RollbackAsync();
                        }
                        return result;
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        return OperationResult.Fail($"Lỗi giao dịch cơ sở dữ liệu: {ex.Message}", "TRANSACTION_ERROR");
                    }
                }

                try
                {
                    return await operation();
                }
                catch (Exception ex)
                {
                    return OperationResult.Fail($"Lỗi xử lý dữ liệu: {ex.Message}", "EXECUTION_ERROR");
                }
            });
        }

        private async Task WriteAuditAsync(
            string action,
            string entityType,
            string entityId,
            string? oldValues,
            string? newValues,
            int actorId,
            string? actorUsername)
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            string ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string userAgent = httpContext?.Request?.Headers["User-Agent"].ToString() ?? "System/DelegationService";

            await _auditLogService.LogAsync(
                action: action,
                entityType: entityType,
                entityId: entityId,
                oldValues: oldValues,
                newValues: newValues,
                userId: actorId > 0 ? actorId : null,
                userName: !string.IsNullOrWhiteSpace(actorUsername) ? actorUsername : $"User#{actorId}",
                ipAddress: ipAddress,
                userAgent: userAgent);
        }
    }
}
