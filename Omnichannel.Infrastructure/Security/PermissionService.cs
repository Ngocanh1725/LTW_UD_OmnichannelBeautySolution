using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.Interfaces.Security;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Security
{
    public class PermissionService : IPermissionService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IDistributedCache? _distributedCache;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<PermissionService> _logger;

        private const string CacheKeyPrefix = "UserPerms:";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

        public PermissionService(
            IServiceScopeFactory scopeFactory,
            IMemoryCache memoryCache,
            ILogger<PermissionService> logger,
            IServiceProvider serviceProvider)
        {
            _scopeFactory = scopeFactory;
            _memoryCache = memoryCache;
            _logger = logger;
            _distributedCache = serviceProvider.GetService<IDistributedCache>();
        }

        public async Task<HashSet<string>> GetEffectivePermissionsAsync(string userId, CancellationToken cancellationToken = default)
        {
            string cacheKey = $"{CacheKeyPrefix}{userId}";

            // 1. Kiểm tra trong DistributedCache (Redis) nếu có cấu hình
            if (_distributedCache != null)
            {
                try
                {
                    var cachedJson = await _distributedCache.GetStringAsync(cacheKey, cancellationToken);
                    if (!string.IsNullOrEmpty(cachedJson))
                    {
                        var perms = JsonSerializer.Deserialize<HashSet<string>>(cachedJson);
                        if (perms != null) return perms;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không thể đọc Redis cache cho User {UserId}. Đang kiểm tra MemoryCache.", userId);
                }
            }

            // 2. Kiểm tra trong IMemoryCache (Local Fallback)
            if (_memoryCache.TryGetValue(cacheKey, out HashSet<string>? memoryPerms) && memoryPerms != null)
            {
                return memoryPerms;
            }

            // 3. Tính toán từ CSDL (3-Tier PBAC Resolution)
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // 3.1. Lấy danh sách RoleIds của User
            var roleIds = await dbContext.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync(cancellationToken);

            // 3.2. Lấy toàn bộ quyền được cấp từ các Role
            var rolePermissions = await dbContext.RolePermissions
                .AsNoTracking()
                .Where(rp => roleIds.Contains(rp.RoleId) && rp.IsGranted)
                .Select(rp => $"{rp.Permission.ModuleCode.ToUpper()}:{rp.Permission.ActionCode.ToUpper()}")
                .ToListAsync(cancellationToken);

            var effectivePermissions = new HashSet<string>(rolePermissions, StringComparer.OrdinalIgnoreCase);

            // 3.3. Áp dụng đặc cách trực tiếp từ bảng UserPermissions (Overrides)
            var userOverrides = await dbContext.UserPermissions
                .AsNoTracking()
                .Where(up => up.UserId == userId)
                .Select(up => new
                {
                    PermissionKey = $"{up.Permission.ModuleCode.ToUpper()}:{up.Permission.ActionCode.ToUpper()}",
                    up.IsGranted
                })
                .ToListAsync(cancellationToken);

            foreach (var ov in userOverrides)
            {
                if (ov.IsGranted)
                    effectivePermissions.Add(ov.PermissionKey);
                else
                    effectivePermissions.Remove(ov.PermissionKey);
            }

            // 4. Cập nhật bộ nhớ đệm
            _memoryCache.Set(cacheKey, effectivePermissions, CacheTtl);

            if (_distributedCache != null)
            {
                try
                {
                    var serialized = JsonSerializer.Serialize(effectivePermissions);
                    var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl };
                    await _distributedCache.SetStringAsync(cacheKey, serialized, options, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi ghi DistributedCache cho User {UserId}.", userId);
                }
            }

            return effectivePermissions;
        }

        public async Task InvalidateUserPermissionCacheAsync(string userId, CancellationToken cancellationToken = default)
        {
            string cacheKey = $"{CacheKeyPrefix}{userId}";
            _memoryCache.Remove(cacheKey);

            if (_distributedCache != null)
            {
                try
                {
                    await _distributedCache.RemoveAsync(cacheKey, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi xóa DistributedCache cho User {UserId}.", userId);
                }
            }

            _logger.LogInformation("Đã Invalidate bộ nhớ đệm quyền cho User {UserId}.", userId);
        }

        public async Task<bool> HasPermissionAsync(string userId, string module, string action, CancellationToken cancellationToken = default)
        {
            var perms = await GetEffectivePermissionsAsync(userId, cancellationToken);
            string key = $"{module.Trim().ToUpperInvariant()}:{action.Trim().ToUpperInvariant()}";
            return perms.Contains(key);
        }
    }
}
