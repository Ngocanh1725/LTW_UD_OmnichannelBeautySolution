using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Services;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class LoyaltyGamificationTests
    {
        private static ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "LoyaltyGamificationTestDb_" + Guid.NewGuid().ToString("N"))
                .Options;

            var context = new ApplicationDbContext(options);

            context.MemberTiers.AddRange(
                new MemberTier { Id = 1, TierName = "Silver", MinSpend = 0, MaxSpend = 4999999m, PointMultiplier = 1.0m, SortOrder = 1, IsActive = true },
                new MemberTier { Id = 2, TierName = "Gold", MinSpend = 5000000m, MaxSpend = 9999999m, PointMultiplier = 1.5m, SortOrder = 2, IsActive = true },
                new MemberTier { Id = 3, TierName = "Diamond", MinSpend = 10000000m, MaxSpend = null, PointMultiplier = 2.0m, SortOrder = 3, IsActive = true }
            );

            context.Users.AddRange(
                new User { Id = 1, Username = "alice", Email = "alice@test.vn", FullName = "Alice Nguyễn", PasswordHash = "hash", IsActive = true },
                new User { Id = 2, Username = "bob", Email = "bob@test.vn", FullName = "Bob Trần", PasswordHash = "hash", IsActive = true },
                new User { Id = 3, Username = "charlie", Email = "charlie@test.vn", FullName = "Charlie Lê", PasswordHash = "hash", IsActive = true }
            );

            context.SaveChanges();
            return context;
        }

        [Fact]
        public async Task CheckInDailyAsync_AcrossVietnamMidnight_Calculates7DayProgressiveStreak_AndBlocksDuplicateSameDay()
        {
            using var context = CreateInMemoryContext();
            var service = new LoyaltyService(context, NullLogger<LoyaltyService>.Instance);

            // Mốc 1: 2026-11-01 16:50:00 UTC = 2026-11-01 23:50:00 (Giờ VN UTC+7) -> Ngày 1 (+10 điểm)
            var day1LateNightUtc = new DateTime(2026, 11, 1, 16, 50, 0, DateTimeKind.Utc);
            var res1 = await service.CheckInDailyAsync(1, day1LateNightUtc);
            Assert.True(res1.Success);
            Assert.Equal(1, res1.CurrentStreak);
            Assert.Equal(10, res1.PointsAwarded);
            Assert.Equal(10, res1.NewBalance);

            // Cố tình điểm danh lần 2 cách đó 8 phút: 16:58:00 UTC = 23:58:00 (cùng ngày 01/11 giờ VN) -> Bị chặn!
            var sameVnDayUtc = new DateTime(2026, 11, 1, 16, 58, 0, DateTimeKind.Utc);
            var duplicateRes = await service.CheckInDailyAsync(1, sameVnDayUtc);
            Assert.False(duplicateRes.Success);
            Assert.Equal(0, duplicateRes.PointsAwarded);
            Assert.Equal(10, duplicateRes.NewBalance);

            // Bước qua mốc nửa đêm VN (00:05:00 ngày 02/11 giờ VN = 17:05:00 ngày 01/11 UTC) -> Ngày 2 (+10 điểm)
            var day2JustAfterVnMidnightUtc = new DateTime(2026, 11, 1, 17, 5, 0, DateTimeKind.Utc);
            var res2 = await service.CheckInDailyAsync(1, day2JustAfterVnMidnightUtc);
            Assert.True(res2.Success);
            Assert.Equal(2, res2.CurrentStreak);
            Assert.Equal(10, res2.PointsAwarded);
            Assert.Equal(20, res2.NewBalance);

            // Tiếp tục điểm danh từ Ngày 3 đến Ngày 7 (20 - 20 - 30 - 30 - 100)
            int[] expectedPoints = { 20, 20, 30, 30, 100 };
            for (int i = 0; i < expectedPoints.Length; i++)
            {
                int dayNum = i + 3;
                var nextDayUtc = day2JustAfterVnMidnightUtc.AddDays(i + 1);
                var r = await service.CheckInDailyAsync(1, nextDayUtc);
                Assert.True(r.Success);
                Assert.Equal(dayNum, r.CurrentStreak);
                Assert.Equal(expectedPoints[i], r.PointsAwarded);
            }

            // Tổng điểm sau 7 ngày: 10 + 10 + 20 + 20 + 30 + 30 + 100 = 220 điểm
            var wallet = await service.GetWalletAsync(1);
            Assert.NotNull(wallet);
            Assert.Equal(220, wallet!.CurrentBalance);
            Assert.Equal(7, wallet.CurrentStreak);
        }

        [Fact]
        public async Task CheckInDailyAsync_WhenSkippingADay_ResetsStreakBackToDay1()
        {
            using var context = CreateInMemoryContext();
            var service = new LoyaltyService(context, NullLogger<LoyaltyService>.Instance);

            var startUtc = new DateTime(2026, 11, 1, 3, 0, 0, DateTimeKind.Utc); // 10:00 sáng giờ VN

            // Ngày 1 (+10), Ngày 2 (+10), Ngày 3 (+20)
            await service.CheckInDailyAsync(1, startUtc);
            await service.CheckInDailyAsync(1, startUtc.AddDays(1));
            var day3 = await service.CheckInDailyAsync(1, startUtc.AddDays(2));
            Assert.Equal(3, day3.CurrentStreak);
            Assert.Equal(20, day3.PointsAwarded);

            // Bỏ qua Ngày 4 (startUtc.AddDays(3)), điểm danh vào Ngày 5 (startUtc.AddDays(4)) -> Mất chuỗi, quay về Ngày 1 (+10)
            var afterSkip = await service.CheckInDailyAsync(1, startUtc.AddDays(4));
            Assert.True(afterSkip.Success);
            Assert.Equal(1, afterSkip.CurrentStreak);
            Assert.Equal(10, afterSkip.PointsAwarded);
        }

        [Fact]
        public async Task ApplyReferralCodeAsync_BlocksSelfReferral_AndCircularReferral_AndDuplicateReferral()
        {
            using var context = CreateInMemoryContext();
            var service = new LoyaltyService(context, NullLogger<LoyaltyService>.Instance);

            var aliceWallet = await service.GetOrCreateWalletAsync(1);
            var bobWallet = await service.GetOrCreateWalletAsync(2);
            var charlieWallet = await service.GetOrCreateWalletAsync(3);

            // 1. Chặn tự giới thiệu chính mình (Alice nhập mã của Alice)
            var selfRef = await service.ApplyReferralCodeAsync(1, aliceWallet.ReferralCode!);
            Assert.False(selfRef.Success);
            Assert.Contains("chính mình", selfRef.Message, StringComparison.OrdinalIgnoreCase);

            // 2. Hợp lệ: Bob nhập mã giới thiệu của Alice
            var validRef = await service.ApplyReferralCodeAsync(2, aliceWallet.ReferralCode!);
            Assert.True(validRef.Success);

            // 3. Chặn giới thiệu vòng lặp A <-> B (Alice nhập ngược lại mã của Bob)
            var circularRef = await service.ApplyReferralCodeAsync(1, bobWallet.ReferralCode!);
            Assert.False(circularRef.Success);
            Assert.Contains("vòng lặp", circularRef.Message, StringComparison.OrdinalIgnoreCase);

            // 4. Chặn nhập mã giới thiệu lần 2 (Bob tiếp tục nhập mã của Charlie)
            var duplicateRef = await service.ApplyReferralCodeAsync(2, charlieWallet.ReferralCode!);
            Assert.False(duplicateRef.Success);
        }

        [Fact]
        public async Task PointLedgerInvariant_AllGamificationOperations_BalanceMatchesSumOfPointTransactions()
        {
            using var context = CreateInMemoryContext();
            var service = new LoyaltyService(context, NullLogger<LoyaltyService>.Instance);
            var promoEngine = new PromotionEngine(context, NullLogger<PromotionEngine>.Instance);

            // Khởi tạo dữ liệu điều kiện nhiệm vụ cho User 2 (Bob)
            context.CustomerProfiles.Add(new CustomerProfile { UserId = 2, CurrentSkinType = "Da hỗn hợp thiên dầu" });
            context.SaveChanges();

            // 1. Bob điểm danh Ngày 1 (+10 điểm)
            var checkIn = await service.CheckInDailyAsync(2, new DateTime(2026, 11, 4, 2, 0, 0, DateTimeKind.Utc));
            Assert.True(checkIn.Success);
            Assert.Equal(10, checkIn.PointsAwarded);

            // 2. Bob nhận thưởng Nhiệm vụ Skin Quiz (+30 điểm)
            var mission = await service.ClaimMissionRewardAsync(2, "MISSION_SKIN_QUIZ");
            Assert.True(mission.Success);
            Assert.Equal(30, mission.PointsAwarded);

            // Chặn nhận trùng nhiệm vụ
            var duplicateMission = await service.ClaimMissionRewardAsync(2, "MISSION_SKIN_QUIZ");
            Assert.False(duplicateMission.Success);

            // 3. Bob nhập mã giới thiệu của Alice và hoàn tất đơn đầu tiên (+50 điểm cho Bob, +100 điểm cho Alice)
            var aliceWallet = await service.GetOrCreateWalletAsync(1);
            await service.ApplyReferralCodeAsync(2, aliceWallet.ReferralCode!);
            var refReward = await service.ProcessReferralFirstOrderCompletedAsync(2, orderId: 999);
            Assert.True(refReward.Success);
            Assert.Equal(50, refReward.PointsAwarded);

            // Tổng điểm của Bob lúc này: 10 (Check-in) + 30 (Skin Quiz) + 50 (Referral) = 90 điểm
            var bobWalletBeforeRedeem = await service.GetWalletAsync(2);
            Assert.Equal(90, bobWalletBeforeRedeem!.CurrentBalance);

            // 4. Bob đổi 50 điểm tại Omni Rewards Bazaar lấy Voucher 50.000₫ (-50 điểm -> còn 40 điểm)
            var redeem = await service.RedeemPointsAsync(2, 50, "Đổi 50 điểm lấy Voucher 50.000₫");
            Assert.Equal(40, redeem.NewBalance);
            Assert.NotNull(redeem.VoucherCode);

            // 5. Kiểm tra Voucher vừa đổi dùng được ngay tại PromotionEngine (Checkout)
            var validation = await promoEngine.ValidateCouponDetailAsync(redeem.VoucherCode!, 2, 200000m);
            Assert.True(validation.IsValid);
            Assert.Equal(50000m, validation.DiscountValue);

            // 6. KIỂM TRA BẤT BIẾN SỔ CÁI (LEDGER INVARIANT) CHO CẢ ALICE VÀ BOB
            foreach (var uid in new[] { 1, 2 })
            {
                var wallet = await context.LoyaltyWallets.FirstAsync(w => w.UserId == uid);
                var ledgerEntries = await context.PointTransactions
                    .Where(t => t.WalletId == wallet.Id)
                    .OrderBy(t => t.Id)
                    .ToListAsync();

                Assert.NotEmpty(ledgerEntries);

                int sumPoints = ledgerEntries.Sum(t => t.Points);
                Assert.Equal(sumPoints, wallet.CurrentBalance);

                int runningBalance = 0;
                foreach (var entry in ledgerEntries)
                {
                    Assert.Equal(runningBalance, entry.BalanceBefore);
                    runningBalance += entry.Points;
                    Assert.Equal(runningBalance, entry.BalanceAfter);
                }
            }
        }
    }
}

