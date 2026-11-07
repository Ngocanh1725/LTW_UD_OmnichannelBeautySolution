namespace Omnichannel.Application.Common
{
    public static class AuthConstants
    {
        public const string CustomerCookieScheme = "CustomerCookieScheme";
        public const string AdminCookieScheme = "AdminCookieScheme";
        public const string DynamicScheme = "DynamicAuth";

        public const string CustomerCookieName = "CanmakeBeauty.Customer.Session";
        public const string AdminCookieName = "CanmakeBeauty.Admin.Session";

        public const string CustomerLoginPath = "/account/login";
        public const string CustomerAccessDeniedPath = "/account/access-denied";
        public const string CustomerLogoutPath = "/account/logout";

        public const string AdminLoginPath = "/admin/auth/login";
        public const string AdminAccessDeniedPath = "/admin/auth/access-denied";
        public const string AdminLogoutPath = "/admin/auth/logout";
    }
}

