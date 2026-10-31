using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Sales
{
    public class OpenShiftDto
    {
        public int StoreId { get; set; }
        public int CashierId { get; set; }
        public decimal OpeningBalance { get; set; }
    }

    public class CloseShiftDto
    {
        public int ShiftId { get; set; }
        public decimal ActualCash { get; set; }
        public string? Note { get; set; }
    }

    public class DrawerTransactionDto
    {
        public string Type { get; set; } = "CASH_OUT"; // CASH_IN, CASH_OUT, PETTY_CASH
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int PerformedBy { get; set; }
    }

    public class DrawerTransactionItemDto
    {
        public int Id { get; set; }
        public int PosShiftId { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int PerformedBy { get; set; }
        public string PerformerName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class PosShiftSummaryDto
    {
        public int Id { get; set; }
        public string ShiftCode { get; set; } = string.Empty;
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public int CashierId { get; set; }
        public string CashierName { get; set; } = string.Empty;
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ExpectedCash { get; set; }
        public decimal ActualCash { get; set; }
        public decimal Difference { get; set; }
        public decimal CashSalesTotal { get; set; }
        public decimal CardSalesTotal { get; set; }
        public int TransactionCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
    }

    public class PosShiftDetailDto
    {
        public int Id { get; set; }
        public string ShiftCode { get; set; } = string.Empty;
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public int CashierId { get; set; }
        public string CashierName { get; set; } = string.Empty;
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ExpectedCash { get; set; }
        public decimal ActualCash { get; set; }
        public decimal Difference { get; set; }
        public decimal CashSalesTotal { get; set; }
        public decimal CardSalesTotal { get; set; }
        public int TransactionCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
        public List<DrawerTransactionItemDto> DrawerTransactions { get; set; } = new();
    }
}
