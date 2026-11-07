using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Services;
using Omnichannel.Tests.Helpers;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class DemoResetServiceTests
    {
        private IConfiguration CreateConfig(bool enabled = true)
        {
            var myConfiguration = new Dictionary<string, string?>
            {
                { "DemoMode:Enabled", enabled.ToString() }
            };
            return new ConfigurationBuilder()
                .AddInMemoryCollection(myConfiguration)
                .Build();
        }

        [Fact]
        public async Task ResetDemoDataAsync_WhenDemoModeDisabled_ReturnsFailure()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var config = CreateConfig(false);
            var service = new DemoResetService(context, config, NullLogger<DemoResetService>.Instance);

            // Act
            var result = await service.ResetDemoDataAsync(1, "SuperAdmin");

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("DemoMode đang tắt");
        }

        [Fact]
        public async Task ResetDemoDataAsync_RemovesNonSeedOrders_AndPreservesSeedOrders()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var config = CreateConfig(true);
            var service = new DemoResetService(context, config, NullLogger<DemoResetService>.Instance);

            var store = new Store { Name = "Store 1", Code = "SH-01", Address = "123 Q1", PhoneNumber = "0901" };
            context.Stores.Add(store);
            await context.SaveChangesAsync();

            // Seed order (should be preserved)
            var seedOrder = new Order
            {
                OrderCode = "ORD-SEED-20261101-0001",
                StoreId = store.Id,
                CustomerName = "Seed Cust",
                CustomerPhone = "0900000001",
                OrderStatus = "COMPLETED",
                PaymentStatus = "PAID",
                TotalAmount = 500000m
            };

            // Non-seed rehearsal order (should be deleted)
            var testOrder = new Order
            {
                OrderCode = "ORD-TEST-20261104-0099",
                StoreId = store.Id,
                CustomerName = "Rehearsal Cust",
                CustomerPhone = "0900000002",
                OrderStatus = "COMPLETED",
                PaymentStatus = "PAID",
                TotalAmount = 300000m
            };

            context.Orders.AddRange(seedOrder, testOrder);
            await context.SaveChangesAsync();

            // Act
            var result = await service.ResetDemoDataAsync(1, "SuperAdmin");

            // Assert
            result.Success.Should().BeTrue();
            result.DeletedOrdersCount.Should().Be(1);

            var remainingOrders = await context.Orders.ToListAsync();
            remainingOrders.Should().HaveCount(1);
            remainingOrders[0].OrderCode.Should().Be("ORD-SEED-20261101-0001");
        }

        [Fact]
        public async Task ResetDemoDataAsync_RestoresPhysicalStock_AndPurgesCarts()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var config = CreateConfig(true);
            var service = new DemoResetService(context, config, NullLogger<DemoResetService>.Instance);

            var store = new Store { Name = "Store 1", Code = "SH-01", Address = "123 Q1", PhoneNumber = "0901" };
            context.Stores.Add(store);

            var batch = new ProductBatch
            {
                BatchNumber = "BATCH-TEST-01",
                InitialQuantity = 100,
                CurrentQuantity = 10,
                ManufacturingDate = DateTime.UtcNow.AddMonths(-2),
                ExpDate = DateTime.UtcNow.AddYears(2),
                Status = "ACTIVE"
            };
            context.ProductBatches.Add(batch);
            await context.SaveChangesAsync();

            var storeInv = new StoreInventory
            {
                StoreId = store.Id,
                ProductBatchId = batch.Id,
                PhysicalQuantity = 10, // low
                ReservedQuantity = 5
            };
            context.StoreInventories.Add(storeInv);

            // Rehearsal order with detail
            var rehearsalOrder = new Order
            {
                OrderCode = "ORD-REHEARSE-001",
                StoreId = store.Id,
                CustomerName = "Tester",
                CustomerPhone = "0912345678",
                OrderStatus = "COMPLETED",
                PaymentStatus = "PAID"
            };
            var detail = new OrderDetail
            {
                Order = rehearsalOrder,
                ProductId = 1,
                ProductBatchId = batch.Id,
                Quantity = 5,
                UnitPrice = 100000m,
                LineTotal = 500000m
            };
            rehearsalOrder.OrderDetails.Add(detail);
            context.Orders.Add(rehearsalOrder);

            // Stray cart
            var cart = new Cart { CartSessionId = "test-session" };
            var cartItem = new CartItem { Cart = cart, ProductId = 1, Quantity = 2 };
            cart.CartItems.Add(cartItem);
            context.Carts.Add(cart);
            await context.SaveChangesAsync();

            // Act
            var result = await service.ResetDemoDataAsync(1, "SuperAdmin");

            // Assert
            result.Success.Should().BeTrue();
            result.DeletedOrdersCount.Should().Be(1);
            result.PurgedCartsCount.Should().Be(1);

            // Verify cart purged
            (await context.Carts.CountAsync()).Should().Be(0);
            (await context.CartItems.CountAsync()).Should().Be(0);

            // Verify order deleted
            (await context.Orders.CountAsync()).Should().Be(0);

            // Verify audit log created
            var auditLog = await context.AuditLogs.FirstOrDefaultAsync(a => a.Action == "RESET_DEMO_DATA");
            auditLog.Should().NotBeNull();
            auditLog!.UserName.Should().Be("SuperAdmin");
        }
    }
}
