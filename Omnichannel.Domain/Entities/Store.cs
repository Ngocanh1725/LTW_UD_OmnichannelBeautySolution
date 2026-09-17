using System.Collections.Generic;

namespace Omnichannel.Domain.Entities
{
    public class Store
    {
        public string StoreId { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool IsCentralWarehouse { get; set; }
        public bool IsActive { get; set; }

        public virtual ICollection<StoreInventory> StoreInventories { get; set; } = new List<StoreInventory>();
        public virtual ICollection<InventoryLog> InventoryLogs { get; set; } = new List<InventoryLog>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}