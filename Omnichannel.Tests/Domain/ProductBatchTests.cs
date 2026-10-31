using System;
using FluentAssertions;
using Omnichannel.Domain.Entities;
using Xunit;

namespace Omnichannel.Tests.Domain
{
    public class ProductBatchTests
    {
        [Fact]
        public void IsNearExpiration_WhenWithin60Days_ReturnsTrue()
        {
            // Arrange
            var batch = new ProductBatch
            {
                BatchNumber = "LOT-2026-001",
                ExpDate = DateTime.UtcNow.Date.AddDays(45)
            };

            // Act
            var result = batch.IsNearExpiration(60);

            // Assert
            result.Should().BeTrue("vì 45 ngày nằm trong ngưỡng cảnh báo cận date 60 ngày");
        }

        [Fact]
        public void IsNearExpiration_WhenMoreThan60Days_ReturnsFalse()
        {
            // Arrange
            var batch = new ProductBatch
            {
                BatchNumber = "LOT-2026-002",
                ExpDate = DateTime.UtcNow.Date.AddDays(90)
            };

            // Act
            var result = batch.IsNearExpiration(60);

            // Assert
            result.Should().BeFalse("vì 90 ngày vượt quá ngưỡng cận date 60 ngày");
        }

        [Fact]
        public void IsNearExpiration_WhenExpired_ReturnsFalse()
        {
            // Arrange
            var batch = new ProductBatch
            {
                BatchNumber = "LOT-2026-003",
                ExpDate = DateTime.UtcNow.Date.AddDays(-5)
            };

            // Act
            var result = batch.IsNearExpiration(60);

            // Assert
            result.Should().BeFalse("vì lô hàng đã hết hạn hoàn toàn, không còn coi là cận date");
        }

        [Fact]
        public void IsExpired_WhenPastExpDate_ReturnsTrue()
        {
            // Arrange
            var batch = new ProductBatch
            {
                BatchNumber = "LOT-2026-004",
                ExpDate = DateTime.UtcNow.Date.AddDays(-1)
            };

            // Act
            var result = batch.IsExpired();

            // Assert
            result.Should().BeTrue("vì hạn sử dụng trước ngày hiện tại");
        }

        [Fact]
        public void IsExpired_WhenFutureExpDate_ReturnsFalse()
        {
            // Arrange
            var batch = new ProductBatch
            {
                BatchNumber = "LOT-2026-005",
                ExpDate = DateTime.UtcNow.Date.AddDays(30)
            };

            // Act
            var result = batch.IsExpired();

            // Assert
            result.Should().BeFalse("vì hạn sử dụng còn trong tương lai");
        }

        [Fact]
        public void IsNearExpiration_CustomThreshold30Days_ReturnsCorrectResult()
        {
            // Arrange
            var batchA = new ProductBatch { ExpDate = DateTime.UtcNow.Date.AddDays(20) };
            var batchB = new ProductBatch { ExpDate = DateTime.UtcNow.Date.AddDays(40) };

            // Act & Assert
            batchA.IsNearExpiration(30).Should().BeTrue("vì 20 ngày <= ngưỡng 30 ngày");
            batchB.IsNearExpiration(30).Should().BeFalse("vì 40 ngày > ngưỡng 30 ngày");
        }
    }
}
