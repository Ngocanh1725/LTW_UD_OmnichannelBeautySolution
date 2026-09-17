using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.Interfaces.Security;

namespace Omnichannel.Infrastructure.Security
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IPermissionService _permissionService;
        private readonly ILogger<PermissionAuthorizationHandler> _logger;

        public PermissionAuthorizationHandler(
            IPermissionService permissionService,
            ILogger<PermissionAuthorizationHandler> logger)
        {
            _permissionService = permissionService;
            _logger = logger;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            if (context.User.Identity?.IsAuthenticated != true)
                return;

            // Tài khoản có vai trò SUPER_ADMIN được bỏ qua kiểm tra và cấp quyền ngay
            if (context.User.IsInRole("SUPER_ADMIN"))
            {
                context.Succeed(requirement);
                return;
            }

            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return;

            var effectivePermissions = await _permissionService.GetEffectivePermissionsAsync(userIdClaim);

            if (effectivePermissions.Contains(requirement.PermissionKey))
            {
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("Truy cập bị từ chối: User {UserId} không có quyền yêu cầu {RequiredKey}.",
                    userIdClaim, requirement.PermissionKey);
            }
        }
    }
}
