using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Omnichannel.Application.DTOs.Finance
{
    public class FinancialLedgerDto
    {
        public int Id { get; set; }
        public string LedgerCode { get; set; } = string.Empty;
        public DateTime EntryDate { get; set; }
        public string EntryType { get; set; } = "REVENUE"; // REVENUE, COGS, EXPENSE, SUPPLIER_PAYMENT
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public int? OrderId { get; set; }
        public string? OrderCode { get; set; }
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public string? Description { get; set; }
        public int? CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }

        // Mở rộng phục vụ hiển thị Sổ cái thu chi & lũy kế
        public string Direction => (EntryType == "REVENUE" || CreditAmount > 0) ? "THU" : "CHI";
        public decimal Amount => Direction == "THU" ? (CreditAmount > 0 ? CreditAmount : DebitAmount) : (DebitAmount > 0 ? DebitAmount : CreditAmount);
        public decimal RunningBalance { get; set; }
    }

    public class CreateLedgerEntryDto
    {
        [Required]
        public string EntryType { get; set; } = "EXPENSE"; // REVENUE, COGS, EXPENSE, SUPPLIER_PAYMENT

        [Required]
        public int StoreId { get; set; }

        public int? OrderId { get; set; }
        public int? SupplierId { get; set; }

        public decimal DebitAmount { get; set; } = 0m;
        public decimal CreditAmount { get; set; } = 0m;

        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
    }

    public class SupplierDebtDto
    {
        public int Id { get; set; }
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string PurchaseOrderCode { get; set; } = string.Empty;
        public decimal DebtAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public DateTime? DueDate { get; set; }
        public string Status { get; set; } = "PENDING"; // PENDING, PARTIAL, PAID
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public bool IsOverdue => Status != "PAID" && DueDate.HasValue && DueDate.Value.Date < DateTime.UtcNow.Date;
        public int OverdueDays => IsOverdue ? (int)Math.Max(0, Math.Floor((DateTime.UtcNow.Date - DueDate!.Value.Date).TotalDays)) : 0;
    }

    public class CreateSupplierDebtDto
    {
        [Required]
        public int SupplierId { get; set; }

        [Required]
        [MaxLength(50)]
        public string PurchaseOrderCode { get; set; } = string.Empty;

        [Range(1000, 10000000000)]
        public decimal DebtAmount { get; set; }

        public DateTime? DueDate { get; set; }
    }

    public class RecordSupplierPaymentDto
    {
        [Required]
        public int DebtId { get; set; }

        [Required]
        public int StoreId { get; set; }

        [Range(1000, 10000000000)]
        public decimal PaymentAmount { get; set; }

        public string? Note { get; set; }
    }

    public class CodReconciliationDto
    {
        public int Id { get; set; }
        public string ReconciliationCode { get; set; } = string.Empty;
        public string ShippingPartner { get; set; } = "GHN"; // GHN, Viettel Post, GHTK
        public DateTime ReconciliationDate { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalCodAmount { get; set; }
        public decimal ReceivedAmount { get; set; }
        public decimal Difference { get; set; }
        public string Status { get; set; } = "PENDING"; // PENDING, MATCHED, DISPUTED
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateCodReconciliationDto
    {
        [Required]
        [MaxLength(50)]
        public string ShippingPartner { get; set; } = "GHN";

        public DateTime ReconciliationDate { get; set; } = DateTime.UtcNow;

        [Range(1, 100000)]
        public int TotalOrders { get; set; }

        [Range(0, 10000000000)]
        public decimal TotalCodAmount { get; set; }

        [Range(0, 10000000000)]
        public decimal ReceivedAmount { get; set; }

        public string? Note { get; set; }
    }

    public class ProfitLossReportDto
    {
        public int? StoreId { get; set; }
        public string? StoreName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal Revenue { get; set; }
        public decimal CostOfGoodsSold { get; set; } // Giá vốn bán hàng (Weighted Average Cost)
        public decimal GrossProfit { get; set; }
        public decimal GrossMarginPercent { get; set; }
        public decimal OperatingExpenses { get; set; } // Chi phí vận hành
        public decimal NetProfit { get; set; }
        public decimal NetMarginPercent { get; set; }
        public List<StoreProfitLossItemDto> BreakdownByStore { get; set; } = new();
        public List<MonthlyProfitLossDto> MonthlyTrends { get; set; } = new();
    }

    public class MonthlyProfitLossDto
    {
        public string MonthLabel { get; set; } = string.Empty; // e.g. "T05/2026"
        public decimal Revenue { get; set; }
        public decimal CostOfGoodsSold { get; set; }
        public decimal OperatingExpenses { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal NetProfit { get; set; }
    }

    public class StoreProfitLossItemDto
    {
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public decimal Area { get; set; }
        public decimal Revenue { get; set; }
        public decimal Cogs { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal OperatingExpenses { get; set; }
        public decimal NetProfit { get; set; }
        public decimal SalesPerSqft { get; set; }
    }

    public class InventoryTurnoverDto
    {
        public int? StoreId { get; set; }
        public string? StoreName { get; set; }
        public decimal PeriodCogs { get; set; }
        public decimal AverageInventoryValue { get; set; }
        public decimal TurnoverRatio { get; set; } // Vòng quay tồn kho (COGS / Average Inventory)
        public decimal DaysSalesOfInventory { get; set; } // Số ngày lưu kho bình quân (365 / Turnover)
    }

    public class SalesPerSqftDto
    {
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public decimal AreaSquareMeters { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal SalesPerSquareMeter { get; set; }
        public int TotalOrders { get; set; }
    }
}
