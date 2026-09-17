using System;
using System.Collections.Generic;

namespace Omnichannel.Domain.Entities
{
    public class Order
    {
        public string OrderId { get; set; } = string.Empty;
        public int OrderChannel { get; set; } // 1: POS Tại quầy, 2: Website, 3: Shopee, 4: TikTok
        public string StoreId { get; set; } = string.Empty;
        public string? CustomerId { get; set; }
        public string? CreatedByStaffId { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal FinalTotal { get; set; }
        public int PaymentMethod { get; set; } // 1: Tiền mặt, 2: VietQR, 3: POS Card, 4: COD
        public int PaymentStatus { get; set; } // 1: Pending, 2: Paid, 3: Refunded
        public int OrderStatus { get; set; } // 1: Created, 2: Reserved, 3: Packing, 4: Shipping, 5: Completed, 6: Cancelled
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual Store Store { get; set; } = null!;
        public virtual User? Customer { get; set; }
        public virtual User? Staff { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public virtual Invoice? Invoice { get; set; }
    }
}
