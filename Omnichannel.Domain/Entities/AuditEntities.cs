using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Omnichannel.Domain.Entities
{
    [Table("AuditLogs")]
    public class AuditLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? UserId { get; set; }

        [MaxLength(100)]
        public string? UserName { get; set; }

        [MaxLength(50)]
        [Column(TypeName = "nvarchar(50)")]
        public string? StaffCode { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string Action { get; set; } = string.Empty; // CREATE, UPDATE, DELETE, LOGIN, PERMISSION_CHANGE

        [Required]
        [MaxLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string EntityType { get; set; } = string.Empty; // Product, Order, Permission...

        [MaxLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string? EntityName { get; set; }

        [Required]
        [MaxLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string EntityId { get; set; } = string.Empty;

        [MaxLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string? RecordId { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? OldValues { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? OldValuesJson { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? NewValues { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? NewValuesJson { get; set; }

        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        [Column(TypeName = "varchar(500)")]
        public string? UserAgent { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
