using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Sales
{
    public class CartItemViewModel
    {
        public int CartItemId { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal => UnitPrice * Quantity;
        public string? NearestExpDateStr { get; set; }
        public string ThumbnailUrl { get; set; } = string.Empty;
    }

    public class CartViewModel
    {
        public string CartId { get; set; } = string.Empty;
        public List<CartItemViewModel> Items { get; set; } = new();
        public int TotalItems => Items.Sum(i => i.Quantity);
        public decimal SubTotal => Items.Sum(i => i.LineTotal);
        public decimal DiscountAmount { get; set; } = 0;
        public decimal ShippingFee => SubTotal >= 500000 || SubTotal == 0 ? 0 : 30000;
        public decimal FinalTotal => Math.Max(0, SubTotal - DiscountAmount + ShippingFee);
        public string? AppliedCouponCode { get; set; }
    }

    public class AddToCartRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
    }

    public class UpdateCartItemRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public int Delta { get; set; }
    }
}