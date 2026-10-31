using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Crm
{
    public class CustomerFilterRequest
    {
        public string? Keyword { get; set; }
        public int? TierId { get; set; }
        public string? SortBy { get; set; } = "spend_desc"; // spend_desc, spend_asc, points_desc, points_asc, newest, oldest, name_asc
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class CustomerListItemDto
    {
        public int Id { get; set; } // UserId
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string TierName { get; set; } = "Thành viên";
        public string? TierBadgeIcon { get; set; }
        public int CurrentPoints { get; set; }
        public decimal TotalSpend12Months { get; set; }
        public decimal TotalSpendAllTime { get; set; }
        public int TotalOrders { get; set; }
        public DateTime MemberSince { get; set; }
        public bool IsActive { get; set; }
        public string? CurrentSkinType { get; set; }
    }

    public class MemberTierOptionDto
    {
        public int Id { get; set; }
        public string TierName { get; set; } = string.Empty;
        public decimal MinSpend { get; set; }
        public decimal? MaxSpend { get; set; }
        public decimal PointMultiplier { get; set; }
        public int MemberCount { get; set; }
    }

    public class CustomerPagedListViewModel
    {
        public List<CustomerListItemDto> Items { get; set; } = new();
        public CustomerFilterRequest Filter { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / (PageSize > 0 ? PageSize : 10));
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
        public Dictionary<string, int> TierCounts { get; set; } = new();
        public List<MemberTierOptionDto> AvailableTiers { get; set; } = new();
    }

    public class Customer360ProfileViewModel
    {
        // Core Identity
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime MemberSince { get; set; }
        public string? QrCodeData { get; set; }

        // Loyalty Tier & Metrics
        public int TierId { get; set; }
        public string TierName { get; set; } = "Silver";
        public decimal PointMultiplier { get; set; } = 1.0m;
        public decimal MinSpend { get; set; }
        public decimal? MaxSpend { get; set; }
        public int CurrentPoints { get; set; }
        public int TotalPointsEarned { get; set; }
        public decimal TotalSpend12Months { get; set; }
        public decimal TotalSpendAllTime { get; set; }
        public int TotalOrdersCount { get; set; }
        public int CompletedOrdersCount { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal SpendNeededForNextTier { get; set; }
        public string? NextTierName { get; set; }

        // 12-Month Spending Trend (for Chart.js Line Chart)
        public List<string> MonthlySpendingLabels { get; set; } = new();
        public List<decimal> MonthlySpendingValues { get; set; } = new();

        // Beauty & Skincare Profile
        public bool HasBeautyProfile { get; set; }
        public int? ProfileId { get; set; }
        public string? CurrentSkinType { get; set; }
        public List<string> SkinConcerns { get; set; } = new();
        public List<string> KnownAllergies { get; set; } = new();
        public List<string> PreferredBrands { get; set; } = new();
        public string? ConsultantNote { get; set; }
        public DateTime? LastConsultationDate { get; set; }
        public int? ConsultantUserId { get; set; }
        public string? ConsultantName { get; set; }
        public List<CustomerSkinHistoryDto> SkinHistories { get; set; } = new();

        // Orders History
        public List<CustomerOrderSummaryDto> Orders { get; set; } = new();

        // Loyalty Point Transactions History
        public List<CustomerPointTransactionDto> PointTransactions { get; set; } = new();
    }

    public class CustomerSkinHistoryDto
    {
        public int Id { get; set; }
        public DateTime RecordedAt { get; set; }
        public string SkinType { get; set; } = string.Empty;
        public string? SkinCondition { get; set; }
        public string? TreatmentNote { get; set; }
        public string? RecordedByName { get; set; }
        public List<string> ImageUrls { get; set; } = new();
    }

    public class CustomerOrderSummaryDto
    {
        public int Id { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string OrderSource { get; set; } = "POS";
        public DateTime CreatedAt { get; set; }
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; } = "PENDING";
        public string PaymentStatus { get; set; } = "UNPAID";
        public string PaymentMethod { get; set; } = "CASH";
        public int ItemsCount { get; set; }
    }

    public class CustomerPointTransactionDto
    {
        public int Id { get; set; }
        public string TransactionType { get; set; } = "EARN"; // EARN, REDEEM, BONUS, ADJUST
        public int Points { get; set; }
        public int BalanceBefore { get; set; }
        public int BalanceAfter { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CustomerEditDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }

        // Beauty Profile Fields
        public string? CurrentSkinType { get; set; }
        public string? SkinConcernsInput { get; set; } // Comma-separated or tag list
        public string? KnownAllergiesInput { get; set; }
        public string? PreferredBrandsInput { get; set; }
        public string? Note { get; set; }
        public int? ConsultantUserId { get; set; }
    }
}
