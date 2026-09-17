using System;
using System.Collections.Generic;

namespace Omnichannel.Domain.Entities
{
    public class Product
    {
        public string ProductId { get; set; } = string.Empty; // SKU
        public int CategoryId { get; set; }
        public string SupplierId { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public decimal SellingPrice { get; set; }
        public decimal CostPrice { get; set; }
        public int SafetyStock { get; set; }
        public string Unit { get; set; } = "Hộp";
        public int Status { get; set; } // 1: Active, 0: Inactive
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual Category Category { get; set; } = null!;
        public virtual Supplier Supplier { get; set; } = null!;
        public virtual ICollection<ProductBatch> ProductBatches { get; set; } = new List<ProductBatch>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
