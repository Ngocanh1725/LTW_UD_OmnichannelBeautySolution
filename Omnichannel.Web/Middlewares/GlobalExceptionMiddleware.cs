using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Omnichannel.Web.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var traceId = context.TraceIdentifier;
                var path = context.Request.Path;
                var method = context.Request.Method;
                var user = context.User?.Identity?.Name ?? "Anonymous";
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

                _logger.LogError(ex, 
                    "[UnhandledException] Lỗi hệ thống chưa bắt tại {Method} {Path} | User: {User} | IP: {IP} | TraceId: {TraceId}",
                    method, path, user, ip, traceId);

                if (IsApiOrAjaxRequest(context.Request))
                {
                    await HandleApiExceptionAsync(context, ex, traceId);
                }
                else
                {
                    // Chuyển tiếp cho ExceptionHandler xử lý View 500
                    throw;
                }
            }
        }

        private static bool IsApiOrAjaxRequest(HttpRequest request)
        {
            // 1. Tuyến đường API chuyên biệt
            if (request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
                return true;

            // 2. Yêu cầu từ HTMX (Header HX-Request)
            if (request.Headers.ContainsKey("HX-Request"))
                return true;

            // 3. Yêu cầu AJAX truyền thống (jQuery, Fetch có X-Requested-With)
            var xRequestedWith = request.Headers["X-Requested-With"].ToString();
            if (string.Equals(xRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
                return true;

            // 4. Header Accept mong muốn JSON
            var acceptHeader = request.Headers["Accept"].ToString();
            if (acceptHeader.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                return true;

            // 5. Payload gửi lên là JSON
            if (request.ContentType != null && request.ContentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private static async Task HandleApiExceptionAsync(HttpContext context, Exception exception, string traceId)
        {
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new
            {
                success = false,
                status = (int)HttpStatusCode.InternalServerError,
                title = "Lỗi máy chủ nội bộ (Internal Server Error)",
                message = "Hệ thống đang bận hoặc gặp sự cố xử lý. Vui lòng thử lại sau giây lát.",
                traceId = traceId,
                timestamp = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            await context.Response.WriteAsync(json);
        }
    }
}
