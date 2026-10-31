using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Omnichannel.Application.DTOs.Cms;
using Omnichannel.Application.Interfaces.Cms;
using Omnichannel.Infrastructure.Security;
using Omnichannel.Web.Areas.Admin.Controllers;
using Xunit;

namespace Omnichannel.Tests.Controllers
{
    public class ThemeConfigControllerTests
    {
        private readonly Mock<IThemeService> _themeServiceMock;
        private readonly ThemeConfigController _controller;

        public ThemeConfigControllerTests()
        {
            _themeServiceMock = new Mock<IThemeService>();
            _controller = new ThemeConfigController(_themeServiceMock.Object);

            var tempDataMock = new Mock<ITempDataDictionary>();
            _controller.TempData = tempDataMock.Object;
        }

        [Fact]
        public void Controller_ShouldHaveRequiredSecurityAttributes()
        {
            var type = typeof(ThemeConfigController);

            var areaAttr = type.GetCustomAttribute<AreaAttribute>();
            areaAttr.Should().NotBeNull();
            areaAttr!.RouteValue.Should().Be("Admin");

            var authAttrs = type.GetCustomAttributes<AuthorizeAttribute>();
            authAttrs.Should().NotBeEmpty();

            var permAttr = type.GetCustomAttribute<HasPermissionAttribute>();
            permAttr.Should().NotBeNull();
            permAttr!.Policy.Should().Contain("THEME_MANAGE");
        }

        [Fact]
        public async Task Index_Get_ReturnsViewResult_WithCurrentTheme()
        {
            // Arrange
            var theme = new ThemeConfigDto
            {
                BrandName = "OMNI BEAUTY",
                PrimaryColor = "#E11D48"
            };
            _themeServiceMock.Setup(s => s.GetCurrentThemeAsync()).ReturnsAsync(theme);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            viewResult.Model.Should().BeEquivalentTo(theme);
        }

        [Fact]
        public async Task Index_Post_ValidModel_CallsUpdateThemeAndRedirects()
        {
            // Arrange
            var model = new ThemeConfigDto
            {
                Id = 1,
                BrandName = "OMNI BEAUTY",
                PrimaryColor = "#0D9488" // Teal
            };
            _themeServiceMock.Setup(s => s.UpdateThemeAsync(model)).ReturnsAsync(true);

            // Act
            var result = await _controller.Index(model);

            // Assert
            var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirectResult.ActionName.Should().Be(nameof(ThemeConfigController.Index));
            _themeServiceMock.Verify(s => s.UpdateThemeAsync(model), Times.Once);
        }

        [Fact]
        public async Task Index_Post_InvalidModelState_ReturnsViewWithoutCallingUpdate()
        {
            // Arrange
            var model = new ThemeConfigDto();
            _controller.ModelState.AddModelError("BrandName", "Brand name is required");

            // Act
            var result = await _controller.Index(model);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            viewResult.Model.Should().Be(model);
            _themeServiceMock.Verify(s => s.UpdateThemeAsync(It.IsAny<ThemeConfigDto>()), Times.Never);
        }
    }
}
