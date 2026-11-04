using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Omnichannel.Application.DTOs.Sales
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [Display(Name = "Họ và tên người nhận")]
        public string RecipientName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại liên hệ")]
        public string RecipientPhone { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ")]
        [Display(Name = "Địa chỉ Email")]
        public string? RecipientEmail { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng")]
        [Display(Name = "Địa chỉ giao hàng (Số nhà, Tên đường)")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn Tỉnh/Thành phố")]
        public string ProvinceId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn Quận/Huyện")]
        public string DistrictId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn Phường/Xã")]
        public string WardCode { get; set; } = string.Empty;

        [Display(Name = "Phương thức vận chuyển")]
        public string ShippingMethod { get; set; } = "STANDARD"; // "STANDARD" (30k/Freeship), "EXPRESS_2H" (45k), hoặc "STORE_PICKUP" (0đ)

        [Display(Name = "Hình thức nhận hàng")]
        public string DeliveryMethod { get; set; } = "HOME_DELIVERY"; // "HOME_DELIVERY" hoặc "STORE_PICKUP"

        [Display(Name = "Showroom nhận hàng")]
        public int? PickupStoreId { get; set; }

        [Display(Name = "Ghi chú đơn hàng")]
        public string? Notes { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        [Display(Name = "Phương thức thanh toán")]
        public int PaymentMethod { get; set; } = 4; // 1: Tiền mặt, 2: VietQR, 3: VNPay, 4: COD, 5: MoMo

        [Display(Name = "Mã giảm giá (Coupon)")]
        public string? CouponCode { get; set; }

        // --- TÍCH HỢP ĐIỂM THƯỞNG LOYALTY ---
        [Display(Name = "Sử dụng điểm thưởng Loyalty")]
        public bool UseLoyaltyPoints { get; set; } = false;

        public int AvailableLoyaltyPoints { get; set; } = 100;
        public int LoyaltyPointsToRedeem { get; set; } = 100;
        public decimal LoyaltyDiscountAmount { get; set; } = 100000;
        public decimal PointConversionRate { get; set; } = 1000; // 1 điểm = 1.000₫

        public CartViewModel Cart { get; set; } = new();
        public List<StorePickupCandidateDto> PickupStores { get; set; } = new();
    }

    public class StorePickupCandidateDto
    {
        public int StoreId { get; set; }
        public string StoreCode { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string OpeningHours { get; set; } = "08:30 – 22:00 (Tất cả các ngày)";
        public bool HasFullCartStock { get; set; }
        public int TotalAvailableUnitsForCart { get; set; }
        public string? EarliestFefoBatchCode { get; set; }
        public DateTime? EarliestFefoExpDate { get; set; }
        public List<string> MissingProductNames { get; set; } = new();
        public string GoogleMapsUrl => $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString($"{StoreName} {Address}")}";
    }

    public class OrderSuccessViewModel
    {
        public string OrderId { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public decimal FinalTotal { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string OrderStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public VietQrPaymentDto? VietQr { get; set; }
        public string ShippingMethod { get; set; } = "STANDARD";
        public string DeliveryMethod { get; set; } = "HOME_DELIVERY";
        public int? PickupStoreId { get; set; }
        public string? PickupStoreName { get; set; }
        public string? PickupStoreAddress { get; set; }
        public string? PickupStorePhone { get; set; }
        public string? PickupCode { get; set; }
        public DateTime? PickupExpiresAt { get; set; }
        public DateTime? PickedUpAt { get; set; }
        public string EstimatedDeliveryTime { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public string? Notes { get; set; }
        public int OrderDbId { get; set; }
        public string? MomoPayUrl { get; set; }
        public string? MomoQrUrl { get; set; }
        public List<OrderDetailItemDto> Items { get; set; } = new();
    }

    public class PosPickupOrderDto
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public int PickupStoreId { get; set; }
        public string PickupStoreName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string OrderStatus { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PickupExpiresAt { get; set; }
        public bool IsExpired => PickupExpiresAt.HasValue && DateTime.UtcNow > PickupExpiresAt.Value;
        public string MaskedPickupCode { get; set; } = "******";
        public List<OrderDetailItemDto> Items { get; set; } = new();
    }

    public class HandoverPickupOrderRequestDto
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string PickupCode { get; set; } = string.Empty;
        public int CashierStoreId { get; set; }
        public int? CashierUserId { get; set; }
        public string? CashierName { get; set; }
        public int? PosShiftId { get; set; }
    }

    public class OrderDetailItemDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string? VariantName { get; set; }
        public string? BatchNumber { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class VietQrPaymentDto
    {
        public string BankName { get; set; } = "MBBank (Ngân Hàng Quân Đội)";
        public string BankAccountNo { get; set; } = "0909999888";
        public string AccountName { get; set; } = "CONG TY TNHH OMNICHANNEL BEAUTY";
        public decimal Amount { get; set; }
        public string OrderInfo { get; set; } = string.Empty;
        public string QrImageUrl { get; set; } = string.Empty;
        public int ExpireSeconds { get; set; } = 900; // 15 phút
    }

    public class BankCallbackDto
    {
        public string OrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string TransactionRef { get; set; } = string.Empty;
        public string Status { get; set; } = "SUCCESS";
    }
}
