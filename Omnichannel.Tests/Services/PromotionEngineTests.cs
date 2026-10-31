using System;
using System.Collections.Generic;
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
    public class PromotionEngineTests
    {
        [Fact]
        public async Task ApplyPercentageDiscount_10Percent_CalculatesCorrectly()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var engine = new PromotionEngine(context, NullLogger<PromotionEngine>.Instance);

            var promo = new Promotion
            {
                PromotionCode = "PROMO10",
                Name = "Giảm 10% toàn sàn",
                PromotionType = "PERCENTAGE",
                DiscountValue = 10m,
                AppliesTo = "ALL",
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(7),
                IsActive = true
            };
            context.Promotions.Add(promo);
            await context.SaveChangesAsync();

            var items = new List<CartItemDto>
            {
                new CartItemDto { ProductId = 1, ProductName = "Son Romand", Quantity = 2, UnitPrice = 200000m }
            };

            // Act: 400,000 VND * 10% = 40,000 VND discount
            var result = await engine.EvaluateCartAsync(items);

            // Assert
            result.SubTotal.Should().Be(400000m);
            result.PromotionDiscount.Should().Be(40000m);
            result.AppliedPromotionCode.Should().Be("PROMO10");
        }

        [Fact]
        public async Task ApplyFixedDiscount_50000VND_DeductsFromTotal()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var engine = new PromotionEngine(context, NullLogger<PromotionEngine>.Instance);

            var promo = new Promotion
            {
                PromotionCode = "FIXED50K",
                Name = "Giảm trực tiếp 50K",
                PromotionType = "FIXED_AMOUNT",
                DiscountValue = 50000m,
                AppliesTo = "ALL",
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(7),
                IsActive = true
            };
            context.Promotions.Add(promo);
            await context.SaveChangesAsync();

            var items = new List<CartItemDto>
            {
                new CartItemDto { ProductId = 2, ProductName = "Kem chống nắng", Quantity = 1, UnitPrice = 350000m }
            };

            // Act
            var result = await engine.EvaluateCartAsync(items);

            // Assert
            result.SubTotal.Should().Be(350000m);
            result.PromotionDiscount.Should().Be(50000m);
        }

        [Fact]
        public async Task ApplyPercentage_WithMaxCap_CapsAtMaxAmount()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var engine = new PromotionEngine(context, NullLogger<PromotionEngine>.Instance);

            // Giảm 20% tối đa 100K
            var promo = new Promotion
            {
                PromotionCode = "CAP20",
                Name = "Giảm 20% tối đa 100K",
                PromotionType = "PERCENTAGE",
                DiscountValue = 20m,
                MaxDiscountAmount = 100000m,
                AppliesTo = "ALL",
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(7),
                IsActive = true
            };
            context.Promotions.Add(promo);
            await context.SaveChangesAsync();

            // Đơn 1,000,000 VND (20% là 200,000 -> nhưng bị chặn ở 100,000)
            var items = new List<CartItemDto>
            {
                new CartItemDto { ProductId = 3, ProductName = "Serum Retinol", Quantity = 2, UnitPrice = 500000m }
            };

            // Act
            var result = await engine.EvaluateCartAsync(items);

            // Assert
            result.SubTotal.Should().Be(1000000m);
            result.PromotionDiscount.Should().Be(100000m);
        }

        [Fact]
        public async Task ValidateCoupon_ValidCode_ReturnsTrue()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var engine = new PromotionEngine(context, NullLogger<PromotionEngine>.Instance);

            var coupon = new Coupon
            {
                CouponCode = "BEAUTY10",
                Description = "Mã ưu đãi mỹ phẩm",
                DiscountType = "PERCENTAGE",
                DiscountValue = 10m,
                MinOrderValue = 100000m,
                UsageLimit = 100,
                UsageCount = 10,
                ValidFrom = DateTime.UtcNow.AddDays(-2),
                ValidTo = DateTime.UtcNow.AddDays(5),
                IsActive = true
            };
            context.Coupons.Add(coupon);
            await context.SaveChangesAsync();

            // Act
            var isValid = await engine.ValidateCouponAsync("BEAUTY10", userId: 1, orderTotal: 250000m);

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateCoupon_ExpiredCode_ReturnsFalse()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var engine = new PromotionEngine(context, NullLogger<PromotionEngine>.Instance);

            var coupon = new Coupon
            {
                CouponCode = "EXPIRED",
                Description = "Mã hết hạn",
                DiscountType = "FIXED_AMOUNT",
                DiscountValue = 50000m,
                ValidFrom = DateTime.UtcNow.AddDays(-20),
                ValidTo = DateTime.UtcNow.AddDays(-5), // Đã hết hạn 5 ngày trước
                IsActive = true
            };
            context.Coupons.Add(coupon);
            await context.SaveChangesAsync();

            // Act
            var isValid = await engine.ValidateCouponAsync("EXPIRED", userId: 1, orderTotal: 200000m);

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateCoupon_UsageLimitReached_ReturnsFalse()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var engine = new PromotionEngine(context, NullLogger<PromotionEngine>.Instance);

            var coupon = new Coupon
            {
                CouponCode = "LIMITED",
                Description = "Mã giới hạn 5 lượt",
                DiscountType = "FIXED_AMOUNT",
                DiscountValue = 30000m,
                UsageLimit = 5,
                UsageCount = 5, // Đã hết lượt
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(10),
                IsActive = true
            };
            context.Coupons.Add(coupon);
            await context.SaveChangesAsync();

            // Act
            var isValid = await engine.ValidateCouponAsync("LIMITED", userId: 1, orderTotal: 200000m);

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateCoupon_BelowMinOrder_ReturnsFalse()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var engine = new PromotionEngine(context, NullLogger<PromotionEngine>.Instance);

            var coupon = new Coupon
            {
                CouponCode = "MIN500K",
                Description = "Đơn từ 500K",
                DiscountType = "FIXED_AMOUNT",
                DiscountValue = 50000m,
                MinOrderValue = 500000m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(10),
                IsActive = true
            };
            context.Coupons.Add(coupon);
            await context.SaveChangesAsync();

            // Act: Đơn chỉ có 300.000 < 500.000
            var isValid = await engine.ValidateCouponAsync("MIN500K", userId: 1, orderTotal: 300000m);

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public async Task EvaluateCart_WithBothPromotionAndCoupon_CalculatesSeparateDiscounts()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var engine = new PromotionEngine(context, NullLogger<PromotionEngine>.Instance);

            // Promotion 10%
            var promo = new Promotion
            {
                PromotionCode = "AUTOPROMO",
                Name = "KM Tự Động 10%",
                Description = "KM Tự Động 10%",
                PromotionType = "PERCENTAGE",
                DiscountValue = 10m,
                AppliesTo = "ALL",
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(10),
                IsActive = true
            };
            context.Promotions.Add(promo);

            // Coupon 50K
            var coupon = new Coupon
            {
                CouponCode = "SAVE50K",
                Description = "Mã Giảm 50K",
                DiscountType = "FIXED_AMOUNT",
                DiscountValue = 50000m,
                MinOrderValue = 200000m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(10),
                IsActive = true
            };
            context.Coupons.Add(coupon);
            await context.SaveChangesAsync();

            var items = new List<CartItemDto>
            {
                new CartItemDto { ProductId = 1, ProductName = "Kem Dưỡng", Quantity = 2, UnitPrice = 250000m }
            };

            // Act: Subtotal = 500K. Promo 10% = 50K. Remaining = 450K. Coupon 50K = 50K.
            var result = await engine.EvaluateCartAsync(items, couponCode: "SAVE50K", userId: 1);

            // Assert
            result.SubTotal.Should().Be(500000m);
            result.PromotionDiscount.Should().Be(50000m);
            result.CouponDiscount.Should().Be(50000m);
            result.TotalDiscount.Should().Be(100000m);
            result.FinalTotal.Should().Be(400000m);
        }

        [Fact]
        public async Task ValidateCouponDetail_BelowMinOrder_ReturnsDetailedMessageAndMissingAmount()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var engine = new PromotionEngine(context, NullLogger<PromotionEngine>.Instance);

            var coupon = new Coupon
            {
                CouponCode = "MIN600K",
                Description = "Đơn từ 600K",
                DiscountType = "FIXED_AMOUNT",
                DiscountValue = 50000m,
                MinOrderValue = 600000m,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(5),
                IsActive = true
            };
            context.Coupons.Add(coupon);
            await context.SaveChangesAsync();

            // Act: Order total is only 400.000 < 600.000
            var val = await engine.ValidateCouponDetailAsync("MIN600K", userId: 1, orderTotal: 400000m);

            // Assert
            val.IsValid.Should().BeFalse();
            val.ErrorCode.Should().Be("MIN_ORDER_NOT_MET");
            val.MissingAmount.Should().Be(200000m);
            val.Message.Should().Contain("600.000₫");
            val.Message.Should().Contain("200.000₫");
        }

        [Fact]
        public async Task EvaluateCart_WithMemberTierGold_Applies10PercentDiscountAutomatically()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var engine = new PromotionEngine(context, NullLogger<PromotionEngine>.Instance);

            var goldTier = new MemberTier { Id = 10, TierName = "Gold", MinSpend = 2000000m, PointMultiplier = 1.5m, IsActive = true };
            context.MemberTiers.Add(goldTier);

            var wallet = new LoyaltyWallet { Id = 10, UserId = 99, CurrentTierId = 10, CurrentTier = goldTier };
            context.LoyaltyWallets.Add(wallet);
            await context.SaveChangesAsync();

            var items = new List<CartItemDto>
            {
                new CartItemDto { ProductId = 101, ProductName = "Serum Niacinamide", Quantity = 2, UnitPrice = 300000m }
            };

            // Act: Subtotal = 600,000 VND. Gold tier discount = 10% = 60,000 VND
            var result = await engine.EvaluateCartAsync(items, userId: 99);

            // Assert
            result.SubTotal.Should().Be(600000m);
            result.MemberTierName.Should().Be("Gold");
            result.MemberDiscountPercent.Should().Be(10m);
            result.MemberTierDiscount.Should().Be(60000m);
            result.TotalDiscount.Should().Be(60000m);
            result.FinalTotal.Should().Be(540000m);
        }

        [Fact]
        public async Task EvaluateCart_WithSunscreen_AddsBuyXGetYGiftAutomatically()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var engine = new PromotionEngine(context, NullLogger<PromotionEngine>.Instance);

            var items = new List<CartItemDto>
            {
                new CartItemDto { ProductId = 202, ProductName = "Kem chống nắng Anessa Perfect UV Milk 60ml", Quantity = 2, UnitPrice = 450000m }
            };

            // Act
            var result = await engine.EvaluateCartAsync(items);

            // Assert: Buy 2 sunscreens -> 2 gifts (Bông tẩy trang / Son mini)
            result.GiftItems.Should().NotBeEmpty();
            result.GiftItems.Should().Contain(g => g.Quantity == 2 && g.GiftReason.Contains("Kem chống nắng"));
        }

        [Fact]
        public async Task EvaluateCart_WithNearExpiryFEFOItem_AppliesClearanceDiscount()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var engine = new PromotionEngine(context, NullLogger<PromotionEngine>.Instance);

            var items = new List<CartItemDto>
            {
                new CartItemDto 
                { 
                    ProductId = 303, 
                    ProductName = "Mặt nạ ngủ Collagen Cận Date", 
                    Quantity = 1, 
                    UnitPrice = 200000m,
                    IsNearExpiration = true,
                    DaysLeftToExpiry = 45,
                    BatchNumber = "LOT-FEFO-NEAR"
                }
            };

            // Act: Default FEFO clearance discount 20% = 40,000 VND
            var result = await engine.EvaluateCartAsync(items);

            // Assert
            result.FefoDiscount.Should().Be(40000m);
            result.FefoClearanceItems.Should().HaveCount(1);
            result.FefoClearanceItems[0].BatchNumber.Should().Be("LOT-FEFO-NEAR");
            result.TotalDiscount.Should().Be(40000m);
            result.FinalTotal.Should().Be(160000m);
        }
    }
}
