using System;
using Microsoft.AspNetCore.Authorization;

namespace Omnichannel.Infrastructure.Security
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public PermissionRequirement(string policyName)
        {
            PolicyName = policyName ?? throw new ArgumentNullException(nameof(policyName));

            // Policy format: "PERM:MODULE:ACTION" or "PERM:CODE"
            var raw = policyName.StartsWith(HasPermissionAttribute.PolicyPrefix, StringComparison.OrdinalIgnoreCase)
                ? policyName.Substring(HasPermissionAttribute.PolicyPrefix.Length)
                : policyName;

            var parts = raw.Split(new[] { ':', '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                Module = parts[0].ToUpperInvariant();
                Action = parts[1].ToUpperInvariant();
                PermissionCode = $"{Module}_{Action}";
            }
            else
            {
                PermissionCode = raw.ToUpperInvariant();
            }
        }

        public string PolicyName { get; }
        public string PermissionCode { get; }
        public string? Module { get; }
        public string? Action { get; }
    }
}