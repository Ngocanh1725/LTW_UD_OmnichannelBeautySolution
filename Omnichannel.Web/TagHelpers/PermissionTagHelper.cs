using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Omnichannel.Application.Interfaces.Security;
using Omnichannel.Web.Extensions;

namespace Omnichannel.Web.TagHelpers
{
    /// <summary>
    /// TagHelper kiểm tra quyền hiệu lực của người dùng hiện tại (đọc GetEffectivePermissionsAsync đúng 1 lần/request qua HttpContext.Items).
    /// Cách dùng:
    ///   &lt;a href="/Admin/Order" asp-permission="ORDER_VIEW"&gt;Quản Lý Đơn Hàng&lt;/a&gt;
    ///   &lt;a href="/Admin/Promotion" asp-any-permission="PROMOTION_MANAGE,COUPON_MANAGE,VOUCHER_MANAGE"&gt;Khuyến Mãi&lt;/a&gt;
    /// </summary>
    [HtmlTargetElement(Attributes = "asp-permission")]
    [HtmlTargetElement(Attributes = "asp-any-permission")]
    public class PermissionTagHelper : TagHelper
    {
        private const string RequestCacheKey = "__AdminEffectivePermissions";

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPermissionService _permissionService;

        public PermissionTagHelper(
            IHttpContextAccessor httpContextAccessor,
            IPermissionService permissionService)
        {
            _httpContextAccessor = httpContextAccessor;
            _permissionService = permissionService;
        }

        [HtmlAttributeName("asp-permission")]
        public string? Permission { get; set; }

        [HtmlAttributeName("asp-any-permission")]
        public string? AnyPermission { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity == null || !httpContext.User.Identity.IsAuthenticated)
            {
                output.SuppressOutput();
                return;
            }

            if (httpContext.User.IsSuperAdmin())
            {
                return;
            }

            int? userId = httpContext.User.GetUserId();
            if (!userId.HasValue || userId.Value <= 0)
            {
                output.SuppressOutput();
                return;
            }

            if (httpContext.Items[RequestCacheKey] is not HashSet<string> effectivePerms)
            {
                effectivePerms = await _permissionService.GetEffectivePermissionsAsync(userId.Value);
                httpContext.Items[RequestCacheKey] = effectivePerms;
            }

            bool isAuthorized = true;

            if (!string.IsNullOrWhiteSpace(Permission))
            {
                var required = Permission.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                isAuthorized = required.All(code => effectivePerms.Contains(code));
            }
            else if (!string.IsNullOrWhiteSpace(AnyPermission))
            {
                var candidates = AnyPermission.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                isAuthorized = candidates.Any(code => effectivePerms.Contains(code));
            }

            if (!isAuthorized)
            {
                output.SuppressOutput();
            }
        }
    }
}

