using System;
using System.Collections.Generic;
using Omnichannel.Application.DTOs.Cms;

namespace Omnichannel.Application.DTOs.Sales
{
    public class HomeIndexViewModel
    {
        public List<BannerItemViewModel> HeroBanners { get; set; } = new();
        public List<CategoryCardDto> FeaturedCategories { get; set; } = new();
        public List<StorefrontProductDto> FlashSaleProducts { get; set; } = new();
        public List<StorefrontProductDto> LatestProducts { get; set; } = new();
    }

    public class CategoryCardDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
        public int ProductCount { get; set; }
    }

    public class StorefrontProductDto
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal SellingPrice { get; set; }
        public decimal OriginalPrice => SellingPrice * 1.25m; // Giá gạch mô phỏng
        public string Unit { get; set; } = "Hộp";
        public string? EarliestExpDateStr { get; set; }
        public int TotalAvailableQuantity { get; set; }
        public bool IsInStock => TotalAvailableQuantity > 0;
    }

    public class AddToCartRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public class UpdateCartItemRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public int Delta { get; set; }
    }
}
