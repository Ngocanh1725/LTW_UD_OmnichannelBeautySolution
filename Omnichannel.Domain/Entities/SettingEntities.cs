using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Omnichannel.Domain.Entities
{
    [Table("Settings")]
    public class Setting
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string SettingKey { get; set; } = string.Empty;

        [Required]
        public string SettingValue { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string SettingGroup { get; set; } = "GENERAL"; // GENERAL, SMTP, PAYMENT, SHIPPING

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string DataType { get; set; } = "STRING"; // STRING, INT, BOOL, JSON

        public bool IsEncrypted { get; set; } = false;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? UpdatedAt { get; set; }

        public int? UpdatedBy { get; set; }

        // Navigation
        public virtual User? UpdatedByUser { get; set; }
    }
}
