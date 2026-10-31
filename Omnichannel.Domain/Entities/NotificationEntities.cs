using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Omnichannel.Domain.Entities
{
    [Table("Notifications")]
    public class Notification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [MaxLength(50)]
        public string? Role { get; set; } // SuperAdmin, StoreManager, Cashier, Customer...

        public int? StoreId { get; set; }

        [ForeignKey("StoreId")]
        public virtual Store? Store { get; set; }

        /// <summary>
        /// Loại thông báo: ORDER, WAREHOUSE, SYSTEM, ALERT
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string Type { get; set; } = "SYSTEM";

        /// <summary>
        /// Mức độ: INFO, SUCCESS, WARNING, DANGER
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

        [MaxLength(500)]
        [Column(TypeName = "varchar(500)")]
        public string? Url { get; set; }

        public bool IsRead { get; set; } = false;

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? ReadAt { get; set; }
    }
}
