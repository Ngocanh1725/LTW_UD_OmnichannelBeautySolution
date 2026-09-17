using System;

namespace Omnichannel.Domain.Entities
{
    public class RolePermission
    {
        public int RolePermissionId { get; set; }
        public string RoleId { get; set; } = string.Empty;
        public int PermissionId { get; set; }
        public bool IsGranted { get; set; }
        public string? GrantedBy { get; set; }
        public DateTime GrantedAt { get; set; }

        public virtual Role Role { get; set; } = null!;
        public virtual Permission Permission { get; set; } = null!;
        public virtual User? Granter { get; set; }
    }
}
