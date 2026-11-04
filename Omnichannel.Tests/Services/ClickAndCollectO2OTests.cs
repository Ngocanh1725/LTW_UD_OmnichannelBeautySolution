using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Omnichannel.Application.DTOs.Sales;
using Omnichannel.Application.Interfaces.Loyalty;
using Omnichannel.Application.Interfaces.Notifications;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Services;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class ClickAndCollectO2OTests
    {
        private static ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("ClickAndCollectTestDb_" + Guid.NewGuid().ToString("N"))
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new ApplicationDbContext(options);
        }

        private static async Task<(Store StoreCauGiay, Store StoreQuan1, Product Product, ProductBatch BatchCauGiay, PosShift OpenShiftCauGiay)> SeedTwoStoresAndInventoryAsync(ApplicationDbContext db)
        {
            var storeCauGiay = new Store
            {
                Id = 10,
                Code = "SR-HN-CG",
                Name = "Showroom Omni Beauty Cầu Giấy",
                Address = "245 Cầu Giấy, Dịch Vọng, Cầu Giấy, Hà Nội",
                PhoneNumber = "02439998888",
                IsHeadquarters = false,
                IsActive = true
            };

            var storeQuan1 = new Store
            {
                Id = 20,
                Code = "SR-HCM-Q1",
                Name = "Showroom Omni Beauty Quận 1",
                Address = "88 Nguyễn Huệ, Bến Nghé, Quận 1, TP.HCM",
                PhoneNumber = "02839998888",
                IsHeadquarters = false,
                IsActive = true
            };

            var category = new Category
            {
                Id = 1,
                Name = "Tinh chất Serum",
                Slug = "tinh-chat-serum",
                IsActive = true
            };

            var product = new Product
            {
                Id = 101,
                Sku = "SRM-B5-30ML",
                Name = "Tinh Chất Phục Hồi La Roche-Posay Hyalu B5 30ml",
                Slug = "la-roche-posay-hyalu-b5-30ml",
                CategoryId = 1,
                SellingPrice = 850000m,
                IsActive = true
            };

            var batchCauGiay = new ProductBatch
            {
                Id = 501,
                ProductId = 101,
                BatchNumber = "LOT-CG-2027",
                ManufacturingDate = DateTime.UtcNow.AddMonths(-2),
                ExpDate = DateTime.UtcNow.AddMonths(18),
                ImportCost = 500000m,
                InitialQuantity = 20,
                CurrentQuantity = 20,
                Status = "ACTIVE"
            };

            var invCauGiay = new StoreInventory
            {
                Id = 1001,
                StoreId = storeCauGiay.Id,
                ProductBatchId = batchCauGiay.Id,
                PhysicalQuantity = 20,
                ReservedQuantity = 0
            };

            var shiftCauGiay = new PosShift
            {
                Id = 701,
                ShiftCode = "SHIFT-CG-01",
                StoreId = storeCauGiay.Id,
                CashierId = 4,
                OpeningBalance = 1000000m,
                ExpectedCash = 1000000m,
                CashSalesTotal = 0m,
                TransactionCount = 0,
                Status = "OPEN",
                OpenedAt = DateTime.UtcNow.AddHours(-1)
            };

            await db.Stores.AddRangeAsync(storeCauGiay, storeQuan1);
            await db.Categories.AddAsync(category);
            await db.Products.AddAsync(product);
            await db.ProductBatches.AddAsync(batchCauGiay);
            await db.StoreInventories.AddAsync(invCauGiay);
            await db.PosShifts.AddAsync(shiftCauGiay);
            await db.SaveChangesAsync();

            return (storeCauGiay, storeQuan1, product, batchCauGiay, shiftCauGiay);
        }

        private static OrderWorkflowService CreateWorkflowService(
            ApplicationDbContext db,
            Product product,
            int qty = 2,
            Mock<ILoyaltyService>? loyaltyMock = null,
            Mock<INotificationService>? notifMock = null)
        {
            var invEngine = new InventoryAllocationEngine(db);

            var cartServiceMock = new Mock<ICartService>();
            cartServiceMock
                .Setup(c => c.GetCartAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CartDto
                {
                    SubTotal = product.SellingPrice * qty,
                    Items = new List<CartItemDto>
                    {
                        new()
                        {
                            ProductId = product.Id,
                            ProductName = product.Name,
                            UnitPrice = product.SellingPrice,
                            Quantity = qty
                        }
                    }
                });

            return new OrderWorkflowService(
                db,
                invEngine,
                cartServiceMock.Object,
                NullLogger<OrderWorkflowService>.Instance,
                loyaltyMock?.Object,
                promotionEngine: null,
                notificationService: notifMock?.Object,
                scopeFactory: null);
        }

        [Fact]
        public async Task ClickAndCollectOrder_ZeroShippingFee_StructuredPinCode_AndStoreAvailabilityCheck()
        {
            using var db = CreateInMemoryContext();
            var (storeCauGiay, storeQuan1, product, _, _) = await SeedTwoStoresAndInventoryAsync(db);
            var workflow = CreateWorkflowService(db, product, qty: 2);

            // 1. Kiểm tra danh sách cửa hàng đủ hàng cho toàn bộ giỏ: Cầu Giấy đủ hàng (20 SP), Quận 1 không có tồn kho (0 SP)
            var candidates = await workflow.GetStorePickupAvailabilityForCartAsync("CART-TEST-D1", "15");
            var cgCandidate = candidates.First(c => c.StoreId == storeCauGiay.Id);
            var q1Candidate = candidates.First(c => c.StoreId == storeQuan1.Id);

            Assert.True(cgCandidate.HasFullCartStock);
            Assert.Equal(20, cgCandidate.TotalAvailableUnitsForCart);
            Assert.False(q1Candidate.HasFullCartStock);

            // 2. Đặt đơn Click & Collect tại Cầu Giấy (Đơn dưới 500k nếu giao thường sẽ mất 30k ship, nhưng Click & Collect phải = 0đ)
            product.SellingPrice = 200000m;
            await db.SaveChangesAsync();
            var workflowUnder500k = CreateWorkflowService(db, product, qty: 1);

            var checkoutVm = new CheckoutViewModel
            {
                RecipientName = "Lê Ngọc Linh",
                RecipientPhone = "0988112233",
                DeliveryMethod = "STORE_PICKUP",
                ShippingMethod = "STORE_PICKUP",
                PickupStoreId = storeCauGiay.Id,
                PaymentMethod = 4 // Thanh toán tại quầy / COD
            };

            var (success, _, result) = await workflowUnder500k.CreateOrderWithSoftReserveAsync("CART-TEST-D1", checkoutVm, "15");
            Assert.True(success);
            Assert.NotNull(result);

            var savedOrder = await db.Orders.FirstAsync(o => o.OrderCode == result!.OrderId);
            Assert.Equal("STORE_PICKUP", savedOrder.DeliveryMethod);
            Assert.Equal(storeCauGiay.Id, savedOrder.PickupStoreId);
            Assert.Equal(0m, savedOrder.ShippingFee);
            Assert.Equal(200000m, savedOrder.TotalAmount);
            Assert.NotNull(savedOrder.PickupCode);
            Assert.Equal(6, savedOrder.PickupCode!.Length);
            Assert.NotNull(savedOrder.PickupExpiresAt);

            // Đảm bảo KHÔNG nhồi mã PIN vào chuỗi ShippingAddress
            Assert.DoesNotContain(savedOrder.PickupCode, savedOrder.ShippingAddress ?? string.Empty);

            // Tồn kho tại Cầu Giấy đã giữ tạm (ReservedQuantity = 1, PhysicalQuantity vẫn = 20)
            var invAfterOrder = await db.StoreInventories.FirstAsync(si => si.StoreId == storeCauGiay.Id);
            Assert.Equal(20, invAfterOrder.PhysicalQuantity);
            Assert.Equal(1, invAfterOrder.ReservedQuantity);
        }

        [Fact]
        public async Task HandoverPickupOrder_WrongPin_AndCrossStoreStaff_AreRejected()
        {
            using var db = CreateInMemoryContext();
            var (storeCauGiay, storeQuan1, product, _, shiftCauGiay) = await SeedTwoStoresAndInventoryAsync(db);
            var workflow = CreateWorkflowService(db, product, qty: 2);

            var (_, _, created) = await workflow.CreateOrderWithSoftReserveAsync("CART-TEST-D1", new CheckoutViewModel
            {
                RecipientName = "Trần Minh Anh",
                RecipientPhone = "0912345678",
                DeliveryMethod = "STORE_PICKUP",
                PickupStoreId = storeCauGiay.Id,
                PaymentMethod = 1
            }, "15");

            var order = await db.Orders.FirstAsync(o => o.OrderCode == created!.OrderId);

            // 1. Nhân viên cửa hàng Quận 1 (StoreId = 20) cố tình bàn giao đơn của Cầu Giấy (StoreId = 10) -> Bị chặn bởi Store Isolation
            var crossStoreAttempt = await workflow.HandoverPickupOrderAtPosAsync(new HandoverPickupOrderRequestDto
            {
                OrderId = order.Id,
                OrderCode = order.OrderCode,
                PickupCode = order.PickupCode!,
                CashierStoreId = storeQuan1.Id,
                CashierUserId = 99,
                CashierName = "Thu ngân Quận 1"
            });

            Assert.False(crossStoreAttempt.Success);
            Assert.Contains("Store Isolation", crossStoreAttempt.Message, StringComparison.OrdinalIgnoreCase);

            // 2. Nhân viên đúng cửa hàng Cầu Giấy nhưng nhập SAI mã PIN -> Bị từ chối
            var wrongPinAttempt = await workflow.HandoverPickupOrderAtPosAsync(new HandoverPickupOrderRequestDto
            {
                OrderId = order.Id,
                OrderCode = order.OrderCode,
                PickupCode = "000000", // Sai PIN
                CashierStoreId = storeCauGiay.Id,
                CashierUserId = 4,
                CashierName = "Thu ngân Cầu Giấy",
                PosShiftId = shiftCauGiay.Id
            });

            Assert.False(wrongPinAttempt.Success);
            Assert.Contains("Mã PIN", wrongPinAttempt.Message, StringComparison.OrdinalIgnoreCase);

            // Đơn vẫn ở trạng thái PENDING, tồn vật lý chưa bị trừ
            var unchangedOrder = await db.Orders.AsNoTracking().FirstAsync(o => o.Id == order.Id);
            Assert.Equal("PENDING", unchangedOrder.OrderStatus);
            var inv = await db.StoreInventories.AsNoTracking().FirstAsync(si => si.StoreId == storeCauGiay.Id);
            Assert.Equal(20, inv.PhysicalQuantity);
            Assert.Equal(2, inv.ReservedQuantity);
        }

        [Fact]
        public async Task HandoverPickupOrder_ValidPin_DeductsStockExactlyOnce_UpdatesShift_AndAwardsLoyalty()
        {
            using var db = CreateInMemoryContext();
            var (storeCauGiay, _, product, _, shiftCauGiay) = await SeedTwoStoresAndInventoryAsync(db);

            var loyaltyMock = new Mock<ILoyaltyService>();
            loyaltyMock
                .Setup(l => l.EarnPointsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Omnichannel.Application.DTOs.Loyalty.EarnPointsDto { PointsEarned = 85 });

            var workflow = CreateWorkflowService(db, product, qty: 2, loyaltyMock: loyaltyMock);

            var (_, _, created) = await workflow.CreateOrderWithSoftReserveAsync("CART-TEST-D1", new CheckoutViewModel
            {
                RecipientName = "Nguyễn Thu Hà",
                RecipientPhone = "0977889900",
                DeliveryMethod = "STORE_PICKUP",
                PickupStoreId = storeCauGiay.Id,
                PaymentMethod = 1
            }, "25");

            var order = await db.Orders.FirstAsync(o => o.OrderCode == created!.OrderId);

            // Act 1: Bàn giao lần 1 với mã PIN hợp lệ tại Cầu Giấy
            var handover1 = await workflow.HandoverPickupOrderAtPosAsync(new HandoverPickupOrderRequestDto
            {
                OrderId = order.Id,
                OrderCode = order.OrderCode,
                PickupCode = order.PickupCode!,
                CashierStoreId = storeCauGiay.Id,
                CashierUserId = 4,
                CashierName = "Thu ngân Cầu Giấy",
                PosShiftId = shiftCauGiay.Id
            });

            Assert.True(handover1.Success);

            // Kiểm tra tồn kho chỉ trừ đúng 1 lần (20 - 2 = 18, Reserved = 0)
            var invAfter1 = await db.StoreInventories.AsNoTracking().FirstAsync(si => si.StoreId == storeCauGiay.Id);
            Assert.Equal(18, invAfter1.PhysicalQuantity);
            Assert.Equal(0, invAfter1.ReservedQuantity);

            // Kiểm tra đơn chuyển COMPLETED, gán vào ca POS và cộng điểm Loyalty
            var completedOrder = await db.Orders.AsNoTracking().FirstAsync(o => o.Id == order.Id);
            Assert.Equal("COMPLETED", completedOrder.OrderStatus);
            Assert.Equal("PAID", completedOrder.PaymentStatus);
            Assert.NotNull(completedOrder.PickedUpAt);
            Assert.Equal(shiftCauGiay.Id, completedOrder.PosShiftId);

            var updatedShift = await db.PosShifts.AsNoTracking().FirstAsync(s => s.Id == shiftCauGiay.Id);
            Assert.Equal(1, updatedShift.TransactionCount);
            Assert.Equal(order.TotalAmount, updatedShift.CashSalesTotal);

            loyaltyMock.Verify(l => l.EarnPointsAsync(25, order.Id, order.TotalAmount, It.IsAny<CancellationToken>()), Times.Once);

            // Act 2: Cố tình bấm Bàn giao lần 2 -> Bị chặn và tồn kho KHÔNG bị trừ lần 2 (vẫn = 18)
            var handover2 = await workflow.HandoverPickupOrderAtPosAsync(new HandoverPickupOrderRequestDto
            {
                OrderId = order.Id,
                OrderCode = order.OrderCode,
                PickupCode = order.PickupCode!,
                CashierStoreId = storeCauGiay.Id,
                CashierUserId = 4,
                CashierName = "Thu ngân Cầu Giấy",
                PosShiftId = shiftCauGiay.Id
            });

            Assert.False(handover2.Success);
            var invAfter2 = await db.StoreInventories.AsNoTracking().FirstAsync(si => si.StoreId == storeCauGiay.Id);
            Assert.Equal(18, invAfter2.PhysicalQuantity);
        }

        [Fact]
        public async Task ReleaseExpiredPickupOrders_After48Hours_CancelsOrderAndRestoresFefoReserve()
        {
            using var db = CreateInMemoryContext();
            var (storeCauGiay, _, product, _, _) = await SeedTwoStoresAndInventoryAsync(db);
            var workflow = CreateWorkflowService(db, product, qty: 3);

            var (_, _, created) = await workflow.CreateOrderWithSoftReserveAsync("CART-TEST-D1", new CheckoutViewModel
            {
                RecipientName = "Khách Quá Hạn 48h",
                RecipientPhone = "0933445566",
                DeliveryMethod = "STORE_PICKUP",
                PickupStoreId = storeCauGiay.Id,
                PaymentMethod = 4
            }, "30");

            var order = await db.Orders.FirstAsync(o => o.OrderCode == created!.OrderId);
            // Giả lập đơn đã quá hạn 48h
            order.PickupExpiresAt = DateTime.UtcNow.AddMinutes(-10);
            await db.SaveChangesAsync();

            // Act: Chạy job giải phóng đơn quá hạn 48h
            int releasedCount = await workflow.ReleaseExpiredPickupOrdersAsync();

            // Assert
            Assert.Equal(1, releasedCount);
            var cancelledOrder = await db.Orders.AsNoTracking().FirstAsync(o => o.Id == order.Id);
            Assert.Equal("CANCELLED", cancelledOrder.OrderStatus);

            var inv = await db.StoreInventories.AsNoTracking().FirstAsync(si => si.StoreId == storeCauGiay.Id);
            Assert.Equal(20, inv.PhysicalQuantity);
            Assert.Equal(0, inv.ReservedQuantity);
        }
    }
}
