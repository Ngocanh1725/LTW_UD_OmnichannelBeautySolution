using System;
using System.Collections.Generic;

namespace Omnichannel.Domain.Entities
{
    public class Menu
    {
        public int MenuId { get; set; }
        public string MenuCode { get; set; } = string.Empty;
        public string MenuName { get; set; } = string.Empty;
        public int Position { get; set; } // 1: Header, 2: MegaMenu, 3: Footer, 4: Sidebar
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    }
}