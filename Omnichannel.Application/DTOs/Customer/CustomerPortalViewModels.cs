using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Customer
{
    // ==========================================
    // 1. SỔ ĐỊA CHỈ KHÁCH HÀNG (ADDRESSES)
    // ==========================================
    public class CustomerAddressViewModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string DetailedAddress { get; set; } = string.Empty;
        public string FullAddress { get; set; } = string.Empty;
        public string Label { get; set; } = "HOME"; // DEFAULT, HOME, OFFICE (MẶC ĐỊNH, NHÀ RIÊNG, CÔNG TY)
        public bool IsDefault { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class SaveAddressRequest
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string DetailedAddress { get; set; } = string.Empty;
        public string Label { get; set; } = "NHÀ RIÊNG"; // MẶC ĐỊNH, NHÀ RIÊNG, CÔNG TY
        public bool IsDefault { get; set; } = false;
    }

    public class AddressListViewModel
    {
        public int UserId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public List<CustomerAddressViewModel> Addresses { get; set; } = new();
    }

    // ==========================================
    // 2. LỊCH SỬ ĐƠN HÀNG (ORDERS LIST)
    // ==========================================
    public class CustomerOrdersViewModel
    {
        public int UserId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CurrentTab { get; set; } = "all"; // all, pending, confirmed, shipping, completed, cancelled
        public Dictionary<string, int> StatusCounts { get; set; } = new();
        public List<CustomerOrderSummaryDto> Orders { get; set; } = new();
    }

    public class CustomerOrderSummaryDto
    {
        public int Id { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string OrderStatus { get; set; } = "PENDING";
        public string OrderStatusDisplayName { get; set; } = "Chờ xác nhận";
        public string OrderStatusBadgeClass { get; set; } = "bg-amber-100 text-amber-800 border-amber-200";
        public decimal TotalAmount { get; set; }
        public int TotalItemsCount { get; set; }
        public string PrimaryProductName { get; set; } = string.Empty;
        public int RemainingProductsCount { get; set; }
        public List<string> ProductThumbnails { get; set; } = new();
        public string PaymentMethod { get; set; } = "COD";
        public string PaymentStatus { get; set; } = "UNPAID";
        public bool CanReorder { get; set; } = true;
    }

    // ==========================================
    // 3. CHI TIẾT ĐƠN HÀNG (ORDER DETAIL)
    // ==========================================
    public class CustomerOrderDetailViewModel
    {
        public int Id { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string OrderStatus { get; set; } = "PENDING";
        public string OrderStatusDisplayName { get; set; } = "Chờ xác nhận";
        public string OrderStatusBadgeClass { get; set; } = "bg-amber-100 text-amber-800 border-amber-200";
        public string PaymentMethod { get; set; } = "COD";
        public string PaymentStatus { get; set; } = "UNPAID";
        public string? OrderNote { get; set; }

        // Timeline trạng thái 5 nấc: [Đặt hàng thành công] -> [Đã xác nhận] -> [Đang chuẩn bị hàng FEFO] -> [Bàn giao Shipper] -> [Giao thành công]
        public int TimelineStep { get; set; } = 1; // 1 to 5
        public List<OrderTimelineEventDto> TimelineEvents { get; set; } = new();

        // Giao hàng & Vận chuyển
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string CarrierName { get; set; } = "Giao Hàng Nhanh (GHN Express)";
        public string TrackingCode { get; set; } = "GHN-89382026VN";

        // Danh sách sản phẩm
        public List<CustomerOrderItemDto> Items { get; set; } = new();

        // Tài chính & Khuyến mãi
        public decimal SubTotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? AppliedVoucherCode { get; set; }
        public decimal TotalAmount { get; set; }
        public int LoyaltyPointsEarned { get; set; } // Điểm tích lũy nhận được từ đơn

        // Yêu cầu xuất hóa đơn VAT
        public bool HasVatRequested { get; set; }
        public VatInvoiceRequestDto? VatInfo { get; set; }
    }

    public class CustomerOrderItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public string? VariantName { get; set; } // e.g. "50ml", "Tone 01 Đỏ thuần"
        public string? FefoBatchNumber { get; set; } // Lô FEFO
        public DateTime? FefoExpiryDate { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountRate { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class OrderTimelineEventDto
    {
        public int StepIndex { get; set; } // 1 to 5
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? FormattedTime { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsCurrent { get; set; }
    }

    public class VatInvoiceRequestDto
    {
        public int OrderId { get; set; }
        public string TaxCode { get; set; } = string.Empty; // Mã số thuế
        public string CompanyName { get; set; } = string.Empty; // Tên công ty
        public string CompanyAddress { get; set; } = string.Empty; // Địa chỉ công ty
        public string InvoiceEmail { get; set; } = string.Empty; // Email nhận hóa đơn điện tử
        public string? Note { get; set; }
        public DateTime? RequestedAt { get; set; }
    }
}
