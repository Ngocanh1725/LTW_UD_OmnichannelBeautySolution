using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Services.Catalog;
using Omnichannel.Tests.Helpers;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class SearchSuggestServiceTests
    {
        private (SearchSuggestService service, IMemoryCache cache) CreateService(string dbName)
        {
            var context = TestDbContextFactory.Create(dbName);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var logger = NullLogger<SearchSuggestService>.Instance;
            var service = new SearchSuggestService(context, cache, logger);
            return (service, cache);
        }

        [Theory]
        [InlineData("kem chống nắng", "kem chong nang")]
        [InlineData("Đỗ Hương Giang", "do huong giang")]
        [InlineData("La Roche-Posay B5", "la roche-posay b5")]
        [InlineData("  Sữa   Rửa Mặt  ", "sua rua mat")]
        [InlineData("ĐẶC TRỊ NÁM TÀN NHANG", "dac tri nam tan nhang")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void RemoveDiacritics_CorrectlyNormalizesVietnameseText(string? input, string expected)
        {
            // Arrange
            var (service, _) = CreateService(Guid.NewGuid().ToString());

            // Act
            var result = service.RemoveDiacritics(input ?? string.Empty);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public async Task GetSuggestionsAsync_WhenQueryLessThan2Chars_ReturnsHotKeywordsAndFeatured()
        {
            // Arrange
            string dbName = Guid.NewGuid().ToString();
            using (var context = TestDbContextFactory.Create(dbName))
            {
                var cat = new Category { Name = "Chăm Sóc Da", Slug = "cham-soc-da", IsActive = true };
                var brand = new Brand { Name = "La Roche-Posay", Slug = "la-roche-posay", IsActive = true };
                context.Categories.Add(cat);
                context.Brands.Add(brand);
                await context.SaveChangesAsync();

                context.Products.AddRange(
                    new Product
                    {
                        Name = "Kem Dưỡng Ẩm B5",
                        Slug = "kem-duong-am-b5",
                        Sku = "B5-001",
                        SellingPrice = 350000,
                        IsActive = true,
                        IsFeatured = true,
                        CategoryId = cat.Id,
                        BrandId = brand.Id
                    },
                    new Product
                    {
                        Name = "Sữa Rửa Mặt Dịu Nhẹ",
                        Slug = "sua-rua-mat-diu-nhe",
                        Sku = "SRM-001",
                        SellingPrice = 200000,
                        IsActive = true,
                        IsFeatured = false,
                        CategoryId = cat.Id,
                        BrandId = brand.Id
                    }
                );
                await context.SaveChangesAsync();
            }

            var (service, _) = CreateService(dbName);

            // Act
            var result = await service.GetSuggestionsAsync("k");

            // Assert
            result.Should().NotBeNull();
            result.HotKeywords.Should().NotBeEmpty();
            result.Products.Should().NotBeEmpty();
            result.Products.All(p => p.IsFeatured).Should().BeTrue();
        }

        [Fact]
        public async Task GetSuggestionsAsync_WithUnaccentedQuery_MatchesAccentedProductName()
        {
            // Arrange
            string dbName = Guid.NewGuid().ToString();
            using (var context = TestDbContextFactory.Create(dbName))
            {
                var cat = new Category { Name = "Chống Nắng", Slug = "chong-nang", IsActive = true };
                var brand = new Brand { Name = "Anessa", Slug = "anessa", IsActive = true };
                context.Categories.Add(cat);
                context.Brands.Add(brand);
                await context.SaveChangesAsync();

                context.Products.Add(new Product
                {
                    Name = "Sữa Chống Nắng Hoàn Hảo Anessa Milk SPF 50+",
                    Slug = "sua-chong-nang-anessa-milk",
                    Sku = "ANE-001",
                    SellingPrice = 650000,
                    IsActive = true,
                    IsFeatured = true,
                    CategoryId = cat.Id,
                    BrandId = brand.Id,
                    FullIngredients = "Zinc Oxide, Titanium Dioxide, Dimethicone"
                });
                await context.SaveChangesAsync();
            }

            var (service, _) = CreateService(dbName);

            // Act - unaccented query "chong nang"
            var result = await service.GetSuggestionsAsync("chong nang");

            // Assert
            result.Should().NotBeNull();
            result.Products.Should().HaveCount(1);
            result.Products[0].Name.Should().Contain("Chống Nắng");
            result.Categories.Should().Contain(c => c.Name == "Chống Nắng");
        }

        [Fact]
        public async Task GetSuggestionsAsync_MatchesIngredientsAndBrand()
        {
            // Arrange
            string dbName = Guid.NewGuid().ToString();
            using (var context = TestDbContextFactory.Create(dbName))
            {
                var cat = new Category { Name = "Serum Tinh Chất", Slug = "serum", IsActive = true };
                var brand = new Brand { Name = "The Ordinary", Slug = "the-ordinary", IsActive = true };
                context.Categories.Add(cat);
                context.Brands.Add(brand);
                await context.SaveChangesAsync();

                context.Products.Add(new Product
                {
                    Name = "Tinh Chất Cấp Ẩm Hyaluronic Acid 2% + B5",
                    Slug = "tinh-chat-the-ordinary-ha-b5",
                    Sku = "TO-B5-01",
                    SellingPrice = 280000,
                    IsActive = true,
                    CategoryId = cat.Id,
                    BrandId = brand.Id,
                    FullIngredients = "Aqua, Sodium Hyaluronate, Panthenol (Vitamin B5), Pentylene Glycol"
                });
                await context.SaveChangesAsync();
            }

            var (service, _) = CreateService(dbName);

            // Act 1: Search by ingredient "panthenol"
            var ingResult = await service.GetSuggestionsAsync("panthenol");
            ingResult.Products.Should().HaveCount(1);
            ingResult.Products[0].Name.Should().Contain("Hyaluronic Acid");

            // Act 2: Search by brand "ordinary"
            var brandResult = await service.GetSuggestionsAsync("ordinary");
            brandResult.Brands.Should().Contain(b => b.Name == "The Ordinary");
        }

        [Fact]
        public async Task GetSuggestionsAsync_Ranking_PrefixMatchRanksHigherThanSubstringMatch()
        {
            // Arrange
            string dbName = Guid.NewGuid().ToString();
            using (var context = TestDbContextFactory.Create(dbName))
            {
                var cat = new Category { Name = "Dưỡng Da", Slug = "duong-da", IsActive = true };
                var brand = new Brand { Name = "Brand A", Slug = "brand-a", IsActive = true };
                context.Categories.Add(cat);
                context.Brands.Add(brand);
                await context.SaveChangesAsync();

                context.Products.AddRange(
                    new Product
                    {
                        Name = "Kem Dưỡng Chứa Serum Trắng Da",
                        Slug = "kem-duong-chua-serum",
                        Sku = "SKU-01",
                        SellingPrice = 300000,
                        IsActive = true,
                        CategoryId = cat.Id,
                        BrandId = brand.Id
                    },
                    new Product
                    {
                        Name = "Serum Phục Hồi B5 Đậm Đặc",
                        Slug = "serum-phuc-hoi-b5",
                        Sku = "SKU-02",
                        SellingPrice = 320000,
                        IsActive = true,
                        CategoryId = cat.Id,
                        BrandId = brand.Id
                    }
                );
                await context.SaveChangesAsync();
            }

            var (service, _) = CreateService(dbName);

            // Act: Search "serum"
            var result = await service.GetSuggestionsAsync("serum");

            // Assert: "Serum Phục Hồi B5" (starts with "Serum") should rank first before "Kem Dưỡng Chứa Serum"
            result.Products.Should().HaveCount(2);
            result.Products[0].Name.Should().Be("Serum Phục Hồi B5 Đậm Đặc");
            result.Products[1].Name.Should().Be("Kem Dưỡng Chứa Serum Trắng Da");
            result.Products[0].MatchScore.Should().BeGreaterThan(result.Products[1].MatchScore);
        }

        [Fact]
        public async Task GetSuggestionsAsync_WithScriptTagOrSpecialCharacters_ExecutesSafely()
        {
            // Arrange
            string dbName = Guid.NewGuid().ToString();
            var (service, _) = CreateService(dbName);

            // Act
            Func<Task> act1 = async () => await service.GetSuggestionsAsync("<script>alert('xss')</script>");
            Func<Task> act2 = async () => await service.GetSuggestionsAsync("!@#$%^&*()_+{}|:\"<>?");
            Func<Task> act3 = async () => await service.GetSuggestionsAsync(new string('a', 100));

            // Assert: No unhandled exceptions thrown
            await act1.Should().NotThrowAsync();
            await act2.Should().NotThrowAsync();
            await act3.Should().NotThrowAsync();
        }
    }
}

