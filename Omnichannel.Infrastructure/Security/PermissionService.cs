using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Omnichannel.Application.DTOs.Security;
using Omnichannel.Application.Interfaces.Security;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Security
{
    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

        private static readonly Dictionary<string, string[]> CompositePermissionInheritance = new(StringComparer.OrdinalIgnoreCase)
        {
            ["USER_MANAGE"] = new[]
            {
                "USER_VIEW", "USER_CREATE", "USER_EDIT", "USER_DELETE",
                "USER_RESET_PASSWORD", "USER_EXPORT", "USER_ASSIGN_ROLE", "DEPARTMENT_VIEW"
            },
            ["ROLE_MANAGE"] = new[]
            {
                "ROLE_VIEW", "ROLE_MANAGE_SCOPED"
            },
            ["PERMISSION_MANAGE"] = new[]
            {
                "PERMISSION_DELEGATE"
            },
            ["ORDER_EDIT"] = new[]
            {
                "ORDER_CONFIRM"
            },
            ["THEME_MANAGE"] = new[]
            {
                "UI_SECTION_MANAGE"
            },
            ["UI_SECTION_MANAGE"] = new[]
            {
                "THEME_MANAGE"
            }
        };

        public PermissionService(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<bool> IsCustomerOrInactiveInDbAsync(int userId)
        {
            var userState = await _context.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.IsActive, u.IsDeleted, u.UserType })
                .FirstOrDefaultAsync();

            if (userState == null)
            {
                return false;
            }

            return !userState.IsActive || userState.IsDeleted || userState.UserType < 2;
        }

        public async Task<HashSet<string>> GetEffectivePermissionsAsync(int userId)
        {
            var userState = await _context.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.IsActive, u.IsDeleted, u.UserType, u.SecurityStamp })
                .FirstOrDefaultAsync();

            // Đảm bảo User không tồn tại, bị khóa (IsActive=false), đã xóa mềm (IsDeleted=true)
            // hoặc là Khách hàng (UserType=1) thì quyền hiệu lực = RỖNG tuyệt đối
            if (userState == null || !userState.IsActive || userState.IsDeleted || userState.UserType < 2)
            {
                await InvalidateUserPermissionCacheAsync(userId);
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            var stamp = string.IsNullOrWhiteSpace(userState.SecurityStamp) ? "v0" : userState.SecurityStamp;
            var baseCacheKey = $"UserPerms:{userId}";
            var stampedCacheKey = $"UserPerms:{userId}:{stamp}";
            var lastStampKey = $"UserPermsLastStamp:{userId}";

            if (_cache.TryGetValue(stampedCacheKey, out HashSet<string>? cachedPerms) &&
                cachedPerms != null &&
                _cache.TryGetValue(baseCacheKey, out _))
            {
                return cachedPerms;
            }

            var effectivePerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Lấy toàn bộ quyền từ các Roles nội bộ đang Active gán cho User (loại trừ Role Customer)
            var rolePermissions = await _context.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == userId &&
                             ur.Role.IsActive &&
                             ur.Role.Name != "Customer" &&
                             ur.Role.Name != "CUSTOMER")
                .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Code))
                .ToListAsync();

            foreach (var code in rolePermissions)
            {
                AddNormalizedPermission(effectivePerms, code);

                if (CompositePermissionInheritance.TryGetValue(code, out var inheritedChildren))
                {
                    foreach (var childCode in inheritedChildren)
                    {
                        AddNormalizedPermission(effectivePerms, childCode);
                    }
                }
            }

            // 2. Lấy danh sách ghi đè cá nhân (User Overrides: Granted & Revoked)
            // Quy tắc: Deny (IsGranted = false) LUÔN THẮNG quyền từ Role
            var userOverrides = await _context.UserPermissions
                .AsNoTracking()
                .Where(up => up.UserId == userId)
                .Select(up => new { up.Permission.Code, up.IsGranted })
                .ToListAsync();

            foreach (var ovr in userOverrides)
            {
                if (ovr.IsGranted)
                {
                    AddNormalizedPermission(effectivePerms, ovr.Code);
                }
                else
                {
                    RemoveNormalizedPermission(effectivePerms, ovr.Code);
                }
            }

            // 3. Cache-Aside với thời hạn TTL 30 phút gắn kèm SecurityStamp
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheTtl,
                Priority = CacheItemPriority.High
            };

            _cache.Set(baseCacheKey, effectivePerms, cacheOptions);
            _cache.Set(stampedCacheKey, effectivePerms, cacheOptions);
            _cache.Set(lastStampKey, stamp, cacheOptions);

            return effectivePerms;
        }

        private static void AddNormalizedPermission(HashSet<string> set, string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return;
            var trimmed = code.Trim();
            set.Add(trimmed);
            set.Add(trimmed.Replace("_", ":"));
        }

        private static void RemoveNormalizedPermission(HashSet<string> set, string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return;
            var trimmed = code.Trim();
            set.Remove(trimmed);
            set.Remove(trimmed.Replace("_", ":"));
        }

        public async Task<bool> HasPermissionAsync(int userId, string permissionCode)
        {
            if (string.IsNullOrWhiteSpace(permissionCode)) return false;

            var permissions = await GetEffectivePermissionsAsync(userId);
            var normalized = permissionCode.Trim().ToUpperInvariant();

            return permissions.Contains(normalized) ||
                   permissions.Contains(normalized.Replace(":", "_")) ||
                   permissions.Contains(normalized.Replace("_", ":"));
        }

        public Task InvalidateUserPermissionCacheAsync(int userId)
        {
            var baseCacheKey = $"UserPerms:{userId}";
            var lastStampKey = $"UserPermsLastStamp:{userId}";

            if (_cache.TryGetValue(lastStampKey, out string? lastStamp) && !string.IsNullOrWhiteSpace(lastStamp))
            {
                _cache.Remove($"UserPerms:{userId}:{lastStamp}");
                _cache.Remove(lastStampKey);
            }

            _cache.Remove(baseCacheKey);
            return Task.CompletedTask;
        }

        public async Task InvalidateRolePermissionCacheAsync(int roleId)
        {
            var userIds = await _context.UserRoles
                .AsNoTracking()
                .Where(ur => ur.RoleId == roleId)
                .Select(ur => ur.UserId)
                .ToListAsync();

            foreach (var uid in userIds)
            {
                await InvalidateUserPermissionCacheAsync(uid);
            }
        }

        public async Task<List<UserPermissionDetailDto>> GetUserPermissionBreakdownAsync(int userId)
        {
            var allPermissions = await _context.Permissions
                .AsNoTracking()
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Code)
                .ToListAsync();

            var rolePermissionIds = await _context.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == userId &&
                             ur.Role.IsActive &&
                             ur.Role.Name != "Customer" &&
                             ur.Role.Name != "CUSTOMER")
                .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.PermissionId))
                .Distinct()
                .ToListAsync();

            var userOverridesList = await _context.UserPermissions
                .AsNoTracking()
                .Where(up => up.UserId == userId)
                .ToListAsync();

            var userOverrides = userOverridesList.ToDictionary(up => up.PermissionId, up => up);

            var grantorIds = userOverridesList
                .Where(up => up.GrantedByUserId.HasValue)
                .Select(up => up.GrantedByUserId!.Value)
                .Distinct()
                .ToList();

            var grantorNames = grantorIds.Count > 0
                ? await _context.Users
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(u => grantorIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => $"{u.FullName} (@{u.Username})")
                : new Dictionary<int, string>();

            var result = new List<UserPermissionDetailDto>();

            foreach (var perm in allPermissions)
            {
                var inherited = rolePermissionIds.Contains(perm.Id);
                var hasOverride = userOverrides.TryGetValue(perm.Id, out var ovr);
                bool? directOverride = hasOverride ? ovr!.IsGranted : null;

                bool isEffective = directOverride ?? inherited;
                string? grantedByName = null;
                if (hasOverride && ovr!.GrantedByUserId.HasValue && grantorNames.TryGetValue(ovr.GrantedByUserId.Value, out var gName))
                {
                    grantedByName = gName;
                }

                result.Add(new UserPermissionDetailDto
                {
                    PermissionId = perm.Id,
                    PermissionCode = perm.Code,
                    PermissionName = perm.Name,
                    PermissionDescription = perm.Description,
                    Module = perm.Module,
                    InheritedFromRole = inherited,
                    DirectOverride = directOverride,
                    IsEffective = isEffective,
                    GrantedByUserId = hasOverride ? ovr!.GrantedByUserId : null,
                    GrantedByName = grantedByName,
                    GrantedAt = hasOverride ? ovr!.GrantedAt : null,
                    Reason = hasOverride ? ovr!.Reason : null
                });
            }

            return result;
        }
    }
}
