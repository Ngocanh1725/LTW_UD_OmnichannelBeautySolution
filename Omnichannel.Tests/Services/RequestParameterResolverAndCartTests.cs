using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Omnichannel.Web.Extensions;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class RequestParameterResolverAndCartTests
    {
        [Fact]
        public void ResolveProductId_FromJsonElement_ReturnsCorrectId()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var json = "{\"productId\": 42, \"quantity\": 2}";
            using var doc = JsonDocument.Parse(json);
            var body = doc.RootElement;

            // Act
            int pId = httpContext.Request.ResolveProductId(body);

            // Assert
            pId.Should().Be(42);
        }

        [Fact]
        public void ResolveProductId_FromFormData_WhenJsonBodyNull_ReturnsCorrectId()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "application/x-www-form-urlencoded";
            httpContext.Request.Form = new FormCollection(new System.Collections.Generic.Dictionary<string, StringValues>
            {
                { "productId", "99" },
                { "quantity", "3" }
            });

            // Act
            int pId = httpContext.Request.ResolveProductId(null);
            int qty = httpContext.Request.ResolveInt(null, "quantity", 1);

            // Assert
            pId.Should().Be(99);
            qty.Should().Be(3);
        }

        [Fact]
        public async Task ReadJsonBodyAsync_WithValidJsonContent_ParsesSuccessfully()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "application/json";
            var jsonBytes = Encoding.UTF8.GetBytes("{\"productId\": 15, \"couponCode\": \"COMBO15\"}");
            httpContext.Request.Body = new MemoryStream(jsonBytes);
            httpContext.Request.ContentLength = jsonBytes.Length;

            // Act
            var body = await httpContext.Request.ReadJsonBodyAsync();

            // Assert
            body.Should().NotBeNull();
            body!.Value.GetProperty("productId").GetInt32().Should().Be(15);
            body!.Value.GetProperty("couponCode").GetString().Should().Be("COMBO15");
        }

        [Fact]
        public async Task ReadJsonBodyAsync_WithFormUrlEncoded_ReturnsNullGracefully()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "application/x-www-form-urlencoded";
            var formBytes = Encoding.UTF8.GetBytes("productId=15&quantity=2");
            httpContext.Request.Body = new MemoryStream(formBytes);
            httpContext.Request.ContentLength = formBytes.Length;

            // Act
            var body = await httpContext.Request.ReadJsonBodyAsync();

            // Assert: Should not throw and should return null so FormCollection is used
            body.Should().BeNull();
        }

        [Fact]
        public void ResolveString_WithAlternativeKeys_ResolvesFirstMatchedKey()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var json = "{\"code\": \"VIPBEAUTY\"}";
            using var doc = JsonDocument.Parse(json);
            var body = doc.RootElement;

            // Act
            string coupon = httpContext.Request.ResolveString(body, "couponCode", "CouponCode", "code");

            // Assert
            coupon.Should().Be("VIPBEAUTY");
        }
    }
}
