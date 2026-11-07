using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Security
{
    public class PermissionMatrixViewModel
    {
        public List<RoleDto> Roles { get; set; } = new();
        public List<RoleDto> AssignableRoles { get; set; } = new();
        public List<UserSummaryDto> Users { get; set; } = new();
        public List<ModulePermissionGroupDto> ModuleGroups { get; set; } = new();
        public int SelectedRoleId { get; set; }
        public int SelectedUserId { get; set; }
        public List<int> ActivePermissionIdsForRole { get; set; } = new();
        public DelegationContextDto? ActorContext { get; set; }
    }

    public class RoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public int Level { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public bool IsSystem { get; set; }
        public string? Color { get; set; }
        public int PermissionCount { get; set; }
    }

    public class UserSummaryDto
    {
        public int Id { get; set; }
        public string? EmployeeCode { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? StoreName { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string? JobTitle { get; set; }
        public List<string> RoleNames { get; set; } = new();
        public List<int> RoleIds { get; set; } = new();
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
        public string Action { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsGrantableByActor { get; set; } = true;
    }

    public class RolePermissionUpdateDto
    {
        public int RoleId { get; set; }
        public List<int> PermissionIds { get; set; } = new();
    }

    public class BulkRolePermissionUpdateDto
    {
        public List<RolePermissionUpdateDto> Roles { get; set; } = new();
    }

    public class UserOverrideDto
    {
        public int UserId { get; set; }
        public int PermissionId { get; set; }
        public bool? OverrideStatus { get; set; } // true = Granted, false = Revoked, null = Remove override
        public string? Reason { get; set; }
    }

    public class ResetUserOverridesDto
    {
        public int UserId { get; set; }
    }

    public class AssignUserRolesRequest
    {
        public int UserId { get; set; }
        public List<int> RoleIds { get; set; } = new();
    }

    public class UserPermissionDetailDto
    {
        public int PermissionId { get; set; }
        public string PermissionCode { get; set; } = string.Empty;
        public string PermissionName { get; set; } = string.Empty;
        public string? PermissionDescription { get; set; }
        public string Module { get; set; } = string.Empty;
        public string ModuleDisplayName { get; set; } = string.Empty;
        public bool InheritedFromRole { get; set; }
        public bool? DirectOverride { get; set; } // true: Grant, false: Revoke, null: None
        public bool IsEffective { get; set; }
        public bool CanActorGrant { get; set; } = true;
        public int? GrantedByUserId { get; set; }
        public string? GrantedByName { get; set; }
        public DateTime? GrantedAt { get; set; }
        public string? Reason { get; set; }
    }

    public class UserPermissionPageViewModel
    {
        public int TargetUserId { get; set; }
        public string? EmployeeCode { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public string? DepartmentName { get; set; }
        public string? ManagerName { get; set; }
        public bool IsActive { get; set; }
        public List<int> CurrentRoleIds { get; set; } = new();
        public List<RoleDto> CurrentRoles { get; set; } = new();
        public List<RoleDto> AssignableRoles { get; set; } = new();
        public bool CanActorAssignRoles { get; set; }
        public List<UserPermissionDetailDto> Permissions { get; set; } = new();
        public DelegationContextDto ActorContext { get; set; } = new();
    }
}
