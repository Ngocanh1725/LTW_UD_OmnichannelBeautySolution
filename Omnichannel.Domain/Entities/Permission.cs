using System.Collections.Generic;

namespace Omnichannel.Domain.Entities
{
    public class Permission
    {
        public int PermissionId { get; set; }
        public string ModuleCode { get; set; } = string.Empty;
        public string ActionCode { get; set; } = string.Empty;
        public string PermissionName { get; set; } = string.Empty;
        public string? Description { get; set; }

        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    }
}

