using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Omnichannel.Domain.Entities
{
    [Table("ThemeConfigs")]
    public partial class ThemeConfig
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? StoreId { get; set; } // Null = Cấu hình dùng chung toàn hệ thống

        [Required]
        [MaxLength(100)]
        public string BrandName { get; set; } = "OMNI BEAUTY";

        [MaxLength(255)]
        public string? Slogan { get; set; } = "Hệ Thống Bán Lẻ Mỹ Phẩm Cao Cấp Tiên Phong Công Nghệ FEFO";

        [Required]
        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string PrimaryColor { get; set; } = "#E11D48"; // Mã HEX màu chính (Rose-600)

        [Required]
        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string SecondaryColor { get; set; } = "#BE185D"; // Mã HEX màu phụ (Pink-700)

        [Required]
        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string AccentColor { get; set; } = "#F59E0B"; // Mã HEX điểm nhấn (Amber-500)

        [Required]
        [MaxLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string FontFamily { get; set; } = "'Inter', sans-serif";

        public int? LogoMediaId { get; set; }
        public int? FaviconMediaId { get; set; }

        [MaxLength(500)]
        public string? AnnouncementBarText { get; set; } = "✨ Chào mừng bạn đến với OMNI BEAUTY - Miễn phí vận chuyển cho đơn hàng từ 300K ✨";

        public bool AnnouncementBarActive { get; set; } = true;

        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string AnnouncementBarBgColor { get; set; } = "#881337"; // Rose-900

        [MaxLength(50)]
        public string? Hotline { get; set; } = "1900 8888";

        [MaxLength(100)]
        public string? ContactEmail { get; set; } = "hotro@omnibeauty.vn";

        [MaxLength(255)]
        public string? Address { get; set; } = "68 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh";

        [MaxLength(500)]
        public string? FooterCopyright { get; set; } = "© 2026 OMNI BEAUTY. Tất cả quyền được bảo lưu.";

        public string? CustomCss { get; set; } // Mã CSS tùy biến lưu trong Database

        public bool IsActive { get; set; } = true;

        [Column(TypeName = "datetime2(7)")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual MediaAsset? LogoMedia { get; set; }
        public virtual MediaAsset? FaviconMedia { get; set; }
    }

    [Table("UiSections")]
    public class UiSection
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string SectionCode { get; set; } = string.Empty; // HERO_SLIDER, FLASH_SALE, FEATURED_CATEGORIES, BEST_SELLERS, MID_BANNER, BEAUTY_BLOG

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Subtitle { get; set; }

        public int DisplayOrder { get; set; } = 0; // Thứ tự xuất hiện trên trang chủ

        public bool IsVisible { get; set; } = true; // Bật / Tắt hiển thị khối

        public int? BackgroundMediaId { get; set; }

        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string LayoutType { get; set; } = "GRID_4"; // GRID_4, GRID_6, CAROUSEL, BANNER_FULL

        public string? ConfigurationJson { get; set; } // Lưu cấu hình riêng (item count, filter tags, auto-scroll interval)

        public virtual MediaAsset? BackgroundMedia { get; set; }
    }
}
