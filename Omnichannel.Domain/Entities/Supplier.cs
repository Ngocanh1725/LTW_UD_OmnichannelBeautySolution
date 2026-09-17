using System;
using System.Collections.Generic;

namespace Omnichannel.Domain.Entities
{
    public class Supplier
    {
        public string SupplierId { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string? TaxCode { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Address { get; set; } = string.Empty;
        public decimal TotalDebt { get; set; }
        public bool Status { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}