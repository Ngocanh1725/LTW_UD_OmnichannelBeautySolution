using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Security;
using Omnichannel.Tests.Helpers;
using Omnichannel.Web.Areas.Admin.Controllers;
using Omnichannel.Web.ViewComponents;
using Xunit;

namespace Omnichannel.Tests.Controllers
{
    public class UiSectionControllerTests
    {
        private readonly IMemoryCache _memoryCache;
        private readonly Mock<ILogger<UiSectionController>> _loggerMock;

        public UiSectionControllerTests()
        {
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _loggerMock = new Mock<ILogger<UiSectionController>>();
        }

        [Fact]
        public void Controller_ShouldHaveRequiredSecurityAttributes()
        {
            var type = typeof(UiSectionController);

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
        public async Task Index_ReturnsViewResult_WithOrderedSections()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            context.UiSections.AddRange(
                new UiSection { SectionCode = "BEST_SELLERS", Title = "Sản Phẩm Bán Chạy", DisplayOrder = 4, IsVisible = true },
                new UiSection { SectionCode = "HERO_SLIDER", Title = "Banner Slider", DisplayOrder = 1, IsVisible = true },
                new UiSection { SectionCode = "FLASH_SALE", Title = "Flash Sale", DisplayOrder = 2, IsVisible = true }
            );
            await context.SaveChangesAsync();

            var controller = new UiSectionController(context, _memoryCache, _loggerMock.Object);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var model = viewResult.Model.Should().BeAssignableTo<List<UiSection>>().Subject;

            model.Should().HaveCount(3);
            model[0].SectionCode.Should().Be("HERO_SLIDER");
            model[1].SectionCode.Should().Be("FLASH_SALE");
            model[2].SectionCode.Should().Be("BEST_SELLERS");
        }

        [Fact]
        public async Task ToggleVisibility_ValidId_UpdatesDatabaseAndInvalidatesCache()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var section = new UiSection
            {
                SectionCode = "FLASH_SALE",
                Title = "Flash Sale",
                DisplayOrder = 1,
                IsVisible = true
            };
            context.UiSections.Add(section);
            await context.SaveChangesAsync();

            _memoryCache.Set(HomeSectionsViewComponent.CacheKey, "cached_data");

            var controller = new UiSectionController(context, _memoryCache, _loggerMock.Object);

            // Act: Turn off visibility
            var result = await controller.ToggleVisibility(new ToggleVisibilityModel { Id = section.Id, IsVisible = false }, null, null);

            // Assert
            var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
            var value = jsonResult.Value;

            var successProp = value?.GetType().GetProperty("success")?.GetValue(value);
            var isVisibleProp = value?.GetType().GetProperty("isVisible")?.GetValue(value);

            successProp.Should().Be(true);
            isVisibleProp.Should().Be(false);

            // Verify database update
            var updated = await context.UiSections.FindAsync(section.Id);
            updated!.IsVisible.Should().BeFalse();

            // Verify cache cleared
            _memoryCache.TryGetValue(HomeSectionsViewComponent.CacheKey, out _).Should().BeFalse();
        }

        [Fact]
        public async Task UpdateOrder_ValidIds_UpdatesDisplayOrdersAndInvalidatesCache()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var sec1 = new UiSection { SectionCode = "HERO_SLIDER", Title = "Slider", DisplayOrder = 1, IsVisible = true };
            var sec2 = new UiSection { SectionCode = "FLASH_SALE", Title = "Flash Sale", DisplayOrder = 2, IsVisible = true };
            var sec3 = new UiSection { SectionCode = "BEST_SELLERS", Title = "Best Sellers", DisplayOrder = 3, IsVisible = true };
            context.UiSections.AddRange(sec1, sec2, sec3);
            await context.SaveChangesAsync();

            _memoryCache.Set(HomeSectionsViewComponent.CacheKey, "cached_data");

            var controller = new UiSectionController(context, _memoryCache, _loggerMock.Object);

            // Act: Reorder to: FLASH_SALE (sec2) -> BEST_SELLERS (sec3) -> HERO_SLIDER (sec1)
            var newOrder = new List<int> { sec2.Id, sec3.Id, sec1.Id };
            var result = await controller.UpdateOrder(new UpdateOrderModel { OrderedIds = newOrder }, null);

            // Assert
            var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
            var value = jsonResult.Value;
            var successProp = value?.GetType().GetProperty("success")?.GetValue(value);
            successProp.Should().Be(true);

            // Verify database updated DisplayOrders
            var dbSec2 = await context.UiSections.FindAsync(sec2.Id);
            var dbSec3 = await context.UiSections.FindAsync(sec3.Id);
            var dbSec1 = await context.UiSections.FindAsync(sec1.Id);

            dbSec2!.DisplayOrder.Should().Be(1);
            dbSec3!.DisplayOrder.Should().Be(2);
            dbSec1!.DisplayOrder.Should().Be(3);

            // Verify cache cleared
            _memoryCache.TryGetValue(HomeSectionsViewComponent.CacheKey, out _).Should().BeFalse();
        }
    }
}
