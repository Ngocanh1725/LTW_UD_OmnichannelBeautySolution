using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Omnichannel.Application.DTOs.Catalog;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Services.BeautyTech;
using Omnichannel.Infrastructure.Services.Catalog;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class ProductComparisonServiceTests
    {
        private ApplicationDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        [Theory]
        [InlineData("Serum Phục Hồi Da La Roche-Posay Hyalu B5 Pure 30ml", 30, "ml")]
        [InlineData("Kem Dưỡng Ẩm Chuyên Sâu 1.7 fl oz", 50.3, "ml")]
        [InlineData("Kem Chống Nắng Kiểm Soát Dầu 50 g", 50, "g")]
        [InlineData("Nước Tẩy Trang L'Oréal Paris Micellar Water 400ml", 400, "ml")]
        [InlineData("Son Kem Lì Rom&nd Zero Velvet Tint 5.5g", 5.5, "g")]
        [InlineData("Nước Cân Bằng Dưỡng Ẩm Toàn Diện 1L", 1000, "ml")]
        public void ExtractVolumeAndUnit_Should_Correctly_Parse_Amount_And_Unit(
            string productName, decimal expectedAmount, string expectedUnit)
        {
            // Act
            var (amount, parsedUnit) = ProductComparisonService.ExtractVolumeAndUnit(productName, "Chai");

            // Assert
            Assert.NotNull(amount);
            Assert.Equal(expectedAmount, amount!.Value);
            Assert.Equal(expectedUnit, parsedUnit);
        }

        [Fact]
        public void ExtractVolumeAndUnit_When_No_Volume_In_Name_Should_Return_Null_And_Display_Dash()
        {
            // Arrange: Tên sản phẩm không chứa bất kỳ dung tích nào
            string productName = "Son Thỏi Mịn Mờ Cao Cấp L'Oréal Paris Color Riche";

            // Act
            var (amount, unit) = ProductComparisonService.ExtractVolumeAndUnit(productName, "Thỏi");

            var dto = new ProductComparisonDetailDto
            {
                Name = productName,
                VolumeAmount = amount,
                VolumeUnit = unit
            };

            // Assert
            Assert.Null(amount);
            Assert.Equal("——", dto.VolumeDisplay);
        }

        [Fact]
        public void MarkBestValueProducts_Should_Highlight_Lowest_Price_Per_Volume_Ratio()
        {
            // Arrange: 2 sản phẩm cùng đơn vị ml
            // SP A: 300.000₫ cho 30ml -> 10.000₫/ml
            // SP B: 400.000₫ cho 50ml -> 8.000₫/ml (Kinh tế nhất!)
            var items = new List<ProductComparisonDetailDto>
            {
                new ProductComparisonDetailDto
                {
                    Id = 1,
                    Name = "Serum A 30ml",
                    FinalPrice = 300000m,
                    VolumeAmount = 30m,
                    VolumeUnit = "ml",
                    UnitPriceRatio = 10000m
                },
                new ProductComparisonDetailDto
                {
                    Id = 2,
                    Name = "Serum B 50ml",
                    FinalPrice = 400000m,
                    VolumeAmount = 50m,
                    VolumeUnit = "ml",
                    UnitPriceRatio = 8000m
                }
            };

            // Act
            ProductComparisonService.MarkBestValueProducts(items);

            // Assert
            Assert.False(items[0].IsBestValuePriceRatio);
            Assert.True(items[1].IsBestValuePriceRatio);
            Assert.True(items[1].IsBestValuePerMl);
        }

        [Fact]
        public void MarkLongestFefoProducts_Should_Highlight_Farthest_Expiration_Date()
        {
            // Arrange
            var items = new List<ProductComparisonDetailDto>
            {
                new ProductComparisonDetailDto
                {
                    Id = 1,
                    Name = "Serum Lô Cũ",
                    DaysUntilExpiry = 120
                },
                new ProductComparisonDetailDto
                {
                    Id = 2,
                    Name = "Serum Lô Mới",
                    DaysUntilExpiry = 720 // Date xa nhất!
                }
            };

            // Act
            ProductComparisonService.MarkLongestFefoProducts(items);

            // Assert
            Assert.False(items[0].IsLongestFefo);
            Assert.True(items[1].IsLongestFefo);
        }

        [Fact]
        public async Task GetCompareQuickItemsAsync_Should_Return_Summaries_In_Requested_Order()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var cat = new Category { Id = 10, Name = "Dưỡng Da", Slug = "duong-da", IsActive = true };
            var brand = new Brand { Id = 10, Name = "Mỹ Phẩm Cao Cấp", Slug = "my-pham", IsActive = true };
            context.Categories.Add(cat);
            context.Brands.Add(brand);

            var p1 = new Product
            {
                Id = 10,
                CategoryId = 10,
                BrandId = 10,
                Name = "Serum Hyalu B5 30ml",
                Slug = "serum-b5-30ml",
                SellingPrice = 900000m,
                DiscountPrice = 810000m,
                IsActive = true,
                Unit = "Chai",
                Sku = "SKU-B5",
                Barcode = "893000000001"
            };
            var p2 = new Product
            {
                Id = 20,
                CategoryId = 10,
                BrandId = 10,
                Name = "Nước Cân Bằng Centella 200ml",
                Slug = "nuoc-can-bang-centella-200ml",
                SellingPrice = 350000m,
                IsActive = true,
                Unit = "Chai",
                Sku = "SKU-CEN",
                Barcode = "893000000002"
            };
            context.Products.AddRange(p1, p2);
            await context.SaveChangesAsync();

            var interactionEngine = new SkincareIngredientInteractionEngine(context, NullLogger<SkincareIngredientInteractionEngine>.Instance);
            var service = new ProductComparisonService(context, interactionEngine, NullLogger<ProductComparisonService>.Instance);

            // Act: Yêu cầu theo thứ tự [20, 10]
            var result = await service.GetCompareQuickItemsAsync(new List<int> { 20, 10 });

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(20, result[0].Id);
            Assert.Equal(10, result[1].Id);
            Assert.Equal(810000m, result[1].FinalPrice);
            Assert.True(result[1].HasDiscount);
            Assert.Equal(30m, result[1].VolumeAmount);
            Assert.Equal("ml", result[1].VolumeUnit);
            Assert.Equal(27000m, result[1].UnitPriceRatio); // 810.000 / 30 = 27.000
        }

        [Fact]
        public async Task GetDetailedComparisonAsync_Should_Safely_Handle_Invalid_And_Cap_At_3_Products()
        {
            // Arrange: Tạo 4 sản phẩm trong database
            using var context = CreateInMemoryDbContext();
            var cat = new Category { Id = 1, Name = "Serum", Slug = "serum", IsActive = true };
            context.Categories.Add(cat);

            for (int i = 1; i <= 4; i++)
            {
                context.Products.Add(new Product
                {
                    Id = i,
                    CategoryId = 1,
                    Name = $"Serum Test {i} 30ml",
                    Slug = $"serum-test-{i}",
                    SellingPrice = 100000m * i,
                    IsActive = true,
                    Unit = "Chai",
                    Sku = $"SKU-{i}",
                    Barcode = $"89300000000{i}"
                });
            }
            await context.SaveChangesAsync();

            var interactionEngine = new SkincareIngredientInteractionEngine(context, NullLogger<SkincareIngredientInteractionEngine>.Instance);
            var service = new ProductComparisonService(context, interactionEngine, NullLogger<ProductComparisonService>.Instance);

            // Act: Truyền ids bao gồm số âm, số 0 và hơn 3 ID hợp lệ: [-5, 0, 1, 2, 3, 4]
            var matrix = await service.GetDetailedComparisonAsync(new List<int> { -5, 0, 1, 2, 3, 4 });

            // Assert: Chỉ lấy tối đa 3 sản phẩm hợp lệ, không gây lỗi ngoại lệ
            Assert.NotNull(matrix);
            Assert.Equal(3, matrix.TotalProducts);
            Assert.Equal(new List<int> { 1, 2, 3 }, matrix.Products.Select(p => p.Id).ToList());

            // Act 2: Truyền chỉ id âm, 0 hoặc ID không tồn tại -> Xử lý an toàn, trả về matrix rỗng
            var emptyMatrix = await service.GetDetailedComparisonAsync(new List<int> { -10, 0, 99999 });
            Assert.NotNull(emptyMatrix);
            Assert.Equal(0, emptyMatrix.TotalProducts);
        }

        [Fact]
        public async Task GetDetailedComparisonAsync_Should_Include_Ewg_And_Interactions()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var brand = new Brand { Id = 1, Name = "La Roche-Posay", Slug = "la-roche-posay", IsActive = true };
            var cat = new Category { Id = 1, Name = "Tinh Chất Phục Hồi", Slug = "tinh-chat", IsActive = true };
            context.Brands.Add(brand);
            context.Categories.Add(cat);

            var p1 = new Product
            {
                Id = 101,
                Name = "Serum Retinol Tinh Khiết 0.3% 30ml",
                Slug = "serum-retinol-03",
                SellingPrice = 600000m,
                IsActive = true,
                BrandId = 1,
                CategoryId = 1,
                Unit = "Chai",
                Sku = "SKU-RET",
                Barcode = "893000000101",
                RoutineStep = "3_Serum",
                IsAlcoholFree = true,
                IsFragranceFree = true,
                IsPregnancySafe = false
            };

            var p2 = new Product
            {
                Id = 102,
                Name = "Dung Dịch Tẩy Tế Bào Chết BHA 2% Salicylic Acid 100ml",
                Slug = "dung-dich-bha-2",
                SellingPrice = 800000m,
                IsActive = true,
                BrandId = 1,
                CategoryId = 1,
                Unit = "Chai",
                Sku = "SKU-BHA",
                Barcode = "893000000102",
                RoutineStep = "2_Toner",
                IsAlcoholFree = true,
                IsFragranceFree = true,
                IsPregnancySafe = false
            };

            context.Products.AddRange(p1, p2);
            await context.SaveChangesAsync();

            var interactionEngine = new SkincareIngredientInteractionEngine(context, NullLogger<SkincareIngredientInteractionEngine>.Instance);
            var service = new ProductComparisonService(context, interactionEngine, NullLogger<ProductComparisonService>.Instance);

            // Act
            var matrix = await service.GetDetailedComparisonAsync(new List<int> { 101, 102 });

            // Assert
            Assert.Equal(2, matrix.TotalProducts);
            Assert.NotEmpty(matrix.Products);

            // Kiểm tra tính toán Đơn giá / ml
            // SP 101: 600k / 30ml = 20.000₫/ml
            // SP 102: 800k / 100ml = 8.000₫/ml (Kinh tế nhất!)
            var item101 = matrix.Products.First(p => p.Id == 101);
            var item102 = matrix.Products.First(p => p.Id == 102);

            Assert.Equal(20000m, item101.UnitPriceRatio);
            Assert.Equal(8000m, item102.UnitPriceRatio);
            Assert.False(item101.IsBestValuePriceRatio);
            Assert.True(item102.IsBestValuePriceRatio);
            Assert.Equal(item102.Name, matrix.BestValueProductName);

            // Kiểm tra phát hiện tương kỵ Retinol + BHA
            Assert.NotEmpty(matrix.Interactions);
            var retinolBhaConflict = matrix.Interactions.FirstOrDefault(i => i.InteractionType == "HIGH_CONFLICT");
            Assert.NotNull(retinolBhaConflict);
            Assert.Contains("Skin Cycling", matrix.RecommendationSummary);
        }
    }
}
