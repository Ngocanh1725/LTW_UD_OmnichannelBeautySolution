using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Omnichannel.Application.DTOs.Notifications;
using Omnichannel.Application.Interfaces.Notifications;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Services.Notifications;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class ProductSubscriptionTests
    {
        private ApplicationDbContext CreateInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        private Product CreateTestProduct(int id, string name, decimal sellingPrice, decimal? discountPrice = null, int stock = 0)
        {
            return new Product
            {
                Id = id,
                Name = name,
                Slug = $"sp-{id}",
                Sku = $"SKU-{id}",
                Barcode = $"BAR-{id}",
                SellingPrice = sellingPrice,
                DiscountPrice = discountPrice,
                IsActive = true,
                Batches = new List<ProductBatch>
                {
                    new ProductBatch
                    {
                        Id = id * 10,
                        ProductId = id,
                        BatchNumber = $"BATCH-{id}",
                        CurrentQuantity = stock,
                        Status = "ACTIVE",
                        ExpDate = DateTime.UtcNow.AddYears(1)
                    }
                }
            };
        }

        [Fact]
        public async Task Should_Subscribe_BackInStock_Successfully()
        {
            // Arrange
            var db = CreateInMemoryDbContext(nameof(Should_Subscribe_BackInStock_Successfully));
            var product = CreateTestProduct(1, "Serum Vitamin C Kháng Oxy Hóa", 500000m, null, 0);
            db.Products.Add(product);
            await db.SaveChangesAsync();

            var mockNotifService = new Mock<INotificationService>();
            var service = new InventoryNotificationService(db, mockNotifService.Object, NullLogger<InventoryNotificationService>.Instance);

            var req = new SubscribeBackInStockRequest
            {
                ProductId = 1,
                Email = "customer@omnichannel.com",
                PhoneNumber = "0912345678",
                CustomerName = "Nguyễn Văn A"
            };

            // Act
            var result = await service.SubscribeBackInStockAsync(req, 10);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.SubscriptionId);
            Assert.False(result.AlreadySubscribed);

            var saved = await db.ProductSubscriptions.FirstOrDefaultAsync(s => s.Id == result.SubscriptionId.Value);
            Assert.NotNull(saved);
            Assert.Equal(1, saved.ProductId);
            Assert.Equal(10, saved.UserId);
            Assert.Equal("BACK_IN_STOCK", saved.Type);
            Assert.Equal("ACTIVE", saved.Status);
        }

        [Fact]
        public async Task Should_Not_Duplicate_Active_BackInStock_Subscription()
        {
            // Arrange
            var db = CreateInMemoryDbContext(nameof(Should_Not_Duplicate_Active_BackInStock_Subscription));
            var product = CreateTestProduct(2, "Kem Dưỡng B5 Phục Hồi", 400000m, null, 0);
            db.Products.Add(product);

            db.ProductSubscriptions.Add(new ProductSubscription
            {
                ProductId = 2,
                UserId = 20,
                Type = "BACK_IN_STOCK",
                Email = "user20@gmail.com",
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var mockNotifService = new Mock<INotificationService>();
            var service = new InventoryNotificationService(db, mockNotifService.Object, NullLogger<InventoryNotificationService>.Instance);

            var req = new SubscribeBackInStockRequest
            {
                ProductId = 2,
                Email = "user20@gmail.com"
            };

            // Act
            var result = await service.SubscribeBackInStockAsync(req, 20);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.AlreadySubscribed);
            Assert.Contains("Bạn đã đăng ký nhận thông báo", result.Message);

            var count = await db.ProductSubscriptions.CountAsync(s => s.ProductId == 2 && s.UserId == 20);
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task Should_Subscribe_PriceDrop_Successfully_When_TargetPrice_Is_Lower()
        {
            // Arrange
            var db = CreateInMemoryDbContext(nameof(Should_Subscribe_PriceDrop_Successfully_When_TargetPrice_Is_Lower));
            var product = CreateTestProduct(3, "Nước Tẩy Trang Rau Má Centella", 300000m, null, 50);
            db.Products.Add(product);
            await db.SaveChangesAsync();

            var mockNotifService = new Mock<INotificationService>();
            var service = new InventoryNotificationService(db, mockNotifService.Object, NullLogger<InventoryNotificationService>.Instance);

            var req = new SubscribePriceDropRequest
            {
                ProductId = 3,
                TargetPrice = 250000m,
                Email = "shopper@gmail.com",
                CustomerName = "Trần Thị B"
            };

            // Act
            var result = await service.SubscribePriceDropAsync(req, 30);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.SubscriptionId);

            var saved = await db.ProductSubscriptions.FindAsync(result.SubscriptionId.Value);
            Assert.NotNull(saved);
            Assert.Equal("PRICE_DROP", saved.Type);
            Assert.Equal(250000m, saved.TargetPrice);
            Assert.Equal("ACTIVE", saved.Status);
        }

        [Fact]
        public async Task Should_Reject_PriceDrop_When_TargetPrice_Higher_Or_Equal_CurrentPrice()
        {
            // Arrange
            var db = CreateInMemoryDbContext(nameof(Should_Reject_PriceDrop_When_TargetPrice_Higher_Or_Equal_CurrentPrice));
            var product = CreateTestProduct(4, "Kem Chống Nắng Phổ Rộng", 350000m, null, 20);
            db.Products.Add(product);
            await db.SaveChangesAsync();

            var mockNotifService = new Mock<INotificationService>();
            var service = new InventoryNotificationService(db, mockNotifService.Object, NullLogger<InventoryNotificationService>.Instance);

            var req = new SubscribePriceDropRequest
            {
                ProductId = 4,
                TargetPrice = 360000m, // Cao hơn giá hiện tại 350.000đ
                Email = "test@omnichannel.com"
            };

            // Act
            var result = await service.SubscribePriceDropAsync(req, 40);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("phải thấp hơn giá hiện tại", result.Message);
        }

        [Fact]
        public async Task Should_Update_TargetPrice_When_Subscribing_Again()
        {
            // Arrange
            var db = CreateInMemoryDbContext(nameof(Should_Update_TargetPrice_When_Subscribing_Again));
            var product = CreateTestProduct(5, "Toner Hoa Cúc Calendula", 600000m, null, 15);
            db.Products.Add(product);

            var existing = new ProductSubscription
            {
                ProductId = 5,
                UserId = 50,
                Type = "PRICE_DROP",
                TargetPrice = 550000m,
                Status = "ACTIVE",
                Email = "vip@gmail.com"
            };
            db.ProductSubscriptions.Add(existing);
            await db.SaveChangesAsync();

            var mockNotifService = new Mock<INotificationService>();
            var service = new InventoryNotificationService(db, mockNotifService.Object, NullLogger<InventoryNotificationService>.Instance);

            var req = new SubscribePriceDropRequest
            {
                ProductId = 5,
                TargetPrice = 500000m, // Cập nhật mục tiêu giảm sâu hơn
                Email = "vip@gmail.com"
            };

            // Act
            var result = await service.SubscribePriceDropAsync(req, 50);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.AlreadySubscribed);

            var updated = await db.ProductSubscriptions.FindAsync(existing.Id);
            Assert.NotNull(updated);
            Assert.Equal(500000m, updated.TargetPrice);
        }

        [Fact]
        public async Task Should_Trigger_BackInStock_And_Dispatch_SignalR_Notification()
        {
            // Arrange
            var db = CreateInMemoryDbContext(nameof(Should_Trigger_BackInStock_And_Dispatch_SignalR_Notification));
            var product = CreateTestProduct(6, "Son Dưỡng Dior Addict Lip Glow", 950000m, null, 0);
            db.Products.Add(product);

            db.ProductSubscriptions.Add(new ProductSubscription
            {
                ProductId = 6,
                UserId = 60,
                Type = "BACK_IN_STOCK",
                Status = "ACTIVE",
                Email = "diorfan@gmail.com"
            });
            await db.SaveChangesAsync();

            var mockNotifService = new Mock<INotificationService>();
            mockNotifService.Setup(n => n.CreateAndSendNotificationAsync(It.IsAny<CreateNotificationDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new NotificationDto());

            var service = new InventoryNotificationService(db, mockNotifService.Object, NullLogger<InventoryNotificationService>.Instance);

            // Act
            int notifiedCount = await service.TriggerBackInStockAlertsAsync(6, 30);

            // Assert
            Assert.Equal(1, notifiedCount);

            var sub = await db.ProductSubscriptions.FirstOrDefaultAsync(s => s.ProductId == 6);
            Assert.NotNull(sub);
            Assert.Equal("NOTIFIED", sub.Status);
            Assert.NotNull(sub.NotifiedAt);

            // Xác nhận notification đã được phát với đúng UserId và nội dung có hàng
            mockNotifService.Verify(n => n.CreateAndSendNotificationAsync(
                It.Is<CreateNotificationDto>(dto =>
                    dto.UserId == 60 &&
                    dto.Type == "INVENTORY" &&
                    dto.Severity == "SUCCESS" &&
                    dto.Title.Contains("có hàng trở lại") &&
                    dto.Message.Contains("30 sản phẩm")),
                It.IsAny<CancellationToken>()),
                Times.Once);

            // Gọi lần 2 liên tiếp trên cùng đợt về hàng -> chỉ gửi 1 lần (count = 0)
            int secondCallCount = await service.TriggerBackInStockAlertsAsync(6, 30);
            Assert.Equal(0, secondCallCount);
        }

        [Fact]
        public async Task Should_Trigger_PriceDrop_Only_When_Price_Hits_Target()
        {
            // Arrange
            var db = CreateInMemoryDbContext(nameof(Should_Trigger_PriceDrop_Only_When_Price_Hits_Target));
            var product = CreateTestProduct(7, "Nước Thần SK-II Facial Treatment Essence", 2000000m, null, 10);
            db.Products.Add(product);

            // User 71 kỳ vọng giá giảm <= 1.800.000đ (SẼ được thông báo khi giá giảm còn 1.750.000đ)
            db.ProductSubscriptions.Add(new ProductSubscription
            {
                ProductId = 7,
                UserId = 71,
                Type = "PRICE_DROP",
                TargetPrice = 1800000m,
                Status = "ACTIVE",
                Email = "user71@gmail.com"
            });

            // User 72 kỳ vọng giá giảm sâu <= 1.500.000đ (CHƯA được thông báo vì giá mới 1.750.000đ > 1.500.000đ)
            db.ProductSubscriptions.Add(new ProductSubscription
            {
                ProductId = 7,
                UserId = 72,
                Type = "PRICE_DROP",
                TargetPrice = 1500000m,
                Status = "ACTIVE",
                Email = "user72@gmail.com"
            });

            await db.SaveChangesAsync();

            var mockNotifService = new Mock<INotificationService>();
            mockNotifService.Setup(n => n.CreateAndSendNotificationAsync(It.IsAny<CreateNotificationDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new NotificationDto());

            var service = new InventoryNotificationService(db, mockNotifService.Object, NullLogger<InventoryNotificationService>.Instance);

            // Act: Giá giảm xuống 1.750.000đ
            int notifiedCount = await service.TriggerPriceDropAlertsAsync(7, 1750000m, 2000000m);

            // Assert
            Assert.Equal(1, notifiedCount);

            var sub71 = await db.ProductSubscriptions.FirstOrDefaultAsync(s => s.UserId == 71);
            Assert.NotNull(sub71);
            Assert.Equal("NOTIFIED", sub71.Status);

            var sub72 = await db.ProductSubscriptions.FirstOrDefaultAsync(s => s.UserId == 72);
            Assert.NotNull(sub72);
            Assert.Equal("ACTIVE", sub72.Status); // Vẫn ở trạng thái ACTIVE chờ giảm sâu hơn

            mockNotifService.Verify(n => n.CreateAndSendNotificationAsync(
                It.Is<CreateNotificationDto>(dto => dto.UserId == 71 && dto.Type == "PROMOTION"),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Should_Allow_Customer_To_Unsubscribe()
        {
            // Arrange
            var db = CreateInMemoryDbContext(nameof(Should_Allow_Customer_To_Unsubscribe));
            var sub = new ProductSubscription
            {
                Id = 100,
                ProductId = 8,
                UserId = 88,
                Type = "BACK_IN_STOCK",
                Status = "ACTIVE"
            };
            db.ProductSubscriptions.Add(sub);
            await db.SaveChangesAsync();

            var mockNotifService = new Mock<INotificationService>();
            var service = new InventoryNotificationService(db, mockNotifService.Object, NullLogger<InventoryNotificationService>.Instance);

            // Act
            var success = await service.UnsubscribeAsync(100, 88);

            // Assert
            Assert.True(success);

            var updated = await db.ProductSubscriptions.FindAsync(100);
            Assert.NotNull(updated);
            Assert.Equal("CANCELLED", updated.Status);
        }

        [Fact]
        public async Task Should_Require_Email_Or_Phone_When_User_Is_Not_Logged_In()
        {
            // Arrange
            var db = CreateInMemoryDbContext(nameof(Should_Require_Email_Or_Phone_When_User_Is_Not_Logged_In));
            var product = CreateTestProduct(9, "Mặt Nạ Giấy Cấp Ẩm Hyaluronic", 30000m, null, 0);
            db.Products.Add(product);
            await db.SaveChangesAsync();

            var mockNotifService = new Mock<INotificationService>();
            var service = new InventoryNotificationService(db, mockNotifService.Object, NullLogger<InventoryNotificationService>.Instance);

            var req = new SubscribeBackInStockRequest
            {
                ProductId = 9,
                Email = null,
                PhoneNumber = null
            };

            // Act
            var result = await service.SubscribeBackInStockAsync(req, null); // Khách vãng lai, không userId, không email

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Vui lòng đăng nhập hoặc nhập Email", result.Message);
        }
    }
}

