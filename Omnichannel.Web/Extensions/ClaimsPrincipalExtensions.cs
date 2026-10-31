using System;
using System.Security.Claims;

namespace Omnichannel.Web.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static bool IsSuperAdmin(this ClaimsPrincipal user)
        {
            if (user?.Identity == null || !user.Identity.IsAuthenticated)
            {
                return false;
            }

            return user.IsInRole("SuperAdmin") ||
                   user.IsInRole("SUPER_ADMIN") ||
                   string.Equals(user.Identity.Name, "superadmin", StringComparison.OrdinalIgnoreCase);
        }

        public static int? GetUserId(this ClaimsPrincipal user)
        {
            var claimVal = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claimVal, out var uid))
            {
                return uid;
            }
            return null;
        }

        public static int? GetStoreId(this ClaimsPrincipal user)
        {
            var claimVal = user.FindFirst("StoreId")?.Value;
            if (int.TryParse(claimVal, out var sid))
            {
                return sid;
            }
            return null;
        }
    }
}
