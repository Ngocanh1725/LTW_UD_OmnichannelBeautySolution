using System;

namespace Omnichannel.Application.DTOs.Inventory
{
    public class AllocatedBatchDto
    {
        public int BatchId { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public DateTime ExpDate { get; set; }
        public int AllocatedQuantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class WarehouseOverviewViewModel
    {
        public decimal TotalStockValue { get; set; }
        public int TotalProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int ExpiredProducts { get; set; }
        public string SelectedStoreName { get; set; } = string.Empty;
        public string SelectedStoreId { get; set; } = string.Empty;
        public string FilterStatus { get; set; } = string.Empty;
        public string Keyword { get; set; } = string.Empty;
        public IEnumerable<StoreLookupDto> AvailableStores { get; set; } = new List<StoreLookupDto>();
        public int TotalExpiredBatches { get; set; }
        public int TotalNearExpiryBatches { get; set; }
        public int TotalLowStockBatches { get; set; }
        public int TotalSafeBatches { get; set; }
        public int FefoCriticalCount { get; set; } // Hạn dùng <= 30 ngày (Đỏ - Khẩn cấp)
        public int FefoWarningCount { get; set; }  // Hạn dùng 31 - 60 ngày (Vàng - Cảnh báo)
        public int FefoSafeCount { get; set; }     // Hạn dùng > 60 ngày (Xanh - An toàn)
        public int TotalSKUs { get; set; }
        public ImportBatchViewModel NewBatchImport { get; set; } = new();
        public List<BatchStockItemViewModel> BatchStockItems { get; set; } = new();
        public List<StockTransferDto> RecentTransfers { get; set; } = new();
        public List<InventoryLogItemDto> InventoryLogs { get; set; } = new();
    }

    public class BatchStockItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSku { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public int InitialQuantity { get; set; }
        public int CurrentQuantity { get; set; }
        public int PhysicalQuantity { get; set; }
        public int ReservedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public decimal ImportCost { get; set; }
        public decimal SellingPrice { get; set; }
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpDate { get; set; }
        public string ReferenceDocNo { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public bool IsExpired { get; set; }
        public bool IsNearExpiry { get; set; }
        public int DaysRemaining { get; set; }
        public int InventoryId { get; set; }
        public string StoreId { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public int SafetyStock { get; set; }
        public double SoldRatioPercent { get; set; }
        public int AlertDaysThreshold { get; set; } = 90;
        public decimal? ClearanceDiscountPercent { get; set; }
        public string FefoBadgeClass { get; set; } = "badge-fefo-fresh";
        public string FefoBadgeText { get; set; } = "Lô Tươi Mới (> 18T)";
        public string FefoStatusTier { get; set; } = "SAFE"; // CRITICAL (<= 30d), WARNING (<= 60d), SAFE (> 60d)
        public string FefoStatusColor { get; set; } = "success"; // danger, warning, success
    }

    public class StoreLookupDto
    {
        public string StoreId { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public bool IsCentralWarehouse { get; set; }
    }

    public class ImportBatchViewModel
    {
        public string StoreId { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public int? SupplierId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal ImportCost { get; set; }
        public DateTime ManufacturingDate { get; set; } = DateTime.Today;
        public DateTime ExpDate { get; set; } = DateTime.Today.AddMonths(24);
        public string? ReferenceDocNo { get; set; }
        public string? Notes { get; set; }
    }

    public class StockAdjustmentViewModel
    {
        public string StoreId { get; set; } = string.Empty;
        public int BatchId { get; set; }
        public string TransactionType { get; set; } = "4"; // 4: BALANCE, 5: DISPOSE
        public int QuantityChange { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class DisposeStockViewModel
    {
        public string StoreId { get; set; } = string.Empty;
        public int BatchId { get; set; }
        public int Quantity { get; set; }
        public string Reason { get; set; } = "EXPIRED"; // EXPIRED, DAMAGED, RETURN_SUPPLIER, AUDIT_LOSS
        public string? Notes { get; set; }
    }

    public class InventoryLogItemDto
    {
        public int Id { get; set; }
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSku { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime? ExpDate { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public int QuantityChanged { get; set; }
        public int QuantityBefore { get; set; }
        public int QuantityAfter { get; set; }
        public string? Note { get; set; }
        public string PerformerName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}