using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Web.Filters
{
    /// <summary>
    /// Guard bảo vệ toàn bộ phân hệ /Admin:
    /// Chặn tuyệt đối tài khoản Khách hàng (UserType = 1), tài khoản bị khóa (IsActive = false)
    /// hoặc đã xóa mềm (IsDeleted = true) truy cập /Admin kể cả khi bị gán nhầm Role nội bộ.
    /// </summary>
    public class AdminAreaBoundaryFilter : IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (context == null) return;

            bool isAdminArea =
                (context.RouteData.Values.TryGetValue("area", out var areaObj) &&
                 string.Equals(areaObj?.ToString(), "Admin", StringComparison.OrdinalIgnoreCase)) ||
                context.HttpContext.Request.Path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase);

            if (!isAdminArea)
            {
                return;
            }

            // Bỏ qua các endpoint AllowAnonymous (như /admin/auth/login, /admin/auth/access-denied)
            if (context.ActionDescriptor.EndpointMetadata.Any(m => m is IAllowAnonymous))
            {
                return;
            }

            var principal = context.HttpContext.User;
            if (principal?.Identity == null || !principal.Identity.IsAuthenticated)
            {
                return;
            }

            // 1. Kiểm tra trực tiếp từ Claim UserType
            var userTypeClaim = principal.FindFirst("UserType")?.Value;
            if (userTypeClaim == "1")
            {
                DenyAdminAccess(context, "Tài khoản khách hàng (UserType = 1) tuyệt đối không được phép truy cập khu vực Quản trị (/Admin).");
                return;
            }

            // 2. Kiểm tra đối chiếu Database (nếu có DbContext trong DI) để chặn tức thì kể cả khi bị gán Role nội bộ nhầm
            var userIdStr = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
            {
                var dbContext = context.HttpContext.RequestServices?.GetService<ApplicationDbContext>();
                if (dbContext != null)
                {
                    var dbUser = await dbContext.Users
                        .IgnoreQueryFilters()
                        .AsNoTracking()
                        .Where(u => u.Id == userId)
                        .Select(u => new { u.UserType, u.IsActive, u.IsDeleted })
                        .FirstOrDefaultAsync();

                    if (dbUser != null && (dbUser.UserType < 2 || !dbUser.IsActive || dbUser.IsDeleted))
                    {
                        DenyAdminAccess(
                            context,
                            dbUser.UserType < 2
                                ? "Tài khoản khách hàng (UserType = 1) không có quyền truy cập khu vực Quản trị (/Admin)."
                                : "Tài khoản nhân sự đã bị khóa hoặc thu hồi quyền truy cập.");
                    }
                }
            }
        }

        private static void DenyAdminAccess(AuthorizationFilterContext context, string message)
        {
            var accept = context.HttpContext.Request.Headers["Accept"].ToString();
            var xRequestedWith = context.HttpContext.Request.Headers["X-Requested-With"].ToString();

            if (xRequestedWith == "XMLHttpRequest" || accept.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new JsonResult(new { success = false, message })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            context.Result = new ContentResult
            {
                StatusCode = StatusCodes.Status403Forbidden,
                ContentType = "text/plain; charset=utf-8",
                Content = message
            };
        }
    }
}

