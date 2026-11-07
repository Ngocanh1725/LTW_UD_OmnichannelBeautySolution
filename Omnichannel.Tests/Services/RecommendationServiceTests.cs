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
    public class RecommendationServiceTests
    {
        private (RecommendationService service, IMemoryCache cache) CreateService(string dbName)
        {
            var context = TestDbContextFactory.Create(dbName);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var logger = NullLogger<RecommendationService>.Instance;
            var service = new RecommendationService(context, cache, logger);
            return (service, cache);
        }

        [Fact]
        public async Task GetFrequentlyBoughtTogetherAsync_WithCoOccurrenceHistory_CalculatesConfidenceAndLift()
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

                var pA = new Product
                {
                    Name = "Sữa Rửa Mặt Tạo Bọt La Roche-Posay Effaclar",
                    Slug = "sua-rua-mat-effaclar",
                    SellingPrice = 385000,
                    IsActive = true,
                    CategoryId = cat.Id,
                    BrandId = brand.Id,
                    RoutineStep = "1_Cleanser"
                };

                var pB = new Product
                {
                    Name = "Nước Hoa Hồng La Roche-Posay Soothing Lotion",
                    Slug = "toner-soothing-lotion",
                    SellingPrice = 410000,
                    IsActive = true,
                    CategoryId = cat.Id,
                    BrandId = brand.Id,
                    RoutineStep = "2_Toner"
                };

                var pC = new Product
                {
                    Name = "Son Dưỡng Dior Addict Lip Glow",
                    Slug = "son-duong-dior",
                    SellingPrice = 950000,
                    IsActive = true,
                    CategoryId = cat.Id,
                    BrandId = brand.Id
                };

                context.Products.AddRange(pA, pB, pC);
                await context.SaveChangesAsync();

                var store = new Store { Name = "Store 1", Code = "ST-01", Address = "123 Q1", PhoneNumber = "0901" };
                context.Stores.Add(store);
                await context.SaveChangesAsync();

                // Tạo 3 đơn hàng hoàn tất chứa cả pA và pB
                for (int i = 1; i <= 3; i++)
                {
                    var order = new Order
                    {
                        OrderCode = $"ORD-TEST-{i:0000}",
                        StoreId = store.Id,
                        OrderStatus = "COMPLETED",
                        OrderSource = "WEBSITE",
                        TotalAmount = 795000
                    };
                    context.Orders.Add(order);
                    await context.SaveChangesAsync();

                    context.OrderDetails.AddRange(
                        new OrderDetail { OrderId = order.Id, ProductId = pA.Id, Quantity = 1, UnitPrice = pA.SellingPrice },
                        new OrderDetail { OrderId = order.Id, ProductId = pB.Id, Quantity = 1, UnitPrice = pB.SellingPrice }
                    );
                }

                // Thêm 1 đơn hàng độc lập chỉ chứa pC
                var orderC = new Order
                {
                    OrderCode = "ORD-TEST-0004",
                    StoreId = store.Id,
                    OrderStatus = "COMPLETED",
                    OrderSource = "WEBSITE",
                    TotalAmount = 950000
                };
                context.Orders.Add(orderC);
                await context.SaveChangesAsync();
                context.OrderDetails.Add(new OrderDetail { OrderId = orderC.Id, ProductId = pC.Id, Quantity = 1, UnitPrice = pC.SellingPrice });

                await context.SaveChangesAsync();
            }

            var (service, _) = CreateService(dbName);

            // Act: Lấy combo FBT cho sản phẩm pA (Id = 1)
            var result = await service.GetFrequentlyBoughtTogetherAsync(1, top: 1);

            // Assert
            result.Should().NotBeNull();
            result!.IsAlgorithmic.Should().BeTrue();
            result.CurrentProduct.Id.Should().Be(1);
            result.BundleProducts.Should().HaveCount(1);

            var suggested = result.BundleProducts[0];
            suggested.Id.Should().Be(2); // pB
            suggested.CoOccurrenceCount.Should().Be(3);
            suggested.Confidence.Should().Be(1.0); // 3/3 = 100%
            suggested.Lift.Should().BeGreaterThan(1.0); // Lift = (3*4)/(3*3) = 1.33
            result.BundleOrderCount.Should().Be(3);
            result.Explanation.Should().Contain("3 đơn hàng");
            result.TotalBundlePrice.Should().BeLessThanOrEqualTo(result.TotalOriginalPrice);
        }

        [Fact]
        public async Task GetFrequentlyBoughtTogetherAsync_WithoutOrderHistory_FallsBackToSkincareRoutine()
        {
            // Arrange
            string dbName = Guid.NewGuid().ToString();
            using (var context = TestDbContextFactory.Create(dbName))
            {
                var cat = new Category { Name = "Dưỡng Da", Slug = "duong-da", IsActive = true };
                var brand = new Brand { Name = "Brand X", Slug = "brand-x", IsActive = true };
                context.Categories.Add(cat);
                context.Brands.Add(brand);
                await context.SaveChangesAsync();

                var pCleanser = new Product
                {
                    Name = "Gel Rửa Mặt Tràm Trà",
                    Slug = "gel-rua-mat-tram-tra",
                    SellingPrice = 180000,
                    IsActive = true,
                    CategoryId = cat.Id,
                    BrandId = brand.Id,
                    RoutineStep = "1_Cleanser"
                };

                var pToner = new Product
                {
                    Name = "Nước Cân Bằng Dịu Da",
                    Slug = "nuoc-can-bang-diu-da",
                    SellingPrice = 220000,
                    IsActive = true,
                    CategoryId = cat.Id,
                    BrandId = brand.Id,
                    RoutineStep = "2_Toner"
                };

                var pMoisturizer = new Product
                {
                    Name = "Kem Dưỡng Khóa Ẩm",
                    Slug = "kem-duong-khoa-am",
                    SellingPrice = 350000,
                    IsActive = true,
                    CategoryId = cat.Id,
                    BrandId = brand.Id,
                    RoutineStep = "4_Moisturizer"
                };

                context.Products.AddRange(pCleanser, pToner, pMoisturizer);
                await context.SaveChangesAsync();
                // Không tạo đơn hàng nào -> Phải fallback theo Skincare Routine
            }

            var (service, _) = CreateService(dbName);

            // Act: Lấy combo FBT cho Cleanser
            var result = await service.GetFrequentlyBoughtTogetherAsync(1, top: 2);

            // Assert
            result.Should().NotBeNull();
            result!.IsAlgorithmic.Should().BeFalse();
            result.BundleProducts.Should().HaveCount(2);

            // Gợi ý bước 2 (Toner) và bước 4 (Moisturizer)
            var routineSteps = result.BundleProducts.Select(p => p.RoutineStep).ToList();
            routineSteps.Should().Contain("2_Toner");
            routineSteps.Should().Contain("4_Moisturizer");
            result.Explanation.Should().Contain("chu trình chăm sóc da");
        }

        [Fact]
        public async Task GetProductsByIdsAsync_PreservesInputOrder_AndFiltersInactiveProducts()
        {
            // Arrange
            string dbName = Guid.NewGuid().ToString();
            using (var context = TestDbContextFactory.Create(dbName))
            {
                var cat = new Category { Name = "Son Môi", Slug = "son-moi", IsActive = true };
                var brand = new Brand { Name = "MAC", Slug = "mac", IsActive = true };
                context.Categories.Add(cat);
                context.Brands.Add(brand);
                await context.SaveChangesAsync();

                context.Products.AddRange(
                    new Product { Id = 10, Name = "Son Thỏi 10", Slug = "son-10", SellingPrice = 500000, IsActive = true, CategoryId = cat.Id, BrandId = brand.Id },
                    new Product { Id = 20, Name = "Son Thỏi 20", Slug = "son-20", SellingPrice = 520000, IsActive = false, CategoryId = cat.Id, BrandId = brand.Id }, // Inactive!
                    new Product { Id = 30, Name = "Son Thỏi 30", Slug = "son-30", SellingPrice = 550000, IsActive = true, CategoryId = cat.Id, BrandId = brand.Id },
                    new Product { Id = 40, Name = "Son Thỏi 40", Slug = "son-40", SellingPrice = 580000, IsActive = true, CategoryId = cat.Id, BrandId = brand.Id }
                );
                await context.SaveChangesAsync();
            }

            var (service, _) = CreateService(dbName);

            // Act: Yêu cầu theo thứ tự [40, 10, 20, 999, 30]
            var result = await service.GetProductsByIdsAsync(new[] { 40, 10, 20, 999, 30 });

            // Assert:
            // 1. Phải giữ đúng thứ tự [40, 10, 30]
            // 2. Sản phẩm 20 (inactive) và 999 (không tồn tại) phải bị loại bỏ
            result.Should().HaveCount(3);
            result[0].Id.Should().Be(40);
            result[1].Id.Should().Be(10);
            result[2].Id.Should().Be(30);
        }

        [Fact]
        public async Task GetFrequentlyBoughtTogetherAsync_WhenProductInactiveOrNotFound_ReturnsNull()
        {
            // Arrange
            string dbName = Guid.NewGuid().ToString();
            var (service, _) = CreateService(dbName);

            // Act
            var resNonExistent = await service.GetFrequentlyBoughtTogetherAsync(9999);
            var resNegative = await service.GetFrequentlyBoughtTogetherAsync(-5);

            // Assert
            resNonExistent.Should().BeNull();
            resNegative.Should().BeNull();
        }
    }
}

