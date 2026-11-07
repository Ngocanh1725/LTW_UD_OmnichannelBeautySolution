using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Omnichannel.Application.DTOs.Security;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Security;
using Omnichannel.Infrastructure.Services;
using Omnichannel.Tests.Helpers;
using Omnichannel.Web.Filters;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class DelegationServiceSecurityTests
    {
        private class TestEnvironment
        {
            public ApplicationDbContext Context { get; set; } = null!;
            public IMemoryCache Cache { get; set; } = null!;
            public PermissionService PermissionService { get; set; } = null!;
            public AuditLogService AuditLogService { get; set; } = null!;
            public DelegationService DelegationService { get; set; } = null!;

            public Department DeptBoard { get; set; } = null!;
            public Department DeptSales { get; set; } = null!;
            public Department DeptCms { get; set; } = null!;

            public Permission PermOrderView { get; set; } = null!;
            public Permission PermOrderConfirm { get; set; } = null!;
            public Permission PermCustomerView { get; set; } = null!;
            public Permission PermThemeManage { get; set; } = null!;
            public Permission PermSettingsManage { get; set; } = null!;

            public Role RoleSuperAdmin { get; set; } = null!;
            public Role RoleSalesAdmin { get; set; } = null!;
            public Role RoleUiAdmin { get; set; } = null!;
            public Role RoleStoreManager { get; set; } = null!;
            public Role RoleSalesStaff { get; set; } = null!;
            public Role RoleCustomer { get; set; } = null!;

            public User SuperAdminUser { get; set; } = null!;
            public User SalesAdminUser { get; set; } = null!; // ad4 (Level 50)
            public User UiAdminUser { get; set; } = null!;    // ad1 (Level 50)
            public User SalesStaffUser { get; set; } = null!; // sale01 (Level 10)
            public User CmsStaffUser { get; set; } = null!;   // editor01 (Level 10, phòng khác)
            public User CustomerUser { get; set; } = null!;   // customer (UserType = 1)
        }

        private static async Task<TestEnvironment> BuildSeededEnvironmentAsync()
        {
            var context = TestDbContextFactory.Create();
            var cache = new MemoryCache(new MemoryCacheOptions());
            var permService = new PermissionService(context, cache);
            var auditService = new AuditLogService(context, Microsoft.Extensions.Logging.Abstractions.NullLogger<AuditLogService>.Instance);
            var delegationService = new DelegationService(context, permService, auditService);

            var deptBoard = new Department { Id = 1, Code = "BAN_GIAM_DOC", Name = "Ban Giám Đốc", IsActive = true, SortOrder = 1 };
            var deptSales = new Department { Id = 3, Code = "PHONG_SALE", Name = "Phòng Kinh Doanh", ParentId = 1, IsActive = true, SortOrder = 3 };
            var deptCms = new Department { Id = 5, Code = "PHONG_GIAO_DIEN_CMS", Name = "Phòng Giao Diện CMS", ParentId = 1, IsActive = true, SortOrder = 5 };
            context.Departments.AddRange(deptBoard, deptSales, deptCms);

            var pOrderView = new Permission { Id = 1, Code = "ORDER_VIEW", Name = "Xem đơn hàng", Module = "ORDER" };
            var pOrderConfirm = new Permission { Id = 2, Code = "ORDER_CONFIRM", Name = "Chốt đơn hàng", Module = "ORDER" };
            var pCustomerView = new Permission { Id = 3, Code = "CUSTOMER_VIEW", Name = "Xem khách hàng", Module = "CUSTOMER" };
            var pThemeManage = new Permission { Id = 4, Code = "THEME_MANAGE", Name = "Quản lý giao diện", Module = "CMS" };
            var pSettingsManage = new Permission { Id = 5, Code = "SETTINGS_MANAGE", Name = "Quản trị hệ thống", Module = "SETTINGS" };
            context.Permissions.AddRange(pOrderView, pOrderConfirm, pCustomerView, pThemeManage, pSettingsManage);

            var rSuperAdmin = new Role { Id = 1, Name = "SuperAdmin", DisplayName = "Admin Tổng", Level = 100, DepartmentId = 1, IsSystem = true, IsActive = true };
            var rSalesAdmin = new Role { Id = 2, Name = "SalesAdmin", DisplayName = "Giám Đốc Sale", Level = 50, DepartmentId = 3, IsSystem = true, IsActive = true };
            var rUiAdmin = new Role { Id = 3, Name = "UiAdmin", DisplayName = "Admin Giao Diện", Level = 50, DepartmentId = 5, IsSystem = true, IsActive = true };
            var rStoreManager = new Role { Id = 4, Name = "StoreManager", DisplayName = "Cửa Hàng Trưởng", Level = 30, DepartmentId = 3, IsSystem = true, IsActive = true };
            var rSalesStaff = new Role { Id = 5, Name = "SalesStaff", DisplayName = "Nhân Viên Sale", Level = 10, DepartmentId = 3, IsSystem = true, IsActive = true };
            var rCustomer = new Role { Id = 6, Name = "Customer", DisplayName = "Khách Hàng", Level = 0, IsSystem = true, IsActive = true };
            context.Roles.AddRange(rSuperAdmin, rSalesAdmin, rUiAdmin, rStoreManager, rSalesStaff, rCustomer);

            // RolePermissions
            context.RolePermissions.AddRange(
                // SuperAdmin có đủ 5 quyền
                new RolePermission { RoleId = 1, PermissionId = 1 },
                new RolePermission { RoleId = 1, PermissionId = 2 },
                new RolePermission { RoleId = 1, PermissionId = 3 },
                new RolePermission { RoleId = 1, PermissionId = 4 },
                new RolePermission { RoleId = 1, PermissionId = 5 },
                // SalesAdmin (ad4) có ORDER_VIEW, ORDER_CONFIRM, CUSTOMER_VIEW
                new RolePermission { RoleId = 2, PermissionId = 1 },
                new RolePermission { RoleId = 2, PermissionId = 2 },
                new RolePermission { RoleId = 2, PermissionId = 3 },
                // UiAdmin (ad1) có THEME_MANAGE
                new RolePermission { RoleId = 3, PermissionId = 4 },
                // StoreManager (Level 30) có ORDER_VIEW, ORDER_CONFIRM
                new RolePermission { RoleId = 4, PermissionId = 1 },
                new RolePermission { RoleId = 4, PermissionId = 2 },
                // SalesStaff (Level 10) mặc định chỉ có ORDER_VIEW
                new RolePermission { RoleId = 5, PermissionId = 1 }
            );

            var uSuperAdmin = new User { Id = 1, Username = "superadmin", FullName = "Admin Tổng", Email = "sa@test.vn", PasswordHash = "h", UserType = 2, DepartmentId = 1, IsActive = true, SecurityStamp = "s1" };
            var uSalesAdmin = new User { Id = 2, Username = "ad4", FullName = "Giám Đốc Sale", Email = "ad4@test.vn", PasswordHash = "h", UserType = 2, DepartmentId = 3, ManagerId = 1, IsActive = true, SecurityStamp = "s2" };
            var uUiAdmin = new User { Id = 3, Username = "ad1", FullName = "Admin Giao Diện", Email = "ad1@test.vn", PasswordHash = "h", UserType = 2, DepartmentId = 5, ManagerId = 1, IsActive = true, SecurityStamp = "s3" };
            var uSalesStaff = new User { Id = 4, Username = "sale01", FullName = "Nhân Viên Sale 01", Email = "sale01@test.vn", PasswordHash = "h", UserType = 2, DepartmentId = 3, ManagerId = 2, IsActive = true, SecurityStamp = "s4" };
            var uCmsStaff = new User { Id = 5, Username = "editor01", FullName = "Nhân Viên CMS", Email = "editor01@test.vn", PasswordHash = "h", UserType = 2, DepartmentId = 5, ManagerId = 3, IsActive = true, SecurityStamp = "s5" };
            var uCustomer = new User { Id = 6, Username = "cust01", FullName = "Khách Hàng 01", Email = "cust01@test.vn", PasswordHash = "h", UserType = 1, IsActive = true, SecurityStamp = "s6" };
            context.Users.AddRange(uSuperAdmin, uSalesAdmin, uUiAdmin, uSalesStaff, uCmsStaff, uCustomer);

            context.UserRoles.AddRange(
                new UserRole { UserId = 1, RoleId = 1 },
                new UserRole { UserId = 2, RoleId = 2 },
                new UserRole { UserId = 3, RoleId = 3 },
                new UserRole { UserId = 4, RoleId = 5 },
                new UserRole { UserId = 5, RoleId = 3 },
                new UserRole { UserId = 6, RoleId = 6 }
            );

            // Cấu hình Scope ban đầu cho ad4 (SalesAdmin):
            // Được quản lý PHONG_SALE (Id=3), MaxGrantLevel = 30, được cấp ORDER_VIEW (1), ORDER_CONFIRM (2), CUSTOMER_VIEW (3)
            var scopeAd4 = new AdminDelegationScope
            {
                Id = 1,
                AdminUserId = 2,
                CanCreateUser = true,
                CanEditUser = true,
                CanDeleteUser = false,
                CanResetPassword = true,
                CanAssignRole = true,
                CanDelegateFurther = false,
                MaxGrantLevel = 30,
                GrantedByUserId = 1
            };
            context.AdminDelegationScopes.Add(scopeAd4);
            context.AdminDelegationDepartments.Add(new AdminDelegationDepartment { AdminDelegationScopeId = 1, DepartmentId = 3 });
            context.AdminDelegationPermissions.AddRange(
                new AdminDelegationPermission { AdminDelegationScopeId = 1, PermissionId = 1 },
                new AdminDelegationPermission { AdminDelegationScopeId = 1, PermissionId = 2 },
                new AdminDelegationPermission { AdminDelegationScopeId = 1, PermissionId = 3 }
            );

            await context.SaveChangesAsync();

            return new TestEnvironment
            {
                Context = context,
                Cache = cache,
                PermissionService = permService,
                AuditLogService = auditService,
                DelegationService = delegationService,
                DeptBoard = deptBoard,
                DeptSales = deptSales,
                DeptCms = deptCms,
                PermOrderView = pOrderView,
                PermOrderConfirm = pOrderConfirm,
                PermCustomerView = pCustomerView,
                PermThemeManage = pThemeManage,
                PermSettingsManage = pSettingsManage,
                RoleSuperAdmin = rSuperAdmin,
                RoleSalesAdmin = rSalesAdmin,
                RoleUiAdmin = rUiAdmin,
                RoleStoreManager = rStoreManager,
                RoleSalesStaff = rSalesStaff,
                RoleCustomer = rCustomer,
                SuperAdminUser = uSuperAdmin,
                SalesAdminUser = uSalesAdmin,
                UiAdminUser = uUiAdmin,
                SalesStaffUser = uSalesStaff,
                CmsStaffUser = uCmsStaff,
                CustomerUser = uCustomer
            };
        }

        // =========================================================================
        // KỊCH BẢN 1: Admin nhỏ cấp quyền nằm ngoài Scope -> Bị từ chối
        // =========================================================================
        [Fact]
        public async Task Scenario1_SubAdmin_GrantsPermissionOutsideScope_IsRejected()
        {
            var env = await BuildSeededEnvironmentAsync();

            // ad4 (SalesAdmin) cố gắng cấp quyền THEME_MANAGE (Id=4) hoặc SETTINGS_MANAGE (Id=5) cho sale01 (Id=4)
            var resTheme = await env.DelegationService.GrantUserPermissionAsync(
                actorId: env.SalesAdminUser.Id,
                targetUserId: env.SalesStaffUser.Id,
                permissionId: env.PermThemeManage.Id,
                isGranted: true,
                reason: "Thử cấp vượt quyền");

            resTheme.Succeeded.Should().BeFalse();
            resTheme.ErrorCode.Should().Be("PERMISSION_OUT_OF_SCOPE");
            resTheme.Message.Should().Contain("vượt phạm vi được giao");

            // Nhưng khi ad4 cấp quyền ORDER_CONFIRM (Id=2, nằm trong Scope của ad4) cho sale01 -> Thành công!
            var resValid = await env.DelegationService.GrantUserPermissionAsync(
                actorId: env.SalesAdminUser.Id,
                targetUserId: env.SalesStaffUser.Id,
                permissionId: env.PermOrderConfirm.Id,
                isGranted: true,
                reason: "Giao việc chốt đơn ca sáng");

            resValid.Succeeded.Should().BeTrue();
            var effective = await env.PermissionService.GetEffectivePermissionsAsync(env.SalesStaffUser.Id);
            effective.Should().Contain("ORDER_CONFIRM");
        }

        // =========================================================================
        // KỊCH BẢN 2: Admin nhỏ tự cấp quyền cho chính mình -> Bị từ chối
        // =========================================================================
        [Fact]
        public async Task Scenario2_SubAdmin_GrantsPermissionToSelf_IsRejected()
        {
            var env = await BuildSeededEnvironmentAsync();

            var resSelfGrant = await env.DelegationService.GrantUserPermissionAsync(
                actorId: env.SalesAdminUser.Id,
                targetUserId: env.SalesAdminUser.Id,
                permissionId: env.PermOrderConfirm.Id,
                isGranted: true);

            resSelfGrant.Succeeded.Should().BeFalse();
            resSelfGrant.ErrorCode.Should().Be("SELF_MODIFICATION_FORBIDDEN");
            resSelfGrant.Message.Should().Contain("chính mình");

            var resSelfRole = await env.DelegationService.AssignRolesAsync(
                actorId: env.SalesAdminUser.Id,
                targetUserId: env.SalesAdminUser.Id,
                roleIds: new[] { env.RoleSuperAdmin.Id });

            resSelfRole.Succeeded.Should().BeFalse();
            resSelfRole.ErrorCode.Should().Be("SELF_MODIFICATION_FORBIDDEN");
        }

        // =========================================================================
        // KỊCH BẢN 3: Admin nhỏ sửa SuperAdmin, Admin ngang cấp hoặc khác phòng ban -> Bị từ chối
        // =========================================================================
        [Fact]
        public async Task Scenario3_SubAdmin_ModifiesSuperAdminOrPeerLevelAdmin_IsRejected()
        {
            var env = await BuildSeededEnvironmentAsync();

            // 3a. ad4 (Level 50) cố gắng tác động lên SuperAdmin (Level 100)
            var canManageSa = await env.DelegationService.CanManageUserAsync(env.SalesAdminUser.Id, env.SuperAdminUser.Id);
            canManageSa.Should().BeFalse();

            var resModifySa = await env.DelegationService.GrantUserPermissionAsync(
                actorId: env.SalesAdminUser.Id,
                targetUserId: env.SuperAdminUser.Id,
                permissionId: env.PermOrderView.Id,
                isGranted: false);

            resModifySa.Succeeded.Should().BeFalse();
            resModifySa.ErrorCode.Should().Be("TARGET_IS_SUPERADMIN");

            // 3b. ad4 (Level 50) cố gắng tác động lên ad1 (Level 50 - ngang cấp)
            var canManagePeer = await env.DelegationService.CanManageUserAsync(env.SalesAdminUser.Id, env.UiAdminUser.Id);
            canManagePeer.Should().BeFalse();

            var resModifyPeer = await env.DelegationService.GrantUserPermissionAsync(
                actorId: env.SalesAdminUser.Id,
                targetUserId: env.UiAdminUser.Id,
                permissionId: env.PermOrderView.Id,
                isGranted: true);

            resModifyPeer.Succeeded.Should().BeFalse();
            resModifyPeer.ErrorCode.Should().Be("TARGET_LEVEL_TOO_HIGH");
            resModifyPeer.Message.Should().Contain("cấp bậc bằng hoặc cao hơn");
        }

        // =========================================================================
        // KỊCH BẢN 4: Admin nhỏ gán Role có Level >= MaxGrantLevel hoặc Role Customer -> Bị từ chối
        // =========================================================================
        [Fact]
        public async Task Scenario4_SubAdmin_AssignsRoleWithLevelGreaterOrEqualMaxGrantLevel_IsRejected()
        {
            var env = await BuildSeededEnvironmentAsync();

            // MaxGrantLevel của ad4 là 30 -> Role StoreManager có Level = 30 (>= MaxGrantLevel) phải bị từ chối!
            var assignableIds = await env.DelegationService.GetAssignableRoleIdsAsync(env.SalesAdminUser.Id);
            assignableIds.Should().Contain(env.RoleSalesStaff.Id); // Level 10 < 30 -> Hợp lệ
            assignableIds.Should().NotContain(env.RoleStoreManager.Id); // Level 30 >= 30 -> Bị chặn
            assignableIds.Should().NotContain(env.RoleSalesAdmin.Id);   // Level 50 -> Bị chặn
            assignableIds.Should().NotContain(env.RoleSuperAdmin.Id);   // Level 100 -> Bị chặn
            assignableIds.Should().NotContain(env.RoleCustomer.Id);     // Role Customer -> Tuyệt đối không xuất hiện

            var resAssignLevel30 = await env.DelegationService.AssignRolesAsync(
                actorId: env.SalesAdminUser.Id,
                targetUserId: env.SalesStaffUser.Id,
                roleIds: new[] { env.RoleStoreManager.Id });

            resAssignLevel30.Succeeded.Should().BeFalse();
            resAssignLevel30.ErrorCode.Should().Be("ROLE_LEVEL_EXCEEDS_MAX_GRANT_LEVEL");

            // Thử gán Role Customer cho nhân sự -> Bị từ chối
            var resAssignCustomer = await env.DelegationService.AssignRolesAsync(
                actorId: env.SuperAdminUser.Id,
                targetUserId: env.SalesStaffUser.Id,
                roleIds: new[] { env.RoleCustomer.Id });

            resAssignCustomer.Succeeded.Should().BeFalse();
            resAssignCustomer.ErrorCode.Should().Be("CUSTOMER_ROLE_FORBIDDEN_FOR_STAFF");

            // Gán Role SalesStaff (Level 10 < 30) -> Thành công!
            var resAssignValid = await env.DelegationService.AssignRolesAsync(
                actorId: env.SalesAdminUser.Id,
                targetUserId: env.SalesStaffUser.Id,
                roleIds: new[] { env.RoleSalesStaff.Id });

            resAssignValid.Succeeded.Should().BeTrue();
        }

        // =========================================================================
        // KỊCH BẢN 5: SuperAdmin thu hẹp Scope của ad4 -> Các quyền ad4 đã cấp vượt Scope mới bị thu hồi dây chuyền (Cascade Revoke)
        // =========================================================================
        [Fact]
        public async Task Scenario5_SuperAdminNarrowsSubAdminScope_CascadesRevokeOnSubordinates()
        {
            var env = await BuildSeededEnvironmentAsync();

            // Bước 1: ad4 cấp đặc cách ORDER_CONFIRM (Id=2) và CUSTOMER_VIEW (Id=3) cho nhân viên sale01
            (await env.DelegationService.GrantUserPermissionAsync(
                env.SalesAdminUser.Id, env.SalesStaffUser.Id, env.PermOrderConfirm.Id, true, "Cấp chốt đơn")).Succeeded.Should().BeTrue();

            (await env.DelegationService.GrantUserPermissionAsync(
                env.SalesAdminUser.Id, env.SalesStaffUser.Id, env.PermCustomerView.Id, true, "Cấp xem CRM")).Succeeded.Should().BeTrue();

            var beforePerms = await env.PermissionService.GetEffectivePermissionsAsync(env.SalesStaffUser.Id);
            beforePerms.Should().Contain("ORDER_CONFIRM");
            beforePerms.Should().Contain("CUSTOMER_VIEW");

            // Bước 2: SuperAdmin thu hẹp Scope của ad4: bỏ quyền CUSTOMER_VIEW (Id=3), chỉ giữ ORDER_VIEW (1) & ORDER_CONFIRM (2)
            var narrowResult = await env.DelegationService.SaveDelegationScopeAsync(
                actorId: env.SuperAdminUser.Id,
                req: new SaveDelegationScopeRequest
                {
                    AdminUserId = env.SalesAdminUser.Id,
                    CanCreateUser = true,
                    CanEditUser = true,
                    CanAssignRole = true,
                    MaxGrantLevel = 30,
                    DepartmentIds = new List<int> { env.DeptSales.Id },
                    PermissionIds = new List<int> { env.PermOrderView.Id, env.PermOrderConfirm.Id } // Đã rút CUSTOMER_VIEW!
                });

            narrowResult.Succeeded.Should().BeTrue();

            // Bước 3: Kiểm tra quyền của sale01 -> CUSTOMER_VIEW đã tự động bị thu hồi (Cascade Revoke), còn ORDER_CONFIRM vẫn giữ nguyên!
            var afterPerms = await env.PermissionService.GetEffectivePermissionsAsync(env.SalesStaffUser.Id);
            afterPerms.Should().NotContain("CUSTOMER_VIEW", "Quyền do ad4 cấp vượt quá Scope mới phải bị thu hồi tự động");
            afterPerms.Should().Contain("ORDER_CONFIRM", "Quyền vẫn nằm trong Scope mới của ad4 phải được giữ nguyên");

            // Kiểm tra AuditLog có bản ghi CASCADE_REVOKE và UPDATE_SCOPE
            var logs = await env.Context.AuditLogs.AsNoTracking().ToListAsync();
            logs.Should().Contain(l => l.Action == "UPDATE_SCOPE");
            logs.Should().Contain(l => l.Action == "CASCADE_REVOKE");
        }

        // =========================================================================
        // KỊCH BẢN 6: Override Deny (IsGranted = false) luôn thắng quyền kế thừa từ Role
        // =========================================================================
        [Fact]
        public async Task Scenario6_OverrideDeny_AlwaysWinsOverRoleInheritedPermission()
        {
            var env = await BuildSeededEnvironmentAsync();

            // sale01 mặc định có ORDER_VIEW từ Role SalesStaff
            var initialPerms = await env.PermissionService.GetEffectivePermissionsAsync(env.SalesStaffUser.Id);
            initialPerms.Should().Contain("ORDER_VIEW");

            // ad4 thực hiện Override Deny (isGranted = false) đối với ORDER_VIEW của sale01
            var denyRes = await env.DelegationService.GrantUserPermissionAsync(
                actorId: env.SalesAdminUser.Id,
                targetUserId: env.SalesStaffUser.Id,
                permissionId: env.PermOrderView.Id,
                isGranted: false,
                reason: "Tạm dừng quyền xem đơn hàng");

            denyRes.Succeeded.Should().BeTrue();

            // Kiểm tra quyền hiệu lực: ORDER_VIEW phải bị loại bỏ ngay lập tức (Cache đã bị evict + SecurityStamp đổi)
            var effectiveAfterDeny = await env.PermissionService.GetEffectivePermissionsAsync(env.SalesStaffUser.Id);
            effectiveAfterDeny.Should().NotContain("ORDER_VIEW");
        }

        // =========================================================================
        // KỊCH BẢN 7: Khách hàng (UserType = 1) dù bị gán nhầm Role nội bộ (kể cả SuperAdmin) vẫn bị chặn hoàn toàn khỏi /Admin
        // =========================================================================
        [Fact]
        public async Task Scenario7_CustomerWithMistakenInternalRole_IsStrictlyBlockedFromAdmin()
        {
            var env = await BuildSeededEnvironmentAsync();

            // Giả sử tài khoản khách hàng (UserType = 1) bị gán nhầm Role SuperAdmin trong bảng UserRoles
            env.Context.UserRoles.Add(new UserRole
            {
                UserId = env.CustomerUser.Id,
                RoleId = env.RoleSuperAdmin.Id
            });
            await env.Context.SaveChangesAsync();

            // 7a. Kiểm tra PermissionService: Tập quyền hiệu lực của UserType = 1 phải bằng RỖNG
            var effectivePerms = await env.PermissionService.GetEffectivePermissionsAsync(env.CustomerUser.Id);
            effectivePerms.Should().BeEmpty("Khách hàng (UserType = 1) tuyệt đối không được nhận bất kỳ quyền nội bộ nào");

            // 7b. Kiểm tra PermissionAuthorizationHandler: Dù có Claim Role SuperAdmin vẫn bị từ chối
            var handler = new PermissionAuthorizationHandler(env.PermissionService);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, env.CustomerUser.Id.ToString()),
                new Claim(ClaimTypes.Name, env.CustomerUser.Username),
                new Claim("UserType", "1"),
                new Claim(ClaimTypes.Role, "SuperAdmin")
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "AdminCookieScheme"));
            var requirement = new PermissionRequirement("PERM:DASHBOARD_VIEW");
            var authCtx = new AuthorizationHandlerContext(new[] { requirement }, principal, null);

            await handler.HandleAsync(authCtx);
            authCtx.HasSucceeded.Should().BeFalse("PermissionAuthorizationHandler phải chặn UserType = 1 kể cả khi có Role SuperAdmin");

            // 7c. Kiểm tra AdminAreaBoundaryFilter trên request vào /Admin
            var services = new ServiceCollection();
            services.AddSingleton(env.Context);
            var sp = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext
            {
                User = principal,
                RequestServices = sp
            };
            httpContext.Request.Path = "/Admin/Dashboard";

            var routeData = new RouteData();
            routeData.Values["area"] = "Admin";
            routeData.Values["controller"] = "Dashboard";
            routeData.Values["action"] = "Index";

            var actionContext = new ActionContext(httpContext, routeData, new ActionDescriptor());
            var filterContext = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
            var boundaryFilter = new AdminAreaBoundaryFilter();

            await boundaryFilter.OnAuthorizationAsync(filterContext);

            filterContext.Result.Should().NotBeNull("AdminAreaBoundaryFilter phải chặn request vào /Admin của UserType = 1");
            if (filterContext.Result is ContentResult contentResult)
            {
                contentResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
            }
        }
    }
}

