using System;
using System.Collections.Generic;
using Omnichannel.Application.DTOs.Cms;
using Omnichannel.Domain.Entities;

namespace Omnichannel.Web.Models
{
    public class ProductDetailViewModel
    {
        public Product Product { get; set; } = null!;
        public ProductBatch? NearestBatch { get; set; }
        public int DaysLeftToExpiry { get; set; }
        public bool IsNearExpiration => DaysLeftToExpiry <= 90;
        public List<string> GalleryImages { get; set; } = new();
        public List<Product> RelatedProducts { get; set; } = new();
        public List<ProductReviewDto> Reviews { get; set; } = new();
        public ReviewStatsDto ReviewStats { get; set; } = new();
        public double AverageRating { get; set; } = 5.0;
        public int TotalReviews { get; set; } = 0;
        public List<string> SuitableSkinTypes { get; set; } = new();
        public List<string> KeyIngredients { get; set; } = new();
        public decimal DiscountPercentage => Product.SellingPrice > 0 && Product.DiscountPrice.HasValue && Product.DiscountPrice.Value < Product.SellingPrice
            ? Math.Round((Product.SellingPrice - Product.DiscountPrice.Value) / Product.SellingPrice * 100)
            : 0;

        // --- PDP SENIOR ENHANCEMENTS ---
        // 1. FEFO Dual Batch Smart Selector
        public ProductBatch? FreshBatch { get; set; }
        public ProductBatch? ClearanceBatch { get; set; }
        public decimal ClearancePrice { get; set; }
        public int ClearanceDiscountPercent { get; set; } = 45;

        // 2. Swatch Video Support
        public string? SwatchVideoUrl { get; set; }

        // 3. Showroom Stock Availability List
        public List<ShowroomStockDto> ShowroomStocks { get; set; } = new();

        // 4. Detailed Ingredients Breakdown
        public List<IngredientDetailDto> IngredientBreakdowns { get; set; } = new();
    }

    public class ShowroomStockDto
    {
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public bool HasTester { get; set; } = true;
        public string StatusText => StockQuantity > 5 ? "Còn hàng" : (StockQuantity > 0 ? "Chỉ còn vài sản phẩm" : "Tạm hết hàng");
        public string DeliveryBadge { get; set; } = "Nhận tại quầy trong 30 phút";
    }

    public class IngredientDetailDto
    {
        public string Name { get; set; } = string.Empty;
        public string Percentage { get; set; } = string.Empty;
        public string Function { get; set; } = string.Empty;
        public int EwgScore { get; set; } = 1;
        public string EwgRating { get; set; } = "Tuyệt đối an toàn";
        public string Source { get; set; } = "Dược phẩm chính ngạch";
    }
}
