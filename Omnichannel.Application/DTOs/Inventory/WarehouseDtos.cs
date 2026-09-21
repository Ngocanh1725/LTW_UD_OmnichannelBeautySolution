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
        public IEnumerable<StoreLookupDto> AvailableStores { get; set; } = new List<StoreLookupDto>();
        public int TotalExpiredBatches { get; set; }
        public int TotalNearExpiryBatches { get; set; }
        public int TotalLowStockBatches { get; set; }
        public int TotalSKUs { get; set; }
        public ImportBatchViewModel NewBatchImport { get; set; } = new();
        public List<BatchStockItemViewModel> BatchStockItems { get; set; } = new();
        public List<BatchStockItemViewModel> Inventories { get; set; } = new();
    }

    public class BatchStockItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int PhysicalQuantity { get; set; }
        public int ReservedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
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
        public string BatchNumber { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime? ManufacturingDate { get; set; }
        public DateTime? ExpDate { get; set; }
        public string ReferenceDocNo { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public class StockAdjustmentViewModel
    {
        public string StoreId { get; set; } = string.Empty;
    }
}