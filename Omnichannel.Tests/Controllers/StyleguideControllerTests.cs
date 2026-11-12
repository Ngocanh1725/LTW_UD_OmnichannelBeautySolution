using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Omnichannel.Domain.Entities;
using Omnichannel.Tests.Helpers;
using Omnichannel.Web.Controllers;
using Xunit;

namespace Omnichannel.Tests.Controllers
{
    /// <summary>
    /// Kiểm thử điều kiện giới hạn môi trường Development cho trang /styleguide (Phiên 1 — Rosé Luxe Design System)
    /// </summary>
    public class StyleguideControllerTests
    {
        [Fact]
        public async Task Styleguide_WhenEnvironmentIsDevelopment_ReturnsViewResultWithSamples()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            context.Categories.Add(new Category
            {
                Name = "Chăm Sóc Da",
                Slug = "cham-soc-da",
                IsActive = true
            });
            await context.SaveChangesAsync();

            var loggerMock = new Mock<ILogger<HomeController>>();
            var envMock = new Mock<IWebHostEnvironment>();
            envMock.SetupGet(e => e.EnvironmentName).Returns(Environments.Development);

            var controller = new HomeController(loggerMock.Object, context, envMock.Object);

            // Act
            var result = await controller.Styleguide();

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var categories = viewResult.ViewData["SampleCategories"].Should().BeAssignableTo<List<Category>>().Subject;
            categories.Should().ContainSingle(c => c.Slug == "cham-soc-da");
        }

        [Fact]
        public async Task Styleguide_WhenEnvironmentIsProduction_ReturnsNotFoundResult()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var loggerMock = new Mock<ILogger<HomeController>>();
            var envMock = new Mock<IWebHostEnvironment>();
            envMock.SetupGet(e => e.EnvironmentName).Returns(Environments.Production);

            var controller = new HomeController(loggerMock.Object, context, envMock.Object);

            // Act
            var result = await controller.Styleguide();

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Styleguide_WhenEnvironmentIsStaging_ReturnsNotFoundResult()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var loggerMock = new Mock<ILogger<HomeController>>();
            var envMock = new Mock<IWebHostEnvironment>();
            envMock.SetupGet(e => e.EnvironmentName).Returns(Environments.Staging);

            var controller = new HomeController(loggerMock.Object, context, envMock.Object);

            // Act
            var result = await controller.Styleguide();

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }
    }
}

