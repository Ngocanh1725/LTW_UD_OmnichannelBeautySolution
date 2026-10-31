using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Omnichannel.Domain.Entities;
using Xunit;

namespace Omnichannel.Tests.Integration.Authorization
{
    [Collection("AuthTests")]
    public class PermissionOverrideTests : IClassFixture<AuthorizationTestFixture>
    {
        private readonly AuthorizationTestFixture _fixture;

        public PermissionOverrideTests(AuthorizationTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task UserPermission_GrantOverride_AllowsPermissionOtherwiseDenied()
        {
            // Arrange: Thu ngân cashier02 ban đầu KHÔNG có quyền FINANCE_LEDGER_VIEW
            var user = await _fixture.DbContext.Users.FirstAsync(u => u.Username == "cashier02");
            var perm = await _fixture.DbContext.Permissions.FirstAsync(p => p.Code == "FINANCE_LEDGER_VIEW");

            // Xóa cache perms nếu có
            var cache = _fixture.ServiceProvider.GetRequiredService<IMemoryCache>();
            cache.Remove($"UserPerms:{user.Id}");

            var initialCheck = await _fixture.CheckPermissionAsync("cashier02", "FINANCE", "LEDGER_VIEW");
            initialCheck.Should().BeFalse("Mặc định vai trò Cashier không có quyền xem sổ cái tài chính");

            // Act: Cấp quyền ghi đè (Grant Override = true) cho riêng cá nhân user này
            var userPerm = new UserPermission
            {
                UserId = user.Id,
                PermissionId = perm.Id,
                IsGranted = true
            };
            _fixture.DbContext.UserPermissions.Add(userPerm);
            await _fixture.DbContext.SaveChangesAsync();
            cache.Remove($"UserPerms:{user.Id}"); // Clear cache để nhận quyền mới

            // Assert
            var overriddenCheck = await _fixture.CheckPermissionAsync("cashier02", "FINANCE", "LEDGER_VIEW");
            overriddenCheck.Should().BeTrue("Sau khi được cá nhân hóa cấp quyền IsGranted=true, thu ngân có quyền xem sổ cái");

            // Cleanup
            _fixture.DbContext.UserPermissions.Remove(userPerm);
            await _fixture.DbContext.SaveChangesAsync();
            cache.Remove($"UserPerms:{user.Id}");
        }

        [Fact]
        public async Task UserPermission_RevokeOverride_DeniesPermissionOtherwiseGranted()
        {
            // Arrange: Cửa hàng trưởng manager_cg ban đầu CÓ quyền ORDER_EDIT
            var user = await _fixture.DbContext.Users.FirstAsync(u => u.Username == "manager_cg");
            var perm = await _fixture.DbContext.Permissions.FirstAsync(p => p.Code == "ORDER_EDIT");

            var cache = _fixture.ServiceProvider.GetRequiredService<IMemoryCache>();
            cache.Remove($"UserPerms:{user.Id}");

            var initialCheck = await _fixture.CheckPermissionAsync("manager_cg", "ORDER", "EDIT");
            initialCheck.Should().BeTrue("Mặc định Store Manager có quyền sửa đơn hàng");

            // Act: Thu hồi quyền ghi đè (Revoke Override: IsGranted = false)
            var userPerm = new UserPermission
            {
                UserId = user.Id,
                PermissionId = perm.Id,
                IsGranted = false
            };
            _fixture.DbContext.UserPermissions.Add(userPerm);
            await _fixture.DbContext.SaveChangesAsync();
            cache.Remove($"UserPerms:{user.Id}");

            // Assert
            var overriddenCheck = await _fixture.CheckPermissionAsync("manager_cg", "ORDER", "EDIT");
            overriddenCheck.Should().BeFalse("Sau khi bị ghi đè IsGranted=false, quyền sửa đơn hàng bị thu hồi thành công");

            // Cleanup
            _fixture.DbContext.UserPermissions.Remove(userPerm);
            await _fixture.DbContext.SaveChangesAsync();
            cache.Remove($"UserPerms:{user.Id}");
        }
    }
}
