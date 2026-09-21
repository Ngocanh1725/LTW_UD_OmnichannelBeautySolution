using System;
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

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng")]
        [Display(Name = "Địa chỉ giao hàng")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Display(Name = "Ghi chú đơn hàng")]
        public string? Notes { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        [Display(Name = "Phương thức thanh toán")]
        public int PaymentMethod { get; set; } = 2; // 1: Tiền mặt, 2: VietQR, 3: Thẻ, 4: COD

        public CartViewModel Cart { get; set; } = new();
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