using System;
using System.ComponentModel.DataAnnotations;

namespace Omnichannel.Application.DTOs.Cms
{
    /// <summary>
    /// Cấu hình tùy biến hiển thị nâng cao cho từng Banner (ẩn/hiện từng phần, kiểu khung, kích thước khung, font chữ, màu chữ, nút phụ...)
    /// Lưu trữ linh hoạt trong bảng Settings dưới dạng JSON theo khóa BANNER_STYLE_CONFIG_{BannerId}.
    /// </summary>
    public class BannerStyleConfigDto
    {
        // 1. Ẩn / Hiển thị từng thành phần (Visibility Toggles)
        public bool ShowContentBox { get; set; } = true;
        public bool ShowOverlayGradient { get; set; } = true;
        public bool ShowBadge { get; set; } = true;
        public bool ShowTitle { get; set; } = true;
        public bool ShowSubtitle { get; set; } = true;
        public bool ShowPrimaryButton { get; set; } = true;
        public bool ShowSecondaryButton { get; set; } = true;

        // 2. Nội dung bổ sung (Badge & Nút phụ thứ 2)
        [StringLength(80)]
        public string BadgeText { get; set; } = "Bộ Sưu Tập Mới 2026";

        [StringLength(60)]
        public string SecondaryButtonText { get; set; } = "Săn Deal Cận Date -50%";

        [StringLength(255)]
        public string SecondaryButtonUrl { get; set; } = "/clearance-fefo";

        // 3. Tùy chọn Khung nội dung (Kiểu khung, Kích thước khung, Vị trí, Bo góc, Lớp phủ nền)
        /// <summary>
        /// transparent | glass-dark | glass-light | rose-luxury | solid-dark | double-border
        /// </summary>
        public string BoxStyle { get; set; } = "transparent";

        /// <summary>
        /// sm (448px) | md (576px) | lg (672px) | xl (896px)
        /// </summary>
        public string BoxWidth { get; set; } = "md";

        /// <summary>
        /// left | center | right
        /// </summary>
        public string BoxPosition { get; set; } = "left";

        /// <summary>
        /// none | md | xl | 3xl
        /// </summary>
        public string BoxRadius { get; set; } = "3xl";

        /// <summary>
        /// 0 | 25 | 45 | 65 | 85
        /// </summary>
        public int OverlayOpacity { get; set; } = 65;

        // 4. Tùy chọn Font chữ, Cỡ chữ, Màu chữ & Kiểu nút
        /// <summary>
        /// serif (Playfair Display) | sans (Inter/Quicksand) | editorial (Georgia Italic) | mono (Space Mono)
        /// </summary>
        public string TitleFont { get; set; } = "serif";

        /// <summary>
        /// sm | md | lg | xl
        /// </summary>
        public string TitleSize { get; set; } = "lg";

        /// <summary>
        /// white | cream | gold | dark
        /// </summary>
        public string TextColorTheme { get; set; } = "white";

        /// <summary>
        /// rose-gradient | white-solid | dark-luxury | glass-outline
        /// </summary>
        public string ButtonStyle { get; set; } = "rose-gradient";

        // 5. Tùy chọn Khung ngoài Banner, Cắt ảnh (ObjectFit), Điểm lấy nét (Focal Point) & Zoom ảnh
        /// <summary>
        /// auto | sm (300px) | md (380px) | lg (500px) | xl (620px)
        /// </summary>
        public string BannerHeight { get; set; } = "auto";

        /// <summary>
        /// none (0px) | xl (16px) | 3xl (24px)
        /// </summary>
        public string ContainerRadius { get; set; } = "3xl";

        /// <summary>
        /// cover (Cắt lấp đầy khung) | contain (Hiển thị trọn vẹn nguyên ảnh) | fill (Co giãn vừa khít khung)
        /// </summary>
        public string ImageObjectFit { get; set; } = "cover";

        /// <summary>
        /// center | top | bottom | left | right
        /// </summary>
        public string ImagePosition { get; set; } = "center";

        /// <summary>
        /// 100 | 105 | 110 | 115 | 125 (%)
        /// </summary>
        public int ImageScale { get; set; } = 100;
    }

    public class BannerItemViewModel
    {
        public int BannerId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string? ButtonText { get; set; }
        public int Position { get; set; }
        public string PositionName => Position switch
        {
            1 => "Hero Slider (Đầu trang chủ)",
            2 => "Popup Khuyến mãi (Giữa màn hình)",
            3 => "Top Banner Danh mục",
            4 => "Footer Strip (Chân trang)",
            5 => "Mid Banner (Giữa trang chủ)",
            _ => "Khác"
        };
        public string DesktopImageUrl { get; set; } = string.Empty;
        public string? MobileImageUrl { get; set; }
        public string? LinkUrl { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public int TrackingClickCount { get; set; }
        public bool IsActive { get; set; }
        public bool IsExpired => DateTime.UtcNow > ValidTo;
        public bool IsUpcoming => DateTime.UtcNow < ValidFrom;
        public BannerStyleConfigDto StyleConfig { get; set; } = new();
    }

    public class CreateBannerViewModel
    {
        [StringLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự")]
        [Display(Name = "Tiêu đề chiến dịch")]
        public string Title { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Mô tả phụ tối đa 255 ký tự")]
        [Display(Name = "Mô tả phụ (Subtitle)")]
        public string? Subtitle { get; set; }

        [StringLength(50, ErrorMessage = "Chữ nút bấm tối đa 50 ký tự")]
        [Display(Name = "Chữ nút bấm (Button Text)")]
        public string? ButtonText { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vị trí hiển thị")]
        [Display(Name = "Vị trí banner")]
        public int Position { get; set; } = 1;

        [Display(Name = "URL ảnh Desktop")]
        public string? DesktopImageUrl { get; set; }

        public int? DesktopMediaId { get; set; }

        [Display(Name = "URL ảnh Mobile (Tùy chọn)")]
        public string? MobileImageUrl { get; set; }

        public int? MobileMediaId { get; set; }

        [Display(Name = "Liên kết khi nhấp (Link URL)")]
        public string? LinkUrl { get; set; }

        [Display(Name = "Thứ tự sắp xếp")]
        public int DisplayOrder { get; set; } = 0;

        [Required(ErrorMessage = "Ngày bắt đầu không được để trống")]
        [Display(Name = "Hiệu lực từ ngày")]
        public DateTime ValidFrom { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "Ngày kết thúc không được để trống")]
        [Display(Name = "Hiệu lực đến ngày")]
        public DateTime ValidTo { get; set; } = DateTime.UtcNow.AddMonths(1);

        public BannerStyleConfigDto StyleConfig { get; set; } = new();
    }

    public class EditBannerViewModel : CreateBannerViewModel
    {
        public int BannerId { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
