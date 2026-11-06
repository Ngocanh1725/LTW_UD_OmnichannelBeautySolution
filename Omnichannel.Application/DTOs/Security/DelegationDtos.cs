using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Security
{
    public class DelegationContextDto
    {
        public int ActorId { get; set; }
        public string ActorUsername { get; set; } = string.Empty;
        public bool IsSuperAdmin { get; set; }
        public int HighestRoleLevel { get; set; }
        public int MaxGrantLevel { get; set; }

        public bool CanCreateUser { get; set; }
        public bool CanEditUser { get; set; }
        public bool CanDeleteUser { get; set; }
        public bool CanResetPassword { get; set; }
        public bool CanAssignRole { get; set; }
        public bool CanDelegateFurther { get; set; }

        public HashSet<string> EffectivePermissionCodes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public IReadOnlyList<int> GrantablePermissionIds { get; set; } = Array.Empty<int>();
        public IReadOnlyList<int> ManagedDepartmentIds { get; set; } = Array.Empty<int>();
        public IReadOnlyList<int> ManagedStoreIds { get; set; } = Array.Empty<int>();
    }

    public class SaveDelegationScopeRequest
    {
        public int AdminUserId { get; set; }
        public bool CanCreateUser { get; set; }
        public bool CanEditUser { get; set; }
        public bool CanDeleteUser { get; set; }
        public bool CanResetPassword { get; set; }
        public bool CanAssignRole { get; set; }
        public bool CanDelegateFurther { get; set; }
        public int MaxGrantLevel { get; set; } = 30;
        public List<int> PermissionIds { get; set; } = new();
        public List<int> DepartmentIds { get; set; } = new();
        public List<int> StoreIds { get; set; } = new();
    }
}

