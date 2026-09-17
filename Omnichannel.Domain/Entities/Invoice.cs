using System;

namespace Omnichannel.Domain.Entities
{
    public class Invoice
    {
        public string InvoiceId { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string InvoiceSeries { get; set; } = string.Empty;
        public long InvoiceNumber { get; set; }
        public string TaxLookupCode { get; set; } = string.Empty;
        public decimal TotalBeforeTax { get; set; }
        public decimal VatRate { get; set; }
        public decimal VatAmount { get; set; }
        public decimal TotalAfterTax { get; set; }
        public string? CustomerTaxCode { get; set; }
        public string? CompanyBillingName { get; set; }
        public DateTime IssuedAt { get; set; }
        public string IssuedByStaffId { get; set; } = string.Empty;
        public string? PdfStorageUrl { get; set; }
        public bool IsCancelled { get; set; }

        public virtual Order Order { get; set; } = null!;
        public virtual User Issuer { get; set; } = null!;
    }
}