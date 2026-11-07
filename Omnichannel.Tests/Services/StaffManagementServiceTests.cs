using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Omnichannel.Application.DTOs.HR;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Security;
using Omnichannel.Infrastructure.Services;
using Omnichannel.Infrastructure.Services.HR;
using Omnichannel.Tests.Helpers;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class StaffManagementServiceTests
    {
        private class TestHarness
        {
            public ApplicationDbContext Context { get; set; } = null!;
            public StaffManagementService Service { get; set; } = null!;
            public DelegationService DelegationService { get; set; } = null!;
            public User SuperAdmin { get; set; } = null!;
            public User SalesAdmin { get; set; } = null!; // ad4 (Phòng Sale)
            public User UiAdmin { get; set; } = null!;    // ad1 (Phòng CMS)
            public User SalesStaff1 { get; set; } = null!;
            public User SalesStaff2 { get; set; } = null!;
            public User CmsStaff { get; set; } = null!;
            public Department DeptSales { get; set; } = null!;
            public Department DeptCms { get; set; } = null!;
            public Role RoleSalesStaff { get; set; } = null!;
            public Role RoleContentEditor { get; set; } = null!;
        }

        private static async Task<TestHarness> CreateHarnessAsync()
        {
            var context = TestDbContextFactory.Create();
            var cache = new MemoryCache(new MemoryCacheOptions());
            var permService = new PermissionService(context, cache);
            var auditService = new AuditLogService(context, NullLogger<AuditLogService>.Instance);
            var delegationService = new DelegationService(context, permService, auditService);
            var passwordHasher = new PasswordHasherService();
            var staffService = new StaffManagementService(context, delegationService, permService, passwordHasher, auditService);

            var deptBoard = new Department { Id = 1, Code = "BAN_GIAM_DOC", Name = "Ban Giám Đốc", IsActive = true, SortOrder = 1 };
            var deptSales = new Department { Id = 2, Code = "PHONG_SALE", Name = "Phòng Kinh Doanh", ParentId = 1, IsActive = true, SortOrder = 2 };
            var deptCms = new Department { Id = 3, Code = "PHONG_GIAO_DIEN_CMS", Name = "Phòng Giao Diện CMS", ParentId = 1, IsActive = true, SortOrder = 3 };
            context.Departments.AddRange(deptBoard, deptSales, deptCms);

            var rSuper = new Role { Id = 1, Name = "SuperAdmin", DisplayName = "Admin Tổng", Level = 100, DepartmentId = 1, IsActive = true, IsSystem = true };
            var rSalesAdmin = new Role { Id = 2, Name = "SalesAdmin", DisplayName = "Giám Đốc Sale", Level = 50, DepartmentId = 2, IsActive = true, IsSystem = true };
            var rUiAdmin = new Role { Id = 3, Name = "UiAdmin", DisplayName = "Admin Giao Diện", Level = 50, DepartmentId = 3, IsActive = true, IsSystem = true };
            var rSalesStaff = new Role { Id = 4, Name = "SalesStaff", DisplayName = "Nhân Viên Sale", Level = 10, DepartmentId = 2, IsActive = true, IsSystem = true };
            var rContentEditor = new Role { Id = 5, Name = "ContentEditor", DisplayName = "Biên Tập Viên", Level = 10, DepartmentId = 3, IsActive = true, IsSystem = true };
            context.Roles.AddRange(rSuper, rSalesAdmin, rUiAdmin, rSalesStaff, rContentEditor);

            var pUserView = new Permission { Id = 1, Code = "USER_VIEW", Name = "Xem nhân sự", Module = "USER" };
            var pUserCreate = new Permission { Id = 2, Code = "USER_CREATE", Name = "Tạo nhân sự", Module = "USER" };
            var pUserEdit = new Permission { Id = 3, Code = "USER_EDIT", Name = "Sửa nhân sự", Module = "USER" };
            var pUserDelete = new Permission { Id = 4, Code = "USER_DELETE", Name = "Xóa nhân sự", Module = "USER" };
            context.Permissions.AddRange(pUserView, pUserCreate, pUserEdit, pUserDelete);

            context.RolePermissions.AddRange(
                new RolePermission { RoleId = 1, PermissionId = 1 },
                new RolePermission { RoleId = 1, PermissionId = 2 },
                new RolePermission { RoleId = 1, PermissionId = 3 },
                new RolePermission { RoleId = 1, PermissionId = 4 },
                new RolePermission { RoleId = 2, PermissionId = 1 },
                new RolePermission { RoleId = 2, PermissionId = 2 },
                new RolePermission { RoleId = 2, PermissionId = 3 },
                new RolePermission { RoleId = 3, PermissionId = 1 }
            );

            var uSuper = new User { Id = 1, Username = "superadmin", FullName = "Admin Tổng", Email = "sa@canmake.vn", PasswordHash = "hash", UserType = 2, DepartmentId = 1, IsActive = true, CreatedAt = DateTime.UtcNow.AddMonths(-5) };
            var uSalesAdmin = new User { Id = 2, Username = "ad4", FullName = "Phạm Khánh Linh", Email = "ad4@canmake.vn", PasswordHash = "hash", UserType = 2, DepartmentId = 2, ManagerId = 1, IsActive = true, CreatedAt = DateTime.UtcNow.AddMonths(-4) };
            var uUiAdmin = new User { Id = 3, Username = "ad1", FullName = "Nguyễn Minh Châu", Email = "ad1@canmake.vn", PasswordHash = "hash", UserType = 2, DepartmentId = 3, ManagerId = 1, IsActive = true, CreatedAt = DateTime.UtcNow.AddMonths(-4) };
            var uSale1 = new User { Id = 4, Username = "sale01", FullName = "Vũ Hải Đăng", Email = "sale01@canmake.vn", PhoneNumber = "0908200001", EmployeeCode = "NV0001", PasswordHash = "hash", UserType = 2, DepartmentId = 2, ManagerId = 2, IsActive = true, CreatedAt = DateTime.UtcNow.AddMonths(-2), HireDate = new DateTime(2025, 1, 15) };
            var uSale2 = new User { Id = 5, Username = "sale02", FullName = "Đỗ Mai Phương", Email = "sale02@canmake.vn", PhoneNumber = "0908200002", EmployeeCode = "NV0002", PasswordHash = "hash", UserType = 2, DepartmentId = 2, ManagerId = 2, IsActive = false, CreatedAt = DateTime.UtcNow.AddMonths(-1), HireDate = new DateTime(2025, 2, 1) }; // Locked
            var uCms = new User { Id = 6, Username = "editor01", FullName = "Lê Hoàng Yến", Email = "editor01@canmake.vn", PhoneNumber = "0908200003", EmployeeCode = "NV0003", PasswordHash = "hash", UserType = 2, DepartmentId = 3, ManagerId = 3, IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-10), HireDate = new DateTime(2025, 3, 1) };
            var uCust = new User { Id = 7, Username = "customer01", FullName = "Khách Hàng Mẫu", Email = "cust@gmail.com", PasswordHash = "hash", UserType = 1, IsActive = true, CreatedAt = DateTime.UtcNow };

            context.Users.AddRange(uSuper, uSalesAdmin, uUiAdmin, uSale1, uSale2, uCms, uCust);

            context.UserRoles.AddRange(
                new UserRole { UserId = 1, RoleId = 1 },
                new UserRole { UserId = 2, RoleId = 2 },
                new UserRole { UserId = 3, RoleId = 3 },
                new UserRole { UserId = 4, RoleId = 4 },
                new UserRole { UserId = 5, RoleId = 4 },
                new UserRole { UserId = 6, RoleId = 5 }
            );

            // Scope cho SalesAdmin (ad4): Quản lý Phòng Sale (Id = 2)
            var scopeAd4 = new AdminDelegationScope
            {
                Id = 1,
                AdminUserId = 2,
                CanCreateUser = true,
                CanEditUser = true,
                CanDeleteUser = true,
                CanResetPassword = true,
                CanAssignRole = true,
                MaxGrantLevel = 50,
                AllowedDepartments = new List<AdminDelegationDepartment>
                {
                    new AdminDelegationDepartment { AdminDelegationScopeId = 1, DepartmentId = 2 }
                },
                AllowedPermissions = new List<AdminDelegationPermission>
                {
                    new AdminDelegationPermission { AdminDelegationScopeId = 1, PermissionId = 1 },
                    new AdminDelegationPermission { AdminDelegationScopeId = 1, PermissionId = 2 },
                    new AdminDelegationPermission { AdminDelegationScopeId = 1, PermissionId = 3 }
                }
            };
            context.AdminDelegationScopes.Add(scopeAd4);

            deptSales.HeadUserId = 2; // ad4 là trưởng phòng Sale

            await context.SaveChangesAsync();

            return new TestHarness
            {
                Context = context,
                Service = staffService,
                DelegationService = delegationService,
                SuperAdmin = uSuper,
                SalesAdmin = uSalesAdmin,
                UiAdmin = uUiAdmin,
                SalesStaff1 = uSale1,
                SalesStaff2 = uSale2,
                CmsStaff = uCms,
                DeptSales = deptSales,
                DeptCms = deptCms,
                RoleSalesStaff = rSalesStaff,
                RoleContentEditor = rContentEditor
            };
        }

        [Fact]
        public async Task GetPagedAsync_SuperAdmin_CanSeeAllInternalStaffAndSearchByKeyword()
        {
            var h = await CreateHarnessAsync();

            var query = new StaffFilterQuery { Keyword = "Hải Đăng" };
            var result = await h.Service.GetPagedAsync(h.SuperAdmin.Id, query);

            result.TotalItems.Should().Be(1);
            result.Items.First().Username.Should().Be("sale01");
            result.Items.First().EmployeeCode.Should().Be("NV0001");
        }

        [Fact]
        public async Task GetPagedAsync_SubAdmin_OnlySeesStaffWithinScopedDepartment()
        {
            var h = await CreateHarnessAsync();

            // ad4 chỉ có scope Phòng Sale
            var result = await h.Service.GetPagedAsync(h.SalesAdmin.Id, new StaffFilterQuery());

            // Trong phòng Sale gồm ad4, sale01, sale02 (tổng = 3). Không được thấy editor01 (phòng CMS) hay SuperAdmin
            result.Items.Should().NotContain(x => x.Username == "editor01");
            result.Items.Should().NotContain(x => x.Username == "superadmin");
            result.Items.Should().Contain(x => x.Username == "sale01");
            result.Items.Should().Contain(x => x.Username == "sale02");
        }

        [Fact]
        public async Task GetPagedAsync_FilterByDepartmentAndRole_ReturnsCorrectSubset()
        {
            var h = await CreateHarnessAsync();

            var query = new StaffFilterQuery
            {
                DepartmentId = h.DeptSales.Id,
                RoleId = h.RoleSalesStaff.Id
            };

            var result = await h.Service.GetPagedAsync(h.SuperAdmin.Id, query);

            result.TotalItems.Should().Be(2); // sale01 and sale02
            result.Items.Select(x => x.Username).Should().BeEquivalentTo(new[] { "sale01", "sale02" });
        }

        [Fact]
        public async Task GetPagedAsync_FilterByStatus_ActiveAndLocked()
        {
            var h = await CreateHarnessAsync();

            var activeResult = await h.Service.GetPagedAsync(h.SuperAdmin.Id, new StaffFilterQuery { Status = "active" });
            activeResult.Items.Should().NotContain(x => x.Username == "sale02"); // sale02 is locked

            var lockedResult = await h.Service.GetPagedAsync(h.SuperAdmin.Id, new StaffFilterQuery { Status = "locked" });
            lockedResult.Items.Should().ContainSingle(x => x.Username == "sale02");
        }

        [Fact]
        public async Task GetPagedAsync_PaginationAndSorting_WorksProperly()
        {
            var h = await CreateHarnessAsync();

            var query = new StaffFilterQuery
            {
                Page = 1,
                PageSize = 2,
                SortBy = "name",
                SortDir = "asc"
            };

            var page1 = await h.Service.GetPagedAsync(h.SuperAdmin.Id, query);
            page1.Items.Count.Should().Be(2);
            page1.TotalPages.Should().Be(3); // 6 internal users / 2 = 3 pages
            page1.HasNextPage.Should().BeTrue();
            page1.HasPreviousPage.Should().BeFalse();
            page1.FromItem.Should().Be(1);
            page1.ToItem.Should().Be(2);

            query.Page = 2;
            var page2 = await h.Service.GetPagedAsync(h.SuperAdmin.Id, query);
            page2.Items.Count.Should().Be(2);
            page2.HasPreviousPage.Should().BeTrue();
            page2.FromItem.Should().Be(3);
            page2.ToItem.Should().Be(4);
        }

        [Fact]
        public async Task CreateAsync_DuplicateUsernameOrEmail_FailsValidation()
        {
            var h = await CreateHarnessAsync();

            // Trùng username
            var req1 = new CreateStaffRequest
            {
                Username = "sale01",
                FullName = "Người Mới",
                Email = "new@canmake.vn",
                Password = "Password@123",
                DepartmentId = h.DeptSales.Id
            };
            var res1 = await h.Service.CreateAsync(h.SuperAdmin.Id, req1);
            res1.Succeeded.Should().BeFalse();
            res1.ErrorCode.Should().Be("DUPLICATE_USERNAME");

            // Trùng email
            var req2 = new CreateStaffRequest
            {
                Username = "newuser01",
                FullName = "Người Mới",
                Email = "sale01@canmake.vn",
                Password = "Password@123",
                DepartmentId = h.DeptSales.Id
            };
            var res2 = await h.Service.CreateAsync(h.SuperAdmin.Id, req2);
            res2.Succeeded.Should().BeFalse();
            res2.ErrorCode.Should().Be("DUPLICATE_EMAIL");
        }

        [Fact]
        public async Task SoftDeleteAsync_CannotDeleteSelf()
        {
            var h = await CreateHarnessAsync();

            var res = await h.Service.SoftDeleteAsync(h.SuperAdmin.Id, h.SuperAdmin.Id);
            res.Succeeded.Should().BeFalse();
            res.ErrorCode.Should().Be("CANNOT_DELETE_SELF");
        }

        [Fact]
        public async Task SoftDeleteAsync_CannotDeleteDepartmentHead_RequiresReassigningHeadFirst()
        {
            var h = await CreateHarnessAsync();

            // ad4 (Id = 2) đang là HeadUserId của DeptSales
            var res = await h.Service.SoftDeleteAsync(h.SuperAdmin.Id, h.SalesAdmin.Id);
            res.Succeeded.Should().BeFalse();
            res.ErrorCode.Should().Be("CANNOT_DELETE_DEPT_HEAD");
            res.Message.Should().Contain("Trưởng phòng");
        }

        [Fact]
        public async Task SoftDeleteAsync_And_RestoreAsync_Flow()
        {
            var h = await CreateHarnessAsync();

            // 1. Xóa mềm sale01
            var delRes = await h.Service.SoftDeleteAsync(h.SuperAdmin.Id, h.SalesStaff1.Id);
            delRes.Succeeded.Should().BeTrue();

            // 2. Không còn xuất hiện trong GetPagedAsync bình thường
            var normalList = await h.Service.GetPagedAsync(h.SuperAdmin.Id, new StaffFilterQuery());
            normalList.Items.Should().NotContain(x => x.Username == "sale01");

            // 3. Xuất hiện trong Trash
            var trashList = await h.Service.GetTrashPagedAsync(h.SuperAdmin.Id, new StaffFilterQuery());
            trashList.Items.Should().ContainSingle(x => x.Username == "sale01");

            // 4. Khôi phục từ Trash
            var restoreRes = await h.Service.RestoreAsync(h.SuperAdmin.Id, h.SalesStaff1.Id);
            restoreRes.Succeeded.Should().BeTrue();

            // 5. Quay lại danh sách bình thường
            var restoredList = await h.Service.GetPagedAsync(h.SuperAdmin.Id, new StaffFilterQuery());
            restoredList.Items.Should().Contain(x => x.Username == "sale01");
        }

        [Fact]
        public async Task ToggleStatusAsync_CannotLockSelf()
        {
            var h = await CreateHarnessAsync();

            var res = await h.Service.ToggleStatusAsync(h.SuperAdmin.Id, h.SuperAdmin.Id);
            res.Succeeded.Should().BeFalse();
            res.ErrorCode.Should().Be("CANNOT_LOCK_SELF");
        }

        [Fact]
        public async Task ResetPasswordAsync_GeneratesStrongPasswordSuccessfully()
        {
            var h = await CreateHarnessAsync();

            var req = new ResetPasswordRequest
            {
                StaffId = h.SalesStaff1.Id,
                NewPassword = null // Tự sinh
            };

            var res = await h.Service.ResetPasswordAsync(h.SuperAdmin.Id, req);
            res.Succeeded.Should().BeTrue();
            res.Data.Should().NotBeNullOrWhiteSpace();
            res.Data.Should().StartWith("Canmake@");
            res.Data.Length.Should().BeGreaterThanOrEqualTo(10);
        }

        [Fact]
        public async Task ExportAsync_GeneratesNonEmptyExcelAndCsv()
        {
            var h = await CreateHarnessAsync();

            var xlsxBytes = await h.Service.ExportAsync(h.SuperAdmin.Id, new StaffFilterQuery(), "xlsx");
            xlsxBytes.Should().NotBeNull();
            xlsxBytes.Length.Should().BeGreaterThan(100);

            var csvBytes = await h.Service.ExportAsync(h.SuperAdmin.Id, new StaffFilterQuery(), "csv");
            csvBytes.Should().NotBeNull();
            csvBytes.Length.Should().BeGreaterThan(50);

            // Kiểm tra UTF-8 BOM
            csvBytes[0].Should().Be(0xEF);
            csvBytes[1].Should().Be(0xBB);
            csvBytes[2].Should().Be(0xBF);
        }

        [Fact]
        public async Task GetPagedAsync_With1000StaffRecords_RespondsUnder300Milliseconds()
        {
            var h = await CreateHarnessAsync();

            var dummyStaff = new List<User>(1000);
            for (int i = 1; i <= 1000; i++)
            {
                dummyStaff.Add(new User
                {
                    Username = $"perf_staff_{i}",
                    FullName = $"Nhân Sự Kiểm Thử Hiệu Năng {i}",
                    Email = $"perf_staff_{i}@canmake.vn",
                    PhoneNumber = $"091{i:D7}",
                    EmployeeCode = $"NV-P{i:D4}",
                    PasswordHash = "HASH",
                    UserType = 2,
                    DepartmentId = h.DeptSales.Id,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-i)
                });
            }

            h.Context.Users.AddRange(dummyStaff);
            await h.Context.SaveChangesAsync();

            // Warm-up query
            _ = await h.Service.GetPagedAsync(h.SuperAdmin.Id, new StaffFilterQuery { Page = 1, PageSize = 20 });

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var paged = await h.Service.GetPagedAsync(h.SuperAdmin.Id, new StaffFilterQuery
            {
                DepartmentId = h.DeptSales.Id,
                Keyword = "Kiểm Thử",
                Page = 2,
                PageSize = 20
            });
            sw.Stop();

            paged.TotalItems.Should().BeGreaterThanOrEqualTo(1000);
            paged.Items.Count.Should().Be(20);
            sw.ElapsedMilliseconds.Should().BeLessThan(300);
        }
    }
}

