using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Customer
{
    public class WishlistItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = "/images/products/_placeholder.webp";
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public int DiscountPercent { get; set; }
        public string CategoryName { get; set; } = "Mỹ Phẩm";
        public string BrandName { get; set; } = "Omni Beauty";
        public bool IsInStock { get; set; } = true;
        public int StockQuantity { get; set; } = 10;
        public double Rating { get; set; } = 4.9;
        public int ReviewCount { get; set; } = 38;
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }

    public class WishlistPageViewModel
    {
        public int? UserId { get; set; }
        public string CustomerName { get; set; } = "Khách Hàng";
        public bool IsAuthenticated { get; set; } = false;
        public List<WishlistItemDto> Items { get; set; } = new();
        public int TotalCount => Items.Count;
        public int InStockCount => Items.Count(i => i.IsInStock);
        public int OutOfStockCount => Items.Count(i => !i.IsInStock);
    }

    public class ToggleWishlistRequest
    {
        public int ProductId { get; set; }
    }

    public class GetProductsByIdsRequest
    {
        public List<int> ProductIds { get; set; } = new();
    }

    public class StockAlertRequest
    {
        public int ProductId { get; set; }
        public int? ProductVariantId { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? CustomerName { get; set; }
    }

    public class StockAlertResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
