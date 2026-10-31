using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Sales
{
    public class PromotionGiftItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImageUrl { get; set; }
        public int Quantity { get; set; } = 1;
        public string GiftReason { get; set; } = string.Empty;
    }

    public class FefoClearanceItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime? ExpDate { get; set; }
        public int DaysLeftToExpiry { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public int Quantity { get; set; }
    }

    public class PromotionValidationResultDto
    {
        public bool IsValid { get; set; }
        public string? CouponCode { get; set; }
        public string? ErrorCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal? MinOrderValue { get; set; }
        public decimal? MissingAmount { get; set; }
        public int? UsageLimit { get; set; }
        public int? UsageCount { get; set; }
        public int? UsageLimitPerUser { get; set; }
        public int? UserUsageCount { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public string? DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
    }

    public class PromotionResultDto
    {
        public decimal SubTotal { get; set; }

        // Khuyến mãi tự động theo CTKM (Giảm %, Giảm cố định, Flash Sale...)
        public decimal PromotionDiscount { get; set; }
        public int? AppliedPromotionId { get; set; }
        public string? AppliedPromotionCode { get; set; }
        public string? AppliedPromotionName { get; set; }

        // Khuyến mãi Coupon nhập tay
        public decimal CouponDiscount { get; set; }
        public string? AppliedCouponCode { get; set; }

        // Khuyến mãi Hạng thành viên (Silver -5%, Gold -10%, Diamond -15%)
        public decimal MemberTierDiscount { get; set; }
        public string? MemberTierName { get; set; }
        public decimal MemberDiscountPercent { get; set; }

        // Khuyến mãi Lô Hạn dùng cận date (FEFO Clearance)
        public decimal FefoDiscount { get; set; }
        public string? FefoDiscountDescription { get; set; }
        public List<FefoClearanceItemDto> FefoClearanceItems { get; set; } = new();

        // Tổng chiết khấu tổng hợp từ mọi cơ chế khuyến mãi
        public decimal TotalDiscount => PromotionDiscount + CouponDiscount + MemberTierDiscount + FefoDiscount;
        public decimal FinalTotal => Math.Max(0, SubTotal - TotalDiscount);

        // Danh sách quà tặng kèm (Mua X tặng Y, Quà GWP)
        public List<PromotionGiftItemDto> GiftItems { get; set; } = new();

        public string? Message { get; set; }
        public bool IsSuccess { get; set; } = true;
        public List<string> AppliedRulesSummary { get; set; } = new();
    }

    public class PromotionItemDto
    {
        public int Id { get; set; }
        public int PromotionId { get; set; }
        public int? ProductId { get; set; }
        public string? ProductName { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int? GiftProductId { get; set; }
        public string? GiftProductName { get; set; }
        public int GiftQuantity { get; set; } = 1;
    }

    public class PromotionDto
    {
        public int Id { get; set; }
        public string PromotionCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string PromotionType { get; set; } = "PERCENTAGE"; // PERCENTAGE, FIXED_AMOUNT, BUY_X_GET_Y, TIERED_GWP, FLASH_SALE
        public decimal DiscountValue { get; set; }
        public decimal? MinOrderValue { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public int? UsageLimit { get; set; }
        public int UsageCount { get; set; }
        public bool IsActive { get; set; }
        public string AppliesTo { get; set; } = "ALL";
        public List<PromotionItemDto> Items { get; set; } = new();
    }

    public class CreatePromotionDto
    {
        public string PromotionCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string PromotionType { get; set; } = "PERCENTAGE";
        public decimal DiscountValue { get; set; }
        public decimal? MinOrderValue { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public int? UsageLimit { get; set; }
        public string AppliesTo { get; set; } = "ALL";
        public List<PromotionItemDto>? Items { get; set; }
    }

    public class CouponDto
    {
        public int Id { get; set; }
        public string CouponCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string DiscountType { get; set; } = "PERCENTAGE"; // PERCENTAGE, FIXED_AMOUNT
        public decimal DiscountValue { get; set; }
        public decimal? MinOrderValue { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public int? UsageLimit { get; set; }
        public int? UsageLimitPerUser { get; set; }
        public int UsageCount { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateCouponDto
    {
        public string CouponCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string DiscountType { get; set; } = "PERCENTAGE";
        public decimal DiscountValue { get; set; }
        public decimal? MinOrderValue { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public int? UsageLimit { get; set; }
        public int? UsageLimitPerUser { get; set; } = 1;
    }

    public class CouponUsageDto
    {
        public int Id { get; set; }
        public string CouponCode { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public int? OrderId { get; set; }
        public decimal DiscountApplied { get; set; }
        public DateTime UsedAt { get; set; }
    }

    public class IssueVoucherDto
    {
        public string TargetGroup { get; set; } = "ALL_VIP"; // ALL_CUSTOMERS, VIP_DIAMOND, VIP_GOLD_DIAMOND, SPECIFIC_USER
        public int? SpecificUserId { get; set; }
        public string VoucherCodePrefix { get; set; } = "VIP";
        public string Description { get; set; } = string.Empty;
        public string DiscountType { get; set; } = "PERCENTAGE"; // PERCENTAGE, FIXED_AMOUNT
        public decimal DiscountValue { get; set; }
        public decimal MinOrderValue { get; set; } = 0;
        public decimal? MaxDiscountAmount { get; set; }
        public int ValidDays { get; set; } = 30;
    }
}
