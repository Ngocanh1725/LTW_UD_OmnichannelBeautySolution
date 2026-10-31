using System;
using Microsoft.AspNetCore.Authorization;

namespace Omnichannel.Infrastructure.Security
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public PermissionRequirement(string policyName)
        {
            PolicyName = policyName ?? throw new ArgumentNullException(nameof(policyName));

            // Policy format: "PERM:MODULE:ACTION" or "PERM:CODE" or "MODULE_ACTION"
            var raw = policyName.StartsWith(HasPermissionAttribute.PolicyPrefix, StringComparison.OrdinalIgnoreCase)
                ? policyName.Substring(HasPermissionAttribute.PolicyPrefix.Length).TrimStart(':')
                : policyName.TrimStart(':');

            if (raw.Contains(':'))
            {
                var parts = raw.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);
                Module = parts[0].ToUpperInvariant();
                Action = parts.Length > 1 ? parts[1].ToUpperInvariant() : null;
                PermissionCode = Action != null ? $"{Module}_{Action}" : Module;
            }
            else
            {
                PermissionCode = raw.ToUpperInvariant();
                var underscoreIdx = raw.IndexOf('_');
                if (underscoreIdx > 0)
                {
                    Module = raw.Substring(0, underscoreIdx).ToUpperInvariant();
                    Action = raw.Substring(underscoreIdx + 1).ToUpperInvariant();
                }
            }
        }

        public string PolicyName { get; }
        public string PermissionCode { get; }
        public string? Module { get; }
        public string? Action { get; }
    }
}