using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.Common;
using Omnichannel.Application.DTOs.HR;
using Omnichannel.Application.DTOs.Security;
using Omnichannel.Application.Interfaces.HR;
using Omnichannel.Application.Interfaces.Security;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services.HR
{
    public class StaffManagementService : IStaffManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDelegationService _delegationService;
        private readonly IPermissionService _permissionService;
        private readonly IPasswordHasherService _passwordHasher;
        private readonly IAuditLogService _auditLogService;

        public StaffManagementService(
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

        public async Task<PagedResult<StaffListItemDto>> GetPagedAsync(int actorId, StaffFilterQuery query)
        {
            var actorCtx = await _delegationService.GetContextAsync(actorId);

            var queryable = _context.Users
                .AsNoTracking()
                .Include(u => u.Department)
                .Include(u => u.Store)
                .Include(u => u.Manager)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => u.UserType >= 2 && !u.IsDeleted);

            queryable = _delegationService.ApplyUserScope(queryable, actorCtx);
            queryable = ApplyStaffFilters(queryable, query);
            queryable = ApplyStaffSorting(queryable, query.SortBy, query.SortDir);

            int totalItems = await queryable.CountAsync();
            int page = Math.Max(1, query.Page);
            int pageSize = Math.Clamp(query.PageSize > 0 ? query.PageSize : 20, 1, 100);

            var entities = await queryable
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var headUserIds = (await _context.Departments
                .AsNoTracking()
                .Where(d => d.HeadUserId.HasValue)
                .Select(d => d.HeadUserId!.Value)
                .ToListAsync())
                .ToHashSet();

            var items = new List<StaffListItemDto>();
            foreach (var u in entities)
            {
                bool canManage = actorCtx.IsSuperAdmin || await _delegationService.CanManageUserAsync(actorId, u.Id);
                items.Add(MapToListItemDto(u, canManage, headUserIds.Contains(u.Id)));
            }

            return new PagedResult<StaffListItemDto>
            {
                Items = items,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<StaffListItemDto>> GetTrashPagedAsync(int actorId, StaffFilterQuery query)
        {
            var actorCtx = await _delegationService.GetContextAsync(actorId);

            var queryable = _context.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(u => u.Department)
                .Include(u => u.Store)
                .Include(u => u.Manager)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => u.UserType >= 2 && u.IsDeleted);

            queryable = _delegationService.ApplyUserScope(queryable, actorCtx);
            queryable = ApplyStaffFilters(queryable, query);
            queryable = ApplyStaffSorting(queryable, query.SortBy, query.SortDir);

            int totalItems = await queryable.CountAsync();
            int page = Math.Max(1, query.Page);
            int pageSize = Math.Clamp(query.PageSize > 0 ? query.PageSize : 20, 1, 100);

            var entities = await queryable
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var deletedByIds = entities
                .Where(u => u.DeletedBy.HasValue)
                .Select(u => u.DeletedBy!.Value)
                .Distinct()
                .ToList();

            var deletedByNames = deletedByIds.Count > 0
                ? await _context.Users
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(u => deletedByIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => $"{u.FullName} (@{u.Username})")
                : new Dictionary<int, string>();

            var items = entities.Select(u =>
            {
                var dto = MapToListItemDto(u, canActorManage: actorCtx.IsSuperAdmin || actorCtx.CanDeleteUser, isHead: false);
                if (u.DeletedBy.HasValue && deletedByNames.TryGetValue(u.DeletedBy.Value, out var delName))
                {
                    dto.DeletedByName = delName;
                }
                return dto;
            }).ToList();

            return new PagedResult<StaffListItemDto>
            {
                Items = items,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<StaffStatsDto> GetStatsAsync(int actorId)
        {
            var actorCtx = await _delegationService.GetContextAsync(actorId);

            var baseQuery = _context.Users.AsNoTracking().Where(u => u.UserType >= 2 && !u.IsDeleted);
            baseQuery = _delegationService.ApplyUserScope(baseQuery, actorCtx);

            var trashQuery = _context.Users.IgnoreQueryFilters().AsNoTracking().Where(u => u.UserType >= 2 && u.IsDeleted);
            trashQuery = _delegationService.ApplyUserScope(trashQuery, actorCtx);

            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            var staffList = await baseQuery
                .Select(u => new
                {
                    u.Id,
                    u.IsActive,
                    u.CreatedAt,
                    DeptName = u.Department != null ? u.Department.Name : "Chưa phân phòng",
                    Roles = u.UserRoles
                        .Where(ur => ur.Role.IsActive && ur.Role.Name != "Customer")
                        .Select(ur => !string.IsNullOrEmpty(ur.Role.DisplayName) ? ur.Role.DisplayName : ur.Role.Name)
                })
                .ToListAsync();

            int trashCount = await trashQuery.CountAsync();

            var stats = new StaffStatsDto
            {
                TotalStaff = staffList.Count,
                ActiveStaff = staffList.Count(u => u.IsActive),
                LockedStaff = staffList.Count(u => !u.IsActive),
                NewStaffLast30Days = staffList.Count(u => u.CreatedAt >= thirtyDaysAgo),
                TrashStaff = trashCount
            };

            foreach (var g in staffList.GroupBy(u => u.DeptName))
            {
                stats.StaffByDepartment[g.Key] = g.Count();
            }

            foreach (var u in staffList)
            {
                foreach (var r in u.Roles)
                {
                    stats.StaffByRole[r] = stats.StaffByRole.TryGetValue(r, out int cnt) ? cnt + 1 : 1;
                }
            }

            return stats;
        }

        public async Task<StaffDetailDto?> GetDetailAsync(int actorId, int id)
        {
            var actorCtx = await _delegationService.GetContextAsync(actorId);

            var user = await _context.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(u => u.Department)
                .Include(u => u.Store)
                .Include(u => u.Manager)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id && u.UserType >= 2);

            if (user == null) return null;

            bool canManage = actorCtx.IsSuperAdmin || await _delegationService.CanManageUserAsync(actorId, id);

            var headDept = await _context.Departments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.HeadUserId == id);

            int subCount = await _context.Users
                .AsNoTracking()
                .CountAsync(u => u.ManagerId == id && u.UserType >= 2 && !u.IsDeleted);

            var permissions = await _permissionService.GetUserPermissionBreakdownAsync(id);

            string userIdStr = id.ToString();
            var recentLogs = await _context.AuditLogs
                .AsNoTracking()
                .Where(a => a.UserId == id || a.EntityId == userIdStr)
                .OrderByDescending(a => a.CreatedAt)
                .Take(20)
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

            var baseDto = MapToListItemDto(user, canManage, headDept != null);

            return new StaffDetailDto
            {
                Id = baseDto.Id,
                EmployeeCode = baseDto.EmployeeCode,
                Username = baseDto.Username,
                FullName = baseDto.FullName,
                Email = baseDto.Email,
                PhoneNumber = baseDto.PhoneNumber,
                DepartmentId = baseDto.DepartmentId,
                DepartmentName = baseDto.DepartmentName,
                JobTitle = baseDto.JobTitle,
                StoreId = baseDto.StoreId,
                StoreName = baseDto.StoreName,
                ManagerId = baseDto.ManagerId,
                ManagerName = baseDto.ManagerName,
                Gender = baseDto.Gender,
                DateOfBirth = baseDto.DateOfBirth,
                HireDate = baseDto.HireDate,
                LastLoginAt = baseDto.LastLoginAt,
                IsActive = baseDto.IsActive,
                IsDeleted = baseDto.IsDeleted,
                CreatedAt = baseDto.CreatedAt,
                DeletedAt = baseDto.DeletedAt,
                CanActorManage = baseDto.CanActorManage,
                IsDepartmentHead = headDept != null,
                HeadOfDepartmentName = headDept?.Name,
                Roles = baseDto.Roles,
                Address = user.Address,
                Notes = user.Notes,
                SubordinatesCount = subCount,
                EffectivePermissions = permissions,
                RecentAuditLogs = recentLogs
            };
        }

        public async Task<OperationResult<int>> CreateAsync(int actorId, CreateStaffRequest request)
        {
            var actorCtx = await _delegationService.GetContextAsync(actorId);
            if (!actorCtx.IsSuperAdmin && !actorCtx.CanCreateUser)
            {
                return OperationResult<int>.Fail("Bạn không có quyền tạo tài khoản nhân sự mới trong phạm vi ủy quyền.", "FORBIDDEN");
            }

            string cleanUsername = request.Username.Trim().ToLowerInvariant();
            if (await CheckUsernameExistsAsync(cleanUsername))
            {
                return OperationResult<int>.Fail($"Tên đăng nhập '{cleanUsername}' đã được sử dụng.", "DUPLICATE_USERNAME");
            }

            string cleanEmail = request.Email.Trim().ToLowerInvariant();
            if (await CheckEmailExistsAsync(cleanEmail))
            {
                return OperationResult<int>.Fail($"Địa chỉ Email '{cleanEmail}' đã tồn tại trong hệ thống.", "DUPLICATE_EMAIL");
            }

            if (!string.IsNullOrWhiteSpace(request.EmployeeCode))
            {
                string cleanCode = request.EmployeeCode.Trim().ToUpperInvariant();
                if (await CheckEmployeeCodeExistsAsync(cleanCode))
                {
                    return OperationResult<int>.Fail($"Mã nhân viên '{cleanCode}' đã được cấp cho nhân sự khác.", "DUPLICATE_EMPLOYEE_CODE");
                }
                request.EmployeeCode = cleanCode;
            }

            if (request.DepartmentId.HasValue && !actorCtx.IsSuperAdmin)
            {
                if (!actorCtx.ManagedDepartmentIds.Contains(request.DepartmentId.Value))
                {
                    return OperationResult<int>.Fail("Phòng ban được chọn nằm ngoài phạm vi quản lý của bạn.", "OUT_OF_SCOPE_DEPARTMENT");
                }
            }

            // Kiểm tra tính hợp lệ của vai trò được gán
            if (request.RoleIds != null && request.RoleIds.Count > 0)
            {
                if (!actorCtx.IsSuperAdmin && !actorCtx.CanAssignRole)
                {
                    return OperationResult<int>.Fail("Bạn không có quyền gán vai trò cho nhân sự.", "FORBIDDEN_ROLE_ASSIGNMENT");
                }

                var assignableRoleIds = (await _delegationService.GetAssignableRoleIdsAsync(actorId)).ToHashSet();
                foreach (var rId in request.RoleIds)
                {
                    if (!actorCtx.IsSuperAdmin && !assignableRoleIds.Contains(rId))
                    {
                        return OperationResult<int>.Fail($"Vai trò ID #{rId} vượt quá trần cấp bậc (MaxGrantLevel = {actorCtx.MaxGrantLevel}) hoặc vượt quyền hạn của bạn.", "ROLE_OUT_OF_SCOPE");
                    }
                }
            }

            var newUser = new User
            {
                Username = cleanUsername,
                NormalizedUsername = cleanUsername.ToUpperInvariant(),
                FullName = request.FullName.Trim(),
                Email = cleanEmail,
                PhoneNumber = request.PhoneNumber?.Trim(),
                EmployeeCode = request.EmployeeCode,
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                UserType = 2,
                DepartmentId = request.DepartmentId,
                JobTitle = request.JobTitle?.Trim(),
                StoreId = request.StoreId,
                ManagerId = request.ManagerId,
                Gender = request.Gender,
                DateOfBirth = request.DateOfBirth,
                HireDate = request.HireDate ?? DateTime.Today,
                Address = request.Address?.Trim(),
                Notes = request.Notes?.Trim(),
                IsActive = true,
                IsDeleted = false,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = actorId
            };

            await _context.Users.AddAsync(newUser);
            await _context.SaveChangesAsync();

            if (request.RoleIds != null && request.RoleIds.Count > 0)
            {
                foreach (var roleId in request.RoleIds.Distinct())
                {
                    await _context.UserRoles.AddAsync(new UserRole
                    {
                        UserId = newUser.Id,
                        RoleId = roleId,
                        AssignedByUserId = actorId,
                        AssignedAt = DateTime.UtcNow
                    });
                }
                await _context.SaveChangesAsync();
            }

            await _auditLogService.LogAsync(
                action: "STAFF_CREATE",
                entityType: "User",
                entityId: newUser.Id.ToString(),
                oldValues: null,
                newValues: $"Tạo mới nhân sự {newUser.FullName} (@{newUser.Username}), Mã: {newUser.EmployeeCode}, Phòng: {request.DepartmentId}.",
                userId: actorId,
                userName: actorCtx.IsSuperAdmin ? "SuperAdmin" : "Admin"
            );

            return OperationResult<int>.Ok(newUser.Id, $"Đã tạo mới nhân sự '{newUser.FullName}' thành công.");
        }

        public async Task<OperationResult> UpdateAsync(int actorId, int id, UpdateStaffRequest request)
        {
            var actorCtx = await _delegationService.GetContextAsync(actorId);
            if (!actorCtx.IsSuperAdmin && !await _delegationService.CanManageUserAsync(actorId, id))
            {
                return OperationResult.Fail("Bạn không có quyền chỉnh sửa nhân sự này (nằm ngoài phạm vi quản lý hoặc cấp bậc).", "FORBIDDEN");
            }

            if (!actorCtx.IsSuperAdmin && !actorCtx.CanEditUser)
            {
                return OperationResult.Fail("Bạn không có quyền chỉnh sửa thông tin nhân viên trong Scope.", "FORBIDDEN");
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id && u.UserType >= 2);

            if (user == null)
            {
                return OperationResult.Fail("Không tìm thấy hồ sơ nhân sự cần cập nhật.", "NOT_FOUND");
            }

            string cleanEmail = request.Email.Trim().ToLowerInvariant();
            if (await CheckEmailExistsAsync(cleanEmail, id))
            {
                return OperationResult.Fail($"Địa chỉ Email '{cleanEmail}' đã được sử dụng bởi nhân sự khác.", "DUPLICATE_EMAIL");
            }

            if (!string.IsNullOrWhiteSpace(request.EmployeeCode))
            {
                string cleanCode = request.EmployeeCode.Trim().ToUpperInvariant();
                if (await CheckEmployeeCodeExistsAsync(cleanCode, id))
                {
                    return OperationResult.Fail($"Mã nhân viên '{cleanCode}' đã được sử dụng.", "DUPLICATE_EMPLOYEE_CODE");
                }
                user.EmployeeCode = cleanCode;
            }
            else
            {
                user.EmployeeCode = null;
            }

            if (request.DepartmentId.HasValue && !actorCtx.IsSuperAdmin)
            {
                if (!actorCtx.ManagedDepartmentIds.Contains(request.DepartmentId.Value))
                {
                    return OperationResult.Fail("Phòng ban chuyển tới nằm ngoài phạm vi quản lý của bạn.", "OUT_OF_SCOPE_DEPARTMENT");
                }
            }

            if (id == actorId && !request.IsActive)
            {
                return OperationResult.Fail("Bạn không thể tự khóa tài khoản của chính mình.", "CANNOT_LOCK_SELF");
            }

            user.FullName = request.FullName.Trim();
            user.Email = cleanEmail;
            user.PhoneNumber = request.PhoneNumber?.Trim();
            user.DepartmentId = request.DepartmentId;
            user.JobTitle = request.JobTitle?.Trim();
            user.StoreId = request.StoreId;
            user.ManagerId = request.ManagerId;
            user.Gender = request.Gender;
            user.DateOfBirth = request.DateOfBirth;
            user.HireDate = request.HireDate;
            user.Address = request.Address?.Trim();
            user.Notes = request.Notes?.Trim();
            user.IsActive = request.IsActive;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = actorId;

            // Xử lý cập nhật danh sách vai trò nếu được phép
            if (request.RoleIds != null)
            {
                if (actorCtx.IsSuperAdmin || actorCtx.CanAssignRole)
                {
                    var assignableRoleIds = (await _delegationService.GetAssignableRoleIdsAsync(actorId)).ToHashSet();
                    var newRoleSet = request.RoleIds.Distinct().ToList();

                    // Giữ lại các role nằm ngoài thẩm quyền gán của actor (không tước oan các role cao hơn)
                    var currentRoles = user.UserRoles.ToList();
                    var rolesToKeep = currentRoles
                        .Where(ur => !assignableRoleIds.Contains(ur.RoleId))
                        .ToList();

                    var allowedNewRoles = newRoleSet
                        .Where(rId => actorCtx.IsSuperAdmin || assignableRoleIds.Contains(rId))
                        .ToList();

                    _context.UserRoles.RemoveRange(currentRoles.Where(ur => assignableRoleIds.Contains(ur.RoleId)));

                    foreach (var rId in allowedNewRoles)
                    {
                        _context.UserRoles.Add(new UserRole
                        {
                            UserId = user.Id,
                            RoleId = rId,
                            AssignedByUserId = actorId,
                            AssignedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            user.SecurityStamp = Guid.NewGuid().ToString("N");
            await _context.SaveChangesAsync();
            await _permissionService.InvalidateUserPermissionCacheAsync(user.Id);

            await _auditLogService.LogAsync(
                action: "STAFF_UPDATE",
                entityType: "User",
                entityId: user.Id.ToString(),
                oldValues: null,
                newValues: $"Cập nhật hồ sơ nhân sự {user.FullName} (@{user.Username}).",
                userId: actorId
            );

            return OperationResult.Ok($"Đã cập nhật thông tin nhân sự '{user.FullName}' thành công.");
        }

        public async Task<OperationResult> SoftDeleteAsync(int actorId, int id)
        {
            if (id == actorId)
            {
                return OperationResult.Fail("Bạn không thể tự xóa tài khoản của chính mình.", "CANNOT_DELETE_SELF");
            }

            var actorCtx = await _delegationService.GetContextAsync(actorId);
            if (!actorCtx.IsSuperAdmin && !await _delegationService.CanManageUserAsync(actorId, id))
            {
                return OperationResult.Fail("Bạn không có quyền xóa nhân sự này (nằm ngoài phạm vi quản lý hoặc cấp bậc).", "FORBIDDEN");
            }

            if (!actorCtx.IsSuperAdmin && !actorCtx.CanDeleteUser)
            {
                return OperationResult.Fail("Bạn không có quyền xóa nhân sự trong phạm vi ủy quyền.", "FORBIDDEN");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.UserType >= 2);
            if (user == null)
            {
                return OperationResult.Fail("Không tìm thấy hồ sơ nhân sự.", "NOT_FOUND");
            }

            var headDept = await _context.Departments.FirstOrDefaultAsync(d => d.HeadUserId == id);
            if (headDept != null)
            {
                return OperationResult.Fail($"Nhân sự này đang giữ chức vụ Trưởng phòng '{headDept.Name}'. Vui lòng bổ nhiệm Trưởng phòng mới cho phòng ban này trước khi xóa nhân sự.", "CANNOT_DELETE_DEPT_HEAD");
            }

            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.DeletedBy = actorId;
            user.IsActive = false;
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = actorId;

            await _context.SaveChangesAsync();
            await _permissionService.InvalidateUserPermissionCacheAsync(user.Id);

            await _auditLogService.LogAsync(
                action: "STAFF_SOFT_DELETE",
                entityType: "User",
                entityId: user.Id.ToString(),
                oldValues: null,
                newValues: $"Xóa mềm nhân sự {user.FullName} (@{user.Username}) và chuyển vào thùng rác.",
                userId: actorId
            );

            return OperationResult.Ok($"Đã đưa nhân sự '{user.FullName}' vào thùng rác thành công.");
        }

        public async Task<OperationResult> RestoreAsync(int actorId, int id)
        {
            var actorCtx = await _delegationService.GetContextAsync(actorId);
            if (!actorCtx.IsSuperAdmin && !actorCtx.CanDeleteUser)
            {
                return OperationResult.Fail("Bạn không có quyền khôi phục nhân sự từ thùng rác.", "FORBIDDEN");
            }

            var user = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == id && u.UserType >= 2 && u.IsDeleted);

            if (user == null)
            {
                return OperationResult.Fail("Không tìm thấy nhân sự trong thùng rác.", "NOT_FOUND");
            }

            if (!actorCtx.IsSuperAdmin && !await _delegationService.CanManageUserAsync(actorId, id))
            {
                return OperationResult.Fail("Nhân sự này nằm ngoài phạm vi phòng ban bạn được quản lý.", "FORBIDDEN");
            }

            user.IsDeleted = false;
            user.DeletedAt = null;
            user.DeletedBy = null;
            user.IsActive = true;
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = actorId;

            await _context.SaveChangesAsync();
            await _permissionService.InvalidateUserPermissionCacheAsync(user.Id);

            await _auditLogService.LogAsync(
                action: "STAFF_RESTORE",
                entityType: "User",
                entityId: user.Id.ToString(),
                oldValues: null,
                newValues: $"Khôi phục nhân sự {user.FullName} (@{user.Username}) từ thùng rác.",
                userId: actorId
            );

            return OperationResult.Ok($"Đã khôi phục nhân sự '{user.FullName}' về danh sách hoạt động.");
        }

        public async Task<OperationResult> ToggleStatusAsync(int actorId, int id)
        {
            if (id == actorId)
            {
                return OperationResult.Fail("Bạn không thể tự khóa tài khoản của chính mình.", "CANNOT_LOCK_SELF");
            }

            var actorCtx = await _delegationService.GetContextAsync(actorId);
            if (!actorCtx.IsSuperAdmin && !await _delegationService.CanManageUserAsync(actorId, id))
            {
                return OperationResult.Fail("Bạn không có quyền thao tác trên nhân sự này.", "FORBIDDEN");
            }

            if (!actorCtx.IsSuperAdmin && !actorCtx.CanEditUser)
            {
                return OperationResult.Fail("Bạn không có quyền khóa hoặc mở khóa tài khoản nhân sự.", "FORBIDDEN");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.UserType >= 2);
            if (user == null)
            {
                return OperationResult.Fail("Không tìm thấy hồ sơ nhân sự.", "NOT_FOUND");
            }

            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = actorId;
            user.SecurityStamp = Guid.NewGuid().ToString("N");

            await _context.SaveChangesAsync();
            await _permissionService.InvalidateUserPermissionCacheAsync(user.Id);

            string actionText = user.IsActive ? "mở khóa" : "tạm khóa";

            await _auditLogService.LogAsync(
                action: "STAFF_TOGGLE_STATUS",
                entityType: "User",
                entityId: user.Id.ToString(),
                oldValues: null,
                newValues: $"Đã {actionText} tài khoản nhân sự {user.FullName} (@{user.Username}).",
                userId: actorId
            );

            return OperationResult.Ok($"Đã {actionText} tài khoản nhân sự '{user.FullName}' thành công.");
        }

        public async Task<OperationResult<string>> ResetPasswordAsync(int actorId, ResetPasswordRequest request)
        {
            var actorCtx = await _delegationService.GetContextAsync(actorId);
            if (!actorCtx.IsSuperAdmin && !await _delegationService.CanManageUserAsync(actorId, request.StaffId))
            {
                return OperationResult<string>.Fail("Bạn không có quyền đặt lại mật khẩu cho nhân sự này.", "FORBIDDEN");
            }

            if (!actorCtx.IsSuperAdmin && !actorCtx.CanResetPassword)
            {
                return OperationResult<string>.Fail("Bạn không có quyền đặt lại mật khẩu nhân sự trong Scope.", "FORBIDDEN");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.StaffId && u.UserType >= 2);
            if (user == null)
            {
                return OperationResult<string>.Fail("Không tìm thấy hồ sơ nhân sự.", "NOT_FOUND");
            }

            string passwordToSet = !string.IsNullOrWhiteSpace(request.NewPassword)
                ? request.NewPassword.Trim()
                : GenerateStrongPassword();

            user.PasswordHash = _passwordHasher.HashPassword(passwordToSet);
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = actorId;

            await _context.SaveChangesAsync();
            await _permissionService.InvalidateUserPermissionCacheAsync(user.Id);

            await _auditLogService.LogAsync(
                action: "STAFF_RESET_PASSWORD",
                entityType: "User",
                entityId: user.Id.ToString(),
                oldValues: null,
                newValues: $"Đặt lại mật khẩu cho nhân sự {user.FullName} (@{user.Username}).",
                userId: actorId
            );

            return OperationResult<string>.Ok(passwordToSet, $"Đã đặt lại mật khẩu cho nhân sự '{user.FullName}' thành công.");
        }

        public async Task<BulkStaffActionResult> BulkAsync(int actorId, BulkStaffActionRequest request)
        {
            var result = new BulkStaffActionResult
            {
                TotalRequested = request.StaffIds.Count
            };

            if (request.StaffIds == null || request.StaffIds.Count == 0)
            {
                result.Messages.Add("Danh sách nhân sự thực hiện thao tác rỗng.");
                return result;
            }

            foreach (var staffId in request.StaffIds.Distinct())
            {
                if (staffId == actorId && (request.Action == "lock" || request.Action == "delete"))
                {
                    result.FailedCount++;
                    result.Messages.Add($"Bỏ qua ID #{staffId}: Không thể tự khóa hoặc xóa tài khoản của chính mình.");
                    continue;
                }

                OperationResult opResult = request.Action.ToLowerInvariant() switch
                {
                    "lock" => await ToggleStatusCoreAsync(actorId, staffId, setActive: false),
                    "unlock" => await ToggleStatusCoreAsync(actorId, staffId, setActive: true),
                    "delete" => await SoftDeleteAsync(actorId, staffId),
                    "assigndepartment" => await AssignDepartmentCoreAsync(actorId, staffId, request.TargetDepartmentId),
                    "assignrole" => await AssignRoleCoreAsync(actorId, staffId, request.TargetRoleId),
                    _ => OperationResult.Fail($"Hành động hàng loạt '{request.Action}' không hợp lệ.")
                };

                if (opResult.Succeeded)
                {
                    result.SucceededCount++;
                }
                else
                {
                    result.FailedCount++;
                    result.Messages.Add($"ID #{staffId}: {opResult.Message}");
                }
            }

            return result;
        }

        public async Task<byte[]> ExportAsync(int actorId, StaffFilterQuery query, string format)
        {
            var actorCtx = await _delegationService.GetContextAsync(actorId);

            var queryable = _context.Users
                .AsNoTracking()
                .Include(u => u.Department)
                .Include(u => u.Store)
                .Include(u => u.Manager)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => u.UserType >= 2 && !u.IsDeleted);

            queryable = _delegationService.ApplyUserScope(queryable, actorCtx);
            queryable = ApplyStaffFilters(queryable, query);
            queryable = ApplyStaffSorting(queryable, query.SortBy, query.SortDir);

            var staffList = await queryable.Take(5000).ToListAsync();

            if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return GenerateExcelExport(staffList);
            }

            return GenerateCsvExport(staffList);
        }

        public async Task<bool> CheckUsernameExistsAsync(string username, int? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(username)) return false;
            string normalized = username.Trim().ToUpperInvariant();

            var query = _context.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(u => u.Username.ToUpper() == normalized);

            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<bool> CheckEmailExistsAsync(string email, int? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            string normalized = email.Trim().ToUpperInvariant();

            var query = _context.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(u => u.Email.ToUpper() == normalized);

            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<bool> CheckEmployeeCodeExistsAsync(string employeeCode, int? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(employeeCode)) return false;
            string normalized = employeeCode.Trim().ToUpperInvariant();

            var query = _context.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(u => u.EmployeeCode != null && u.EmployeeCode.ToUpper() == normalized);

            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<PagedResult<CustomerUserListItemDto>> GetCustomersPagedAsync(CustomerUserFilterQuery query)
        {
            var queryable = _context.Users
                .AsNoTracking()
                .Include(u => u.Store)
                .Include(u => u.Orders)
                .Where(u => u.UserType == 1 && !u.IsDeleted);

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                string kw = query.Keyword.Trim().ToLower();
                queryable = queryable.Where(u =>
                    u.FullName.ToLower().Contains(kw) ||
                    u.Username.ToLower().Contains(kw) ||
                    u.Email.ToLower().Contains(kw) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(kw)));
            }

            if (query.Status == "active") queryable = queryable.Where(u => u.IsActive);
            else if (query.Status == "locked") queryable = queryable.Where(u => !u.IsActive);

            int totalItems = await queryable.CountAsync();
            int page = Math.Max(1, query.Page);
            int pageSize = Math.Clamp(query.PageSize > 0 ? query.PageSize : 20, 1, 100);

            var entities = await queryable
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new CustomerUserListItemDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt,
                    TotalOrders = u.Orders.Count,
                    TotalSpent = u.Orders.Where(o => o.OrderStatus != "CANCELLED").Sum(o => (decimal?)o.TotalAmount) ?? 0,
                    StoreName = u.Store != null ? u.Store.Name : "Trực tuyến"
                })
                .ToListAsync();

            return new PagedResult<CustomerUserListItemDto>
            {
                Items = entities,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<OperationResult> ToggleCustomerStatusAsync(int actorId, int customerId)
        {
            var customer = await _context.Users.FirstOrDefaultAsync(u => u.Id == customerId && u.UserType == 1);
            if (customer == null)
            {
                return OperationResult.Fail("Không tìm thấy tài khoản khách hàng.", "NOT_FOUND");
            }

            customer.IsActive = !customer.IsActive;
            customer.UpdatedAt = DateTime.UtcNow;
            customer.UpdatedBy = actorId;
            customer.SecurityStamp = Guid.NewGuid().ToString("N");

            await _context.SaveChangesAsync();
            string statusStr = customer.IsActive ? "mở khóa" : "tạm khóa";

            await _auditLogService.LogAsync(
                action: "CUSTOMER_TOGGLE_STATUS",
                entityType: "User",
                entityId: customer.Id.ToString(),
                oldValues: null,
                newValues: $"Đã {statusStr} tài khoản khách hàng {customer.FullName} (@{customer.Username}).",
                userId: actorId
            );

            return OperationResult.Ok($"Đã {statusStr} tài khoản khách hàng '{customer.FullName}' thành công.");
        }

        #region Private Helpers

        private static IQueryable<User> ApplyStaffFilters(IQueryable<User> queryable, StaffFilterQuery query)
        {
            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                string kw = query.Keyword.Trim().ToLower();
                queryable = queryable.Where(u =>
                    u.FullName.ToLower().Contains(kw) ||
                    u.Username.ToLower().Contains(kw) ||
                    u.Email.ToLower().Contains(kw) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(kw)) ||
                    (u.EmployeeCode != null && u.EmployeeCode.ToLower().Contains(kw)));
            }

            if (query.DepartmentId.HasValue && query.DepartmentId.Value > 0)
            {
                queryable = queryable.Where(u => u.DepartmentId == query.DepartmentId.Value);
            }

            if (query.RoleId.HasValue && query.RoleId.Value > 0)
            {
                queryable = queryable.Where(u => u.UserRoles.Any(ur => ur.RoleId == query.RoleId.Value));
            }

            if (query.StoreId.HasValue && query.StoreId.Value > 0)
            {
                queryable = queryable.Where(u => u.StoreId == query.StoreId.Value);
            }

            if (query.ManagerId.HasValue && query.ManagerId.Value > 0)
            {
                queryable = queryable.Where(u => u.ManagerId == query.ManagerId.Value);
            }

            if (query.Status == "active")
            {
                queryable = queryable.Where(u => u.IsActive);
            }
            else if (query.Status == "locked")
            {
                queryable = queryable.Where(u => !u.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(query.Gender))
            {
                queryable = queryable.Where(u => u.Gender == query.Gender);
            }

            if (query.CreatedFrom.HasValue)
            {
                queryable = queryable.Where(u => u.CreatedAt >= query.CreatedFrom.Value);
            }

            if (query.CreatedTo.HasValue)
            {
                var end = query.CreatedTo.Value.Date.AddDays(1);
                queryable = queryable.Where(u => u.CreatedAt < end);
            }

            if (query.HireFrom.HasValue)
            {
                queryable = queryable.Where(u => u.HireDate >= query.HireFrom.Value);
            }

            if (query.HireTo.HasValue)
            {
                queryable = queryable.Where(u => u.HireDate <= query.HireTo.Value);
            }

            return queryable;
        }

        private static IQueryable<User> ApplyStaffSorting(IQueryable<User> queryable, string? sortBy, string? sortDir)
        {
            bool isDesc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

            return (sortBy?.ToLowerInvariant()) switch
            {
                "name" => isDesc ? queryable.OrderByDescending(u => u.FullName) : queryable.OrderBy(u => u.FullName),
                "hiredate" => isDesc ? queryable.OrderByDescending(u => u.HireDate) : queryable.OrderBy(u => u.HireDate),
                "lastlogin" => isDesc ? queryable.OrderByDescending(u => u.LastLoginAt) : queryable.OrderBy(u => u.LastLoginAt),
                "department" => isDesc ? queryable.OrderByDescending(u => u.Department != null ? u.Department.Name : "") : queryable.OrderBy(u => u.Department != null ? u.Department.Name : ""),
                "code" => isDesc ? queryable.OrderByDescending(u => u.EmployeeCode) : queryable.OrderBy(u => u.EmployeeCode),
                _ => isDesc ? queryable.OrderByDescending(u => u.CreatedAt) : queryable.OrderBy(u => u.CreatedAt)
            };
        }

        private static StaffListItemDto MapToListItemDto(User u, bool canActorManage, bool isHead)
        {
            return new StaffListItemDto
            {
                Id = u.Id,
                EmployeeCode = u.EmployeeCode,
                Username = u.Username,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                DepartmentId = u.DepartmentId,
                DepartmentName = u.Department?.Name,
                JobTitle = u.JobTitle,
                StoreId = u.StoreId,
                StoreName = u.Store?.Name,
                ManagerId = u.ManagerId,
                ManagerName = u.Manager?.FullName,
                Gender = u.Gender,
                DateOfBirth = u.DateOfBirth,
                HireDate = u.HireDate,
                LastLoginAt = u.LastLoginAt,
                IsActive = u.IsActive,
                IsDeleted = u.IsDeleted,
                CreatedAt = u.CreatedAt,
                DeletedAt = u.DeletedAt,
                CanActorManage = canActorManage,
                IsDepartmentHead = isHead,
                Roles = u.UserRoles
                    .Where(ur => ur.Role != null && ur.Role.IsActive && ur.Role.Name != "Customer")
                    .OrderByDescending(ur => ur.Role.Level)
                    .Select(ur => new StaffRoleBadgeDto
                    {
                        Id = ur.Role.Id,
                        Name = ur.Role.Name,
                        DisplayName = !string.IsNullOrEmpty(ur.Role.DisplayName) ? ur.Role.DisplayName : ur.Role.Name,
                        Color = !string.IsNullOrEmpty(ur.Role.Color) ? ur.Role.Color : "slate",
                        Level = ur.Role.Level
                    })
                    .ToList()
            };
        }

        private async Task<OperationResult> ToggleStatusCoreAsync(int actorId, int staffId, bool setActive)
        {
            var actorCtx = await _delegationService.GetContextAsync(actorId);
            if (!actorCtx.IsSuperAdmin && !await _delegationService.CanManageUserAsync(actorId, staffId))
            {
                return OperationResult.Fail("Nằm ngoài phạm vi quản lý của bạn.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == staffId && u.UserType >= 2);
            if (user == null) return OperationResult.Fail("Không tìm thấy nhân sự.");

            user.IsActive = setActive;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = actorId;
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            await _context.SaveChangesAsync();
            await _permissionService.InvalidateUserPermissionCacheAsync(user.Id);

            return OperationResult.Ok();
        }

        private async Task<OperationResult> AssignDepartmentCoreAsync(int actorId, int staffId, int? targetDepartmentId)
        {
            var actorCtx = await _delegationService.GetContextAsync(actorId);
            if (!actorCtx.IsSuperAdmin && !await _delegationService.CanManageUserAsync(actorId, staffId))
            {
                return OperationResult.Fail("Nằm ngoài phạm vi quản lý của bạn.");
            }

            if (targetDepartmentId.HasValue && !actorCtx.IsSuperAdmin && !actorCtx.ManagedDepartmentIds.Contains(targetDepartmentId.Value))
            {
                return OperationResult.Fail("Phòng ban chuyển đến nằm ngoài Scope.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == staffId && u.UserType >= 2);
            if (user == null) return OperationResult.Fail("Không tìm thấy nhân sự.");

            user.DepartmentId = targetDepartmentId;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = actorId;
            await _context.SaveChangesAsync();

            return OperationResult.Ok();
        }

        private async Task<OperationResult> AssignRoleCoreAsync(int actorId, int staffId, int? targetRoleId)
        {
            if (!targetRoleId.HasValue || targetRoleId.Value <= 0)
            {
                return OperationResult.Fail("Chưa chọn vai trò hợp lệ.");
            }

            var actorCtx = await _delegationService.GetContextAsync(actorId);
            if (!actorCtx.IsSuperAdmin && !await _delegationService.CanManageUserAsync(actorId, staffId))
            {
                return OperationResult.Fail("Nằm ngoài phạm vi quản lý của bạn.");
            }

            var assignableRoleIds = (await _delegationService.GetAssignableRoleIdsAsync(actorId)).ToHashSet();
            if (!actorCtx.IsSuperAdmin && !assignableRoleIds.Contains(targetRoleId.Value))
            {
                return OperationResult.Fail("Vai trò được chọn vượt trần cấp bậc được phép gán.");
            }

            var user = await _context.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Id == staffId && u.UserType >= 2);
            if (user == null) return OperationResult.Fail("Không tìm thấy nhân sự.");

            if (!user.UserRoles.Any(ur => ur.RoleId == targetRoleId.Value))
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = targetRoleId.Value,
                    AssignedByUserId = actorId,
                    AssignedAt = DateTime.UtcNow
                });
                user.SecurityStamp = Guid.NewGuid().ToString("N");
                await _context.SaveChangesAsync();
                await _permissionService.InvalidateUserPermissionCacheAsync(user.Id);
            }

            return OperationResult.Ok();
        }

        private static string GenerateStrongPassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string specials = "@#$%&*!";

            var bytes = new byte[8];
            RandomNumberGenerator.Fill(bytes);

            var sb = new StringBuilder();
            sb.Append(upper[bytes[0] % upper.Length]);
            sb.Append(lower[bytes[1] % lower.Length]);
            sb.Append(digits[bytes[2] % digits.Length]);
            sb.Append(specials[bytes[3] % specials.Length]);

            string allChars = upper + lower + digits + specials;
            for (int i = 4; i < 8; i++)
            {
                sb.Append(allChars[bytes[i] % allChars.Length]);
            }

            return "Canmake@" + sb.ToString();
        }

        private static byte[] GenerateExcelExport(List<User> list)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Danh Sách Nhân Sự");

            string[] headers =
            {
                "Mã Nhân Viên", "Họ Và Tên", "Tên Đăng Nhập", "Email", "Số Điện Thoại",
                "Phòng Ban", "Chức Danh", "Chi Nhánh", "Quản Lý Trực Tiếp", "Vai Trò",
                "Trạng Thái", "Ngày Vào Làm", "Ngày Tạo", "Đăng Nhập Cuối"
            };

            for (int col = 0; col < headers.Length; col++)
            {
                var cell = ws.Cell(1, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E11D48");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int row = 2;
            foreach (var u in list)
            {
                string rolesStr = string.Join(", ", u.UserRoles
                    .Where(ur => ur.Role != null && ur.Role.Name != "Customer")
                    .Select(ur => !string.IsNullOrEmpty(ur.Role.DisplayName) ? ur.Role.DisplayName : ur.Role.Name));

                ws.Cell(row, 1).Value = u.EmployeeCode ?? "";
                ws.Cell(row, 2).Value = u.FullName;
                ws.Cell(row, 3).Value = u.Username;
                ws.Cell(row, 4).Value = u.Email;
                ws.Cell(row, 5).Value = u.PhoneNumber ?? "";
                ws.Cell(row, 6).Value = u.Department?.Name ?? "";
                ws.Cell(row, 7).Value = u.JobTitle ?? "";
                ws.Cell(row, 8).Value = u.Store?.Name ?? "Trụ sở chính";
                ws.Cell(row, 9).Value = u.Manager?.FullName ?? "";
                ws.Cell(row, 10).Value = rolesStr;
                ws.Cell(row, 11).Value = u.IsActive ? "Đang hoạt động" : "Đã tạm khóa";
                ws.Cell(row, 12).Value = u.HireDate.HasValue ? u.HireDate.Value.ToString("dd/MM/yyyy") : "";
                ws.Cell(row, 13).Value = u.CreatedAt.ToString("dd/MM/yyyy HH:mm");
                ws.Cell(row, 14).Value = u.LastLoginAt.HasValue ? u.LastLoginAt.Value.ToString("dd/MM/yyyy HH:mm") : "Chưa đăng nhập";

                row++;
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        private static byte[] GenerateCsvExport(List<User> list)
        {
            var sb = new StringBuilder();

            // UTF-8 BOM
            sb.Append('\uFEFF');

            sb.AppendLine("Mã Nhân Viên,Họ Và Tên,Tên Đăng Nhập,Email,Số Điện Thoại,Phòng Ban,Chức Danh,Chi Nhánh,Quản Lý Trực Tiếp,Vai Trò,Trạng Thái,Ngày Vào Làm,Ngày Tạo,Đăng Nhập Cuối");

            foreach (var u in list)
            {
                string rolesStr = string.Join(";", u.UserRoles
                    .Where(ur => ur.Role != null && ur.Role.Name != "Customer")
                    .Select(ur => !string.IsNullOrEmpty(ur.Role.DisplayName) ? ur.Role.DisplayName : ur.Role.Name));

                sb.AppendLine(string.Join(",",
                    EscapeCsv(u.EmployeeCode ?? ""),
                    EscapeCsv(u.FullName),
                    EscapeCsv(u.Username),
                    EscapeCsv(u.Email),
                    EscapeCsv(u.PhoneNumber ?? ""),
                    EscapeCsv(u.Department?.Name ?? ""),
                    EscapeCsv(u.JobTitle ?? ""),
                    EscapeCsv(u.Store?.Name ?? "Trụ sở chính"),
                    EscapeCsv(u.Manager?.FullName ?? ""),
                    EscapeCsv(rolesStr),
                    EscapeCsv(u.IsActive ? "Đang hoạt động" : "Đã tạm khóa"),
                    EscapeCsv(u.HireDate.HasValue ? u.HireDate.Value.ToString("dd/MM/yyyy") : ""),
                    EscapeCsv(u.CreatedAt.ToString("dd/MM/yyyy HH:mm")),
                    EscapeCsv(u.LastLoginAt.HasValue ? u.LastLoginAt.Value.ToString("dd/MM/yyyy HH:mm") : "Chưa đăng nhập")
                ));
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static string EscapeCsv(string val)
        {
            if (string.IsNullOrEmpty(val)) return "\"\"";
            if (val.Contains(',') || val.Contains('"') || val.Contains('\n') || val.Contains('\r'))
            {
                return $"\"{val.Replace("\"", "\"\"")}\"";
            }
            return $"\"{val}\"";
        }

        #endregion
    }
}
