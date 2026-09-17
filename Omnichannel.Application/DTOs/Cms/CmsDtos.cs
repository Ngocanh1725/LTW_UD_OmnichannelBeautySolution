using System;
using System.ComponentModel.DataAnnotations;

namespace Omnichannel.Application.DTOs.Cms
{
    public class PostItemViewModel
    {
        public int PostId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public int Status { get; set; } // 1: Bản nháp, 2: Chờ duyệt, 3: Đã xuất bản, 4: Ẩn
        public string StatusName => Status switch
        {
            1 => "Bản nháp",
            2 => "Chờ duyệt",
            3 => "Đã xuất bản",
            4 => "Đã ẩn",
            _ => "Không xác định"
        };
        public int ViewCount { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreatePostViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn chuyên mục bài viết")]
        [Display(Name = "Chuyên mục")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Tiêu đề bài viết không được để trống")]
        [StringLength(255, ErrorMessage = "Tiêu đề tối đa 255 ký tự")]
        [Display(Name = "Tiêu đề bài viết")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Đường dẫn thân thiện (Slug) không được để trống")]
        [Display(Name = "URL Slug")]
        public string Slug { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Tóm tắt tối đa 500 ký tự")]
        [Display(Name = "Tóm tắt ngắn")]
        public string? Summary { get; set; }

        [Required(ErrorMessage = "Nội dung bài viết không được để trống")]
        [Display(Name = "Nội dung bài viết (HTML)")]
        public string ContentHtml { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ảnh bìa bài viết không được để trống")]
        [Display(Name = "Đường dẫn ảnh đại diện (Thumbnail URL)")]
        public string ThumbnailUrl { get; set; } = string.Empty;

        [Display(Name = "Trạng thái")]
        public int Status { get; set; } = 3; // 3: Xuất bản ngay

        [Display(Name = "Thẻ Tiêu đề SEO (Meta Title)")]
        public string? MetaTitle { get; set; }

        [Display(Name = "Từ khóa SEO (Meta Keywords)")]
        public string? MetaKeywords { get; set; }

        [Display(Name = "Mô tả SEO (Meta Description)")]
        public string? MetaDescription { get; set; }
    }
}
