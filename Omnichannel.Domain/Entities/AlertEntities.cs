using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Omnichannel.Domain.Entities
{
    [Table("Alerts")]
    public class Alert
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Mã loại cảnh báo: OUT_OF_STOCK, LOW_STOCK, EXPIRY_CRITICAL, EXPIRY_WARNING, REVENUE_ANOMALY, PENDING_ORDERS_DELAY
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Mức độ nghiêm trọng: DANGER (🔴), WARNING (🟡), SUCCESS (🟢), INFO (🔵)
        /// </summary>
        [Required]
        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string Severity { get; set; } = "INFO";

        [Required]
        [MaxLength(250)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? ReadAt { get; set; }

        public int? UserId { get; set; } // Null: Toàn hệ thống / Quản trị viên, hoặc user cụ thể

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        public int? StoreId { get; set; } // Chi nhánh phát sinh cảnh báo (nếu có)

        [ForeignKey("StoreId")]
        public virtual Store? Store { get; set; }

        [MaxLength(500)]
        [Column(TypeName = "varchar(500)")]
        public string? ActionUrl { get; set; }

        [MaxLength(150)]
        [Column(TypeName = "varchar(150)")]
        public string? EntityKey { get; set; } // Khóa đối tượng để chống trùng lặp cảnh báo trong chu kỳ kiểm tra
    }
}
