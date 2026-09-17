using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Omnichannel.Infrastructure.Security
{
    public class DynamicPermissionPolicyProvider : IAuthorizationPolicyProvider
    {
        public DefaultAuthorizationPolicyProvider FallbackPolicyProvider { get; }

        public DynamicPermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            FallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
        }

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (policyName.StartsWith(HasPermissionAttribute.PolicyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var tokens = policyName.Split(HasPermissionAttribute.Separator);
                if (tokens.Length == 3)
                {
                    string module = tokens[1];
                    string action = tokens[2];

                    var policy = new AuthorizationPolicyBuilder();
                    policy.RequireAuthenticatedUser();
                    policy.AddRequirements(new PermissionRequirement(module, action));
                    return Task.FromResult<AuthorizationPolicy?>(policy.Build());
                }
            }

            return FallbackPolicyProvider.GetPolicyAsync(policyName);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
            FallbackPolicyProvider.GetDefaultPolicyAsync();

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
            FallbackPolicyProvider.GetFallbackPolicyAsync();
    }
}
