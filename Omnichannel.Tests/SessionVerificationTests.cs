using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Omnichannel.Application.DTOs.Cms;
using Omnichannel.Application.DTOs.Inventory;
using Omnichannel.Application.Interfaces.Cms;
using Omnichannel.Application.Interfaces.Inventory;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Web.Controllers;
using Omnichannel.Web.Helpers;
using Omnichannel.Web.Models;
using Xunit;
using Xunit.Abstractions;

namespace Omnichannel.Tests
{
    public class SessionVerificationTests
    {
        private readonly ITestOutputHelper _output;

        public SessionVerificationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task Session1_1_ProductController_Detail_ShouldNeverCreateFakeBatchIds_99991_Or_99992()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_Session1_1_" + Guid.NewGuid())
                .Options;

            await using var context = new ApplicationDbContext(options);

            var category = new Category { Id = 1, Name = "Serum & Tinh Chất", Slug = "serum-tinh-chat", IsActive = true };
            var productWithBoth = new Product
            {
                Id = 10,
                Name = "Tinh Chất Phục Hồi La Roche-Posay Hyalu B5 Serum 30ml",
                Slug = "tinh-chat-la-roche-posay-hyalu-b5-30ml",
                Sku = "LRP-B5-010",
                SellingPrice = 980000m,
                CategoryId = 1,
                Category = category,
                IsActive = true
            };
            var productFreshOnly = new Product
            {
                Id = 11,
                Name = "Kem Dưỡng Ẩm CeraVe Moisturizing Cream 340g",
                Slug = "kem-duong-am-cerave-340g",
                Sku = "CRV-CRM-011",
                SellingPrice = 420000m,
                CategoryId = 1,
                Category = category,
                IsActive = true
            };

            context.Categories.Add(category);
            context.Products.AddRange(productWithBoth, productFreshOnly);
            context.ProductBatches.AddRange(
                new ProductBatch
                {
                    Id = 501,
                    ProductId = 10,
                    BatchNumber = "LOT-2028-010",
                    ManufacturingDate = DateTime.UtcNow.AddMonths(-2),
                    ExpDate = DateTime.UtcNow.AddMonths(22),
                    InitialQuantity = 150,
                    CurrentQuantity = 120,
                    Status = "ACTIVE"
                },
                new ProductBatch
                {
                    Id = 502,
                    ProductId = 10,
                    BatchNumber = "LOT-2027-EXP-010",
                    ManufacturingDate = DateTime.UtcNow.AddMonths(-18),
                    ExpDate = DateTime.UtcNow.AddMonths(5),
                    InitialQuantity = 30,
                    CurrentQuantity = 8,
                    Status = "NEAR_EXPIRATION",
                    ClearanceDiscountPercent = 45m
                },
                new ProductBatch
                {
                    Id = 503,
                    ProductId = 11,
                    BatchNumber = "LOT-2028-011",
                    ManufacturingDate = DateTime.UtcNow.AddMonths(-1),
                    ExpDate = DateTime.UtcNow.AddMonths(24),
                    InitialQuantity = 150,
                    CurrentQuantity = 120,
                    Status = "ACTIVE"
                }
            );
            await context.SaveChangesAsync();

