using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Omnichannel.Domain.Entities
{
    /// <summary>
    /// Sổ cái tài chính ghi nhận doanh thu, giá vốn COGS, chi phí vận hành và thanh toán
    /// </summary>
    [Table("FinancialLedgers")]
    public class FinancialLedger
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string LedgerCode { get; set; } = string.Empty;

        [Column(TypeName = "datetime2(7)")]
        public DateTime EntryDate { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(30)]
        [Column(TypeName = "varchar(30)")]
        public string EntryType { get; set; } = "REVENUE"; // REVENUE, COGS, EXPENSE, SUPPLIER_PAYMENT

        public int StoreId { get; set; }
        public int? OrderId { get; set; }
        public int? SupplierId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DebitAmount { get; set; } = 0m; // Ghi Nợ

        [Column(TypeName = "decimal(18,2)")]
        public decimal CreditAmount { get; set; } = 0m; // Ghi Có

        [MaxLength(500)]
        public string? Description { get; set; }

        public int? CreatedBy { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Store Store { get; set; } = null!;
        public virtual Order? Order { get; set; }
        public virtual Supplier? Supplier { get; set; }
        public virtual User? CreatedByUser { get; set; }
    }

    /// <summary>
    /// Quản lý công nợ nhà cung cấp khi nhập hàng theo lô
    /// </summary>
    [Table("SupplierDebts")]
    public class SupplierDebt
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int SupplierId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string PurchaseOrderCode { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DebtAmount { get; set; } = 0m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; } = 0m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingAmount { get; set; } = 0m;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? DueDate { get; set; }

        [Required]
        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string Status { get; set; } = "PENDING"; // PENDING, PARTIAL, PAID

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public virtual Supplier Supplier { get; set; } = null!;
    }

    /// <summary>
    /// Đối soát tiền thu hộ COD từ các đơn vị vận chuyển (GHN, Viettel Post, GHTK)
    /// </summary>
    [Table("CodReconciliations")]
    public class CodReconciliation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string ReconciliationCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string ShippingPartner { get; set; } = "GHN"; // GHN, Viettel Post, GHTK

        [Column(TypeName = "datetime2(7)")]
        public DateTime ReconciliationDate { get; set; } = DateTime.UtcNow;

        public int TotalOrders { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCodAmount { get; set; } = 0m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ReceivedAmount { get; set; } = 0m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Difference { get; set; } = 0m;

        [Required]
        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string Status { get; set; } = "PENDING"; // PENDING, MATCHED, DISPUTED

        [MaxLength(500)]
        public string? Note { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
