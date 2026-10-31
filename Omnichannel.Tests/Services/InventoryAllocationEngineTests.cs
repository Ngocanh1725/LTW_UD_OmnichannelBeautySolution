using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Inventory;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Services;
using Omnichannel.Tests.Helpers;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class InventoryAllocationEngineTests
    {
        [Fact]
        public async Task AllocateBatchesFEFO_PicksNearestExpDateFirst()
        {
            // Arrange: 3 lô hàng có hạn dùng khác nhau, ưu tiên xuất lô cận date trước
            using var context = TestDbContextFactory.Create();
            var engine = new InventoryAllocationEngine(context);

            var store = new Store { Id = 1, Code = "STORE-Q1", Name = "Chi nhánh Quận 1", Address = "123 Lê Lợi", IsActive = true };
            var product = new Product { Id = 101, Sku = "SKU-SERUM-B5", Barcode = "8931234567890", Name = "Serum La Roche-Posay B5", IsActive = true, SellingPrice = 500000m };

            var batch1 = new ProductBatch { Id = 1, ProductId = 101, BatchNumber = "LOT-2026-EARLY", ExpDate = DateTime.Today.AddDays(70), CurrentQuantity = 5, Status = "ACTIVE" };
            var batch2 = new ProductBatch { Id = 2, ProductId = 101, BatchNumber = "LOT-2026-MID", ExpDate = DateTime.Today.AddDays(150), CurrentQuantity = 10, Status = "ACTIVE" };
            var batch3 = new ProductBatch { Id = 3, ProductId = 101, BatchNumber = "LOT-2026-LATE", ExpDate = DateTime.Today.AddDays(300), CurrentQuantity = 15, Status = "ACTIVE" };

            var inv1 = new StoreInventory { Id = 1, StoreId = 1, ProductBatchId = 1, PhysicalQuantity = 5, ReservedQuantity = 0, ProductBatch = batch1, Store = store };
            var inv2 = new StoreInventory { Id = 2, StoreId = 1, ProductBatchId = 2, PhysicalQuantity = 10, ReservedQuantity = 0, ProductBatch = batch2, Store = store };
            var inv3 = new StoreInventory { Id = 3, StoreId = 1, ProductBatchId = 3, PhysicalQuantity = 15, ReservedQuantity = 0, ProductBatch = batch3, Store = store };

            context.Stores.Add(store);
            context.Products.Add(product);
            context.ProductBatches.AddRange(batch1, batch2, batch3);
            context.StoreInventories.AddRange(inv1, inv2, inv3);
            await context.SaveChangesAsync();

            // Act: Đặt 8 sản phẩm
            var allocations = await engine.AllocateBatchesFEFOAsync("1", "SKU-SERUM-B5", 8);

            // Assert: Lấy 5 hộp từ LOT-2026-EARLY (hạn 70 ngày) và 3 hộp từ LOT-2026-MID (hạn 150 ngày)
            allocations.Should().NotBeNull();
            allocations.Should().HaveCount(2);

            allocations[0].BatchId.Should().Be(1);
            allocations[0].BatchCode.Should().Be("LOT-2026-EARLY");
            allocations[0].AllocatedQuantity.Should().Be(5);

            allocations[1].BatchId.Should().Be(2);
            allocations[1].BatchCode.Should().Be("LOT-2026-MID");
            allocations[1].AllocatedQuantity.Should().Be(3);
        }

        [Fact]
        public async Task AllocateBatchesFEFO_PrioritizesSafeBatches_WhenNotClearance()
        {
            // Arrange: 1 lô cận date (20 ngày < 60 ngày an toàn) và 1 lô an toàn (120 ngày)
            using var context = TestDbContextFactory.Create();
            var engine = new InventoryAllocationEngine(context);

            var store = new Store { Id = 2, Code = "STORE-CG", Name = "Chi nhánh Cầu Giấy", Address = "45 Cầu Giấy", IsActive = true };
            var product = new Product { Id = 202, Sku = "SKU-SUNSCREEN", Barcode = "8939876543210", Name = "Kem chống nắng Anessa 60ml", IsActive = true, SellingPrice = 650000m };

            var batchNearDate = new ProductBatch { Id = 10, ProductId = 202, BatchNumber = "LOT-NEAR-EXP", ExpDate = DateTime.Today.AddDays(20), CurrentQuantity = 5, Status = "ACTIVE" };
            var batchSafeDate = new ProductBatch { Id = 11, ProductId = 202, BatchNumber = "LOT-SAFE-EXP", ExpDate = DateTime.Today.AddDays(120), CurrentQuantity = 10, Status = "ACTIVE" };

            var invNear = new StoreInventory { Id = 10, StoreId = 2, ProductBatchId = 10, PhysicalQuantity = 5, ReservedQuantity = 0, ProductBatch = batchNearDate, Store = store };
            var invSafe = new StoreInventory { Id = 11, StoreId = 2, ProductBatchId = 11, PhysicalQuantity = 10, ReservedQuantity = 0, ProductBatch = batchSafeDate, Store = store };

            context.Stores.Add(store);
            context.Products.Add(product);
            context.ProductBatches.AddRange(batchNearDate, batchSafeDate);
            context.StoreInventories.AddRange(invNear, invSafe);
            await context.SaveChangesAsync();

            // Act 1: Đơn chuẩn không xả hàng (allowNearExpiration = false)
            var normalAlloc = await engine.AllocateBatchesFEFOAsync("2", "SKU-SUNSCREEN", 2, allowNearExpiration: false);

            // Assert 1: Phải lấy từ lô an toàn (LOT-SAFE-EXP)
            normalAlloc.Should().HaveCount(1);
            normalAlloc[0].BatchId.Should().Be(11);
            normalAlloc[0].BatchCode.Should().Be("LOT-SAFE-EXP");

            // Act 2: Đơn xả kho FEFO (allowNearExpiration = true)
            var clearanceAlloc = await engine.AllocateBatchesFEFOAsync("2", "SKU-SUNSCREEN", 2, allowNearExpiration: true);

            // Assert 2: Phải lấy từ lô cận date trước (LOT-NEAR-EXP)
            clearanceAlloc.Should().HaveCount(1);
            clearanceAlloc[0].BatchId.Should().Be(10);
            clearanceAlloc[0].BatchCode.Should().Be("LOT-NEAR-EXP");
        }

        [Fact]
        public async Task TwoPhaseAllocation_ReserveAndDeduct_UpdatesQuantitiesAndLogs()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var engine = new InventoryAllocationEngine(context);

            var store = new Store { Id = 3, Code = "STORE-TD", Name = "Chi nhánh Thủ Đức", Address = "88 Võ Văn Ngân", IsActive = true };
            var product = new Product { Id = 303, Sku = "SKU-TONER-KLV", Barcode = "8935555555555", Name = "Nước hoa hồng Klairs 180ml", IsActive = true, SellingPrice = 320000m };
            var batch = new ProductBatch { Id = 20, ProductId = 303, BatchNumber = "LOT-2027-KL01", ExpDate = DateTime.Today.AddDays(365), CurrentQuantity = 10, Status = "ACTIVE" };
            var inv = new StoreInventory { Id = 20, StoreId = 3, ProductBatchId = 20, PhysicalQuantity = 10, ReservedQuantity = 0, ProductBatch = batch, Store = store };

            context.Stores.Add(store);
            context.Products.Add(product);
            context.ProductBatches.Add(batch);
            context.StoreInventories.Add(inv);
            await context.SaveChangesAsync();

            var allocations = new List<AllocatedBatchDto>
            {
                new AllocatedBatchDto { BatchId = 20, BatchCode = "LOT-2027-KL01", BatchNumber = "LOT-2027-KL01", ProductId = 303, AllocatedQuantity = 4 }
            };

            // Act - Giai đoạn 1: Giữ hàng khi đặt đơn (Reserve)
            var reserveOk = await engine.ReserveStockAsync("3", allocations);

            // Assert Giai đoạn 1: Physical vẫn = 10, Reserved = 4, Khả dụng = 6
            reserveOk.Should().BeTrue();
            var invAfterReserve = await context.StoreInventories.FindAsync(20);
            invAfterReserve!.PhysicalQuantity.Should().Be(10);
            invAfterReserve.ReservedQuantity.Should().Be(4);
            invAfterReserve.GetAvailableQuantity().Should().Be(6);

            // Act - Giai đoạn 2: Xuất kho thực tế khi đóng gói/thanh toán (Deduct)
            var deductOk = await engine.DeductStockAsync("3", allocations, "ONLINE_ORDER_DISPATCH");

            // Assert Giai đoạn 2: Physical = 6, Reserved = 0, Khả dụng = 6
            deductOk.Should().BeTrue();
            var invAfterDeduct = await context.StoreInventories.FindAsync(20);
            invAfterDeduct!.PhysicalQuantity.Should().Be(6);
            invAfterDeduct.ReservedQuantity.Should().Be(0);
            invAfterDeduct.GetAvailableQuantity().Should().Be(6);

            // Kiểm tra nhật ký bất biến InventoryLogs
            var log = await context.InventoryLogs.FirstOrDefaultAsync(l => l.StoreId == 3 && l.ProductBatchId == 20);
            log.Should().NotBeNull();
            log!.QuantityChanged.Should().Be(-4);
            log.QuantityBefore.Should().Be(10);
            log.QuantityAfter.Should().Be(6);
        }

        [Fact]
        public async Task TwoPhaseAllocation_ReleaseReservedStock_WhenOrderCancelled()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var engine = new InventoryAllocationEngine(context);

            var store = new Store { Id = 4, Code = "STORE-DN", Name = "Chi nhánh Đà Nẵng", Address = "12 Nguyễn Văn Linh", IsActive = true };
            var product = new Product { Id = 404, Sku = "SKU-CREAM-B5", Barcode = "8937777777777", Name = "Kem dưỡng B5", IsActive = true, SellingPrice = 450000m };
            var batch = new ProductBatch { Id = 30, ProductId = 404, BatchNumber = "LOT-2027-CR01", ExpDate = DateTime.Today.AddDays(200), CurrentQuantity = 10, Status = "ACTIVE" };
            var inv = new StoreInventory { Id = 30, StoreId = 4, ProductBatchId = 30, PhysicalQuantity = 10, ReservedQuantity = 3, ProductBatch = batch, Store = store };

            context.Stores.Add(store);
            context.Products.Add(product);
            context.ProductBatches.Add(batch);
            context.StoreInventories.Add(inv);
            await context.SaveChangesAsync();

            var allocations = new List<AllocatedBatchDto>
            {
                new AllocatedBatchDto { BatchId = 30, BatchCode = "LOT-2027-CR01", BatchNumber = "LOT-2027-CR01", ProductId = 404, AllocatedQuantity = 3 }
            };

            // Act: Khách hàng hủy đơn hoặc hết hạn thanh toán -> Hoàn trả tồn giữ chỗ
            var releaseOk = await engine.ReleaseReservedStockAsync("4", allocations);

            // Assert: ReservedQuantity trở về 0, Physical vẫn nguyên 10
            releaseOk.Should().BeTrue();
            var invAfterRelease = await context.StoreInventories.FindAsync(30);
            invAfterRelease!.PhysicalQuantity.Should().Be(10);
            invAfterRelease.ReservedQuantity.Should().Be(0);
            invAfterRelease.GetAvailableQuantity().Should().Be(10);
        }
    }
}
