using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Omnichannel.Domain.Entities
{
    [Table("MediaAssets")]
    public partial class MediaAsset
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string ContentType { get; set; } = string.Empty; // MIME type: image/jpeg, image/png, image/webp, image/svg+xml

        [Required]
        [Column(TypeName = "varbinary(max)")]
        public byte[] Data { get; set; } = Array.Empty<byte>(); // Dữ liệu nhị phân lưu trực tiếp trong SQL Server

        public long FileSize { get; set; } // Dung lượng tính bằng Byte

        public int? Width { get; set; }
        public int? Height { get; set; }

        [MaxLength(255)]
        public string? AltText { get; set; } // SEO Alt Text

        [Required]
        [MaxLength(64)]
        [Column(TypeName = "varchar(64)")]
        public string ChecksumHash { get; set; } = string.Empty; // Mã SHA256 chống lưu trùng ảnh và làm ETag HTTP

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string Category { get; set; } = "PRODUCT"; // PRODUCT, BANNER, BRAND, BLOG, THEME, AVATAR, SYSTEM

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedBy { get; set; }
        public virtual User? CreatedByUser { get; set; }
    }
}
