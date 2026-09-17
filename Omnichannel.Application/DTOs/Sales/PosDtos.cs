using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Omnichannel.Application.DTOs.Sales
{
    public class PosProductSearchDto
    {
        public string ProductId { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal SellingPrice { get; set; }
        public string Unit { get; set; } = "Hộp";
        public int AvailableQuantity { get; set; }
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public string ExpDateStr { get; set; } = string.Empty;
    }

    public class PosCartItemDto
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public string ExpDateStr { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal DiscountRate { get; set; } = 0; // % giảm giá dòng
        public decimal LineTotal => Math.Round(UnitPrice * Quantity * (1 - DiscountRate / 100), 2);
    }

    public class PosCheckoutRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn cửa hàng thu ngân")]
        public string StoreId { get; set; } = string.Empty;

        public string? CustomerPhone { get; set; }
        public string? CustomerName { get; set; }

        public List<PosCartItemDto> Items { get; set; } = new();

        public decimal OrderDiscountAmount { get; set; } = 0;

        [Required]
        public int PaymentMethod { get; set; } = 1; // 1: Tiền mặt, 2: VietQR, 3: Thẻ POS quẹt

        public decimal CashGiven { get; set; } = 0; // Tiền khách đưa
        public string? Notes { get; set; }
    }

    public class PosCheckoutResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string InvoiceId { get; set; } = string.Empty;
        public string InvoiceSeries { get; set; } = string.Empty;
        public long InvoiceNumber { get; set; }
        public string TaxLookupCode { get; set; } = string.Empty;
        public decimal FinalTotal { get; set; }
        public decimal CashGiven { get; set; }
        public decimal ChangeAmount => Math.Max(0, CashGiven - FinalTotal);
        public DateTime CreatedAt { get; set; }
    }

    public class PosReceiptPrintViewModel
    {
        public string StoreName { get; set; } = string.Empty;
        public string StoreAddress { get; set; } = string.Empty;
        public string StorePhone { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string InvoiceId { get; set; } = string.Empty;
        public string InvoiceSeries { get; set; } = string.Empty;
        public long InvoiceNumber { get; set; }
        public string TaxLookupCode { get; set; } = string.Empty;
        public string CashierName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<PosCartItemDto> Items { get; set; } = new();
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal VatRate { get; set; } = 10.00m;
        public decimal VatAmount { get; set; }
        public decimal FinalTotal { get; set; }
        public int PaymentMethod { get; set; }
        public string PaymentMethodName => PaymentMethod switch
        {
            1 => "Tiền mặt",
            2 => "VietQR Động",
            3 => "Thẻ ngân hàng (POS)",
            _ => "Khác"
        };
        public decimal CashGiven { get; set; }
        public decimal ChangeAmount { get; set; }
    }
}