using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Loyalty
{
    public class CustomerDashboardViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? AvatarUrl { get; set; }

        // Loyalty Card Info
        public int WalletId { get; set; }
        public string TierName { get; set; } = "GOLD"; // BRONZE, SILVER, GOLD, DIAMOND
        public string TierNormalized => (TierName ?? "BRONZE").ToUpperInvariant();
        public decimal PointMultiplier { get; set; } = 1.5m;
        public int CurrentPoints { get; set; } = 850;
        public int TotalPointsEarned { get; set; } = 1250;
        public decimal AvailableDiscountVnd => CurrentPoints * 1000m; // 1 điểm = 1.000 VNĐ
        public decimal TotalSpend12Months { get; set; } = 8550000m;
        public DateTime MemberSince { get; set; } = DateTime.UtcNow.AddMonths(-10);
        public DateTime TierExpirationDate { get; set; } = new DateTime(DateTime.UtcNow.Year, 12, 31);
        public string QrCodeData { get; set; } = string.Empty;
        public string BarcodeNumber { get; set; } = string.Empty;

        // Upgrade Progress
        public string NextTierName { get; set; } = "DIAMOND";
        public decimal SpendNeededForNextTier { get; set; } = 1450000m;
        public decimal CurrentTierMinSpend { get; set; } = 2000000m;
        public decimal NextTierMinSpend { get; set; } = 10000000m;
        public int ProgressPercentage { get; set; } = 85;
        public string UpgradeDeadlineText { get; set; } = "31/12";

        // Lists
        public List<VoucherDto> AvailableVouchers { get; set; } = new();
        public List<PointTransactionDto> RecentTransactions { get; set; } = new();
        public List<MemberTierPrivilegeDto> TierPrivileges { get; set; } = new();

        // Gamification (GAP-08 - Phiên C7)
        public DailyCheckInStatusDto CheckInStatus { get; set; } = new();
        public ReferralDashboardDto ReferralInfo { get; set; } = new();
        public List<BeautyMissionDto> BeautyMissions { get; set; } = new();
        public List<RewardCatalogItemDto> RewardsCatalog { get; set; } = new();
    }

    public class MemberTierPrivilegeDto
    {
        public string TierCode { get; set; } = string.Empty; // BRONZE, SILVER, GOLD, DIAMOND
        public string TierName { get; set; } = string.Empty;
        public decimal MinSpend { get; set; }
        public string CashBackRate { get; set; } = string.Empty; // e.g. 1% (x1.0), 1.5% (x1.5), 2% (x2.0), 3% (x3.0)
        public string BirthdayGift { get; set; } = string.Empty;
        public string ShippingPrivilege { get; set; } = string.Empty;
        public string FragranceTesting { get; set; } = string.Empty;
        public string ConsultantPrivilege { get; set; } = string.Empty;
        public bool IsCurrentTier { get; set; }
    }
}
