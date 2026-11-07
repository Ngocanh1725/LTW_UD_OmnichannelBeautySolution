using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Domain.Entities;
using Omnichannel.Tests.Helpers;
using Omnichannel.Web.Helpers;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class IngredientParserAndCleanBeautyTests
    {
        [Fact]
        public void IngredientParser_KeyActives_IdentifiedAndStyledAsGold()
        {
            // Arrange
            const string ingredients = "Aqua, Glycerin, Niacinamide 2%, Panthenol (Vitamin B5), Retinol 0.5%, Sodium Hyaluronate, Phenoxyethanol";

            // Act
            var parsed = IngredientParserHelper.ParseIngredients(ingredients);

            // Assert
            parsed.Should().NotBeNull();
            parsed.Should().HaveCount(7);

            var keyActives = parsed.Where(i => i.IsKeyActive).ToList();
            keyActives.Should().HaveCount(4); // Niacinamide, Panthenol, Retinol, Sodium Hyaluronate
            keyActives.Should().AllSatisfy(a =>
            {
                a.InciCategory.Should().Be("KEY_ACTIVE");
                a.BadgeColorClass.Should().Contain("bg-amber-50");
                a.BadgeColorClass.Should().Contain("text-amber-900");
                a.BadgeIcon.Should().Contain("fa-sparkles");
            });
        }

        [Fact]
        public void IngredientParser_NaturalExtracts_IdentifiedAndStyledAsGreen()
        {
            // Arrange
            const string ingredients = "Water, Butylene Glycol, Centella Asiatica (Rau Má) Extract, Tea Tree Leaf Oil, Chamomilla Recutita Extract, Carbomer";

            // Act
            var parsed = IngredientParserHelper.ParseIngredients(ingredients);

            // Assert
            parsed.Should().NotBeNull();
            var naturalExtracts = parsed.Where(i => i.IsNaturalExtract).ToList();
            naturalExtracts.Should().HaveCount(3); // Centella, Tea tree, Chamomilla
            naturalExtracts.Should().AllSatisfy(n =>
            {
                n.InciCategory.Should().Be("NATURAL_EXTRACT");
                n.BadgeColorClass.Should().Contain("bg-emerald-50");
                n.BadgeColorClass.Should().Contain("text-emerald-900");
                n.BadgeIcon.Should().Contain("fa-leaf");
            });
        }

        [Fact]
        public async Task CategoryFilter_CleanBeautyFlags_FiltersCorrectProducts()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();

            var cat = new Category { Name = "Chăm Sóc Da", Slug = "cham-soc-da" };
            context.Categories.Add(cat);
            await context.SaveChangesAsync();

            var pClean = new Product
            {
                Name = "Serum Phục Hồi Dịu Nhẹ Clean",
                Sku = "P-CLEAN-01",
                Slug = "serum-phuc-hoi-clean",
                Barcode = "893000000101",
                CategoryId = cat.Id,
                SellingPrice = 450000m,
                IsActive = true,
                IsAlcoholFree = true,
                IsFragranceFree = true,
                IsPregnancySafe = true
            };

            var pFragranceOnly = new Product
            {
                Name = "Nước Hoa Hồng Thơm Ngát",
                Sku = "P-FRAG-02",
                Slug = "nuoc-hoa-hong-frag",
                Barcode = "893000000102",
                CategoryId = cat.Id,
                SellingPrice = 300000m,
                IsActive = true,
                IsAlcoholFree = true,
                IsFragranceFree = false,
                IsPregnancySafe = true
            };

            var pAlcoholOnly = new Product
            {
                Name = "Kem Chống Nắng Cồn Kiềm Dầu",
                Sku = "P-ALC-03",
                Slug = "kem-chong-nang-alc",
                Barcode = "893000000103",
                CategoryId = cat.Id,
                SellingPrice = 500000m,
                IsActive = true,
                IsAlcoholFree = false,
                IsFragranceFree = true,
                IsPregnancySafe = true
            };

            context.Products.AddRange(pClean, pFragranceOnly, pAlcoholOnly);
            await context.SaveChangesAsync();

            // Act: Lọc sản phẩm Không Hương Liệu
            var fragranceFreeList = await context.Products
                .Where(p => p.IsActive && p.IsFragranceFree)
                .ToListAsync();

            // Act: Lọc sản phẩm Không Cồn Khô
            var alcoholFreeList = await context.Products
                .Where(p => p.IsActive && p.IsAlcoholFree)
                .ToListAsync();

            // Act: Lọc sản phẩm Clean 100% (cả cồn và hương liệu)
            var ultraCleanList = await context.Products
                .Where(p => p.IsActive && p.IsAlcoholFree && p.IsFragranceFree && p.IsPregnancySafe)
                .ToListAsync();

            // Assert
            fragranceFreeList.Should().HaveCount(2);
            fragranceFreeList.Select(p => p.Sku).Should().Contain(new[] { "P-CLEAN-01", "P-ALC-03" });

            alcoholFreeList.Should().HaveCount(2);
            alcoholFreeList.Select(p => p.Sku).Should().Contain(new[] { "P-CLEAN-01", "P-FRAG-02" });

            ultraCleanList.Should().HaveCount(1);
            ultraCleanList[0].Sku.Should().Be("P-CLEAN-01");
        }
    }
}

