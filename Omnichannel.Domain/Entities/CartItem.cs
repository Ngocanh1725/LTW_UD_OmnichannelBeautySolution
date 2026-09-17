using System;

namespace Omnichannel.Domain.Entities
{
    public class CartItem
    {
        public int CartItemId { get; set; }
        public string CartId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime AddedAt { get; set; }

        public virtual Cart Cart { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}