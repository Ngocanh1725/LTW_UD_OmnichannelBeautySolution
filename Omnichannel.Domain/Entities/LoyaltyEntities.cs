using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Omnichannel.Domain.Entities
{
    /// <summary>
    /// Bậc hạng thành viên (Silver, Gold, Diamond...) với các đặc quyền và hệ số tích điểm
    /// </summary>
    [Table("MemberTiers")]
    public class MemberTier
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string TierName { get; set; } = string.Empty; // Silver, Gold, Diamond

        [Column(TypeName = "decimal(18,2)")]
        public decimal MinSpend { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxSpend { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal PointMultiplier { get; set; } = 1.0m; // 1.0, 1.5, 2.0

        public int BirthdayBonusPoints { get; set; } = 0;

        [MaxLength(255)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? BadgeIconUrl { get; set; }

        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<LoyaltyWallet> Wallets { get; set; } = new List<LoyaltyWallet>();
    }

    /// <summary>
    /// Ví điểm thành viên của khách hàng, liên kết 1-1 với User
    /// </summary>
    [Table("LoyaltyWallets")]
    public class LoyaltyWallet
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        public int UserId { get; set; }

        public int CurrentTierId { get; set; }

        public int TotalPointsEarned { get; set; } = 0; // Tổng điểm tích lũy trọn đời

        public int CurrentBalance { get; set; } = 0; // Số điểm khả dụng hiện tại

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalSpend12Months { get; set; } = 0; // Tổng chi tiêu 12 tháng gần nhất

        [Column(TypeName = "datetime2(7)")]
        public DateTime MemberSince { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? LastTierEvaluationDate { get; set; }

        [MaxLength(200)]
        [Column(TypeName = "varchar(200)")]
        public string? QrCodeData { get; set; } // Mã QR định danh thành viên

        [MaxLength(30)]
        [Column(TypeName = "varchar(30)")]
        public string? ReferralCode { get; set; } // Mã giới thiệu bạn bè (VD: OMNI-REF-0101)

        public int CurrentStreak { get; set; } = 0; // Chuỗi ngày điểm danh liên tiếp (0..7)

        [Column(TypeName = "datetime2(7)")]
        public DateTime? LastCheckInDate { get; set; } // Ngày điểm danh gần nhất (theo ngày giờ VN UTC+7)

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? UpdatedAt { get; set; }

        // Navigations
        public virtual User User { get; set; } = null!;
        public virtual MemberTier CurrentTier { get; set; } = null!;
        public virtual ICollection<PointTransaction> Transactions { get; set; } = new List<PointTransaction>();
        public virtual ICollection<LoyaltyCheckIn> CheckIns { get; set; } = new List<LoyaltyCheckIn>();
    }

    /// <summary>
    /// Nhật ký giao dịch điểm (Tích điểm, Đổi điểm, Hết hạn, Thưởng sinh nhật, Điều chỉnh)
    /// </summary>
    [Table("PointTransactions")]
    public class PointTransaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int WalletId { get; set; }

        public int? OrderId { get; set; }

        [Required]
        [MaxLength(30)]
        [Column(TypeName = "varchar(30)")]
        public string TransactionType { get; set; } = "EARN"; // EARN, REDEEM, EXPIRE, BONUS, ADJUST

        public int Points { get; set; } // Dương = Tích/Cộng, Âm = Đổi/Trừ

        public int BalanceBefore { get; set; }

        public int BalanceAfter { get; set; }

        [Required]
        [MaxLength(255)]
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? ExpiresAt { get; set; } // Hạn điểm (thường 12 tháng từ ngày tích)

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigations
        public virtual LoyaltyWallet Wallet { get; set; } = null!;
        public virtual Order? Order { get; set; }
    }

    /// <summary>
    /// Kho voucher cá nhân hoặc voucher chung
    /// </summary>
    [Table("Vouchers")]
    public class Voucher
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? UserId { get; set; } // null = voucher công khai

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string VoucherCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string DiscountType { get; set; } = "PERCENTAGE"; // PERCENTAGE, FIXED_AMOUNT

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MinOrderValue { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxDiscountAmount { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime ValidFrom { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime ValidTo { get; set; }

        public bool IsUsed { get; set; } = false;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? UsedAt { get; set; }

        public int? UsedOrderId { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigations
        public virtual User? User { get; set; }
        public virtual Order? UsedOrder { get; set; }
    }

    /// <summary>
    /// Lịch sử điểm danh hằng ngày 7 ngày (Gamification - GAP-08)
    /// </summary>
    [Table("LoyaltyCheckIns")]
    public class LoyaltyCheckIn
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int UserId { get; set; }

        public int WalletId { get; set; }

        /// <summary>
        /// Ngày điểm danh theo múi giờ Việt Nam (UTC+7), lưu phần Date (00:00:00)
        /// </summary>
        [Column(TypeName = "datetime2(7)")]
        public DateTime CheckInDate { get; set; }

        /// <summary>
        /// Ngày thứ mấy trong chuỗi 7 ngày (1..7)
        /// </summary>
        public int StreakDay { get; set; }

        /// <summary>
        /// Số điểm nhận được (10, 10, 20, 20, 30, 30, 100)
        /// </summary>
        public int PointsAwarded { get; set; }

        public int? PointTransactionId { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigations
        public virtual User User { get; set; } = null!;
        public virtual LoyaltyWallet Wallet { get; set; } = null!;
        public virtual PointTransaction? PointTransaction { get; set; }
    }

    /// <summary>
    /// Chương trình giới thiệu bạn bè (Referral Program - GAP-08)
    /// </summary>
    [Table("LoyaltyReferrals")]
    public class LoyaltyReferral
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Người giới thiệu
        /// </summary>
        public int ReferrerUserId { get; set; }

        /// <summary>
        /// Người được giới thiệu (mỗi người chỉ được giới thiệu 1 lần)
        /// </summary>
        public int ReferredUserId { get; set; }

        [Required]
        [MaxLength(30)]
        [Column(TypeName = "varchar(30)")]
        public string ReferralCode { get; set; } = string.Empty;

        /// <summary>
        /// PENDING (Đã nhập mã, chờ hoàn tất đơn đầu tiên), REWARDED (Đã cộng điểm & tặng voucher cho cả 2)
        /// </summary>
        [Required]
        [MaxLength(30)]
        [Column(TypeName = "varchar(30)")]
        public string Status { get; set; } = "PENDING";

        public int ReferrerPointsAwarded { get; set; } = 0;

        public int ReferredPointsAwarded { get; set; } = 0;

        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string? RewardVoucherCode { get; set; }

        public int? CompletedOrderId { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? RewardedAt { get; set; }

        // Navigations
        public virtual User ReferrerUser { get; set; } = null!;
        public virtual User ReferredUser { get; set; } = null!;
        public virtual Order? CompletedOrder { get; set; }
    }
}
