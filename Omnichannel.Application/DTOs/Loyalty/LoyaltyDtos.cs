using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Loyalty
{
    public class TierInfoDto
    {
        public int Id { get; set; }
        public string TierName { get; set; } = string.Empty;
        public decimal MinSpend { get; set; }
        public decimal? MaxSpend { get; set; }
        public decimal PointMultiplier { get; set; }
        public int BirthdayBonusPoints { get; set; }
        public string? Description { get; set; }
        public string? BadgeIconUrl { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class PointTransactionDto
    {
        public int Id { get; set; }
        public int WalletId { get; set; }
        public int? OrderId { get; set; }
        public string? OrderCode { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public int Points { get; set; }
        public int BalanceBefore { get; set; }
        public int BalanceAfter { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LoyaltyWalletDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public TierInfoDto? CurrentTier { get; set; }
        public int TotalPointsEarned { get; set; }
        public int CurrentBalance { get; set; }
        public decimal AvailableDiscountVnd => CurrentBalance * 1000m; // 1 điểm = 1.000 VNĐ
        public decimal TotalSpend12Months { get; set; }
        public DateTime MemberSince { get; set; }
        public DateTime? LastTierEvaluationDate { get; set; }
        public string? NextTierName { get; set; }
        public decimal? SpendNeededForNextTier { get; set; }
        public string? QrCodeData { get; set; }
        public string? ReferralCode { get; set; }
        public int CurrentStreak { get; set; }
        public DateTime? LastCheckInDate { get; set; }
        public List<PointTransactionDto> RecentTransactions { get; set; } = new();
    }

    public class VoucherDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string VoucherCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DiscountType { get; set; } = "PERCENTAGE"; // PERCENTAGE, FIXED_AMOUNT
        public decimal DiscountValue { get; set; }
        public decimal MinOrderValue { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public bool IsUsed { get; set; }
        public DateTime? UsedAt { get; set; }
        public int? UsedOrderId { get; set; }
        public string? UsedOrderCode { get; set; }
        public bool IsValid => !IsUsed && DateTime.UtcNow >= ValidFrom && DateTime.UtcNow <= ValidTo;
    }

    public class EarnPointsDto
    {
        public int UserId { get; set; }
        public int OrderId { get; set; }
        public decimal OrderAmount { get; set; }
        public int BasePoints { get; set; }
        public int PointsEarned { get; set; }
        public int NewBalance { get; set; }
        public decimal TierMultiplier { get; set; }
        public string TierName { get; set; } = string.Empty;
    }

    public class RedeemPointsDto
    {
        public int UserId { get; set; }
        public int PointsToRedeem { get; set; }
        public decimal DiscountAmountVnd { get; set; }
        public int NewBalance { get; set; }
        public string? VoucherCode { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class AdjustPointsRequest
    {
        public int UserId { get; set; }
        public int Points { get; set; } // Số điểm cần điều chỉnh (dương để cộng, âm để trừ)
        public string Reason { get; set; } = string.Empty; // Lý do điều chỉnh (Thưởng sự kiện, Bù điểm, Thu hồi hoàn đơn...)
        public string? Note { get; set; } // Ghi chú chi tiết của quản trị viên
    }

    public class LoyaltySummaryDto
    {
        public int TotalMembers { get; set; }
        public int TotalActivePoints { get; set; }
        public int TotalLifetimePoints { get; set; }
        public int SilverCount { get; set; }
        public int GoldCount { get; set; }
        public int DiamondCount { get; set; }
        public decimal TotalSpendOverall { get; set; }
    }

    // =========================================================================
    // GAMIFICATION LOYALTY DTOs (GAP-08 - PHIÊN C7)
    // =========================================================================

    public class DailyCheckInSlotDto
    {
        public int DayNumber { get; set; } // 1..7
        public int Points { get; set; } // 10, 10, 20, 20, 30, 30, 100
        public bool IsCheckedIn { get; set; }
        public bool IsTodaySlot { get; set; }
        public bool IsJackpot => DayNumber == 7;
    }

    public class DailyCheckInStatusDto
    {
        public int CurrentStreak { get; set; }
        public bool CheckedInToday { get; set; }
        public string TodayVietnamDate { get; set; } = string.Empty;
        public int NextPoints { get; set; }
        public List<DailyCheckInSlotDto> Slots { get; set; } = new();
    }

    public class DailyCheckInResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int PointsAwarded { get; set; }
        public int CurrentStreak { get; set; }
        public int NewBalance { get; set; }
        public bool CheckedInToday { get; set; }
    }

    public class ReferralItemDto
    {
        public int Id { get; set; }
        public int ReferredUserId { get; set; }
        public string ReferredUserName { get; set; } = string.Empty;
        public string Status { get; set; } = "PENDING"; // PENDING, REWARDED
        public int PointsAwarded { get; set; }
        public string? RewardVoucherCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RewardedAt { get; set; }
    }

    public class ReferralDashboardDto
    {
        public string MyReferralCode { get; set; } = string.Empty;
        public int TotalReferredCount { get; set; }
        public int RewardedReferredCount { get; set; }
        public int TotalPointsEarnedFromReferrals { get; set; }
        public bool CanApplyReferralCode { get; set; }
        public string? AppliedReferralCode { get; set; }
        public string? ReferredByStatus { get; set; }
        public List<ReferralItemDto> Referrals { get; set; } = new();
    }

    public class ApplyReferralResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ReferralCode { get; set; }
        public int PointsAwarded { get; set; }
        public string? VoucherCode { get; set; }
        public int NewBalance { get; set; }
    }

    public class BeautyMissionDto
    {
        public string MissionCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconClass { get; set; } = "fa-solid fa-star";
        public int PointsReward { get; set; }
        public bool IsConditionMet { get; set; }
        public bool IsClaimed { get; set; }
        public string ActionUrl { get; set; } = "/Shop";
        public string ActionText { get; set; } = "Thực hiện ngay";
    }

    public class ClaimMissionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string MissionCode { get; set; } = string.Empty;
        public int PointsAwarded { get; set; }
        public int NewBalance { get; set; }
    }

    public class RewardCatalogItemDto
    {
        public string RewardCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string BadgeText { get; set; } = string.Empty;
        public int PointsCost { get; set; }
        public decimal DiscountValueVnd { get; set; }
        public decimal MinOrderValueVnd { get; set; }
        public bool CanAfford { get; set; }
    }
}
