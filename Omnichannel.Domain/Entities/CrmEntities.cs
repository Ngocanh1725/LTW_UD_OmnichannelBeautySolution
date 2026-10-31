using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Omnichannel.Domain.Entities
{
    [Table("CustomerProfiles")]
    public class CustomerProfile
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int UserId { get; set; } // Quan hệ 1-1 với Users

        [Column(TypeName = "datetime2(7)")]
        public DateTime? DateOfBirth { get; set; }

        [MaxLength(10)]
        [Column(TypeName = "varchar(10)")]
        public string? Gender { get; set; } // Male, Female, Other

        [MaxLength(50)]
        public string? CurrentSkinType { get; set; } // Da dầu, Da khô, Da nhạy cảm, Da hỗn hợp...

        [MaxLength(500)]
        public string? SkinConcerns { get; set; } // JSON array: ["Mụn", "Nám", "Lão hóa", "Thâm"]

        [MaxLength(500)]
        public string? KnownAllergies { get; set; } // JSON array: thành phần dị ứng ["Paraben", "Fragrance"]

        [MaxLength(500)]
        public string? PreferredBrands { get; set; } // JSON array: thương hiệu yêu thích

        [MaxLength(1000)]
        public string? Note { get; set; } // Ghi chú của chuyên viên tư vấn / Beauty Advisor

        [Column(TypeName = "datetime2(7)")]
        public DateTime? LastConsultationDate { get; set; }

        public int? ConsultantUserId { get; set; } // Chuyên viên tư vấn phụ trách

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public virtual User User { get; set; } = null!;
        public virtual User? Consultant { get; set; }
        public virtual ICollection<SkinHistory> SkinHistories { get; set; } = new List<SkinHistory>();
        public virtual ICollection<RepurchaseSchedule> RepurchaseSchedules { get; set; } = new List<RepurchaseSchedule>();
    }

    [Table("SkinHistories")]
    public class SkinHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int CustomerProfileId { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(50)]
        public string SkinType { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? SkinCondition { get; set; } // "Mụn viêm vùng trán", "Da khô bong tróc"

        [MaxLength(1000)]
        public string? TreatmentNote { get; set; }

        public int? RecordedBy { get; set; } // Beauty Advisor ghi nhận

        public string? ImageUrls { get; set; } // JSON array ảnh da

        // Navigation
        public virtual CustomerProfile CustomerProfile { get; set; } = null!;
        public virtual User? RecordedByUser { get; set; }
    }

    [Table("RepurchaseSchedules")]
    public class RepurchaseSchedule
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int CustomerProfileId { get; set; }
        public int ProductId { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime LastPurchaseDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(10,2)")]
        public decimal EstimatedVolumeMl { get; set; } // Dung tích sản phẩm (ml hoặc g)

        [Column(TypeName = "decimal(10,2)")]
        public decimal DailyUsageMl { get; set; } // Liều dùng trung bình hằng ngày (ml hoặc g)

        [Column(TypeName = "datetime2(7)")]
        public DateTime EstimatedDepletionDate { get; set; } // Ngày dự kiến hết = LastPurchaseDate + (Volume / DailyUsage) ngày

        public bool IsNotified { get; set; } = false; // Đã gửi thông báo nhắc mua lại chưa

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual CustomerProfile CustomerProfile { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}
