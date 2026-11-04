using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Omnichannel.Domain.Entities;
using Omnichannel.Web.Filters;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class RoleBoundaryAndAuditInterceptorTests
    {
        [Fact]
        public async Task BlockAdminShopping_WhenUserHasStaffClaim_Returns403Forbidden()
        {
            // Arrange
            var filter = new BlockAdminShoppingAttribute();
            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "admin"),
                new Claim("IsStaff", "true"),
                new Claim(ClaimTypes.Role, "SUPER_ADMIN")
            };
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "AdminCookieScheme"));

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var executingContext = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object?>(),
                new object());

            bool nextCalled = false;
            ActionExecutionDelegate next = () =>
            {
                nextCalled = true;
                return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), new object()));
            };

            // Act
            await filter.OnActionExecutionAsync(executingContext, next);

            // Assert
            Assert.False(nextCalled, "Action execution must be blocked for Admin users");
            Assert.NotNull(executingContext.Result);

            if (executingContext.Result is JsonResult jsonResult)
            {
                Assert.Equal(403, jsonResult.StatusCode);
                dynamic val = jsonResult.Value!;
                string message = val.message;
                Assert.Contains("Tài khoản quản trị viên không được phép tạo đơn mua hàng cá nhân", message);
            }
            else if (executingContext.Result is ContentResult contentResult)
            {
                Assert.Equal(403, contentResult.StatusCode);
                Assert.Contains("Tài khoản quản trị viên không được phép tạo đơn mua hàng cá nhân", contentResult.Content);
            }
        }

        [Fact]
        public async Task BlockAdminShopping_WhenUserIsCustomer_AllowsExecution()
        {
            // Arrange
            var filter = new BlockAdminShoppingAttribute();
            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "100"),
                new Claim(ClaimTypes.Name, "customer_test"),
                new Claim(ClaimTypes.Role, "CUSTOMER"),
                new Claim("UserType", "1")
            };
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "CustomerCookieScheme"));

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var executingContext = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object?>(),
                new object());

            bool nextCalled = false;
            ActionExecutionDelegate next = () =>
            {
                nextCalled = true;
                return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), new object()));
            };

            // Act
            await filter.OnActionExecutionAsync(executingContext, next);

            // Assert
            Assert.True(nextCalled, "Customer must be allowed to perform shopping actions");
            Assert.Null(executingContext.Result);
        }

        [Fact]
        public void AuditLog_EntityFields_SupportAuditTrailStructure()
        {
            // Arrange
            var log = new AuditLog
            {
                UserId = 1,
                UserName = "admin",
                StaffCode = "STAFF001",
                Action = "UPDATE",
                EntityType = "ThemeConfig",
                EntityName = "ThemeConfig",
                EntityId = "1",
                RecordId = "1",
                OldValues = "{\"PrimaryColor\":\"#B76E79\"}",
                OldValuesJson = "{\"PrimaryColor\":\"#B76E79\"}",
                NewValues = "{\"PrimaryColor\":\"#6366F1\"}",
                NewValuesJson = "{\"PrimaryColor\":\"#6366F1\"}",
                IpAddress = "127.0.0.1",
                CreatedAt = DateTime.UtcNow
            };

            // Assert
            Assert.Equal("STAFF001", log.StaffCode);
            Assert.Equal("UPDATE", log.Action);
            Assert.Equal("ThemeConfig", log.EntityName);
            Assert.Contains("#B76E79", log.OldValuesJson);
            Assert.Contains("#6366F1", log.NewValuesJson);
        }
    }
}

