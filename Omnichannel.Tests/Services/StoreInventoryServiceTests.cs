using System;
using System.Linq;
using System.Threading.Tasks;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Services.Inventory;
using Omnichannel.Tests.Helpers;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class StoreInventoryServiceTests
    {
        [Fact]
        public async Task GetProductStockByShowroomsAsync_CalculatesPhysicalMinusReserved_AndDetectsOutOfStock()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();

            var store1 = new Store
            {
                Id = 1,
                Code = "SH-Q1",
                Name = "CANMAKE BEAUTY Flagship Quận 1",
                Address = "128 Hai Bà Trưng, Quận 1, TP.HCM",
                IsActive = true
            };
            var store2 = new Store
            {
                Id = 2,
                Code = "SH-CG",
                Name = "CANMAKE BEAUTY Cầu Giấy",
                Address = "68 Cầu Giấy, Cầu Giấy, Hà Nội",
                IsActive = true
            };
            context.Stores.AddRange(store1, store2);

            var product = new Product
            {
                Id = 101,
                Name = "Serum Cấp Ẩm HA Đa Tầng",
                Sku = "SRM-HA-01",
                SellingPrice = 350000m,
                IsActive = true
            };
            context.Products.Add(product);

            var batch1 = new ProductBatch
            {
                Id = 201,
                ProductId = 101,
                BatchNumber = "LOT-HA-01",
                ExpDate = DateTime.UtcNow.AddYears(2)
            };
            context.ProductBatches.Add(batch1);

            // Store 1: Physical = 10, Reserved = 2 => Available = 8 (>5 => Còn hàng)
            var inv1 = new StoreInventory
            {
                StoreId = 1,
                ProductBatchId = 201,
                PhysicalQuantity = 10,
                ReservedQuantity = 2
            };
            // Store 2: Physical = 3, Reserved = 3 => Available = 0 (Tạm hết hàng)
            var inv2 = new StoreInventory
            {
                StoreId = 2,
                ProductBatchId = 201,
                PhysicalQuantity = 3,
                ReservedQuantity = 3
            };
            context.StoreInventories.AddRange(inv1, inv2);
            await context.SaveChangesAsync();

            var service = new StoreInventoryService(context);

            // Act
            var results = await service.GetProductStockByShowroomsAsync(101);

            // Assert
            Assert.Equal(2, results.Count);

            var q1 = results.First(r => r.StoreId == 1);
            Assert.Equal(8, q1.StockQuantity);
            Assert.True(q1.HasStock);
            Assert.Contains("Còn hàng", q1.StatusText);
            Assert.Equal("TP. Hồ Chí Minh", q1.City);

            var cg = results.First(r => r.StoreId == 2);
            Assert.Equal(0, cg.StockQuantity);
            Assert.False(cg.HasStock);
            Assert.Equal("Tạm hết hàng", cg.StatusText);
            Assert.Equal("Hà Nội", cg.City);
        }

        [Fact]
        public async Task GetProductStockByShowroomsAsync_FiltersByCityCorrectly()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();

            var store1 = new Store { Id = 1, Code = "SH-Q1", Name = "Showroom Quận 1", Address = "Quận 1, TP.HCM", IsActive = true };
            var store2 = new Store { Id = 2, Code = "SH-CG", Name = "Showroom Cầu Giấy", Address = "Cầu Giấy, Hà Nội", IsActive = true };
            context.Stores.AddRange(store1, store2);
            await context.SaveChangesAsync();

            var service = new StoreInventoryService(context);

            // Act
            var hanoiStores = await service.GetProductStockByShowroomsAsync(productId: 999, city: "Hà Nội");
            var hcmStores = await service.GetProductStockByShowroomsAsync(productId: 999, city: "TP. Hồ Chí Minh");

            // Assert
            Assert.Single(hanoiStores);
            Assert.Equal("Hà Nội", hanoiStores[0].City);
            Assert.Equal("SH-CG", hanoiStores[0].StoreCode);

            Assert.Single(hcmStores);
            Assert.Equal("TP. Hồ Chí Minh", hcmStores[0].City);
            Assert.Equal("SH-Q1", hcmStores[0].StoreCode);
        }

        [Fact]
        public async Task GetActiveFlashSaleAsync_ReturnsActiveCampaignWithCountdownAndSoldProgress()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var now = DateTime.UtcNow;

            var promo = new Promotion
            {
                Id = 1,
                PromotionCode = "PROMO-FLASH20",
                Name = "Flash Sale Deal Sốc",
                PromotionType = "FLASH_SALE",
                DiscountValue = 20m,
                ValidFrom = now.AddHours(-1),
                ValidTo = now.AddHours(5),
                IsActive = true,
                AppliesTo = "ALL"
            };
            context.Promotions.Add(promo);

            var cat = new Category { Id = 1, Name = "Chăm sóc da", Slug = "cham-soc-da" };
            context.Categories.Add(cat);

            var prod1 = new Product
            {
                Id = 1,
                CategoryId = 1,
                Sku = "SKU-B5",
                Name = "Kem Dưỡng B5 Phục Hồi",
                SellingPrice = 300000m,
                DiscountPrice = 240000m,
                IsActive = true
            };
            context.Products.Add(prod1);
            await context.SaveChangesAsync();

            var service = new StoreInventoryService(context);

            // Act
            var flashSale = await service.GetActiveFlashSaleAsync();

            // Assert
            Assert.NotNull(flashSale);
            Assert.Equal("PROMO-FLASH20", flashSale.CampaignCode);
            Assert.True(flashSale.EndTime > now);
            Assert.NotEmpty(flashSale.Products);

            var item = flashSale.Products.First();
            Assert.True(item.StockLimit > 0);
            Assert.True(item.SoldCount > 0);
            Assert.True(item.SoldPercentage >= 0 && item.SoldPercentage <= 100);
        }
    }
}
