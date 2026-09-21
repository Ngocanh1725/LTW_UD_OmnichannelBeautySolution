using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Sales
{
    public class CartDto
    {
        public int Id { get; set; }
        public string? SessionId { get; set; }
        public int? UserId { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public int TotalItems { get; set; }

        public List<CartItemDto> Items { get; set; } = new();
        public List<CartItemDto> CartItems { get; set; } = new();
    }

    // Định nghĩa CartViewModel để tương thích với OrderDtos.cs
    public class CartViewModel : CartDto
    {
        public decimal ShippingFee { get; set; }
        public decimal FinalTotal { get; set; }
    }

    public class CartItemDto
    {
        public int Id { get; set; }
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductSku { get; set; }
        public string? ProductImageUrl { get; set; }
        public int? ProductBatchId { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpDate { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal LineTotal { get; set; }
        public string NearestExpDateStr { get; set; } = string.Empty;
    }

    public class AddToCartRequestDto
    {
        public string? SessionId { get; set; }
        public string? UserId { get; set; }
        public int ProductId { get; set; }
        public int? ProductBatchId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class UpdateCartItemQuantityDto
    {
        public string? SessionId { get; set; }
        public string? UserId { get; set; }
        public int CartItemId { get; set; }
        public int Quantity { get; set; }
    }
}