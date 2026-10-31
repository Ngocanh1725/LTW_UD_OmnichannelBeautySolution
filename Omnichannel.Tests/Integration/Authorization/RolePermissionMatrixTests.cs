using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Omnichannel.Tests.Integration.Authorization
{
    [Collection("AuthTests")]
    public class RolePermissionMatrixTests : IClassFixture<AuthorizationTestFixture>
    {
        private readonly AuthorizationTestFixture _fixture;

        public RolePermissionMatrixTests(AuthorizationTestFixture fixture)
        {
            _fixture = fixture;
        }

        // =========================================================================
        // 1. DASHBOARD MODULE TESTS
        // =========================================================================
        [Fact]
        public async Task Dashboard_SuperAdmin_HasAccess()
        {
            var canView = await _fixture.CheckPermissionAsync("superadmin", "DASHBOARD", "VIEW");
            canView.Should().BeTrue("SuperAdmin có toàn quyền truy cập Dashboard");
        }

        [Fact]
        public async Task Dashboard_StoreManager_HasAccess()
        {
            var canView = await _fixture.CheckPermissionAsync("manager_hq", "DASHBOARD", "VIEW");
            canView.Should().BeTrue("StoreManager được phép xem Dashboard tổng quan chi nhánh");
        }

        [Fact]
        public async Task Dashboard_Cashier_HasNoAccess()
        {
            var canView = await _fixture.CheckPermissionAsync("cashier01", "DASHBOARD", "VIEW");
            canView.Should().BeFalse("Thu ngân không được phân quyền xem Dashboard doanh thu quản trị");
        }

        [Fact]
        public async Task Dashboard_WarehouseStaff_HasNoAccess()
        {
            var canView = await _fixture.CheckPermissionAsync("warehouse01", "DASHBOARD", "VIEW");
            canView.Should().BeFalse("Nhân viên kho chỉ làm việc với tồn kho, không xem Dashboard điều hành");
        }

        [Fact]
        public async Task Dashboard_Customer_HasNoAccess()
        {
            var canView = await _fixture.CheckPermissionAsync("customer01", "DASHBOARD", "VIEW");
            canView.Should().BeFalse("Khách hàng tuyệt đối không được truy cập Dashboard quản trị");
        }

        // =========================================================================
        // 2. PRODUCT & CATALOG MODULE TESTS
        // =========================================================================
        [Fact]
        public async Task Product_SuperAdmin_CanManageFully()
        {
            (await _fixture.CheckPermissionAsync("superadmin", "PRODUCT", "VIEW")).Should().BeTrue();
            (await _fixture.CheckPermissionAsync("superadmin", "PRODUCT", "CREATE")).Should().BeTrue();
            (await _fixture.CheckPermissionAsync("superadmin", "PRODUCT", "EDIT")).Should().BeTrue();
            (await _fixture.CheckPermissionAsync("superadmin", "PRODUCT", "DELETE")).Should().BeTrue();
        }

        [Fact]
        public async Task Product_StoreManager_CanView_CannotCreateOrDelete()
        {
            (await _fixture.CheckPermissionAsync("manager_cg", "PRODUCT", "VIEW")).Should().BeTrue("Store Manager xem được danh mục sản phẩm");
            (await _fixture.CheckPermissionAsync("manager_cg", "PRODUCT", "CREATE")).Should().BeFalse("Store Manager không được tạo sản phẩm toàn hệ thống");
            (await _fixture.CheckPermissionAsync("manager_cg", "PRODUCT", "DELETE")).Should().BeFalse("Store Manager không được xóa sản phẩm");
        }

        [Fact]
        public async Task Product_Cashier_CanViewOnly()
        {
            (await _fixture.CheckPermissionAsync("cashier01", "PRODUCT", "VIEW")).Should().BeTrue("Thu ngân cần tra cứu sản phẩm");
            (await _fixture.CheckPermissionAsync("cashier01", "PRODUCT", "CREATE")).Should().BeFalse();
            (await _fixture.CheckPermissionAsync("cashier01", "PRODUCT", "DELETE")).Should().BeFalse();
        }

        // =========================================================================
        // 3. POS MODULE TESTS
        // =========================================================================
        [Fact]
        public async Task Pos_SuperAdmin_HasFullPosAccess()
        {
            (await _fixture.CheckPermissionAsync("superadmin", "POS", "ACCESS")).Should().BeTrue();
            (await _fixture.CheckPermissionAsync("superadmin", "POS", "CHECKOUT")).Should().BeTrue();
            (await _fixture.CheckPermissionAsync("superadmin", "POS", "REFUND")).Should().BeTrue();
        }

        [Fact]
        public async Task Pos_Cashier_CanCheckout_CannotRefund()
        {
            (await _fixture.CheckPermissionAsync("cashier01", "POS", "ACCESS")).Should().BeTrue("Thu ngân có quyền truy cập màn hình POS");
            (await _fixture.CheckPermissionAsync("cashier01", "POS", "CHECKOUT")).Should().BeTrue("Thu ngân có quyền thanh toán đơn POS");
            (await _fixture.CheckPermissionAsync("cashier01", "POS", "REFUND")).Should().BeFalse("Thu ngân KHÔNG được tự ý hoàn tiền trả hàng");
        }

        [Fact]
        public async Task Pos_StoreManager_CanRefund()
        {
            (await _fixture.CheckPermissionAsync("manager_cg", "POS", "REFUND")).Should().BeTrue("Cửa hàng trưởng có quyền phê duyệt hoàn tiền POS");
        }

        [Fact]
        public async Task Pos_WarehouseStaff_HasNoPosAccess()
        {
            (await _fixture.CheckPermissionAsync("warehouse01", "POS", "ACCESS")).Should().BeFalse("Nhân viên kho không bán lẻ POS");
        }

        // =========================================================================
        // 4. STOCK TRANSFER MODULE TESTS
        // =========================================================================
        [Fact]
        public async Task StockTransfer_SuperAdmin_CanApprove()
        {
            (await _fixture.CheckPermissionAsync("superadmin", "TRANSFER", "APPROVE")).Should().BeTrue("SuperAdmin phê duyệt lệnh chuyển kho");
        }

        [Fact]
        public async Task StockTransfer_StoreManager_CanCreate_CannotApprove()
        {
            (await _fixture.CheckPermissionAsync("manager_cg", "TRANSFER", "CREATE")).Should().BeTrue("Cửa hàng trưởng tạo phiếu yêu cầu chuyển kho");
            (await _fixture.CheckPermissionAsync("manager_cg", "TRANSFER", "APPROVE")).Should().BeFalse("Cửa hàng trưởng không được tự duyệt xuất kho tổng");
        }

        [Fact]
        public async Task StockTransfer_Cashier_HasNoAccess()
        {
            (await _fixture.CheckPermissionAsync("cashier01", "TRANSFER", "CREATE")).Should().BeFalse("Thu ngân không được điều phối kho");
        }

        // =========================================================================
        // 5. CMS MODULE TESTS
        // =========================================================================
        [Fact]
        public async Task Cms_ContentEditor_HasFullCmsRights()
        {
            (await _fixture.CheckPermissionAsync("editor01", "CMS", "POST_MANAGE")).Should().BeTrue();
            (await _fixture.CheckPermissionAsync("editor01", "CMS", "BANNER_MANAGE")).Should().BeTrue();
            (await _fixture.CheckPermissionAsync("editor01", "CMS", "REVIEW_MODERATE")).Should().BeTrue("Biên tập viên có quyền duyệt review UGC");
        }

        [Fact]
        public async Task Cms_CashierAndWarehouse_HaveNoCmsRights()
        {
            (await _fixture.CheckPermissionAsync("cashier01", "CMS", "POST_MANAGE")).Should().BeFalse();
            (await _fixture.CheckPermissionAsync("warehouse01", "CMS", "REVIEW_MODERATE")).Should().BeFalse();
        }

        // =========================================================================
        // 6. FINANCE MODULE TESTS
        // =========================================================================
        [Fact]
        public async Task Finance_SuperAdmin_HasFullFinanceAccess()
        {
            (await _fixture.CheckPermissionAsync("superadmin", "FINANCE", "LEDGER_VIEW")).Should().BeTrue();
            (await _fixture.CheckPermissionAsync("superadmin", "FINANCE", "PNL_VIEW")).Should().BeTrue();
            (await _fixture.CheckPermissionAsync("superadmin", "FINANCE", "SUPPLIER_DEBT")).Should().BeTrue();
        }

        [Fact]
        public async Task Finance_StoreManager_CanViewLedger_CannotManageSupplierDebt()
        {
            (await _fixture.CheckPermissionAsync("manager_cg", "FINANCE", "LEDGER_VIEW")).Should().BeTrue("Store Manager xem sổ thu chi chi nhánh");
            (await _fixture.CheckPermissionAsync("manager_cg", "FINANCE", "SUPPLIER_DEBT")).Should().BeFalse("Store Manager không quản lý công nợ NCC cấp tổng");
        }

        [Fact]
        public async Task Finance_Cashier_HasNoAccess()
        {
            (await _fixture.CheckPermissionAsync("cashier01", "FINANCE", "LEDGER_VIEW")).Should().BeFalse("Thu ngân không được xem sổ cái tài chính");
        }

        // =========================================================================
        // 7. SECURITY & SETTINGS MODULE TESTS
        // =========================================================================
        [Fact]
        public async Task Security_OnlySuperAdmin_CanManageUsersAndSettings()
        {
            (await _fixture.CheckPermissionAsync("superadmin", "USER", "MANAGE")).Should().BeTrue();
            (await _fixture.CheckPermissionAsync("superadmin", "SETTINGS", "MANAGE")).Should().BeTrue();

            (await _fixture.CheckPermissionAsync("manager_cg", "USER", "MANAGE")).Should().BeFalse();
            (await _fixture.CheckPermissionAsync("manager_cg", "SETTINGS", "MANAGE")).Should().BeFalse();

            (await _fixture.CheckPermissionAsync("cashier01", "USER", "MANAGE")).Should().BeFalse();
            (await _fixture.CheckPermissionAsync("editor01", "SETTINGS", "MANAGE")).Should().BeFalse();
        }
    }
}
