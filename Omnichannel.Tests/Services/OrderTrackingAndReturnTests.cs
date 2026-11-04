using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Omnichannel.Application.DTOs.Inventory;
using Omnichannel.Application.DTOs.Notifications;
using Omnichannel.Application.Interfaces.Notifications;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Services;
using Omnichannel.Infrastructure.Services.Sales;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class OrderTrackingAndReturnTests
    {
        private static ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"OrderTrackingTests_{Guid.NewGuid():N}")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new ApplicationDbContext(options);
        }

        private static async Task<(Store Store, Product Product, ProductBatch BatchA, ProductBatch BatchB, User CustomerA, User CustomerB)> SeedBaseDataAsync(ApplicationDbContext db)
        {
            var store = new Store
            {
                Id = 1,
                Code = "HQ-01",
                Name = "Showroom Quận 1",
                Address = "123 Nguyễn Huệ, Quận 1, TP.HCM",
                IsActive = true,
                IsHeadquarters = true
            };

            var customerA = new User
            {
                Id = 10,
                Username = "customerA",
                Email = "a@omnibeauty.vn",
                FullName = "Nguyễn Khách Hàng A",
                PhoneNumber = "0901112222",
                PasswordHash = "hash",
                IsActive = true
            };

            var customerB = new User
            {
                Id = 20,
                Username = "customerB",
                Email = "b@omnibeauty.vn",
                FullName = "Trần Khách Hàng B",
                PhoneNumber = "0908889999",
                PasswordHash = "hash",
                IsActive = true
            };

            var product = new Product
            {
                Id = 100,
                Name = "Serum La Roche-Posay Hyalu B5 30ml",
                Slug = "serum-b5-30ml",
                Sku = "LRP-B5-30",
                SellingPrice = 850000m,
                CostPrice = 500000m,
                IsActive = true
            };

            var batchA = new ProductBatch
            {
                Id = 501,
                ProductId = 100,
                BatchNumber = "LOT-FEFO-NEAR-01",
                ManufacturingDate = DateTime.UtcNow.AddMonths(-6),
                ExpDate = DateTime.UtcNow.AddMonths(8),
                InitialQuantity = 50,
                CurrentQuantity = 30,
                Status = "ACTIVE"
            };

            var batchB = new ProductBatch
            {
                Id = 502,
                ProductId = 100,
                BatchNumber = "LOT-FEFO-FAR-02",
                ManufacturingDate = DateTime.UtcNow.AddMonths(-2),
                ExpDate = DateTime.UtcNow.AddMonths(24),
                InitialQuantity = 50,
                CurrentQuantity = 50,
                Status = "ACTIVE"
            };

            var invA = new StoreInventory
            {
                Id = 1001,
                StoreId = 1,
                ProductBatchId = 501,
                PhysicalQuantity = 30,
                ReservedQuantity = 5 // 5 units reserved for an active order
            };

            var invB = new StoreInventory
            {
                Id = 1002,
                StoreId = 1,
                ProductBatchId = 502,
                PhysicalQuantity = 50,
                ReservedQuantity = 0
            };

            await db.Stores.AddAsync(store);
            await db.Users.AddRangeAsync(customerA, customerB);
            await db.Products.AddAsync(product);
            await db.ProductBatches.AddRangeAsync(batchA, batchB);
            await db.StoreInventories.AddRangeAsync(invA, invB);
            await db.SaveChangesAsync();

            return (store, product, batchA, batchB, customerA, customerB);
        }

        private static (OrderManagementService OrderService, Mock<INotificationService> NotifMock) CreateServices(ApplicationDbContext db)
        {
            var invEngine = new InventoryAllocationEngine(db);
            var cartMock = new Mock<ICartService>();
            var notifMock = new Mock<INotificationService>();
            var shippingMock = new Mock<IShippingService>();
            shippingMock
                .Setup(s => s.CreateShippingOrderAsync(It.IsAny<Order>(), It.IsAny<int>()))
                .ReturnsAsync((Order o, int _) => (true, $"GHN261104{o.Id:D4}", "https://print.ghn.vn", "OK"));

            var workflow = new OrderWorkflowService(
                db,
                invEngine,
                cartMock.Object,
                NullLogger<OrderWorkflowService>.Instance,
                notificationService: notifMock.Object);

            var orderService = new OrderManagementService(
                db,
                workflow,
                NullLogger<OrderManagementService>.Instance,
                notifMock.Object,
                shippingMock.Object);

            return (orderService, notifMock);
        }

        [Fact]
        public async Task CancelOrderByCustomer_WhenOrderIsCompletedOrShipping_ShouldRejectCancellation()
        {
            using var db = CreateInMemoryContext();
            var (_, product, batchA, _, customerA, _) = await SeedBaseDataAsync(db);
            var (orderService, _) = CreateServices(db);

            var deliveredOrder = new Order
            {
                Id = 1,
                OrderCode = "ORD-20261104-DELIV",
                UserId = customerA.Id,
                StoreId = 1,
                OrderSource = "WEBSITE",
                OrderStatus = "COMPLETED",
                PaymentStatus = "PAID",
                CustomerName = customerA.FullName,
                CustomerPhone = customerA.PhoneNumber!,
                TotalAmount = 850000m,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            };
            deliveredOrder.OrderDetails.Add(new OrderDetail
            {
                ProductId = product.Id,
                ProductBatchId = batchA.Id,
                Quantity = 2,
                UnitPrice = 850000m,
                LineTotal = 1700000m
            });
            await db.Orders.AddAsync(deliveredOrder);
            await db.SaveChangesAsync();

            var (success, message) = await orderService.CancelOrderByCustomerAsync(
                deliveredOrder.Id, customerA.Id, "Đổi ý sau khi nhận hàng");

            Assert.False(success);
            Assert.Contains("Đã giao thành công", message);

            var orderAfter = await db.Orders.FindAsync(deliveredOrder.Id);
            Assert.Equal("COMPLETED", orderAfter!.OrderStatus);
        }

        [Fact]
        public async Task CancelOrderByCustomer_WhenOrderIsPending_ShouldRestoreExactFefoBatchAndRecordHistory()
        {
            using var db = CreateInMemoryContext();
            var (_, product, batchA, batchB, customerA, _) = await SeedBaseDataAsync(db);
            var (orderService, _) = CreateServices(db);

            // Order allocated 3 units from BatchA (Id=501) which currently has ReservedQuantity = 5
            var pendingOrder = new Order
            {
                Id = 2,
                OrderCode = "ORD-20261104-PEND",
                UserId = customerA.Id,
                StoreId = 1,
                OrderSource = "WEBSITE",
                OrderStatus = "PENDING",
                PaymentStatus = "UNPAID",
                CustomerName = customerA.FullName,
                CustomerPhone = customerA.PhoneNumber!,
                TotalAmount = 2550000m,
                CreatedAt = DateTime.UtcNow.AddMinutes(-10)
            };
            pendingOrder.OrderDetails.Add(new OrderDetail
            {
                ProductId = product.Id,
                ProductBatchId = batchA.Id,
                Quantity = 3,
                UnitPrice = 850000m,
                LineTotal = 2550000m
            });
            await db.Orders.AddAsync(pendingOrder);
            await db.SaveChangesAsync();

            var (success, _) = await orderService.CancelOrderByCustomerAsync(
                pendingOrder.Id, customerA.Id, "Muốn đổi địa chỉ nhận hàng");

            Assert.True(success);

            var updatedOrder = await db.Orders
                .Include(o => o.StatusHistories)
                .FirstAsync(o => o.Id == pendingOrder.Id);
            Assert.Equal("CANCELLED", updatedOrder.OrderStatus);
            Assert.Contains(updatedOrder.StatusHistories, h => h.Status == "CANCELLED");

            // Kiểm tra hoàn tồn đúng lô BatchA (ReservedQuantity từ 5 giảm xuống 2), lô BatchB không bị ảnh hưởng
            var invA = await db.StoreInventories.FirstAsync(si => si.ProductBatchId == batchA.Id);
            var invB = await db.StoreInventories.FirstAsync(si => si.ProductBatchId == batchB.Id);
            Assert.Equal(2, invA.ReservedQuantity);
            Assert.Equal(30, invA.PhysicalQuantity);
            Assert.Equal(0, invB.ReservedQuantity);
            Assert.Equal(50, invB.PhysicalQuantity);
        }

        [Fact]
        public async Task OwnershipCheck_CustomerACannotViewOrCancelCustomerBOrder()
        {
            using var db = CreateInMemoryContext();
            var (_, product, batchA, _, customerA, customerB) = await SeedBaseDataAsync(db);
            var (orderService, _) = CreateServices(db);

            var orderOfB = new Order
            {
                Id = 3,
                OrderCode = "ORD-20261104-CUSTB",
                UserId = customerB.Id,
                StoreId = 1,
                OrderSource = "WEBSITE",
                OrderStatus = "PENDING",
                PaymentStatus = "UNPAID",
                CustomerName = customerB.FullName,
                CustomerPhone = customerB.PhoneNumber!,
                TotalAmount = 850000m,
                CreatedAt = DateTime.UtcNow
            };
            orderOfB.OrderDetails.Add(new OrderDetail
            {
                ProductId = product.Id,
                ProductBatchId = batchA.Id,
                Quantity = 1,
                UnitPrice = 850000m,
                LineTotal = 850000m
            });
            await db.Orders.AddAsync(orderOfB);
            await db.SaveChangesAsync();

            // 1. Khách A cố xem chi tiết đơn của Khách B -> Bị chặn
            var (allowedView, viewError, detailVm) = await orderService.GetCustomerOrderDetailAsync(
                orderOfB.Id, null, currentUserId: customerA.Id, isAdmin: false);
            Assert.False(allowedView);
            Assert.Null(detailVm);
            Assert.Contains("không có quyền", viewError!, StringComparison.OrdinalIgnoreCase);

            // 2. Khách A cố hủy đơn của Khách B -> Bị chặn
            var (cancelSuccess, cancelError) = await orderService.CancelOrderByCustomerAsync(
                orderOfB.Id, currentUserId: customerA.Id, reason: "Cố tình hủy đơn người khác");
            Assert.False(cancelSuccess);
            Assert.Contains("không có quyền", cancelError, StringComparison.OrdinalIgnoreCase);

            // Đơn của Khách B vẫn giữ nguyên PENDING
            var orderStillPending = await db.Orders.FindAsync(orderOfB.Id);
            Assert.Equal("PENDING", orderStillPending!.OrderStatus);
        }

        [Fact]
        public async Task GuestOrderLookup_WhenWrongPhoneNumber_ShouldNotLeakOrderInformation()
        {
            using var db = CreateInMemoryContext();
            var (_, product, batchA, _, _, customerB) = await SeedBaseDataAsync(db);
            var (orderService, _) = CreateServices(db);

            var order = new Order
            {
                Id = 4,
                OrderCode = "ORD-20261104-GUEST01",
                UserId = customerB.Id,
                StoreId = 1,
                OrderSource = "WEBSITE",
                OrderStatus = "SHIPPING",
                PaymentStatus = "PAID",
                CustomerName = "Lê Thị Bảo Mật",
                CustomerPhone = "0908889999",
                ShippingAddress = "Số 99 Đường Nguyễn Huệ, Quận 1, TP.HCM",
                TotalAmount = 1700000m,
                CreatedAt = DateTime.UtcNow.AddHours(-5)
            };
            order.OrderDetails.Add(new OrderDetail
            {
                ProductId = product.Id,
                ProductBatchId = batchA.Id,
                Quantity = 2,
                UnitPrice = 850000m,
                LineTotal = 1700000m
            });
            await db.Orders.AddAsync(order);
            await db.SaveChangesAsync();

            // 1. Tra cứu đúng mã đơn nhưng SAI số điện thoại -> Tuyệt đối không lộ OrderDetail
            var wrongPhoneLookup = await orderService.LookupGuestOrderAsync("ORD-20261104-GUEST01", "0901112222");
            Assert.True(wrongPhoneLookup.HasSearched);
            Assert.Null(wrongPhoneLookup.OrderDetail);
            Assert.NotNull(wrongPhoneLookup.ErrorMessage);
            Assert.DoesNotContain("Lê Thị Bảo Mật", wrongPhoneLookup.ErrorMessage);
            Assert.DoesNotContain("Nguyễn Huệ", wrongPhoneLookup.ErrorMessage);

            // 2. Tra cứu ĐÚNG mã đơn + ĐÚNG số điện thoại -> Trả về Timeline 6 bước & che một phần SĐT
            var validLookup = await orderService.LookupGuestOrderAsync("ord-20261104-guest01", "+84 908 889 999");
            Assert.Null(validLookup.ErrorMessage);
            Assert.NotNull(validLookup.OrderDetail);
            Assert.Equal(6, validLookup.OrderDetail!.TimelineEvents.Count);
            Assert.Contains("****", validLookup.OrderDetail.RecipientPhone);
        }

        [Fact]
        public async Task AdminStatusTransition_And_ReturnRequestFlow_ShouldRecordHistoryAndSendRealtimeNotifications()
        {
            using var db = CreateInMemoryContext();
            var (_, product, batchA, _, customerA, _) = await SeedBaseDataAsync(db);
            var (orderService, notifMock) = CreateServices(db);

            var order = new Order
            {
                Id = 5,
                OrderCode = "ORD-20261104-FLOW",
                UserId = customerA.Id,
                StoreId = 1,
                OrderSource = "WEBSITE",
                OrderStatus = "PENDING",
                PaymentStatus = "UNPAID",
                CustomerName = customerA.FullName,
                CustomerPhone = customerA.PhoneNumber!,
                ShippingAddress = "456 Lê Lợi, Quận 1, TP.HCM",
                TotalAmount = 850000m,
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            };
            order.OrderDetails.Add(new OrderDetail
            {
                ProductId = product.Id,
                ProductBatchId = batchA.Id,
                Quantity = 1,
                UnitPrice = 850000m,
                LineTotal = 850000m
            });
            await db.Orders.AddAsync(order);
            await db.SaveChangesAsync();

            // Bước 1 -> 2 -> 3 -> 4/5 -> 6: Admin lần lượt chuyển trạng thái
            Assert.True((await orderService.UpdateOrderStatusAsync(order.Id, "CONFIRMED")).Success);
            Assert.True((await orderService.UpdateOrderStatusAsync(order.Id, "PROCESSING")).Success);
            Assert.True((await orderService.UpdateOrderStatusAsync(order.Id, "SHIPPING")).Success);
            Assert.True((await orderService.UpdateOrderStatusAsync(order.Id, "COMPLETED")).Success);

            // Kiểm tra Timeline 6 bước của khách
            var (allowed, _, detailVm) = await orderService.GetCustomerOrderDetailAsync(order.Id, null, customerA.Id);
            Assert.True(allowed);
            Assert.NotNull(detailVm);
            Assert.Equal(6, detailVm!.TimelineStep);
            Assert.Equal(6, detailVm.TimelineEvents.Count);
            Assert.All(detailVm.TimelineEvents, e => Assert.True(e.IsCompleted));
            Assert.StartsWith("GHN", detailVm.TrackingCode);
            Assert.True(detailVm.CanRequestReturn);

            // Khách gửi yêu cầu đổi trả trong hạn 7 ngày
            var (retSuccess, _, retVm) = await orderService.SubmitReturnRequestAsync(
                order.Id,
                customerA.Id,
                "Sản phẩm bị rò rỉ nắp khi vận chuyển",
                "Nắp chai bị nứt nhẹ",
                new List<string> { "/uploads/returns/sample1.jpg" });

            Assert.True(retSuccess);
            Assert.NotNull(retVm);
            Assert.Equal("PENDING", retVm!.Status);

            // Admin duyệt yêu cầu đổi trả -> Hoàn tồn kho lô FEFO & bắn thông báo SignalR realtime cho khách
            var (procSuccess, _) = await orderService.ProcessReturnRequestAsync(
                retVm.Id, isApproved: true, adminNote: "Đồng ý đổi mới và hoàn tiền 100%", adminUserId: 1);

            Assert.True(procSuccess);

            var updatedOrder = await db.Orders.FindAsync(order.Id);
            Assert.Equal("REFUNDED", updatedOrder!.PaymentStatus);

            notifMock.Verify(n => n.CreateAndSendNotificationAsync(
                It.Is<CreateNotificationDto>(dto => dto.UserId == customerA.Id && dto.Title.Contains("Đã Duyệt Đổi/Trả")),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

