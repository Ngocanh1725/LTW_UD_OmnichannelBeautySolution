using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Omnichannel.Application.DTOs.Security;

namespace Omnichannel.Application.DTOs.HR
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalItems { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalItems / (double)Math.Max(1, PageSize)));
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
        public int FromItem => TotalItems == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int ToItem => Math.Min(Page * PageSize, TotalItems);
    }

    public class StaffFilterQuery
    {
        public string? Keyword { get; set; }
        public int? DepartmentId { get; set; }
        public int? RoleId { get; set; }
        public int? StoreId { get; set; }
        public int? ManagerId { get; set; }
        public string? Status { get; set; } = "all"; // all, active, locked
        public string? Gender { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public DateTime? HireFrom { get; set; }
        public DateTime? HireTo { get; set; }
        public string? SortBy { get; set; } = "createdAt"; // name, createdAt, hireDate, lastLogin, department, code
        public string? SortDir { get; set; } = "desc"; // asc, desc
        public string ViewMode { get; set; } = "table"; // table, card, department
        public string? Ids { get; set; } // comma-separated IDs for selected export
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class StaffRoleBadgeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Color { get; set; } = "slate";
        public int Level { get; set; }
    }

    public class StaffListItemDto
    {
        public int Id { get; set; }
        public string? EmployeeCode { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string? JobTitle { get; set; }
        public int? StoreId { get; set; }
        public string? StoreName { get; set; }
        public int? ManagerId { get; set; }
        public string? ManagerName { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime? HireDate { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedByName { get; set; }
        public bool CanActorManage { get; set; }
        public bool CanActorEdit { get; set; }
        public bool CanActorDelete { get; set; }
        public bool CanActorResetPassword { get; set; }
        public bool CanActorDelegate { get; set; }
        public bool IsDepartmentHead { get; set; }
        public List<StaffRoleBadgeDto> Roles { get; set; } = new();
    }

    public class StaffStatsDto
    {
        public int TotalStaff { get; set; }
        public int ActiveStaff { get; set; }
        public int LockedStaff { get; set; }
        public int NewStaffLast30Days { get; set; }
        public int TrashStaff { get; set; }
        public int DepartmentCount { get; set; }
        public int AdminCount { get; set; }
        public int TotalCustomers { get; set; }
        public Dictionary<string, int> StaffByDepartment { get; set; } = new();
        public Dictionary<string, int> StaffByRole { get; set; } = new();
    }

    public class CreateStaffRequest : IValidatableObject
    {
        [Required(ErrorMessage = "Vui lòng nhập Tên đăng nhập.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3 đến 50 ký tự.")]
        [RegularExpression(@"^[a-z0-9._]+$", ErrorMessage = "Tên đăng nhập chỉ chấp nhận chữ thường không dấu, số, dấu gạch dưới hoặc dấu chấm.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập Họ và tên.")]
        [StringLength(100, ErrorMessage = "Họ và tên tối đa 100 ký tự.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ Email.")]
        [EmailAddress(ErrorMessage = "Địa chỉ Email không đúng định dạng hợp lệ.")]
        [StringLength(150, ErrorMessage = "Email tối đa 150 ký tự.")]
        public string Email { get; set; } = string.Empty;

        [RegularExpression(@"^(0[3|5|7|8|9])[0-9]{8}$", ErrorMessage = "Số điện thoại di động Việt Nam phải gồm 10 chữ số bắt đầu bằng 03, 05, 07, 08 hoặc 09.")]
        public string? PhoneNumber { get; set; }

        [StringLength(30, ErrorMessage = "Mã nhân viên tối đa 30 ký tự.")]
        public string? EmployeeCode { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Mật khẩu ban đầu.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            ErrorMessage = "Mật khẩu phải chứa ít nhất 1 chữ hoa, 1 chữ thường, 1 chữ số và 1 ký tự đặc biệt.")]
        public string Password { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }
        public int? AvatarMediaId { get; set; }
        public int? DepartmentId { get; set; }
        public string? JobTitle { get; set; }
        public int? StoreId { get; set; }
        public int? ManagerId { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime? HireDate { get; set; }
        public string? Address { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public List<int> RoleIds { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DateOfBirth.HasValue)
            {
                var age = DateTime.Today.Year - DateOfBirth.Value.Year;
                if (DateOfBirth.Value.Date > DateTime.Today.AddYears(-age)) age--;
                if (age < 16)
                {
                    yield return new ValidationResult("Nhân sự phải từ đủ 16 tuổi trở lên theo quy định pháp luật lao động.", new[] { nameof(DateOfBirth) });
                }
            }

            if (HireDate.HasValue && HireDate.Value.Date > DateTime.Today)
            {
                yield return new ValidationResult("Ngày bắt đầu làm việc (HireDate) không được lớn hơn ngày hiện tại.", new[] { nameof(HireDate) });
            }
        }
    }

    public class UpdateStaffRequest : IValidatableObject
    {
        [Required(ErrorMessage = "Vui lòng nhập Họ và tên.")]
        [StringLength(100, ErrorMessage = "Họ và tên tối đa 100 ký tự.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ Email.")]
        [EmailAddress(ErrorMessage = "Địa chỉ Email không đúng định dạng hợp lệ.")]
        [StringLength(150, ErrorMessage = "Email tối đa 150 ký tự.")]
        public string Email { get; set; } = string.Empty;

        [RegularExpression(@"^(0[3|5|7|8|9])[0-9]{8}$", ErrorMessage = "Số điện thoại di động Việt Nam phải gồm 10 chữ số bắt đầu bằng 03, 05, 07, 08 hoặc 09.")]
        public string? PhoneNumber { get; set; }

        [StringLength(30, ErrorMessage = "Mã nhân viên tối đa 30 ký tự.")]
        public string? EmployeeCode { get; set; }

        public string? AvatarUrl { get; set; }
        public int? AvatarMediaId { get; set; }
        public int? DepartmentId { get; set; }
        public string? JobTitle { get; set; }
        public int? StoreId { get; set; }
        public int? ManagerId { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime? HireDate { get; set; }
        public string? Address { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public List<int> RoleIds { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DateOfBirth.HasValue)
            {
                var age = DateTime.Today.Year - DateOfBirth.Value.Year;
                if (DateOfBirth.Value.Date > DateTime.Today.AddYears(-age)) age--;
                if (age < 16)
                {
                    yield return new ValidationResult("Nhân sự phải từ đủ 16 tuổi trở lên.", new[] { nameof(DateOfBirth) });
                }
            }

            if (HireDate.HasValue && HireDate.Value.Date > DateTime.Today)
            {
                yield return new ValidationResult("Ngày bắt đầu làm việc không được lớn hơn ngày hiện tại.", new[] { nameof(HireDate) });
            }
        }
    }

    public class StaffOrderSummaryDto
    {
        public int Id { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal FinalAmount { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
    }

    public class StaffDetailDto : StaffListItemDto
    {
        public int? AvatarMediaId { get; set; }
        public string? Address { get; set; }
        public string? Notes { get; set; }
        public int SubordinatesCount { get; set; }
        public string? HeadOfDepartmentName { get; set; }
        public List<UserPermissionDetailDto> EffectivePermissions { get; set; } = new();
        public List<AuditLogDto> RecentAuditLogs { get; set; } = new();
        public List<StaffListItemDto> Subordinates { get; set; } = new();
        public List<StaffOrderSummaryDto> HandledOrders { get; set; } = new();
    }

    public class BulkStaffActionRequest
    {
        [Required]
        public string Action { get; set; } = string.Empty; // lock, unlock, delete, assignDepartment, assignRole
        public List<int> StaffIds { get; set; } = new();
        public int? TargetDepartmentId { get; set; }
        public int? TargetRoleId { get; set; }
        public string? Reason { get; set; }
    }

    public class BulkStaffActionResult
    {
        public int TotalRequested { get; set; }
        public int SucceededCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> Messages { get; set; } = new();
    }

    public class ResetPasswordRequest
    {
        public int StaffId { get; set; }
        public string? NewPassword { get; set; }
        public bool RequireChangeOnLogin { get; set; } = true;
    }

    public class CustomerUserFilterQuery
    {
        public string? Keyword { get; set; }
        public string? Status { get; set; } = "all"; // all, active, locked
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class CustomerUserListItemDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public string? MemberTierName { get; set; }
        public string? MemberTierBadgeColor { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public string? StoreName { get; set; }
    }

    public class StaffIndexViewModel
    {
        public PagedResult<StaffListItemDto> StaffPaged { get; set; } = new();
        public StaffStatsDto Stats { get; set; } = new();
        public StaffFilterQuery Filter { get; set; } = new();
        public List<DepartmentOptionDto> ScopedDepartments { get; set; } = new();
        public List<RoleDto> AssignableRoles { get; set; } = new();
        public List<StoreOptionDto> ScopedStores { get; set; } = new();
        public List<UserSummaryDto> PotentialManagers { get; set; } = new();
        public DelegationContextDto ActorContext { get; set; } = new();
    }

    public class DepartmentOptionDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public int Depth { get; set; }
        public string DisplayTreeName { get; set; } = string.Empty;
    }

    public class StaffTrashViewModel
    {
        public PagedResult<StaffListItemDto> TrashPaged { get; set; } = new();
        public StaffFilterQuery Filter { get; set; } = new();
        public List<DepartmentOptionDto> ScopedDepartments { get; set; } = new();
        public DelegationContextDto ActorContext { get; set; } = new();
    }

    public class CustomerUserIndexViewModel
    {
        public PagedResult<CustomerUserListItemDto> CustomersPaged { get; set; } = new();
        public CustomerUserFilterQuery Filter { get; set; } = new();
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int LockedCustomers { get; set; }
        public int TotalInternalStaff { get; set; }
    }

    public class StaffFormViewModel
    {
        public bool IsEditMode { get; set; }
        public int StaffId { get; set; }
        public CreateStaffRequest CreateRequest { get; set; } = new();
        public UpdateStaffRequest UpdateRequest { get; set; } = new();
        public string Username { get; set; } = string.Empty;
        public bool SaveAndContinue { get; set; }
        public List<DepartmentOptionDto> ScopedDepartments { get; set; } = new();
        public List<RoleDto> AssignableRoles { get; set; } = new();
        public List<StoreOptionDto> ScopedStores { get; set; } = new();
        public List<UserSummaryDto> PotentialManagers { get; set; } = new();
        public DelegationContextDto ActorContext { get; set; } = new();
    }
}
