using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Services;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class GhnShippingServiceTests
    {
        private readonly IConfiguration _config;

        public GhnShippingServiceTests()
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "GhnConfig:Token", "" },
                { "GhnConfig:ShopId", "" }
            };

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        [Theory]
        [InlineData("1442", "1A01", 500, 22000)] // HCM Quận 1 <= 500g
        [InlineData("1442", "1A01", 1200, 32000)] // HCM Quận 1: 1200g (vượt 700g -> +2 bậc = +10,000) -> 32,000
        [InlineData("1482", "1A02", 500, 25000)] // Hà Nội Hoàn Kiếm <= 500g
        [InlineData("1600", "1A01", 500, 32000)] // Tỉnh khác <= 500g
        public async Task CalculateShippingFeeAsync_WeightTiers_CalculatesProperTierRate(
            string districtId, string wardCode, int weightGram, decimal expectedFee)
        {
            // Arrange
            using var httpClient = new HttpClient();
            var service = new GhnShippingService(httpClient, _config, NullLogger<GhnShippingService>.Instance);

            // Act
            decimal fee = await service.CalculateShippingFeeAsync(districtId, wardCode, weightGram);

            // Assert
            fee.Should().Be(expectedFee);
        }

        [Fact]
        public async Task CreateShippingOrderAsync_ValidOrder_ReturnsTrackingCodeAndPrintUrl()
        {
            // Arrange
            using var httpClient = new HttpClient();
            var service = new GhnShippingService(httpClient, _config, NullLogger<GhnShippingService>.Instance);

            var order = new Order
            {
                OrderCode = "ORD-GHN-TEST-01",
                CustomerName = "Trần Thị B",
                CustomerPhone = "0912345678",
                ShippingAddress = "123 Lê Lợi, Phường Bến Nghé, Quận 1, TP.HCM"
            };

            // Act
            var (success, trackingCode, printUrl, message) = await service.CreateShippingOrderAsync(order, 500);

            // Assert
            success.Should().BeTrue();
            trackingCode.Should().StartWith("GHN");
            printUrl.Should().NotBeNullOrEmpty();
            printUrl.Should().Contain(trackingCode);
            message.Should().Contain("thành công");
        }
    }
}
