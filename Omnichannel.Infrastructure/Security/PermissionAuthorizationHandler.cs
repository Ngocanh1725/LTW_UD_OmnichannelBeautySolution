using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Omnichannel.Application.Interfaces.Security;

namespace Omnichannel.Infrastructure.Security
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IPermissionService _permissionService;

        public PermissionAuthorizationHandler(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            if (context.User?.Identity == null || !context.User.Identity.IsAuthenticated)
            {
                return;
            }

            // =========================================================================
            // TẦNG 1: KIỂM TRA QUYỀN SUPERADMIN TỐI CAO (GOD MODE BYPASS)
            // =========================================================================
            if (context.User.IsInRole("SuperAdmin") ||
                context.User.IsInRole("SUPER_ADMIN") ||
                context.User.HasClaim(ClaimTypes.Role, "SuperAdmin"))
            {
                context.Succeed(requirement);
                return;
            }

            // =========================================================================
            // TẦNG 2: LẤY USER ID VÀ ĐỐI SOÁT TẬP QUYỀN HỮU HIỆU (EFFECTIVE PERMISSIONS)
            // =========================================================================
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return;
            }

            var effectivePermissions = await _permissionService.GetEffectivePermissionsAsync(userId);

            // =========================================================================
            // TẦNG 3: SO KHỚP ĐA ĐỊNH DẠNG (PERM:MODULE:ACTION HOẶC MODULE_ACTION)
            // =========================================================================
            var normalizedCode = requirement.PermissionCode;
            var alternativeCode = requirement.Module != null && requirement.Action != null
                ? $"{requirement.Module}_{requirement.Action}"
                : normalizedCode;

            if (effectivePermissions.Contains(normalizedCode) ||
                effectivePermissions.Contains(alternativeCode) ||
                effectivePermissions.Contains(requirement.PolicyName))
            {
                context.Succeed(requirement);
            }
        }
    }
}