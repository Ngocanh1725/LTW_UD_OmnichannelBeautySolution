using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Security
{
    public class PermissionMatrixViewModel
    {
        public List<RoleDto> Roles { get; set; } = new();
        public List<UserSummaryDto> Users { get; set; } = new();
        public List<ModulePermissionGroupDto> ModuleGroups { get; set; } = new();
        public int SelectedRoleId { get; set; }
        public List<int> ActivePermissionIdsForRole { get; set; } = new();
    }

    public class RoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UserSummaryDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? StoreName { get; set; }
    }

    public class ModulePermissionGroupDto
    {
        public string ModuleCode { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
        public List<PermissionItemDto> Permissions { get; set; } = new();
    }

    public class PermissionItemDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // VIEW, CREATE, EDIT, DELETE, APPROVE, EXPORT
        public string? Description { get; set; }
    }

    public class RolePermissionUpdateDto
    {
        public int RoleId { get; set; }
        public List<int> PermissionIds { get; set; } = new();
    }

    public class UserOverrideDto
    {
        public int UserId { get; set; }
        public int PermissionId { get; set; }
        public bool? OverrideStatus { get; set; } // true = Granted, false = Revoked, null = Remove override
    }

    public class UserPermissionDetailDto
    {
        public int PermissionId { get; set; }
        public string PermissionCode { get; set; } = string.Empty;
        public string PermissionName { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public bool InheritedFromRole { get; set; }
        public bool? DirectOverride { get; set; } // true: Grant, false: Revoke, null: None
        public bool IsEffective { get; set; }
    }
}