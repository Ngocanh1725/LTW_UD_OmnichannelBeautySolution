using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Cms
{
    /// <summary>
    /// Cấu hình tùy biến chi tiết (hình ảnh, kích thước, số lượng, điều hướng) cho khối giao diện UI Section
    /// </summary>
    public class UiSectionConfigDto
    {
        public string? ImageUrl { get; set; }
        public int? MediaAssetId { get; set; }
        public string? Height { get; set; } = "md"; // "sm", "md", "lg", "xl", "auto"
        public string? ImageFit { get; set; } = "cover"; // "cover", "contain", "fill"
        public string? ImagePosition { get; set; } = "center"; // "center", "top", "bottom", "left", "right"
        public int? ItemCount { get; set; } // Số lượng item hiển thị (ví dụ 3, 4, 6, 8, 12)
        public string? LinkUrl { get; set; }
        public string? ButtonText { get; set; }
        public string? BadgeText { get; set; }
        public string? BackgroundColor { get; set; }
        public string? ContainerRadius { get; set; } = "2xl"; // "none", "xl", "2xl", "3xl"
        public bool ShowBadge { get; set; } = true;
    }

    /// <summary>
    /// DTO trả về chi tiết khối giao diện để chỉnh sửa
    /// </summary>
    public class UiSectionDetailDto
    {
        public int Id { get; set; }
        public string SectionCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsVisible { get; set; }
        public string LayoutType { get; set; } = "GRID_4";
        public UiSectionConfigDto Config { get; set; } = new();
    }

    /// <summary>
    /// Model nhận dữ liệu lưu/cập nhật khối giao diện từ Admin Form
    /// </summary>
    public class UiSectionSaveRequest
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string LayoutType { get; set; } = "GRID_4";
        public bool IsVisible { get; set; } = true;
        public int DisplayOrder { get; set; }

        // Cấu hình hình ảnh & kích thước
        public string? ImageUrl { get; set; }
        public int? MediaAssetId { get; set; }
        public string? Height { get; set; } = "md";
        public string? ImageFit { get; set; } = "cover";
        public string? ImagePosition { get; set; } = "center";
        public int? ItemCount { get; set; }
        public string? LinkUrl { get; set; }
        public string? ButtonText { get; set; }
        public string? BadgeText { get; set; }
        public string? BackgroundColor { get; set; }
    }
}

