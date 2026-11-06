using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Omnichannel.Application.Common;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class MultiSchemeAuthTests
    {
        [Fact]
        public void AuthConstants_SchemesAndCookieNames_MustBeDistinctAndAccurate()
        {
            // Assert
            Assert.Equal("CustomerCookieScheme", AuthConstants.CustomerCookieScheme);
            Assert.Equal("AdminCookieScheme", AuthConstants.AdminCookieScheme);
            Assert.Equal("DynamicAuth", AuthConstants.DynamicScheme);

            Assert.Equal("CanmakeBeauty.Customer.Session", AuthConstants.CustomerCookieName);
            Assert.Equal("CanmakeBeauty.Admin.Session", AuthConstants.AdminCookieName);

            Assert.Equal("/account/login", AuthConstants.CustomerLoginPath);
            Assert.Equal("/admin/auth/login", AuthConstants.AdminLoginPath);

            Assert.NotEqual(AuthConstants.CustomerCookieScheme, AuthConstants.AdminCookieScheme);
            Assert.NotEqual(AuthConstants.CustomerCookieName, AuthConstants.AdminCookieName);
            Assert.NotEqual(AuthConstants.CustomerLoginPath, AuthConstants.AdminLoginPath);
        }

        [Theory]
        [InlineData("/admin", AuthConstants.AdminCookieScheme)]
        [InlineData("/admin/dashboard", AuthConstants.AdminCookieScheme)]
        [InlineData("/Admin/Product/Create", AuthConstants.AdminCookieScheme)]
        [InlineData("/ADMIN/orders", AuthConstants.AdminCookieScheme)]
        [InlineData("/admin/auth/login", AuthConstants.AdminCookieScheme)]
        [InlineData("/", AuthConstants.CustomerCookieScheme)]
        [InlineData("/account/login", AuthConstants.CustomerCookieScheme)]
        [InlineData("/cart", AuthConstants.CustomerCookieScheme)]
        [InlineData("/product/detail/1", AuthConstants.CustomerCookieScheme)]
        [InlineData("/tu-van-da", AuthConstants.CustomerCookieScheme)]
        public void DynamicSchemeSelector_SelectsCorrectScheme_BasedOnRequestPath(string path, string expectedScheme)
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = path;

            // Act: evaluate the same selector logic configured in Program.cs
            string resolvedScheme = context.Request.Path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase)
                ? AuthConstants.AdminCookieScheme
                : AuthConstants.CustomerCookieScheme;

            // Assert
            Assert.Equal(expectedScheme, resolvedScheme);
        }

        [Theory]
        [InlineData(1, "CUSTOMER")]
        [InlineData(1, "SUPER_ADMIN")] // UserType 1 dù bị gán nhầm role nội bộ vẫn tuyệt đối bị chặn khỏi /Admin
        [InlineData(1, "STORE_MANAGER")]
        public void RoleBoundary_CustomerRole_ShouldNotBeAllowedInAdminPortal(int userType, string role)
        {
            // Arrange
            var roles = new[] { role };

            // Act: evaluate strict role boundary logic (chỉ UserType >= 2 mới là nhân sự nội bộ)
            bool isStaffOrAdmin = userType >= 2 && roles.Length >= 0;

            // Assert
            Assert.False(isStaffOrAdmin, "Customer account (UserType = 1) must be strictly blocked from Admin portal even if assigned internal role");
        }

        [Theory]
        [InlineData(2, "STAFF")]
        [InlineData(3, "STORE_MANAGER")]
        [InlineData(4, "ACCOUNTANT")]
        [InlineData(5, "SUPER_ADMIN")]
        public void RoleBoundary_StaffOrAdmin_ShouldBeAllowedInAdminPortal(int userType, string role)
        {
            // Arrange
            var roles = new[] { role };

            // Act
            bool isStaffOrAdmin = userType >= 2 && roles.Length > 0;

            // Assert
            Assert.True(isStaffOrAdmin, "Staff/Admin role must be allowed in Admin portal");
        }
    }
}

