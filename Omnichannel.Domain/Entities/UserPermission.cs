using System;

namespace Omnichannel.Domain.Entities
{
    public class UserPermission
    {
        public int UserPermissionId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int PermissionId { get; set; }
        public bool IsGranted { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime ModifiedAt { get; set; }

        public virtual User User { get; set; } = null!;
        public virtual Permission Permission { get; set; } = null!;
        public virtual User? Modifier { get; set; }
    }
}

