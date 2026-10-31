using FluentAssertions;
using Omnichannel.Domain.Entities;
using Xunit;

namespace Omnichannel.Tests.Domain
{
    public class StockTransferDetailTests
    {
        [Fact]
        public void ShortageQuantity_WhenReceivedEqualsSent_ReturnsZero()
        {
            // Arrange
            var detail = new StockTransferDetail
            {
                SentQuantity = 20,
                ReceivedQuantity = 20
            };

            // Act & Assert
            detail.ShortageQuantity.Should().Be(0, "vì nhận đủ toàn bộ số lượng thực xuất");
        }

        [Fact]
        public void ShortageQuantity_WhenReceivedLessThanSent_ReturnsDifference()
        {
            // Arrange
            var detail = new StockTransferDetail
            {
                SentQuantity = 50,
                ReceivedQuantity = 45 // Bị vỡ/thất lạc 5 chai
            };

            // Act & Assert
            detail.ShortageQuantity.Should().Be(5, "vì thiếu 5 đơn vị sản phẩm so với thực xuất");
        }

        [Fact]
        public void ShortageQuantity_WhenReceivedGreaterThanSent_ReturnsZero()
        {
            // Arrange
            var detail = new StockTransferDetail
            {
                SentQuantity = 30,
                ReceivedQuantity = 35 // Nhận thừa do kiểm đếm
            };

            // Act & Assert
            detail.ShortageQuantity.Should().Be(0, "vì không bị thiếu hụt, giá trị clamped ở mức 0");
        }

        [Fact]
        public void ShortageQuantity_WhenNothingReceived_ReturnsFullSentQuantity()
        {
            // Arrange
            var detail = new StockTransferDetail
            {
                SentQuantity = 15,
                ReceivedQuantity = 0
            };

            // Act & Assert
            detail.ShortageQuantity.Should().Be(15, "khi kho đích chưa nhận được hoặc thất lạc toàn bộ kiện hàng");
        }
    }
}
