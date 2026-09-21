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

        public PermissionService(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<HashSet<string>> GetEffectivePermissionsAsync(int userId)
        {
            var cacheKey = $"UserPerms:{userId}";

            if (_cache.TryGetValue(cacheKey, out HashSet<string>? cachedPerms) && cachedPerms != null)
            {
                return cachedPerms;
            }

            var effectivePerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Lấy toàn bộ quyền từ các Roles đang Active gán cho User
            var rolePermissions = await _context.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == userId && ur.Role.IsActive)
                .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Code))
                .ToListAsync();

            foreach (var code in rolePermissions)
            {
                effectivePerms.Add(code);
                effectivePerms.Add(code.Replace("_", ":"));
            }

            // 2. Lấy danh sách ghi đè cá nhân (User Overrides: Granted & Revoked)
            var userOverrides = await _context.UserPermissions
                .AsNoTracking()
                .Where(up => up.UserId == userId)
                .Select(up => new { up.Permission.Code, up.IsGranted })
                .ToListAsync();

            foreach (var ovr in userOverrides)
            {
                var codeNorm = ovr.Code;
                var codeAlt = ovr.Code.Replace("_", ":");

                if (ovr.IsGranted)
                {
                    // Granted Override -> Cộng thêm quyền riêng biệt
                    effectivePerms.Add(codeNorm);
                    effectivePerms.Add(codeAlt);
                }
                else
                {
                    // Revoked Override -> Tước bỏ quyền kể cả khi Role có sở hữu
                    effectivePerms.Remove(codeNorm);
                    effectivePerms.Remove(codeAlt);
                }
            }

            // 3. Cache-Aside với thời hạn TTL 30 phút
            _cache.Set(cacheKey, effectivePerms, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheTtl,
                Priority = CacheItemPriority.High
            });

            return effectivePerms;
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
            var cacheKey = $"UserPerms:{userId}";
            _cache.Remove(cacheKey);
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
                _cache.Remove($"UserPerms:{uid}");
            }
        }

        public async Task<List<UserPermissionDetailDto>> GetUserPermissionBreakdownAsync(int userId)
        {
            var allPermissions = await _context.Permissions.AsNoTracking().ToListAsync();

            var rolePermissionIds = await _context.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == userId && ur.Role.IsActive)
                .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.PermissionId))
                .Distinct()
                .ToListAsync();

            var userOverrides = await _context.UserPermissions
                .AsNoTracking()
                .Where(up => up.UserId == userId)
                .ToDictionaryAsync(up => up.PermissionId, up => up.IsGranted);

            var result = new List<UserPermissionDetailDto>();

            foreach (var perm in allPermissions)
            {
                var inherited = rolePermissionIds.Contains(perm.Id);
                bool? directOverride = userOverrides.ContainsKey(perm.Id) ? userOverrides[perm.Id] : null;

                bool isEffective;
                if (directOverride.HasValue)
                {
                    isEffective = directOverride.Value;
                }
                else
                {
                    isEffective = inherited;
                }

                result.Add(new UserPermissionDetailDto
                {
                    PermissionId = perm.Id,
                    PermissionCode = perm.Code,
                    PermissionName = perm.Name,
                    Module = perm.Module,
                    InheritedFromRole = inherited,
                    DirectOverride = directOverride,
                    IsEffective = isEffective
                });
            }

            return result;
        }
    }
}