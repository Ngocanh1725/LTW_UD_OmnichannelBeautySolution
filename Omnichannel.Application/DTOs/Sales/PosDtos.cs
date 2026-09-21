using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Sales
{
    public class PosProductDto
    {
        public int ProductId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal SellingPrice { get; set; }
        public decimal DiscountPrice { get; set; }
        public decimal FinalPrice { get; set; }
        public string? ImageUrl { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int AvailableQuantity { get; set; }
    }

    public class PosCartItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
    }

    public class PosReceiptPrintViewModel
    {
        public string OrderId { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public string StoreAddress { get; set; } = string.Empty;
        public string StorePhone { get; set; } = string.Empty;
        public string InvoiceSeries { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CashierName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal VatAmount { get; set; }
        public decimal FinalTotal { get; set; }
        public string PaymentMethodName { get; set; } = string.Empty;
        public decimal CashGiven { get; set; }
        public decimal ChangeAmount { get; set; }
        public string TaxLookupCode { get; set; } = string.Empty;
        public List<PosReceiptPrintItemViewModel> Items { get; set; } = new();
    }

    public class PosReceiptPrintItemViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public string ExpDateStr { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class PosCheckoutDto
    {
        public string StoreId { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string PaymentMethod { get; set; } = "CASH";
        public decimal AmountReceived { get; set; }
        public decimal DiscountAmount { get; set; }
        public List<PosCartItemDto> Items { get; set; } = new();
    }

    public class PosReceiptItemDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string? BatchCode { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class PosReceiptDto
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public string CashierName { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountReceived { get; set; }
        public decimal ChangeAmount { get; set; }
        public string PaymentMethod { get; set; } = "CASH";
        public List<PosReceiptItemDto> Items { get; set; } = new();
    }
}