            var reviewMock = new Mock<IProductReviewService>();
            reviewMock.Setup(r => r.GetProductReviewsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(new List<ProductReviewDto>());
            reviewMock.Setup(r => r.GetReviewStatsAsync(It.IsAny<int>(), It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(new ReviewStatsDto { AverageRating = 4.9, TotalReviews = 25 });

            var storeInvMock = new Mock<IStoreInventoryService>();
            storeInvMock.Setup(s => s.GetShowroomAvailabilityAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(new ShowroomAvailabilityViewModel());

            var controller = new ProductController(context, reviewMock.Object, storeInvMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            // 1. Kiểm tra sản phẩm có cả Lô Date Xa (Id=501) và Lô Cận Date (Id=502)
            var result1 = await controller.Detail("tinh-chat-la-roche-posay-hyalu-b5-30ml") as ViewResult;
            result1.Should().NotBeNull();
            var vm1 = result1!.Model as ProductDetailViewModel;
            vm1.Should().NotBeNull();
            vm1!.FreshBatch.Should().NotBeNull();
            vm1.FreshBatch!.Id.Should().Be(501).And.NotBe(99991);
            vm1.ClearanceBatch.Should().NotBeNull();
            vm1.ClearanceBatch!.Id.Should().Be(502).And.NotBe(99992);

            // 2. Kiểm tra sản phẩm chỉ có Lô Date Xa (Id=503), không có Lô Cận Date -> ClearanceBatch phải là null
            var result2 = await controller.Detail("kem-duong-am-cerave-340g") as ViewResult;
            var vm2 = result2!.Model as ProductDetailViewModel;
            vm2.Should().NotBeNull();
            vm2!.FreshBatch.Should().NotBeNull();
            vm2.FreshBatch!.Id.Should().Be(503).And.NotBe(99991);
            vm2.ClearanceBatch.Should().BeNull();
        }

        [Fact]
        public void Session1_2_IngredientParserHelper_ShouldExtractAccurateBreakdowns_ByProductAndCategory()
        {
            // 1. Serum Rau Má Skin1004 -> Centella Asiatica (Chiết xuất Rau Má Madagascar) ở vị trí đầu tiên
            var skin1004 = new Product
            {
                Id = 1,
                Name = "Tinh Chất Rau Má Skin1004 Madagascar Centella Ampoule 100ml",
                Category = new Category { Name = "Serum & Tinh Chất", Slug = "serum-tinh-chat" }
            };
            var breakdownSkin1004 = IngredientParserHelper.BuildDynamicIngredientBreakdowns(skin1004);
            _output.WriteLine("=== Serum Rau Má Skin1004 ===");
            foreach (var item in breakdownSkin1004)
            {
                _output.WriteLine($" - {item.Name} ({item.Percentage}) | EWG {item.EwgScore}");
            }
            breakdownSkin1004.Should().NotBeEmpty();
            breakdownSkin1004[0].Name.Should().Contain("Centella Asiatica (Chiết xuất Rau Má Madagascar)");

            // 2. Son môi MAC / Rom&nd -> Shea Butter, Vitamin E, Dimethicone, Jojoba Seed Oil
            var macLipstick = new Product
            {
                Id = 2,
                Name = "Son Thỏi Lì Mịn Môi MAC Matte Lipstick #602 Chili",
                Category = new Category { Name = "Son Môi Cao Cấp", Slug = "son-moi" }
            };
            var breakdownMac = IngredientParserHelper.BuildDynamicIngredientBreakdowns(macLipstick);
            _output.WriteLine("=== Son môi MAC / Rom&nd ===");
            foreach (var item in breakdownMac)
            {
                _output.WriteLine($" - {item.Name} ({item.Percentage}) | EWG {item.EwgScore}");
            }
            var macNames = string.Join(" | ", breakdownMac.Select(i => i.Name));
            macNames.Should().Contain("Shea Butter");
            macNames.Should().Contain("Vitamin E");
            macNames.Should().Contain("Dimethicone");
            macNames.Should().Contain("Jojoba Seed Oil");

            // 3. Kem chống nắng Anessa -> Zinc Oxide, Titanium Dioxide
            var anessaSunscreen = new Product
            {
                Id = 3,
                Name = "Sữa Chống Nắng Bảo Vệ Hoàn Hảo Anessa Perfect UV Sunscreen Skincare Milk SPF50+",
                Category = new Category { Name = "Kem Chống Nắng", Slug = "kem-chong-nang" }
            };
            var breakdownAnessa = IngredientParserHelper.BuildDynamicIngredientBreakdowns(anessaSunscreen);
            _output.WriteLine("=== Kem chống nắng Anessa ===");
            foreach (var item in breakdownAnessa)
            {
                _output.WriteLine($" - {item.Name} ({item.Percentage}) | EWG {item.EwgScore}");
            }
            var anessaNames = string.Join(" | ", breakdownAnessa.Select(i => i.Name));
            anessaNames.Should().Contain("Zinc Oxide");
            anessaNames.Should().Contain("Titanium Dioxide");
        }
    }
}
