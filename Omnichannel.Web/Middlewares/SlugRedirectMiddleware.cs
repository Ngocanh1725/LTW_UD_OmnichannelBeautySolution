using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.Interfaces.Cms;

namespace Omnichannel.Web.Middlewares
{
    /// <summary>
    /// Middleware kiểm tra URL yêu cầu và thực hiện HTTP 301 Redirect nếu khớp với OldSlug đã thay đổi
    /// </summary>
    public class SlugRedirectMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SlugRedirectMiddleware> _logger;

        private static readonly string[] ExcludedPrefixes = new[]
        {
            "/admin",
            "/api",
            "/lib",
            "/css",
            "/js",
            "/images",
            "/uploads",
            "/assets",
            "/_framework",
            "/hubs"
        };

        private static readonly string[] StaticExtensions = new[]
        {
            ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".ico", ".webp", ".woff", ".woff2", ".ttf", ".map"
        };

        public SlugRedirectMiddleware(RequestDelegate _next, ILogger<SlugRedirectMiddleware> logger)
        {
            this._next = _next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IMenuEngineService menuService)
        {
            // Chỉ kiểm tra phương thức GET hoặc HEAD
            if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
            {
                await _next(context);
                return;
            }

            var path = context.Request.Path.Value;
            if (string.IsNullOrEmpty(path) || path == "/")
            {
                await _next(context);
                return;
            }

            // Bỏ qua các đường dẫn hệ thống và tệp tĩnh
            var lowerPath = path.ToLowerInvariant();
            if (ExcludedPrefixes.Any(p => lowerPath.StartsWith(p)))
            {
                await _next(context);
                return;
            }

            var ext = Path.GetExtension(lowerPath);
            if (!string.IsNullOrEmpty(ext) && StaticExtensions.Contains(ext))
            {
                await _next(context);
                return;
            }

            // Trích xuất slug từ đường dẫn (ví dụ: "/category/son-romand-cu" -> "son-romand-cu")
            var segments = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            var slug = segments.Length > 0 ? segments[^1] : string.Empty;

            if (!string.IsNullOrEmpty(slug))
            {
                // 1. Kiểm tra theo slug đơn lẻ
                var redirect = await menuService.GetRedirectAsync(slug, context.RequestAborted);

                // 2. Nếu không có, kiểm tra theo toàn bộ relative path (ví dụ: "category/son-romand-cu")
                if (redirect == null && segments.Length > 1)
                {
                    redirect = await menuService.GetRedirectAsync(string.Join('/', segments), context.RequestAborted);
                }

                if (redirect != null && !string.Equals(slug, redirect.NewSlug, StringComparison.OrdinalIgnoreCase))
                {
                    string targetUrl;
                    if (redirect.NewSlug.StartsWith("/") || redirect.NewSlug.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || redirect.NewSlug.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        targetUrl = redirect.NewSlug;
                    }
                    else
                    {
                        targetUrl = redirect.EntityType.ToUpperInvariant() switch
                        {
                            "CATEGORY" => $"/category/{redirect.NewSlug}",
                            "PRODUCT" => $"/product/{redirect.NewSlug}",
                            "POST" => $"/blog/{redirect.NewSlug}",
                            "STATIC_PAGE" => $"/page/{redirect.NewSlug}",
                            _ => $"/{redirect.NewSlug}"
                        };
                    }

                    if (context.Request.QueryString.HasValue)
                    {
                        targetUrl += context.Request.QueryString.Value;
                    }

                    _logger.LogInformation("HTTP 301 Permanent Redirect: '{OldPath}' -> '{NewPath}' (EntityType: {EntityType})",
                        path, targetUrl, redirect.EntityType);

                    context.Response.Redirect(targetUrl, permanent: true);
                    return;
                }
            }

            await _next(context);
        }
    }
}
