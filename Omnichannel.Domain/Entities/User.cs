using System;
using System.Collections.Generic;

namespace Omnichannel.Domain.Entities
{
    public class User
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string NormalizedUsername { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public string? SecurityStamp { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int UserType { get; set; } // 1: Customer, 2: SalesStaff, 3: WarehouseStaff, 4: Manager, 5: Admin
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
        public virtual ICollection<InventoryLog> InventoryLogs { get; set; } = new List<InventoryLog>();
        public virtual ICollection<Order> CustomerOrders { get; set; } = new List<Order>();
        public virtual ICollection<Order> StaffOrders { get; set; } = new List<Order>();
        public virtual ICollection<Invoice> IssuedInvoices { get; set; } = new List<Invoice>();
    }
}