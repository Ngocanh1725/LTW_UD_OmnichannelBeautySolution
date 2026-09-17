using System;

namespace Omnichannel.Domain.Entities
{
    public class StoreInventory
    {
        public int InventoryId { get; set; }
        public string StoreId { get; set; } = string.Empty;
        public int BatchId { get; set; }
        public int PhysicalQuantity { get; set; }
        public int ReservedQuantity { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual Store Store { get; set; } = null!;
        public virtual ProductBatch Batch { get; set; } = null!;

        public int AvailableQuantity => PhysicalQuantity - ReservedQuantity;
    }
}