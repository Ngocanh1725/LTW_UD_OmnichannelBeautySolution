using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Omnichannel.Web.Extensions
{
    /// <summary>
    /// Helper tiện ích dùng chung để trích xuất tham số an toàn từ cả 3 nguồn:
    /// JSON Body, Multipart Form Data, và URL Query String.
    /// Giúp loại bỏ hoàn toàn các hàm bóc tách tham số trùng lặp trong CartController và CheckoutController.
    /// </summary>
    public static class RequestParameterResolver
    {
        /// <summary>
        /// Trích xuất chuỗi tham số theo danh sách khóa ưu tiên (case-insensitive)
        /// </summary>
        public static string ResolveString(this HttpRequest request, JsonElement? body, params string[] paramKeys)
        {
            if (paramKeys == null || paramKeys.Length == 0) return string.Empty;

            // 1. Kiểm tra JSON Body
            if (body.HasValue && body.Value.ValueKind != JsonValueKind.Null && body.Value.ValueKind != JsonValueKind.Undefined)
            {
                var el = body.Value;
                if (el.ValueKind == JsonValueKind.String)
                {
                    return el.GetString() ?? string.Empty;
                }

                if (el.ValueKind == JsonValueKind.Object)
                {
                    foreach (var key in paramKeys)
                    {
                        // Thử key nguyên bản và lowercase/camelCase
                        if (el.TryGetProperty(key, out var prop) ||
                            el.TryGetProperty(char.ToLowerInvariant(key[0]) + key.Substring(1), out prop) ||
                            el.TryGetProperty(char.ToUpperInvariant(key[0]) + key.Substring(1), out prop))
                        {
                            if (prop.ValueKind == JsonValueKind.String) return prop.GetString() ?? string.Empty;
                            if (prop.ValueKind == JsonValueKind.Number) return prop.ToString();
                        }
                    }
                }
            }

            // 2. Kiểm tra Form Data
            if (request.HasFormContentType)
            {
                foreach (var key in paramKeys)
                {
                    if (request.Form.TryGetValue(key, out var formVal) && !string.IsNullOrWhiteSpace(formVal))
                    {
                        return formVal.ToString();
                    }
                }
            }

            // 3. Kiểm tra Query String
            foreach (var key in paramKeys)
            {
                if (request.Query.TryGetValue(key, out var queryVal) && !string.IsNullOrWhiteSpace(queryVal))
                {
                    return queryVal.ToString();
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Trích xuất mã sản phẩm ProductId từ JSON Body (Number/Object), Form Data hoặc Query
        /// </summary>
        public static int ResolveProductId(this HttpRequest request, JsonElement? body)
        {
            if (body.HasValue)
            {
                var b = body.Value;
                if (b.ValueKind == JsonValueKind.Number && b.TryGetInt32(out var num)) return num;
                if (b.ValueKind == JsonValueKind.String && int.TryParse(b.GetString(), out var ps)) return ps;
                if (b.ValueKind == JsonValueKind.Object)
                {
                    if (b.TryGetProperty("productId", out var p1) || b.TryGetProperty("ProductId", out p1))
                    {
                        if (p1.ValueKind == JsonValueKind.Number && p1.TryGetInt32(out var n)) return n;
                        if (p1.ValueKind == JsonValueKind.String && int.TryParse(p1.GetString(), out var s)) return s;
                    }
                    if (b.TryGetProperty("id", out var p2) || b.TryGetProperty("Id", out p2))
                    {
                        if (p2.ValueKind == JsonValueKind.Number && p2.TryGetInt32(out var n2)) return n2;
                        if (p2.ValueKind == JsonValueKind.String && int.TryParse(p2.GetString(), out var s2)) return s2;
                    }
                }
            }

            if (request.HasFormContentType)
            {
                if (request.Form.TryGetValue("productId", out var v1) && int.TryParse(v1, out var id1)) return id1;
                if (request.Form.TryGetValue("ProductId", out var v2) && int.TryParse(v2, out var id2)) return id2;
                if (request.Form.TryGetValue("id", out var v3) && int.TryParse(v3, out var id3)) return id3;
                if (request.Form.TryGetValue("Id", out var v4) && int.TryParse(v4, out var id4)) return id4;
            }

            if (request.Query.TryGetValue("productId", out var q1) && int.TryParse(q1, out var qid1)) return qid1;
            if (request.Query.TryGetValue("id", out var q2) && int.TryParse(q2, out var qid2)) return qid2;

            return 0;
        }

        /// <summary>
        /// Trích xuất số nguyên an toàn theo tên trường với giá trị mặc định
        /// </summary>
        public static int ResolveInt(this HttpRequest request, JsonElement? body, string paramKey, int defaultValue = 0)
        {
            if (body.HasValue && body.Value.ValueKind == JsonValueKind.Object)
            {
                var b = body.Value;
                if (b.TryGetProperty(paramKey, out var p) || 
                    b.TryGetProperty(char.ToLowerInvariant(paramKey[0]) + paramKey[1..], out p) ||
                    b.TryGetProperty(char.ToUpperInvariant(paramKey[0]) + paramKey[1..], out p))
                {
                    if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var num)) return num;
                    if (p.ValueKind == JsonValueKind.String && int.TryParse(p.GetString(), out var s)) return s;
                }
            }

            if (request.HasFormContentType)
            {
                if (request.Form.TryGetValue(paramKey, out var v1) && int.TryParse(v1, out var n1)) return n1;
                string cap = char.ToUpperInvariant(paramKey[0]) + paramKey[1..];
                if (request.Form.TryGetValue(cap, out var v2) && int.TryParse(v2, out var n2)) return n2;
                string lower = char.ToLowerInvariant(paramKey[0]) + paramKey[1..];
                if (request.Form.TryGetValue(lower, out var v3) && int.TryParse(v3, out var n3)) return n3;
            }

            if (request.Query.TryGetValue(paramKey, out var q1) && int.TryParse(q1, out var qn1)) return qn1;

            return defaultValue;
        }

        /// <summary>
        /// Trích xuất số nguyên an toàn theo danh sách khóa ưu tiên
        /// </summary>
        public static int? ResolveInt(this HttpRequest request, JsonElement? body, int? defaultValue, params string[] paramKeys)
        {
            var str = request.ResolveString(body, paramKeys);
            if (int.TryParse(str, out int val))
            {
                return val;
            }
            return defaultValue;
        }

        /// <summary>
        /// Trích xuất boolean an toàn
        /// </summary>
        public static bool ResolveBool(this HttpRequest request, JsonElement? body, bool defaultValue, params string[] paramKeys)
        {
            var str = request.ResolveString(body, paramKeys);
            if (bool.TryParse(str, out bool val))
            {
                return val;
            }
            if (string.Equals(str, "1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(str, "yes", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return defaultValue;
        }

        /// <summary>
        /// Đọc và phân tích JSON Body bất đồng bộ một cách an toàn.
        /// Trả về null nếu Content-Type không phải JSON hoặc stream rỗng/hỏng.
        /// </summary>
        public static async Task<JsonElement?> ReadJsonBodyAsync(this HttpRequest request)
        {
            if (request.ContentType != null && request.ContentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    if (request.ContentLength == 0 || request.Body == null) return null;
                    using var doc = await JsonDocument.ParseAsync(request.Body);
                    return doc.RootElement.Clone();
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }
    }
}
