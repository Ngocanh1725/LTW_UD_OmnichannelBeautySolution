using Microsoft.AspNetCore.Http;

namespace Omnichannel.Web.Extensions
{
    public static class HttpRequestExtensions
    {
        public static bool IsHtmx(this HttpRequest request)
        {
            return request.Headers.ContainsKey("HX-Request");
        }
    }
}
