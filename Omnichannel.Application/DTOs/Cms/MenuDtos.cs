using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Omnichannel.Application.DTOs.Cms
{
    public class MenuItemDto
    {
        public int MenuItemId { get; set; }
        public int MenuId { get; set; }
        public int? ParentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int TargetType { get; set; } // 1: Category, 2: Product, 3: Post, 4: StaticPage, 5: CustomUrl
        public string? TargetId { get; set; }
        public string? TargetSlug { get; set; }
        public string? CustomUrl { get; set; }
        public string ResolvedUrl { get; set; } = "#";
        public int DisplayOrder { get; set; }
        public bool OpenInNewTab { get; set; }
        public string? IconClass { get; set; }
        public string? CssClass { get; set; }
        public bool IsActive { get; set; }
        public List<MenuItemDto> Children { get; set; } = new();
    }

    public class CreateMenuItemViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn khung Menu")]
        public int MenuId { get; set; }

        public int? ParentId { get; set; }

        [Required(ErrorMessage = "Tiêu đề nút menu không được để trống")]
        [StringLength(150, ErrorMessage = "Tiêu đề tối đa 150 ký tự")]
        [Display(Name = "Tiêu đề hiển thị")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn loại liên kết")]
        [Display(Name = "Loại đối tượng liên kết")]
        public int TargetType { get; set; } = 1; // Mặc định: Category

        [Display(Name = "Đối tượng đích")]
        public string? TargetId { get; set; }

        [Display(Name = "Đường dẫn Slug")]
        public string? TargetSlug { get; set; }

        [Display(Name = "Đường dẫn tùy chọn (URL ngoài)")]
        [StringLength(500, ErrorMessage = "Đường dẫn tối đa 500 ký tự")]
        public string? CustomUrl { get; set; }

        [Display(Name = "Thứ tự sắp xếp")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Mở trong tab mới")]
        public bool OpenInNewTab { get; set; } = false;

        [Display(Name = "Icon class (FontAwesome)")]
        public string? IconClass { get; set; }

        [Display(Name = "CSS Class tùy biến")]
        public string? CssClass { get; set; }
    }

    public class MenuManagementViewModel
    {
        public int SelectedMenuId { get; set; }
        public string SelectedMenuCode { get; set; } = string.Empty;
        public string SelectedMenuName { get; set; } = string.Empty;
        public List<MenuLookupDto> AvailableMenus { get; set; } = new();
        public List<MenuItemDto> MenuTree { get; set; } = new();
        public CreateMenuItemViewModel NewItem { get; set; } = new();
    }

    public class MenuLookupDto
    {
        public int MenuId { get; set; }
        public string MenuCode { get; set; } = string.Empty;
        public string MenuName { get; set; } = string.Empty;
        public int Position { get; set; }
    }

    public class TargetLookupItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
    }
}
