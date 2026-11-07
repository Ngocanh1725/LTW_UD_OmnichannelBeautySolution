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

    public class DepartmentTreeItemDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ParentId { get; set; }
        public string? ParentName { get; set; }
        public int? HeadUserId { get; set; }
        public string? HeadUserName { get; set; }
        public string? HeadFullName { get; set; }
        public string? HeadJobTitle { get; set; }
        public string? HeadAvatarUrl { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public int StaffCount { get; set; }
        public int TotalTreeStaffCount { get; set; }
        public List<DepartmentTreeItemDto> Children { get; set; } = new();
    }

    public class SaveDepartmentRequest
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ParentId { get; set; }
        public int? HeadUserId { get; set; }
        public int SortOrder { get; set; } = 1;
        public bool IsActive { get; set; } = true;
    }

    public class DepartmentIndexViewModel
    {
        public List<DepartmentTreeItemDto> TreeRoots { get; set; } = new();
        public List<DepartmentTreeItemDto> FlatDepartments { get; set; } = new();
        public List<UserSummaryDto> EligibleHeads { get; set; } = new();
        public bool CanManage { get; set; }
        public int TotalDepartments { get; set; }
        public int ActiveDepartments { get; set; }
        public int TotalStaffCount { get; set; }
    }

    public class SubAdminCardDto
    {
        public int UserId { get; set; }
        public string? EmployeeCode { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? JobTitle { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; }

        public int? PrimaryDepartmentId { get; set; }
        public string? PrimaryDepartmentName { get; set; }
        public List<RoleDto> Roles { get; set; } = new();

        public bool HasScope { get; set; }
        public bool CanCreateUser { get; set; }
        public bool CanEditUser { get; set; }
        public bool CanDeleteUser { get; set; }
        public bool CanResetPassword { get; set; }
        public bool CanAssignRole { get; set; }
        public bool CanDelegateFurther { get; set; }
        public int MaxGrantLevel { get; set; } = 50;

        public List<int> AllowedPermissionIds { get; set; } = new();
        public List<string> AllowedPermissionCodes { get; set; } = new();
        public List<int> AllowedDepartmentIds { get; set; } = new();
        public List<string> AllowedDepartmentNames { get; set; } = new();
        public List<int> AllowedStoreIds { get; set; } = new();
        public List<string> AllowedStoreNames { get; set; } = new();

        public int ManagedStaffCount { get; set; }
        public DateTime? ScopeUpdatedAt { get; set; }
    }

    public class StoreOptionDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsHeadquarters { get; set; }
    }

    public class DelegationHubViewModel
    {
        public List<SubAdminCardDto> SubAdmins { get; set; } = new();
        public List<ModulePermissionGroupDto> ModuleGroups { get; set; } = new();
        public List<DepartmentTreeItemDto> Departments { get; set; } = new();
        public List<StoreOptionDto> Stores { get; set; } = new();
        public List<RoleDto> SubAdminRoles { get; set; } = new();
        public List<UserSummaryDto> EligibleStaffForPromotion { get; set; } = new();
        public DelegationContextDto ActorContext { get; set; } = new();
    }

    public class CreateSubAdminRequest
    {
        public string Mode { get; set; } = "existing"; // "existing" | "new"
        public int? ExistingUserId { get; set; }

        public string? Username { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Password { get; set; }
        public string? JobTitle { get; set; }
        public int? PrimaryDepartmentId { get; set; }

        public int RoleId { get; set; }
        public SaveDelegationScopeRequest Scope { get; set; } = new();
    }

    public class DelegationAuditLogViewModel
    {
        public List<AuditLogDto> Logs { get; set; } = new();
        public List<UserSummaryDto> ScopedActors { get; set; } = new();
        public int? FilterActorId { get; set; }
        public string? FilterTargetKeyword { get; set; }
        public string? FilterActionType { get; set; }
        public DateTime? FilterFromDate { get; set; }
        public DateTime? FilterToDate { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
        public bool IsSuperAdmin { get; set; }
    }
}
