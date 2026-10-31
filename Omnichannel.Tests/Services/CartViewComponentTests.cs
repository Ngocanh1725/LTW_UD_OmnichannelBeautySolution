using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Moq;
using Omnichannel.Application.DTOs.Sales;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Web.ViewComponents;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class CartViewComponentTests
    {
        [Fact]
        public void CartViewModel_FromDto_WithNull_ReturnsEmptyCartViewModel()
        {
            // Act
            var vm = CartViewModel.FromDto(null);

            // Assert
            vm.Should().NotBeNull();
            vm.TotalItems.Should().Be(0);
            vm.SubTotal.Should().Be(0);
            vm.ShippingFee.Should().Be(0);
            vm.FinalTotal.Should().Be(0);
            vm.Items.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void CartViewModel_FromDto_WithUnder500k_AppliesShippingFee()
        {
            // Arrange
            var dto = new CartDto
            {
                Id = 1,
                SubTotal = 300000,
                TotalAmount = 300000,
                TotalItems = 2,
                Items = new List<CartItemDto>
                {
                    new CartItemDto { ProductId = 1, ProductName = "Son dưỡng", Quantity = 2, UnitPrice = 150000 }
                }
            };

            // Act
            var vm = CartViewModel.FromDto(dto);

            // Assert
            vm.Should().NotBeNull();
            vm.TotalItems.Should().Be(2);
            vm.SubTotal.Should().Be(300000);
            vm.ShippingFee.Should().Be(30000);
            vm.FinalTotal.Should().Be(330000);
            vm.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task MiniCartViewComponent_InvokeAsync_WhenCartEmpty_ReturnsViewWithValidNonNullModel()
        {
            // Arrange
            var mockCartService = new Mock<ICartService>();
            mockCartService
                .Setup(s => s.GetCartAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((CartDto?)null);

            var component = new MiniCartViewComponent(mockCartService.Object)
            {
                ViewComponentContext = new ViewComponentContext
                {
                    ViewContext = new Microsoft.AspNetCore.Mvc.Rendering.ViewContext
                    {
                        HttpContext = new DefaultHttpContext()
                    }
                }
            };

            // Act
            var result = await component.InvokeAsync();

            // Assert
            result.Should().BeOfType<ViewViewComponentResult>();
            var viewResult = (ViewViewComponentResult)result;
            viewResult.ViewData?.Model.Should().NotBeNull();
            var model = viewResult.ViewData!.Model.Should().BeOfType<CartViewModel>().Subject;
            model.TotalItems.Should().Be(0);
            model.SubTotal.Should().Be(0);
            model.Items.Should().BeEmpty();
        }
    }
}
