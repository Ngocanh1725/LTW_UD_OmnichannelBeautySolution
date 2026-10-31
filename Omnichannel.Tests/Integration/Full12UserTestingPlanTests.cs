using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Infrastructure.Security;
using Omnichannel.Tests.Integration.Authorization;
using Xunit;

namespace Omnichannel.Tests.Integration
{
    [Collection("AuthTests")]
    public class Full12UserTestingPlanTests : IClassFixture<AuthorizationTestFixture>
    {
        private readonly AuthorizationTestFixture _fixture;
        private readonly PasswordHasherService _passwordHasher;

        public Full12UserTestingPlanTests(AuthorizationTestFixture fixture)
        {
            _fixture = fixture;
            _passwordHasher = new PasswordHasherService();
        }

        // =========================================================================
        // 1. SUPERADMIN TEST SUITE
        // =========================================================================
        [Fact]
        public async Task Test1_SuperAdmin_HasFullGlobalAccessAndPermissions()
        {
            // [x] Đăng nhập superadmin / Admin@123
            var user = await _fixture.DbContext.Users.FirstOrDefaultAsync(u => u.Username == "superadmin");
            user.Should().NotBeNull();
            _passwordHasher.VerifyPassword(user!.PasswordHash, "Admin@123").Should().BeTrue();

            // [x] Global Scope (Toàn bộ chi nhánh)
            user.StoreId.Should().BeNull("SuperAdmin quản trị toàn hệ thống");

            // [x] Quyền hạn đầy đủ
            var canManageProduct = await _fixture.CheckPermissionAsync("superadmin", "PRODUCT", "CREATE");
            var canManageOrder = await _fixture.CheckPermissionAsync("superadmin", "ORDER", "VIEW");
            var canManageCustomer = await _fixture.CheckPermissionAsync("superadmin", "CUSTOMER", "VIEW");
            var canManagePermissions = await _fixture.CheckPermissionAsync("superadmin", "SECURITY", "PERMISSION_MANAGE");
            var canManageSettings = await _fixture.CheckPermissionAsync("superadmin", "SETTINGS", "VIEW");
            var canApproveTransfer = await _fixture.CheckPermissionAsync("superadmin", "TRANSFER", "APPROVE");
            var canViewFinance = await _fixture.CheckPermissionAsync("superadmin", "FINANCE", "PNL_VIEW");

            canManageProduct.Should().BeTrue();
            canManageOrder.Should().BeTrue();
            canManageCustomer.Should().BeTrue();
            canManagePermissions.Should().BeTrue();
            canManageSettings.Should().BeTrue();
            canApproveTransfer.Should().BeTrue();
            canViewFinance.Should().BeTrue();
        }

        // =========================================================================
        // 2. STORE MANAGER (HQ-001) TEST SUITE
        // =========================================================================
        [Fact]
        public async Task Test2_StoreManagerHQ_HasBranchScopeAndNoSystemSettingsAccess()
        {
            // [x] Đăng nhập manager_hq / Admin@123
            var user = await _fixture.DbContext.Users
                .Include(u => u.Store)
                .FirstOrDefaultAsync(u => u.Username == "manager_hq");
            user.Should().NotBeNull();
            _passwordHasher.VerifyPassword(user!.PasswordHash, "Admin@123").Should().BeTrue();

            // [x] Phạm vi chi nhánh SH-Q1
            user.Store.Should().NotBeNull();
            user.Store!.Code.Should().Be("SH-Q1");

            // [x] Có quyền quản lý chi nhánh và tạo phiếu chuyển kho
            var canViewDashboard = await _fixture.CheckPermissionAsync("manager_hq", "DASHBOARD", "VIEW");
            var canViewInventory = await _fixture.CheckPermissionAsync("manager_hq", "INVENTORY", "VIEW");
            var canCreateTransfer = await _fixture.CheckPermissionAsync("manager_hq", "TRANSFER", "CREATE");

            canViewDashboard.Should().BeTrue();
            canViewInventory.Should().BeTrue();
            canCreateTransfer.Should().BeTrue();

            // [x] KHÔNG CÓ quyền Cài Đặt Hệ Thống, Ma Trận Phân Quyền và Phê Duyệt Chuyển Kho
            var canManageSettings = await _fixture.CheckPermissionAsync("manager_hq", "SETTINGS", "VIEW");
            var canManagePermissions = await _fixture.CheckPermissionAsync("manager_hq", "SECURITY", "PERMISSION_MANAGE");
            var canApproveTransfer = await _fixture.CheckPermissionAsync("manager_hq", "TRANSFER", "APPROVE");

            canManageSettings.Should().BeFalse("StoreManager không được truy cập Cài Đặt Hệ Thống");
            canManagePermissions.Should().BeFalse("StoreManager không được truy cập Ma Trận Phân Quyền");
            canApproveTransfer.Should().BeFalse("StoreManager không được phê duyệt phiếu chuyển kho (chỉ SuperAdmin)");
        }

