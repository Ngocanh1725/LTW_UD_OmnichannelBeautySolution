using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Security;
using Omnichannel.Tests.Helpers;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class PermissionEngineTests
    {
        [Fact]
        public async Task GetEffectivePermissions_RoleOnly_ReturnsAllRolePermissions()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var service = new PermissionService(context, memoryCache);

            var perm1 = new Permission { Code = "ORDER_VIEW", Name = "Xem đơn hàng", Module = "ORDER" };
            var perm2 = new Permission { Code = "ORDER_CREATE", Name = "Tạo đơn hàng", Module = "ORDER" };
            context.Permissions.AddRange(perm1, perm2);

            var role = new Role { Name = "Cashier", Description = "Thu ngân POS", IsActive = true };
            context.Roles.Add(role);
            await context.SaveChangesAsync();

            context.RolePermissions.AddRange(
                new RolePermission { RoleId = role.Id, PermissionId = perm1.Id },
                new RolePermission { RoleId = role.Id, PermissionId = perm2.Id }
            );

            var user = new User { Username = "cashier_test", Email = "cashier@test.com", PasswordHash = "hash" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
            await context.SaveChangesAsync();

            // Act
            var perms = await service.GetEffectivePermissionsAsync(user.Id);

            // Assert
            perms.Should().Contain("ORDER_VIEW");
            perms.Should().Contain("ORDER_CREATE");
            perms.Should().NotContain("ORDER_DELETE");
        }

        [Fact]
        public async Task GetEffectivePermissions_UserOverride_Granted_AddsExtraPermission()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var service = new PermissionService(context, memoryCache);

            var permView = new Permission { Code = "INVENTORY_VIEW", Name = "Xem kho", Module = "INVENTORY" };
            var permAdjust = new Permission { Code = "INVENTORY_ADJUST", Name = "Cân bằng kho", Module = "INVENTORY" };
            context.Permissions.AddRange(permView, permAdjust);

            var role = new Role { Name = "Staff", IsActive = true };
            context.Roles.Add(role);
            await context.SaveChangesAsync();

            context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permView.Id });

            var user = new User { Username = "staff_special", Email = "staff@test.com", PasswordHash = "hash" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
            // Đặc cách cấp thêm INVENTORY_ADJUST cho cá nhân này
            context.UserPermissions.Add(new UserPermission { UserId = user.Id, PermissionId = permAdjust.Id, IsGranted = true });
            await context.SaveChangesAsync();

            // Act
            var perms = await service.GetEffectivePermissionsAsync(user.Id);

            // Assert
            perms.Should().Contain("INVENTORY_VIEW");
            perms.Should().Contain("INVENTORY_ADJUST");
        }

        [Fact]
        public async Task GetEffectivePermissions_UserOverride_Revoked_RemovesInheritedPermission()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var service = new PermissionService(context, memoryCache);

            var permDelete = new Permission { Code = "PRODUCT_DELETE", Name = "Xóa sản phẩm", Module = "PRODUCT" };
            var permEdit = new Permission { Code = "PRODUCT_EDIT", Name = "Sửa sản phẩm", Module = "PRODUCT" };
            context.Permissions.AddRange(permDelete, permEdit);

            var role = new Role { Name = "StoreManager", IsActive = true };
            context.Roles.Add(role);
            await context.SaveChangesAsync();

            context.RolePermissions.AddRange(
                new RolePermission { RoleId = role.Id, PermissionId = permDelete.Id },
                new RolePermission { RoleId = role.Id, PermissionId = permEdit.Id }
            );

            var user = new User { Username = "manager_restricted", Email = "mgr@test.com", PasswordHash = "hash" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
            // Tước quyền PRODUCT_DELETE cụ thể của cá nhân này
            context.UserPermissions.Add(new UserPermission { UserId = user.Id, PermissionId = permDelete.Id, IsGranted = false });
            await context.SaveChangesAsync();

            // Act
            var perms = await service.GetEffectivePermissionsAsync(user.Id);

            // Assert
            perms.Should().Contain("PRODUCT_EDIT");
            perms.Should().NotContain("PRODUCT_DELETE"); // Tước quyền thành công!
        }

        [Fact]
        public async Task PermissionAuthorizationHandler_SuperAdmin_BypassesAllRequirements()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var permService = new PermissionService(context, memoryCache);
            var handler = new PermissionAuthorizationHandler(permService);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "999"),
                new Claim(ClaimTypes.Name, "superadmin"),
                new Claim(ClaimTypes.Role, "SuperAdmin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var requirement = new PermissionRequirement("PERM:SUPER_SECRET_ACTION");
            var authContext = new AuthorizationHandlerContext(new[] { requirement }, principal, null);

            // Act
            await handler.HandleAsync(authContext);

            // Assert
            authContext.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task InvalidatePermissionCache_EvictsCachedPermissions()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var service = new PermissionService(context, memoryCache);

            var user = new User { Id = 50, Username = "u50", Email = "u50@test.com", PasswordHash = "h" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Cache data
            await service.GetEffectivePermissionsAsync(50);
            memoryCache.TryGetValue("UserPerms:50", out var _).Should().BeTrue();

            // Invalidate
            await service.InvalidateUserPermissionCacheAsync(50);
            memoryCache.TryGetValue("UserPerms:50", out var _).Should().BeFalse();
        }
    }
}
