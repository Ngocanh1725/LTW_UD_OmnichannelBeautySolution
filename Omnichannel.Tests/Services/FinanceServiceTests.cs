using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Omnichannel.Application.DTOs.Finance;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Services;
using Omnichannel.Tests.Helpers;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class FinanceServiceTests
    {
        [Fact]
        public async Task RecordLedgerEntry_ValidRevenue_Success()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new FinanceService(context, NullLogger<FinanceService>.Instance);

            var store = new Store { Name = "Kho Tổng", Code = "HQ-01", Address = "Hà Nội" };
            context.Stores.Add(store);
            await context.SaveChangesAsync();

            var dto = new CreateLedgerEntryDto
            {
                StoreId = store.Id,
                EntryType = "REVENUE",
                CreditAmount = 1500000m,
                DebitAmount = 0m,
                Description = "Doanh thu bán lẻ POS ca sáng"
            };

            // Act
            var result = await service.RecordLedgerEntryAsync(dto, userId: 1);

            // Assert
            result.Should().NotBeNull();
            result.LedgerCode.Should().StartWith("LED-");
            result.CreditAmount.Should().Be(1500000m);
            result.DebitAmount.Should().Be(0m);
            result.EntryType.Should().Be("REVENUE");
        }

        [Fact]
        public async Task GetLedgerEntries_FilterByStore_ReturnsOnlyTargetStoreEntries()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new FinanceService(context, NullLogger<FinanceService>.Instance);

            var store1 = new Store { Id = 1, Name = "Store 1", Code = "S1", Address = "A1" };
            var store2 = new Store { Id = 2, Name = "Store 2", Code = "S2", Address = "A2" };
            context.Stores.AddRange(store1, store2);

            context.FinancialLedgers.AddRange(
                new FinancialLedger { StoreId = 1, LedgerCode = "L1", EntryType = "REVENUE", CreditAmount = 100000m },
                new FinancialLedger { StoreId = 1, LedgerCode = "L2", EntryType = "EXPENSE", DebitAmount = 20000m },
                new FinancialLedger { StoreId = 2, LedgerCode = "L3", EntryType = "REVENUE", CreditAmount = 300000m }
            );
            await context.SaveChangesAsync();

            // Act
            var list = await service.GetLedgerEntriesAsync(storeId: 1);

            // Assert
            list.Should().HaveCount(2);
            list.Should().OnlyContain(x => x.StoreId == 1);
        }

        [Fact]
        public async Task CreateSupplierDebt_ValidData_SetsStatusToPending()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new FinanceService(context, NullLogger<FinanceService>.Instance);

            var supplier = new Supplier { Name = "AmorePacific VN", Code = "SUP-AP", ContactPerson = "Ms Trang" };
            context.Suppliers.Add(supplier);
            await context.SaveChangesAsync();

            var dto = new CreateSupplierDebtDto
            {
                SupplierId = supplier.Id,
                PurchaseOrderCode = "PO-2026-001",
                DebtAmount = 50000000m,
                DueDate = DateTime.UtcNow.AddDays(30)
            };

            // Act
            var result = await service.CreateSupplierDebtAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be("PENDING");
            result.DebtAmount.Should().Be(50000000m);
            result.PaidAmount.Should().Be(0m);
            result.RemainingAmount.Should().Be(50000000m);
        }

        [Fact]
        public async Task RecordSupplierPayment_PartialPayment_UpdatesStatusToPartial()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new FinanceService(context, NullLogger<FinanceService>.Instance);

            var store = new Store { Name = "Kho Tổng", Code = "HQ", Address = "HN" };
            var supplier = new Supplier { Name = "L'Oreal", Code = "SUP-LO", ContactPerson = "Mr Nam" };
            context.Stores.Add(store);
            context.Suppliers.Add(supplier);
            await context.SaveChangesAsync();

            var debt = new SupplierDebt
            {
                SupplierId = supplier.Id,
                PurchaseOrderCode = "PO-LOREAL-01",
                DebtAmount = 20000000m,
                PaidAmount = 0m,
                RemainingAmount = 20000000m,
                Status = "PENDING"
            };
            context.SupplierDebts.Add(debt);
            await context.SaveChangesAsync();

            // Act: Thanh toán một phần 8.000.000 VNĐ
            var payDto = new RecordSupplierPaymentDto
            {
                DebtId = debt.Id,
                StoreId = store.Id,
                PaymentAmount = 8000000m,
                Note = "Thanh toán đợt 1 40%"
            };
            var success = await service.RecordSupplierPaymentAsync(payDto, userId: 1);

            // Assert
            success.Should().BeTrue();
            var debts = await service.GetSupplierDebtsAsync(supplierId: supplier.Id);
            var updated = debts[0];
            updated.Status.Should().Be("PARTIAL");
            updated.PaidAmount.Should().Be(8000000m);
            updated.RemainingAmount.Should().Be(12000000m);
        }

        [Fact]
        public async Task RecordSupplierPayment_FullPayment_UpdatesStatusToPaid()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new FinanceService(context, NullLogger<FinanceService>.Instance);

            var store = new Store { Name = "Kho Tổng", Code = "HQ", Address = "HN" };
            var supplier = new Supplier { Name = "Rohto", Code = "SUP-RH", ContactPerson = "Mr Hùng" };
            context.Stores.Add(store);
            context.Suppliers.Add(supplier);
            await context.SaveChangesAsync();

            var debt = new SupplierDebt
            {
                SupplierId = supplier.Id,
                PurchaseOrderCode = "PO-ROHTO-01",
                DebtAmount = 10000000m,
                PaidAmount = 0m,
                RemainingAmount = 10000000m,
                Status = "PENDING"
            };
            context.SupplierDebts.Add(debt);
            await context.SaveChangesAsync();

            // Act: Trả toàn bộ 10.000.000 VNĐ
            var payDto = new RecordSupplierPaymentDto
            {
                DebtId = debt.Id,
                StoreId = store.Id,
                PaymentAmount = 10000000m,
                Note = "Tất toán công nợ đơn hàng"
            };
            var success = await service.RecordSupplierPaymentAsync(payDto, userId: 1);

            // Assert
            success.Should().BeTrue();
            var debts = await service.GetSupplierDebtsAsync(supplierId: supplier.Id);
            var updated = debts[0];
            updated.Status.Should().Be("PAID");
            updated.RemainingAmount.Should().Be(0m);
        }

        [Fact]
        public async Task RecordOrderPaymentAndCogs_ValidOrder_CalculatesCogsFromBatchImportCostAndWritesDoubleEntry()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new FinanceService(context, NullLogger<FinanceService>.Instance);

            var store = new Store { Name = "Showroom Q1", Code = "STR-Q1", Address = "HCM" };
            var product = new Product
            {
                Name = "Serum Vitamin C",
                Sku = "SRM-VITC-01",
                Barcode = "893000000001",
                Slug = "serum-vitamin-c",
                SellingPrice = 500000m
            };
            context.Stores.Add(store);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var batch1 = new ProductBatch
            {
                ProductId = product.Id,
                BatchNumber = "LOT-2026-01",
                ImportCost = 250000m,
                InitialQuantity = 100,
                CurrentQuantity = 100,
                ManufacturingDate = DateTime.UtcNow.AddMonths(-2),
                ExpDate = DateTime.UtcNow.AddYears(2),
                Status = "ACTIVE"
            };
            var batch2 = new ProductBatch
            {
                ProductId = product.Id,
                BatchNumber = "LOT-2026-02",
                ImportCost = 270000m,
                InitialQuantity = 100,
                CurrentQuantity = 100,
                ManufacturingDate = DateTime.UtcNow.AddMonths(-1),
                ExpDate = DateTime.UtcNow.AddYears(2),
                Status = "ACTIVE"
            };
            context.ProductBatches.AddRange(batch1, batch2);
            await context.SaveChangesAsync();

            var order = new Order
            {
                OrderCode = "ORD-TEST-FIN-01",
                StoreId = store.Id,
                CustomerName = "Nguyễn Văn A",
                TotalAmount = 1500000m,
                OrderStatus = "COMPLETED",
                PaymentStatus = "PAID",
                PaymentMethod = "VIETQR"
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            // Đơn hàng xuất 2 sản phẩm từ batch1 (2 * 250k = 500k) và 1 sản phẩm từ batch2 (1 * 270k = 270k) -> Tổng COGS = 770k
            var detail1 = new OrderDetail
            {
                OrderId = order.Id,
                ProductId = product.Id,
                ProductBatchId = batch1.Id,
                Quantity = 2,
                UnitPrice = 500000m,
                LineTotal = 1000000m
            };
            var detail2 = new OrderDetail
            {
                OrderId = order.Id,
                ProductId = product.Id,
                ProductBatchId = batch2.Id,
                Quantity = 1,
                UnitPrice = 500000m,
                LineTotal = 500000m
            };
            context.OrderDetails.AddRange(detail1, detail2);
            await context.SaveChangesAsync();

            // Act
            var (revEntry, cogsEntry) = await service.RecordOrderPaymentAndCogsAsync(order.Id);

            // Assert
            revEntry.Should().NotBeNull();
            revEntry.EntryType.Should().Be("REVENUE");
            revEntry.CreditAmount.Should().Be(1500000m);
            revEntry.DebitAmount.Should().Be(0m);

            cogsEntry.Should().NotBeNull();
            cogsEntry!.EntryType.Should().Be("COGS");
            cogsEntry.DebitAmount.Should().Be(770000m);
            cogsEntry.CreditAmount.Should().Be(0m);
        }

        [Fact]
        public async Task CalculateOrderCogs_ReturnsExactCostFromBatches()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new FinanceService(context, NullLogger<FinanceService>.Instance);

            var batch = new ProductBatch
            {
                BatchNumber = "LOT-COGS-01",
                ImportCost = 150000m,
                ManufacturingDate = DateTime.UtcNow,
                ExpDate = DateTime.UtcNow.AddYears(1)
            };
            context.ProductBatches.Add(batch);

            var order = new Order { OrderCode = "ORD-COGS-01" };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var detail = new OrderDetail
            {
                OrderId = order.Id,
                ProductBatchId = batch.Id,
                Quantity = 4,
                UnitPrice = 300000m,
                LineTotal = 1200000m
            };
            context.OrderDetails.Add(detail);
            await context.SaveChangesAsync();

            // Act
            decimal cogs = await service.CalculateOrderCogsAsync(order.Id);

            // Assert: 4 * 150.000 = 600.000
            cogs.Should().Be(600000m);
        }

        [Fact]
        public async Task RecordPurchaseOrderReceipt_CreatesLedgerDebitAndSupplierDebt()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new FinanceService(context, NullLogger<FinanceService>.Instance);

            var store = new Store { Name = "Kho Tổng", Code = "HQ" };
            var supplier = new Supplier { Name = "Shiseido VN", Code = "SUP-SHI" };
            context.Stores.Add(store);
            context.Suppliers.Add(supplier);
            await context.SaveChangesAsync();

            // Act
            var debt = await service.RecordPurchaseOrderReceiptAsync(
                purchaseOrderCode: "PO-SHI-2026-01",
                supplierId: supplier.Id,
                storeId: store.Id,
                totalAmount: 120000000m,
                dueDate: DateTime.UtcNow.AddDays(45));

            // Assert
            debt.Should().NotBeNull();
            debt.DebtAmount.Should().Be(120000000m);
            debt.Status.Should().Be("PENDING");

            var ledgers = await service.GetLedgerEntriesAsync(storeId: store.Id, entryType: "PURCHASE_RECEIPT");
            ledgers.Should().HaveCount(1);
            ledgers[0].DebitAmount.Should().Be(120000000m);
            ledgers[0].CreditAmount.Should().Be(0m);
        }

        [Fact]
        public async Task GetOverdueSupplierDebts_ReturnsOnlyDebtsPastDueDate()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new FinanceService(context, NullLogger<FinanceService>.Instance);

            var supplier = new Supplier { Name = "Innisfree", Code = "SUP-INNI" };
            context.Suppliers.Add(supplier);
            await context.SaveChangesAsync();

            var overdueDebt = new SupplierDebt
            {
                SupplierId = supplier.Id,
                PurchaseOrderCode = "PO-OVERDUE-01",
                DebtAmount = 30000000m,
                RemainingAmount = 30000000m,
                DueDate = DateTime.UtcNow.AddDays(-10),
                Status = "PENDING"
            };

            var validDebt = new SupplierDebt
            {
                SupplierId = supplier.Id,
                PurchaseOrderCode = "PO-VALID-02",
                DebtAmount = 40000000m,
                RemainingAmount = 40000000m,
                DueDate = DateTime.UtcNow.AddDays(20),
                Status = "PENDING"
            };

            context.SupplierDebts.AddRange(overdueDebt, validDebt);
            await context.SaveChangesAsync();

            // Act
            var overdues = await service.GetOverdueSupplierDebtsAsync();

            // Assert
            overdues.Should().HaveCount(1);
            overdues[0].PurchaseOrderCode.Should().Be("PO-OVERDUE-01");
            overdues[0].IsOverdue.Should().BeTrue();
            overdues[0].OverdueDays.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task ReconcileCodOrders_MatchedAndDisputed_CalculatesCorrectDifference()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new FinanceService(context, NullLogger<FinanceService>.Instance);

            var order1 = new Order { OrderCode = "ORD-COD-1", PaymentMethod = "COD", TotalAmount = 500000m };
            var order2 = new Order { OrderCode = "ORD-COD-2", PaymentMethod = "COD", TotalAmount = 300000m };
            context.Orders.AddRange(order1, order2);
            await context.SaveChangesAsync();

            var orderCodes = new System.Collections.Generic.List<string> { "ORD-COD-1", "ORD-COD-2" };

            // Act 1: Khớp hoàn toàn (Tổng 800k, Thực nhận 800k)
            var matchedRecon = await service.ReconcileCodOrdersAsync("GHN", orderCodes, 800000m);

            // Assert 1
            matchedRecon.Status.Should().Be("MATCHED");
            matchedRecon.TotalCodAmount.Should().Be(800000m);
            matchedRecon.Difference.Should().Be(0m);

            // Act 2: Lệch tiền (Tổng 800k, Thực nhận 750k)
            var disputedRecon = await service.ReconcileCodOrdersAsync("GHN", orderCodes, 750000m);

            // Assert 2
            disputedRecon.Status.Should().Be("DISPUTED");
            disputedRecon.Difference.Should().Be(50000m);
        }
    }
}
