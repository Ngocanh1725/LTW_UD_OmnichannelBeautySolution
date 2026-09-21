using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Omnichannel.Infrastructure.Security
{
    public class DynamicPermissionPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        private readonly AuthorizationOptions _options;

        public DynamicPermissionPolicyProvider(IOptions<AuthorizationOptions> options)
            : base(options)
        {
            _options = options.Value;
        }

        public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // Kiểm tra xem policy đã được đăng ký tĩnh chưa
            var policy = await base.GetPolicyAsync(policyName);
            if (policy != null)
            {
                return policy;
            }

            // Nếu policy bắt đầu với tiền tố động "PERM:"
            if (policyName.StartsWith(HasPermissionAttribute.PolicyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var requirement = new PermissionRequirement(policyName);
                var dynamicPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(requirement)
                    .Build();

                // Lưu tạm vào cache của options để tái sử dụng
                _options.AddPolicy(policyName, dynamicPolicy);
                return dynamicPolicy;
            }

            return null;
        }
    }
}