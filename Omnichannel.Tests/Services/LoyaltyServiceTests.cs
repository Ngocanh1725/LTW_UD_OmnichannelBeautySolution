using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Services;
using Omnichannel.Tests.Helpers;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class LoyaltyServiceTests
    {
        private async Task SeedTiersAsync(Omnichannel.Infrastructure.Data.ApplicationDbContext context)
        {
            context.MemberTiers.AddRange(
                new MemberTier
                {
                    Id = 1,
                    TierName = "Silver",
                    MinSpend = 0,
                    MaxSpend = 2000000m,
                    PointMultiplier = 1.0m,
                    BirthdayBonusPoints = 50,
                    SortOrder = 1,
                    IsActive = true
                },
                new MemberTier
                {
                    Id = 2,
                    TierName = "Gold",
                    MinSpend = 2000001m,
                    MaxSpend = 10000000m,
                    PointMultiplier = 1.5m,
                    BirthdayBonusPoints = 150,
                    SortOrder = 2,
                    IsActive = true
                },
                new MemberTier
                {
                    Id = 3,
                    TierName = "Diamond",
                    MinSpend = 10000001m,
                    MaxSpend = null,
                    PointMultiplier = 2.0m,
                    BirthdayBonusPoints = 300,
                    SortOrder = 3,
                    IsActive = true
                }
            );
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task EarnPoints_10000VND_Earns1Point()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            await SeedTiersAsync(context);
            var service = new LoyaltyService(context, NullLogger<LoyaltyService>.Instance);

            var user = new User { Id = 10, Username = "cust01", Email = "cust01@test.com", FullName = "Khách Hàng 1" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Act: Mua đơn hàng 100,000 VND (10 lần 10.000)
            var result = await service.EarnPointsAsync(user.Id, orderId: 101, orderAmount: 100000m);

            // Assert: Hạng Silver (x1.0) -> 10 điểm
            result.BasePoints.Should().Be(10);
            result.PointsEarned.Should().Be(10);
            result.NewBalance.Should().Be(10);
        }

        [Fact]
        public async Task EarnPoints_GoldMultiplier1_5x_CalculatesCorrectly()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            await SeedTiersAsync(context);
            var service = new LoyaltyService(context, NullLogger<LoyaltyService>.Instance);

            var user = new User { Id = 20, Username = "cust02", Email = "cust02@test.com", FullName = "Khách Gold" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Khởi tạo ví ở hạng Gold
            var wallet = new LoyaltyWallet
            {
                UserId = user.Id,
                CurrentTierId = 2, // Gold (x1.5)
                CurrentBalance = 0,
                TotalPointsEarned = 0,
                TotalSpend12Months = 5000000m
            };
            context.LoyaltyWallets.Add(wallet);
            await context.SaveChangesAsync();

            // Act: Mua đơn 200,000đ (20 base points * 1.5 = 30 points)
            var result = await service.EarnPointsAsync(user.Id, orderId: 102, orderAmount: 200000m);

            // Assert
            result.BasePoints.Should().Be(20);
            result.PointsEarned.Should().Be(30);
            result.TierMultiplier.Should().Be(1.5m);
        }

        [Fact]
        public async Task EarnPoints_DiamondMultiplier2x_CalculatesCorrectly()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            await SeedTiersAsync(context);
            var service = new LoyaltyService(context, NullLogger<LoyaltyService>.Instance);

            var user = new User { Id = 30, Username = "cust03", Email = "cust03@test.com", FullName = "Khách Diamond" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var wallet = new LoyaltyWallet
            {
                UserId = user.Id,
                CurrentTierId = 3, // Diamond (x2.0)
                CurrentBalance = 100,
                TotalPointsEarned = 100,
                TotalSpend12Months = 15000000m
            };
            context.LoyaltyWallets.Add(wallet);
            await context.SaveChangesAsync();

            // Act: Mua đơn 500,000đ (50 base points * 2.0 = 100 points)
            var result = await service.EarnPointsAsync(user.Id, orderId: 103, orderAmount: 500000m);

            // Assert
            result.BasePoints.Should().Be(50);
            result.PointsEarned.Should().Be(100);
            result.NewBalance.Should().Be(200); // 100 cũ + 100 mới
        }

        [Fact]
        public async Task RedeemPoints_SufficientBalance_DeductsPoints()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            await SeedTiersAsync(context);
            var service = new LoyaltyService(context, NullLogger<LoyaltyService>.Instance);

            var user = new User { Id = 40, Username = "cust04", Email = "cust04@test.com", FullName = "Khách Đổi Điểm" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var wallet = new LoyaltyWallet
            {
                UserId = user.Id,
                CurrentTierId = 1,
                CurrentBalance = 150,
                TotalPointsEarned = 150
            };
            context.LoyaltyWallets.Add(wallet);
            await context.SaveChangesAsync();

            // Act: Đổi 50 điểm thành Voucher 50.000 VNĐ
            var result = await service.RedeemPointsAsync(user.Id, 50, "Đổi voucher 50K");

            // Assert
            result.Should().NotBeNull();
            result.PointsToRedeem.Should().Be(50);
            result.DiscountAmountVnd.Should().Be(50000m);
            result.NewBalance.Should().Be(100);
            result.VoucherCode.Should().StartWith("LOYALTY-");
        }

        [Fact]
        public async Task RedeemPoints_InsufficientBalance_ThrowsException()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            await SeedTiersAsync(context);
            var service = new LoyaltyService(context, NullLogger<LoyaltyService>.Instance);

            var user = new User { Id = 50, Username = "cust05", Email = "cust05@test.com", FullName = "Khách Ít Điểm" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var wallet = new LoyaltyWallet
            {
                UserId = user.Id,
                CurrentTierId = 1,
                CurrentBalance = 20, // Chỉ có 20 điểm
                TotalPointsEarned = 20
            };
            context.LoyaltyWallets.Add(wallet);
            await context.SaveChangesAsync();

            // Act: Đổi 100 điểm vượt quá số dư
            Func<Task> act = async () => await service.RedeemPointsAsync(user.Id, 100, "Đổi voucher");

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*không đủ để thực hiện đổi*");
        }

        [Fact]
        public async Task EvaluateTier_SpendUnder2M_ReturnsSilver()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            await SeedTiersAsync(context);
            var service = new LoyaltyService(context, NullLogger<LoyaltyService>.Instance);

            var user = new User { Id = 60, Username = "cust06", Email = "cust06@test.com", FullName = "Khách Mới" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Thêm 1 đơn hàng 1,500,000đ (<= 2M -> Silver)
            context.Orders.Add(new Order
            {
                UserId = user.Id,
                PaymentStatus = "PAID",
                OrderStatus = "COMPLETED",
                SubTotal = 1500000m,
                TotalAmount = 1500000m,
                CustomerName = "Khách Mới",
                CustomerPhone = "0912345678",
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            });
            await context.SaveChangesAsync();

            // Act
            var tierInfo = await service.EvaluateTierAsync(user.Id);
            var wallet = await service.GetWalletAsync(user.Id);

            // Assert
            tierInfo.TierName.Should().Be("Silver");
            wallet!.TotalSpend12Months.Should().Be(1500000m);
        }

        [Fact]
        public async Task EvaluateTier_Spend5M_ReturnsGold()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            await SeedTiersAsync(context);
            var service = new LoyaltyService(context, NullLogger<LoyaltyService>.Instance);

            var user = new User { Id = 70, Username = "cust07", Email = "cust07@test.com", FullName = "Khách Chi Tiêu 5M" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // 1 đơn 5M (nằm trong 2M-10M -> Gold)
            context.Orders.Add(new Order
            {
                UserId = user.Id,
                PaymentStatus = "PAID",
                OrderStatus = "COMPLETED",
                SubTotal = 5000000m,
                TotalAmount = 5000000m,
                CustomerName = "Khách Chi Tiêu 5M",
                CustomerPhone = "0912345679",
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            });
            await context.SaveChangesAsync();

            // Act
            var tierInfo = await service.EvaluateTierAsync(user.Id);
            var wallet = await service.GetWalletAsync(user.Id);

            // Assert
            tierInfo.TierName.Should().Be("Gold");
            wallet!.TotalSpend12Months.Should().Be(5000000m);
        }

        [Fact]
        public async Task EvaluateTier_Spend15M_ReturnsDiamond()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            await SeedTiersAsync(context);
            var service = new LoyaltyService(context, NullLogger<LoyaltyService>.Instance);

            var user = new User { Id = 80, Username = "cust08", Email = "cust08@test.com", FullName = "Khách VIP 15M" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // 2 đơn tổng cộng 15M (> 10M -> Diamond)
            context.Orders.AddRange(
                new Order
                {
                    UserId = user.Id,
                    PaymentStatus = "PAID",
                    OrderStatus = "COMPLETED",
                    SubTotal = 8000000m,
                    TotalAmount = 8000000m,
                    CustomerName = "VIP",
                    CustomerPhone = "0912345680",
                    CreatedAt = DateTime.UtcNow.AddDays(-60)
                },
                new Order
                {
                    UserId = user.Id,
                    PaymentStatus = "PAID",
                    OrderStatus = "COMPLETED",
                    SubTotal = 7000000m,
                    TotalAmount = 7000000m,
                    CustomerName = "VIP",
                    CustomerPhone = "0912345680",
                    CreatedAt = DateTime.UtcNow.AddDays(-20)
                }
            );
            await context.SaveChangesAsync();

            // Act
            var tierInfo = await service.EvaluateTierAsync(user.Id);
            var wallet = await service.GetWalletAsync(user.Id);

            // Assert
            tierInfo.TierName.Should().Be("Diamond");
            wallet!.TotalSpend12Months.Should().Be(15000000m);
        }
    }
}
