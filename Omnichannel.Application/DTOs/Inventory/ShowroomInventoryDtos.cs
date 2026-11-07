using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Inventory
{
    /// <summary>
    /// DTO đại diện tình trạng tồn kho sản phẩm tại một Showroom/Chi nhánh vật lý (O2O)
    /// </summary>
    public class ShowroomStockDto
    {
        public int StoreId { get; set; }
        public string StoreCode { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Phone
        {
            get => PhoneNumber ?? string.Empty;
            set => PhoneNumber = value;
        }
        public int StockQuantity { get; set; }
        public bool HasStock => StockQuantity > 0;
        public string StatusText => StockQuantity > 5 
            ? "Còn hàng - Sẵn sàng lấy ngay" 
            : (StockQuantity > 0 ? "Chỉ còn vài sản phẩm" : "Tạm hết hàng");
        public string DeliveryBadge { get; set; } = "Nhận tại quầy trong 30 phút";
        public bool HasTester { get; set; } = true;
    }

    /// <summary>
    /// ViewModel hỗ trợ bộ lọc Showroom theo Tỉnh/Thành phố trên PDP
    /// </summary>
    public class ShowroomAvailabilityViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? SelectedCity { get; set; }
        public List<string> AvailableCities { get; set; } = new();
        public List<ShowroomStockDto> Stores { get; set; } = new();
    }

    /// <summary>
    /// DTO chiến dịch Flash Sale đếm ngược thời gian thực
    /// </summary>
    public class FlashSaleCampaignDto
    {
        public int Id { get; set; }
        public string CampaignCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsActive { get; set; }
        public decimal DiscountPercent { get; set; }
        public List<FlashSaleProductItemDto> Products { get; set; } = new();
    }

    /// <summary>
    /// Sản phẩm trong phiên Flash Sale kèm số lượng đã bán & thanh tiến độ
    /// </summary>
    public class FlashSaleProductItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string? Slug { get; set; }
        public string? ImageUrl { get; set; }
        public string? BrandName { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal SalePrice { get; set; }
        public int DiscountPercent => OriginalPrice > 0 && SalePrice < OriginalPrice 
            ? (int)Math.Round((OriginalPrice - SalePrice) / OriginalPrice * 100) 
            : 0;
        public int StockLimit { get; set; } = 100;
        public int SoldCount { get; set; } = 45;
        public int SoldPercentage => StockLimit > 0 
            ? Math.Min(100, (int)Math.Round((double)SoldCount / StockLimit * 100)) 
            : 0;
        public bool IsAlmostSoldOut => SoldPercentage >= 80;
    }
}

