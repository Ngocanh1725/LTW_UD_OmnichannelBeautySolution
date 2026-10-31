using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Omnichannel.Domain.Common;

namespace Omnichannel.Domain.Entities
{
    [Table("PosShifts")]
    public class PosShift : AggregateRoot
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string ShiftCode { get; set; } = string.Empty; // e.g., "SHIFT-20261001-001"

        public int StoreId { get; set; }
        public int CashierId { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime OpenedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? ClosedAt { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OpeningBalance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ExpectedCash { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ActualCash { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Difference { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CashSalesTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CardSalesTotal { get; set; }

        public int TransactionCount { get; set; }

        [Required]
        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string Status { get; set; } = "OPEN"; // OPEN, CLOSED, VERIFIED

        [MaxLength(500)]
        public string? Note { get; set; }

        // Navigation properties
        public virtual Store Store { get; set; } = null!;
        public virtual User Cashier { get; set; } = null!;
        public virtual ICollection<PosDrawerTransaction> DrawerTransactions { get; set; } = new List<PosDrawerTransaction>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }

    [Table("PosDrawerTransactions")]
    public class PosDrawerTransaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int PosShiftId { get; set; }

        [Required]
        [MaxLength(30)]
        [Column(TypeName = "varchar(30)")]
        public string Type { get; set; } = string.Empty; // CASH_IN, CASH_OUT, PETTY_CASH

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(255)]
        public string Reason { get; set; } = string.Empty;

        public int PerformedBy { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual PosShift PosShift { get; set; } = null!;
        public virtual User Performer { get; set; } = null!;
    }
}
