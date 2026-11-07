using System;
using System.ComponentModel.DataAnnotations;

namespace Omnichannel.Application.DTOs.Notifications
{
    public class SubscribeBackInStockRequest
    {
        [Required(ErrorMessage = "Mã sản phẩm không được để trống")]
        public int ProductId { get; set; }

        public string? CustomerName { get; set; }

        [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ")]
        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }
    }

    public class SubscribePriceDropRequest
    {
        [Required(ErrorMessage = "Mã sản phẩm không được để trống")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mức giá mong đợi")]
        [Range(1000, 100000000, ErrorMessage = "Mức giá mong đợi không hợp lệ")]
        public decimal TargetPrice { get; set; }

        public string? CustomerName { get; set; }

        [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ")]
        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }
    }

    public class SubscriptionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? SubscriptionId { get; set; }
        public bool AlreadySubscribed { get; set; }
    }

    public class CustomerSubscriptionDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSlug { get; set; } = string.Empty;
        public string? ProductThumbnailUrl { get; set; }
        public string Type { get; set; } = "BACK_IN_STOCK"; // BACK_IN_STOCK, PRICE_DROP
        public decimal? TargetPrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public string Status { get; set; } = "ACTIVE"; // ACTIVE, NOTIFIED, CANCELLED
        public DateTime CreatedAt { get; set; }
        public DateTime? NotifiedAt { get; set; }
        public bool IsInStock { get; set; }
        public int AvailableStock { get; set; }
    }
}

