using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Omnichannel.Application.DTOs.Catalog;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Services.Catalog;
using Omnichannel.Tests.Helpers;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class SmartFilterServiceTests
    {
        [Fact]
        public async Task GetSmartRecommendations_MatchesSkinTypeAndConcerns_CalculatesHighScore()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new SmartFilterService(context, NullLogger<SmartFilterService>.Instance);

            var cat = new Category { Name = "Chăm sóc da", Slug = "cham-soc-da" };
            context.Categories.Add(cat);

            var stOily = new SkinType { Name = "Da dầu", Slug = "da-dau" };
            var stDry = new SkinType { Name = "Da khô", Slug = "da-kho" };
            context.SkinTypes.AddRange(stOily, stDry);

            var scAcne = new SkinConcern { Name = "Trị mụn", Slug = "acne" };
            var scHydration = new SkinConcern { Name = "Dưỡng ẩm", Slug = "hydration" };
            context.SkinConcerns.AddRange(scAcne, scHydration);
            await context.SaveChangesAsync();

            // Product 1: Best match for Oily + Acne
            var p1 = new Product
            {
                Name = "Serum Niacinamide 10% Giảm Mụn Kiềm Dầu",
                Slug = "serum-niacinamide-10",
                Sku = "SRM-01",
                CategoryId = cat.Id,
                SellingPrice = 350000m,
                IsActive = true
            };
            // Product 2: Best match for Dry + Hydration
            var p2 = new Product
            {
                Name = "Kem Dưỡng Ẩm Chuyên Sâu Cho Da Khô",
                Slug = "kem-duong-am-da-kho",
                Sku = "CRM-02",
                CategoryId = cat.Id,
                SellingPrice = 450000m,
                IsActive = true
            };
            context.Products.AddRange(p1, p2);
            await context.SaveChangesAsync();

            context.ProductSkinTypes.AddRange(
                new ProductSkinType { ProductId = p1.Id, SkinTypeId = stOily.Id },
                new ProductSkinType { ProductId = p2.Id, SkinTypeId = stDry.Id }
            );

            context.ProductConcerns.AddRange(
                new ProductConcern { ProductId = p1.Id, SkinConcernId = scAcne.Id },
                new ProductConcern { ProductId = p2.Id, SkinConcernId = scHydration.Id }
            );
            await context.SaveChangesAsync();

            // Act
            var criteria = new SmartFilterCriteriaDto
            {
                SkinType = "da-dau",
                Concerns = new List<string> { "acne" },
                BudgetBracket = "300k_500k"
            };
            var result = await service.GetSmartRecommendationsAsync(criteria);

            // Assert
            result.Success.Should().BeTrue();
            result.Products.Should().NotBeEmpty();
            result.Products[0].Id.Should().Be(p1.Id);
            result.Products[0].MatchScore.Should().BeGreaterThanOrEqualTo(90);
            result.Products[0].MatchReasons.Should().NotBeEmpty();
            result.RecommendedRoutine.Should().NotBeEmpty();
        }

        [Fact]
        public async Task BulkUpdateAttributes_ReplaceMode_ReplacesOldAttributes()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new SmartFilterService(context, NullLogger<SmartFilterService>.Instance);

            var cat = new Category { Name = "Chăm sóc da", Slug = "cham-soc-da" };
            context.Categories.Add(cat);

            var st1 = new SkinType { Name = "Da dầu", Slug = "da-dau" };
            var st2 = new SkinType { Name = "Da khô", Slug = "da-kho" };
            context.SkinTypes.AddRange(st1, st2);

            var sc1 = new SkinConcern { Name = "Trị mụn", Slug = "acne" };
            var sc2 = new SkinConcern { Name = "Chống lão hóa", Slug = "anti_aging" };
            context.SkinConcerns.AddRange(sc1, sc2);

            var p1 = new Product { Name = "Sản phẩm A", Slug = "sp-a", Sku = "SPA", CategoryId = cat.Id, SellingPrice = 200000m };
            var p2 = new Product { Name = "Sản phẩm B", Slug = "sp-b", Sku = "SPB", CategoryId = cat.Id, SellingPrice = 300000m };
            context.Products.AddRange(p1, p2);
            await context.SaveChangesAsync();

            // Initial: p1 has st1 and sc1
            context.ProductSkinTypes.Add(new ProductSkinType { ProductId = p1.Id, SkinTypeId = st1.Id });
            context.ProductConcerns.Add(new ProductConcern { ProductId = p1.Id, SkinConcernId = sc1.Id });
            await context.SaveChangesAsync();

            // Act: Bulk update p1 and p2 with st2 and sc2 (REPLACE)
            var bulkDto = new BulkUpdateAttributesDto
            {
                ProductIds = new List<int> { p1.Id, p2.Id },
                SkinTypeIds = new List<int> { st2.Id },
                ConcernIds = new List<int> { sc2.Id },
                Mode = "REPLACE"
            };

            var count = await service.BulkUpdateAttributesAsync(bulkDto);

            // Assert
            count.Should().Be(2);

            var p1SkinTypes = await context.ProductSkinTypes.Where(x => x.ProductId == p1.Id).ToListAsync();
            p1SkinTypes.Should().HaveCount(1);
            p1SkinTypes[0].SkinTypeId.Should().Be(st2.Id);

            var p1Concerns = await context.ProductConcerns.Where(x => x.ProductId == p1.Id).ToListAsync();
            p1Concerns.Should().HaveCount(1);
            p1Concerns[0].SkinConcernId.Should().Be(sc2.Id);
        }
    }
}
