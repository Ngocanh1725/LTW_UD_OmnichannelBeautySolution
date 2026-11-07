using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Omnichannel.Application.DTOs.BeautyTech;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Services.Beauty;
using Omnichannel.Tests.Helpers;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class SkincareConsultantServiceTests
    {
        [Fact]
        public async Task GenerateRoutineAsync_OilyAcne_Returns4StepsForOilySkin()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();

            // Seed test categories
            var catCleanser = new Category { Name = "Sữa Rửa Mặt", Slug = "sua-rua-mat" };
            var catToner = new Category { Name = "Toner", Slug = "toner-nuoc-hoa-hong" };
            var catSerum = new Category { Name = "Serum", Slug = "serum-tinh-chat" };
            var catMoisturizer = new Category { Name = "Kem Dưỡng", Slug = "kem-duong-am" };
            context.Categories.AddRange(catCleanser, catToner, catSerum, catMoisturizer);

            // Seed test skin types and concerns
            var stOily = new SkinType { Name = "Da dầu", Slug = "da-dau" };
            var scAcne = new SkinConcern { Name = "Trị mụn", Slug = "acne" };
            context.SkinTypes.Add(stOily);
            context.SkinConcerns.Add(scAcne);
            await context.SaveChangesAsync();

            // Seed 4 test products
            var p1 = new Product
            {
                Name = "Sữa Rửa Mặt La Roche-Posay Effaclar Cho Da Dầu Mụn",
                Sku = "LRP-CLEANSER",
                Slug = "sua-rua-mat-effaclar",
                Barcode = "893000000001",
                CategoryId = catCleanser.Id,
                RoutineStep = "1_Cleanser",
                SellingPrice = 350000m,
                IsActive = true
            };
            var p2 = new Product
            {
                Name = "Toner BHA Obagi Cân Bằng Dầu Mụn",
                Sku = "OBG-TONER",
                Slug = "toner-bha-obagi",
                Barcode = "893000000002",
                CategoryId = catToner.Id,
                RoutineStep = "2_Toner",
                SellingPrice = 450000m,
                IsActive = true
            };
            var p3 = new Product
            {
                Name = "Serum Tràm Trà Giảm Mụn Sưng Đỏ",
                Sku = "SERUM-TEATREE",
                Slug = "serum-tram-tra-mun",
                Barcode = "893000000003",
                CategoryId = catSerum.Id,
                RoutineStep = "3_Serum",
                SellingPrice = 500000m,
                IsActive = true
            };
            var p4 = new Product
            {
                Name = "Kem Dưỡng Ẩm Kiềm Dầu Mỏng Nhẹ",
                Sku = "CREAM-OILY",
                Slug = "kem-duong-kiem-dau",
                Barcode = "893000000004",
                CategoryId = catMoisturizer.Id,
                RoutineStep = "4_Moisturizer",
                SellingPrice = 400000m,
                IsActive = true
            };
            context.Products.AddRange(p1, p2, p3, p4);
            await context.SaveChangesAsync();

            context.ProductSkinTypes.AddRange(
                new ProductSkinType { ProductId = p1.Id, SkinTypeId = stOily.Id },
                new ProductSkinType { ProductId = p2.Id, SkinTypeId = stOily.Id },
                new ProductSkinType { ProductId = p3.Id, SkinTypeId = stOily.Id },
                new ProductSkinType { ProductId = p4.Id, SkinTypeId = stOily.Id }
            );
            context.ProductConcerns.AddRange(
                new ProductConcern { ProductId = p1.Id, SkinConcernId = scAcne.Id },
                new ProductConcern { ProductId = p2.Id, SkinConcernId = scAcne.Id },
                new ProductConcern { ProductId = p3.Id, SkinConcernId = scAcne.Id },
                new ProductConcern { ProductId = p4.Id, SkinConcernId = scAcne.Id }
            );
            await context.SaveChangesAsync();

            var service = new SkincareConsultantService(context, NullLogger<SkincareConsultantService>.Instance);
            var request = new SkinQuizRequestDto
            {
                SkinType = "da-dau",
                SkinConcern = "acne",
                BudgetTier = "standard"
            };

            // Act
            var result = await service.GenerateRoutineAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.SkinType.Should().Be("da-dau");
            result.SkinConcern.Should().Be("acne");
            result.Steps.Should().HaveCount(4);
            result.Steps[0].StepCode.Should().Be("1_Cleanser");
            result.Steps[1].StepCode.Should().Be("2_Toner");
            result.Steps[2].StepCode.Should().Be("3_Serum");
            result.Steps[3].StepCode.Should().Be("4_Moisturizer");

            result.OriginalTotalPrice.Should().Be(1700000m); // 350k + 450k + 500k + 400k
            result.ComboDiscountPercent.Should().Be(15m);
            result.ComboDiscountAmount.Should().Be(255000m); // 15% of 1.7M
            result.FinalComboPrice.Should().Be(1445000m);
            result.ComboCouponCode.Should().Be("COMBO15");
        }

        [Fact]
        public async Task GenerateRoutineAsync_Calculates15PercentComboDiscountCorrectly()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();

            var cat = new Category { Name = "Serum", Slug = "serum-tinh-chat" };
            context.Categories.Add(cat);
            await context.SaveChangesAsync();

            var p = new Product
            {
                Name = "Serum Phục Hồi B5 30ml",
                Sku = "TEST-B5",
                Slug = "serum-b5-test",
                Barcode = "893999999999",
                CategoryId = cat.Id,
                RoutineStep = "3_Serum",
                SellingPrice = 1000000m,
                IsActive = true
            };
            context.Products.Add(p);
            await context.SaveChangesAsync();

            var service = new SkincareConsultantService(context, NullLogger<SkincareConsultantService>.Instance);
            var request = new SkinQuizRequestDto
            {
                SkinType = "da-kho",
                SkinConcern = "hydration"
            };

            // Act
            var result = await service.GenerateRoutineAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.ComboDiscountPercent.Should().Be(15m);
            result.ComboDiscountAmount.Should().Be(Math.Round(result.OriginalTotalPrice * 0.15m, 0));
            result.FinalComboPrice.Should().Be(result.OriginalTotalPrice - result.ComboDiscountAmount);
        }
    }
}

