using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Omnichannel.Application.DTOs.Reports;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Web.Areas.Admin.Controllers;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class AdminDashboardDecisionCenterTests
    {
        private ApplicationDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"OmniDb_Dashboard_{Guid.NewGuid()}")
                .Options;

            return new ApplicationDbContext(options);
        }

        private void SeedStoreAndOrders(ApplicationDbContext context, DateTime baseDate)
        {
            var store1 = new Store { Id = 1, Code = "ST-HN-01", Name = "Showroom Cầu Giấy", IsActive = true, Address = "HN" };
            var store2 = new Store { Id = 2, Code = "ST-HCM-01", Name = "Showroom Quận 1", IsActive = true, Address = "HCM" };
            context.Stores.AddRange(store1, store2);

            var cat = new Category { Id = 1, Name = "Chăm sóc da", Slug = "cham-soc-da" };
            var brand = new Brand { Id = 1, Name = "La Roche-Posay", Slug = "la-roche-posay", CountryOfOrigin = "Pháp" };
            context.Categories.Add(cat);
            context.Brands.Add(brand);

            var product = new Product
            {
                Id = 1,
                Name = "Serum Phục Hồi B5 Hyalu",
                Sku = "SKU-B5-01",
                CategoryId = 1,
                BrandId = 1,
                SellingPrice = 500000m,
                IsActive = true
            };
            context.Products.Add(product);

            var batch1 = new ProductBatch
            {
                Id = 1,
                ProductId = 1,
                BatchNumber = "LOT-B5-2026",
                CurrentQuantity = 50,
                ExpDate = baseDate.AddDays(45), // Cận date <= 90 ngày
                ImportCost = 300000m,
                Status = "NEAR_EXPIRATION"
            };
            context.ProductBatches.Add(batch1);

            var inv1 = new StoreInventory
            {
                Id = 1,
                StoreId = 1,
                ProductBatchId = 1,
                PhysicalQuantity = 30,
                ReservedQuantity = 0
            };
            var inv2 = new StoreInventory
            {
                Id = 2,
                StoreId = 2,
                ProductBatchId = 1,
                PhysicalQuantity = 20,
                ReservedQuantity = 0
            };
            context.StoreInventories.AddRange(inv1, inv2);

            // Seed Orders for Store 1:
            // Order 1: Completed, 1.000.000₫ (Customer A - Phone: 0901000001, UserId = 10)
            var ord1 = new Order
            {
                Id = 101,
                OrderCode = "ORD-TEST-01",
                StoreId = 1,
                UserId = 10,
                CustomerName = "Nguyễn Thu Hà",
                CustomerPhone = "0901000001",
                TotalAmount = 1000000m,
                OrderStatus = "COMPLETED",
                OrderSource = "POS",
                PaymentStatus = "PAID",
                CreatedAt = baseDate.AddDays(-2)
            };
            ord1.OrderDetails.Add(new OrderDetail { OrderId = 101, ProductId = 1, Quantity = 2, UnitPrice = 500000m, LineTotal = 1000000m });

            // Order 2: Completed, 500.000₫ (Customer A - 2nd order within 5 days)
            var ord2 = new Order
            {
                Id = 102,
                OrderCode = "ORD-TEST-02",
                StoreId = 1,
                UserId = 10,
                CustomerName = "Nguyễn Thu Hà",
                CustomerPhone = "0901000001",
                TotalAmount = 500000m,
                OrderStatus = "COMPLETED",
                OrderSource = "WEBSITE",
                PaymentStatus = "PAID",
                CreatedAt = baseDate.AddDays(-5)
            };
            ord2.OrderDetails.Add(new OrderDetail { OrderId = 102, ProductId = 1, Quantity = 1, UnitPrice = 500000m, LineTotal = 500000m });

            // Order 3: Cancelled, 500.000₫ (Store 1)
            var ord3 = new Order
            {
                Id = 103,
                OrderCode = "ORD-TEST-03",
                StoreId = 1,
                CustomerName = "Khách Hủy",
                CustomerPhone = "0909999999",
                TotalAmount = 500000m,
                OrderStatus = "CANCELLED",
                OrderSource = "WEBSITE",
                CreatedAt = baseDate.AddDays(-3)
            };

            // Order 4: Store 2 Order: 2.000.000₫ (Customer B - Phone: 0902000002, At-Risk customer: last order 45 days ago)
            var ord4 = new Order
            {
                Id = 104,
                OrderCode = "ORD-TEST-04",
                StoreId = 2,
                UserId = 20,
                CustomerName = "Trần Minh Quân",
                CustomerPhone = "0902000002",
                TotalAmount = 2000000m,
                OrderStatus = "COMPLETED",
                OrderSource = "POS",
                PaymentStatus = "PAID",
                CreatedAt = baseDate.AddDays(-45)
            };
            ord4.OrderDetails.Add(new OrderDetail { OrderId = 104, ProductId = 1, Quantity = 4, UnitPrice = 500000m, LineTotal = 2000000m });

            context.Orders.AddRange(ord1, ord2, ord3, ord4);

            var user10 = new User { Id = 10, FullName = "Nguyễn Thu Hà", PhoneNumber = "0901000001", UserType = 1, StoreId = 1, CreatedAt = baseDate.AddDays(-6) };
            var user20 = new User { Id = 20, FullName = "Trần Minh Quân", PhoneNumber = "0902000002", UserType = 1, StoreId = 2, CreatedAt = baseDate.AddDays(-50) };
            context.Users.AddRange(user10, user20);

            context.SaveChanges();
        }

        [Fact]
        public async Task Dashboard_KpiMetrics_ReconcileExactlyWithDirectSqlFormulas()
        {
            // Arrange
            var context = CreateInMemoryDbContext();
            var now = DateTime.UtcNow;
            SeedStoreAndOrders(context, now);

            var cache = new MemoryCache(new MemoryCacheOptions());
            var controller = new DashboardController(context, null, cache);

            // SuperAdmin claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "superadmin"),
                new Claim(ClaimTypes.Role, "SuperAdmin")
            };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) }
            };

            // Act - query last 30 days, all channels, all stores
            var result = await controller.Index(days: 30, channel: "ALL", storeId: null) as ViewResult;
            Assert.NotNull(result);
            var vm = Assert.IsType<DashboardViewModel>(result.Model);

            // Assert against manual SQL verification formulas:
            // SQL 1: NetRevenue = SUM(TotalAmount) of Orders where OrderStatus != 'CANCELLED' within 30 days
            // Orders in last 30 days: ord1 (1.000.000), ord2 (500.000). (ord3 is CANCELLED; ord4 is 45 days ago)
            // Total = 1.500.000₫
            decimal expectedNetRevenue = 1500000m;
            int expectedValidOrders = 2;
            decimal expectedAov = 750000m; // 1.500.000 / 2
            int expectedAllOrdersInPeriod = 3; // ord1, ord2, ord3
            int expectedCancelledOrders = 1; // ord3
            decimal expectedCancelRate = Math.Round((1m / 3m) * 100m, 1); // 33.3%

            Assert.Equal(expectedAllOrdersInPeriod, vm.PeriodAllOrdersIncludingCancelled);
            Assert.Equal(expectedNetRevenue, vm.PeriodNetRevenue);
            Assert.Equal(expectedValidOrders, vm.PeriodOrderCount);
            Assert.Equal(expectedAov, vm.PeriodAov);
            Assert.Equal(expectedCancelRate, vm.PeriodCancellationRate);
            Assert.Equal(expectedCancelledOrders, vm.PeriodCancelledOrders);
        }

        [Fact]
        public async Task Dashboard_StoreIsolation_EnforcesBranchManagerSeesOnlyTheirStore()
        {
            // Arrange
            var context = CreateInMemoryDbContext();
            var now = DateTime.UtcNow;
            SeedStoreAndOrders(context, now);

            var cache = new MemoryCache(new MemoryCacheOptions());
            var controller = new DashboardController(context, null, cache);

            // Branch Manager of Store 1 claims (Has Claim "StoreId" = "1", Not SuperAdmin)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "manager_store_1"),
                new Claim(ClaimTypes.NameIdentifier, "99"),
                new Claim(ClaimTypes.Role, "StoreManager"),
                new Claim("StoreId", "1")
            };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) }
            };

            // Act - Branch Manager attempts to query Store 2 explicitly (storeId: 2)
            var result = await controller.Index(days: 60, channel: "ALL", storeId: 2) as ViewResult;
            Assert.NotNull(result);
            var vm = Assert.IsType<DashboardViewModel>(result.Model);

            // Assert: Store Isolation strictly overrides requested storeId with locked StoreId = 1
            Assert.True(vm.IsStoreScoped);
            Assert.Equal(1, vm.CurrentStoreId);
            Assert.Equal("Showroom Cầu Giấy", vm.CurrentStoreName);

            // Store 1 revenue in last 60 days is ord1 (1.000.000) + ord2 (500.000) = 1.500.000₫.
            // ord4 (2.000.000₫ belonging to Store 2) MUST NOT be included!
            Assert.Equal(1500000m, vm.PeriodNetRevenue);
            Assert.DoesNotContain(vm.RecentOrders, o => o.OrderId == 104);
        }

        [Fact]
        public async Task Dashboard_RfmSegmentation_AndTargetedVoucherCreation_WorksCorrectly()
        {
            // Arrange
            var context = CreateInMemoryDbContext();
            var now = DateTime.UtcNow;
            SeedStoreAndOrders(context, now);

            var cache = new MemoryCache(new MemoryCacheOptions());
            var controller = new DashboardController(context, null, cache);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "superadmin"),
                new Claim(ClaimTypes.Role, "SuperAdmin")
            };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) }
            };

            // 1. Test RFM Segments calculation
            var result = await controller.Index(days: 60, channel: "ALL", storeId: null) as ViewResult;
            Assert.NotNull(result);
            var vm = Assert.IsType<DashboardViewModel>(result.Model);

            Assert.NotEmpty(vm.RfmSegments);
            // Verify customer with phone 0902000002 (ord4 placed 45 days ago, spend 2.000.000đ) is categorized as AT_RISK
            var atRiskCustomer = vm.RfmCustomers.FirstOrDefault(c => c.Phone == "0902000002");
            Assert.NotNull(atRiskCustomer);
            Assert.Equal("AT_RISK", atRiskCustomer.SegmentCode);

            // 2. Test 1-Click Targeted Voucher Creation for AT_RISK segment
            var voucherRequest = new CreateTargetedVoucherRequestDto
            {
                SegmentCode = "AT_RISK",
                DiscountPercent = 20m
            };

            var jsonResult = await controller.CreateTargetedVoucher(voucherRequest) as JsonResult;
            Assert.NotNull(jsonResult);

            dynamic data = jsonResult.Value!;
            Assert.True((bool)data.GetType().GetProperty("success")!.GetValue(data)!);
            string couponCode = (string)data.GetType().GetProperty("couponCode")!.GetValue(data)!;
            Assert.StartsWith("WINBACK20-", couponCode);

            // Verify Coupon was actually saved in DB
            var couponInDb = await context.Coupons.FirstOrDefaultAsync(c => c.CouponCode == couponCode);
            Assert.NotNull(couponInDb);
            Assert.Equal(20m, couponInDb.DiscountValue);

            // Verify personal voucher was added to target customer's wallet
            var personalVoucher = await context.Vouchers.FirstOrDefaultAsync(v => v.UserId == 20 && v.VoucherCode == couponCode);
            Assert.NotNull(personalVoucher);
        }

        [Fact]
        public async Task Dashboard_ExportRfmSegmentCsv_ReturnsUtf8BomCsvFile()
        {
            // Arrange
            var context = CreateInMemoryDbContext();
            var now = DateTime.UtcNow;
            SeedStoreAndOrders(context, now);

            var cache = new MemoryCache(new MemoryCacheOptions());
            var controller = new DashboardController(context, null, cache);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "superadmin"),
                new Claim(ClaimTypes.Role, "SuperAdmin")
            };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) }
            };

            // Act
            var fileResult = await controller.ExportRfmSegment("AT_RISK") as FileContentResult;
            Assert.NotNull(fileResult);
            Assert.Equal("text/csv; charset=utf-8", fileResult.ContentType);

            // Verify file has UTF-8 BOM
            byte[] fileBytes = fileResult.FileContents;
            Assert.True(fileBytes.Length >= 3);
            Assert.Equal(0xEF, fileBytes[0]);
            Assert.Equal(0xBB, fileBytes[1]);
            Assert.Equal(0xBF, fileBytes[2]);

            string csvContent = Encoding.UTF8.GetString(fileBytes);
            Assert.Contains("KhachHang,SoDienThoai,Email,PhanKhucRFM", csvContent);
            Assert.Contains("0902000002", csvContent);
        }
    }
}
