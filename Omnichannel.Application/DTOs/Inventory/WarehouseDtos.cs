using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Omnichannel.Application.DTOs.Inventory
{
    public class BatchStockItemViewModel
    {
        public int InventoryId { get; set; }
        public string StoreId { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime ManufacturingDate { get; set; }
        public DateTime ExpDate { get; set; }
        public int PhysicalQuantity { get; set; }
        public int ReservedQuantity { get; set; }
        public int AvailableQuantity => PhysicalQuantity - ReservedQuantity;
        public int SafetyStock { get; set; }
        public int DaysRemaining => (ExpDate.Date - DateTime.Today).Days;

        // Cờ cảnh báo màu sắc trực quan (Color-Coded Alert)
        public bool IsExpired => DaysRemaining <= 0;
        public bool IsNearExpiry => DaysRemaining > 0 && DaysRemaining <= 60;
        public bool IsLowStock => PhysicalQuantity < SafetyStock;
    }

    public class ImportBatchViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn Kho nhận hàng")]
        [Display(Name = "Kho lưu trữ")]
        public string StoreId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn sản phẩm")]
        [Display(Name = "Sản phẩm SKU")]
        public string ProductId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số hiệu Lô sản xuất không được để trống")]
        [StringLength(100, ErrorMessage = "Số Lô tối đa 100 ký tự")]
        [Display(Name = "Số hiệu Lô (Batch Number)")]
        public string BatchNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập Ngày sản xuất")]
        [Display(Name = "Ngày sản xuất (NSX)")]
        public DateTime ManufacturingDate { get; set; } = DateTime.Today.AddMonths(-1);

        [Required(ErrorMessage = "Vui lòng nhập Hạn sử dụng")]
        [Display(Name = "Hạn sử dụng (HSD)")]
        public DateTime ExpDate { get; set; } = DateTime.Today.AddYears(2);

        [Required(ErrorMessage = "Số lượng nhập không được để trống")]
        [Range(1, 1000000, ErrorMessage = "Số lượng nhập tối thiểu là 1")]
        [Display(Name = "Số lượng nhập thực tế")]
        public int Quantity { get; set; }

        [Display(Name = "Mã chứng từ PO / Hóa đơn")]
        public string? ReferenceDocNo { get; set; }

        [Display(Name = "Ghi chú nhập kho")]
        public string? Notes { get; set; }
    }

    public class StockAdjustmentViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn Kho")]
        public string StoreId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn Lô hàng")]
        public int BatchId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại điều chỉnh")]
        [Display(Name = "Loại điều chỉnh")]
        public int TransactionType { get; set; } // 4: Cân bằng kiểm kê, 5: Xuất hủy hàng hỏng/hết date

        [Required(ErrorMessage = "Vui lòng nhập số lượng điều chỉnh")]
        [Display(Name = "Số lượng chênh lệch")]
        public int QuantityChange { get; set; } // Âm nếu giảm, Dương nếu tăng

        [Required(ErrorMessage = "Lý do điều chỉnh không được để trống")]
        [StringLength(500, ErrorMessage = "Lý do tối đa 500 ký tự")]
        [Display(Name = "Lý do & Mã biên bản")]
        public string Notes { get; set; } = string.Empty;
    }

    public class AllocatedBatchDto
    {
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime ExpDate { get; set; }
        public int AllocatedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
    }

    public class WarehouseOverviewViewModel
    {
        public string SelectedStoreId { get; set; } = string.Empty;
        public string SelectedStoreName { get; set; } = string.Empty;
        public string? FilterStatus { get; set; }
        public List<StoreLookupDto> AvailableStores { get; set; } = new();
        public List<BatchStockItemViewModel> BatchStockItems { get; set; } = new();
        public int TotalSKUs { get; set; }
        public int TotalExpiredBatches { get; set; }
        public int TotalNearExpiryBatches { get; set; }
        public int TotalLowStockBatches { get; set; }
        public ImportBatchViewModel NewBatchImport { get; set; } = new();
    }

    public class StoreLookupDto
    {
        public string StoreId { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public bool IsCentralWarehouse { get; set; }
    }
}
