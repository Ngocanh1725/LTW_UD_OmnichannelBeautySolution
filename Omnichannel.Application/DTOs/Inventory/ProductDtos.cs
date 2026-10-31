using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Omnichannel.Application.DTOs.Inventory
{
    public class ProductItemViewModel
    {
        public int Id { get; set; }
        public string ProductId { get; set; } = string.Empty; // Sku alias for backward compatibility
        public string Sku { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? SupplierName { get; set; }
        public string? ThumbnailUrl { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal? DiscountPrice { get; set; }
        public decimal CostPrice { get; set; }
        public int TotalStock { get; set; }
        public int SafetyStock { get; set; }
        public string Unit { get; set; } = "Hộp";
        public int Status { get; set; }
        public bool IsActive => Status == 1;
        public bool HasVariants { get; set; }
        public int VariantCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ProductFilterRequest
    {
        public string? Keyword { get; set; }
        public int? CategoryId { get; set; }
        public string? StockStatus { get; set; } = "all"; // all, in_stock, out_of_stock, low_stock
        public int? Status { get; set; } // null = all, 1 = active, 0 = inactive
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class ProductListPagedViewModel
    {
        public List<ProductItemViewModel> Items { get; set; } = new();
        public ProductFilterRequest Filter { get; set; } = new();
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalActive { get; set; }
        public int TotalInactive { get; set; }
        public int TotalOutOfStock { get; set; }
        public int TotalInStock { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class ProductFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Mã SKU sản phẩm không được để trống")]
        [StringLength(50, ErrorMessage = "Mã SKU tối đa 50 ký tự")]
        [Display(Name = "Mã SKU")]
        public string Sku { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã vạch Barcode không được để trống")]
        [StringLength(50, ErrorMessage = "Mã Barcode tối đa 50 ký tự")]
        [Display(Name = "Mã Barcode EAN-13")]
        public string Barcode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên mỹ phẩm không được để trống")]
        [StringLength(200, ErrorMessage = "Tên mỹ phẩm tối đa 200 ký tự")]
        [Display(Name = "Tên sản phẩm")]
        public string ProductName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Đường dẫn Slug không được để trống")]
        [StringLength(250, ErrorMessage = "Slug tối đa 250 ký tự")]
        [Display(Name = "Đường dẫn SEO (Slug)")]
        public string Slug { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Display(Name = "Danh mục ngành hàng")]
        public int CategoryId { get; set; }

        [Display(Name = "Thương hiệu / Nhà phân phối")]
        public int? SupplierId { get; set; }

        [Display(Name = "Mô tả ngắn (SEO Meta Description)")]
        [StringLength(500, ErrorMessage = "Mô tả ngắn tối đa 500 ký tự")]
        public string? ShortDescription { get; set; }

        [Display(Name = "Mô tả chi tiết & Hướng dẫn sử dụng")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Giá vốn không được để trống")]
        [Range(0, 1000000000, ErrorMessage = "Giá vốn phải từ 0 trở lên")]
        [Display(Name = "Giá vốn nhập kho (VNĐ)")]
        public decimal CostPrice { get; set; }

        [Required(ErrorMessage = "Giá bán niêm yết không được để trống")]
        [Range(0, 1000000000, ErrorMessage = "Giá bán phải từ 0 trở lên")]
        [Display(Name = "Giá bán lẻ niêm yết (VNĐ)")]
        public decimal SellingPrice { get; set; }

        [Range(0, 1000000000, ErrorMessage = "Giá khuyến mãi phải từ 0 trở lên")]
        [Display(Name = "Giá khuyến mãi (VNĐ - tùy chọn)")]
        public decimal? DiscountPrice { get; set; }

        [Display(Name = "Đường dẫn ảnh Thumbnail")]
        public string? ThumbnailUrl { get; set; }

        public int? ThumbnailMediaId { get; set; }

        [Required(ErrorMessage = "Đơn vị tính không được để trống")]
        [StringLength(50, ErrorMessage = "Đơn vị tính tối đa 50 ký tự")]
        [Display(Name = "Đơn vị tính")]
        public string Unit { get; set; } = "Chai";

        [Display(Name = "Đang kinh doanh")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Sản phẩm nổi bật")]
        public bool IsFeatured { get; set; } = false;

        [Display(Name = "Phân loại sản phẩm")]
        public string ProductType { get; set; } = "STANDARD";

        [Display(Name = "Số công bố BYT")]
        [StringLength(100)]
        public string? CosmeticNotificationNumber { get; set; }

        [Display(Name = "Hãng sản xuất / Brand")]
        [StringLength(200)]
        public string? ManufacturerName { get; set; }

        [Display(Name = "Xuất xứ")]
        [StringLength(100)]
        public string? CountryOfOrigin { get; set; }

        [Display(Name = "Đơn vị nhập khẩu")]
        [StringLength(200)]
        public string? ImporterName { get; set; }

        // Cosmetic Attributes
        [Display(Name = "Dung tích / Trọng lượng")]
        public string? Volume { get; set; }

        [Display(Name = "Kết cấu / Dạng bào chế")]
        public string? Formulation { get; set; }

        [Display(Name = "Loại da phù hợp")]
        public List<int> SelectedSkinTypeIds { get; set; } = new();

        [Display(Name = "Thành phần hoạt tính")]
        public List<int> SelectedIngredientIds { get; set; } = new();
    }

    // Backward-compatibility subclass
    public class CreateProductViewModel : ProductFormViewModel
    {
        public string ProductId
        {
            get => Sku;
            set => Sku = value;
        }

        public string SupplierIdStr
        {
            get => SupplierId?.ToString() ?? string.Empty;
            set => SupplierId = int.TryParse(value, out int id) ? id : null;
        }

        public int Status
        {
            get => IsActive ? 1 : 0;
            set => IsActive = value == 1;
        }

        public int SafetyStock { get; set; } = 10;
    }
}