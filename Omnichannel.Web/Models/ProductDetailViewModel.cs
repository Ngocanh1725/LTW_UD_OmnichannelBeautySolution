using System;
using System.Collections.Generic;
using Omnichannel.Application.DTOs.Cms;
using Omnichannel.Application.DTOs.Inventory;
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

        // 3. Showroom Stock Availability List (O2O)
        public ShowroomAvailabilityViewModel ShowroomAvailability { get; set; } = new();
        public List<ShowroomStockDto> ShowroomStocks
        {
            get => ShowroomAvailability.Stores;
            set => ShowroomAvailability.Stores = value;
        }

        // 4. Detailed Ingredients Breakdown
        public List<IngredientDetailDto> IngredientBreakdowns { get; set; } = new();

        // 5. Clean Beauty & Parsed INCI Ingredients
        public List<Omnichannel.Application.DTOs.BeautyTech.ParsedIngredientDto> ParsedIngredients { get; set; } = new();

        // 6. Frequently Bought Together (FBT) - Phiên C2
        public Omnichannel.Application.DTOs.Catalog.FrequentlyBoughtTogetherDto? FrequentlyBoughtTogether { get; set; }
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
