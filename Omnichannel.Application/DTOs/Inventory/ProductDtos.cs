using System;
using System.ComponentModel.DataAnnotations;

namespace Omnichannel.Application.DTOs.Inventory
{
    public class ProductItemViewModel
    {
        public string ProductId { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public decimal SellingPrice { get; set; }
        public decimal CostPrice { get; set; }
        public int SafetyStock { get; set; }
        public string Unit { get; set; } = "Hộp";
        public int Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateProductViewModel
    {
        [Required(ErrorMessage = "Mã SKU sản phẩm không được để trống")]
        [StringLength(50, ErrorMessage = "Mã SKU tối đa 50 ký tự")]
        [Display(Name = "Mã SKU (Product ID)")]
        public string ProductId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Display(Name = "Danh mục sản phẩm")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn nhà cung cấp")]
        [Display(Name = "Nhà cung cấp")]
        public string SupplierId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã vạch Barcode không được để trống")]
        [StringLength(50, ErrorMessage = "Mã Barcode tối đa 50 ký tự")]
        [Display(Name = "Mã vạch (Barcode EAN-13)")]
        public string Barcode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        [StringLength(255, ErrorMessage = "Tên sản phẩm tối đa 255 ký tự")]
        [Display(Name = "Tên sản phẩm")]
        public string ProductName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Đường dẫn Slug không được để trống")]
        [StringLength(255, ErrorMessage = "Slug tối đa 255 ký tự")]
        [Display(Name = "URL Slug")]
        public string Slug { get; set; } = string.Empty;

        [Required(ErrorMessage = "Giá bán niêm yết không được để trống")]
        [Range(0, 1000000000, ErrorMessage = "Giá bán phải lớn hơn hoặc bằng 0")]
        [Display(Name = "Giá bán lẻ niêm yết (VNĐ)")]
        public decimal SellingPrice { get; set; }

        [Required(ErrorMessage = "Giá vốn không được để trống")]
        [Range(0, 1000000000, ErrorMessage = "Giá vốn phải lớn hơn hoặc bằng 0")]
        [Display(Name = "Giá vốn nhập kho (VNĐ)")]
        public decimal CostPrice { get; set; }

        [Required(ErrorMessage = "Ngưỡng tồn an toàn không được để trống")]
        [Range(0, 100000, ErrorMessage = "Ngưỡng tồn an toàn từ 0 trở lên")]
        [Display(Name = "Ngưỡng tồn an toàn (Safety Stock)")]
        public int SafetyStock { get; set; } = 10;

        [Required(ErrorMessage = "Đơn vị tính không được để trống")]
        [StringLength(50, ErrorMessage = "Đơn vị tính tối đa 50 ký tự")]
        [Display(Name = "Đơn vị tính")]
        public string Unit { get; set; } = "Hộp";

        [Display(Name = "Trạng thái kinh doanh")]
        public int Status { get; set; } = 1;
    }
}