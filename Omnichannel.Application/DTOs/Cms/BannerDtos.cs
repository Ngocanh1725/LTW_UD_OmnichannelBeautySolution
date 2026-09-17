using System;
using System.ComponentModel.DataAnnotations;

namespace Omnichannel.Application.DTOs.Cms
{
    public class BannerItemViewModel
    {
        public int BannerId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Position { get; set; }
        public string PositionName => Position switch
        {
            1 => "Hero Slider (Đầu trang chủ)",
            2 => "Popup Khuyến mãi (Giữa màn hình)",
            3 => "Top Banner Danh mục",
            4 => "Footer Strip (Chân trang)",
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
    }

    public class CreateBannerViewModel
    {
        [Required(ErrorMessage = "Tiêu đề banner không được để trống")]
        [StringLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự")]
        [Display(Name = "Tiêu đề chiến dịch")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn vị trí hiển thị")]
        [Display(Name = "Vị trí banner")]
        public int Position { get; set; } = 1;

        [Required(ErrorMessage = "Ảnh máy tính (Desktop) không được để trống")]
        [Display(Name = "URL ảnh Desktop")]
        public string DesktopImageUrl { get; set; } = string.Empty;

        [Display(Name = "URL ảnh Mobile (Tùy chọn)")]
        public string? MobileImageUrl { get; set; }

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
    }
}
