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
}
