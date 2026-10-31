using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Omnichannel.Domain.Entities
{
    /// <summary>
    /// Các loại chương trình khuyến mãi trong hệ sinh thái bán lẻ mỹ phẩm
    /// </summary>
    public static class PromotionTypes
    {
        public const string Percentage = "PERCENTAGE";
        public const string FixedAmount = "FIXED_AMOUNT";
        public const string BuyXGetY = "BUY_X_GET_Y";
        public const string TieredGwp = "TIERED_GWP";
        public const string FlashSale = "FLASH_SALE";
        public const string MemberTier = "MEMBER_TIER";
        public const string FefoClearance = "FEFO_CLEARANCE";
    }

    /// <summary>
    /// Phạm vi áp dụng của chương trình khuyến mãi
    /// </summary>
    public static class PromotionAppliesTo
    {
        public const string All = "ALL";
        public const string SpecificProducts = "SPECIFIC_PRODUCTS";
        public const string SpecificCategories = "SPECIFIC_CATEGORIES";
        public const string MemberTier = "MEMBER_TIER";
        public const string FefoClearance = "FEFO_CLEARANCE";
    }

    /// <summary>
    /// Mức chiết khấu đặc quyền tự động theo hạng thành viên (Silver, Gold, Diamond)
    /// </summary>
    public static class MemberTierDiscounts
    {
        public const decimal SilverPercent = 5.0m;
        public const decimal GoldPercent = 10.0m;
        public const decimal DiamondPercent = 15.0m;
    }

    /// <summary>
    /// Chương trình khuyến mãi (Flash Sale, Chiết khấu %, Giảm cố định, Mua X tặng Y, Quà tặng GWP theo bậc đơn, Hạng thành viên, FEFO Clearance)
    /// </summary>
    [Table("Promotions")]
    public class Promotion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string PromotionCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(30)]
        [Column(TypeName = "varchar(30)")]
        public string PromotionType { get; set; } = "PERCENTAGE"; // PERCENTAGE, FIXED_AMOUNT, BUY_X_GET_Y, TIERED_GWP, FLASH_SALE

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; } // % giảm giá hoặc số tiền giảm

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinOrderValue { get; set; } // Giá trị đơn hàng tối thiểu để áp dụng

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxDiscountAmount { get; set; } // Giới hạn số tiền giảm tối đa (khi giảm %)

        [Column(TypeName = "datetime2(7)")]
        public DateTime ValidFrom { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime ValidTo { get; set; }

        public int? UsageLimit { get; set; } // Giới hạn tổng số lượt sử dụng toàn hệ thống

        public int UsageCount { get; set; } = 0; // Số lượt đã áp dụng

        public bool IsActive { get; set; } = true;

        [Required]
        [MaxLength(30)]
        [Column(TypeName = "varchar(30)")]
        public string AppliesTo { get; set; } = "ALL"; // ALL, SPECIFIC_PRODUCTS, SPECIFIC_CATEGORIES

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public virtual ICollection<PromotionItem> PromotionItems { get; set; } = new List<PromotionItem>();
    }

    /// <summary>
    /// Danh sách sản phẩm, danh mục hoặc quà tặng liên kết với chương trình khuyến mãi
    /// </summary>
    [Table("PromotionItems")]
    public class PromotionItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int PromotionId { get; set; }

        public int? ProductId { get; set; }

        public int? CategoryId { get; set; }

        public int? GiftProductId { get; set; } // Sản phẩm quà tặng kèm (cho BUY_X_GET_Y hoặc GWP)

        public int GiftQuantity { get; set; } = 1;

        // Navigations
        public virtual Promotion Promotion { get; set; } = null!;
        public virtual Product? Product { get; set; }
        public virtual Category? Category { get; set; }
        public virtual Product? GiftProduct { get; set; }
    }

    /// <summary>
    /// Mã giảm giá / Coupon nhập tay của khách hàng
    /// </summary>
    [Table("Coupons")]
    public class Coupon
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string CouponCode { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string DiscountType { get; set; } = "PERCENTAGE"; // PERCENTAGE, FIXED_AMOUNT

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinOrderValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxDiscountAmount { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime ValidFrom { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime ValidTo { get; set; }

        public int? UsageLimit { get; set; } // Giới hạn tổng số lượt sử dụng

        public int? UsageLimitPerUser { get; set; } = 1; // Giới hạn số lượt/mỗi khách hàng

        public int UsageCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<CouponUsage> Usages { get; set; } = new List<CouponUsage>();
    }

    /// <summary>
    /// Lịch sử sử dụng Coupon trên từng đơn hàng
    /// </summary>
    [Table("CouponUsages")]
    public class CouponUsage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int CouponId { get; set; }

        public int? UserId { get; set; }

        public int? OrderId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountApplied { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime UsedAt { get; set; } = DateTime.UtcNow;

        // Navigations
        public virtual Coupon Coupon { get; set; } = null!;
        public virtual User? User { get; set; }
        public virtual Order? Order { get; set; }
    }
}
