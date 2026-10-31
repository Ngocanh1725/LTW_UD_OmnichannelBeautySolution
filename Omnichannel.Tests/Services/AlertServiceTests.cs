using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Omnichannel.Application.DTOs.Alerts;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Services;
using Omnichannel.Tests.Helpers;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class AlertServiceTests
    {
        [Fact]
        public async Task CheckAndGenerateAlertsAsync_DetectsOutOfStock_GeneratesDangerAlert()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new AlertService(context, NullLogger<AlertService>.Instance);

            var store = new Store { Name = "Chi nhánh Quận 1", Code = "ST-Q1", Address = "123 Lê Lợi" };
            var cat = new Category { Name = "Dưỡng Da", Slug = "duong-da" };
            context.Stores.Add(store);
            context.Categories.Add(cat);
            await context.SaveChangesAsync();

            var product = new Product
            {
                Name = "Serum Phục Hồi B5",
                Sku = "SRM-B5",
                Slug = "serum-b5",
                CategoryId = cat.Id,
                SellingPrice = 400000m,
                IsActive = true
            };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var batch = new ProductBatch
            {
                ProductId = product.Id,
                BatchNumber = "LOT-B5-001",
                ExpDate = DateTime.UtcNow.AddMonths(12),
                CurrentQuantity = 0
            };
            context.ProductBatches.Add(batch);
            await context.SaveChangesAsync();

            var storeInv = new StoreInventory
            {
                StoreId = store.Id,
                ProductBatchId = batch.Id,
                PhysicalQuantity = 0, // Stock = 0
                ReservedQuantity = 0
            };
            context.StoreInventories.Add(storeInv);
            await context.SaveChangesAsync();

            // Act
            var count = await service.CheckAndGenerateAlertsAsync();

            // Assert
            count.Should().BeGreaterThanOrEqualTo(1);
            var alert = await context.Alerts.FirstOrDefaultAsync(a => a.Type == "OUT_OF_STOCK");
            alert.Should().NotBeNull();
            alert!.Severity.Should().Be("DANGER");
            alert.Message.Should().Contain("đã HẾT KHO");
            alert.Message.Should().Contain("Serum Phục Hồi B5");
            alert.Message.Should().Contain("Chi nhánh Quận 1");
        }

        [Fact]
        public async Task CheckAndGenerateAlertsAsync_DetectsLowStock_GeneratesWarningAlert()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new AlertService(context, NullLogger<AlertService>.Instance);

            var store = new Store { Name = "Chi nhánh Cầu Giấy", Code = "ST-CG", Address = "456 Cầu Giấy" };
            var cat = new Category { Name = "Chống Nắng", Slug = "chong-nang" };
            context.Stores.Add(store);
            context.Categories.Add(cat);
            await context.SaveChangesAsync();

            var product = new Product
            {
                Name = "Kem Chống Nắng Anessa",
                Sku = "ANE-01",
                Slug = "kem-chong-nang-anessa",
                CategoryId = cat.Id,
                SellingPrice = 550000m,
                IsActive = true
            };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var batch = new ProductBatch
            {
                ProductId = product.Id,
                BatchNumber = "LOT-ANE-002",
                ExpDate = DateTime.UtcNow.AddMonths(8),
                CurrentQuantity = 5
            };
            context.ProductBatches.Add(batch);
            await context.SaveChangesAsync();

            var storeInv = new StoreInventory
            {
                StoreId = store.Id,
                ProductBatchId = batch.Id,
                PhysicalQuantity = 4, // Stock <= 10
                ReservedQuantity = 0
            };
            context.StoreInventories.Add(storeInv);
            await context.SaveChangesAsync();

            // Act
            var count = await service.CheckAndGenerateAlertsAsync();

            // Assert
            var alert = await context.Alerts.FirstOrDefaultAsync(a => a.Type == "LOW_STOCK");
            alert.Should().NotBeNull();
            alert!.Severity.Should().Be("WARNING");
            alert.Message.Should().Contain("chỉ còn 4 — cần nhập thêm");
        }

        [Fact]
        public async Task CheckAndGenerateAlertsAsync_DetectsCriticalExpiryWithin15Days_GeneratesDangerAlert()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new AlertService(context, NullLogger<AlertService>.Instance);

            var cat = new Category { Name = "Toner", Slug = "toner" };
            context.Categories.Add(cat);
            await context.SaveChangesAsync();

            var product = new Product
            {
                Name = "Toner Klairs Không Mùi",
                Sku = "KLA-01",
                Slug = "toner-klairs",
                CategoryId = cat.Id,
                SellingPrice = 320000m,
                IsActive = true
            };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var batch = new ProductBatch
            {
                ProductId = product.Id,
                BatchNumber = "LOT-KLA-12D",
                ExpDate = DateTime.UtcNow.Date.AddDays(10), // Expiring in 10 days (<= 15 days)
                CurrentQuantity = 25
            };
            context.ProductBatches.Add(batch);
            await context.SaveChangesAsync();

            // Act
            var count = await service.CheckAndGenerateAlertsAsync();

            // Assert
            var alert = await context.Alerts.FirstOrDefaultAsync(a => a.Type == "EXPIRY_CRITICAL");
            alert.Should().NotBeNull();
            alert!.Severity.Should().Be("DANGER");
            alert.Message.Should().Contain("LOT-KLA-12D");
            alert.Message.Should().Contain("ĐỀ XUẤT xả kho giảm giá 40%");
        }

        [Fact]
        public async Task CheckAndGenerateAlertsAsync_DetectsDelayedPendingOrders_GeneratesInfoAlert()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new AlertService(context, NullLogger<AlertService>.Instance);

            var store = new Store { Name = "Kho Online", Code = "ST-ONL", Address = "Kho Online" };
            context.Stores.Add(store);
            await context.SaveChangesAsync();

            var oldOrder = new Order
            {
                OrderCode = "ORD-DELAY-001",
                StoreId = store.Id,
                OrderStatus = "PENDING",
                PaymentStatus = "UNPAID",
                TotalAmount = 500000m,
                CustomerName = "Nguyễn Văn A",
                CustomerPhone = "0901234567",
                CreatedAt = DateTime.UtcNow.AddHours(-30) // Over 24 hours ago
            };
            context.Orders.Add(oldOrder);
            await context.SaveChangesAsync();

            // Act
            var count = await service.CheckAndGenerateAlertsAsync();

            // Assert
            var alert = await context.Alerts.FirstOrDefaultAsync(a => a.Type == "PENDING_ORDERS_DELAY");
            alert.Should().NotBeNull();
            alert!.Severity.Should().Be("INFO");
            alert.Message.Should().Contain("chờ xác nhận quá 24h");
        }

        [Fact]
        public async Task MarkAsRead_And_MarkAllAsRead_UpdatesStatusCorrectly()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new AlertService(context, NullLogger<AlertService>.Instance);

            var a1 = new Alert { Type = "OUT_OF_STOCK", Title = "A1", Message = "M1", IsRead = false };
            var a2 = new Alert { Type = "LOW_STOCK", Title = "A2", Message = "M2", IsRead = false };
            context.Alerts.AddRange(a1, a2);
            await context.SaveChangesAsync();

            // Act 1: Mark single as read
            var markSuccess = await service.MarkAsReadAsync(a1.Id);
            markSuccess.Should().BeTrue();

            var unreadCount1 = await service.GetUnreadCountAsync();
            unreadCount1.Should().Be(1);

            // Act 2: Mark all as read
            var markedAll = await service.MarkAllAsReadAsync();
            markedAll.Should().Be(1);

            var unreadCount2 = await service.GetUnreadCountAsync();
            unreadCount2.Should().Be(0);
        }

        [Fact]
        public async Task CheckAndGenerateAlertsAsync_AvoidsDuplicateSpam_WithinTimeWindow()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new AlertService(context, NullLogger<AlertService>.Instance);

            var cat = new Category { Name = "Dưỡng Da", Slug = "duong-da" };
            context.Categories.Add(cat);
            await context.SaveChangesAsync();

            var product = new Product
            {
                Name = "Serum Rau Má",
                Sku = "SRM-RM",
                Slug = "serum-rau-ma",
                CategoryId = cat.Id,
                SellingPrice = 300000m,
                IsActive = true
            };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var batch = new ProductBatch
            {
                ProductId = product.Id,
                BatchNumber = "LOT-RM-01",
                ExpDate = DateTime.UtcNow.Date.AddDays(5),
                CurrentQuantity = 10
            };
            context.ProductBatches.Add(batch);
            await context.SaveChangesAsync();

            // Run first time -> should create 1 alert
            var count1 = await service.CheckAndGenerateAlertsAsync();
            count1.Should().BeGreaterThanOrEqualTo(1);

            // Run immediately again -> should detect recent alert and NOT create duplicate
            var count2 = await service.CheckAndGenerateAlertsAsync();
            count2.Should().Be(0);
        }
    }
}
