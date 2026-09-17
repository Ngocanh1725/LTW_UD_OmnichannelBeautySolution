using System;

namespace Omnichannel.Domain.Entities
{
    public class InventoryLog
    {
        public long LogId { get; set; }
        public string StoreId { get; set; } = string.Empty;
        public int BatchId { get; set; }
        public int TransactionType { get; set; } // 1: Nhập NCC, 2: Bán POS, 3: Bán Online, 4: Cân bằng kiểm kê, 5: Xuất hủy
        public string ReferenceDocNo { get; set; } = string.Empty;
        public int QuantityChange { get; set; }
        public int RemainingQuantity { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? Notes { get; set; }

        public virtual Store Store { get; set; } = null!;
        public virtual ProductBatch Batch { get; set; } = null!;
        public virtual User Creator { get; set; } = null!;
    }
}