        // =========================================================================
        // 3. STORE MANAGER 2 (SH-BD) & STORE ISOLATION
        // =========================================================================
        [Fact]
        public async Task Test3_StoreManagerCG_StrictStoreIsolation()
        {
            // [x] Đăng nhập manager_cg / Admin@123
            var userCg = await _fixture.DbContext.Users
                .Include(u => u.Store)
                .FirstOrDefaultAsync(u => u.Username == "manager_cg");
            var userHq = await _fixture.DbContext.Users
                .Include(u => u.Store)
                .FirstOrDefaultAsync(u => u.Username == "manager_hq");

            userCg.Should().NotBeNull();
            _passwordHasher.VerifyPassword(userCg!.PasswordHash, "Admin@123").Should().BeTrue();
            userCg.Store!.Code.Should().Be("SH-BD");

            // [x] Kiểm tra phân lập chi nhánh (Store Isolation)
            userCg.StoreId.Should().NotBe(userHq!.StoreId, "Hai chi nhánh phải hoàn toàn độc lập");

            var storeHqInventories = await _fixture.DbContext.StoreInventories
                .Where(si => si.StoreId == userHq.StoreId)
                .Select(si => si.StoreId)
                .Distinct()
                .ToListAsync();

            var storeCgInventories = await _fixture.DbContext.StoreInventories
                .Where(si => si.StoreId == userCg.StoreId)
                .Select(si => si.StoreId)
                .Distinct()
                .ToListAsync();

            storeHqInventories.Should().ContainSingle().Which.Should().Be(userHq.StoreId!.Value);
            storeCgInventories.Should().ContainSingle().Which.Should().Be(userCg.StoreId!.Value);
        }

        // =========================================================================
        // 4. CASHIER TEST SUITE
        // =========================================================================
        [Fact]
        public async Task Test4_Cashier_HasPosAccessAndBlockedFromFinanceSettings()
        {
            // [x] Đăng nhập cashier01 / Admin@123
            var user = await _fixture.DbContext.Users.FirstOrDefaultAsync(u => u.Username == "cashier01");
            user.Should().NotBeNull();
            _passwordHasher.VerifyPassword(user!.PasswordHash, "Admin@123").Should().BeTrue();

            // [x] Có quyền POS và xem sản phẩm/kho
            var canAccessPos = await _fixture.CheckPermissionAsync("cashier01", "POS", "ACCESS");
            var canCheckout = await _fixture.CheckPermissionAsync("cashier01", "POS", "CHECKOUT");
            var canViewProduct = await _fixture.CheckPermissionAsync("cashier01", "PRODUCT", "VIEW");
            var canViewInventory = await _fixture.CheckPermissionAsync("cashier01", "INVENTORY", "VIEW");

            canAccessPos.Should().BeTrue();
            canCheckout.Should().BeTrue();
            canViewProduct.Should().BeTrue();
            canViewInventory.Should().BeTrue();

            // [x] KHÔNG truy cập được Settings, Finance
            var canSettings = await _fixture.CheckPermissionAsync("cashier01", "SETTINGS", "VIEW");
            var canFinance = await _fixture.CheckPermissionAsync("cashier01", "FINANCE", "PNL_VIEW");

            canSettings.Should().BeFalse("Thu ngân không được truy cập Cài Đặt Hệ Thống (403)");
            canFinance.Should().BeFalse("Thu ngân không được truy cập Báo Cáo Tài Chính (403)");
        }

