using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
                "editor01", "customer01", "customer02", "customer_vip"
            };

            var dbUsers = await _fixture.DbContext.Users.Select(u => u.Username).ToListAsync();

            dbUsers.Should().Contain(expectedUsers);
            dbUsers.Count.Should().BeGreaterThanOrEqualTo(12);
        }

        [Fact]
        public async Task Database_ContainsExactly6Roles()
        {
            var expectedRoles = new List<string>
            {
                "SuperAdmin", "StoreManager", "Cashier", "WarehouseStaff", "ContentEditor", "Customer"
            };

            var dbRoles = await _fixture.DbContext.Roles.Select(r => r.Name).ToListAsync();

            dbRoles.Should().Contain(expectedRoles);
        }

        [Fact]
        public async Task Database_ContainsExpectedPermissions()
        {
            var permissionCount = await _fixture.DbContext.Permissions.CountAsync();
            permissionCount.Should().BeGreaterThanOrEqualTo(66);
        }

        [Theory]
        [InlineData("superadmin", "Admin@123", true)]
        [InlineData("admin", "Admin@123456", true)]
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
    }
}
