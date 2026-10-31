using Microsoft.AspNetCore.Authorization;

namespace Omnichannel.Infrastructure.Security
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class HasPermissionAttribute : AuthorizeAttribute
    {
        public const string PolicyPrefix = "PERM";
        public const string Separator = ":";

        public string Module { get; }
        public string Action { get; }

        public HasPermissionAttribute(string module, string action)
            : base(policy: $"{PolicyPrefix}{Separator}{module.ToUpperInvariant()}{Separator}{action.ToUpperInvariant()}")
        {
            Module = module.ToUpperInvariant();
            Action = action.ToUpperInvariant();
        }

        public HasPermissionAttribute(string permissionCode)
            : base(policy: $"{PolicyPrefix}{Separator}{permissionCode.ToUpperInvariant()}")
        {
            var underscoreIdx = permissionCode.IndexOf('_');
            if (underscoreIdx > 0)
            {
                Module = permissionCode.Substring(0, underscoreIdx).ToUpperInvariant();
                Action = permissionCode.Substring(underscoreIdx + 1).ToUpperInvariant();
            }
            else
            {
                Module = permissionCode.ToUpperInvariant();
                Action = string.Empty;
            }
        }
    }
}