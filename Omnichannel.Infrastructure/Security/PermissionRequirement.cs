using Microsoft.AspNetCore.Authorization;

namespace Omnichannel.Infrastructure.Security
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Module { get; }
        public string Action { get; }
        public string PermissionKey => $"{Module}:{Action}";

        public PermissionRequirement(string module, string action)
        {
            Module = module.ToUpperInvariant();
            Action = action.ToUpperInvariant();
        }
    }
}