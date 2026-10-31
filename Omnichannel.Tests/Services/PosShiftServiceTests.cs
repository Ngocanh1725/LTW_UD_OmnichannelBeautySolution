using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Omnichannel.Application.DTOs.Sales;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Services;
using Omnichannel.Tests.Helpers;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class PosShiftServiceTests
    {
        [Fact]
        public async Task OpenShift_WhenNoActiveShift_CreatesNewShift()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var logger = NullLogger<PosShiftService>.Instance;
            var service = new PosShiftService(context, logger);

            var store = new Store { Name = "Showroom Cầu Giấy", Code = "SH-CG", Address = "123 Cầu Giấy" };
            var cashier = new User { Username = "cashier01", Email = "cashier01@example.com", FullName = "Nguyễn Thu Ngân" };
            context.Stores.Add(store);
            context.Users.Add(cashier);
            await context.SaveChangesAsync();

            var dto = new OpenShiftDto
            {
                StoreId = store.Id,
                CashierId = cashier.Id,
                OpeningBalance = 2000000m
            };

            // Act
            var result = await service.OpenShiftAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be("OPEN");
            result.OpeningBalance.Should().Be(2000000m);
            result.ExpectedCash.Should().Be(2000000m);
            result.ShiftCode.Should().StartWith("SHIFT-");
        }

        [Fact]
        public async Task OpenShift_WhenActiveShiftExists_ThrowsException()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new PosShiftService(context, NullLogger<PosShiftService>.Instance);

            var store = new Store { Name = "Showroom Cầu Giấy", Code = "SH-CG", Address = "123 Cầu Giấy" };
            var cashier = new User { Username = "cashier01", Email = "cashier01@example.com", FullName = "Thu Ngân 01" };
            context.Stores.Add(store);
            context.Users.Add(cashier);
            await context.SaveChangesAsync();

            // Mở ca đầu tiên
            await service.OpenShiftAsync(new OpenShiftDto
            {
                StoreId = store.Id,
                CashierId = cashier.Id,
                OpeningBalance = 1000000m
            });

            // Act: Mở thêm ca thứ 2 khi ca 1 chưa đóng
            Func<Task> act = async () => await service.OpenShiftAsync(new OpenShiftDto
            {
                StoreId = store.Id,
                CashierId = cashier.Id,
                OpeningBalance = 500000m
            });

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*hiện đã có ca làm việc đang mở*");
        }

        [Fact]
        public async Task CloseShift_CalculatesDifference_Correctly()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new PosShiftService(context, NullLogger<PosShiftService>.Instance);

            var store = new Store { Name = "Showroom Cầu Giấy", Code = "SH-CG", Address = "123 Cầu Giấy" };
            var cashier = new User { Username = "cashier01", Email = "cashier01@example.com", FullName = "Thu Ngân" };
            context.Stores.Add(store);
            context.Users.Add(cashier);
            await context.SaveChangesAsync();

            var openResult = await service.OpenShiftAsync(new OpenShiftDto
            {
                StoreId = store.Id,
                CashierId = cashier.Id,
                OpeningBalance = 1000000m
            });

            // Thêm 1 đơn bán tiền mặt 500.000đ trong ca
            context.Orders.Add(new Order
            {
                StoreId = store.Id,
                PosShiftId = openResult.Id,
                PaymentMethod = "CASH",
                PaymentStatus = "PAID",
                OrderStatus = "COMPLETED",
                SubTotal = 500000m,
                TotalAmount = 500000m,
                CustomerName = "Khách Lẻ",
                CustomerPhone = "0901234567"
            });
            await context.SaveChangesAsync();

            // Kỳ vọng: 1,000,000 + 500,000 = 1,500,000. Thực tế kiểm đếm: 1,480,000 (thụt 20k)
            var closeDto = new CloseShiftDto
            {
                ShiftId = openResult.Id,
                ActualCash = 1480000m,
                Note = "Lệch âm 20k tiền thối lẻ"
            };

            // Act
            var closeResult = await service.CloseShiftAsync(closeDto);

            // Assert
            closeResult.ExpectedCash.Should().Be(1500000m);
            closeResult.ActualCash.Should().Be(1480000m);
            closeResult.Difference.Should().Be(-20000m);
            closeResult.Status.Should().Be("CLOSED");
        }

        [Fact]
        public async Task CloseShift_UpdatesStatus_ToClosed()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new PosShiftService(context, NullLogger<PosShiftService>.Instance);

            var store = new Store { Name = "Showroom Cầu Giấy", Code = "SH-CG", Address = "123 Cầu Giấy" };
            var cashier = new User { Username = "cashier01", Email = "cashier01@example.com", FullName = "Thu Ngân" };
            context.Stores.Add(store);
            context.Users.Add(cashier);
            await context.SaveChangesAsync();

            var shift = await service.OpenShiftAsync(new OpenShiftDto
            {
                StoreId = store.Id,
                CashierId = cashier.Id,
                OpeningBalance = 500000m
            });

            // Act
            var closed = await service.CloseShiftAsync(new CloseShiftDto
            {
                ShiftId = shift.Id,
                ActualCash = 500000m
            });

            // Assert
            closed.Status.Should().Be("CLOSED");
            closed.ClosedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task AddDrawerTransaction_CashIn_IncreasesExpectedCashInShiftDetail()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new PosShiftService(context, NullLogger<PosShiftService>.Instance);

            var store = new Store { Name = "Showroom Cầu Giấy", Code = "SH-CG", Address = "123 Cầu Giấy" };
            var cashier = new User { Username = "cashier01", Email = "cashier01@example.com", FullName = "Thu Ngân" };
            context.Stores.Add(store);
            context.Users.Add(cashier);
            await context.SaveChangesAsync();

            var shift = await service.OpenShiftAsync(new OpenShiftDto
            {
                StoreId = store.Id,
                CashierId = cashier.Id,
                OpeningBalance = 1000000m
            });

            // Act: Nạp thêm tiền đổi 500.000đ vào két
            await service.AddDrawerTransactionAsync(shift.Id, new DrawerTransactionDto
            {
                Type = "CASH_IN",
                Amount = 500000m,
                Reason = "Tiền lẻ đổi đầu ca",
                PerformedBy = cashier.Id
            });

            var shiftDetail = await service.GetCurrentOpenShiftAsync(store.Id, cashier.Id);

            // Assert
            shiftDetail.Should().NotBeNull();
            shiftDetail!.ExpectedCash.Should().Be(1500000m, "vì 1,000,000 gốc + 500,000 tiền nạp vào");
            shiftDetail.DrawerTransactions.Should().HaveCount(1);
        }

        [Fact]
        public async Task AddDrawerTransaction_InvalidAmount_ThrowsArgumentException()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new PosShiftService(context, NullLogger<PosShiftService>.Instance);

            var store = new Store { Name = "Showroom Cầu Giấy", Code = "SH-CG", Address = "123 Cầu Giấy" };
            var cashier = new User { Username = "cashier01", Email = "cashier01@example.com", FullName = "Thu Ngân" };
            context.Stores.Add(store);
            context.Users.Add(cashier);
            await context.SaveChangesAsync();

            var shift = await service.OpenShiftAsync(new OpenShiftDto
            {
                StoreId = store.Id,
                CashierId = cashier.Id,
                OpeningBalance = 1000000m
            });

            // Act: Thao tác giao dịch với số tiền 0 hoặc âm
            Func<Task> act = async () => await service.AddDrawerTransactionAsync(shift.Id, new DrawerTransactionDto
            {
                Type = "CASH_OUT",
                Amount = -50000m,
                Reason = "Chi mua văn phòng phẩm",
                PerformedBy = cashier.Id
            });

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*phải lớn hơn 0*");
        }
    }
}
