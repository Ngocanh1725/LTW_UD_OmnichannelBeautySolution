using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Security
{
    public class PermissionMatrixViewModel
    {
        public string SelectedRoleId { get; set; } = string.Empty;
        public string SelectedRoleName { get; set; } = string.Empty;
        public List<RoleDto> Roles { get; set; } = new();
        public List<ModulePermissionGroupDto> ModuleGroups { get; set; } = new();
        public HashSet<int> GrantedPermissionIds { get; set; } = new();
    }

    public class RoleDto
    {
        public string RoleId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class ModulePermissionGroupDto
    {
        public string ModuleCode { get; set; } = string.Empty;
        public string ModuleDisplayName { get; set; } = string.Empty;
        public string IconClass { get; set; } = "fa-solid fa-layer-group";
        public List<PermissionDetailDto> Permissions { get; set; } = new();
    }

    public class PermissionDetailDto
    {
        public int PermissionId { get; set; }
        public string ActionCode { get; set; } = string.Empty;
        public string PermissionName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UpdateRolePermissionsRequest
    {
        public string RoleId { get; set; } = string.Empty;
        public List<int> PermissionIds { get; set; } = new();
    }
}
