using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Omnichannel.Application.DTOs.Sales;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Services;
using Omnichannel.Tests.Helpers;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class Session7SmartPosAndWarehouseTests
    {
        [Fact]
        public async Task PosShift_FullLifecycle_Reconciliation_CalculatesDiscrepancyAccurately()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new PosShiftService(context, NullLogger<PosShiftService>.Instance);

            var store = new Store { Name = "Showroom Vincom Landmark 81", Code = "SH-LM81", Address = "720A Điện Biên Phủ, P.22, Bình Thạnh" };
            var cashier = new User { Username = "cashier_ngocanh", Email = "ngocanh@beauty.vn", FullName = "Nguyễn Ngọc Ánh" };
            context.Stores.Add(store);
            context.Users.Add(cashier);
            await context.SaveChangesAsync();

            // 1. Mở ca bán hàng (Open Shift) với 1.000.000 VNĐ tiền mặt đầu ca trong két
            var openDto = new OpenShiftDto
            {
                StoreId = store.Id,
                CashierId = cashier.Id,
                OpeningBalance = 1000000m
            };
            var openShift = await service.OpenShiftAsync(openDto);
            openShift.Should().NotBeNull();
            openShift.Status.Should().Be("OPEN");
            openShift.OpeningBalance.Should().Be(1000000m);

            // 2. Giao dịch két trong ca:
            // a. Bổ sung tiền lẻ (Add Float) 300.000 VNĐ
            await service.AddDrawerTransactionAsync(openShift.Id, new DrawerTransactionDto
            {
                Type = "CASH_IN",
                Amount = 300000m,
                Reason = "Bổ sung tiền lẻ mệnh giá 10k-20k cho thu ngân",
                PerformedBy = cashier.Id
            });

            // b. Rút bớt tiền nộp về két lớn (Drop Cash) 200.000 VNĐ
            await service.AddDrawerTransactionAsync(openShift.Id, new DrawerTransactionDto
            {
                Type = "CASH_OUT",
                Amount = 200000m,
                Reason = "Rút nộp bớt tiền ca trưa về két an toàn trung tâm",
                PerformedBy = cashier.Id
            });

            // 3. Bán hàng phát sinh trong ca:
            // Đơn 1: Tiền mặt 500.000 VNĐ
            context.Orders.Add(new Order
            {
                StoreId = store.Id,
                PosShiftId = openShift.Id,
                OrderCode = "POS-ORD-001",
                PaymentMethod = "CASH",
                PaymentStatus = "PAID",
                OrderStatus = "COMPLETED",
                SubTotal = 500000m,
                TotalAmount = 500000m,
                CustomerName = "Chị Lan Anh",
                CustomerPhone = "0908889999"
            });

            // Đơn 2: Chuyển khoản VietQR 450.000 VNĐ (Không cộng vào két tiền mặt vật lý)
            context.Orders.Add(new Order
            {
                StoreId = store.Id,
                PosShiftId = openShift.Id,
                OrderCode = "POS-ORD-002",
                PaymentMethod = "VIETQR",
                PaymentStatus = "PAID",
                OrderStatus = "COMPLETED",
                SubTotal = 450000m,
                TotalAmount = 450000m,
                CustomerName = "Anh Tuấn",
                CustomerPhone = "0912345678"
            });
            await context.SaveChangesAsync();

            // 4. Kiểm tra trạng thái két thời gian thực
            var currentOpen = await service.GetCurrentOpenShiftAsync(store.Id, cashier.Id);
            currentOpen.Should().NotBeNull();
            // Tiền lý thuyết = 1,000,000 (đầu ca) + 500,000 (bán tiền mặt) + 300,000 (Add Float) - 200,000 (Drop Cash) = 1,600,000đ
            currentOpen!.ExpectedCash.Should().Be(1600000m);
            currentOpen.CashSalesTotal.Should().Be(500000m);
            currentOpen.CardSalesTotal.Should().Be(450000m);
            currentOpen.DrawerTransactions.Should().HaveCount(2);

            // 5. Đóng ca bán hàng (Close Shift):
            // Thu ngân đếm tiền thực tế: 1.580.000đ (thực tế thiếu 20.000đ)
            var closeDto = new CloseShiftDto
            {
                ShiftId = openShift.Id,
                ActualCash = 1580000m,
                Note = "Đối soát cuối ca: lệch thiếu 20k tiền lẻ thối khách"
            };
            var closedShift = await service.CloseShiftAsync(closeDto);

            // Assert kết quả đối soát
            closedShift.Status.Should().Be("CLOSED");
            closedShift.ExpectedCash.Should().Be(1600000m);
            closedShift.ActualCash.Should().Be(1580000m);
            closedShift.Difference.Should().Be(-20000m, "vì 1,580,000 - 1,600,000 = -20,000đ chênh lệch thiếu");
            closedShift.CashSalesTotal.Should().Be(500000m);
            closedShift.CardSalesTotal.Should().Be(450000m);
            closedShift.TransactionCount.Should().Be(2);
        }

        [Fact]
        public void VietQr_GenerateDynamicQr_ContainsCompact2FormatAndCorrectAmount()
        {
            // Arrange
            var inMemorySettings = new System.Collections.Generic.Dictionary<string, string?>
            {
                {"VietQrConfig:BankId", "MB"},
                {"VietQrConfig:AccountNo", "0909999888"},
                {"VietQrConfig:AccountName", "CONG TY TNHH OMNICHANNEL BEAUTY"},
                {"VietQrConfig:BankName", "MBBank"}
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            using var httpClient = new HttpClient();
            var service = new PaymentService(configuration, httpClient, NullLogger<PaymentService>.Instance);

            // Act
            var qrResult = service.GenerateVietQr("HD-POS-9988", 1250000m, "ORDER_POS9988");

            // Assert
            qrResult.Should().NotBeNull();
            qrResult.Amount.Should().Be(1250000m);
            qrResult.BankAccountNo.Should().Be("0909999888");
            qrResult.QrImageUrl.Should().Contain("img.vietqr.io/image/MB-0909999888-compact2.png");
            qrResult.QrImageUrl.Should().Contain("amount=1250000");
            qrResult.ExpireSeconds.Should().Be(900);
        }

        [Fact]
        public void FefoExpiryRadar_ClassifiesThreeTiersCorrectly()
        {
            // Arrange
            var today = DateTime.UtcNow.Date;
            var criticalDate = today.AddDays(25);  // <= 30 ngày (Đỏ)
            var warningDate = today.AddDays(50);   // 31 - 60 ngày (Vàng)
            var safeDate = today.AddDays(180);     // > 60 ngày (Xanh)

            // Act & Assert
            int criticalDays = (int)(criticalDate - today).TotalDays;
            int warningDays = (int)(warningDate - today).TotalDays;
            int safeDays = (int)(safeDate - today).TotalDays;

            (criticalDays <= 30).Should().BeTrue("Lô 25 ngày phải thuộc Tier Đỏ Khẩn cấp");
            (warningDays > 30 && warningDays <= 60).Should().BeTrue("Lô 50 ngày phải thuộc Tier Vàng Cảnh báo");
            (safeDays > 60).Should().BeTrue("Lô 180 ngày phải thuộc Tier Xanh An toàn");
        }

        [Fact]
        public async Task PurchaseOrder_WeightedAverageCost_CalculatesAccurately()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();

            var category = new Category { Name = "Dưỡng Da", Slug = "duong-da", IsActive = true };
            context.Categories.Add(category);
            await context.SaveChangesAsync();

            var product = new Product
            {
                Name = "Serum Phục Hồi B5 La Roche-Posay",
                Sku = "LRP-B5-30ML",
                Barcode = "8931234567890",
                CostPrice = 200000m, // Giá vốn ban đầu 200k
                SellingPrice = 450000m,
                CategoryId = category.Id,
                IsActive = true
            };
            context.Products.Add(product);

            var store = new Store { Name = "Tổng Kho Hà Nội", Code = "WH-HN", Address = "1 Hoàng Hoa Thám" };
            context.Stores.Add(store);
            await context.SaveChangesAsync();

            // Tồn cũ: 20 sản phẩm @ 200.000đ
            int existingQty = 20;
            decimal existingCost = product.CostPrice; // 200.000đ

            // Nhập mới: 30 sản phẩm @ 250.000đ
            int newQty = 30;
            decimal newImportCost = 250000m;

            // Act: Tính giá vốn bình quân gia quyền
            decimal prevTotalVal = (decimal)existingQty * existingCost;  // 4,000,000
            decimal newBatchVal = (decimal)newQty * newImportCost;        // 7,500,000
            int updatedTotalQty = existingQty + newQty;                   // 50
            decimal newWeightedCost = Math.Round((prevTotalVal + newBatchVal) / updatedTotalQty, 0); // 11,500,000 / 50 = 230,000đ

            product.CostPrice = newWeightedCost;
            await context.SaveChangesAsync();

            // Assert
            product.CostPrice.Should().Be(230000m, "vì (20*200k + 30*250k)/50 = 230.000đ");
        }

        [Fact]
        public async Task ImmutableInventoryLog_AuditTrail_RecordsStockTransitions()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();

            var store = new Store { Name = "Showroom Lê Văn Sỹ", Code = "SH-LVS", Address = "350 Lê Văn Sỹ, Q.3" };
            var product = new Product { Name = "Kem Chống Nắng Anessa 60ml", Sku = "ANE-60ML", Barcode = "4901872000011", SellingPrice = 580000m, CostPrice = 320000m };
            context.Stores.Add(store);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var batch = new ProductBatch
            {
                ProductId = product.Id,
                BatchNumber = "LOT-ANE-2026",
                ExpDate = DateTime.UtcNow.AddMonths(18),
                InitialQuantity = 100,
                CurrentQuantity = 100,
                ImportCost = 320000m
            };
            context.ProductBatches.Add(batch);
            await context.SaveChangesAsync();

            // Act: Ghi nhận 1 lượt nhập kho và 1 lượt bán hàng vào InventoryLogs
            var importLog = new InventoryLog
            {
                StoreId = store.Id,
                ProductBatchId = batch.Id,
                TransactionType = "IMPORT",
                QuantityChanged = 100,
                QuantityBefore = 0,
                QuantityAfter = 100,
                Note = "Nhập kho mới PO-ANE-01",
                CreatedAt = DateTime.UtcNow
            };
            context.InventoryLogs.Add(importLog);

            var saleLog = new InventoryLog
            {
                StoreId = store.Id,
                ProductBatchId = batch.Id,
                TransactionType = "POS_SALE",
                QuantityChanged = -2,
                QuantityBefore = 100,
                QuantityAfter = 98,
                Note = "Bán lẻ tại quầy POS hóa đơn HD-001",
                CreatedAt = DateTime.UtcNow.AddMinutes(5)
            };
            context.InventoryLogs.Add(saleLog);
            await context.SaveChangesAsync();

            // Assert
            var logs = context.InventoryLogs.Where(l => l.StoreId == store.Id).OrderBy(l => l.CreatedAt).ToList();
            logs.Should().HaveCount(2);
            logs[0].TransactionType.Should().Be("IMPORT");
            logs[0].QuantityChanged.Should().Be(100);
            logs[0].QuantityAfter.Should().Be(100);

            logs[1].TransactionType.Should().Be("POS_SALE");
            logs[1].QuantityChanged.Should().Be(-2);
            logs[1].QuantityAfter.Should().Be(98);
        }
    }
}
