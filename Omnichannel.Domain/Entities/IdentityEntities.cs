using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Omnichannel.Domain.Entities
{
    [Table("Departments")]
    public class Department
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        [Column(TypeName = "varchar(30)")]
        public string Code { get; set; } = string.Empty; // e.g., BAN_GIAM_DOC, PHONG_SALE

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public int? ParentId { get; set; }

        public int? HeadUserId { get; set; }

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; } = 0;

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual Department? Parent { get; set; }
        public virtual ICollection<Department> Children { get; set; } = new List<Department>();
        public virtual User? HeadUser { get; set; }
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
        public virtual ICollection<AdminDelegationDepartment> DelegationDepartments { get; set; } = new List<AdminDelegationDepartment>();
    }

    [Table("Roles")]
    public class Role
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? DisplayName { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }

        public int? DepartmentId { get; set; }

        /// <summary>
        /// Cấp bậc vai trò: 100 = SuperAdmin, 50 = Admin nhỏ / Trưởng phòng, 30 = Quản lý, 10 = Nhân viên, 0 = Khách hàng
        /// </summary>
        public int Level { get; set; } = 10;

        /// <summary>
        /// Role cốt lõi của hệ thống không được phép xóa hoặc đổi mã Name
        /// </summary>
        public bool IsSystem { get; set; } = false;

        [MaxLength(30)]
        [Column(TypeName = "varchar(30)")]
        public string? Color { get; set; }

        public bool IsActive { get; set; } = true;

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual Department? Department { get; set; }
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }

    [Table("Permissions")]
    public class Permission
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string Code { get; set; } = string.Empty; // e.g., PRODUCT_VIEW, POS_CHECKOUT

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string Module { get; set; } = string.Empty; // e.g., INVENTORY, POS, CMS, SECURITY, HR

        [MaxLength(255)]
        public string? Description { get; set; }

        // Navigation properties
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
        public virtual ICollection<AdminDelegationPermission> DelegationPermissions { get; set; } = new List<AdminDelegationPermission>();
    }

    [Table("Users")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string Username { get; set; } = string.Empty;

        [MaxLength(30)]
        [Column(TypeName = "varchar(30)")]
        public string? EmployeeCode { get; set; }

        [Required]
        [MaxLength(150)]
        [EmailAddress]
        [Column(TypeName = "varchar(150)")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        [Column(TypeName = "varchar(500)")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string? PhoneNumber { get; set; }

        [MaxLength(500)]
        [Column(TypeName = "varchar(500)")]
        public string? AvatarUrl { get; set; }

        public int? AvatarMediaId { get; set; }

        [MaxLength(50)]
        public string? NormalizedUsername { get; set; }

        [MaxLength(100)]
        public string? SecurityStamp { get; set; }

        /// <summary>
        /// 1 = Khách hàng (Customer), 2 = Nhân sự / Quản trị viên nội bộ (Staff/Admin)
        /// </summary>
        public int UserType { get; set; } = 1;

        public bool IsActive { get; set; } = true;

        // Gán chi nhánh làm việc (Áp dụng cho nhân viên/quản lý cửa hàng)
        public int? StoreId { get; set; }

        // Gán phòng ban và người quản lý trực tiếp (Áp dụng cho nhân sự nội bộ UserType = 2)
        public int? DepartmentId { get; set; }

        public int? ManagerId { get; set; }

        [MaxLength(100)]
        public string? JobTitle { get; set; }

        [MaxLength(20)]
        public string? Gender { get; set; }

        [Column(TypeName = "date")]
        public DateTime? DateOfBirth { get; set; }

        [Column(TypeName = "date")]
        public DateTime? HireDate { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime? LastLoginAt { get; set; }

        // Xóa mềm (Soft Delete)
        public bool IsDeleted { get; set; } = false;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? DeletedAt { get; set; }

        public int? DeletedBy { get; set; }

        public int? CreatedBy { get; set; }

        public int? UpdatedBy { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual Store? Store { get; set; }
        public virtual Department? Department { get; set; }
        public virtual User? Manager { get; set; }
        public virtual ICollection<User> Subordinates { get; set; } = new List<User>();
        public virtual AdminDelegationScope? DelegationScope { get; set; }
        public virtual MediaAsset? AvatarMedia { get; set; }
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<InventoryLog> InventoryLogs { get; set; } = new List<InventoryLog>();
        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
        public virtual CustomerProfile? CustomerProfile { get; set; }
        public virtual LoyaltyWallet? LoyaltyWallet { get; set; }
        public virtual ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();
        public virtual ICollection<CouponUsage> CouponUsages { get; set; } = new List<CouponUsage>();
    }

    [Table("UserRoles")]
    public class UserRole
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }

        public int? AssignedByUserId { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public virtual User User { get; set; } = null!;
        public virtual Role Role { get; set; } = null!;
        public virtual User? AssignedByUser { get; set; }
    }

    [Table("RolePermissions")]
    public class RolePermission
    {
        public int RoleId { get; set; }
        public int PermissionId { get; set; }

        public virtual Role Role { get; set; } = null!;
        public virtual Permission Permission { get; set; } = null!;
    }

    [Table("UserPermissions")]
    public class UserPermission
    {
        public int UserId { get; set; }
        public int PermissionId { get; set; }
        public bool IsGranted { get; set; } = true; // Cho phép override phân quyền trực tiếp

        public int? GrantedByUserId { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(255)]
        public string? Reason { get; set; }

        public virtual User User { get; set; } = null!;
        public virtual Permission Permission { get; set; } = null!;
        public virtual User? GrantedByUser { get; set; }
    }

    [Table("AdminDelegationScopes")]
    public class AdminDelegationScope
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int AdminUserId { get; set; }

        public bool CanCreateUser { get; set; } = false;
        public bool CanEditUser { get; set; } = false;
        public bool CanDeleteUser { get; set; } = false;
        public bool CanResetPassword { get; set; } = false;
        public bool CanAssignRole { get; set; } = false;
        public bool CanDelegateFurther { get; set; } = false;

        /// <summary>
        /// Chỉ cho phép gán các Role có Level &lt;= MaxGrantLevel (và &lt; Level của người thao tác)
        /// </summary>
        public int MaxGrantLevel { get; set; } = 30;

        public int? GrantedByUserId { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual User AdminUser { get; set; } = null!;
        public virtual User? GrantedByUser { get; set; }
        public virtual ICollection<AdminDelegationPermission> AllowedPermissions { get; set; } = new List<AdminDelegationPermission>();
        public virtual ICollection<AdminDelegationDepartment> AllowedDepartments { get; set; } = new List<AdminDelegationDepartment>();
        public virtual ICollection<AdminDelegationStore> AllowedStores { get; set; } = new List<AdminDelegationStore>();
    }

    [Table("AdminDelegationPermissions")]
    public class AdminDelegationPermission
    {
        public int AdminDelegationScopeId { get; set; }
        public int PermissionId { get; set; }

        public virtual AdminDelegationScope Scope { get; set; } = null!;
        public virtual Permission Permission { get; set; } = null!;
    }

    [Table("AdminDelegationDepartments")]
    public class AdminDelegationDepartment
    {
        public int AdminDelegationScopeId { get; set; }
        public int DepartmentId { get; set; }

        public virtual AdminDelegationScope Scope { get; set; } = null!;
        public virtual Department Department { get; set; } = null!;
    }

    [Table("AdminDelegationStores")]
    public class AdminDelegationStore
    {
        public int AdminDelegationScopeId { get; set; }
        public int StoreId { get; set; }

        public virtual AdminDelegationScope Scope { get; set; } = null!;
        public virtual Store Store { get; set; } = null!;
    }
}