        // =========================================================================
        // 5. WAREHOUSE STAFF TEST SUITE
        // =========================================================================
        [Fact]
        public async Task Test5_WarehouseStaff_HasInventoryAccessAndBlockedFromPosFinance()
        {
            // [x] Đăng nhập warehouse01 / Admin@123
            var user = await _fixture.DbContext.Users.FirstOrDefaultAsync(u => u.Username == "warehouse01");
            user.Should().NotBeNull();
            _passwordHasher.VerifyPassword(user!.PasswordHash, "Admin@123").Should().BeTrue();

            // [x] Có quyền Kho và Quản lý Lô
            var canViewInventory = await _fixture.CheckPermissionAsync("warehouse01", "INVENTORY", "VIEW");
            var canImport = await _fixture.CheckPermissionAsync("warehouse01", "INVENTORY", "IMPORT");
            var canAdjust = await _fixture.CheckPermissionAsync("warehouse01", "INVENTORY", "ADJUST");

            canViewInventory.Should().BeTrue();
            canImport.Should().BeTrue();
            canAdjust.Should().BeTrue();

            // [x] KHÔNG truy cập được POS, Finance, Settings
            var canPos = await _fixture.CheckPermissionAsync("warehouse01", "POS", "ACCESS");
            var canFinance = await _fixture.CheckPermissionAsync("warehouse01", "FINANCE", "PNL_VIEW");
            var canSettings = await _fixture.CheckPermissionAsync("warehouse01", "SETTINGS", "VIEW");

            canPos.Should().BeFalse();
            canFinance.Should().BeFalse();
            canSettings.Should().BeFalse();
        }

        // =========================================================================
        // 6. CONTENT EDITOR TEST SUITE
        // =========================================================================
        [Fact]
        public async Task Test6_ContentEditor_HasCmsAccessAndBlockedFromPosInventoryFinance()
        {
            // [x] Đăng nhập editor01 / Admin@123
            var user = await _fixture.DbContext.Users.FirstOrDefaultAsync(u => u.Username == "editor01");
            user.Should().NotBeNull();
            _passwordHasher.VerifyPassword(user!.PasswordHash, "Admin@123").Should().BeTrue();

            // [x] Có quyền CMS: Bài viết, Banner, Menu, Review
            var canPost = await _fixture.CheckPermissionAsync("editor01", "CMS", "POST_MANAGE");
            var canBanner = await _fixture.CheckPermissionAsync("editor01", "CMS", "BANNER_MANAGE");
            var canMenu = await _fixture.CheckPermissionAsync("editor01", "CMS", "MENU_MANAGE");
            var canReview = await _fixture.CheckPermissionAsync("editor01", "CMS", "REVIEW_MODERATE");

            canPost.Should().BeTrue();
            canBanner.Should().BeTrue();
            canMenu.Should().BeTrue();
            canReview.Should().BeTrue();

            // [x] KHÔNG truy cập được POS, Finance, Kho
            var canPos = await _fixture.CheckPermissionAsync("editor01", "POS", "ACCESS");
            var canFinance = await _fixture.CheckPermissionAsync("editor01", "FINANCE", "PNL_VIEW");
            var canInventory = await _fixture.CheckPermissionAsync("editor01", "INVENTORY", "VIEW");

            canPos.Should().BeFalse();
            canFinance.Should().BeFalse();
            canInventory.Should().BeFalse();
        }

        // =========================================================================
        // 7. CUSTOMER (customer01) TEST SUITE
        // =========================================================================
        [Fact]
        public async Task Test7_Customer01_HasStorefrontAccessAndSilverTierWith50Points()
        {
            // [x] Đăng nhập customer01 / Admin@123
            var user = await _fixture.DbContext.Users
                .Include(u => u.LoyaltyWallet)
                    .ThenInclude(w => w!.CurrentTier)
                .FirstOrDefaultAsync(u => u.Username == "customer01");

            user.Should().NotBeNull();
            _passwordHasher.VerifyPassword(user!.PasswordHash, "Admin@123").Should().BeTrue();
            user.UserType.Should().Be(1, "Customer có UserType = 1");

            // [x] Bị chặn hoàn toàn khỏi Admin Dashboard
            var canAdmin = await _fixture.CheckPermissionAsync("customer01", "DASHBOARD", "VIEW");
            canAdmin.Should().BeFalse("Customer không có quyền truy cập Admin Dashboard (403)");

            // [x] Điểm và Hạng Thành Viên: Silver + 50 điểm
            user.LoyaltyWallet.Should().NotBeNull();
            user.LoyaltyWallet!.CurrentTier.Should().NotBeNull();
            user.LoyaltyWallet.CurrentTier!.TierName.Should().Be("Silver");
            user.LoyaltyWallet.CurrentBalance.Should().Be(50);
        }

