using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Services.BeautyTech;
using Omnichannel.Tests.Helpers;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class SkincareIngredientInteractionEngineTests
    {
        [Fact]
        public async Task CheckIngredientConflicts_RetinolAndBha_ReturnsWarningConflict()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var engine = new SkincareIngredientInteractionEngine(context, NullLogger<SkincareIngredientInteractionEngine>.Instance);

            var ingredients = new List<string> { "Retinol 1%", "Salicylic Acid 2% (BHA)" };

            // Act
            var analysis = await engine.CheckIngredientConflictsAsync(ingredients);

            // Assert
            analysis.Should().NotBeNull();
            analysis.Interactions.Should().Contain(f => f.InteractionType == "HIGH_CONFLICT");
            var conflict = analysis.Interactions.Find(f => f.InteractionType == "HIGH_CONFLICT");
            conflict!.SeverityBadge.Should().Be("WARNING");
            conflict.RecommendedResolution.Should().Contain("Skin Cycling");
        }

        [Fact]
        public async Task CheckIngredientConflicts_RetinolAndBenzoylPeroxide_ReturnsDangerDeactivation()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var engine = new SkincareIngredientInteractionEngine(context, NullLogger<SkincareIngredientInteractionEngine>.Instance);

            var ingredients = new List<string> { "Retinol", "Benzoyl Peroxide" };

            // Act
            var analysis = await engine.CheckIngredientConflictsAsync(ingredients);

            // Assert
            analysis.Should().NotBeNull();
            analysis.Interactions.Should().Contain(f => f.InteractionType == "CHEMICAL_DEACTIVATION");
            var conflict = analysis.Interactions.Find(f => f.InteractionType == "CHEMICAL_DEACTIVATION");
            conflict!.SeverityBadge.Should().Be("DANGER");
        }

        [Fact]
        public async Task CheckIngredientConflicts_VitaminCAndCopperPeptides_ReturnsDangerRedox()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var engine = new SkincareIngredientInteractionEngine(context, NullLogger<SkincareIngredientInteractionEngine>.Instance);

            var ingredients = new List<string> { "Pure Vitamin C (L-Ascorbic Acid)", "Copper Tripeptide-1" };

            // Act
            var analysis = await engine.CheckIngredientConflictsAsync(ingredients);

            // Assert
            analysis.Should().NotBeNull();
            analysis.Interactions.Should().Contain(f => f.InteractionType == "REDOX_CONFLICT");
            var conflict = analysis.Interactions.Find(f => f.InteractionType == "REDOX_CONFLICT");
            conflict!.SeverityBadge.Should().Be("DANGER");
        }

        [Fact]
        public async Task CheckIngredientConflicts_VitaminCAndFerulicAcid_ReturnsGoldSynergy()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var engine = new SkincareIngredientInteractionEngine(context, NullLogger<SkincareIngredientInteractionEngine>.Instance);

            var ingredients = new List<string> { "Vitamin C", "Ferulic Acid" };

            // Act
            var analysis = await engine.CheckIngredientConflictsAsync(ingredients);

            // Assert
            analysis.Should().NotBeNull();
            analysis.Interactions.Should().Contain(f => f.InteractionType == "GOLD_SYNERGY");
            var synergy = analysis.Interactions.Find(f => f.InteractionType == "GOLD_SYNERGY");
            synergy!.SeverityBadge.Should().Be("SUCCESS_SYNERGY");
        }

        [Fact]
        public async Task CheckIngredientConflicts_RetinolAndPanthenol_ReturnsBarrierSynergy()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var engine = new SkincareIngredientInteractionEngine(context, NullLogger<SkincareIngredientInteractionEngine>.Instance);

            var ingredients = new List<string> { "Retinol", "Panthenol (Vitamin B5)" };

            // Act
            var analysis = await engine.CheckIngredientConflictsAsync(ingredients);

            // Assert
            analysis.Should().NotBeNull();
            analysis.Interactions.Should().Contain(f => f.InteractionType == "BARRIER_SYNERGY");
            var synergy = analysis.Interactions.Find(f => f.InteractionType == "BARRIER_SYNERGY");
            synergy!.SeverityBadge.Should().Be("SUCCESS_SYNERGY");
        }

        [Fact]
        public async Task AnalyzeCartOrRoutineAsync_ConflictingProducts_DetectsConflictsAndSchedulesRoutine()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var engine = new SkincareIngredientInteractionEngine(context, NullLogger<SkincareIngredientInteractionEngine>.Instance);

            var p1 = new Product
            {
                Name = "Serum Tái Tạo Da Chứa Retinol 0.5%",
                Slug = "serum-retinol-05",
                Sku = "RET-01",
                ShortDescription = "Chứa Retinol nguyên chất nồng độ cao",
                IsActive = true
            };

            var p2 = new Product
            {
                Name = "Dung Dịch Tẩy Da Chết BHA Salicylic Acid 2%",
                Slug = "tay-da-chet-bha-2",
                Sku = "BHA-02",
                ShortDescription = "Chứa Salicylic Acid BHA 2%",
                IsActive = true
            };

            context.Products.AddRange(p1, p2);
            await context.SaveChangesAsync();

            // Act
            var result = await engine.AnalyzeCartOrRoutineAsync(new List<int> { p1.Id, p2.Id });

            // Assert
            result.Should().NotBeNull();
            result.HasCriticalConflicts.Should().BeTrue();
            result.Interactions.Should().NotBeEmpty();
            result.EveningRoutineSteps.Should().NotBeEmpty();
            result.AlternateNightNotes.Should().NotBeEmpty();
        }

        [Fact]
        public async Task AnalyzeCartOrRoutineAsync_CustomerAllergy_TriggersAllergyWarning()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var engine = new SkincareIngredientInteractionEngine(context, NullLogger<SkincareIngredientInteractionEngine>.Instance);

            var customer = new User
            {
                Username = "allergic_user",
                Email = "allergic@omni.vn",
                FullName = "Nguyễn Văn Dị Ứng",
                PasswordHash = "hash"
            };
            context.Users.Add(customer);
            await context.SaveChangesAsync();

            var profile = new CustomerProfile
            {
                UserId = customer.Id,
                KnownAllergies = JsonSerializer.Serialize(new List<string> { "Niacinamide", "Paraben" })
            };
            context.CustomerProfiles.Add(profile);

            var p = new Product
            {
                Name = "Serum Phục Hồi Chứa Niacinamide 10%",
                Slug = "serum-niacinamide-10",
                Sku = "NIA-01",
                ShortDescription = "Tinh chất Niacinamide 10% và Zinc PCA",
                IsActive = true
            };
            context.Products.Add(p);
            await context.SaveChangesAsync();

            // Act
            var result = await engine.AnalyzeCartOrRoutineAsync(new List<int> { p.Id }, customer.Id);

            // Assert
            result.Should().NotBeNull();
            result.HasAllergyAlerts.Should().BeTrue();
            result.AllergyWarnings.Should().NotBeEmpty();
            result.AllergyWarnings[0].Should().Contain("Niacinamide");
        }
    }
}
