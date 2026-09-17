using System;
using System.Collections.Generic;

namespace Omnichannel.Domain.Entities
{
    public class Cart
    {
        public string CartId { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public DateTime LastActive { get; set; }

        public virtual User? User { get; set; }
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}