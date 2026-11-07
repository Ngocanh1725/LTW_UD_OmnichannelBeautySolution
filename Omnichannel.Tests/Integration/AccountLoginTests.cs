using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Security;
using Omnichannel.Tests.Integration.Authorization;
using Xunit;

namespace Omnichannel.Tests.Integration
{
    public class AccountLoginTests : IClassFixture<AuthorizationTestFixture>
    {
        private readonly AuthorizationTestFixture _fixture;
        private readonly PasswordHasherService _passwordHasher;

        public AccountLoginTests(AuthorizationTestFixture fixture)
        {
            _fixture = fixture;
            _passwordHasher = new PasswordHasherService();
        }

        [Fact]
        public async Task Database_ContainsAll12ExpectedUsers()
        {
            var expectedUsers = new List<string>
            {
                "superadmin", "admin", "manager_hq", "manager_cg",
                "cashier01", "cashier02", "warehouse01", "warehouse02",
                "editor01", "ad1", "ad2", "ad3", "ad4",
                "sale01", "sale02", "mkt01", "hr01", "data01",
                "customer01", "customer02", "customer_vip"
            };

            var dbUsers = await _fixture.DbContext.Users.Select(u => u.Username).ToListAsync();

            dbUsers.Should().Contain(expectedUsers);
            dbUsers.Count.Should().BeGreaterThanOrEqualTo(21);
        }

        [Fact]
        public async Task Database_ContainsExactly6Roles()
        {
            var expectedRoles = new List<string>
            {
                "SuperAdmin", "UiAdmin", "DataAdmin", "HrAdmin", "SalesAdmin",
                "StoreManager", "SalesStaff", "MarketingStaff", "HrStaff", "DataStaff",
                "Cashier", "WarehouseStaff", "ContentEditor", "Customer"
            };

            var dbRoles = await _fixture.DbContext.Roles.Select(r => r.Name).ToListAsync();

            dbRoles.Should().Contain(expectedRoles);
            dbRoles.Count.Should().Be(14);
        }

        [Fact]
        public async Task Database_ContainsExpectedPermissions()
        {
            var permissionCount = await _fixture.DbContext.Permissions.CountAsync();
            permissionCount.Should().BeGreaterThanOrEqualTo(66);
        }

        [Fact]
        public async Task Seed_IsIdempotent_RunningTwiceDoesNotDuplicateData()
        {
            var roleCountBefore = await _fixture.DbContext.Roles.CountAsync();
            var deptCountBefore = await _fixture.DbContext.Departments.CountAsync();
            var staffCountBefore = await _fixture.DbContext.Users.CountAsync(u => u.UserType == 2);
            var scopeCountBefore = await _fixture.DbContext.AdminDelegationScopes.CountAsync();

            // Chạy lại seed lần 2
            await DbInitializer.EnsureDelegatedRbacAndDepartmentsSeededAsync(_fixture.DbContext);

            var roleCountAfter = await _fixture.DbContext.Roles.CountAsync();
            var deptCountAfter = await _fixture.DbContext.Departments.CountAsync();
            var staffCountAfter = await _fixture.DbContext.Users.CountAsync(u => u.UserType == 2);
            var scopeCountAfter = await _fixture.DbContext.AdminDelegationScopes.CountAsync();

            roleCountAfter.Should().Be(roleCountBefore);
            deptCountAfter.Should().Be(deptCountBefore);
            staffCountAfter.Should().Be(staffCountBefore);
            scopeCountAfter.Should().Be(scopeCountBefore);
        }

        [Theory]
        [InlineData("superadmin", "Admin@123", true)]
        [InlineData("admin", "Admin@123456", true)]
        [InlineData("ad1", "Admin@123", true)]
        [InlineData("ad2", "Admin@123", true)]
        [InlineData("ad3", "Admin@123", true)]
        [InlineData("ad4", "Admin@123", true)]
        [InlineData("sale01", "Admin@123", true)]
        [InlineData("sale02", "Admin@123", true)]
        [InlineData("mkt01", "Admin@123", true)]
        [InlineData("hr01", "Admin@123", true)]
        [InlineData("data01", "Admin@123", true)]
        [InlineData("manager_hq", "Admin@123", true)]
        [InlineData("manager_cg", "Admin@123", true)]
        [InlineData("cashier01", "Admin@123", true)]
        [InlineData("cashier02", "Admin@123", true)]
        [InlineData("warehouse01", "Admin@123", true)]
        [InlineData("warehouse02", "Admin@123", true)]
        [InlineData("editor01", "Admin@123", true)]
        [InlineData("customer01", "Admin@123", true)]
        [InlineData("customer02", "Admin@123", true)]
        [InlineData("customer_vip", "Admin@123", true)]
        [InlineData("superadmin", "WrongPassword", false)]
        [InlineData("admin", "WrongPassword", false)]
        public async Task AuthenticateUser_WithExpectedPassword_ValidatesCorrectly(string username, string password, bool expectedResult)
        {
            var user = await _fixture.DbContext.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            user.Should().NotBeNull($"Người dùng '{username}' phải tồn tại trong cơ sở dữ liệu");
            user!.IsActive.Should().BeTrue();

            bool isValid = _passwordHasher.VerifyPassword(user.PasswordHash, password);
            isValid.Should().Be(expectedResult);
        }

        [Fact]
        public async Task DepartmentAdmins_HaveLeastPrivilegeAndStrictModuleIsolation()
        {
            // ad4 (SalesAdmin): truy cập Order = OK, truy cập PermissionManagement toàn cục (PERMISSION_MANAGE) = Blocked
            (await _fixture.CheckPermissionAsync("ad4", "ORDER", "VIEW")).Should().BeTrue();
            (await _fixture.CheckPermissionAsync("ad4", "ORDER", "CONFIRM")).Should().BeTrue();
            (await _fixture.CheckPermissionAsync("ad4", "PERMISSION", "MANAGE")).Should().BeFalse();

            // ad1 (UiAdmin): truy cập UiSection/Theme = OK, truy cập Order = Blocked
            (await _fixture.CheckPermissionAsync("ad1", "UI", "SECTION_MANAGE")).Should().BeTrue();
            (await _fixture.CheckPermissionAsync("ad1", "THEME", "MANAGE")).Should().BeTrue();
            (await _fixture.CheckPermissionAsync("ad1", "CMS", "MENU_MANAGE")).Should().BeTrue();
            (await _fixture.CheckPermissionAsync("ad1", "ORDER", "VIEW")).Should().BeFalse();
        }
    }
}
