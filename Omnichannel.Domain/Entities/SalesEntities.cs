using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Omnichannel.Domain.Common;
//Lưu giỏ hàng, thông tin đơn đặt hàng, chi tiết món hàng đã mua và hóa đơn thanh toán.
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
        public string? CartSessionId { get; set; } // Cho khách vãng lai (Guest)

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
    public class Order : AggregateRoot
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string OrderCode { get; set; } = string.Empty; // e.g., ORD-20260921-001

        public int? UserId { get; set; }
        public int StoreId { get; set; }
        public int? PosShiftId { get; set; }

        [Required]
        [MaxLength(30)]
        [Column(TypeName = "varchar(30)")]
        public string OrderSource { get; set; } = "POS"; // POS, WEBSITE, MOBILE_APP

        [Required]
        [MaxLength(30)]
        [Column(TypeName = "varchar(30)")]
        public string OrderStatus { get; set; } = "PENDING"; // PENDING, CONFIRMED, PROCESSING, SHIPPING, COMPLETED, CANCELLED

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

        [Required]
        [MaxLength(30)]
        [Column(TypeName = "varchar(30)")]
        public string DeliveryMethod { get; set; } = "HOME_DELIVERY"; // HOME_DELIVERY, STORE_PICKUP

        public int? PickupStoreId { get; set; }

        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string? PickupCode { get; set; } // Mã PIN 6 số nhận hàng tại quầy (Click & Collect)

        [Column(TypeName = "datetime2(7)")]
        public DateTime? PickupExpiresAt { get; set; } // Thời hạn giữ hàng 48h tại Showroom

        [Column(TypeName = "datetime2(7)")]
        public DateTime? PickedUpAt { get; set; } // Thời điểm bàn giao thực tế tại quầy POS

        [MaxLength(500)]
        public string? Note { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public virtual User? User { get; set; }
        public virtual Store Store { get; set; } = null!;
        public virtual Store? PickupStore { get; set; }
        public virtual PosShift? PosShift { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public virtual Invoice? Invoice { get; set; }
        public virtual ICollection<PointTransaction> PointTransactions { get; set; } = new List<PointTransaction>();
        public virtual ICollection<Voucher> UsedVouchers { get; set; } = new List<Voucher>();
        public virtual ICollection<CouponUsage> CouponUsages { get; set; } = new List<CouponUsage>();
        public virtual ICollection<OrderStatusHistory> StatusHistories { get; set; } = new List<OrderStatusHistory>();
        public virtual ICollection<OrderReturnRequest> ReturnRequests { get; set; } = new List<OrderReturnRequest>();
    }

    [Table("OrderDetails")]
    public class OrderDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int? ProductBatchId { get; set; } // Gán lô cụ thể xuất kho theo thuật toán FEFO

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountRate { get; set; } = 0; // Chiết khấu theo % (0.00% - 100.00%)

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotal { get; set; }

        public virtual Order Order { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
        public virtual ProductBatch? ProductBatch { get; set; }

        /// <summary>
        /// Domain Method: Tính tổng tiền dòng sau khi trừ chiết khấu dòng
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
        public decimal TaxRate { get; set; } = 8.00m; // Thuế VAT làm đẹp 8% hoặc 10%

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public int? CashierId { get; set; } // Thu ngân xuất hóa đơn

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

    [Table("OrderStatusHistories")]
    public class OrderStatusHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int OrderId { get; set; }

        [Required]
        [MaxLength(30)]
        [Column(TypeName = "varchar(30)")]
        public string Status { get; set; } = string.Empty; // PENDING, CONFIRMED, PROCESSING, HANDED_OVER, SHIPPING, COMPLETED, CANCELLED, RETURN_REQUESTED, RETURN_APPROVED, RETURN_REJECTED

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string? TrackingCode { get; set; }

        [MaxLength(100)]
        public string? CarrierName { get; set; }

        public int? ChangedByUserId { get; set; }

        [MaxLength(100)]
        public string? ChangedByName { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Order Order { get; set; } = null!;
    }

    [Table("OrderReturnRequests")]
    public class OrderReturnRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int OrderId { get; set; }
        public int? UserId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string RequestCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(2000)]
        public string? EvidenceUrlsJson { get; set; }

        [Required]
        [MaxLength(30)]
        [Column(TypeName = "varchar(30)")]
        public string Status { get; set; } = "PENDING"; // PENDING, APPROVED, REJECTED

        [MaxLength(500)]
        public string? AdminNote { get; set; }

        public int? ProcessedByUserId { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? ProcessedAt { get; set; }

        public virtual Order Order { get; set; } = null!;
        public virtual User? User { get; set; }
    }
}
