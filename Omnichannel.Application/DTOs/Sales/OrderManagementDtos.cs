using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Sales
{
    public class AdminOrderFilterRequest
    {
        public string? Keyword { get; set; }
        public string? Status { get; set; } = "all"; // all, PENDING, CONFIRMED, PROCESSING, SHIPPING, COMPLETED, CANCELLED
        public string? OrderSource { get; set; } = "all"; // all, POS, WEBSITE
        public int? StoreId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class AdminOrderItemViewModel
    {
        public int Id { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public string? ShippingAddress { get; set; }
        public string OrderSource { get; set; } = "POS"; // POS, WEBSITE
        public string OrderStatus { get; set; } = "PENDING"; // PENDING, CONFIRMED, PROCESSING, SHIPPING, COMPLETED, CANCELLED
        public string PaymentStatus { get; set; } = "UNPAID"; // UNPAID, PAID, REFUNDED
        public string PaymentMethod { get; set; } = "CASH";
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class AdminOrderPagedListViewModel
    {
        public List<AdminOrderItemViewModel> Items { get; set; } = new();
        public AdminOrderFilterRequest Filter { get; set; } = new();
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }

        // Tab counts
        public int CountAll { get; set; }
        public int CountPending { get; set; }
        public int CountConfirmed { get; set; }
        public int CountShipping { get; set; }
        public int CountCompleted { get; set; }
        public int CountCancelled { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class AdminOrderDetailViewModel
    {
        public int Id { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string StoreAddress { get; set; } = string.Empty;
        public string? StorePhone { get; set; }

        public string OrderSource { get; set; } = "POS";
        public string OrderStatus { get; set; } = "PENDING";
        public string PaymentStatus { get; set; } = "UNPAID";
        public string PaymentMethod { get; set; } = "CASH";

        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalAmount { get; set; }

        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public string? ShippingAddress { get; set; }
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public string? InvoiceCode { get; set; }
        public DateTime? InvoiceDate { get; set; }

        // Bổ sung thông tin khách hàng VIP & Lịch sử mua sắm
        public string CustomerVipTier { get; set; } = "Khách Hàng Mới";
        public string CustomerVipBadgeColor { get; set; } = "slate";
        public decimal CustomerTotalSpent { get; set; }
        public int CustomerTotalOrders { get; set; }
        public int CustomerLoyaltyPoints { get; set; }
        public DateTime? CustomerMemberSince { get; set; }
        public List<CustomerOrderHistoryBriefDto> CustomerRecentOrders { get; set; } = new();

        public List<AdminOrderDetailItemViewModel> Items { get; set; } = new();
        public List<AdminOrderStatusHistoryDto> Timeline { get; set; } = new();
    }

    public class AdminOrderDetailItemViewModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSku { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public string Unit { get; set; } = "Chai";
        public int? ProductBatchId { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpDate { get; set; }
        public int? DaysRemaining { get; set; }
        public string? BatchStatus { get; set; }
        public string? FefoAssignedReason { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountRate { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class CustomerOrderHistoryBriefDto
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string OrderSource { get; set; } = "POS";
    }

    public class AdminOrderStatusHistoryDto
    {
        public string Status { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsCurrent { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class UpdateOrderStatusRequest
    {
        public int OrderId { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public string? Note { get; set; }
    }
}

