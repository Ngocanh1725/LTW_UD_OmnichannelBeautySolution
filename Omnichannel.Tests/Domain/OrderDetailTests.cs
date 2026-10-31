using FluentAssertions;
using Omnichannel.Domain.Entities;
using Xunit;

namespace Omnichannel.Tests.Domain
{
    public class OrderDetailTests
    {
        [Fact]
        public void CalculateLineTotal_NoDiscount_ReturnsQuantityTimesPrice()
        {
            // Arrange
            var detail = new OrderDetail
            {
                Quantity = 3,
                UnitPrice = 250000m,
                DiscountRate = 0m
            };

            // Act
            var total = detail.CalculateLineTotal();

            // Assert
            total.Should().Be(750000m);
            detail.LineTotal.Should().Be(750000m);
        }

        [Fact]
        public void CalculateLineTotal_WithPercent_ReturnsDiscountedAmount()
        {
            // Arrange
            var detail = new OrderDetail
            {
                Quantity = 2,
                UnitPrice = 300000m,
                DiscountRate = 15m // Giảm 15%
            };

            // Act: 600,000 - 15% (90,000) = 510,000
            var total = detail.CalculateLineTotal();

            // Assert
            total.Should().Be(510000m);
            detail.LineTotal.Should().Be(510000m);
        }

        [Fact]
        public void CalculateLineTotal_100Percent_ReturnsZero()
        {
            // Arrange
            var detail = new OrderDetail
            {
                Quantity = 5,
                UnitPrice = 120000m,
                DiscountRate = 100m // Giảm 100% (quà tặng 0đ)
            };

            // Act
            var total = detail.CalculateLineTotal();

            // Assert
            total.Should().Be(0m);
            detail.LineTotal.Should().Be(0m);
        }

        [Fact]
        public void CalculateLineTotal_RoundsToTwoDecimals()
        {
            // Arrange
            var detail = new OrderDetail
            {
                Quantity = 3,
                UnitPrice = 199999.3333m,
                DiscountRate = 7.55m
            };

            // Act
            var total = detail.CalculateLineTotal();

            // Assert
            // 3 * 199999.3333 = 599997.9999
            // 7.55% = 45299.84899245
            // Net = 554698.15090755 => round 554698.15
            total.Should().Be(554698.15m);
        }
    }
}