        // =========================================================================
        // 8. CUSTOMER VIP (customer_vip) TEST SUITE
        // =========================================================================
        [Fact]
        public async Task Test8_CustomerVip_HasDiamondTierWithHighPoints()
        {
            // [x] Đăng nhập customer_vip / Admin@123
            var user = await _fixture.DbContext.Users
                .Include(u => u.LoyaltyWallet)
                    .ThenInclude(w => w!.CurrentTier)
                .FirstOrDefaultAsync(u => u.Username == "customer_vip");

            user.Should().NotBeNull();
            _passwordHasher.VerifyPassword(user!.PasswordHash, "Admin@123").Should().BeTrue();
            user.UserType.Should().Be(1, "Customer VIP có UserType = 1");

            // [x] Điểm và Hạng Thành Viên: Diamond + 1800 điểm (>= 1200 điểm)
            user.LoyaltyWallet.Should().NotBeNull();
            user.LoyaltyWallet!.CurrentTier.Should().NotBeNull();
            user.LoyaltyWallet.CurrentTier!.TierName.Should().Be("Diamond");
            user.LoyaltyWallet.CurrentBalance.Should().BeGreaterThanOrEqualTo(1200);
        }

        // =========================================================================
        // 9. CASHIER 2 (cashier02 - Chi nhánh Ba Đình)
        // =========================================================================
        [Fact]
        public async Task Test9_Cashier02_BranchScopeAndPermissions()
        {
            var user = await _fixture.DbContext.Users
                .Include(u => u.Store)
                .FirstOrDefaultAsync(u => u.Username == "cashier02");

            user.Should().NotBeNull();
            _passwordHasher.VerifyPassword(user!.PasswordHash, "Admin@123").Should().BeTrue();
            user.Store!.Code.Should().Be("SH-BD");

            var canPos = await _fixture.CheckPermissionAsync("cashier02", "POS", "ACCESS");
            var canSettings = await _fixture.CheckPermissionAsync("cashier02", "SETTINGS", "VIEW");
            canPos.Should().BeTrue();
            canSettings.Should().BeFalse();
        }

        // =========================================================================
        // 10. WAREHOUSE STAFF 2 (warehouse02 - Chi nhánh Ba Đình)
        // =========================================================================
        [Fact]
        public async Task Test10_Warehouse02_BranchScopeAndPermissions()
        {
            var user = await _fixture.DbContext.Users
                .Include(u => u.Store)
                .FirstOrDefaultAsync(u => u.Username == "warehouse02");

            user.Should().NotBeNull();
            _passwordHasher.VerifyPassword(user!.PasswordHash, "Admin@123").Should().BeTrue();
            user.Store!.Code.Should().Be("SH-BD");

            var canInventory = await _fixture.CheckPermissionAsync("warehouse02", "INVENTORY", "VIEW");
            var canPos = await _fixture.CheckPermissionAsync("warehouse02", "POS", "ACCESS");
            canInventory.Should().BeTrue();
            canPos.Should().BeFalse();
        }

        // =========================================================================
        // 11. CUSTOMER 2 (customer02 - Hạng Gold 400 điểm)
        // =========================================================================
        [Fact]
        public async Task Test11_Customer02_HasGoldTierWith400Points()
        {
            var user = await _fixture.DbContext.Users
                .Include(u => u.LoyaltyWallet)
                    .ThenInclude(w => w!.CurrentTier)
                .FirstOrDefaultAsync(u => u.Username == "customer02");

            user.Should().NotBeNull();
            _passwordHasher.VerifyPassword(user!.PasswordHash, "Admin@123").Should().BeTrue();
            user.LoyaltyWallet.Should().NotBeNull();
            user.LoyaltyWallet!.CurrentTier.Should().NotBeNull();
            user.LoyaltyWallet.CurrentTier!.TierName.Should().Be("Gold");
            user.LoyaltyWallet.CurrentBalance.Should().Be(400);

            var canAdmin = await _fixture.CheckPermissionAsync("customer02", "DASHBOARD", "VIEW");
            canAdmin.Should().BeFalse();
        }

        // =========================================================================
        // 12. LEGACY ADMIN (admin - Quản trị viên hệ thống)
        // =========================================================================
        [Fact]
        public async Task Test12_LegacyAdmin_FullSystemAccess()
        {
            var user = await _fixture.DbContext.Users.FirstOrDefaultAsync(u => u.Username == "admin");
            user.Should().NotBeNull();
            // Cho phép đăng nhập linh hoạt bằng cả Admin@123 hoặc Admin@123456
            var validPassword = _passwordHasher.VerifyPassword(user!.PasswordHash, "Admin@123") || 
                                _passwordHasher.VerifyPassword(user.PasswordHash, "Admin@123456");
            validPassword.Should().BeTrue();

            var canSettings = await _fixture.CheckPermissionAsync("admin", "SETTINGS", "VIEW");
            var canSec = await _fixture.CheckPermissionAsync("admin", "SECURITY", "PERMISSION_MANAGE");
            canSettings.Should().BeTrue();
            canSec.Should().BeTrue();
        }
    }
}

