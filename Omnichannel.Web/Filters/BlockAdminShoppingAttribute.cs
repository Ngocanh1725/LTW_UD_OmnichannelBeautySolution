using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.Common;
using Omnichannel.Application.Interfaces.Security;

namespace Omnichannel.Web.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class BlockAdminShoppingAttribute : ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpContext = context.HttpContext;
            bool isAdminOrStaff = false;
            string? actorIdentifier = null;

            // 1. Kiểm tra Claims trên User hiện tại
            var user = httpContext.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                if (user.HasClaim("IsStaff", "true") ||
                    user.IsInRole("SUPER_ADMIN") ||
                    user.IsInRole("SuperAdmin") ||
                    user.IsInRole("STORE_MANAGER") ||
                    user.IsInRole("STAFF") ||
                    user.HasClaim(c => c.Type == "UserType" && c.Value != "1"))
                {
                    isAdminOrStaff = true;
                    actorIdentifier = user.Identity.Name ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                }
            }

            // 2. Kiểm tra nếu request mang phiên Admin Cookie (OmniBeauty.Admin.Session)
            if (!isAdminOrStaff && httpContext.RequestServices != null)
            {
                var authService = httpContext.RequestServices.GetService(typeof(IAuthenticationService)) as IAuthenticationService;
                if (authService != null)
                {
                    var adminAuthResult = await httpContext.AuthenticateAsync(AuthConstants.AdminCookieScheme);
                    if (adminAuthResult?.Succeeded == true && adminAuthResult.Principal?.Identity?.IsAuthenticated == true)
                    {
                        isAdminOrStaff = true;
                        actorIdentifier = adminAuthResult.Principal.Identity.Name ?? adminAuthResult.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    }
                }
            }

            // 3. Nếu là tài khoản Quản trị viên/Nhân viên -> CHẶN NGHIÊM NGẶT VỚI MÃ LỖI 403
            if (isAdminOrStaff)
            {
                const string forbiddenMessage = "Tài khoản quản trị viên không được phép tạo đơn mua hàng cá nhân.";

                // Ghi nhận Audit Log hành vi bị chặn
                try
                {
                    var auditService = httpContext.RequestServices?.GetService<IAuditLogService>();
                    if (auditService != null)
                    {
                        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                        var ua = httpContext.Request.Headers["User-Agent"].ToString();
                        _ = auditService.LogAsync(
                            action: "ADMIN_SHOPPING_BLOCKED",
                            entityType: "ShoppingBoundary",
                            entityId: httpContext.Request.Path,
                            oldValues: null,
                            newValues: $"Đã chặn hành vi mua sắm Storefront từ Quản trị viên '{actorIdentifier ?? "Unknown"}'. Endpoint: {httpContext.Request.Method} {httpContext.Request.Path}",
                            userId: null,
                            userName: actorIdentifier,
                            ipAddress: ip,
                            userAgent: ua
                        );
                    }
                }
                catch
                {
                    // Tránh gây crash luồng chặn
                }

                // Kiểm tra loại request (API/JSON hay Browser navigation)
                bool isApiOrJson = httpContext.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) ||
                                   httpContext.Request.Headers["Accept"].ToString().Contains("application/json") ||
                                   httpContext.Request.Headers["Content-Type"].ToString().Contains("application/json") ||
                                   string.Equals(httpContext.Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

                if (isApiOrJson)
                {
                    context.Result = new JsonResult(new
                    {
                        success = false,
                        message = forbiddenMessage
                    })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };
                }
                else
                {
                    context.Result = new ContentResult
                    {
                        StatusCode = StatusCodes.Status403Forbidden,
                        Content = forbiddenMessage,
                        ContentType = "text/plain; charset=utf-8"
                    };
                }

                return;
            }

            await next();
        }
    }
}
