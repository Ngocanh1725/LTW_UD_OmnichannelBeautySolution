using System;
using System.Collections.Generic;

namespace Omnichannel.Domain.Entities
{
    public class ProductBatch
    {
        public int BatchId { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime ManufacturingDate { get; set; }
        public DateTime ExpDate { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual Product Product { get; set; } = null!;
        public virtual ICollection<StoreInventory> StoreInventories { get; set; } = new List<StoreInventory>();
        public virtual ICollection<InventoryLog> InventoryLogs { get; set; } = new List<InventoryLog>();
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}