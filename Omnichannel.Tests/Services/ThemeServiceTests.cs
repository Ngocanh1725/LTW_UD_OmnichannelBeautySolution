using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Omnichannel.Application.DTOs.Cms;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Services;
using Omnichannel.Tests.Helpers;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class ThemeServiceTests
    {
        private readonly IMemoryCache _memoryCache;
        private readonly Mock<ILogger<ThemeService>> _loggerMock;

        public ThemeServiceTests()
        {
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _loggerMock = new Mock<ILogger<ThemeService>>();
        }

        [Fact]
        public async Task GetCurrentThemeAsync_WhenNoConfigInDb_ReturnsDefaultTheme()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new ThemeService(context, _memoryCache, _loggerMock.Object);

            // Act
            var theme = await service.GetCurrentThemeAsync();

            // Assert
            theme.Should().NotBeNull();
            theme.BrandName.Should().Be("CANMAKE BEAUTY");
            theme.PrimaryColor.Should().Be("#E8618C");
            theme.FontFamily.Should().Contain("Quicksand");
        }

        [Fact]
        public async Task UpdateThemeAsync_WithTealColor_PersistsToDbAndUpdatesCache()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var initialTheme = new ThemeConfig
            {
                BrandName = "CANMAKE BEAUTY",
                PrimaryColor = "#E8618C",
                SecondaryColor = "#B8336A",
                AccentColor = "#F7C6D4",
                FontFamily = "'Quicksand', sans-serif",
                IsActive = true
            };
            context.ThemeConfigs.Add(initialTheme);
            await context.SaveChangesAsync();

            var service = new ThemeService(context, _memoryCache, _loggerMock.Object);

            // Populate cache
            var cached = await service.GetCurrentThemeAsync();
            cached.PrimaryColor.Should().Be("#E8618C");

            // Act: Update to Teal (#0D9488)
            var updateDto = new ThemeConfigDto
            {
                Id = initialTheme.Id,
                BrandName = "CANMAKE BEAUTY PREMIUM",
                PrimaryColor = "#0D9488",
                SecondaryColor = "#0F766E",
                AccentColor = "#F59E0B",
                FontFamily = "'Playfair Display', serif",
                AnnouncementBarText = "✨ Ưu đãi hè rực rỡ - Giảm tới 50% ✨",
                AnnouncementBarActive = true,
                AnnouncementBarBgColor = "#0F766E"
            };

            var result = await service.UpdateThemeAsync(updateDto);

            // Assert
            result.Should().BeTrue();

            // Verify cache invalidated and freshly retrieved from DB
            var refreshed = await service.GetCurrentThemeAsync();
            refreshed.BrandName.Should().Be("CANMAKE BEAUTY PREMIUM");
            refreshed.PrimaryColor.Should().Be("#0D9488");
            refreshed.SecondaryColor.Should().Be("#0F766E");
            refreshed.FontFamily.Should().Be("'Playfair Display', serif");
        }
    }
}
