using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Omnichannel.Domain.Entities
{
    [Table("Carts")]
    public class Cart
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? UserId { get; set; }

        [MaxLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string? CartSessionId { get; set; } // Cho khÃ¡ch vÃ£ng lai (Guest)

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? UpdatedAt { get; set; }

        public virtual User? User { get; set; }
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }

    [Table("CartItems")]
    public class CartItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int CartId { get; set; }
        public int ProductId { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Cart Cart { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }

    [Table("Orders")]
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string OrderCode { get; set; } = string.Empty; // e.g., ORD-20260921-001

        public int? UserId { get; set; }
        public int StoreId { get; set; }

        [Required]
        [MaxLength(30)]
        [Column(TypeName = "varchar(30)")]
        public string OrderSource { get; set; } = "POS"; // POS, WEBSITE, MOBILE_APP

        [Required]
        [MaxLength(30)]
        [Column(TypeName = "varchar(30)")]
        public string OrderStatus { get; set; } = "PENDING"; // PENDING, CONFIRMED, PROCESSING, COMPLETED, CANCELLED

        [Required]
        [MaxLength(30)]
        [Column(TypeName = "varchar(30)")]
        public string PaymentStatus { get; set; } = "UNPAID"; // UNPAID, PAID, REFUNDED

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string PaymentMethod { get; set; } = "CASH"; // CASH, VNPAY, MOMO, CREDIT_CARD, COD

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [MaxLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string CustomerPhone { get; set; } = string.Empty;

        [MaxLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string? CustomerEmail { get; set; }

        [MaxLength(255)]
        public string? ShippingAddress { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public virtual User? User { get; set; }
        public virtual Store Store { get; set; } = null!;
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public virtual Invoice? Invoice { get; set; }
    }

    [Table("OrderDetails")]
    public class OrderDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int? ProductBatchId { get; set; } // GÃ¡n lÃ´ cá»¥ thá»ƒ xuáº¥t kho theo thuáº­t toÃ¡n FEFO

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountRate { get; set; } = 0; // Chiáº¿t kháº¥u theo % (0.00% - 100.00%)

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotal { get; set; }

        public virtual Order Order { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
        public virtual ProductBatch? ProductBatch { get; set; }

        /// <summary>
        /// Domain Method: TÃ­nh tá»•ng tiá»n dÃ²ng sau khi trá»« chiáº¿t kháº¥u dÃ²ng
        /// </summary>
        public decimal CalculateLineTotal()
        {
            var grossAmount = Quantity * UnitPrice;
            var discount = grossAmount * (DiscountRate / 100m);
            LineTotal = Math.Round(grossAmount - discount, 2, MidpointRounding.AwayFromZero);
            return LineTotal;
        }
    }

    [Table("Invoices")]
    public class Invoice
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string InvoiceCode { get; set; } = string.Empty; // e.g., INV-20260921-001

        public int OrderId { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string PaymentMethod { get; set; } = "CASH";

        [Column(TypeName = "decimal(5,2)")]
        public decimal TaxRate { get; set; } = 8.00m; // Thuáº¿ VAT lÃ m Ä‘áº¹p 8% hoáº·c 10%

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public int? CashierId { get; set; } // Thu ngÃ¢n xuáº¥t hÃ³a Ä‘Æ¡n

        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string? CustomerTaxCode { get; set; }

        [MaxLength(200)]
        public string? CompanyName { get; set; }

        [Required]
        [MaxLength(30)]
        [Column(TypeName = "varchar(30)")]
        public string Status { get; set; } = "ISSUED"; // ISSUED, CANCELLED

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Order Order { get; set; } = null!;
        public virtual User? Cashier { get; set; }
    }
}
