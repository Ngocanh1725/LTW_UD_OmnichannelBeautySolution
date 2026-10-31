using FluentAssertions;
using Omnichannel.Domain.Entities;
using Xunit;

namespace Omnichannel.Tests.Domain
{
    public class StoreInventoryTests
    {
        [Fact]
        public void GetAvailableQuantity_NormalCase_ReturnsPhysicalMinusReserved()
        {
            // Arrange
            var inventory = new StoreInventory
            {
                PhysicalQuantity = 100,
                ReservedQuantity = 30
            };

            // Act
            var available = inventory.GetAvailableQuantity();

            // Assert
            available.Should().Be(70, "vì số lượng khả dụng = tồn thực (100) - tạm giữ (30)");
        }

        [Fact]
        public void GetAvailableQuantity_AllReserved_ReturnsZero()
        {
            // Arrange
            var inventory = new StoreInventory
            {
                PhysicalQuantity = 50,
                ReservedQuantity = 50
            };

            // Act
            var available = inventory.GetAvailableQuantity();

            // Assert
            available.Should().Be(0, "vì toàn bộ tồn kho đã được đặt giữ trước");
        }

        [Fact]
        public void GetAvailableQuantity_OverReserved_ReturnsZero()
        {
            // Arrange
            var inventory = new StoreInventory
            {
                PhysicalQuantity = 20,
                ReservedQuantity = 30 // Bị giữ vượt quá do dữ liệu tạm
            };

            // Act
            var available = inventory.GetAvailableQuantity();

            // Assert
            available.Should().Be(0, "vì tồn kho khả dụng không bao giờ được phép âm, phải được chặn ở mức 0");
        }

        [Fact]
        public void GetAvailableQuantity_NoReserved_ReturnsPhysical()
        {
            // Arrange
            var inventory = new StoreInventory
            {
                PhysicalQuantity = 80,
                ReservedQuantity = 0
            };

            // Act
            var available = inventory.GetAvailableQuantity();

            // Assert
            available.Should().Be(80, "khi không có đơn giữ hàng thì toàn bộ số lượng thực tế đều sẵn sàng bán");
        }
    }
}
