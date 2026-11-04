using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Domain.Entities;

namespace Omnichannel.Infrastructure.Data
{
    public static partial class DbInitializer
    {
        private static void LogInfo(ILogger? logger, string message)
        {
            logger?.LogInformation("{Message}", message);
        }

        private static void LogWarning(ILogger? logger, string message)
        {
            logger?.LogWarning("{Message}", message);
        }

        private static void LogError(ILogger? logger, Exception? ex, string message)
        {
            if (logger != null)
            {
                logger.LogError(ex, "{Message}", message);
            }
        }

        public static async Task SeedAsync(ApplicationDbContext context, ILogger? logger = null)
        {
            if (context.Database.IsRelational())
            {
                await context.Database.MigrateAsync();

                // Chỉ chạy kiểm tra schema bổ sung nếu bảng Products chưa có dữ liệu (DB mới khởi tạo lần đầu)
                bool isDatabaseInitialized = await context.Products.AnyAsync();
                if (!isDatabaseInitialized)
                {
                    await EnsureChatTablesExistAsync(context);
                    await EnsureSkinConcernTablesExistAsync(context);
                    await EnsureAlertTablesExistAsync(context);
                    await EnsureNotificationTablesExistAsync(context);
                    await EnsureInternalMessageTablesExistAsync(context);
                    await EnsureCategoryIconColumnExistAsync(context);
                    await EnsureBrandTablesExistAsync(context);
                    await EnsureProductBrandColumnExistAsync(context);
                    await EnsureProductVariantColumnsExistAsync(context);
                    await EnsureProductBatchFefoColumnsExistAsync(context, logger);
                    await EnsureWishlistAndStockAlertTablesExistAsync(context, logger);
                    await EnsureProductReviewColumnsAndSeedAsync(context, logger);
                    await EnsureRowVersionColumnsExistAsync(context, logger);
                }

                // Đảm bảo các chỉ mục (indexes) hiệu năng cao luôn sẵn sàng
                await EnsureIndexesExistAsync(context, logger);
            }

            // Đảm bảo dữ liệu Dynamic CMS, Theme Tokens & Sections luôn sẵn sàng
            await EnsureDynamicCmsDataSeededAsync(context, logger);

            // Đảm bảo các cột Clean Beauty và chuỗi FullIngredients luôn sẵn sàng
            await EnsureCleanBeautyColumnsExistAsync(context, logger);

            // Đảm bảo cột RoutineStep và Coupon COMBO15 luôn sẵn sàng
            await EnsureRoutineStepAndComboSeededAsync(context, logger);

            // Seed dữ liệu chỉ chạy khi bảng rỗng (nếu đã có Products và Roles thì hệ thống đã khởi tạo hoàn tất)
            if (await context.Products.AnyAsync() && await context.Roles.AnyAsync())
            {
                // Đảm bảo đồng bộ lô hàng FEFO thật (Date xa & Cận date) cho toàn bộ sản phẩm
                await EnsureFefoBatchesSynchronizedAsync(context, logger);
                // Đảm bảo tồn kho showroom O2O và chiến dịch Flash Sale luôn sẵn sàng
                await EnsureFlashSaleAndShowroomStockSeededAsync(context, logger);
                // Đảm bảo dữ liệu demo chuẩn hóa (150+ đơn 90 ngày, 200+ review, 30+ khách hàng, voucher)
                await EnsureRealisticDemoDataSeededAsync(context, logger);
                await EnsureOrderStatusHistoriesAndReturnsSeededAsync(context, logger);
                await EnsureLoyaltyGamificationSeededAsync(context, logger);
                LogInfo(logger, "[DbInitializer] Cơ sở dữ liệu đã có sẵn dữ liệu nghiệp vụ (Roles, Products). Đã đồng bộ lô FEFO thật và dữ liệu Demo chuẩn hóa.");
                return;
            }

            // Thực hiện seed từng phân hệ nếu chưa có dữ liệu
            await SeedIdentityAndSecurityAsync(context);
            await SeedStoresAsync(context);
            await SeedUsersAndRolesAsync(context);
            await SeedSuppliersAsync(context);
            await SeedCategoriesAsync(context);
            await SeedBrandsAsync(context);
            await SeedSkinTypesAndIngredientsAsync(context);
            await SeedProductsAndBatchesAsync(context);
            await ProductVariantSeedData.SeedVariantsAndInventoryAsync(context);
            await SeedFefoWarehouseBatchesAsync(context);
            await SeedMemberTiersAndLoyaltyAsync(context);
            await SeedCustomerProfilesAndCrmAsync(context);
            await SeedPromotionsAndCouponsAsync(context);
            await SeedPosShiftsAndOrdersAsync(context);
            await SeedStockTransfersAsync(context);
            await SeedCmsDataAsync(context);
            await SeedFinanceDataAsync(context);
            await SeedDefaultSettingsAsync(context);
            await SeedAuditLogsAsync(context);
            await SeedChatbotDataAsync(context);
            await SeedSkinConcernsAndProductAttributesAsync(context);
            await SeedInitialAlertsAsync(context);
            await SeedInitialNotificationsAsync(context);
            await SeedInitialInternalMessagesAsync(context);

            // Đảm bảo đồng bộ lô hàng FEFO thật và tồn kho showroom O2O hoàn tất
            await EnsureFefoBatchesSynchronizedAsync(context, logger);
            await EnsureFlashSaleAndShowroomStockSeededAsync(context, logger);
            await EnsureRealisticDemoDataSeededAsync(context, logger);
            await EnsureOrderStatusHistoriesAndReturnsSeededAsync(context, logger);
        }

        private static string HashPassword(string password)
        {
            var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<string>();
            return hasher.HashPassword("OmnichannelSystemUser", password);
        }

        // =========================================================================
        // 1. SEED ROLES, PERMISSIONS & ROLE_PERMISSIONS
        // =========================================================================
        private static async Task SeedIdentityAndSecurityAsync(ApplicationDbContext context)
        {
            // Seed Roles (6 vai trò cốt lõi)
            if (!await context.Roles.AnyAsync())
            {
                var roles = new List<Role>
                {
                    new() { Name = "SuperAdmin", Description = "Quản trị viên toàn quyền hệ thống, quản lý tất cả chi nhánh" },
                    new() { Name = "StoreManager", Description = "Quản lý chi nhánh, xem báo cáo và điều hành kho chi nhánh" },
                    new() { Name = "Cashier", Description = "Thu ngân điểm bán lẻ POS, tạo đơn và xem tồn kho chi nhánh" },
                    new() { Name = "WarehouseStaff", Description = "Thủ kho, nhập xuất kho, kiểm kê và điều chuyển hàng" },
                    new() { Name = "ContentEditor", Description = "Biên tập viên nội dung CMS, banner, bài viết và kiểm duyệt review" },
                    new() { Name = "Customer", Description = "Khách hàng mua sắm đa kênh, tích điểm thành viên" }
                };

                await context.Roles.AddRangeAsync(roles);
                await context.SaveChangesAsync();
            }

            // Seed Permissions (51 permissions toàn diện)
            var allPermissions = new List<Permission>
            {
                // Module DASHBOARD
                new() { Code = "DASHBOARD_VIEW", Name = "Xem dashboard tổng quan", Module = "DASHBOARD" },
                new() { Code = "DASHBOARD_ANALYTICS", Name = "Xem phân tích nâng cao", Module = "DASHBOARD" },

                // Module PRODUCT
                new() { Code = "PRODUCT_VIEW", Name = "Xem danh sách sản phẩm", Module = "PRODUCT" },
                new() { Code = "PRODUCT_CREATE", Name = "Tạo mới sản phẩm", Module = "PRODUCT" },
                new() { Code = "PRODUCT_EDIT", Name = "Chỉnh sửa sản phẩm", Module = "PRODUCT" },
                new() { Code = "PRODUCT_DELETE", Name = "Xóa sản phẩm", Module = "PRODUCT" },
                new() { Code = "PRODUCT_IMPORT", Name = "Import danh sách sản phẩm", Module = "PRODUCT" },

                // Module INVENTORY
                new() { Code = "INVENTORY_VIEW", Name = "Xem tồn kho FEFO", Module = "INVENTORY" },
                new() { Code = "INVENTORY_IMPORT", Name = "Nhập kho theo lô", Module = "INVENTORY" },
                new() { Code = "INVENTORY_EXPORT", Name = "Xuất hủy / xuất kho", Module = "INVENTORY" },
                new() { Code = "INVENTORY_ADJUST", Name = "Điều chỉnh tồn kho kiểm kê", Module = "INVENTORY" },
                new() { Code = "INVENTORY_STOCKTAKE", Name = "Thực hiện kiểm kê kho", Module = "INVENTORY" },

                // Module POS
                new() { Code = "POS_ACCESS", Name = "Truy cập giao diện POS", Module = "POS" },
                new() { Code = "POS_CHECKOUT", Name = "Thanh toán đơn POS", Module = "POS" },
                new() { Code = "POS_REFUND", Name = "Hoàn tiền trả hàng POS", Module = "POS" },
                new() { Code = "POS_SHIFT_OPEN", Name = "Mở ca bán hàng POS", Module = "POS" },
                new() { Code = "POS_SHIFT_CLOSE", Name = "Đóng ca bán hàng POS", Module = "POS" },

                // Module ORDER
                new() { Code = "ORDER_VIEW", Name = "Xem danh sách đơn hàng", Module = "ORDER" },
                new() { Code = "ORDER_CREATE", Name = "Tạo mới đơn hàng", Module = "ORDER" },
                new() { Code = "ORDER_EDIT", Name = "Chỉnh sửa đơn hàng", Module = "ORDER" },
                new() { Code = "ORDER_CANCEL", Name = "Hủy đơn hàng", Module = "ORDER" },

                // Module CUSTOMER
                new() { Code = "CUSTOMER_VIEW", Name = "Xem danh sách khách hàng", Module = "CUSTOMER" },
                new() { Code = "CUSTOMER_CREATE", Name = "Tạo mới khách hàng", Module = "CUSTOMER" },
                new() { Code = "CUSTOMER_EDIT", Name = "Chỉnh sửa thông tin khách hàng", Module = "CUSTOMER" },
                new() { Code = "CUSTOMER_PROFILE_VIEW", Name = "Xem hồ sơ sắc đẹp khách hàng", Module = "CUSTOMER" },

                // Module CMS
                new() { Code = "CMS_POST_MANAGE", Name = "Quản lý bài viết và danh mục tin", Module = "CMS" },
                new() { Code = "CMS_BANNER_MANAGE", Name = "Quản lý Banner quảng cáo", Module = "CMS" },
                new() { Code = "CMS_MENU_MANAGE", Name = "Quản lý Menu điều hướng đa cấp", Module = "CMS" },
                new() { Code = "CMS_REVIEW_MODERATE", Name = "Kiểm duyệt đánh giá sản phẩm", Module = "CMS" },
                new() { Code = "CMS_STATIC_PAGE_MANAGE", Name = "Quản lý trang tĩnh", Module = "CMS" },
                new() { Code = "THEME_MANAGE", Name = "Quản lý giao diện, bảng màu và nhận diện thương hiệu", Module = "CMS" },


                // Module REPORT
                new() { Code = "REPORT_SALES", Name = "Xem báo cáo doanh số", Module = "REPORT" },
                new() { Code = "REPORT_INVENTORY", Name = "Xem báo cáo tồn kho & cận date", Module = "REPORT" },
                new() { Code = "REPORT_FINANCE", Name = "Xem báo cáo tài chính P&L", Module = "REPORT" },

                // Module FINANCE
                new() { Code = "FINANCE_LEDGER_VIEW", Name = "Xem sổ cái thu chi", Module = "FINANCE" },
                new() { Code = "FINANCE_SUPPLIER_DEBT", Name = "Quản lý công nợ nhà cung cấp", Module = "FINANCE" },
                new() { Code = "FINANCE_COD_RECONCILE", Name = "Đối soát tiền COD", Module = "FINANCE" },
                new() { Code = "FINANCE_PNL_VIEW", Name = "Xem báo cáo P&L chi nhánh", Module = "FINANCE" },

                // Module PROMOTION
                new() { Code = "PROMOTION_MANAGE", Name = "Quản lý chương trình khuyến mãi", Module = "PROMOTION" },
                new() { Code = "COUPON_MANAGE", Name = "Quản lý mã coupon giảm giá", Module = "PROMOTION" },
                new() { Code = "VOUCHER_MANAGE", Name = "Quản lý phát hành voucher cá nhân", Module = "PROMOTION" },

                // Module LOYALTY
                new() { Code = "LOYALTY_VIEW", Name = "Xem thông tin ví điểm & hạng thành viên", Module = "LOYALTY" },
                new() { Code = "LOYALTY_ADJUST_POINTS", Name = "Điều chỉnh điểm thưởng thủ công", Module = "LOYALTY" },
                new() { Code = "LOYALTY_TIER_MANAGE", Name = "Quản trị chính sách hạng thành viên", Module = "LOYALTY" },

                // Module STOCK_TRANSFER
                new() { Code = "TRANSFER_CREATE", Name = "Tạo phiếu điều chuyển kho", Module = "STOCK_TRANSFER" },
                new() { Code = "TRANSFER_APPROVE", Name = "Phê duyệt phiếu điều chuyển", Module = "STOCK_TRANSFER" },
                new() { Code = "TRANSFER_RECEIVE", Name = "Xác nhận nhập kho chuyển đến", Module = "STOCK_TRANSFER" },

                // Module SETTINGS
                new() { Code = "SETTINGS_VIEW", Name = "Xem cấu hình hệ thống", Module = "SETTINGS" },
                new() { Code = "SETTINGS_MANAGE", Name = "Chỉnh sửa cấu hình hệ thống", Module = "SETTINGS" },

                // Module SECURITY
                new() { Code = "USER_MANAGE", Name = "Quản lý người dùng hệ thống", Module = "SECURITY" },
                new() { Code = "ROLE_MANAGE", Name = "Quản lý vai trò", Module = "SECURITY" },
                new() { Code = "PERMISSION_MANAGE", Name = "Quản lý phân quyền chi tiết", Module = "SECURITY" }
            };

            foreach (var perm in allPermissions)
            {
                if (!await context.Permissions.AnyAsync(p => p.Code == perm.Code))
                {
                    await context.Permissions.AddAsync(perm);
                }
            }
            await context.SaveChangesAsync();

            // Seed RolePermissions (Ma trận phân quyền chi tiết)
            if (!await context.RolePermissions.AnyAsync())
            {
                var rolesInDb = await context.Roles.ToDictionaryAsync(r => r.Name, r => r.Id);
                var permsInDb = await context.Permissions.ToDictionaryAsync(p => p.Code, p => p.Id);

                // 1. SuperAdmin: Full tất cả permissions
                if (rolesInDb.TryGetValue("SuperAdmin", out var superAdminRoleId))
                {
                    foreach (var p in permsInDb.Values)
                    {
                        await context.RolePermissions.AddAsync(new RolePermission { RoleId = superAdminRoleId, PermissionId = p });
                    }
                }

                // 2. StoreManager: Quản lý chi nhánh
                if (rolesInDb.TryGetValue("StoreManager", out var managerRoleId))
                {
                    var managerPermCodes = new[]
                    {
                        "DASHBOARD_VIEW",
                        "PRODUCT_VIEW",
                        "INVENTORY_VIEW", "INVENTORY_ADJUST", "INVENTORY_STOCKTAKE",
                        "POS_ACCESS", "POS_CHECKOUT", "POS_REFUND", "POS_SHIFT_OPEN", "POS_SHIFT_CLOSE",
                        "ORDER_VIEW", "ORDER_CREATE", "ORDER_EDIT", "ORDER_CANCEL",
                        "CUSTOMER_VIEW", "CUSTOMER_CREATE", "CUSTOMER_EDIT", "CUSTOMER_PROFILE_VIEW",
                        "REPORT_SALES", "REPORT_INVENTORY",
                        "FINANCE_LEDGER_VIEW", "FINANCE_PNL_VIEW",
                        "VOUCHER_MANAGE",
                        "LOYALTY_VIEW", "LOYALTY_ADJUST_POINTS",
                        "TRANSFER_CREATE", "TRANSFER_RECEIVE"
                    };

                    foreach (var code in managerPermCodes)
                    {
                        if (permsInDb.TryGetValue(code, out var pId))
                            await context.RolePermissions.AddAsync(new RolePermission { RoleId = managerRoleId, PermissionId = pId });
                    }
                }

                // 3. Cashier: Thu ngân POS
                if (rolesInDb.TryGetValue("Cashier", out var cashierRoleId))
                {
                    var cashierPermCodes = new[]
                    {
                        "PRODUCT_VIEW",
                        "INVENTORY_VIEW",
                        "POS_ACCESS", "POS_CHECKOUT", "POS_SHIFT_OPEN", "POS_SHIFT_CLOSE",
                        "ORDER_VIEW", "ORDER_CREATE",
                        "CUSTOMER_VIEW", "CUSTOMER_CREATE",
                        "LOYALTY_VIEW"
                    };

                    foreach (var code in cashierPermCodes)
                    {
                        if (permsInDb.TryGetValue(code, out var pId))
                            await context.RolePermissions.AddAsync(new RolePermission { RoleId = cashierRoleId, PermissionId = pId });
                    }
                }

                // 4. WarehouseStaff: Thủ kho
                if (rolesInDb.TryGetValue("WarehouseStaff", out var warehouseRoleId))
                {
                    var warehousePermCodes = new[]
                    {
                        "PRODUCT_VIEW", "PRODUCT_IMPORT",
                        "INVENTORY_VIEW", "INVENTORY_IMPORT", "INVENTORY_EXPORT", "INVENTORY_ADJUST", "INVENTORY_STOCKTAKE",
                        "REPORT_INVENTORY",
                        "TRANSFER_CREATE", "TRANSFER_RECEIVE"
                    };

                    foreach (var code in warehousePermCodes)
                    {
                        if (permsInDb.TryGetValue(code, out var pId))
                            await context.RolePermissions.AddAsync(new RolePermission { RoleId = warehouseRoleId, PermissionId = pId });
                    }
                }

                // 5. ContentEditor: Biên tập nội dung CMS
                if (rolesInDb.TryGetValue("ContentEditor", out var editorRoleId))
                {
                    var editorPermCodes = new[]
                    {
                        "PRODUCT_VIEW",
                        "CMS_POST_MANAGE", "CMS_BANNER_MANAGE", "CMS_MENU_MANAGE", "CMS_REVIEW_MODERATE", "CMS_STATIC_PAGE_MANAGE", "THEME_MANAGE"
                    };

                    foreach (var code in editorPermCodes)
                    {
                        if (permsInDb.TryGetValue(code, out var pId))
                            await context.RolePermissions.AddAsync(new RolePermission { RoleId = editorRoleId, PermissionId = pId });
                    }
                }

                // 6. Customer: Khách hàng mua sắm
                if (rolesInDb.TryGetValue("Customer", out var customerRoleId))
                {
                    var customerPermCodes = new[]
                    {
                        "ORDER_VIEW", "ORDER_CREATE", "ORDER_CANCEL",
                        "CUSTOMER_PROFILE_VIEW",
                        "LOYALTY_VIEW"
                    };

                    foreach (var code in customerPermCodes)
                    {
                        if (permsInDb.TryGetValue(code, out var pId))
                            await context.RolePermissions.AddAsync(new RolePermission { RoleId = customerRoleId, PermissionId = pId });
                    }
                }

                await context.SaveChangesAsync();
            }
        }

        // =========================================================================
        // 2. SEED STORES (3 CHI NHÁNH CHÍNH THEO TIÊU CHUẨN FEFO)
        // =========================================================================
        private static async Task SeedStoresAsync(ApplicationDbContext context)
        {
            // Chi nhánh 1: OMNI BEAUTY - Flagship Showroom Quận 1 (128 Hai Bà Trưng, P. Bến Nghé, Q.1, TP.HCM)
            var store1 = await context.Stores.FirstOrDefaultAsync(s => s.Id == 1 || s.Code == "SH-Q1" || s.Code == "HQ-001");
            if (store1 == null)
            {
                store1 = new Store { Code = "SH-Q1" };
                await context.Stores.AddAsync(store1);
            }
            store1.Code = "SH-Q1";
            store1.Name = "OMNI BEAUTY - Flagship Showroom Quận 1";
            store1.Address = "128 Hai Bà Trưng, P. Bến Nghé, Q.1, TP.HCM";
            store1.PhoneNumber = "02838221199";
            store1.Email = "flagship.q1@omnichannelbeauty.vn";
            store1.Area = 250m;
            store1.IsHeadquarters = false;
            store1.IsActive = true;

            // Chi nhánh 2: OMNI BEAUTY - Showroom Ba Đình (86 Trần Phú, Q. Ba Đình, Hà Nội)
            var store2 = await context.Stores.FirstOrDefaultAsync(s => s.Id == 2 || s.Code == "SH-BD" || s.Code == "SH-CG");
            if (store2 == null)
            {
                store2 = new Store { Code = "SH-BD" };
                await context.Stores.AddAsync(store2);
            }
            store2.Code = "SH-BD";
            store2.Name = "OMNI BEAUTY - Showroom Ba Đình";
            store2.Address = "86 Trần Phú, Q. Ba Đình, Hà Nội";
            store2.PhoneNumber = "02437665588";
            store2.Email = "badinh@omnichannelbeauty.vn";
            store2.Area = 180m;
            store2.IsHeadquarters = false;
            store2.IsActive = true;

            // Chi nhánh 3: OMNI BEAUTY - Tổng Kho Phân Phối Miền Nam (KCN Tân Bình, TP.HCM)
            var store3 = await context.Stores.FirstOrDefaultAsync(s => s.Id == 3 || s.Code == "WH-MN" || s.Code == "SH-TD");
            if (store3 == null)
            {
                store3 = new Store { Code = "WH-MN" };
                await context.Stores.AddAsync(store3);
            }
            store3.Code = "WH-MN";
            store3.Name = "OMNI BEAUTY - Tổng Kho Phân Phối Miền Nam";
            store3.Address = "KCN Tân Bình, TP.HCM";
            store3.PhoneNumber = "02838150000";
            store3.Email = "tongkho.miennam@omnichannelbeauty.vn";
            store3.Area = 1500m;
            store3.IsHeadquarters = true;
            store3.IsActive = true;

            await context.SaveChangesAsync();
        }

        // =========================================================================
        // 3. SEED USERS & USER_ROLES (11 TÀI KHOẢN TEST PRODUCTION-GRADE)
        // Mật khẩu mặc định toàn hệ thống: Admin@123
        // =========================================================================
        private static async Task SeedUsersAndRolesAsync(ApplicationDbContext context)
        {
            var stores = await context.Stores.ToDictionaryAsync(s => s.Code, s => s.Id);
            var storeQ1Id = stores.GetValueOrDefault("SH-Q1") != 0 ? stores["SH-Q1"] : (stores.GetValueOrDefault("HQ-001") != 0 ? stores["HQ-001"] : 1);
            var storeBdId = stores.GetValueOrDefault("SH-BD") != 0 ? stores["SH-BD"] : (stores.GetValueOrDefault("SH-CG") != 0 ? stores["SH-CG"] : 2);
            var storeMnId = stores.GetValueOrDefault("WH-MN") != 0 ? stores["WH-MN"] : (stores.GetValueOrDefault("SH-TD") != 0 ? stores["SH-TD"] : 3);

            var roles = await context.Roles.ToDictionaryAsync(r => r.Name, r => r.Id);

            var defaultPasswordHash = HashPassword("Admin@123");

            var testUsers = new List<(User User, string RoleName)>
            {
                (
                    new User
                    {
                        Username = "superadmin",
                        Email = "superadmin@omnichannelbeauty.vn",
                        FullName = "Nguyễn Quản Trị",
                        PasswordHash = defaultPasswordHash,
                        PhoneNumber = "0909000001",
                        AvatarUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=300&h=300&fit=crop",
                        UserType = 2,
                        IsActive = true,
                        StoreId = null
                    },
                    "SuperAdmin"
                ),
                (
                    new User
                    {
                        Username = "manager_hq",
                        Email = "manager.hq@omnichannelbeauty.vn",
                        FullName = "Trần Quản Lý HQ",
                        PasswordHash = defaultPasswordHash,
                        PhoneNumber = "0909000002",
                        AvatarUrl = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=300&h=300&fit=crop",
                        UserType = 2,
                        IsActive = true,
                        StoreId = storeQ1Id
                    },
                    "StoreManager"
                ),
                (
                    new User
                    {
                        Username = "manager_cg",
                        Email = "manager.cg@omnichannelbeauty.vn",
                        FullName = "Lê Quản Lý CG",
                        PasswordHash = defaultPasswordHash,
                        PhoneNumber = "0909000003",
                        AvatarUrl = "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=300&h=300&fit=crop",
                        UserType = 2,
                        IsActive = true,
                        StoreId = storeBdId
                    },
                    "StoreManager"
                ),
                (
                    new User
                    {
                        Username = "cashier01",
                        Email = "cashier01@omnichannelbeauty.vn",
                        FullName = "Phạm Thu Ngân 01",
                        PasswordHash = defaultPasswordHash,
                        PhoneNumber = "0909000004",
                        AvatarUrl = "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=300&h=300&fit=crop",
                        UserType = 2,
                        IsActive = true,
                        StoreId = storeQ1Id
                    },
                    "Cashier"
                ),
                (
                    new User
                    {
                        Username = "cashier02",
                        Email = "cashier02@omnichannelbeauty.vn",
                        FullName = "Hoàng Thu Ngân 02",
                        PasswordHash = defaultPasswordHash,
                        PhoneNumber = "0909000005",
                        AvatarUrl = "https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=300&h=300&fit=crop",
                        UserType = 2,
                        IsActive = true,
                        StoreId = storeBdId
                    },
                    "Cashier"
                ),
                (
                    new User
                    {
                        Username = "warehouse01",
                        Email = "warehouse01@omnichannelbeauty.vn",
                        FullName = "Đặng Thủ Kho 01",
                        PasswordHash = defaultPasswordHash,
                        PhoneNumber = "0909000006",
                        AvatarUrl = "https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=300&h=300&fit=crop",
                        UserType = 2,
                        IsActive = true,
                        StoreId = storeMnId
                    },
                    "WarehouseStaff"
                ),
                (
                    new User
                    {
                        Username = "warehouse02",
                        Email = "warehouse02@omnichannelbeauty.vn",
                        FullName = "Vũ Thủ Kho 02",
                        PasswordHash = defaultPasswordHash,
                        PhoneNumber = "0909000007",
                        AvatarUrl = "https://images.unsplash.com/photo-1519085360753-af0119f7cbe7?w=300&h=300&fit=crop",
                        UserType = 2,
                        IsActive = true,
                        StoreId = storeBdId
                    },
                    "WarehouseStaff"
                ),
                (
                    new User
                    {
                        Username = "editor01",
                        Email = "editor01@omnichannelbeauty.vn",
                        FullName = "Bùi Biên Tập",
                        PasswordHash = defaultPasswordHash,
                        PhoneNumber = "0909000008",
                        AvatarUrl = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=300&h=300&fit=crop",
                        UserType = 2,
                        IsActive = true,
                        StoreId = null
                    },
                    "ContentEditor"
                ),
                (
                    new User
                    {
                        Username = "customer01",
                        Email = "customer01@gmail.com",
                        FullName = "Ngọc Khách Hàng 01",
                        PasswordHash = defaultPasswordHash,
                        PhoneNumber = "0909000009",
                        AvatarUrl = "https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=300&h=300&fit=crop",
                        UserType = 1,
                        IsActive = true,
                        StoreId = null
                    },
                    "Customer"
                ),
                (
                    new User
                    {
                        Username = "customer02",
                        Email = "customer02@gmail.com",
                        FullName = "Mai Khách Hàng 02",
                        PasswordHash = defaultPasswordHash,
                        PhoneNumber = "0909000010",
                        AvatarUrl = "https://images.unsplash.com/photo-1517841905240-472988babdf9?w=300&h=300&fit=crop",
                        UserType = 1,
                        IsActive = true,
                        StoreId = null
                    },
                    "Customer"
                ),
                (
                    new User
                    {
                        Username = "customer_vip",
                        Email = "customer.vip@gmail.com",
                        FullName = "Linh VIP Diamond",
                        PasswordHash = defaultPasswordHash,
                        PhoneNumber = "0909000011",
                        AvatarUrl = "https://images.unsplash.com/photo-1524504388940-b1c1722653e1?w=300&h=300&fit=crop",
                        UserType = 1,
                        IsActive = true,
                        StoreId = null
                    },
                    "Customer"
                ),
                (
                    new User
                    {
                        Username = "customer_diamond",
                        Email = "customer.diamond@omnichannelbeauty.vn",
                        FullName = "Linh VIP Kim Cương",
                        PasswordHash = defaultPasswordHash,
                        PhoneNumber = "0909111001",
                        AvatarUrl = "https://images.unsplash.com/photo-1524504388940-b1c1722653e1?w=300&h=300&fit=crop",
                        UserType = 1,
                        IsActive = true,
                        StoreId = null
                    },
                    "Customer"
                ),
                (
                    new User
                    {
                        Username = "customer_platinum",
                        Email = "customer.platinum@omnichannelbeauty.vn",
                        FullName = "Hoàng VIP Bạch Kim",
                        PasswordHash = defaultPasswordHash,
                        PhoneNumber = "0909111002",
                        AvatarUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=300&h=300&fit=crop",
                        UserType = 1,
                        IsActive = true,
                        StoreId = null
                    },
                    "Customer"
                ),
                (
                    new User
                    {
                        Username = "customer_gold",
                        Email = "customer.gold@omnichannelbeauty.vn",
                        FullName = "Trần Thành Viên Vàng",
                        PasswordHash = defaultPasswordHash,
                        PhoneNumber = "0909111003",
                        AvatarUrl = "https://images.unsplash.com/photo-1517841905240-472988babdf9?w=300&h=300&fit=crop",
                        UserType = 1,
                        IsActive = true,
                        StoreId = null
                    },
                    "Customer"
                ),
                (
                    new User
                    {
                        Username = "customer_silver",
                        Email = "customer.silver@omnichannelbeauty.vn",
                        FullName = "Lê Thành Viên Bạc",
                        PasswordHash = defaultPasswordHash,
                        PhoneNumber = "0909111004",
                        AvatarUrl = "https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=300&h=300&fit=crop",
                        UserType = 1,
                        IsActive = true,
                        StoreId = null
                    },
                    "Customer"
                ),
                (
                    new User
                    {
                        Username = "customer_bronze",
                        Email = "customer.bronze@omnichannelbeauty.vn",
                        FullName = "Phạm Khách Mới Bronze",
                        PasswordHash = defaultPasswordHash,
                        PhoneNumber = "0909111005",
                        AvatarUrl = "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=300&h=300&fit=crop",
                        UserType = 1,
                        IsActive = true,
                        StoreId = null
                    },
                    "Customer"
                )
            };

            foreach (var item in testUsers)
            {
                var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Username == item.User.Username);
                if (existingUser == null)
                {
                    await context.Users.AddAsync(item.User);
                    await context.SaveChangesAsync();

                    if (roles.TryGetValue(item.RoleName, out var roleId))
                    {
                        await context.UserRoles.AddAsync(new UserRole
                        {
                            UserId = item.User.Id,
                            RoleId = roleId
                        });
                        await context.SaveChangesAsync();
                    }
                }
                else
                {
                    existingUser.PasswordHash = defaultPasswordHash;
                    existingUser.IsActive = true;
                    if (item.User.StoreId.HasValue && existingUser.StoreId != item.User.StoreId)
                    {
                        existingUser.StoreId = item.User.StoreId;
                    }
                    if (string.IsNullOrEmpty(existingUser.PhoneNumber) && !string.IsNullOrEmpty(item.User.PhoneNumber))
                    {
                        existingUser.PhoneNumber = item.User.PhoneNumber;
                    }
                    await context.SaveChangesAsync();
                }
            }

            // Đồng thời giữ các tài khoản admin/manager/cashier nếu cần tương thích
            if (!await context.Users.AnyAsync(u => u.Username == "admin"))
            {
                var legacyAdmin = new User
                {
                    Username = "admin",
                    Email = "admin@omnichannel.com",
                    FullName = "Quản Trị Viên Kế Thừa",
                    PasswordHash = HashPassword("Admin@123456"),
                    PhoneNumber = "0909999999",
                    UserType = 2,
                    IsActive = true
                };
                await context.Users.AddAsync(legacyAdmin);
                await context.SaveChangesAsync();

                if (roles.TryGetValue("SuperAdmin", out var sRole))
                {
                    await context.UserRoles.AddAsync(new UserRole { UserId = legacyAdmin.Id, RoleId = sRole });
                    await context.SaveChangesAsync();
                }
            }
        }

        // =========================================================================
        // 4. SEED SUPPLIERS (NHÀ CUNG CẤP CHÍNH HÃNG)
        // =========================================================================
        private static async Task SeedSuppliersAsync(ApplicationDbContext context)
        {
            if (!await context.Suppliers.AnyAsync())
            {
                var suppliers = new List<Supplier>
                {
                    new()
                    {
                        Code = "SUP-001",
                        Name = "Công ty TNHH Amore Pacific VN",
                        ContactPerson = "Nguyễn Văn A",
                        PhoneNumber = "02838246232",
                        Email = "b2b@amorepacific.vn",
                        Address = "Tòa nhà Mplaza Saigon, 39 Lê Duẩn, Bến Nghé, Quận 1, TP.HCM",
                        IsActive = true
                    },
                    new()
                    {
                        Code = "SUP-002",
                        Name = "L'Oréal Vietnam",
                        ContactPerson = "Trần Thị B",
                        PhoneNumber = "02839103968",
                        Email = "order@loreal.vn",
                        Address = "Tòa nhà Vincom Center, 72 Lê Thánh Tôn, Bến Nghé, Quận 1, TP.HCM",
                        IsActive = true
                    },
                    new()
                    {
                        Code = "SUP-003",
                        Name = "Rohto-Mentholatum VN",
                        ContactPerson = "Lê Văn C",
                        PhoneNumber = "02743743400",
                        Email = "sales@rohto.com.vn",
                        Address = "Số 16 Đường số 5, VSIP 1, Thuận An, Bình Dương",
                        IsActive = true
                    }
                };

                await context.Suppliers.AddRangeAsync(suppliers);
                await context.SaveChangesAsync();
            }
        }

        // =========================================================================
        // =========================================================================
        // 5. SEED CATEGORIES (CÂY DANH MỤC MỸ PHẨM ĐA CẤP 2 TẦNG CHUẨN COCOLUX / HASAKI)
        // =========================================================================
        private static async Task SeedCategoriesAsync(ApplicationDbContext context)
        {
            var categoryTree = new List<(Category Root, List<Category> Subs)>
            {
                // 1. Chăm Sóc Da (Skincare)
                (
                    new Category
                    {
                        Name = "Chăm Sóc Da",
                        Slug = "cham-soc-da",
                        Description = "Chu trình dưỡng da chuyên sâu từ làm sạch, phục hồi màng ẩm đến bảo vệ quang phổ rộng chuẩn y khoa.",
                        Icon = "fa-solid fa-droplet",
                        ImageUrl = "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?w=600&h=600&fit=crop&q=80",
                        SortOrder = 1,
                        IsActive = true
                    },
                    new List<Category>
                    {
                        new() { Name = "Tẩy Trang", Slug = "nuoc-tay-trang", Description = "Nước, dầu và sáp tẩy trang làm sạch sâu bụi mịn và cặn makeup cứng đầu.", Icon = "fa-solid fa-water", SortOrder = 1, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=500&h=500&fit=crop" },
                        new() { Name = "Sữa Rửa Mặt", Slug = "sua-rua-mat", Description = "Làm sạch dịu nhẹ với độ pH 5.5 lý tưởng không gây khô căng da.", Icon = "fa-solid fa-soap", SortOrder = 2, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1556228722-d0b5d92fe77c?w=500&h=500&fit=crop" },
                        new() { Name = "Nước Hoa Hồng (Toner)", Slug = "toner-nuoc-hoa-hong", Description = "Cân bằng độ ẩm pH, se khít lỗ chân lông và làm dịu ửng đỏ tức thì.", Icon = "fa-solid fa-bottle-droplet", SortOrder = 3, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1608248597359-009848520330?w=500&h=500&fit=crop" },
                        new() { Name = "Serum & Tinh Chất", Slug = "serum-tinh-chat", Description = "Tinh chất cô đặc phục hồi màng lipid, dưỡng trắng và chống lão hóa sâu.", Icon = "fa-solid fa-eye-dropper", SortOrder = 4, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=500&h=500&fit=crop" },
                        new() { Name = "Kem Dưỡng Ẩm", Slug = "kem-duong-am", Description = "Khóa ẩm đa tầng, phục hồi hàng rào bảo vệ cho làn da căng mọng.", Icon = "fa-solid fa-jar", SortOrder = 5, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=500&h=500&fit=crop" },
                        new() { Name = "Kem Chống Nắng", Slug = "kem-chong-nang", Description = "Màng lọc quang phổ rộng bảo vệ da toàn diện khỏi tia UVA/UVB và ánh sáng xanh.", Icon = "fa-solid fa-sun", SortOrder = 6, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=500&h=500&fit=crop" },
                        new() { Name = "Mặt Nạ Dưỡng Da", Slug = "mat-na-duong-da", Description = "Mặt nạ giấy, đất sét và mặt nạ ngủ cấp nước tức thì cho làn da rạng rỡ.", Icon = "fa-solid fa-mask-face", SortOrder = 7, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1567928815116-f75471158609?w=500&h=500&fit=crop" },
                        new() { Name = "Chăm Sóc Vùng Mắt", Slug = "cham-soc-vung-mat", Description = "Kem dưỡng xóa mờ quầng thâm, giảm bọng mắt và nếp nhăn đuôi mắt.", Icon = "fa-solid fa-eye", SortOrder = 8, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1512290900672-1f02e6a0d33e?w=500&h=500&fit=crop" }
                    }
                ),

                // 2. Trang Điểm (Makeup)
                (
                    new Category
                    {
                        Name = "Trang Điểm",
                        Slug = "trang-diem",
                        Description = "Mỹ phẩm trang điểm thời thượng, tôn vinh nét đẹp tự nhiên và khí chất quý phái.",
                        Icon = "fa-solid fa-wand-magic-sparkles",
                        ImageUrl = "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=600&h=600&fit=crop&q=80",
                        SortOrder = 2,
                        IsActive = true
                    },
                    new List<Category>
                    {
                        new() { Name = "Kem Lót & Kem Nền", Slug = "kem-lot-kem-nen", Description = "Lớp nền mỏng nhẹ, che phủ khuyết điểm hoàn hảo và kiềm dầu lâu trôi 24H.", Icon = "fa-solid fa-paint-roller", SortOrder = 1, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=500&h=500&fit=crop" },
                        new() { Name = "Phấn Phủ & Cushion", Slug = "kem-nen-cushion", Description = "Phấn nước mỏng mịn như sương và phấn phủ kiềm dầu cố định lớp trang điểm.", Icon = "fa-solid fa-compact-disc", SortOrder = 2, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1512496015851-a90fb38ba796?w=500&h=500&fit=crop" },
                        new() { Name = "Má Hồng & Bắt Sáng", Slug = "ma-hong-bat-sang", Description = "Tạo khối sắc sảo và gò má ửng hồng ngọt ngào rạng rỡ dưới ánh đèn.", Icon = "fa-solid fa-palette", SortOrder = 3, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1583241800698-e8ab01830a07?w=500&h=500&fit=crop" },
                        new() { Name = "Son Môi Cao Cấp", Slug = "son-moi", Description = "Son thỏi lì, son tint bóng và son dưỡng ẩm từ các nhà mốt danh tiếng.", Icon = "fa-solid fa-heart", SortOrder = 4, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=500&h=500&fit=crop" },
                        new() { Name = "Trang Điểm Mắt & Mascara", Slug = "trang-diem-mat-mascara", Description = "Bảng phấn mắt đa sắc, kẻ mắt sắc nét và chuốt mi cong vút quyến rũ.", Icon = "fa-solid fa-eye", SortOrder = 5, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1516975080664-ed2fc6a32937?w=500&h=500&fit=crop" },
                        new() { Name = "Xịt Khóa Nền (Setting Spray)", Slug = "xit-khoa-nen", Description = "Khóa chặt lớp trang điểm không lem trôi dưới thời tiết nhiệt đới.", Icon = "fa-solid fa-spray-can", SortOrder = 6, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1571781926291-c477ebfd024b?w=500&h=500&fit=crop" }
                    }
                ),

                // 3. Nước Hoa (Haute Parfumerie)
                (
                    new Category
                    {
                        Name = "Nước Hoa",
                        Slug = "nuoc-hoa",
                        Description = "Tuyệt tác hương thơm sang trọng từ những nhà điều chế nước hoa huyền thoại thế giới.",
                        Icon = "fa-solid fa-spray-can-sparkles",
                        ImageUrl = "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=600&h=600&fit=crop&q=80",
                        SortOrder = 3,
                        IsActive = true
                    },
                    new List<Category>
                    {
                        new() { Name = "Nước Hoa Nữ", Slug = "nuoc-hoa-nu", Description = "Nốt hương hoa cỏ phương Đông quyến rũ, ngọt ngào và kiêu kỳ.", Icon = "fa-solid fa-venus", SortOrder = 1, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1547887537-6158d64c35b3?w=500&h=500&fit=crop" },
                        new() { Name = "Nước Hoa Nam", Slug = "nuoc-hoa-nam", Description = "Mùi hương gỗ trầm ấm, lịch lãm, mạnh mẽ và đầy cuốn hút.", Icon = "fa-solid fa-mars", SortOrder = 2, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1523293182086-7651a899d37f?w=500&h=500&fit=crop" },
                        new() { Name = "Nước Hoa Unisex", Slug = "nuoc-hoa-unisex", Description = "Hương thơm phá vỡ mọi định kiến giới tính, độc bản và khác biệt.", Icon = "fa-solid fa-venus-mars", SortOrder = 3, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1594035910387-fea47794261f?w=500&h=500&fit=crop" },
                        new() { Name = "Bộ Quà Tặng Nước Hoa (Giftset)", Slug = "giftset-nuoc-hoa", Description = "Hộp quà tặng xa xỉ tinh tế gồm nước hoa kèm lotion dưỡng thể cùng nốt hương.", Icon = "fa-solid fa-gift", SortOrder = 4, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1512290900672-1f02e6a0d33e?w=500&h=500&fit=crop" },
                        new() { Name = "Nước Hoa Mini Bỏ Túi", Slug = "nuoc-hoa-mini", Description = "Phiên bản bỏ túi 5-10ml tiện lợi đồng hành cùng quý cô mọi lúc mọi nơi.", Icon = "fa-solid fa-vial", SortOrder = 5, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1588405748880-12d1d2a59f75?w=500&h=500&fit=crop" }
                    }
                ),

                // 4. Chăm Sóc Tóc (Hair Care)
                (
                    new Category
                    {
                        Name = "Chăm Sóc Tóc",
                        Slug = "cham-soc-toc",
                        Description = "Liệu pháp phục hồi tóc chuẩn Salon cao cấp, nuôi dưỡng mái tóc bóng khỏe từ chân tóc.",
                        Icon = "fa-solid fa-wind",
                        ImageUrl = "https://images.unsplash.com/photo-1527799820374-dcf8d9d4a388?w=600&h=600&fit=crop&q=80",
                        SortOrder = 4,
                        IsActive = true
                    },
                    new List<Category>
                    {
                        new() { Name = "Dầu Gội & Dầu Xả", Slug = "dau-goi-dau-xa", Description = "Bộ đôi làm sạch da đầu, ngăn ngừa gãy rụng và nuôi dưỡng tóc bồng bềnh.", Icon = "fa-solid fa-shower", SortOrder = 1, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1535585209827-a15fcdbc4c2d?w=500&h=500&fit=crop" },
                        new() { Name = "Dầu Dưỡng & Serum Tóc", Slug = "dau-duong-serum-toc", Description = "Tinh dầu Argan phục hồi tóc khô xơ chẻ ngọn, tạo độ bóng mượt tức thì.", Icon = "fa-solid fa-spa", SortOrder = 2, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1608248597359-009848520330?w=500&h=500&fit=crop" },
                        new() { Name = "Mặt Nạ Ủ Tóc", Slug = "mat-na-u-toc", Description = "Kem ủ phục hồi keratin chuyên sâu cho tóc tẩy nhuộm và hư tổn nặng.", Icon = "fa-solid fa-bowl-rice", SortOrder = 3, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=500&h=500&fit=crop" },
                        new() { Name = "Sản Phẩm Tạo Kiểu", Slug = "san-pham-tao-kieu", Description = "Xịt tạo phồng, kem định hình lọn xoăn và sáp tạo nếp không bết dính.", Icon = "fa-solid fa-scissors", SortOrder = 4, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1527799820374-dcf8d9d4a388?w=500&h=500&fit=crop" }
                    }
                ),

                // 5. Chăm Sóc Cơ Thể (Body Care)
                (
                    new Category
                    {
                        Name = "Chăm Sóc Cơ Thể",
                        Slug = "cham-soc-co-the",
                        Description = "Chăm sóc làn da cơ thể mịn màng, ẩm mượt như lụa với hương thơm ngọt ngào thư giãn.",
                        Icon = "fa-solid fa-hand-holding-droplet",
                        ImageUrl = "https://images.unsplash.com/photo-1540555700478-4be289fbecef?w=600&h=600&fit=crop&q=80",
                        SortOrder = 5,
                        IsActive = true
                    },
                    new List<Category>
                    {
                        new() { Name = "Sữa Tắm Dưỡng Ẩm", Slug = "sua-tam-duong-am", Description = "Bọt mịn nhẹ nhàng làm sạch, lưu hương nước hoa thư giãn suốt ngày dài.", Icon = "fa-solid fa-bath", SortOrder = 1, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=500&h=500&fit=crop" },
                        new() { Name = "Dưỡng Thể Trắng Mịn", Slug = "duong-the-body", Description = "Sữa dưỡng thể chứa B3 & Vitamin C nuôi dưỡng làn da sáng mịn đều màu.", Icon = "fa-solid fa-feather", SortOrder = 2, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?w=500&h=500&fit=crop" },
                        new() { Name = "Tẩy Tế Bào Chết Body", Slug = "tay-te-bao-chet-body", Description = "Tẩy da chết hạt cà phê và muối khoáng giúp da mềm mịn và hấp thu dưỡng chất.", Icon = "fa-solid fa-sparkles", SortOrder = 3, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1540555700478-4be289fbecef?w=500&h=500&fit=crop" },
                        new() { Name = "Xịt Thơm Toàn Thân (Body Mist)", Slug = "body-mist", Description = "Hương thơm thanh mát, nhẹ nhàng xua tan căng thẳng sau ngày làm việc.", Icon = "fa-solid fa-fan", SortOrder = 4, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=500&h=500&fit=crop" }
                    }
                ),

                // 6. Thực Phẩm Làm Đẹp (Nutri-Cosmetics)
                (
                    new Category
                    {
                        Name = "Thực Phẩm Làm Đẹp",
                        Slug = "thuc-pham-lam-dep",
                        Description = "Bảo dưỡng nhan sắc từ sâu bên trong với các dưỡng chất sinh học và thảo mộc quý hiếm.",
                        Icon = "fa-solid fa-capsules",
                        ImageUrl = "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=600&h=600&fit=crop&q=80",
                        SortOrder = 6,
                        IsActive = true
                    },
                    new List<Category>
                    {
                        new() { Name = "Collagen Nước", Slug = "collagen-nuoc", Description = "Collagen peptide thủy phân hàm lượng cao giúp da căng bóng và mờ nếp nhăn.", Icon = "fa-solid fa-flask-vial", SortOrder = 1, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=500&h=500&fit=crop" },
                        new() { Name = "Vitamin & Viên Uống Trắng Da", Slug = "vitamin-vien-uong", Description = "Viên uống chống oxy hóa, hỗ trợ giảm thâm nám và bảo vệ da trước ánh nắng.", Icon = "fa-solid fa-pills", SortOrder = 2, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1584017911766-d451b3d0e843?w=500&h=500&fit=crop" },
                        new() { Name = "Nước Hồng Sâm Phục Hồi", Slug = "nuoc-hong-sam", Description = "Hồng sâm 6 năm tuổi bồi bổ khí huyết, tăng cường lưu thông máu cho sắc diện hồng hào.", Icon = "fa-solid fa-leaf", SortOrder = 3, IsActive = true, ImageUrl = "https://images.unsplash.com/photo-1512069772995-ec65ed45afd6?w=500&h=500&fit=crop" }
                    }
                )
            };

            foreach (var (root, subs) in categoryTree)
            {
                var existingRoot = await context.Categories.FirstOrDefaultAsync(c => c.Slug == root.Slug);
                if (existingRoot == null)
                {
                    await context.Categories.AddAsync(root);
                    await context.SaveChangesAsync();
                    existingRoot = root;
                }
                else
                {
                    existingRoot.Name = root.Name;
                    existingRoot.ParentId = null; // Luôn đảm bảo là danh mục gốc
                    existingRoot.Description = root.Description;
                    existingRoot.Icon = root.Icon;
                    existingRoot.SortOrder = root.SortOrder;
                    existingRoot.IsActive = true;
                    if (string.IsNullOrEmpty(existingRoot.ImageUrl)) existingRoot.ImageUrl = root.ImageUrl;
                    await context.SaveChangesAsync();
                }

                // Seed Subcategories
                foreach (var sub in subs)
                {
                    var existingSub = await context.Categories.FirstOrDefaultAsync(c => c.Slug == sub.Slug);
                    if (existingSub == null)
                    {
                        sub.ParentId = existingRoot.Id;
                        await context.Categories.AddAsync(sub);
                    }
                    else
                    {
                        existingSub.Name = sub.Name;
                        existingSub.ParentId = existingRoot.Id;
                        existingSub.Description = sub.Description;
                        existingSub.Icon = sub.Icon;
                        existingSub.SortOrder = sub.SortOrder;
                        existingSub.IsActive = true;
                        if (string.IsNullOrEmpty(existingSub.ImageUrl)) existingSub.ImageUrl = sub.ImageUrl;
                    }
                }
                await context.SaveChangesAsync();
            }

            // Vô hiệu hóa các danh mục cũ không còn thuộc hệ thống phân cấp chuẩn (nếu không có sản phẩm liên kết)
            var activeSlugs = categoryTree.Select(c => c.Root.Slug).Concat(categoryTree.SelectMany(c => c.Subs.Select(s => s.Slug))).ToHashSet();
            var obsoleteCategories = await context.Categories
                .Where(c => !activeSlugs.Contains(c.Slug))
                .ToListAsync();

            foreach (var ob in obsoleteCategories)
            {
                var hasProducts = await context.Products.AnyAsync(p => p.CategoryId == ob.Id);
                if (!hasProducts)
                {
                    ob.IsActive = false;
                }
            }
            await context.SaveChangesAsync();
        }

        // =========================================================================
        // 5B. SEED BRANDS (26 THƯƠNG HIỆU MỸ PHẨM CHÍNH HÃNG HÀNG ĐẦU THẾ GIỚI)
        // =========================================================================
        private static async Task SeedBrandsAsync(ApplicationDbContext context)
        {
            var brandList = new List<Brand>
            {
                // --- PHÁP (7 THƯƠNG HIỆU) ---
                new()
                {
                    Name = "La Roche-Posay",
                    Slug = "la-roche-posay",
                    CountryOfOrigin = "Pháp",
                    LogoUrl = "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=200&h=100&fit=crop",
                    Description = "Thương hiệu dược mỹ phẩm hàng đầu nước Pháp, chuyên sâu phục hồi và bảo vệ làn da nhạy cảm mụn được hơn 90.000 bác sĩ da liễu toàn cầu khuyên dùng.",
                    IsFeatured = true,
                    SortOrder = 1,
                    IsActive = true
                },
                new()
                {
                    Name = "Bioderma",
                    Slug = "bioderma",
                    CountryOfOrigin = "Pháp",
                    LogoUrl = "https://images.unsplash.com/photo-1556228722-d0b5d92fe77c?w=200&h=100&fit=crop",
                    Description = "Thương hiệu tiên phong ứng dụng công nghệ sinh học bảo vệ hệ sinh thái tự nhiên của làn da với dòng nước tẩy trang Sensibio H2O huyền thoại.",
                    IsFeatured = true,
                    SortOrder = 2,
                    IsActive = true
                },
                new()
                {
                    Name = "L'Oréal Paris",
                    Slug = "loreal-paris",
                    CountryOfOrigin = "Pháp",
                    LogoUrl = "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=200&h=100&fit=crop",
                    Description = "Tập đoàn làm đẹp số 1 thế giới với sứ mệnh mang đến vẻ đẹp sang trọng, khoa học vượt bậc cho mọi phụ nữ với tuyên ngôn Vì Bạn Xứng Đáng.",
                    IsFeatured = true,
                    SortOrder = 3,
                    IsActive = true
                },
                new()
                {
                    Name = "Lancôme",
                    Slug = "lancome",
                    CountryOfOrigin = "Pháp",
                    LogoUrl = "https://images.unsplash.com/photo-1512290900672-1f02e6a0d33e?w=200&h=100&fit=crop",
                    Description = "Biểu tượng nhan sắc xa xỉ nước Pháp với hoa hồng vàng kiêu hãnh và tinh chất trẻ hóa da Advanced Génifique biểu tượng toàn cầu.",
                    IsFeatured = true,
                    SortOrder = 4,
                    IsActive = true
                },
                new()
                {
                    Name = "Dior",
                    Slug = "dior",
                    CountryOfOrigin = "Pháp",
                    LogoUrl = "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=200&h=100&fit=crop",
                    Description = "Đỉnh cao Haute Couture và Haute Parfumerie kinh điển với các dòng son Rouge Dior và nước hoa Miss Dior, Sauvage quý phái thượng lưu.",
                    IsFeatured = true,
                    SortOrder = 5,
                    IsActive = true
                },
                new()
                {
                    Name = "Yves Saint Laurent",
                    Slug = "ysl",
                    CountryOfOrigin = "Pháp",
                    LogoUrl = "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=200&h=100&fit=crop",
                    Description = "Thương hiệu thời trang xa hoa mang phong cách tự do, phá cách với thỏi son vàng Rouge Pur Couture và mùi hương Libre nồng nàn quyến rũ.",
                    IsFeatured = true,
                    SortOrder = 6,
                    IsActive = true
                },
                new()
                {
                    Name = "Chanel",
                    Slug = "chanel",
                    CountryOfOrigin = "Pháp",
                    LogoUrl = "https://images.unsplash.com/photo-1547887537-6158d64c35b3?w=200&h=100&fit=crop",
                    Description = "Huyền thoại bất tử của thời trang và hương thơm xa xỉ thế giới với chai nước hoa Chanel N°5 và phong thái thanh lịch vượt thời gian.",
                    IsFeatured = true,
                    SortOrder = 7,
                    IsActive = true
                },

                // --- HÀN QUỐC (7 THƯƠNG HIỆU) ---
                new()
                {
                    Name = "Sulwhasoo",
                    Slug = "sulwhasoo",
                    CountryOfOrigin = "Hàn Quốc",
                    LogoUrl = "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?w=200&h=100&fit=crop",
                    Description = "Đỉnh cao mỹ phẩm thảo dược hoàng gia Hàn Quốc kết tinh từ nhân sâm nghìn năm và công nghệ trẻ hóa da Jaum Activator danh giá bậc nhất châu Á.",
                    IsFeatured = true,
                    SortOrder = 8,
                    IsActive = true
                },
                new()
                {
                    Name = "Laneige",
                    Slug = "laneige",
                    CountryOfOrigin = "Hàn Quốc",
                    LogoUrl = "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=200&h=100&fit=crop",
                    Description = "Thương hiệu bậc thầy về khoa học cấp nước với mặt nạ ngủ môi Lip Sleeping Mask và cushion kiềm dầu Neo Cushion gây sốt giới làm đẹp.",
                    IsFeatured = true,
                    SortOrder = 9,
                    IsActive = true
                },
                new()
                {
                    Name = "Innisfree",
                    Slug = "innisfree",
                    CountryOfOrigin = "Hàn Quốc",
                    LogoUrl = "https://images.unsplash.com/photo-1527799820374-dcf8d9d4a388?w=200&h=100&fit=crop",
                    Description = "Mỹ phẩm thiên nhiên thuần khiết từ hòn đảo ngọc Jeju với dòng trà xanh hữu cơ Green Tea Seed và phấn phủ kiềm dầu No-Sebum trứ danh.",
                    IsFeatured = true,
                    SortOrder = 10,
                    IsActive = true
                },
                new()
                {
                    Name = "Rom&nd",
                    Slug = "romand",
                    CountryOfOrigin = "Hàn Quốc",
                    LogoUrl = "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=200&h=100&fit=crop",
                    Description = "Cơn sốt son môi xu hướng của giới trẻ toàn châu Á với dòng son bóng Juicy Lasting Tint và bảng mắt đa năng màu sắc thời thượng.",
                    IsFeatured = true,
                    SortOrder = 11,
                    IsActive = true
                },
                new()
                {
                    Name = "COSRX",
                    Slug = "cosrx",
                    CountryOfOrigin = "Hàn Quốc",
                    LogoUrl = "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=200&h=100&fit=crop",
                    Description = "Thương hiệu chăm sóc da tối giản với nồng độ hoạt chất chuẩn y khoa BHA, ốc sên giải quyết triệt để vấn đề mụn và phục hồi hàng rào ẩm.",
                    IsFeatured = true,
                    SortOrder = 12,
                    IsActive = true
                },
                new()
                {
                    Name = "Torriden",
                    Slug = "torriden",
                    CountryOfOrigin = "Hàn Quốc",
                    LogoUrl = "https://images.unsplash.com/photo-1608248597359-009848520330?w=200&h=100&fit=crop",
                    Description = "Thương hiệu cấp ẩm quốc dân dẫn đầu bảng xếp hạng Olive Young Hàn Quốc với serum phân tử siêu nhỏ Dive-In Hyaluronic Acid.",
                    IsFeatured = true,
                    SortOrder = 13,
                    IsActive = true
                },
                new()
                {
                    Name = "Some By Mi",
                    Slug = "some-by-mi",
                    CountryOfOrigin = "Hàn Quốc",
                    LogoUrl = "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=200&h=100&fit=crop",
                    Description = "Phép màu phục hồi làn da mụn sau 30 ngày với phức hợp AHA-BHA-PHA và chiết xuất tràm trà thiên nhiên lành tính.",
                    IsFeatured = true,
                    SortOrder = 14,
                    IsActive = true
                },

                // --- NHẬT BẢN (6 THƯƠNG HIỆU) ---
                new()
                {
                    Name = "Shiseido",
                    Slug = "shiseido",
                    CountryOfOrigin = "Nhật Bản",
                    LogoUrl = "https://images.unsplash.com/photo-1540555700478-4be289fbecef?w=200&h=100&fit=crop",
                    Description = "Tinh hoa làm đẹp hơn 150 năm của đất nước Phù Tang, kết hợp y học phương Đông và công nghệ phương Tây với dòng Ultimune bảo vệ sinh khí da.",
                    IsFeatured = true,
                    SortOrder = 15,
                    IsActive = true
                },
                new()
                {
                    Name = "SK-II",
                    Slug = "sk-ii",
                    CountryOfOrigin = "Nhật Bản",
                    LogoUrl = "https://images.unsplash.com/photo-1567928815116-f75471158609?w=200&h=100&fit=crop",
                    Description = "Bí mật làn da pha lê không tì vết với nước thần Facial Treatment Essence chứa hơn 90% tinh chất men Pitera kỳ diệu độc quyền.",
                    IsFeatured = true,
                    SortOrder = 16,
                    IsActive = true
                },
                new()
                {
                    Name = "Hada Labo",
                    Slug = "hada-labo",
                    CountryOfOrigin = "Nhật Bản",
                    LogoUrl = "https://images.unsplash.com/photo-1556228722-d0b5d92fe77c?w=200&h=100&fit=crop",
                    Description = "Triết lý Đơn Giản & Hoàn Hảo của tập đoàn Dược Rohto Nhật Bản với công nghệ Super Hyaluronic Acid khóa ẩm sâu suốt cả ngày.",
                    IsFeatured = true,
                    SortOrder = 17,
                    IsActive = true
                },
                new()
                {
                    Name = "Anessa",
                    Slug = "anessa",
                    CountryOfOrigin = "Nhật Bản",
                    LogoUrl = "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=200&h=100&fit=crop",
                    Description = "Thương hiệu chống nắng số 1 Nhật Bản 21 năm liên tiếp với công nghệ Auto Booster giúp màng lọc chống nắng càng mạnh mẽ hơn khi gặp nhiệt độ cao và mồ hôi.",
                    IsFeatured = true,
                    SortOrder = 18,
                    IsActive = true
                },
                new()
                {
                    Name = "DHC",
                    Slug = "dhc",
                    CountryOfOrigin = "Nhật Bản",
                    LogoUrl = "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=200&h=100&fit=crop",
                    Description = "Tinh hoa dầu olive nguyên chất Tây Ban Nha kết hợp công nghệ chiết xuất Nhật Bản với dầu tẩy trang Deep Cleansing Oil và thực phẩm bổ sung sắc đẹp.",
                    IsFeatured = true,
                    SortOrder = 19,
                    IsActive = true
                },
                new()
                {
                    Name = "Canmake",
                    Slug = "canmake",
                    CountryOfOrigin = "Nhật Bản",
                    LogoUrl = "https://images.unsplash.com/photo-1512496015851-a90fb38ba796?w=200&h=100&fit=crop",
                    Description = "Thương hiệu trang điểm dễ thương ngọt ngào chuẩn phong cách Tokyo với phấn phủ Marshmallow Finish Powder được yêu thích bậc nhất châu Á.",
                    IsFeatured = true,
                    SortOrder = 20,
                    IsActive = true
                },

                // --- MỸ & ANH (6 THƯƠNG HIỆU) ---
                new()
                {
                    Name = "Estée Lauder",
                    Slug = "estee-lauder",
                    CountryOfOrigin = "Mỹ",
                    LogoUrl = "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=200&h=100&fit=crop",
                    Description = "Tượng đài chống lão hóa toàn cầu của tập đoàn mỹ phẩm xa xỉ Hoa Kỳ với chai serum phục hồi đêm Advanced Night Repair phục hồi DNA tế bào.",
                    IsFeatured = true,
                    SortOrder = 21,
                    IsActive = true
                },
                new()
                {
                    Name = "MAC Cosmetics",
                    Slug = "mac-cosmetics",
                    CountryOfOrigin = "Mỹ",
                    LogoUrl = "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=200&h=100&fit=crop",
                    Description = "Thương hiệu nghệ thuật trang điểm chuyên nghiệp hàng đầu thế giới với các thỏi son hình viên đạn kinh điển như Ruby Woo, Chili, Marrakesh.",
                    IsFeatured = true,
                    SortOrder = 22,
                    IsActive = true
                },
                new()
                {
                    Name = "CeraVe",
                    Slug = "cerave",
                    CountryOfOrigin = "Mỹ",
                    LogoUrl = "https://images.unsplash.com/photo-1556228722-d0b5d92fe77c?w=200&h=100&fit=crop",
                    Description = "Nhãn hiệu sữa rửa mặt và dưỡng ẩm số 1 Hoa Kỳ được các chuyên gia da liễu khuyên dùng nhờ phức hợp 3 Ceramide thiết yếu và công nghệ giải phóng chậm MVE.",
                    IsFeatured = true,
                    SortOrder = 23,
                    IsActive = true
                },
                new()
                {
                    Name = "Paula's Choice",
                    Slug = "paulas-choice",
                    CountryOfOrigin = "Mỹ",
                    LogoUrl = "https://images.unsplash.com/photo-1608248597359-009848520330?w=200&h=100&fit=crop",
                    Description = "Triết lý chăm sóc da thông minh, dựa trên bằng chứng khoa học và công khai 100% thành phần với dung dịch tẩy tế bào chết hóa học Skin Perfecting 2% BHA Liquid.",
                    IsFeatured = true,
                    SortOrder = 24,
                    IsActive = true
                },
                new()
                {
                    Name = "The Ordinary",
                    Slug = "the-ordinary",
                    CountryOfOrigin = "Canada",
                    LogoUrl = "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=200&h=100&fit=crop",
                    Description = "Cuộc cách mạng mỹ phẩm trung thực với các công thức hóa học hiệu năng cao Niacinamide 10%, AHA 30% BHA 2% Peeling Solution ở mức giá minh bạch.",
                    IsFeatured = true,
                    SortOrder = 25,
                    IsActive = true
                },
                new()
                {
                    Name = "Jo Malone London",
                    Slug = "jo-malone-london",
                    CountryOfOrigin = "Anh",
                    LogoUrl = "https://images.unsplash.com/photo-1594035910387-fea47794261f?w=200&h=100&fit=crop",
                    Description = "Đỉnh cao nghệ thuật nước hoa thủ công thanh lịch xứ sở sương mù với hộp quà kem thắt ruy băng đen quý tộc và mùi hương English Pear & Freesia bất hủ.",
                    IsFeatured = true,
                    SortOrder = 26,
                    IsActive = true
                }
            };

            foreach (var b in brandList)
            {
                var existing = await context.Brands.FirstOrDefaultAsync(x => x.Slug == b.Slug);
                if (existing == null)
                {
                    await context.Brands.AddAsync(b);
                }
                else
                {
                    existing.Name = b.Name;
                    existing.CountryOfOrigin = b.CountryOfOrigin;
                    existing.Description = b.Description;
                    existing.IsFeatured = b.IsFeatured;
                    existing.SortOrder = b.SortOrder;
                    existing.IsActive = true;
                    if (string.IsNullOrEmpty(existing.LogoUrl)) existing.LogoUrl = b.LogoUrl;
                }
            }
            await context.SaveChangesAsync();
        }

        // =========================================================================
        // 6. SEED SKIN TYPES & ACTIVE INGREDIENTS (IDEMPOTENT & CHUẨN Y KHOA)
        // =========================================================================
        private static async Task SeedSkinTypesAndIngredientsAsync(ApplicationDbContext context)
        {
            var skinTypes = new List<SkinType>
            {
                new() { Name = "Da dầu", Slug = "da-dau" },
                new() { Name = "Da khô", Slug = "da-kho" },
                new() { Name = "Da nhạy cảm", Slug = "da-nhay-cam" },
                new() { Name = "Da hỗn hợp", Slug = "da-hon-hop" },
                new() { Name = "Da thường", Slug = "da-thuong" }
            };

            foreach (var st in skinTypes)
            {
                if (!await context.SkinTypes.AnyAsync(s => s.Slug == st.Slug))
                {
                    await context.SkinTypes.AddAsync(st);
                }
            }
            await context.SaveChangesAsync();

            var ingredients = new List<ActiveIngredient>
            {
                new() { Name = "Niacinamide", Slug = "niacinamide", Description = "Phục hồi hàng rào ẩm, kiềm dầu, sáng da", IsActive = true },
                new() { Name = "Retinol", Slug = "retinol", Description = "Kích thích tăng sinh collagen, ngừa lão hóa", IsActive = true },
                new() { Name = "BHA (Salicylic Acid)", Slug = "bha-salicylic-acid", Description = "Làm sạch sâu bã nhờn trong lỗ chân lông, ngừa mụn", IsActive = true },
                new() { Name = "AHA (Glycolic Acid)", Slug = "aha-glycolic-acid", Description = "Tẩy tế bào chết bề mặt, giúp da mềm mịn", IsActive = true },
                new() { Name = "Hyaluronic Acid", Slug = "hyaluronic-acid", Description = "Cấp ẩm chuyên sâu, ngậm nước đa tầng", IsActive = true },
                new() { Name = "Vitamin C", Slug = "vitamin-c", Description = "Chống oxy hóa, mờ thâm nám và dưỡng trắng", IsActive = true },
                new() { Name = "Centella Asiatica", Slug = "centella-asiatica", Description = "Chiết xuất rau má làm dịu da nhạy cảm kích ứng", IsActive = true },
                new() { Name = "Ceramide", Slug = "ceramide", Description = "Củng cố hàng rào lipid bảo vệ da tự nhiên", IsActive = true },
                new() { Name = "Panthenol (Vitamin B5)", Slug = "panthenol-vitamin-b5", Description = "Làm dịu tức thì, phục hồi màng ẩm tổn thương", IsActive = true },
                new() { Name = "Peptides", Slug = "peptides", Description = "Chuỗi axit amin tái tạo cấu trúc da săn chắc", IsActive = true },
                new() { Name = "Collagen Thủy Phân", Slug = "collagen-thuy-phan", Description = "Tăng sinh collagen nội sinh, mờ nếp nhăn", IsActive = true },
                new() { Name = "Tràm Trà (Tea Tree)", Slug = "tea-tree-tram-tra", Description = "Kháng khuẩn, làm dịu các nốt mụn sưng đỏ", IsActive = true },
                new() { Name = "Men Pitera", Slug = "pitera", Description = "Men vi sinh độc quyền thúc đẩy tái tạo tế bào da", IsActive = true },
                new() { Name = "Squalane", Slug = "squalane", Description = "Dầu dưỡng ẩm tự nhiên tương thích cao với lipid da", IsActive = true },
                new() { Name = "Dầu Argan", Slug = "argan-oil", Description = "Dưỡng chất vàng nuôi dưỡng tóc và da khô ráp", IsActive = true }
            };

            foreach (var ing in ingredients)
            {
                if (!await context.ActiveIngredients.AnyAsync(i => i.Slug == ing.Slug))
                {
                    await context.ActiveIngredients.AddAsync(ing);
                }
            }
            await context.SaveChangesAsync();
        }

        // =========================================================================
        // 7. SEED PRODUCTS (80+ MỸ PHẨM CHÍNH HÃNG THỰC TẾ) + LÔ BATCH FEFO + TỒN KHO STORE 1 & 2
        // =========================================================================
        private static async Task SeedProductsAndBatchesAsync(ApplicationDbContext context)
        {
            var stores = await context.Stores.ToListAsync();
            var hqStore = stores.FirstOrDefault(s => s.Code == "SH-Q1" || s.Code == "HQ-001") ?? stores[0];
            var cgStore = stores.FirstOrDefault(s => s.Code == "SH-BD" || s.Code == "SH-CG") ?? (stores.Count > 1 ? stores[1] : hqStore);

            var supList = await context.Suppliers.ToListAsync();
            var supDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in supList) supDict[s.Code] = s.Id;

            var catList = await context.Categories.ToListAsync();
            var catDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in catList) catDict[c.Slug] = c.Id;

            var brandList = await context.Brands.ToListAsync();
            var brandDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var b in brandList)
            {
                brandDict[b.Name] = b.Id;
                brandDict[b.Slug] = b.Id;
            }

            var stList = await context.SkinTypes.ToListAsync();
            var stDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in stList) stDict[s.Slug] = s.Id;

            var ingList = await context.ActiveIngredients.ToListAsync();
            var ingDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var i in ingList) ingDict[i.Slug] = i.Id;

            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "superadmin") ?? await context.Users.FirstAsync();
            var now = DateTime.UtcNow;

            var catalogItems = ProductCatalogData.Get80CosmeticProducts();

            foreach (var item in catalogItems)
            {
                // Xác định nhà cung cấp chính thức
                int? supId = null;
                if (item.CountryOfOrigin == "Hàn Quốc")
                    supId = supDict.GetValueOrDefault("SUP-001");
                else if (item.CountryOfOrigin == "Nhật Bản")
                    supId = supDict.GetValueOrDefault("SUP-003");
                else
                    supId = supDict.GetValueOrDefault("SUP-002");

                // Xác định CategoryId
                var catId = catDict.GetValueOrDefault(item.CatSlug);
                if (catId == 0)
                {
                    catId = catDict.Values.FirstOrDefault();
                }

                // Xác định BrandId
                int? brandId = null;
                if (brandDict.TryGetValue(item.BrandName, out var bId))
                {
                    brandId = bId;
                }

                var product = await context.Products.FirstOrDefaultAsync(p => p.Sku == item.Sku);
                if (product == null)
                {
                    product = new Product
                    {
                        Sku = item.Sku,
                        Barcode = item.Barcode,
                        Name = item.Name,
                        Slug = item.Slug,
                        CategoryId = catId,
                        BrandId = brandId,
                        SupplierId = supId,
                        ManufacturerName = item.BrandName,
                        CountryOfOrigin = item.CountryOfOrigin,
                        CostPrice = item.CostPrice,
                        SellingPrice = item.SellingPrice,
                        DiscountPrice = item.DiscountPrice,
                        Unit = item.Unit,
                        HasVariants = item.HasVariants,
                        ProductType = "STANDARD",
                        ShortDescription = item.ShortDescription,
                        Description = item.Description,
                        ThumbnailUrl = item.ThumbnailUrl,
                        ImagesJson = JsonSerializer.Serialize(item.Images),
                        CosmeticNotificationNumber = item.CosmeticNotificationNumber,
                        ImporterName = item.ImporterName,
                        IsActive = true,
                        IsFeatured = item.IsFeatured,
                        CreatedAt = now.AddDays(-120)
                    };
                    await context.Products.AddAsync(product);
                    await context.SaveChangesAsync();
                }
                else
                {
                    product.Name = item.Name;
                    product.CategoryId = catId;
                    product.BrandId = brandId;
                    product.SupplierId = supId;
                    product.ManufacturerName = item.BrandName;
                    product.CountryOfOrigin = item.CountryOfOrigin;
                    product.CostPrice = item.CostPrice;
                    product.SellingPrice = item.SellingPrice;
                    product.DiscountPrice = item.DiscountPrice;
                    product.Unit = item.Unit;
                    product.ShortDescription = item.ShortDescription;
                    product.Description = item.Description;
                    product.ThumbnailUrl = item.ThumbnailUrl;
                    product.ImagesJson = JsonSerializer.Serialize(item.Images);
                    product.CosmeticNotificationNumber = item.CosmeticNotificationNumber;
                    product.ImporterName = item.ImporterName;
                    product.IsActive = true;
                    product.IsFeatured = item.IsFeatured;
                    await context.SaveChangesAsync();
                }

                // Gán loại da phù hợp (ProductSkinTypes)
                foreach (var stSlug in item.SkinTypeSlugs)
                {
                    if (stDict.TryGetValue(stSlug, out var stId))
                    {
                        var hasRelation = await context.ProductSkinTypes.AnyAsync(pst => pst.ProductId == product.Id && pst.SkinTypeId == stId);
                        if (!hasRelation)
                        {
                            await context.ProductSkinTypes.AddAsync(new ProductSkinType
                            {
                                ProductId = product.Id,
                                SkinTypeId = stId
                            });
                        }
                    }
                }

                // Gán hoạt chất vàng (ProductIngredients)
                foreach (var (ingSlug, conc) in item.Ingredients)
                {
                    if (ingDict.TryGetValue(ingSlug, out var ingId))
                    {
                        var hasIng = await context.ProductIngredients.AnyAsync(pi => pi.ProductId == product.Id && pi.ActiveIngredientId == ingId);
                        if (!hasIng)
                        {
                            await context.ProductIngredients.AddAsync(new ProductIngredient
                            {
                                ProductId = product.Id,
                                ActiveIngredientId = ingId,
                                Concentration = conc
                            });
                        }
                    }
                }
                await context.SaveChangesAsync();

                // Seed 2 ProductBatches (1 Date xa ACTIVE, 1 Cận date NEAR_EXPIRATION) nếu chưa có
                if (!await context.ProductBatches.AnyAsync(b => b.ProductId == product.Id))
                {
                    var batchFar = new ProductBatch
                    {
                        ProductId = product.Id,
                        BatchNumber = $"LOT-{product.Sku}-26A01",
                        ManufacturingDate = now.AddMonths(-4),
                        ExpDate = now.AddYears(2).AddMonths(6),
                        ImportCost = product.CostPrice,
                        InitialQuantity = 100,
                        CurrentQuantity = 85,
                        Status = "ACTIVE",
                        CreatedAt = now.AddMonths(-4)
                    };

                    var batchNear = new ProductBatch
                    {
                        ProductId = product.Id,
                        BatchNumber = $"LOT-{product.Sku}-24FEFO",
                        ManufacturingDate = now.AddYears(-2).AddMonths(-9),
                        ExpDate = now.AddDays(38), // Cận date <60 ngày (38 ngày)
                        ImportCost = product.CostPrice,
                        InitialQuantity = 30,
                        CurrentQuantity = 15,
                        Status = "NEAR_EXPIRATION",
                        CreatedAt = now.AddMonths(-9)
                    };

                    await context.ProductBatches.AddRangeAsync(batchFar, batchNear);
                    await context.SaveChangesAsync();

                    // Seed tồn kho phân bổ cho Store HQ (Trụ sở chính) và Store CG (Cầu Giấy)
                    await context.StoreInventories.AddRangeAsync(
                        new StoreInventory
                        {
                            StoreId = hqStore.Id,
                            ProductBatchId = batchFar.Id,
                            PhysicalQuantity = 55,
                            ReservedQuantity = 5,
                            LastUpdated = now
                        },
                        new StoreInventory
                        {
                            StoreId = hqStore.Id,
                            ProductBatchId = batchNear.Id,
                            PhysicalQuantity = 12,
                            ReservedQuantity = 2,
                            LastUpdated = now
                        },
                        new StoreInventory
                        {
                            StoreId = cgStore.Id,
                            ProductBatchId = batchFar.Id,
                            PhysicalQuantity = 30,
                            ReservedQuantity = 0,
                            LastUpdated = now
                        },
                        new StoreInventory
                        {
                            StoreId = cgStore.Id,
                            ProductBatchId = batchNear.Id,
                            PhysicalQuantity = 3,
                            ReservedQuantity = 0,
                            LastUpdated = now
                        }
                    );

                    await context.InventoryLogs.AddAsync(new InventoryLog
                    {
                        StoreId = hqStore.Id,
                        ProductBatchId = batchFar.Id,
                        UserId = adminUser.Id,
                        TransactionType = "INITIAL_IMPORT",
                        QuantityChanged = 55,
                        QuantityBefore = 0,
                        QuantityAfter = 55,
                        Note = $"Nhập kho ban đầu lô date xa chuẩn FEFO {batchFar.BatchNumber}",
                        CreatedAt = now.AddMonths(-4)
                    });

                    await context.SaveChangesAsync();
                }

                // Seed biến thể cho các sản phẩm son môi và phấn nước nếu chưa có
                if (item.HasVariants && !await context.ProductVariants.AnyAsync(v => v.ProductId == product.Id))
                {
                    if (item.Sku.StartsWith("SKU-LIP-MAC"))
                    {
                        var vMac = new ProductVariant
                        {
                            ProductId = product.Id,
                            Sku = $"{product.Sku}-V1",
                            Barcode = product.Barcode,
                            VariantName = product.Name,
                            ColorName = item.Sku.Contains("031") ? "#707 Ruby Woo" : (item.Sku.Contains("032") ? "#602 Chili" : "#316 Devoted To Chili"),
                            ColorHex = item.Sku.Contains("031") ? "#BA1628" : (item.Sku.Contains("032") ? "#B43D2A" : "#9B3828"),
                            Volume = "3g",
                            Formulation = "Matte Lipstick",
                            CostPrice = product.CostPrice,
                            SellingPrice = product.SellingPrice,
                            DiscountPrice = product.DiscountPrice,
                            SortOrder = 1,
                            IsActive = true
                        };
                        await context.ProductVariants.AddAsync(vMac);
                    }
                    else if (item.Sku == "SKU-LIP-ROM-034")
                    {
                        var r1 = new ProductVariant
                        {
                            ProductId = product.Id,
                            Sku = "SKU-LIP-ROM-034-V1",
                            Barcode = "8935001202034",
                            VariantName = "Rom&nd Zero Velvet Tint - #02 Joyful (Đỏ Cam)",
                            ColorName = "#02 Joyful",
                            ColorHex = "#C94C3A",
                            Volume = "5.5g",
                            Formulation = "Velvet Tint",
                            CostPrice = product.CostPrice,
                            SellingPrice = product.SellingPrice,
                            DiscountPrice = product.DiscountPrice,
                            SortOrder = 1,
                            IsActive = true
                        };
                        var r2 = new ProductVariant
                        {
                            ProductId = product.Id,
                            Sku = "SKU-LIP-ROM-034-V2",
                            Barcode = "8935001202099",
                            VariantName = "Rom&nd Zero Velvet Tint - #05 Witty (Cam Cháy)",
                            ColorName = "#05 Witty",
                            ColorHex = "#9B3828",
                            Volume = "5.5g",
                            Formulation = "Velvet Tint",
                            CostPrice = product.CostPrice,
                            SellingPrice = product.SellingPrice,
                            DiscountPrice = product.DiscountPrice,
                            SortOrder = 2,
                            IsActive = true
                        };
                        await context.ProductVariants.AddRangeAsync(r1, r2);
                    }
                    else if (item.Sku == "SKU-MAK-LAN-041")
                    {
                        var c1 = new ProductVariant
                        {
                            ProductId = product.Id,
                            Sku = "SKU-MAK-LAN-041-21N",
                            Barcode = "8935001202041",
                            VariantName = "Laneige Neo Cushion Matte - #21N Beige",
                            ColorName = "#21N Beige",
                            ColorHex = "#EBD2BE",
                            Volume = "15g x 2",
                            CostPrice = product.CostPrice,
                            SellingPrice = product.SellingPrice,
                            DiscountPrice = product.DiscountPrice,
                            SortOrder = 1,
                            IsActive = true
                        };
                        var c2 = new ProductVariant
                        {
                            ProductId = product.Id,
                            Sku = "SKU-MAK-LAN-041-23N",
                            Barcode = "8935001202098",
                            VariantName = "Laneige Neo Cushion Matte - #23N Sand",
                            ColorName = "#23N Sand",
                            ColorHex = "#D8BA9E",
                            Volume = "15g x 2",
                            CostPrice = product.CostPrice,
                            SellingPrice = product.SellingPrice,
                            DiscountPrice = product.DiscountPrice,
                            SortOrder = 2,
                            IsActive = true
                        };
                        await context.ProductVariants.AddRangeAsync(c1, c2);
                    }
                    await context.SaveChangesAsync();
                }
            }
        }

        // =========================================================================
        // 8. SEED MEMBER TIERS, LOYALTY WALLETS & POINT TRANSACTIONS
        // =========================================================================
        private static async Task SeedMemberTiersAndLoyaltyAsync(ApplicationDbContext context)
        {
            // Đảm bảo đầy đủ 5 bậc hạng thành viên: Bronze, Silver, Gold, Platinum, Diamond
            var defaultTiers = new List<MemberTier>
            {
                new()
                {
                    TierName = "Bronze",
                    MinSpend = 0m,
                    MaxSpend = 2000000m,
                    PointMultiplier = 1.0m,
                    BirthdayBonusPoints = 30,
                    Description = "Hạng Đồng: Thành viên mới hoàn tất xác thực, tích điểm cơ bản 10.000 VNĐ = 1 điểm. Thưởng 30 điểm sinh nhật.",
                    SortOrder = 1,
                    IsActive = true
                },
                new()
                {
                    TierName = "Silver",
                    MinSpend = 2000001m,
                    MaxSpend = 5000000m,
                    PointMultiplier = 1.2m,
                    BirthdayBonusPoints = 50,
                    Description = "Hạng Bạc: Chi tiêu > 2 triệu, tích điểm x1.2, đặc quyền chiết khấu 5%. Thưởng 50 điểm sinh nhật.",
                    SortOrder = 2,
                    IsActive = true
                },
                new()
                {
                    TierName = "Gold",
                    MinSpend = 5000001m,
                    MaxSpend = 15000000m,
                    PointMultiplier = 1.5m,
                    BirthdayBonusPoints = 150,
                    Description = "Hạng Vàng: Chi tiêu > 5 triệu, tích điểm x1.5, đặc quyền chiết khấu 10%. Thưởng 150 điểm sinh nhật và quà tặng GWP độc quyền.",
                    SortOrder = 3,
                    IsActive = true
                },
                new()
                {
                    TierName = "Platinum",
                    MinSpend = 15000001m,
                    MaxSpend = 30000000m,
                    PointMultiplier = 1.8m,
                    BirthdayBonusPoints = 200,
                    Description = "Hạng Bạch Kim: Chi tiêu > 15 triệu, tích điểm x1.8, đặc quyền chiết khấu 12%. Thưởng 200 điểm sinh nhật.",
                    SortOrder = 4,
                    IsActive = true
                },
                new()
                {
                    TierName = "Diamond",
                    MinSpend = 30000001m,
                    MaxSpend = null,
                    PointMultiplier = 2.0m,
                    BirthdayBonusPoints = 300,
                    Description = "Hạng Kim Cương: Chi tiêu > 30 triệu, tích điểm x2.0 VIP, đặc quyền chiết khấu 15%. Thưởng 300 điểm sinh nhật, ưu đãi tư vấn 1-1.",
                    SortOrder = 5,
                    IsActive = true
                }
            };

            foreach (var dt in defaultTiers)
            {
                var existingTier = await context.MemberTiers.FirstOrDefaultAsync(t => t.TierName == dt.TierName);
                if (existingTier == null)
                {
                    await context.MemberTiers.AddAsync(dt);
                }
                else
                {
                    existingTier.MinSpend = dt.MinSpend;
                    existingTier.MaxSpend = dt.MaxSpend;
                    existingTier.PointMultiplier = dt.PointMultiplier;
                    existingTier.BirthdayBonusPoints = dt.BirthdayBonusPoints;
                    existingTier.Description = dt.Description;
                    existingTier.SortOrder = dt.SortOrder;
                    existingTier.IsActive = true;
                }
            }
            await context.SaveChangesAsync();

            var tiersDict = await context.MemberTiers.ToDictionaryAsync(t => t.TierName, t => t.Id);

            // Seed Wallets cho 3 khách hàng test (customer01, customer02, customer_vip)
            var customer01 = await context.Users.FirstOrDefaultAsync(u => u.Username == "customer01");
            var customer02 = await context.Users.FirstOrDefaultAsync(u => u.Username == "customer02");
            var customerVip = await context.Users.FirstOrDefaultAsync(u => u.Username == "customer_vip");

            if (customer01 != null && !await context.LoyaltyWallets.AnyAsync(w => w.UserId == customer01.Id))
            {
                var wallet01 = new LoyaltyWallet
                {
                    UserId = customer01.Id,
                    CurrentTierId = tiersDict["Silver"],
                    TotalSpend12Months = 500000m,
                    TotalPointsEarned = 50,
                    CurrentBalance = 50,
                    MemberSince = DateTime.UtcNow.AddMonths(-2),
                    QrCodeData = $"LOYALTY-{customer01.Id}-SILVER"
                };
                await context.LoyaltyWallets.AddAsync(wallet01);
                await context.SaveChangesAsync();

                await context.PointTransactions.AddAsync(new PointTransaction
                {
                    WalletId = wallet01.Id,
                    TransactionType = "EARN",
                    Points = 50,
                    BalanceBefore = 0,
                    BalanceAfter = 50,
                    Description = "Tích điểm đơn hàng đầu tiên chào mừng",
                    CreatedAt = DateTime.UtcNow.AddMonths(-1)
                });
            }

            if (customer02 != null && !await context.LoyaltyWallets.AnyAsync(w => w.UserId == customer02.Id))
            {
                var wallet02 = new LoyaltyWallet
                {
                    UserId = customer02.Id,
                    CurrentTierId = tiersDict["Gold"],
                    TotalSpend12Months = 5000000m,
                    TotalPointsEarned = 600,
                    CurrentBalance = 400,
                    MemberSince = DateTime.UtcNow.AddMonths(-6),
                    QrCodeData = $"LOYALTY-{customer02.Id}-GOLD"
                };
                await context.LoyaltyWallets.AddAsync(wallet02);
                await context.SaveChangesAsync();

                await context.PointTransactions.AddRangeAsync(
                    new PointTransaction
                    {
                        WalletId = wallet02.Id,
                        TransactionType = "EARN",
                        Points = 500,
                        BalanceBefore = 0,
                        BalanceAfter = 500,
                        Description = "Tích điểm đơn hàng mua mỹ phẩm Skincare",
                        CreatedAt = DateTime.UtcNow.AddMonths(-3)
                    },
                    new PointTransaction
                    {
                        WalletId = wallet02.Id,
                        TransactionType = "BONUS",
                        Points = 100,
                        BalanceBefore = 500,
                        BalanceAfter = 600,
                        Description = "Thưởng thăng hạng Gold Member",
                        CreatedAt = DateTime.UtcNow.AddMonths(-2)
                    },
                    new PointTransaction
                    {
                        WalletId = wallet02.Id,
                        TransactionType = "REDEEM",
                        Points = -200,
                        BalanceBefore = 600,
                        BalanceAfter = 400,
                        Description = "Đổi voucher giảm 200.000 VNĐ",
                        CreatedAt = DateTime.UtcNow.AddMonths(-1)
                    }
                );
            }

            if (customerVip != null && !await context.LoyaltyWallets.AnyAsync(w => w.UserId == customerVip.Id))
            {
                var walletVip = new LoyaltyWallet
                {
                    UserId = customerVip.Id,
                    CurrentTierId = tiersDict["Diamond"],
                    TotalSpend12Months = 15000000m,
                    TotalPointsEarned = 2500,
                    CurrentBalance = 1800,
                    MemberSince = DateTime.UtcNow.AddYears(-1),
                    QrCodeData = $"LOYALTY-{customerVip.Id}-DIAMOND"
                };
                await context.LoyaltyWallets.AddAsync(walletVip);
                await context.SaveChangesAsync();

                await context.PointTransactions.AddRangeAsync(
                    new PointTransaction
                    {
                        WalletId = walletVip.Id,
                        TransactionType = "EARN",
                        Points = 2000,
                        BalanceBefore = 0,
                        BalanceAfter = 2000,
                        Description = "Tích điểm đơn hàng trọn bộ mỹ phẩm Luxury",
                        CreatedAt = DateTime.UtcNow.AddMonths(-4)
                    },
                    new PointTransaction
                    {
                        WalletId = walletVip.Id,
                        TransactionType = "BONUS",
                        Points = 300,
                        BalanceBefore = 2000,
                        BalanceAfter = 2300,
                        Description = "Thưởng sinh nhật Diamond Member VIP",
                        CreatedAt = DateTime.UtcNow.AddMonths(-2)
                    },
                    new PointTransaction
                    {
                        WalletId = walletVip.Id,
                        TransactionType = "REDEEM",
                        Points = -500,
                        BalanceBefore = 2300,
                        BalanceAfter = 1800,
                        Description = "Đổi voucher mua sắm 500.000 VNĐ",
                        CreatedAt = DateTime.UtcNow.AddDays(-10)
                    }
                );
            }

            await context.SaveChangesAsync();

            // Seed Vouchers cho customer_vip (1 đã dùng, 1 chưa dùng)
            if (customerVip != null && !await context.Vouchers.AnyAsync(v => v.UserId == customerVip.Id))
            {
                await context.Vouchers.AddRangeAsync(
                    new Voucher
                    {
                        UserId = customerVip.Id,
                        VoucherCode = "VIPDIAMOND-USED-100K",
                        Description = "Voucher độc quyền Diamond 100K",
                        DiscountType = "FIXED_AMOUNT",
                        DiscountValue = 100000m,
                        MinOrderValue = 500000m,
                        ValidFrom = DateTime.UtcNow.AddMonths(-2),
                        ValidTo = DateTime.UtcNow.AddMonths(1),
                        IsUsed = true,
                        UsedAt = DateTime.UtcNow.AddDays(-15)
                    },
                    new Voucher
                    {
                        UserId = customerVip.Id,
                        VoucherCode = "VIPDIAMOND-ACTIVE-200K",
                        Description = "Voucher tri ân khách hàng Kim Cương 200K",
                        DiscountType = "FIXED_AMOUNT",
                        DiscountValue = 200000m,
                        MinOrderValue = 1000000m,
                        ValidFrom = DateTime.UtcNow.AddDays(-5),
                        ValidTo = DateTime.UtcNow.AddDays(60),
                        IsUsed = false
                    }
                );
                await context.SaveChangesAsync();
            }

            // =========================================================================
            // SEED / CẬP NHẬT TRỌN BỘ TÀI KHOẢN KHÁCH HÀNG VIP TEST (SENIOR QA REQUIREMENT)
            // - customer_diamond: Kim Cương (> 30tr, 1.500 điểm, đặc quyền giảm 15%)
            // - customer_platinum: Bạch Kim (> 15tr, 800 điểm, đặc quyền giảm 12%)
            // - customer_gold: Vàng (> 5tr, 350 điểm, đặc quyền giảm 10%)
            // - customer_silver: Bạc (> 2tr, 120 điểm, đặc quyền giảm 5%)
            // - customer_bronze: Khách mới (50 điểm chào mừng)
            // =========================================================================
            var vipAccountsData = new List<(string Username, string TierName, decimal Spend, int Balance, int Earned, string Note)>
            {
                ("customer_diamond", "Diamond", 35000000m, 1500, 2500, "Khách hàng Kim Cương VIP (Chi tiêu > 30 triệu, 1.500 điểm, đặc quyền giảm 15%)"),
                ("customer_platinum", "Platinum", 18000000m, 800, 1200, "Khách hàng Bạch Kim VIP (Chi tiêu > 15 triệu, 800 điểm, đặc quyền giảm 12%)"),
                ("customer_gold", "Gold", 7500000m, 350, 600, "Khách hàng Vàng VIP (Chi tiêu > 5 triệu, 350 điểm, đặc quyền giảm 10%)"),
                ("customer_silver", "Silver", 2500000m, 120, 200, "Khách hàng Bạc (Chi tiêu > 2 triệu, 120 điểm, đặc quyền giảm 5%)"),
                ("customer_bronze", "Bronze", 0m, 50, 50, "Khách hàng mới hoàn tất xác thực (50 điểm chào mừng)")
            };

            foreach (var (uname, tname, spend, balance, earned, note) in vipAccountsData)
            {
                var user = await context.Users.FirstOrDefaultAsync(u => u.Username == uname);
                if (user != null && tiersDict.TryGetValue(tname, out var tierId))
                {
                    var wallet = await context.LoyaltyWallets.FirstOrDefaultAsync(w => w.UserId == user.Id);
                    if (wallet == null)
                    {
                        wallet = new LoyaltyWallet
                        {
                            UserId = user.Id,
                            CurrentTierId = tierId,
                            TotalSpend12Months = spend,
                            TotalPointsEarned = earned,
                            CurrentBalance = balance,
                            MemberSince = DateTime.UtcNow.AddMonths(-6),
                            QrCodeData = $"LOYALTY-{user.Id}-{tname.ToUpperInvariant()}"
                        };
                        await context.LoyaltyWallets.AddAsync(wallet);
                        await context.SaveChangesAsync();

                        await context.PointTransactions.AddAsync(new PointTransaction
                        {
                            WalletId = wallet.Id,
                            TransactionType = "EARN",
                            Points = balance,
                            BalanceBefore = 0,
                            BalanceAfter = balance,
                            Description = note,
                            CreatedAt = DateTime.UtcNow.AddDays(-7)
                        });
                        await context.SaveChangesAsync();
                    }
                    else
                    {
                        wallet.CurrentTierId = tierId;
                        wallet.TotalSpend12Months = spend;
                        wallet.CurrentBalance = balance;
                        wallet.TotalPointsEarned = earned;
                        await context.SaveChangesAsync();
                    }

                    // Tặng thêm voucher cho customer_diamond để kiểm thử checkout kết hợp voucher
                    if (tname == "Diamond" && !await context.Vouchers.AnyAsync(v => v.UserId == user.Id && v.VoucherCode == "VIPDIAMOND-TEST-200K"))
                    {
                        await context.Vouchers.AddAsync(new Voucher
                        {
                            UserId = user.Id,
                            VoucherCode = "VIPDIAMOND-TEST-200K",
                            Description = "Voucher tri ân độc quyền Kim Cương 200.000₫",
                            DiscountType = "FIXED_AMOUNT",
                            DiscountValue = 200000m,
                            MinOrderValue = 500000m,
                            ValidFrom = DateTime.UtcNow.AddDays(-1),
                            ValidTo = DateTime.UtcNow.AddDays(90),
                            IsUsed = false
                        });
                        await context.SaveChangesAsync();
                    }
                }
            }
        }

        // =========================================================================
        // 9. SEED CRM: CUSTOMER PROFILES, SKIN HISTORIES & REPURCHASE
        // =========================================================================
        private static async Task SeedCustomerProfilesAndCrmAsync(ApplicationDbContext context)
        {
            var customer01 = await context.Users.FirstOrDefaultAsync(u => u.Username == "customer01");
            var customer02 = await context.Users.FirstOrDefaultAsync(u => u.Username == "customer02");
            var customerVip = await context.Users.FirstOrDefaultAsync(u => u.Username == "customer_vip");
            var manager = await context.Users.FirstOrDefaultAsync(u => u.Username == "manager_hq");
            var productSerum = await context.Products.FirstOrDefaultAsync(p => p.Sku == "LOR-HYA-B5");

            if (customer01 != null && !await context.CustomerProfiles.AnyAsync(p => p.UserId == customer01.Id))
            {
                var profile = new CustomerProfile
                {
                    UserId = customer01.Id,
                    Gender = "Female",
                    CurrentSkinType = "Da dầu",
                    SkinConcerns = "[\"Mụn ẩn\", \"Dầu thừa vùng chữ T\"]",
                    KnownAllergies = "[\"Cồn khô\"]",
                    PreferredBrands = "[\"Rohto\", \"La Roche-Posay\"]",
                    Note = "Khách hàng mới bắt đầu xây dựng routine dưỡng da kiềm dầu",
                    ConsultantUserId = manager?.Id,
                    LastConsultationDate = DateTime.UtcNow.AddDays(-20)
                };
                await context.CustomerProfiles.AddAsync(profile);
                await context.SaveChangesAsync();

                await context.SkinHistories.AddAsync(new SkinHistory
                {
                    CustomerProfileId = profile.Id,
                    SkinType = "Da dầu mụn",
                    SkinCondition = "Nhiều sợi bã nhờn cánh mũi và mụn cám trán",
                    TreatmentNote = "Khuyên dùng sữa rửa mặt dịu nhẹ Hada Labo + BHA 2%",
                    RecordedBy = manager?.Id,
                    RecordedAt = DateTime.UtcNow.AddDays(-20)
                });
            }

            if (customerVip != null && !await context.CustomerProfiles.AnyAsync(p => p.UserId == customerVip.Id))
            {
                var profileVip = new CustomerProfile
                {
                    UserId = customerVip.Id,
                    Gender = "Female",
                    CurrentSkinType = "Da hỗn hợp thiên khô",
                    SkinConcerns = "[\"Lão hóa\", \"Nếp nhăn khóe mắt\"]",
                    KnownAllergies = "[]",
                    PreferredBrands = "[\"Laneige\", \"L'Oréal Paris\"]",
                    Note = "Khách VIP trung thành, ưu tiên các sản phẩm cao cấp phục hồi da",
                    ConsultantUserId = manager?.Id,
                    LastConsultationDate = DateTime.UtcNow.AddDays(-5)
                };
                await context.CustomerProfiles.AddAsync(profileVip);
                await context.SaveChangesAsync();

                if (productSerum != null)
                {
                    await context.RepurchaseSchedules.AddAsync(new RepurchaseSchedule
                    {
                        CustomerProfileId = profileVip.Id,
                        ProductId = productSerum.Id,
                        LastPurchaseDate = DateTime.UtcNow.AddDays(-40),
                        EstimatedVolumeMl = 30m,
                        DailyUsageMl = 0.5m,
                        EstimatedDepletionDate = DateTime.UtcNow.AddDays(20),
                        IsNotified = false
                    });
                }
            }

            // Seed hồ sơ chăm sóc da chuyên biệt cho 5 tài khoản VIP
            var vipProfilesData = new List<(string Username, string SkinType, string Concerns, string Allergies, string Note)>
            {
                ("customer_diamond", "Da hỗn hợp thiên khô", "[\"Lão hóa\", \"Nếp nhăn\"]", "[]", "Khách hàng Kim Cương - VIP cao cấp, tích lũy 1.500 điểm"),
                ("customer_platinum", "Da thường", "[\"Dưỡng sáng\", \"Đều màu da\"]", "[\"Cồn khô\"]", "Khách hàng Bạch Kim - Tích lũy 800 điểm, chiết khấu 12%"),
                ("customer_gold", "Da dầu mụn", "[\"Mụn ẩn\", \"Dầu nhờn\"]", "[\"Hương liệu\"]", "Khách hàng Vàng - Tích lũy 350 điểm, chiết khấu 10%"),
                ("customer_silver", "Da nhạy cảm", "[\"Mẩn đỏ\", \"Khô ráp\"]", "[\"Paraben\"]", "Khách hàng Bạc - Tích lũy 120 điểm, chiết khấu 5%"),
                ("customer_bronze", "Da hỗn hợp", "[\"Cấp ẩm cơ bản\"]", "[]", "Khách hàng mới Bronze - Hoàn tất xác thực, 50 điểm chào mừng")
            };

            foreach (var (uname, skinType, concerns, allergies, note) in vipProfilesData)
            {
                var user = await context.Users.FirstOrDefaultAsync(u => u.Username == uname);
                if (user != null)
                {
                    var prof = await context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
                    if (prof == null)
                    {
                        prof = new CustomerProfile
                        {
                            UserId = user.Id,
                            Gender = "Female",
                            CurrentSkinType = skinType,
                            SkinConcerns = concerns,
                            KnownAllergies = allergies,
                            Note = note,
                            ConsultantUserId = manager?.Id,
                            LastConsultationDate = DateTime.UtcNow.AddDays(-3)
                        };
                        await context.CustomerProfiles.AddAsync(prof);
                    }
                    else
                    {
                        prof.CurrentSkinType = skinType;
                        prof.SkinConcerns = concerns;
                        prof.KnownAllergies = allergies;
                        prof.Note = note;
                    }
                }
            }

            await context.SaveChangesAsync();
        }

        // =========================================================================
        // 10. SEED PROMOTIONS & COUPONS
        // =========================================================================
        private static async Task SeedPromotionsAndCouponsAsync(ApplicationDbContext context)
        {
            var now = DateTime.UtcNow;

            if (!await context.Promotions.AnyAsync())
            {
                var promoFlashSale = new Promotion
                {
                    PromotionCode = "PROMO-FLASH20",
                    Name = "Flash Sale Đẹp Rạng Ngời - Giảm 20%",
                    Description = "Giảm giá 20% cho toàn bộ sản phẩm Trang điểm",
                    PromotionType = "FLASH_SALE",
                    DiscountValue = 20m,
                    MinOrderValue = 200000m,
                    MaxDiscountAmount = 100000m,
                    ValidFrom = now.AddDays(-2),
                    ValidTo = now.AddDays(15),
                    UsageLimit = 500,
                    UsageCount = 18,
                    IsActive = true,
                    AppliesTo = "SPECIFIC_CATEGORIES"
                };

                var promoBuyXGetY = new Promotion
                {
                    PromotionCode = "PROMO-BUYSERUM-GETWASH",
                    Name = "Mua Serum Phục Hồi Tặng Sữa Rửa Mặt Hada Labo 0đ",
                    Description = "Mua Serum La Roche-Posay Hyalu B5 tặng kèm Sữa rửa mặt Hada Labo",
                    PromotionType = "BUY_X_GET_Y",
                    DiscountValue = 0m,
                    ValidFrom = now.AddDays(-10),
                    ValidTo = now.AddDays(30),
                    IsActive = true,
                    AppliesTo = "SPECIFIC_PRODUCTS"
                };

                await context.Promotions.AddRangeAsync(promoFlashSale, promoBuyXGetY);
                await context.SaveChangesAsync();

                var productSerum = await context.Products.FirstOrDefaultAsync(p => p.Sku == "LOR-HYA-B5");
                var productWash = await context.Products.FirstOrDefaultAsync(p => p.Sku == "ROH-HDL-100");

                if (productSerum != null && productWash != null)
                {
                    await context.PromotionItems.AddAsync(new PromotionItem
                    {
                        PromotionId = promoBuyXGetY.Id,
                        ProductId = productSerum.Id,
                        GiftProductId = productWash.Id,
                        GiftQuantity = 1
                    });
                    await context.SaveChangesAsync();
                }
            }

            if (!await context.Coupons.AnyAsync())
            {
                var couponBeauty = new Coupon
                {
                    CouponCode = "BEAUTY10",
                    Description = "Giảm 10% tối đa 50.000 VNĐ cho đơn từ 200.000 VNĐ",
                    DiscountType = "PERCENTAGE",
                    DiscountValue = 10m,
                    MinOrderValue = 200000m,
                    MaxDiscountAmount = 50000m,
                    ValidFrom = now.AddDays(-10),
                    ValidTo = now.AddDays(60),
                    UsageLimit = 1000,
                    UsageCount = 24,
                    IsActive = true
                };

                var couponVipGold = new Coupon
                {
                    CouponCode = "VIPGOLD15",
                    Description = "Giảm 15% tối đa 150.000 VNĐ cho đơn từ 500.000 VNĐ",
                    DiscountType = "PERCENTAGE",
                    DiscountValue = 15m,
                    MinOrderValue = 500000m,
                    MaxDiscountAmount = 150000m,
                    ValidFrom = now.AddDays(-5),
                    ValidTo = now.AddDays(90),
                    UsageLimit = 200,
                    UsageCount = 12,
                    IsActive = true
                };

                await context.Coupons.AddRangeAsync(couponBeauty, couponVipGold);
                await context.SaveChangesAsync();
            }
        }

        // =========================================================================
        // 11. SEED POS SHIFTS, ORDERS & INVOICES (ĐA DẠNG POS, WEBSITE & COD)
        // =========================================================================
        private static async Task SeedPosShiftsAndOrdersAsync(ApplicationDbContext context)
        {
            if (await context.Orders.AnyAsync()) return;

            var stores = await context.Stores.ToListAsync();
            var hqStore = stores.FirstOrDefault(s => s.Code == "SH-Q1" || s.Code == "HQ-001") ?? stores[0];
            var cgStore = stores.FirstOrDefault(s => s.Code == "SH-BD" || s.Code == "SH-CG") ?? (stores.Count > 1 ? stores[1] : hqStore);

            var cashier01 = await context.Users.FirstOrDefaultAsync(u => u.Username == "cashier01") ?? await context.Users.FirstAsync();
            var customer01 = await context.Users.FirstOrDefaultAsync(u => u.Username == "customer01");
            var customer02 = await context.Users.FirstOrDefaultAsync(u => u.Username == "customer02");
            var customerVip = await context.Users.FirstOrDefaultAsync(u => u.Username == "customer_vip");

            var products = await context.Products.Include(p => p.Batches).Take(5).ToListAsync();
            if (products.Count < 2) return;

            var now = DateTime.UtcNow;

            // 1. Tạo 1 Ca POS Đã Đóng (Yesterday) & 1 Ca POS Đang Mở (Today)
            var closedShift = new PosShift
            {
                ShiftCode = $"SHIFT-{now.AddDays(-1):yyyyMMdd}-001",
                StoreId = hqStore.Id,
                CashierId = cashier01.Id,
                OpenedAt = now.AddDays(-1).AddHours(-8),
                ClosedAt = now.AddDays(-1),
                OpeningBalance = 1000000m,
                ExpectedCash = 1850000m,
                ActualCash = 1850000m,
                Difference = 0m,
                CashSalesTotal = 850000m,
                CardSalesTotal = 0m,
                TransactionCount = 2,
                Status = "CLOSED",
                Note = "Ca chiều khớp tiền 100%"
            };

            var openShift = new PosShift
            {
                ShiftCode = $"SHIFT-{now:yyyyMMdd}-001",
                StoreId = hqStore.Id,
                CashierId = cashier01.Id,
                OpenedAt = now.AddHours(-3),
                OpeningBalance = 1000000m,
                ExpectedCash = 1500000m,
                ActualCash = 0m,
                Difference = 0m,
                CashSalesTotal = 500000m,
                CardSalesTotal = 0m,
                TransactionCount = 1,
                Status = "OPEN",
                Note = "Ca sáng đang hoạt động"
            };

            await context.PosShifts.AddRangeAsync(closedShift, openShift);
            await context.SaveChangesAsync();

            // Transaction ngăn kéo
            await context.PosDrawerTransactions.AddAsync(new PosDrawerTransaction
            {
                PosShiftId = closedShift.Id,
                Type = "CASH_IN",
                Amount = 1000000m,
                Reason = "Tiền mặt đầu ca bán hàng",
                PerformedBy = cashier01.Id,
                CreatedAt = closedShift.OpenedAt
            });
            await context.SaveChangesAsync();

            // 2. Tạo đơn POS 01 (COMPLETED)
            var p1 = products[0];
            var batch1 = p1.Batches.FirstOrDefault();
            var orderPos1 = new Order
            {
                OrderCode = $"ORD-POS-{now.AddDays(-1):yyyyMMdd}-001",
                StoreId = hqStore.Id,
                PosShiftId = closedShift.Id,
                UserId = customer01?.Id,
                OrderSource = "POS",
                OrderStatus = "COMPLETED",
                PaymentStatus = "PAID",
                PaymentMethod = "CASH",
                SubTotal = p1.SellingPrice * 2,
                DiscountAmount = 0m,
                ShippingFee = 0m,
                TotalAmount = p1.SellingPrice * 2,
                CustomerName = customer01?.FullName ?? "Khách Lẻ POS",
                CustomerPhone = customer01?.PhoneNumber ?? "0909000009",
                CustomerEmail = customer01?.Email,
                CreatedAt = now.AddDays(-1).AddHours(-4)
            };
            await context.Orders.AddAsync(orderPos1);
            await context.SaveChangesAsync();

            var od1 = new OrderDetail
            {
                OrderId = orderPos1.Id,
                ProductId = p1.Id,
                ProductBatchId = batch1?.Id,
                Quantity = 2,
                UnitPrice = p1.SellingPrice,
                DiscountRate = 0m,
                LineTotal = p1.SellingPrice * 2
            };
            await context.OrderDetails.AddAsync(od1);

            var inv1 = new Invoice
            {
                InvoiceCode = $"INV-{now.AddDays(-1):yyyyMMdd}-001",
                OrderId = orderPos1.Id,
                InvoiceDate = orderPos1.CreatedAt,
                PaymentMethod = "CASH",
                TaxRate = 8m,
                TaxAmount = Math.Round(orderPos1.TotalAmount * 0.08m, 2),
                TotalAmount = orderPos1.TotalAmount,
                CashierId = cashier01.Id,
                Status = "ISSUED",
                CreatedAt = orderPos1.CreatedAt
            };
            await context.Invoices.AddAsync(inv1);

            // 3. Tạo đơn WEBSITE 01 (COMPLETED, Đã thanh toán VNPAY)
            var p2 = products[1];
            var batch2 = p2.Batches.FirstOrDefault();
            var orderWebCompleted = new Order
            {
                OrderCode = $"ORD-WEB-{now:yyyyMMdd}-002",
                StoreId = hqStore.Id,
                UserId = customer02?.Id,
                OrderSource = "WEBSITE",
                OrderStatus = "COMPLETED",
                PaymentStatus = "PAID",
                PaymentMethod = "VNPAY",
                SubTotal = p2.SellingPrice,
                DiscountAmount = 20000m,
                ShippingFee = 30000m,
                TotalAmount = p2.SellingPrice - 20000m + 30000m,
                CustomerName = customer02?.FullName ?? "Mai Khách Hàng",
                CustomerPhone = customer02?.PhoneNumber ?? "0909000010",
                CustomerEmail = customer02?.Email,
                ShippingAddress = "72 Lê Thánh Tôn, Bến Nghé, Quận 1, TP.HCM",
                CreatedAt = now.AddHours(-10)
            };
            await context.Orders.AddAsync(orderWebCompleted);
            await context.SaveChangesAsync();

            var od2 = new OrderDetail
            {
                OrderId = orderWebCompleted.Id,
                ProductId = p2.Id,
                ProductBatchId = batch2?.Id,
                Quantity = 1,
                UnitPrice = p2.SellingPrice,
                DiscountRate = 0m,
                LineTotal = p2.SellingPrice
            };
            await context.OrderDetails.AddAsync(od2);

            var inv2 = new Invoice
            {
                InvoiceCode = $"INV-{now:yyyyMMdd}-002",
                OrderId = orderWebCompleted.Id,
                InvoiceDate = orderWebCompleted.CreatedAt,
                PaymentMethod = "VNPAY",
                TaxRate = 8m,
                TaxAmount = Math.Round(orderWebCompleted.TotalAmount * 0.08m, 2),
                TotalAmount = orderWebCompleted.TotalAmount,
                Status = "ISSUED",
                CreatedAt = orderWebCompleted.CreatedAt
            };
            await context.Invoices.AddAsync(inv2);

            // 4. Tạo đơn WEBSITE 02 (PENDING, Khách đặt chờ giao)
            var orderWebPending = new Order
            {
                OrderCode = $"ORD-WEB-{now:yyyyMMdd}-003",
                StoreId = cgStore.Id,
                UserId = customerVip?.Id,
                OrderSource = "WEBSITE",
                OrderStatus = "PENDING",
                PaymentStatus = "UNPAID",
                PaymentMethod = "COD",
                SubTotal = p1.SellingPrice,
                DiscountAmount = 0m,
                ShippingFee = 30000m,
                TotalAmount = p1.SellingPrice + 30000m,
                CustomerName = customerVip?.FullName ?? "Linh VIP",
                CustomerPhone = customerVip?.PhoneNumber ?? "0909000011",
                CustomerEmail = customerVip?.Email,
                ShippingAddress = "12 Nguyễn Khánh Toàn, Cầu Giấy, Hà Nội",
                CreatedAt = now.AddMinutes(-30)
            };
            await context.Orders.AddAsync(orderWebPending);
            await context.SaveChangesAsync();

            await context.OrderDetails.AddAsync(new OrderDetail
            {
                OrderId = orderWebPending.Id,
                ProductId = p1.Id,
                ProductBatchId = batch1?.Id,
                Quantity = 1,
                UnitPrice = p1.SellingPrice,
                DiscountRate = 0m,
                LineTotal = p1.SellingPrice
            });

            await context.SaveChangesAsync();
        }

        // =========================================================================
        // 12. SEED STOCK TRANSFERS (ĐIỀU CHUYỂN KHO LIÊN CHI NHÁNH MẪU)
        // =========================================================================
        private static async Task SeedStockTransfersAsync(ApplicationDbContext context)
        {
            if (!await context.StockTransfers.AnyAsync())
            {
                var stores = await context.Stores.ToListAsync();
                if (stores.Count < 2) return;

                var product = await context.Products.Include(p => p.Batches).FirstOrDefaultAsync();
                var user = await context.Users.FirstOrDefaultAsync(u => u.Username == "warehouse01") ?? await context.Users.FirstAsync();
                var approver = await context.Users.FirstOrDefaultAsync(u => u.Username == "superadmin") ?? user;

                if (product == null) return;
                var batch = product.Batches.FirstOrDefault();

                var transfer = new StockTransfer
                {
                    TransferCode = "TRF-20260930-001",
                    SourceStoreId = stores[0].Id,
                    DestinationStoreId = stores[1].Id,
                    CreatedByUserId = user.Id,
                    ApprovedByUserId = approver.Id,
                    Status = "IN_TRANSIT",
                    TotalRequestedQuantity = 20,
                    TotalSentQuantity = 20,
                    TotalReceivedQuantity = 0,
                    Note = $"Điều chuyển 20 sản phẩm {product.Name} từ {stores[0].Name} sang {stores[1].Name} theo lô FEFO",
                    RequestedAt = DateTime.UtcNow.AddHours(-6),
                    ApprovedAt = DateTime.UtcNow.AddHours(-4),
                    CreatedAt = DateTime.UtcNow.AddHours(-6),
                    Details = new List<StockTransferDetail>
                    {
                        new()
                        {
                            ProductId = product.Id,
                            ProductBatchId = batch?.Id,
                            RequestedQuantity = 20,
                            SentQuantity = 20,
                            ReceivedQuantity = 0,
                            Note = "Xuất lô cận date ưu tiên"
                        }
                    }
                };

                await context.StockTransfers.AddAsync(transfer);
                await context.SaveChangesAsync();
            }
        }

        // =========================================================================
        // 13. SEED CMS: MENUS, BANNERS, POSTS, STATIC PAGES & PRODUCT REVIEWS
        // =========================================================================
        private static async Task SeedCmsDataAsync(ApplicationDbContext context)
        {
            var now = DateTime.UtcNow;

            // 1. Menus & MenuItems
            if (!await context.Menus.AnyAsync())
            {
                var menuHeader = new Menu { Code = "HEADER_MAIN", Title = "Menu Điều Hướng Chính", Position = "TOP", IsActive = true };
                var menuFooter = new Menu { Code = "FOOTER_MAIN", Title = "Menu Chân Trang", Position = "BOTTOM", IsActive = true };

                await context.Menus.AddRangeAsync(menuHeader, menuFooter);
                await context.SaveChangesAsync();

                var miHome = new MenuItem { MenuId = menuHeader.Id, Title = "Trang Chủ", Url = "/", LinkType = "CUSTOM", SortOrder = 1, IsActive = true };
                var miSkincare = new MenuItem { MenuId = menuHeader.Id, Title = "Chăm Sóc Da", Url = "/category/cham-soc-da", LinkType = "CATEGORY", SortOrder = 2, IsActive = true };
                var miMakeup = new MenuItem { MenuId = menuHeader.Id, Title = "Trang Điểm", Url = "/category/trang-diem", LinkType = "CATEGORY", SortOrder = 3, IsActive = true };
                var miPerfume = new MenuItem { MenuId = menuHeader.Id, Title = "Nước Hoa", Url = "/category/nuoc-hoa", LinkType = "CATEGORY", SortOrder = 4, IsActive = true };
                var miFefo = new MenuItem { MenuId = menuHeader.Id, Title = "Deal Cận Date (FEFO)", Url = "/clearance-fefo", LinkType = "CUSTOM", SortOrder = 5, IsActive = true };

                await context.MenuItems.AddRangeAsync(miHome, miSkincare, miMakeup, miPerfume, miFefo);
                await context.SaveChangesAsync();

                // Menu items con của Skincare
                await context.MenuItems.AddRangeAsync(
                    new MenuItem { MenuId = menuHeader.Id, ParentId = miSkincare.Id, Title = "Sữa Rửa Mặt", Url = "/category/sua-rua-mat", LinkType = "CATEGORY", SortOrder = 1, IsActive = true },
                    new MenuItem { MenuId = menuHeader.Id, ParentId = miSkincare.Id, Title = "Serum Phục Hồi", Url = "/category/serum-tinh-chat", LinkType = "CATEGORY", SortOrder = 2, IsActive = true },
                    new MenuItem { MenuId = menuHeader.Id, ParentId = miSkincare.Id, Title = "Kem Chống Nắng", Url = "/category/kem-chong-nang", LinkType = "CATEGORY", SortOrder = 3, IsActive = true }
                );

                // Footer items
                await context.MenuItems.AddRangeAsync(
                    new MenuItem { MenuId = menuFooter.Id, Title = "Giới Thiệu Về Chúng Tôi", Url = "/page/gioi-thieu", LinkType = "STATIC_PAGE", SortOrder = 1, IsActive = true },
                    new MenuItem { MenuId = menuFooter.Id, Title = "Chính Sách Đổi Trả & FEFO", Url = "/page/chinh-sach-doi-tra", LinkType = "STATIC_PAGE", SortOrder = 2, IsActive = true },
                    new MenuItem { MenuId = menuFooter.Id, Title = "Hệ Thống Chuỗi Cửa Hàng", Url = "/stores", LinkType = "CUSTOM", SortOrder = 3, IsActive = true }
                );
                await context.SaveChangesAsync();
            }

            // 2. Banners
            if (!await context.Banners.AnyAsync())
            {
                await context.Banners.AddRangeAsync(
                    new Banner
                    {
                        Title = "SUMMER BEAUTY VIBES",
                        Subtitle = "Bừng Sáng Nhan Sắc Mùa Hè - Ưu Đãi Đến 40%",
                        ImageUrl = "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=1600&h=600&fit=crop",
                        LinkUrl = "/category/trang-diem",
                        ButtonText = "Khám Phá Ngay",
                        Position = "HERO_HOME",
                        SortOrder = 1,
                        IsActive = true,
                        StartDate = now.AddDays(-15),
                        EndDate = now.AddDays(90)
                    },
                    new Banner
                    {
                        Title = "K-BEAUTY GLOW SKIN",
                        Subtitle = "Bí Quyết Da Căng Bóng Chuẩn Hàn Cùng Laneige & Rom&nd",
                        ImageUrl = "https://images.unsplash.com/photo-1512496015851-a90fb38ba796?w=1600&h=600&fit=crop",
                        LinkUrl = "/category/cham-soc-da",
                        ButtonText = "Mua Ngay",
                        Position = "HERO_HOME",
                        SortOrder = 2,
                        IsActive = true,
                        StartDate = now.AddDays(-10),
                        EndDate = now.AddDays(60)
                    },
                    new Banner
                    {
                        Title = "FEFO SMART CLEARANCE",
                        Subtitle = "Săn Mỹ Phẩm Chính Hãng Cận Date Giá Cực Hời - Cam Kết Hạn Sử Dụng Minh Bạch",
                        ImageUrl = "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?w=1600&h=600&fit=crop",
                        LinkUrl = "/clearance-fefo",
                        ButtonText = "Săn Deal Cận Date",
                        Position = "MID_PROMO",
                        SortOrder = 3,
                        IsActive = true,
                        StartDate = now,
                        EndDate = now.AddDays(30)
                    }
                );
                await context.SaveChangesAsync();
            }

            // 3. PostCategories & Posts
            if (!await context.PostCategories.AnyAsync())
            {
                var pcSkincare = new PostCategory { Name = "Cẩm Nang Skincare", Slug = "cam-nang-skincare", Description = "Bí quyết chăm sóc da khoa học từ chuyên gia", DisplayOrder = 1, IsActive = true };
                var pcMakeup = new PostCategory { Name = "Xu Hướng Trang Điểm", Slug = "xu-huong-trang-diem", Description = "Cập nhật các phong cách trang điểm mới nhất", DisplayOrder = 2, IsActive = true };

                await context.PostCategories.AddRangeAsync(pcSkincare, pcMakeup);
                await context.SaveChangesAsync();

                var editor = await context.Users.FirstOrDefaultAsync(u => u.Username == "editor01");

                await context.Posts.AddRangeAsync(
                    new Post
                    {
                        PostCategoryId = pcSkincare.Id,
                        AuthorId = editor?.Id,
                        Title = "Top 5 Hoạt Chất Vàng Cho Làn Da Dầu Mụn Vào Mùa Hè",
                        Slug = "top-5-hoat-chat-vang-cho-lan-da-dau-mun",
                        Summary = "Khám phá tác dụng thần kỳ của Niacinamide, BHA và Centella Asiatica giúp kiểm soát dầu nhờn và giảm viêm mụn hiệu quả.",
                        Content = "<p>Làn da dầu vào mùa hè thường xuyên đối mặt với tình trạng bóng nhờn và bít tắc lỗ chân lông. Việc sử dụng hoạt chất BHA và Niacinamide đúng cách sẽ giúp bạn tự tin hơn bao giờ hết...</p>",
                        ThumbnailUrl = "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?w=600&h=400&fit=crop",
                        IsPublished = true,
                        PublishedAt = now.AddDays(-10),
                        CreatedAt = now.AddDays(-10)
                    },
                    new Post
                    {
                        PostCategoryId = pcSkincare.Id,
                        AuthorId = editor?.Id,
                        Title = "Phục Hồi Hàng Rào Ẩm Với Serum Vitamin B5 Sau Liệu Trình Da",
                        Slug = "phuc-hoi-hang-rao-am-voi-serum-b5",
                        Summary = "Cách kết hợp Panthenol B5 và Hyaluronic Acid để hồi sinh làn da đang bị kích ứng hoặc bong tróc sau peel.",
                        Content = "<p>Panthenol (Vitamin B5) được mệnh danh là cứu tinh cho làn da tổn thương. Bài viết này hướng dẫn chi tiết chu trình dưỡng phục hồi cấp tốc...</p>",
                        ThumbnailUrl = "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=600&h=400&fit=crop",
                        IsPublished = true,
                        PublishedAt = now.AddDays(-5),
                        CreatedAt = now.AddDays(-5)
                    },
                    new Post
                    {
                        PostCategoryId = pcMakeup.Id,
                        AuthorId = editor?.Id,
                        Title = "Bí Quyết Chọn Màu Son MLBB Tự Nhiên Đi Học Đi Làm Hằng Ngày",
                        Slug = "bi-quyet-chon-mau-son-mlbb-tu-nhien",
                        Summary = "Khám phá các tông son My Lips But Better như Rom&nd #02 và #05 giúp tôn da rạng rỡ tự nhiên mà không cần trang điểm đậm.",
                        Content = "<p>Phong cách MLBB mang đến vẻ đẹp trong trẻo, tự nhiên như làn môi nguyên bản nhưng tươi tắn hơn gấp nhiều lần...</p>",
                        ThumbnailUrl = "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=600&h=400&fit=crop",
                        IsPublished = true,
                        PublishedAt = now.AddDays(-2),
                        CreatedAt = now.AddDays(-2)
                    }
                );
                await context.SaveChangesAsync();
            }

            // 4. StaticPages
            if (!await context.StaticPages.AnyAsync())
            {
                await context.StaticPages.AddRangeAsync(
                    new StaticPage
                    {
                        Title = "Giới Thiệu Hệ Thống Omnichannel Beauty",
                        Slug = "gioi-thieu",
                        Content = "<h2>Về Chúng Tôi</h2><p>Omnichannel Beauty Solution là hệ thống bán lẻ mỹ phẩm chính hãng hàng đầu, áp dụng công nghệ thông minh FEFO quản lý hạn sử dụng minh bạch và đem lại trải nghiệm mua sắm đồng nhất từ Online đến Offline.</p>",
                        MetaTitle = "Giới Thiệu - Omnichannel Beauty Solution",
                        MetaDescription = "Hệ thống phân phối mỹ phẩm chính hãng đa kênh với công nghệ quản lý date thông minh.",
                        IsActive = true
                    },
                    new StaticPage
                    {
                        Title = "Chính Sách Đổi Trả & Cam Kết Date",
                        Slug = "chinh-sach-doi-tra",
                        Content = "<h2>Cam Kết Chất Lượng</h2><p>Chúng tôi cam kết 100% sản phẩm có nguồn gốc xuất xứ rõ ràng. Với hàng cận date trong mục FEFO Clearance, mọi thông tin hạn sử dụng đều được hiển thị công khai và đổi trả trong vòng 7 ngày nếu lỗi từ nhà sản xuất.</p>",
                        MetaTitle = "Chính Sách Đổi Trả - Omnichannel Beauty Solution",
                        MetaDescription = "Chính sách đổi trả minh bạch, bảo vệ quyền lợi người tiêu dùng tối đa.",
                        IsActive = true
                    }
                );
                await context.SaveChangesAsync();
            }

            // 5. ProductReviews (Verified Purchases)
            if (!await context.ProductReviews.AnyAsync())
            {
                var romandProduct = await context.Products.FirstOrDefaultAsync(p => p.Sku == "AMO-ROM-VT02");
                var serumProduct = await context.Products.FirstOrDefaultAsync(p => p.Sku == "LOR-HYA-B5");
                var customer01 = await context.Users.FirstOrDefaultAsync(u => u.Username == "customer01");
                var customerVip = await context.Users.FirstOrDefaultAsync(u => u.Username == "customer_vip");
                var order = await context.Orders.FirstOrDefaultAsync();

                if (romandProduct != null && customer01 != null)
                {
                    await context.ProductReviews.AddAsync(new ProductReview
                    {
                        ProductId = romandProduct.Id,
                        UserId = customer01.Id,
                        OrderId = order?.Id,
                        Rating = 5,
                        Title = "Màu son cực kỳ xinh, mịn môi không hề khô rãnh!",
                        Content = "Mình mua màu #02 Joyful, chất son xốp mịn tán dễ dàng. Mùi thơm nhẹ không gắt, ăn uống có trôi nhưng để lại lớp tint hồng cam rất tự nhiên. Rất đáng tiền!",
                        ImagesJson = "[\"https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=400&h=400&fit=crop\"]",
                        IsVerifiedPurchase = true,
                        IsApproved = true,
                        SkinType = "Da dầu",
                        AgeRange = "18-25",
                        CreatedAt = now.AddDays(-3)
                    });
                }

                if (serumProduct != null && customerVip != null)
                {
                    await context.ProductReviews.AddAsync(new ProductReview
                    {
                        ProductId = serumProduct.Id,
                        UserId = customerVip.Id,
                        OrderId = order?.Id,
                        Rating = 5,
                        Title = "Cứu tinh phục hồi sau khi dùng treatment!",
                        Content = "Serum thấm nhanh, không bết dính. Sau 1 tuần sử dụng da mình bớt đỏ rát hẳn, căng mọng mịn màng. Hàng chính hãng có tem phụ yên tâm 100%.",
                        ImagesJson = "[\"https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=400&h=400&fit=crop\"]",
                        IsVerifiedPurchase = true,
                        IsApproved = true,
                        SkinType = "Da nhạy cảm",
                        AgeRange = "26-35",
                        CreatedAt = now.AddDays(-1)
                    });
                }

                await context.SaveChangesAsync();
            }

            // 5.1. Product Questions & Official Answers (Q&A)
            if (!await context.ProductQuestions.AnyAsync())
            {
                var q1 = new ProductQuestion
                {
                    ProductId = 1,
                    AuthorName = "Lê Ngọc Linh",
                    Content = "Sản phẩm này có dùng được cho phụ nữ mang thai hoặc cho con bú không ạ?",
                    IsApproved = true,
                    CreatedAt = now.AddDays(-5),
                    Answers = new List<ProductAnswer>
                    {
                        new ProductAnswer
                        {
                            AuthorName = "Chuyên viên Dược Mỹ Phẩm Omni Beauty",
                            Content = "Chào bạn Linh! Sản phẩm đạt tiêu chuẩn Clean Beauty 5 KHÔNG, lành tính và an toàn cho phụ nữ mang thai cũng như đang cho con bú theo chuẩn da liễu nhé.",
                            IsOfficial = true,
                            IsApproved = true,
                            CreatedAt = now.AddDays(-5).AddHours(2)
                        }
                    }
                };

                var q2 = new ProductQuestion
                {
                    ProductId = 6,
                    AuthorName = "Trần Thị Mai",
                    Content = "Serum này nên thoa trên nền da ẩm hay da khô để đạt hiệu quả cấp ẩm tốt nhất?",
                    IsApproved = true,
                    CreatedAt = now.AddDays(-3),
                    Answers = new List<ProductAnswer>
                    {
                        new ProductAnswer
                        {
                            AuthorName = "Chuyên viên Dược Mỹ Phẩm Omni Beauty",
                            Content = "Chào bạn Mai! Vì serum có nồng độ Hyaluronic Acid và B5 cao, bạn nên thoa khi da còn hơi ẩm sau bước Toner để phân tử hút ẩm hoạt động tối ưu và không bị hút ẩm ngược.",
                            IsOfficial = true,
                            IsApproved = true,
                            CreatedAt = now.AddDays(-3).AddHours(1)
                        }
                    }
                };

                var q3 = new ProductQuestion
                {
                    ProductId = 15,
                    AuthorName = "Nguyễn Văn Hưng",
                    Content = "Da đang bị mụn viêm và dầu nhiều có thể dùng chung với hoạt chất Retinol không?",
                    IsApproved = true,
                    CreatedAt = now.AddDays(-2),
                    Answers = new List<ProductAnswer>
                    {
                        new ProductAnswer
                        {
                            AuthorName = "Chuyên viên Dược Mỹ Phẩm Omni Beauty",
                            Content = "Chào bạn Hưng! Nếu da đang có mụn viêm cấp tính, bạn nên ưu tiên làm dịu và phục hồi hàng rào bảo vệ da trước. Không nên dùng đồng thời Retinol cùng buổi với BHA để tránh quá tải sừng. Bạn có thể áp dụng chu trình Skin Cycling cách ngày nhé!",
                            IsOfficial = true,
                            IsApproved = true,
                            CreatedAt = now.AddDays(-2).AddHours(3)
                        }
                    }
                };

                context.ProductQuestions.AddRange(q1, q2, q3);
                await context.SaveChangesAsync();
            }

            // 5.5. Product Subscriptions (Back-In-Stock & Price Drop - GAP-05)
            if (!await context.ProductSubscriptions.AnyAsync())
            {
                var customerUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "customer@omnichannel.com");
                int? cUserId = customerUser?.Id;

                var p1 = await context.Products.FirstOrDefaultAsync(p => p.Id == 1);
                var p6 = await context.Products.FirstOrDefaultAsync(p => p.Id == 6);

                if (p1 != null && p6 != null)
                {
                    var subs = new List<ProductSubscription>
                    {
                        new ProductSubscription
                        {
                            ProductId = 1,
                            UserId = cUserId,
                            Type = "BACK_IN_STOCK",
                            CustomerName = customerUser?.FullName ?? "Khách Hàng Thân Thiết",
                            Email = "customer@omnichannel.com",
                            PhoneNumber = "0987654321",
                            Status = "ACTIVE",
                            CreatedAt = now.AddDays(-3),
                            Notes = "Đăng ký nhận tin khi có hàng lại (Demo Seed)"
                        },
                        new ProductSubscription
                        {
                            ProductId = 6,
                            UserId = cUserId,
                            Type = "PRICE_DROP",
                            TargetPrice = 450000m,
                            CustomerName = customerUser?.FullName ?? "Khách Hàng Thân Thiết",
                            Email = "customer@omnichannel.com",
                            PhoneNumber = "0987654321",
                            Status = "ACTIVE",
                            CreatedAt = now.AddDays(-2),
                            Notes = "Theo dõi giảm giá về mức 450.000 đ (Demo Seed)"
                        }
                    };

                    context.ProductSubscriptions.AddRange(subs);
                    await context.SaveChangesAsync();
                }
            }

            // 5.6. Order Status Histories & Return Requests (GAP-07 - Phiên C6)
            if (!await context.OrderStatusHistories.AnyAsync())
            {
                var recentOrders = await context.Orders
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(40)
                    .ToListAsync();

                var historyList = new List<OrderStatusHistory>();
                foreach (var ord in recentOrders)
                {
                    string trackCode = $"GHN{ord.CreatedAt:yyMMdd}{(ord.OrderCode.Length >= 6 ? ord.OrderCode[^6..].Replace("-", "").ToUpperInvariant() : ord.Id.ToString("D6"))}";
                    string carrier = ord.ShippingFee == 45000m ? "Giao Hàng Hỏa Tốc 2H (Omni VIP Express)" : "Giao Hàng Nhanh (GHN Express)";

                    // Mốc 1: Đặt hàng
                    historyList.Add(new OrderStatusHistory
                    {
                        OrderId = ord.Id,
                        Status = "PENDING",
                        Title = "Đặt hàng",
                        Description = $"Hệ thống tiếp nhận đơn hàng #{ord.OrderCode} từ kênh {ord.OrderSource}.",
                        ChangedByUserId = ord.UserId,
                        ChangedByName = ord.CustomerName,
                        CreatedAt = ord.CreatedAt
                    });

                    if (ord.OrderStatus == "CANCELLED")
                    {
                        historyList.Add(new OrderStatusHistory
                        {
                            OrderId = ord.Id,
                            Status = "CANCELLED",
                            Title = "Đã hủy đơn hàng & Hoàn tồn FEFO",
                            Description = !string.IsNullOrWhiteSpace(ord.Note) ? ord.Note : "Khách hàng yêu cầu hủy đơn • Đã hoàn trả tồn kho đúng lô FEFO.",
                            ChangedByName = ord.CustomerName,
                            CreatedAt = ord.CreatedAt.AddMinutes(20)
                        });
                        continue;
                    }

                    if (ord.OrderStatus == "CONFIRMED" || ord.OrderStatus == "PROCESSING" || ord.OrderStatus == "SHIPPING" || ord.OrderStatus == "COMPLETED")
                    {
                        historyList.Add(new OrderStatusHistory
                        {
                            OrderId = ord.Id,
                            Status = "CONFIRMED",
                            Title = "Xác nhận",
                            Description = "Bộ phận CSKH đã xác nhận đơn hàng & giữ chỗ lô FEFO.",
                            ChangedByName = "CSKH Omni Beauty",
                            CreatedAt = ord.CreatedAt.AddMinutes(15)
                        });
                    }

                    if (ord.OrderStatus == "PROCESSING" || ord.OrderStatus == "SHIPPING" || ord.OrderStatus == "COMPLETED")
                    {
                        historyList.Add(new OrderStatusHistory
                        {
                            OrderId = ord.Id,
                            Status = "PROCESSING",
                            Title = "Đóng gói (Lô FEFO đã cấp phát)",
                            Description = "Kho GSP đã cấp phát lô mỹ phẩm theo chuẩn FEFO và đóng gói chống sốc.",
                            ChangedByName = "Kho Điều Phối FEFO",
                            CreatedAt = ord.CreatedAt.AddMinutes(50)
                        });
                    }

                    if (ord.OrderStatus == "SHIPPING" || ord.OrderStatus == "COMPLETED")
                    {
                        historyList.Add(new OrderStatusHistory
                        {
                            OrderId = ord.Id,
                            Status = "HANDED_OVER",
                            Title = "Bàn giao vận chuyển",
                            Description = $"Đã bàn giao kiện hàng cho {carrier}. Mã vận đơn: {trackCode}.",
                            TrackingCode = trackCode,
                            CarrierName = carrier,
                            ChangedByName = "Điều Phối Logistics",
                            CreatedAt = ord.CreatedAt.AddHours(2)
                        });

                        historyList.Add(new OrderStatusHistory
                        {
                            OrderId = ord.Id,
                            Status = "SHIPPING",
                            Title = "Đang giao",
                            Description = $"Bưu tá {carrier} đang giao kiện hàng #{trackCode} đến người nhận.",
                            TrackingCode = trackCode,
                            CarrierName = carrier,
                            ChangedByName = carrier,
                            CreatedAt = ord.CreatedAt.AddHours(3)
                        });
                    }

                    if (ord.OrderStatus == "COMPLETED")
                    {
                        var compTime = ord.UpdatedAt ?? ord.CreatedAt.AddHours(8);
                        if (compTime <= ord.CreatedAt.AddHours(3)) compTime = ord.CreatedAt.AddHours(5);
                        historyList.Add(new OrderStatusHistory
                        {
                            OrderId = ord.Id,
                            Status = "COMPLETED",
                            Title = "Giao thành công",
                            Description = "Kiện hàng đã giao tận tay khách hàng thành công & cộng điểm thưởng Beauty Club.",
                            TrackingCode = trackCode,
                            CarrierName = carrier,
                            ChangedByName = carrier,
                            CreatedAt = compTime
                        });
                    }
                }

                if (historyList.Any())
                {
                    await context.OrderStatusHistories.AddRangeAsync(historyList);
                    await context.SaveChangesAsync();
                }

                // Seed 1 yêu cầu đổi trả mẫu đang chờ duyệt (PENDING) trên đơn COMPLETED gần đây
                if (!await context.OrderReturnRequests.AnyAsync())
                {
                    var completedOrder = recentOrders.FirstOrDefault(o => o.OrderStatus == "COMPLETED");
                    if (completedOrder != null)
                    {
                        var sampleReturn = new OrderReturnRequest
                        {
                            OrderId = completedOrder.Id,
                            UserId = completedOrder.UserId,
                            RequestCode = $"RET-{now:yyyyMMdd}-DEMO",
                            Reason = "Vỏ hộp bị móp nhẹ trong quá trình vận chuyển",
                            Description = "Nhờ shop hỗ trợ kiểm tra đổi hộp mới giúp mình để làm quà tặng sinh nhật ạ.",
                            EvidenceUrlsJson = "[\"/images/products/serum-b5.jpg\"]",
                            Status = "PENDING",
                            CreatedAt = now.AddHours(-3)
                        };
                        await context.OrderReturnRequests.AddAsync(sampleReturn);
                        await context.SaveChangesAsync();
                    }
                }
            }

            // 6. UI Sections (Storefront Dynamic Sections Builder)
            var standardSections = new Dictionary<string, (string Title, string Subtitle, int Order, string Layout)>
            {
                ["HERO_SLIDER"] = ("Banner Slider", "Bộ sưu tập mỹ phẩm dẫn đầu xu hướng làm đẹp 2026", 1, "CAROUSEL"),
                ["FLASH_SALE"] = ("Deal Chớp Nhoáng (Flash Sale)", "Cơ hội săn mỹ phẩm cao cấp chuẩn hạn dùng FEFO với giá ưu đãi cực lớn", 2, "GRID_4"),
                ["FEATURED_CATEGORIES"] = ("Danh Mục Nổi Bật", "Khám phá vẻ đẹp thuần khiết theo từng phân nhóm da liễu", 3, "GRID_6"),
                ["BEST_SELLERS"] = ("Sản Phẩm Bán Chạy", "Top mỹ phẩm được các tín đồ làm đẹp tin chọn nhiều nhất", 4, "GRID_4"),
                ["MID_BANNER"] = ("Banner Giữa Trang", "Chiến dịch ưu đãi nổi bật giữa trang", 5, "BANNER_FULL"),
                ["BEAUTY_BLOG"] = ("Bài Viết Làm Đẹp", "Cẩm nang làm đẹp khoa học và xu hướng mỹ phẩm mới nhất", 6, "GRID_3")
            };

            var existingSections = await context.UiSections.ToListAsync();
            if (!existingSections.Any())
            {
                foreach (var kvp in standardSections)
                {
                    context.UiSections.Add(new UiSection
                    {
                        SectionCode = kvp.Key,
                        Title = kvp.Value.Title,
                        Subtitle = kvp.Value.Subtitle,
                        DisplayOrder = kvp.Value.Order,
                        IsVisible = true,
                        LayoutType = kvp.Value.Layout
                    });
                }
                await context.SaveChangesAsync();
            }
            else
            {
                bool hasUpdates = false;
                foreach (var sec in existingSections)
                {
                    if (standardSections.TryGetValue(sec.SectionCode, out var std))
                    {
                        // Auto-repair any mojibake characters or missing accents
                        if (sec.Title.Contains("»") || sec.Title.Contains("›") || sec.Title.Contains("Ã") || sec.Title.Contains("á»") || sec.Title.Contains("áº") ||
                            (sec.Subtitle != null && (sec.Subtitle.Contains("»") || sec.Subtitle.Contains("›") || sec.Subtitle.Contains("Ã") || sec.Subtitle.Contains("á»"))))
                        {
                            sec.Title = std.Title;
                            sec.Subtitle = std.Subtitle;
                            hasUpdates = true;
                        }
                    }
                }
                if (hasUpdates)
                {
                    await context.SaveChangesAsync();
                }
            }
        }

        // =========================================================================
        // 14. SEED FINANCE: SỔ CÁI FINANCIAL LEDGER, CÔNG NỢ & ĐỐI SOÁT COD
        // =========================================================================
        private static async Task SeedFinanceDataAsync(ApplicationDbContext context)
        {
            var store = await context.Stores.FirstOrDefaultAsync();
            var supplier = await context.Suppliers.FirstOrDefaultAsync();
            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "superadmin");
            var completedOrder = await context.Orders.FirstOrDefaultAsync(o => o.OrderStatus == "COMPLETED");

            var now = DateTime.UtcNow;

            // 1. Sổ cái thu chi
            if (!await context.FinancialLedgers.AnyAsync() && store != null)
            {
                var ledgers = new List<FinancialLedger>
                {
                    new()
                    {
                        LedgerCode = $"LED-{now:yyyyMMdd}-001",
                        EntryDate = now.AddDays(-15),
                        EntryType = "EXPENSE",
                        StoreId = store.Id,
                        DebitAmount = 15000000m,
                        CreditAmount = 0m,
                        Description = "Chi trả tiền thuê mặt bằng chi nhánh HQ tháng hiện tại",
                        CreatedBy = adminUser?.Id,
                        CreatedAt = now.AddDays(-15)
                    },
                    new()
                    {
                        LedgerCode = $"LED-{now:yyyyMMdd}-002",
                        EntryDate = now.AddDays(-10),
                        EntryType = "EXPENSE",
                        StoreId = store.Id,
                        DebitAmount = 3200000m,
                        CreditAmount = 0m,
                        Description = "Thanh toán hóa đơn điện nước & internet chi nhánh",
                        CreatedBy = adminUser?.Id,
                        CreatedAt = now.AddDays(-10)
                    },
                    new()
                    {
                        LedgerCode = $"LED-{now:yyyyMMdd}-003",
                        EntryDate = now.AddDays(-5),
                        EntryType = "EXPENSE",
                        StoreId = store.Id,
                        DebitAmount = 2500000m,
                        CreditAmount = 0m,
                        Description = "Chi phí quảng cáo tiếp thị TikTok & Facebook Ads",
                        CreatedBy = adminUser?.Id,
                        CreatedAt = now.AddDays(-5)
                    }
                };

                // Doanh thu từ đơn bán hàng hoàn thành
                if (completedOrder != null)
                {
                    ledgers.Add(new FinancialLedger
                    {
                        LedgerCode = $"LED-{now:yyyyMMdd}-REV01",
                        EntryDate = completedOrder.CreatedAt,
                        EntryType = "REVENUE",
                        StoreId = completedOrder.StoreId,
                        OrderId = completedOrder.Id,
                        DebitAmount = 0m,
                        CreditAmount = completedOrder.TotalAmount,
                        Description = $"Ghi nhận doanh thu từ đơn hàng #{completedOrder.OrderCode}",
                        CreatedBy = adminUser?.Id,
                        CreatedAt = completedOrder.CreatedAt
                    });
                }

                await context.FinancialLedgers.AddRangeAsync(ledgers);
                await context.SaveChangesAsync();
            }

            // 2. Công nợ nhà cung cấp
            if (supplier != null && !await context.SupplierDebts.AnyAsync())
            {
                await context.SupplierDebts.AddRangeAsync(
                    new SupplierDebt
                    {
                        SupplierId = supplier.Id,
                        PurchaseOrderCode = "PO-2026-AP-01",
                        DebtAmount = 45000000m,
                        PaidAmount = 20000000m,
                        RemainingAmount = 25000000m,
                        DueDate = now.AddDays(15),
                        Status = "PARTIAL",
                        CreatedAt = now.AddDays(-20)
                    },
                    new SupplierDebt
                    {
                        SupplierId = supplier.Id,
                        PurchaseOrderCode = "PO-2026-AP-02",
                        DebtAmount = 32000000m,
                        PaidAmount = 0m,
                        RemainingAmount = 32000000m,
                        DueDate = now.AddDays(30),
                        Status = "PENDING",
                        CreatedAt = now.AddDays(-5)
                    }
                );
                await context.SaveChangesAsync();
            }

            // 3. Đối soát COD
            if (!await context.CodReconciliations.AnyAsync())
            {
                await context.CodReconciliations.AddAsync(new CodReconciliation
                {
                    ReconciliationCode = $"COD-{now:yyyyMMdd}-001",
                    ShippingPartner = "GHN",
                    ReconciliationDate = now.AddDays(-7),
                    TotalOrders = 42,
                    TotalCodAmount = 14500000m,
                    ReceivedAmount = 14500000m,
                    Difference = 0m,
                    Status = "MATCHED",
                    Note = "Bảng kê đối soát tuần 38 đợt 1 - Khớp 100%",
                    CreatedAt = now.AddDays(-7)
                });
                await context.SaveChangesAsync();
            }
        }

        // =========================================================================
        // 15. SEED CẤU HÌNH HỆ THỐNG ĐỘNG (SETTINGS)
        // =========================================================================
        private static async Task SeedDefaultSettingsAsync(ApplicationDbContext context)
        {
            var defaultSettings = new List<Setting>
            {
                // GENERAL
                new() { SettingKey = "SITE_NAME", SettingValue = "Omnichannel Beauty Solution", SettingGroup = "GENERAL", DataType = "STRING", Description = "Tên thương hiệu hệ thống cửa hàng" },
                new() { SettingKey = "HOTLINE", SettingValue = "1900 6868", SettingGroup = "GENERAL", DataType = "STRING", Description = "Tổng đài hỗ trợ khách hàng đa kênh" },
                new() { SettingKey = "SUPPORT_EMAIL", SettingValue = "support@omnichannelbeauty.vn", SettingGroup = "GENERAL", DataType = "STRING", Description = "Email nhận khiếu nại và phản hồi" },
                new() { SettingKey = "CURRENCY_FORMAT", SettingValue = "VND", SettingGroup = "GENERAL", DataType = "STRING", Description = "Đơn vị tiền tệ chính" },
                new() { SettingKey = "TIMEZONE", SettingValue = "SE Asia Standard Time", SettingGroup = "GENERAL", DataType = "STRING", Description = "Múi giờ vận hành hệ thống" },

                // SMTP
                new() { SettingKey = "SMTP_HOST", SettingValue = "smtp.gmail.com", SettingGroup = "SMTP", DataType = "STRING", Description = "Máy chủ gửi Email SMTP" },
                new() { SettingKey = "SMTP_PORT", SettingValue = "587", SettingGroup = "SMTP", DataType = "INT", Description = "Cổng kết nối TLS/STARTTLS" },
                new() { SettingKey = "SMTP_USER", SettingValue = "no-reply@omnichannelbeauty.vn", SettingGroup = "SMTP", DataType = "STRING", Description = "Tài khoản gửi Email" },
                new() { SettingKey = "SMTP_PASSWORD", SettingValue = "P@sswordEncrypted123", SettingGroup = "SMTP", DataType = "STRING", IsEncrypted = true, Description = "Mật khẩu máy chủ Email (được mã hóa)" },
                new() { SettingKey = "SMTP_ENABLE_SSL", SettingValue = "true", SettingGroup = "SMTP", DataType = "BOOL", Description = "Kích hoạt giao thức bảo mật SSL/TLS" },

                // PAYMENT
                new() { SettingKey = "PAYMENT_COD_ENABLED", SettingValue = "true", SettingGroup = "PAYMENT", DataType = "BOOL", Description = "Bật/Tắt hình thức thanh toán khi nhận hàng (COD)" },
                new() { SettingKey = "PAYMENT_VNPAY_TMN_CODE", SettingValue = "OMNICODE", SettingGroup = "PAYMENT", DataType = "STRING", Description = "Mã định danh Merchant cổng VNPAY" },
                new() { SettingKey = "VNPAY_SECRET_KEY", SettingValue = "SECRETKEY123456789ABCXYZ", SettingGroup = "PAYMENT", DataType = "STRING", IsEncrypted = true, Description = "Khóa bí mật tạo chữ ký số VNPAY" },

                // SHIPPING
                new() { SettingKey = "SHIPPING_GHN_TOKEN", SettingValue = "GHN_SANDBOX_TOKEN_2026", SettingGroup = "SHIPPING", DataType = "STRING", IsEncrypted = true, Description = "Token xác thực API Giao Hàng Nhanh (GHN)" },
                new() { SettingKey = "SHIPPING_GHN_SHOP_ID", SettingValue = "123456", SettingGroup = "SHIPPING", DataType = "INT", Description = "Mã cửa hàng GHN Shop ID" },
                new() { SettingKey = "DEFAULT_SHIPPING_FEE", SettingValue = "30000", SettingGroup = "SHIPPING", DataType = "INT", Description = "Phí giao hàng đồng giá mặc định (VNĐ)" },
                new() { SettingKey = "FREE_SHIPPING_MIN_ORDER", SettingValue = "500000", SettingGroup = "SHIPPING", DataType = "INT", Description = "Giá trị đơn hàng tối thiểu để miễn phí vận chuyển (VNĐ)" },

                // LOYALTY & INVENTORY RULES
                new() { SettingKey = "POINT_EARN_RATIO", SettingValue = "10000", SettingGroup = "LOYALTY", DataType = "INT", Description = "Số tiền tương ứng với 1 điểm thưởng (10.000 VNĐ = 1 điểm)" },
                new() { SettingKey = "POINT_REDEEM_VALUE", SettingValue = "1000", SettingGroup = "LOYALTY", DataType = "INT", Description = "Giá trị quy đổi 1 điểm sang tiền mặt (1 điểm = 1.000 VNĐ)" },
                new() { SettingKey = "EXPIRY_WARNING_DAYS", SettingValue = "60", SettingGroup = "INVENTORY", DataType = "INT", Description = "Số ngày cảnh báo hàng cận date theo chuẩn FEFO ngành mỹ phẩm" }
            };

            foreach (var setting in defaultSettings)
            {
                if (!await context.Settings.AnyAsync(s => s.SettingKey == setting.SettingKey))
                {
                    setting.UpdatedAt = DateTime.UtcNow;
                    await context.Settings.AddAsync(setting);
                }
            }

            await context.SaveChangesAsync();
        }

        // =========================================================================
        // 16. SEED AUDIT LOGS
        // =========================================================================
        private static async Task SeedAuditLogsAsync(ApplicationDbContext context)
        {
            if (!await context.AuditLogs.AnyAsync())
            {
                var admin = await context.Users.FirstOrDefaultAsync(u => u.Username == "superadmin");
                if (admin == null) return;

                var now = DateTime.UtcNow;
                await context.AuditLogs.AddRangeAsync(
                    new AuditLog
                    {
                        UserId = admin.Id,
                        Action = "INITIAL_SYSTEM_BOOTSTRAP",
                        EntityType = "System",
                        EntityId = "1",
                        NewValues = "{\"status\": \"SUCCESS\", \"event\": \"Production-grade database seed completed\"}",
                        IpAddress = "127.0.0.1",
                        CreatedAt = now
                    },
                    new AuditLog
                    {
                        UserId = admin.Id,
                        Action = "UPDATE_SECURITY_PERMISSIONS",
                        EntityType = "RolePermission",
                        EntityId = "ALL",
                        NewValues = "{\"status\": \"SUCCESS\", \"description\": \"Configured RBAC 51 permissions across 6 roles\"}",
                        IpAddress = "127.0.0.1",
                        CreatedAt = now
                    }
                );
                await context.SaveChangesAsync();
            }
        }

        // =========================================================================
        // 17. ENSURE CHAT TABLES & SEED CHAT DATA
        // =========================================================================
        private static async Task EnsureChatTablesExistAsync(ApplicationDbContext context)
        {
            var sql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ChatSessions' AND xtype='U')
                BEGIN
                    CREATE TABLE [ChatSessions] (
                        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [SessionKey] VARCHAR(64) NOT NULL,
                        [UserId] INT NULL,
                        [CustomerName] NVARCHAR(100) NULL,
                        [CustomerEmail] NVARCHAR(100) NULL,
                        [StartedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        [LastMessageAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        [MessageCount] INT NOT NULL DEFAULT 0,
                        [DetectedTopic] NVARCHAR(50) NULL
                    );
                    CREATE INDEX [IX_ChatSessions_SessionKey] ON [ChatSessions]([SessionKey]);
                END;

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ChatMessages' AND xtype='U')
                BEGIN
                    CREATE TABLE [ChatMessages] (
                        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [SessionKey] VARCHAR(64) NOT NULL,
                        [SessionId] INT NULL,
                        [Sender] VARCHAR(20) NOT NULL,
                        [Content] NVARCHAR(MAX) NOT NULL,
                        [Intent] NVARCHAR(50) NULL,
                        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        CONSTRAINT [FK_ChatMessages_ChatSessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [ChatSessions]([Id]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_ChatMessages_SessionKey] ON [ChatMessages]([SessionKey]);
                    CREATE INDEX [IX_ChatMessages_SessionId] ON [ChatMessages]([SessionId]);
                END;
            ";

            await context.Database.ExecuteSqlRawAsync(sql);
        }

        private static async Task SeedChatbotDataAsync(ApplicationDbContext context)
        {
            if (!await context.ChatSessions.AnyAsync())
            {
                var now = DateTime.UtcNow;

                var session1 = new ChatSession
                {
                    SessionKey = "sess-demo-skin-01",
                    CustomerName = "Nguyễn Thu Hà",
                    CustomerEmail = "thuha.nguyen@example.com",
                    StartedAt = now.AddHours(-3),
                    LastMessageAt = now.AddHours(-2).AddMinutes(-50),
                    MessageCount = 4,
                    DetectedTopic = "SKINCARE"
                };

                var session2 = new ChatSession
                {
                    SessionKey = "sess-demo-order-02",
                    CustomerName = "Lê Hoàng Long",
                    CustomerEmail = "long.lh@example.com",
                    StartedAt = now.AddHours(-5),
                    LastMessageAt = now.AddHours(-4).AddMinutes(-45),
                    MessageCount = 4,
                    DetectedTopic = "ORDER"
                };

                var session3 = new ChatSession
                {
                    SessionKey = "sess-demo-promo-03",
                    CustomerName = "Trần Minh Trang",
                    CustomerEmail = "trang.tm@example.com",
                    StartedAt = now.AddHours(-1),
                    LastMessageAt = now.AddMinutes(-50),
                    MessageCount = 2,
                    DetectedTopic = "PROMOTION"
                };

                await context.ChatSessions.AddRangeAsync(session1, session2, session3);
                await context.SaveChangesAsync();

                var messages = new List<ChatMessage>
                {
                    // Session 1
                    new()
                    {
                        SessionId = session1.Id,
                        SessionKey = session1.SessionKey,
                        Sender = "USER",
                        Content = "Da mình là da dầu mụn thì nên dùng serum nào kiềm dầu tốt vậy bot?",
                        Intent = "consultation",
                        CreatedAt = session1.StartedAt
                    },
                    new()
                    {
                        SessionId = session1.Id,
                        SessionKey = session1.SessionKey,
                        Sender = "BOT",
                        Content = "Chào bạn! Với làn da dầu mụn, bạn nên ưu tiên các sản phẩm chứa Niacinamide (5-10%), BHA Salicylic Acid hoặc Zinc PCA để điều tiết bã nhờn và làm sạch sâu lỗ chân lông. Gợi ý bạn có thể tham khảo Serum Niacinamide 10% + Zinc 1% và Gel rửa mặt tràm trà tại danh mục Skincare của chúng mình nhé!",
                        Intent = "consultation",
                        CreatedAt = session1.StartedAt.AddSeconds(2)
                    },
                    new()
                    {
                        SessionId = session1.Id,
                        SessionKey = session1.SessionKey,
                        Sender = "USER",
                        Content = "Cho mình xin link xem danh mục Skincare với ạ",
                        Intent = "skincare",
                        CreatedAt = session1.StartedAt.AddMinutes(5)
                    },
                    new()
                    {
                        SessionId = session1.Id,
                        SessionKey = session1.SessionKey,
                        Sender = "BOT",
                        Content = "Dạ bạn có thể khám phá toàn bộ bộ sưu tập Chăm Sóc Da chuyên sâu tại: [Khám phá Skincare](/Shop?category=skincare). Các sản phẩm đều cam kết 100% chính hãng và chuẩn hạn sử dụng FEFO.",
                        Intent = "skincare",
                        CreatedAt = session1.StartedAt.AddMinutes(5).AddSeconds(2)
                    },

                    // Session 2
                    new()
                    {
                        SessionId = session2.Id,
                        SessionKey = session2.SessionKey,
                        Sender = "USER",
                        Content = "Làm sao để mình kiểm tra tình trạng đơn hàng vừa đặt?",
                        Intent = "order",
                        CreatedAt = session2.StartedAt
                    },
                    new()
                    {
                        SessionId = session2.Id,
                        SessionKey = session2.SessionKey,
                        Sender = "BOT",
                        Content = "Để theo dõi tiến độ đơn hàng, bạn có thể vào mục [Lịch Sử Đơn Hàng](/Account/Orders) trong phần tài khoản của bạn. Đơn hàng sẽ cập nhật liên tục các trạng thái: Đã xác nhận -> Đang đóng gói -> Đang giao hàng -> Hoàn thành. Nếu bạn cần tra cứu gấp, hãy nhắn mã đơn hàng (VD: ORD-...) cho bot nhé!",
                        Intent = "order",
                        CreatedAt = session2.StartedAt.AddSeconds(3)
                    },
                    new()
                    {
                        SessionId = session2.Id,
                        SessionKey = session2.SessionKey,
                        Sender = "USER",
                        Content = "Bao lâu thì nhận được hàng ở nội thành Hà Nội vậy bạn?",
                        Intent = "shipping",
                        CreatedAt = session2.StartedAt.AddMinutes(3)
                    },
                    new()
                    {
                        SessionId = session2.Id,
                        SessionKey = session2.SessionKey,
                        Sender = "BOT",
                        Content = "Tại khu vực nội thành Hà Nội và TP.HCM, Omnichannel Beauty Solution có dịch vụ giao hỏa tốc 2H hoặc giao tiêu chuẩn trong ngày. Đơn hàng từ 500.000đ được hoàn toàn miễn phí vận chuyển!",
                        Intent = "shipping",
                        CreatedAt = session2.StartedAt.AddMinutes(3).AddSeconds(2)
                    },

                    // Session 3
                    new()
                    {
                        SessionId = session3.Id,
                        SessionKey = session3.SessionKey,
                        Sender = "USER",
                        Content = "Shop đang có chương trình khuyến mãi hay voucher giảm giá nào không?",
                        Intent = "promotion",
                        CreatedAt = session3.StartedAt
                    },
                    new()
                    {
                        SessionId = session3.Id,
                        SessionKey = session3.SessionKey,
                        Sender = "BOT",
                        Content = "Hiện tại Omnichannel đang có các ưu đãi cực hot: Mã BEAUTY20 (Giảm 20% cho đơn từ 600K), Mã FREESHIP (Miễn phí vận chuyển toàn quốc cho đơn từ 500K) và Tích lũy x2 điểm thưởng Loyalty khi mua sắm các sản phẩm chống nắng mùa hè. Bạn có thể xem chi tiết tại trang [Chương Trình Khuyến Mãi](/Promotion) nhé!",
                        Intent = "promotion",
                        CreatedAt = session3.StartedAt.AddSeconds(2)
                    }
                };

                await context.ChatMessages.AddRangeAsync(messages);
                await context.SaveChangesAsync();
            }
        }

        // =========================================================================
        // 18. ENSURE SKIN CONCERN TABLES & SEED SMART FILTER ATTRIBUTES
        // =========================================================================
        private static async Task EnsureSkinConcernTablesExistAsync(ApplicationDbContext context)
        {
            var sql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SkinConcerns' AND xtype='U')
                BEGIN
                    CREATE TABLE [SkinConcerns] (
                        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [Name] NVARCHAR(100) NOT NULL,
                        [Slug] VARCHAR(50) NOT NULL,
                        [Icon] VARCHAR(50) NULL,
                        [Description] NVARCHAR(255) NULL,
                        [IsActive] BIT NOT NULL DEFAULT 1,
                        [SortOrder] INT NOT NULL DEFAULT 0
                    );
                    CREATE UNIQUE INDEX [IX_SkinConcerns_Slug] ON [SkinConcerns]([Slug]);
                END;

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ProductConcerns' AND xtype='U')
                BEGIN
                    CREATE TABLE [ProductConcerns] (
                        [ProductId] INT NOT NULL,
                        [SkinConcernId] INT NOT NULL,
                        CONSTRAINT [PK_ProductConcerns] PRIMARY KEY ([ProductId], [SkinConcernId]),
                        CONSTRAINT [FK_ProductConcerns_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products]([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_ProductConcerns_SkinConcerns_SkinConcernId] FOREIGN KEY ([SkinConcernId]) REFERENCES [SkinConcerns]([Id]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_ProductConcerns_SkinConcernId] ON [ProductConcerns]([SkinConcernId]);
                END;
            ";

            await context.Database.ExecuteSqlRawAsync(sql);
        }

        private static async Task SeedSkinConcernsAndProductAttributesAsync(ApplicationDbContext context)
        {
            // 1. Seed 6 Skin Concerns nếu chưa có
            if (!await context.SkinConcerns.AnyAsync())
            {
                var concerns = new List<SkinConcern>
                {
                    new() { Name = "Dưỡng ẩm", Slug = "hydration", Icon = "fa-droplet", Description = "Cấp nước và củng cố hàng rào bảo vệ da căng mọng", SortOrder = 1 },
                    new() { Name = "Chống nắng", Slug = "sun_protection", Icon = "fa-sun", Description = "Bảo vệ phổ rộng trước tia UVA/UVB và lão hóa do ánh nắng", SortOrder = 2 },
                    new() { Name = "Trị mụn", Slug = "acne", Icon = "fa-shield-virus", Description = "Kháng viêm, thông thoáng lỗ chân lông và kiểm soát vi khuẩn mụn", SortOrder = 3 },
                    new() { Name = "Chống lão hóa", Slug = "anti_aging", Icon = "fa-clock-rotate-left", Description = "Tăng sinh collagen, giảm nếp nhăn và cải thiện độ săn chắc", SortOrder = 4 },
                    new() { Name = "Làm sáng da", Slug = "brightening", Icon = "fa-wand-magic-sparkles", Description = "Mờ thâm mụn, đốm nâu, nám sạm và dưỡng da đều màu", SortOrder = 5 },
                    new() { Name = "Se khít lỗ chân lông", Slug = "pore_minimizing", Icon = "fa-circle-dot", Description = "Kiềm dầu thừa, thanh lọc bã nhờn và se nhỏ lỗ chân lông", SortOrder = 6 }
                };

                await context.SkinConcerns.AddRangeAsync(concerns);
                await context.SaveChangesAsync();
            }

            // 2. Seed Mappings ProductSkinTypes & ProductConcerns cho các sản phẩm
            var products = await context.Products.ToListAsync();
            var skinTypes = await context.SkinTypes.ToDictionaryAsync(st => st.Slug, st => st.Id);
            var skinConcerns = await context.SkinConcerns.ToDictionaryAsync(sc => sc.Slug, sc => sc.Id);

            if (!await context.ProductSkinTypes.AnyAsync() || !await context.ProductConcerns.AnyAsync())
            {
                foreach (var product in products)
                {
                    var nameLower = product.Name.ToLowerInvariant();
                    var descLower = (product.ShortDescription ?? "").ToLowerInvariant();

                    // Xác định Skin Types phù hợp
                    var matchedSkinTypeSlugs = new HashSet<string>();
                    // Xác định Concerns phù hợp
                    var matchedConcernSlugs = new HashSet<string>();

                    if (nameLower.Contains("mụn") || nameLower.Contains("dầu") || descLower.Contains("dầu") || descLower.Contains("bha") || descLower.Contains("tràm trà"))
                    {
                        matchedSkinTypeSlugs.Add("da-dau");
                        matchedSkinTypeSlugs.Add("da-hon-hop");
                        matchedConcernSlugs.Add("acne");
                        matchedConcernSlugs.Add("pore_minimizing");
                    }

                    if (nameLower.Contains("chống nắng") || nameLower.Contains("sun") || nameLower.Contains("uv"))
                    {
                        matchedSkinTypeSlugs.Add("da-dau");
                        matchedSkinTypeSlugs.Add("da-kho");
                        matchedSkinTypeSlugs.Add("da-hon-hop");
                        matchedSkinTypeSlugs.Add("da-nhay-cam");
                        matchedSkinTypeSlugs.Add("da-thuong");
                        matchedConcernSlugs.Add("sun_protection");
                        matchedConcernSlugs.Add("hydration");
                    }

                    if (nameLower.Contains("ẩm") || nameLower.Contains("hyaluronic") || nameLower.Contains("ha") || nameLower.Contains("b5") || nameLower.Contains("ceramide") || descLower.Contains("ẩm"))
                    {
                        matchedSkinTypeSlugs.Add("da-kho");
                        matchedSkinTypeSlugs.Add("da-nhay-cam");
                        matchedSkinTypeSlugs.Add("da-thuong");
                        matchedConcernSlugs.Add("hydration");
                        matchedConcernSlugs.Add("anti_aging");
                    }

                    if (nameLower.Contains("sáng") || nameLower.Contains("trắng") || nameLower.Contains("vitamin c") || nameLower.Contains("niacinamide") || descLower.Contains("sáng"))
                    {
                        matchedSkinTypeSlugs.Add("da-thuong");
                        matchedSkinTypeSlugs.Add("da-hon-hop");
                        matchedSkinTypeSlugs.Add("da-dau");
                        matchedConcernSlugs.Add("brightening");
                        matchedConcernSlugs.Add("pore_minimizing");
                    }

                    if (nameLower.Contains("retinol") || nameLower.Contains("lão hóa") || nameLower.Contains("collagen"))
                    {
                        matchedSkinTypeSlugs.Add("da-kho");
                        matchedSkinTypeSlugs.Add("da-thuong");
                        matchedSkinTypeSlugs.Add("da-hon-hop");
                        matchedConcernSlugs.Add("anti_aging");
                        matchedConcernSlugs.Add("brightening");
                    }

                    // Fallback mặc định nếu chưa khớp
                    if (!matchedSkinTypeSlugs.Any())
                    {
                        matchedSkinTypeSlugs.Add("da-thuong");
                        matchedSkinTypeSlugs.Add("da-hon-hop");
                    }
                    if (!matchedConcernSlugs.Any())
                    {
                        matchedConcernSlugs.Add("hydration");
                    }

                    // Lưu ProductSkinTypes
                    foreach (var slug in matchedSkinTypeSlugs)
                    {
                        if (skinTypes.TryGetValue(slug, out var skinTypeId))
                        {
                            if (!await context.ProductSkinTypes.AnyAsync(pst => pst.ProductId == product.Id && pst.SkinTypeId == skinTypeId))
                            {
                                await context.ProductSkinTypes.AddAsync(new ProductSkinType
                                {
                                    ProductId = product.Id,
                                    SkinTypeId = skinTypeId
                                });
                            }
                        }
                    }

                    // Lưu ProductConcerns
                    foreach (var slug in matchedConcernSlugs)
                    {
                        if (skinConcerns.TryGetValue(slug, out var concernId))
                        {
                            if (!await context.ProductConcerns.AnyAsync(pc => pc.ProductId == product.Id && pc.SkinConcernId == concernId))
                            {
                                await context.ProductConcerns.AddAsync(new ProductConcern
                                {
                                    ProductId = product.Id,
                                    SkinConcernId = concernId
                                });
                            }
                        }
                    }
                }

                await context.SaveChangesAsync();
            }
        }

        // =========================================================================
        // 19. ENSURE ALERTS TABLE & SEED INITIAL ALERTS
        // =========================================================================
        private static async Task EnsureAlertTablesExistAsync(ApplicationDbContext context)
        {
            var sql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Alerts' AND xtype='U')
                BEGIN
                    CREATE TABLE [Alerts] (
                        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [Type] VARCHAR(50) NOT NULL,
                        [Severity] VARCHAR(20) NOT NULL DEFAULT 'INFO',
                        [Title] NVARCHAR(250) NOT NULL,
                        [Message] NVARCHAR(1000) NOT NULL,
                        [IsRead] BIT NOT NULL DEFAULT 0,
                        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        [ReadAt] DATETIME2 NULL,
                        [UserId] INT NULL,
                        [StoreId] INT NULL,
                        [ActionUrl] VARCHAR(500) NULL,
                        [EntityKey] VARCHAR(150) NULL,
                        CONSTRAINT [FK_Alerts_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE SET NULL,
                        CONSTRAINT [FK_Alerts_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores]([Id]) ON DELETE SET NULL
                    );
                    CREATE INDEX [IX_Alerts_Type_EntityKey] ON [Alerts]([Type], [EntityKey]);
                    CREATE INDEX [IX_Alerts_IsRead_CreatedAt] ON [Alerts]([IsRead], [CreatedAt]);
                END;
            ";

            await context.Database.ExecuteSqlRawAsync(sql);
        }

        private static async Task SeedInitialAlertsAsync(ApplicationDbContext context)
        {
            if (await context.Alerts.AnyAsync()) return;

            var store = await context.Stores.FirstOrDefaultAsync();
            int? storeId = store?.Id;
            string storeName = store?.Name ?? "Chi nhánh Hồ Chí Minh";

            var initialAlerts = new List<Alert>
            {
                new()
                {
                    Type = "OUT_OF_STOCK",
                    Severity = "DANGER",
                    Title = "🔴 Hết hàng tồn kho",
                    Message = $"Serum Trị Mụn B5 La Roche-Posay tại {storeName} đã HẾT KHO",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-25),
                    StoreId = storeId,
                    ActionUrl = "/Admin/Warehouse",
                    EntityKey = "SEED_OUT_OF_STOCK_1"
                },
                new()
                {
                    Type = "LOW_STOCK",
                    Severity = "WARNING",
                    Title = "🟡 Cảnh báo sắp hết hàng",
                    Message = $"Kem Chống Nắng Anessa Perfect UV tại {storeName} chỉ còn 4 — cần nhập thêm",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                    StoreId = storeId,
                    ActionUrl = "/Admin/Warehouse",
                    EntityKey = "SEED_LOW_STOCK_1"
                },
                new()
                {
                    Type = "EXPIRY_CRITICAL",
                    Severity = "DANGER",
                    Title = "🔴 Lô hàng cận date cấp bách",
                    Message = "Lô BATCH-2024-001 của Toner Dưỡng Ẩm Klairs sẽ hết hạn trong 12 ngày — ĐỀ XUẤT xả kho giảm giá 40%",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-2),
                    StoreId = storeId,
                    ActionUrl = "/Admin/Warehouse",
                    EntityKey = "SEED_EXPIRY_CRIT_1"
                },
                new()
                {
                    Type = "EXPIRY_WARNING",
                    Severity = "WARNING",
                    Title = "🟡 Cảnh báo hạn sử dụng lô hàng",
                    Message = "Lô BATCH-2024-009 cận date — xem xét chương trình FEFO Sale",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-4),
                    StoreId = storeId,
                    ActionUrl = "/Admin/Warehouse",
                    EntityKey = "SEED_EXPIRY_WARN_1"
                },
                new()
                {
                    Type = "REVENUE_ANOMALY",
                    Severity = "SUCCESS",
                    Title = "🟢 Đột biến doanh thu tích cực",
                    Message = "Doanh thu hôm nay TĂNG 150% so với trung bình 7 ngày — kiểm tra?",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-5),
                    StoreId = storeId,
                    ActionUrl = "/Admin/Dashboard",
                    EntityKey = "SEED_REV_ANOMALY_1"
                },
                new()
                {
                    Type = "PENDING_ORDERS_DELAY",
                    Severity = "INFO",
                    Title = "🔵 Đơn hàng chờ xử lý lâu",
                    Message = "3 đơn hàng chờ xác nhận quá 24h",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-6),
                    StoreId = storeId,
                    ActionUrl = "/Admin/Order",
                    EntityKey = "SEED_PENDING_DELAY_1"
                }
            };

            await context.Alerts.AddRangeAsync(initialAlerts);
            await context.SaveChangesAsync();
        }

        // =========================================================================
        // 20. ENSURE NOTIFICATIONS TABLE & SEED INITIAL NOTIFICATIONS
        // =========================================================================
        private static async Task EnsureNotificationTablesExistAsync(ApplicationDbContext context)
        {
            var sql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Notifications' AND xtype='U')
                BEGIN
                    CREATE TABLE [Notifications] (
                        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [UserId] INT NULL,
                        [Role] VARCHAR(50) NULL,
                        [StoreId] INT NULL,
                        [Type] VARCHAR(50) NOT NULL DEFAULT 'SYSTEM',
                        [Severity] VARCHAR(20) NOT NULL DEFAULT 'INFO',
                        [Title] NVARCHAR(250) NOT NULL,
                        [Message] NVARCHAR(1000) NOT NULL,
                        [Url] VARCHAR(500) NULL,
                        [IsRead] BIT NOT NULL DEFAULT 0,
                        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        [ReadAt] DATETIME2 NULL,
                        CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_Notifications_Stores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [Stores]([Id]) ON DELETE SET NULL
                    );
                    CREATE INDEX [IX_Notifications_UserId_IsRead] ON [Notifications]([UserId], [IsRead], [CreatedAt]);
                    CREATE INDEX [IX_Notifications_Role_IsRead] ON [Notifications]([Role], [IsRead], [CreatedAt]);
                    CREATE INDEX [IX_Notifications_StoreId_IsRead] ON [Notifications]([StoreId], [IsRead], [CreatedAt]);
                    CREATE INDEX [IX_Notifications_Type] ON [Notifications]([Type]);
                END;
            ";

            await context.Database.ExecuteSqlRawAsync(sql);
        }

        private static async Task SeedInitialNotificationsAsync(ApplicationDbContext context)
        {
            if (await context.Notifications.AnyAsync()) return;

            var store = await context.Stores.FirstOrDefaultAsync();
            int? storeId = store?.Id;

            var initialNotifications = new List<Notification>
            {
                new()
                {
                    Type = "ORDER",
                    Severity = "INFO",
                    Title = "📦 Đơn Hàng Mới: #ORD-20261002-001",
                    Message = "Đơn hàng mới từ khách Nguyễn Thu Hà (850,000đ) - Chờ xác nhận xử lý",
                    Role = "StoreManager",
                    StoreId = storeId,
                    Url = "/Admin/Order",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10)
                },
                new()
                {
                    Type = "WAREHOUSE",
                    Severity = "WARNING",
                    Title = "🔄 Phiếu Chuyển Kho Cần Duyệt",
                    Message = "Yêu cầu điều chuyển hàng TRF-20261002-A1B2C3 từ Kho Tổng đến Chi nhánh cần phê duyệt.",
                    Role = "SuperAdmin",
                    Url = "/Admin/StockTransfer",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-30)
                },
                new()
                {
                    Type = "ORDER",
                    Severity = "SUCCESS",
                    Title = "✅ Đơn Hàng Hoàn Tất: #ORD-20260930-008",
                    Message = "Đơn hàng đã được giao thành công và hoàn tất đối soát thanh toán COD.",
                    Role = "Cashier",
                    StoreId = storeId,
                    Url = "/Admin/Order",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-2)
                },
                new()
                {
                    Type = "SYSTEM",
                    Severity = "INFO",
                    Title = "🛡️ Sao Lưu Hệ Thống Tự Động",
                    Message = "Hệ thống đã tự động hoàn tất tiến trình sao lưu cơ sở dữ liệu định kỳ an toàn.",
                    Role = "SuperAdmin",
                    Url = "/Admin/Settings",
                    IsRead = true,
                    ReadAt = DateTime.UtcNow.AddHours(-1),
                    CreatedAt = DateTime.UtcNow.AddHours(-4)
                }
            };

            await context.Notifications.AddRangeAsync(initialNotifications);
            await context.SaveChangesAsync();
        }

        // =========================================================================
        // 21. ENSURE INTERNAL MESSAGES TABLE & SEED INITIAL MESSAGES
        // =========================================================================
        private static async Task EnsureInternalMessageTablesExistAsync(ApplicationDbContext context)
        {
            var sql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='InternalMessages' AND xtype='U')
                BEGIN
                    CREATE TABLE [InternalMessages] (
                        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [SenderId] INT NOT NULL,
                        [ReceiverId] INT NOT NULL,
                        [Subject] NVARCHAR(250) NOT NULL,
                        [Body] NVARCHAR(MAX) NOT NULL,
                        [IsRead] BIT NOT NULL DEFAULT 0,
                        [SentAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        [ReadAt] DATETIME2 NULL,
                        [IsDeletedBySender] BIT NOT NULL DEFAULT 0,
                        [IsDeletedByReceiver] BIT NOT NULL DEFAULT 0,
                        CONSTRAINT [FK_InternalMessages_Users_SenderId] FOREIGN KEY ([SenderId]) REFERENCES [Users]([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_InternalMessages_Users_ReceiverId] FOREIGN KEY ([ReceiverId]) REFERENCES [Users]([Id]) ON DELETE NO ACTION
                    );
                    CREATE INDEX [IX_InternalMessages_Receiver] ON [InternalMessages]([ReceiverId], [IsRead], [IsDeletedByReceiver]);
                    CREATE INDEX [IX_InternalMessages_Sender] ON [InternalMessages]([SenderId], [IsDeletedBySender]);
                END;
            ";

            await context.Database.ExecuteSqlRawAsync(sql);
        }

        private static async Task SeedInitialInternalMessagesAsync(ApplicationDbContext context)
        {
            if (await context.InternalMessages.AnyAsync()) return;

            var users = await context.Users.Take(5).ToListAsync();
            if (users.Count < 2) return;

            var admin = users.FirstOrDefault(u => u.Username.Contains("admin", StringComparison.OrdinalIgnoreCase)) ?? users[0];
            var staff = users.FirstOrDefault(u => u.Id != admin.Id) ?? users[1];

            var initialMessages = new List<InternalMessage>
            {
                new()
                {
                    SenderId = admin.Id,
                    ReceiverId = staff.Id,
                    Subject = "Kế hoạch ra mắt Bộ sưu tập Mùa Thu & Chiến dịch FEFO Sale",
                    Body = "Chào bạn,\n\nBan Giám Đốc vừa phê duyệt kế hoạch triển khai chiến dịch FEFO Sale cho các lô hàng cận hạn trong tháng tới. Chi nhánh của bạn vui lòng kiểm tra lại số lượng tồn kho thực tế và chuẩn bị quầy kệ trưng bày nổi bật nhé.\n\nTrân trọng,\nAdmin HQ.",
                    IsRead = false,
                    SentAt = DateTime.UtcNow.AddHours(-1)
                },
                new()
                {
                    SenderId = staff.Id,
                    ReceiverId = admin.Id,
                    Subject = "Báo cáo kiểm kê ca sáng và đề xuất nhập thêm Toner B5",
                    Body = "Kính gửi Ban Quản Trị,\n\nCa sáng hôm nay tại chi nhánh có lượng khách tăng đột biến 150%, mặt hàng Toner B5 và Kem Chống Nắng Anessa hiện chỉ còn 2 chai trên kệ. Kính đề nghị Quản trị viên duyệt phiếu chuyển kho bổ sung từ Kho Tổng về để đáp ứng ca chiều tối.\n\nEm cảm ơn!",
                    IsRead = true,
                    SentAt = DateTime.UtcNow.AddHours(-3),
                    ReadAt = DateTime.UtcNow.AddHours(-2)
                },
                new()
                {
                    SenderId = admin.Id,
                    ReceiverId = staff.Id,
                    Subject = "Thông báo: Nâng cấp hệ thống AI Beauty Advisor & SignalR",
                    Body = "Toàn thể nhân sự lưu ý:\n\nHệ thống vừa hoàn tất cập nhật phân hệ AI Tư Vấn Sắc Đẹp và Hộp Thư Nội Bộ Realtime. Nhân viên thu ngân và quản lý vui lòng trải nghiệm thử và phản hồi nếu có góp ý.\n\nChúc toàn đội ngũ một ngày làm việc hiệu quả và đạt doanh số cao!",
                    IsRead = false,
                    SentAt = DateTime.UtcNow.AddMinutes(-25)
                }
            };

            await context.InternalMessages.AddRangeAsync(initialMessages);
            await context.SaveChangesAsync();
        }

        // =========================================================================
        // 22. ENSURE CATEGORY ICON COLUMN & BRANDS TABLE EXIST
        // =========================================================================
        private static async Task EnsureCategoryIconColumnExistAsync(ApplicationDbContext context)
        {
            var sql = @"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Categories' AND COLUMN_NAME='Icon')
                BEGIN
                    ALTER TABLE [Categories] ADD [Icon] VARCHAR(100) NULL;
                END;
            ";
            await context.Database.ExecuteSqlRawAsync(sql);
        }

        private static async Task EnsureBrandTablesExistAsync(ApplicationDbContext context)
        {
            var sql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Brands' AND xtype='U')
                BEGIN
                    CREATE TABLE [Brands] (
                        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [Name] NVARCHAR(100) NOT NULL,
                        [Slug] VARCHAR(150) NOT NULL,
                        [CountryOfOrigin] NVARCHAR(100) NOT NULL,
                        [LogoUrl] VARCHAR(500) NULL,
                        [Description] NVARCHAR(1000) NULL,
                        [IsFeatured] BIT NOT NULL DEFAULT 1,
                        [SortOrder] INT NOT NULL DEFAULT 0,
                        [IsActive] BIT NOT NULL DEFAULT 1,
                        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                    );
                    CREATE INDEX [IX_Brands_Slug] ON [Brands]([Slug]);
                    CREATE INDEX [IX_Brands_Country] ON [Brands]([CountryOfOrigin]);
                END;
            ";
            await context.Database.ExecuteSqlRawAsync(sql);
        }

        private static async Task EnsureProductBrandColumnExistAsync(ApplicationDbContext context)
        {
            var sql = @"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Products' AND COLUMN_NAME='BrandId')
                BEGIN
                    ALTER TABLE [Products] ADD [BrandId] INT NULL;
                    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='Brands')
                    BEGIN
                        ALTER TABLE [Products] ADD CONSTRAINT [FK_Products_Brands_BrandId] FOREIGN KEY ([BrandId]) REFERENCES [Brands]([Id]) ON DELETE SET NULL;
                    END;
                    CREATE INDEX [IX_Products_BrandId] ON [Products]([BrandId]);
                END;
            ";
            await context.Database.ExecuteSqlRawAsync(sql);
        }

        private static async Task EnsureProductVariantColumnsExistAsync(ApplicationDbContext context)
        {
            var sql = @"
                -- 1. Cột mở rộng trong ProductVariants
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ProductVariants' AND COLUMN_NAME='SwatchColorHex')
                BEGIN
                    ALTER TABLE [ProductVariants] ADD [SwatchColorHex] VARCHAR(50) NULL;
                END;

                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ProductVariants' AND COLUMN_NAME='SwatchImageUrl')
                BEGIN
                    ALTER TABLE [ProductVariants] ADD [SwatchImageUrl] VARCHAR(500) NULL;
                END;

                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ProductVariants' AND COLUMN_NAME='PriceAdjustment')
                BEGIN
                    ALTER TABLE [ProductVariants] ADD [PriceAdjustment] DECIMAL(18,2) NOT NULL DEFAULT 0;
                END;

                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ProductVariants' AND COLUMN_NAME='WeightGrams')
                BEGIN
                    ALTER TABLE [ProductVariants] ADD [WeightGrams] DECIMAL(10,2) NULL;
                END;

                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ProductVariants' AND COLUMN_NAME='StockQuantity')
                BEGIN
                    ALTER TABLE [ProductVariants] ADD [StockQuantity] INT NOT NULL DEFAULT 0;
                END;

                -- 2. Cột ProductVariantId trong ProductBatches
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ProductBatches' AND COLUMN_NAME='ProductVariantId')
                BEGIN
                    ALTER TABLE [ProductBatches] ADD [ProductVariantId] INT NULL;
                    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='ProductVariants')
                    BEGIN
                        ALTER TABLE [ProductBatches] ADD CONSTRAINT [FK_ProductBatches_ProductVariants_ProductVariantId] FOREIGN KEY ([ProductVariantId]) REFERENCES [ProductVariants]([Id]) ON DELETE SET NULL;
                    END;
                    CREATE INDEX [IX_ProductBatches_ProductVariantId] ON [ProductBatches]([ProductVariantId]);
                END;

                -- 3. Bảng ProductAttributes
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ProductAttributes' AND xtype='U')
                BEGIN
                    CREATE TABLE [ProductAttributes] (
                        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [Name] NVARCHAR(100) NOT NULL,
                        [Code] VARCHAR(50) NOT NULL,
                        [DisplayType] VARCHAR(30) NOT NULL DEFAULT 'SWATCH',
                        [Description] NVARCHAR(255) NULL,
                        [IsActive] BIT NOT NULL DEFAULT 1,
                        [SortOrder] INT NOT NULL DEFAULT 0
                    );
                    CREATE UNIQUE INDEX [IX_ProductAttributes_Code] ON [ProductAttributes]([Code]);
                END;

                -- 4. Bảng ProductAttributeValues
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ProductAttributeValues' AND xtype='U')
                BEGIN
                    CREATE TABLE [ProductAttributeValues] (
                        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [ProductAttributeId] INT NOT NULL,
                        [Value] NVARCHAR(100) NOT NULL,
                        [ColorHex] VARCHAR(20) NULL,
                        [ImageUrl] VARCHAR(500) NULL,
                        [SortOrder] INT NOT NULL DEFAULT 0,
                        [IsActive] BIT NOT NULL DEFAULT 1,
                        CONSTRAINT [FK_ProductAttributeValues_ProductAttributes] FOREIGN KEY ([ProductAttributeId]) REFERENCES [ProductAttributes]([Id]) ON DELETE CASCADE
                    );
                END;

                -- 5. Bảng ProductVariantAttributeValues
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ProductVariantAttributeValues' AND xtype='U')
                BEGIN
                    CREATE TABLE [ProductVariantAttributeValues] (
                        [ProductVariantId] INT NOT NULL,
                        [ProductAttributeValueId] INT NOT NULL,
                        PRIMARY KEY ([ProductVariantId], [ProductAttributeValueId]),
                        CONSTRAINT [FK_PVAV_ProductVariants] FOREIGN KEY ([ProductVariantId]) REFERENCES [ProductVariants]([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_PVAV_ProductAttributeValues] FOREIGN KEY ([ProductAttributeValueId]) REFERENCES [ProductAttributeValues]([Id]) ON DELETE CASCADE
                    );
                END;
            ";
            await context.Database.ExecuteSqlRawAsync(sql);
        }

        // =========================================================================
        // ENSURE FEFO EXTENSION COLUMNS ON PRODUCTBATCHES
        // =========================================================================
        private static async Task EnsureProductBatchFefoColumnsExistAsync(ApplicationDbContext context, ILogger? logger = null)
        {
            try
            {
                var sql = @"
                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ProductBatches' AND COLUMN_NAME='AlertDaysThreshold')
                    BEGIN
                        ALTER TABLE [ProductBatches] ADD [AlertDaysThreshold] INT NOT NULL DEFAULT 90;
                    END;

                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ProductBatches' AND COLUMN_NAME='ClearanceDiscountPercent')
                    BEGIN
                        ALTER TABLE [ProductBatches] ADD [ClearanceDiscountPercent] DECIMAL(5,2) NULL;
                    END;
                ";
                await context.Database.ExecuteSqlRawAsync(sql);
            }
            catch (Exception ex)
            {
                LogWarning(logger, $"[EnsureProductBatchFefoColumnsExistAsync] Note: {ex.Message}");
            }
        }

        // =========================================================================
        // SEED FEFO WAREHOUSE BATCHES & STORE INVENTORIES (CHUẨN 3 LÔ ĐA CHI NHÁNH)
        // =========================================================================
        private static async Task SeedFefoWarehouseBatchesAsync(ApplicationDbContext context)
        {
            var stores = await context.Stores.ToListAsync();
            var storeQ1 = stores.FirstOrDefault(s => s.Code == "SH-Q1" || s.Id == 1) ?? stores[0];
            var storeBd = stores.FirstOrDefault(s => s.Code == "SH-BD" || s.Id == 2) ?? (stores.Count > 1 ? stores[1] : storeQ1);
            var storeMn = stores.FirstOrDefault(s => s.Code == "WH-MN" || s.Id == 3) ?? (stores.Count > 2 ? stores[2] : storeQ1);

            var products = await context.Products.Include(p => p.Brand).OrderBy(p => p.Id).ToListAsync();
            if (!products.Any()) return;

            var now = DateTime.UtcNow;

            // Load existing batches into memory lookup map
            var existingBatches = await context.ProductBatches.ToListAsync();
            var existingBatchMap = new Dictionary<string, ProductBatch>(StringComparer.OrdinalIgnoreCase);
            foreach (var b in existingBatches)
            {
                existingBatchMap[$"{b.ProductId}_{b.BatchNumber}"] = b;
            }

            var newBatches = new List<ProductBatch>();
            var productBatchTriples = new List<(Product Product, ProductBatch Near, ProductBatch Std, ProductBatch Fresh)>();

            // Dictionary đếm thứ tự sản phẩm theo từng Brand để sinh số hiệu BatchNumber chuyên nghiệp (VD: LRP-202409-B1, LRP02-202409-B1)
            var brandProductCounter = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var product in products)
            {
                var prefix = GetBrandPrefix(product.Brand?.Name, product.Sku);
                if (!brandProductCounter.ContainsKey(prefix))
                {
                    brandProductCounter[prefix] = 1;
                }
                else
                {
                    brandProductCounter[prefix]++;
                }
                var brandIndex = brandProductCounter[prefix];
                var suffixIndex = brandIndex == 1 ? "" : brandIndex.ToString("D2");

                // -------------------------------------------------------------
                // 1. Lô Cận Date (Clearance / Flash Sale FEFO - Hạn dùng 3-6 tháng)
                // Dành riêng cho chương trình "Smart Clearance Flash Sale", chiết khấu 40% - 70%
                // -------------------------------------------------------------
                var batchNearCode = $"{prefix}{suffixIndex}-202409-B1";
                var keyNear = $"{product.Id}_{batchNearCode}";

                decimal clearanceDiscount = (product.Id % 5) switch
                {
                    0 => 40.0m,
                    1 => 45.0m,
                    2 => 50.0m,
                    3 => 60.0m,
                    _ => 70.0m
                };

                int nearTotalInit = 35 + (product.Id % 20); // 35 - 54
                int nearTotalCurrent = 20 + (product.Id % 15); // 20 - 34

                if (!existingBatchMap.TryGetValue(keyNear, out var batchNear))
                {
                    batchNear = new ProductBatch
                    {
                        ProductId = product.Id,
                        BatchNumber = batchNearCode,
                        ManufacturingDate = new DateTime(2024, 9, 15),
                        ExpDate = now.AddDays(110 + (product.Id % 40)), // ~3.5 đến 5 tháng (rơi vào khoảng Jan - Apr 2027)
                        ImportCost = Math.Round(product.CostPrice * 0.9m, 0),
                        InitialQuantity = nearTotalInit,
                        CurrentQuantity = nearTotalCurrent,
                        Status = "NEAR_EXPIRATION",
                        AlertDaysThreshold = 90,
                        ClearanceDiscountPercent = clearanceDiscount,
                        CreatedAt = new DateTime(2024, 9, 15)
                    };
                    newBatches.Add(batchNear);
                    existingBatchMap[keyNear] = batchNear;
                }
                else
                {
                    batchNear.Status = "NEAR_EXPIRATION";
                    batchNear.AlertDaysThreshold = 90;
                    batchNear.ClearanceDiscountPercent = clearanceDiscount;
                    batchNear.ExpDate = now.AddDays(110 + (product.Id % 40));
                }

                // -------------------------------------------------------------
                // 2. Lô Tiêu Chuẩn (Standard Stock - Hạn dùng 10-18 tháng: Cuối năm 2027)
                // -------------------------------------------------------------
                var batchStdCode = $"{prefix}{suffixIndex}-202506-B2";
                var keyStd = $"{product.Id}_{batchStdCode}";

                int stdTotalInit = 120 + (product.Id % 50); // 120 - 169
                int stdTotalCurrent = 95 + (product.Id % 40); // 95 - 134

                if (!existingBatchMap.TryGetValue(keyStd, out var batchStd))
                {
                    batchStd = new ProductBatch
                    {
                        ProductId = product.Id,
                        BatchNumber = batchStdCode,
                        ManufacturingDate = new DateTime(2025, 6, 20),
                        ExpDate = new DateTime(2027, 11, 20).AddDays(product.Id % 30), // Cuối năm 2027 (~13.5 - 14.5 tháng)
                        ImportCost = product.CostPrice,
                        InitialQuantity = stdTotalInit,
                        CurrentQuantity = stdTotalCurrent,
                        Status = "ACTIVE",
                        AlertDaysThreshold = 90,
                        ClearanceDiscountPercent = null,
                        CreatedAt = new DateTime(2025, 6, 20)
                    };
                    newBatches.Add(batchStd);
                    existingBatchMap[keyStd] = batchStd;
                }
                else
                {
                    batchStd.Status = "ACTIVE";
                    batchStd.AlertDaysThreshold = 90;
                    batchStd.ClearanceDiscountPercent = null;
                    batchStd.ExpDate = new DateTime(2027, 11, 20).AddDays(product.Id % 30);
                }

                // -------------------------------------------------------------
                // 3. Lô Tươi Mới (Fresh Stock - Hạn dùng > 18 tháng: Đến 2028, dồi dào)
                // -------------------------------------------------------------
                var batchFreshCode = $"{prefix}{suffixIndex}-202604-B3";
                var keyFresh = $"{product.Id}_{batchFreshCode}";

                int freshTotalInit = 350 + (product.Id % 120); // 350 - 469
                int freshTotalCurrent = 320 + (product.Id % 100); // 320 - 419

                if (!existingBatchMap.TryGetValue(keyFresh, out var batchFresh))
                {
                    batchFresh = new ProductBatch
                    {
                        ProductId = product.Id,
                        BatchNumber = batchFreshCode,
                        ManufacturingDate = new DateTime(2026, 4, 10).AddDays(product.Id % 60), // Gần đây (Tháng 4 - 6/2026)
                        ExpDate = new DateTime(2028, 10, 15).AddDays(product.Id % 60), // Hạn dùng đến cuối 2028 (~24 - 26 tháng)
                        ImportCost = product.CostPrice,
                        InitialQuantity = freshTotalInit,
                        CurrentQuantity = freshTotalCurrent,
                        Status = "ACTIVE",
                        AlertDaysThreshold = 90,
                        ClearanceDiscountPercent = null,
                        CreatedAt = new DateTime(2026, 4, 10)
                    };
                    newBatches.Add(batchFresh);
                    existingBatchMap[keyFresh] = batchFresh;
                }
                else
                {
                    batchFresh.Status = "ACTIVE";
                    batchFresh.AlertDaysThreshold = 90;
                    batchFresh.ClearanceDiscountPercent = null;
                    batchFresh.ExpDate = new DateTime(2028, 10, 15).AddDays(product.Id % 60);
                }

                productBatchTriples.Add((product, batchNear, batchStd, batchFresh));
            }

            // Lưu toàn bộ Lô Mới một lần duy nhất để sinh Id
            if (newBatches.Any())
            {
                await context.ProductBatches.AddRangeAsync(newBatches);
                await context.SaveChangesAsync();
            }

            // Đồng bộ ngưỡng cảnh báo 90 ngày cho các lô hàng cũ khác nếu có
            foreach (var b in existingBatches)
            {
                if (b.AlertDaysThreshold != 90) b.AlertDaysThreshold = 90;
                if (b.Status == "NEAR_EXPIRATION" && b.ClearanceDiscountPercent == null)
                {
                    b.ClearanceDiscountPercent = 50.0m;
                }
            }

            // -------------------------------------------------------------
            // PHÂN BỔ TỒN KHO CHO 3 CHI NHÁNH (STORE INVENTORIES)
            // -------------------------------------------------------------
            var existingInventories = await context.StoreInventories.ToListAsync();
            var existingInvMap = new Dictionary<string, StoreInventory>();
            foreach (var inv in existingInventories)
            {
                existingInvMap[$"{inv.StoreId}_{inv.ProductBatchId}"] = inv;
            }

            var newInventories = new List<StoreInventory>();

            void SetInventory(int storeId, int batchId, int physicalQty, int reservedQty)
            {
                var invKey = $"{storeId}_{batchId}";
                if (existingInvMap.TryGetValue(invKey, out var existing))
                {
                    existing.PhysicalQuantity = physicalQty;
                    existing.ReservedQuantity = reservedQty;
                    existing.LastUpdated = now;
                }
                else
                {
                    var newInv = new StoreInventory
                    {
                        StoreId = storeId,
                        ProductBatchId = batchId,
                        PhysicalQuantity = physicalQty,
                        ReservedQuantity = reservedQty,
                        LastUpdated = now
                    };
                    newInventories.Add(newInv);
                    existingInvMap[invKey] = newInv;
                }
            }

            foreach (var (product, nearBatch, stdBatch, freshBatch) in productBatchTriples)
            {
                // Phân bổ Lô Cận Date:
                int nearTotal = nearBatch.CurrentQuantity;
                int nearQ1 = Math.Max(5, (int)(nearTotal * 0.4));
                int nearBd = Math.Max(4, (int)(nearTotal * 0.3));
                int nearMn = Math.Max(0, nearTotal - nearQ1 - nearBd);
                SetInventory(storeQ1.Id, nearBatch.Id, nearQ1, 1);
                SetInventory(storeBd.Id, nearBatch.Id, nearBd, 0);
                SetInventory(storeMn.Id, nearBatch.Id, nearMn, 0);

                // Phân bổ Lô Tiêu Chuẩn:
                int stdTotal = stdBatch.CurrentQuantity;
                int stdQ1 = Math.Max(25, (int)(stdTotal * 0.35));
                int stdBd = Math.Max(20, (int)(stdTotal * 0.25));
                int stdMn = Math.Max(0, stdTotal - stdQ1 - stdBd);
                SetInventory(storeQ1.Id, stdBatch.Id, stdQ1, 2);
                SetInventory(storeBd.Id, stdBatch.Id, stdBd, 1);
                SetInventory(storeMn.Id, stdBatch.Id, stdMn, 0);

                // Phân bổ Lô Tươi Mới (Tổng kho Miền Nam dồi dào nhất):
                int freshTotal = freshBatch.CurrentQuantity;
                int freshQ1 = Math.Max(80, (int)(freshTotal * 0.30));
                int freshBd = Math.Max(60, (int)(freshTotal * 0.20));
                int freshMn = Math.Max(0, freshTotal - freshQ1 - freshBd);
                SetInventory(storeQ1.Id, freshBatch.Id, freshQ1, 5);
                SetInventory(storeBd.Id, freshBatch.Id, freshBd, 2);
                SetInventory(storeMn.Id, freshBatch.Id, freshMn, 0);
            }

            // Đồng bộ tồn kho Tổng Kho Miền Nam cho các lô cũ khác nếu chưa có
            foreach (var b in existingBatches)
            {
                var mnKey = $"{storeMn.Id}_{b.Id}";
                if (!existingInvMap.ContainsKey(mnKey))
                {
                    int mnQty = b.Status == "NEAR_EXPIRATION" ? 5 : (b.CurrentQuantity > 40 ? 25 : 10);
                    SetInventory(storeMn.Id, b.Id, mnQty, 0);
                }
            }

            if (newInventories.Any())
            {
                await context.StoreInventories.AddRangeAsync(newInventories);
            }

            await context.SaveChangesAsync();
        }

        private static string GetBrandPrefix(string? brandName, string? sku)
        {
            if (string.IsNullOrWhiteSpace(brandName))
            {
                if (!string.IsNullOrEmpty(sku) && sku.Length >= 7)
                {
                    var parts = sku.Split('-');
                    if (parts.Length >= 3) return parts[2].ToUpperInvariant();
                }
                return "OBE";
            }

            var b = brandName.Trim().ToLowerInvariant();
            if (b.Contains("la roche") || b.Contains("roche-posay")) return "LRP";
            if (b.Contains("anessa")) return "ANE";
            if (b.Contains("mac")) return "MAC";
            if (b.Contains("rom&nd") || b.Contains("romand")) return "ROM";
            if (b.Contains("estée") || b.Contains("estee")) return "EST";
            if (b.Contains("cerave")) return "CRV";
            if (b.Contains("klairs")) return "KLR";
            if (b.Contains("dior")) return "DIO";
            if (b.Contains("chanel")) return "CHA";
            if (b.Contains("laurent") || b.Contains("ysl")) return "YSL";
            if (b.Contains("maybelline")) return "MAY";
            if (b.Contains("sulwhasoo")) return "SUL";
            if (b.Contains("paula")) return "PLC";
            if (b.Contains("innisfree")) return "INN";
            if (b.Contains("bioderma")) return "BIO";
            if (b.Contains("kiehl")) return "KHL";
            if (b.Contains("l'oréal") || b.Contains("loreal")) return "LOR";
            if (b.Contains("vichy")) return "VIC";
            if (b.Contains("dhc")) return "DHC";
            if (b.Contains("cosrx")) return "CSX";
            if (b.Contains("sk-ii") || b.Contains("sk2")) return "SK2";
            if (b.Contains("shiseido")) return "SHI";
            if (b.Contains("nars")) return "NAR";
            if (b.Contains("clinique")) return "CLN";
            if (b.Contains("torriden")) return "TOR";
            if (b.Contains("some by mi")) return "SBM";
            if (b.Contains("hada labo")) return "HDL";
            if (b.Contains("canmake")) return "CAN";
            if (b.Contains("lancôme") || b.Contains("lancome")) return "LAN";
            if (b.Contains("laneige")) return "LNE";
            if (b.Contains("ordinary")) return "ORD";
            if (b.Contains("jo malone")) return "JML";

            var words = b.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 3)
            {
                return $"{char.ToUpperInvariant(words[0][0])}{char.ToUpperInvariant(words[1][0])}{char.ToUpperInvariant(words[2][0])}";
            }
            if (b.Length >= 3)
            {
                return b[..3].ToUpperInvariant();
            }
            return "OBE";
        }

        private static async Task EnsureWishlistAndStockAlertTablesExistAsync(ApplicationDbContext context, ILogger? logger = null)
        {
            var sql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='WishlistItems' AND xtype='U')
                BEGIN
                    CREATE TABLE [WishlistItems] (
                        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [UserId] INT NOT NULL,
                        [ProductId] INT NOT NULL,
                        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        CONSTRAINT [FK_WishlistItems_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_WishlistItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products]([Id]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_WishlistItems_UserId] ON [WishlistItems]([UserId]);
                    CREATE INDEX [IX_WishlistItems_ProductId] ON [WishlistItems]([ProductId]);
                END;

                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='StockAlerts' AND xtype='U')
                BEGIN
                    CREATE TABLE [StockAlerts] (
                        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [ProductId] INT NOT NULL,
                        [ProductVariantId] INT NULL,
                        [UserId] INT NULL,
                        [CustomerName] NVARCHAR(100) NULL,
                        [Email] VARCHAR(100) NULL,
                        [PhoneNumber] VARCHAR(20) NULL,
                        [Status] VARCHAR(30) NOT NULL DEFAULT 'PENDING',
                        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        [NotifiedAt] DATETIME2 NULL,
                        [Notes] NVARCHAR(500) NULL,
                        CONSTRAINT [FK_StockAlerts_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products]([Id]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_StockAlerts_ProductId] ON [StockAlerts]([ProductId]);
                    CREATE INDEX [IX_StockAlerts_Status] ON [StockAlerts]([Status]);
                END;
            ";

            try
            {
                await context.Database.ExecuteSqlRawAsync(sql);
            }
            catch (Exception ex)
            {
                LogError(logger, ex, $"[DbInitializer] EnsureWishlistAndStockAlertTablesExistAsync error: {ex.Message}");
            }
        }

        private static async Task EnsureProductReviewColumnsAndSeedAsync(ApplicationDbContext context, ILogger? logger = null)
        {
            var sql = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ProductReviews') AND name = 'Scent')
                    ALTER TABLE ProductReviews ADD Scent NVARCHAR(50) NULL;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ProductReviews') AND name = 'Longevity')
                    ALTER TABLE ProductReviews ADD Longevity NVARCHAR(50) NULL;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ProductReviews') AND name = 'SkinFeel')
                    ALTER TABLE ProductReviews ADD SkinFeel NVARCHAR(50) NULL;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ProductReviews') AND name = 'HelpfulCount')
                    ALTER TABLE ProductReviews ADD HelpfulCount INT NOT NULL DEFAULT 0;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ProductReviews') AND name = 'AdminReply')
                    ALTER TABLE ProductReviews ADD AdminReply NVARCHAR(1000) NULL;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ProductReviews') AND name = 'AdminRepliedAt')
                    ALTER TABLE ProductReviews ADD AdminRepliedAt DATETIME2 NULL;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ProductReviews') AND name = 'AdminReplyBy')
                    ALTER TABLE ProductReviews ADD AdminReplyBy NVARCHAR(100) NULL;
            ";

            try
            {
                await context.Database.ExecuteSqlRawAsync(sql);
            }
            catch (Exception ex)
            {
                LogError(logger, ex, $"[DbInitializer] EnsureProductReviewColumns error: {ex.Message}");
            }

            // Seed dữ liệu mẫu đánh giá chất lượng cao nếu chưa có đủ
            try
            {
                var reviewCount = await context.ProductReviews.CountAsync();
                if (reviewCount < 8)
                {
                    var products = await context.Products.Take(4).ToListAsync();
                    var users = await context.Users.Take(4).ToListAsync();

                    if (products.Any())
                    {
                        var reviewsToSeed = new List<ProductReview>();
                        var now = DateTime.UtcNow;

                        for (int i = 0; i < products.Count; i++)
                        {
                            var prod = products[i];
                            var user1 = users.ElementAtOrDefault(0);
                            var user2 = users.ElementAtOrDefault(1);
                            var user3 = users.ElementAtOrDefault(2);

                            // Review 1: 5 sao, có ảnh swatch + bao bì, Verified Buyer, phản hồi CSKH
                            reviewsToSeed.Add(new ProductReview
                            {
                                ProductId = prod.Id,
                                UserId = user1?.Id,
                                Rating = 5,
                                Title = "Chất lượng tuyệt vời, date mới chuẩn FEFO 2028!",
                                Content = "Mình nhận hàng sau 2h đặt siêu nhanh. Bao bì bọc 3 lớp chống sốc nguyên seal. Thử chất kem lên da thấm tức thì không hề bết rít hay nặng mặt. Mùi thơm hoa hồng Pháp rất tự nhiên thư giãn. Quét mã QR ra đúng thông tin lô hàng minh bạch của hãng, 10/10 sẽ tiếp tục ủng hộ!",
                                ImagesJson = System.Text.Json.JsonSerializer.Serialize(new List<string>
                                {
                                    "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=600&h=600&fit=crop",
                                    "https://images.unsplash.com/photo-1571781926291-c477ebfd024b?w=600&h=600&fit=crop"
                                }),
                                IsVerifiedPurchase = true,
                                IsApproved = true,
                                IsHidden = false,
                                SkinType = "Da hỗn hợp thiên dầu",
                                AgeRange = "26-35",
                                Scent = "Hương hoa dịu nhẹ thư giãn",
                                Longevity = "Cấp ẩm mịn suốt 8 tiếng",
                                SkinFeel = "Mỏng nhẹ thoáng da, không nhờn rít",
                                HelpfulCount = 28,
                                AdminReply = "Dạ Omni Beauty xin chân thành cảm ơn chị đã dành sự tin tưởng trọn vẹn cho sản phẩm và quy trình kiểm định FEFO của hệ thống! Chúc chị luôn có làn da rạng ngời và tươi trẻ mỗi ngày ạ! ❤️",
                                AdminReplyBy = "Chuyên viên CSKH Omni Beauty",
                                AdminRepliedAt = now.AddDays(-2),
                                CreatedAt = now.AddDays(-3)
                            });

                            // Review 2: 5 sao, có ảnh swatch chất kem/son
                            reviewsToSeed.Add(new ProductReview
                            {
                                ProductId = prod.Id,
                                UserId = user2?.Id,
                                Rating = 5,
                                Title = "Đúng chuẩn hàng chính ngạch, bám màu và nâng tone tự nhiên",
                                Content = "Đã dùng qua nhiều nơi nhưng mua tại Omni Beauty cảm giác yên tâm nhất vì cam kết nguồn gốc và hạn sử dụng rõ ràng. Sản phẩm lên da rất êm, không gây châm chích dù da mình thuộc loại nhạy cảm. Recommend mọi người nên dùng thử!",
                                ImagesJson = System.Text.Json.JsonSerializer.Serialize(new List<string>
                                {
                                    "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=600&h=600&fit=crop"
                                }),
                                IsVerifiedPurchase = true,
                                IsApproved = true,
                                IsHidden = false,
                                SkinType = "Da nhạy cảm",
                                AgeRange = "18-25",
                                Scent = "Thanh mát tự nhiên",
                                Longevity = "Lâu trôi bền màu 6-8 tiếng",
                                SkinFeel = "Mát mịn, căng bóng tự nhiên",
                                HelpfulCount = 19,
                                AdminReply = "Cảm ơn bạn đã chia sẻ trải nghiệm tuyệt vời! Omni Beauty rất vui vì sản phẩm phù hợp hoàn hảo với làn da nhạy cảm của bạn.",
                                AdminReplyBy = "Chuyên viên CSKH Omni Beauty",
                                AdminRepliedAt = now.AddDays(-4),
                                CreatedAt = now.AddDays(-5)
                            });

                            // Review 3: 4 sao, Verified Buyer, không ảnh
                            reviewsToSeed.Add(new ProductReview
                            {
                                ProductId = prod.Id,
                                UserId = user3?.Id,
                                Rating = 4,
                                Title = "Sản phẩm tốt, giao hàng cẩn thận",
                                Content = "Thiết kế lọ cầm rất chắc tay, tinh tế. Dùng ban ngày kiềm dầu ổn định khoảng 5 tiếng. Sẽ theo dõi thêm hiệu quả sáng da sau 1 tháng sử dụng.",
                                ImagesJson = null,
                                IsVerifiedPurchase = true,
                                IsApproved = true,
                                IsHidden = false,
                                SkinType = "Da dầu",
                                AgeRange = "26-35",
                                Scent = "Không mùi lành tính",
                                Longevity = "Kiềm dầu tốt 4-6 tiếng",
                                SkinFeel = "Khô thoáng, kiềm dầu tốt",
                                HelpfulCount = 8,
                                AdminReply = null,
                                CreatedAt = now.AddDays(-7)
                            });
                        }

                        context.ProductReviews.AddRange(reviewsToSeed);
                        await context.SaveChangesAsync();
                        LogInfo(logger, $"[DbInitializer] Đã seed thành công {reviewsToSeed.Count} đánh giá mẫu cao cấp có hình ảnh & phản hồi CSKH.");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(logger, ex, $"[DbInitializer] Seed reviews error: {ex.Message}");
            }
        }

        private static async Task EnsureRowVersionColumnsExistAsync(ApplicationDbContext context, ILogger? logger = null)
        {
            var sql = @"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Coupons') AND name = 'RowVersion')
                    ALTER TABLE Coupons ADD RowVersion ROWVERSION NOT NULL;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('StoreInventories') AND name = 'RowVersion')
                    ALTER TABLE StoreInventories ADD RowVersion ROWVERSION NOT NULL;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ProductBatches') AND name = 'RowVersion')
                    ALTER TABLE ProductBatches ADD RowVersion ROWVERSION NOT NULL;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('LoyaltyWallets') AND name = 'RowVersion')
                    ALTER TABLE LoyaltyWallets ADD RowVersion ROWVERSION NOT NULL;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PosShifts') AND name = 'RowVersion')
                    ALTER TABLE PosShifts ADD RowVersion ROWVERSION NOT NULL;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'RowVersion')
                    ALTER TABLE Orders ADD RowVersion ROWVERSION NOT NULL;
            ";

            try
            {
                await context.Database.ExecuteSqlRawAsync(sql);
            }
            catch (Exception ex)
            {
                LogError(logger, ex, $"[DbInitializer] EnsureRowVersionColumnsExistAsync error: {ex.Message}");
            }
        }

        public static async Task EnsureIndexesExistAsync(ApplicationDbContext context, ILogger? logger = null)
        {
            try
            {
                var sql = @"
                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_BrandId' AND object_id = OBJECT_ID('Products'))
                        CREATE NONCLUSTERED INDEX IX_Products_BrandId ON Products(BrandId) WHERE BrandId IS NOT NULL;

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_Slug' AND object_id = OBJECT_ID('Products'))
                        CREATE UNIQUE NONCLUSTERED INDEX IX_Products_Slug ON Products(Slug) WHERE Slug IS NOT NULL;

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_CategoryId_IsActive' AND object_id = OBJECT_ID('Products'))
                        CREATE NONCLUSTERED INDEX IX_Products_CategoryId_IsActive ON Products(CategoryId, IsActive);

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductBatches_ExpDate' AND object_id = OBJECT_ID('ProductBatches'))
                        CREATE NONCLUSTERED INDEX IX_ProductBatches_ExpDate ON ProductBatches(ExpDate);

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StoreInventories_Store_Batch' AND object_id = OBJECT_ID('StoreInventories'))
                        CREATE UNIQUE NONCLUSTERED INDEX IX_StoreInventories_Store_Batch ON StoreInventories(StoreId, ProductBatchId);
                ";
                await context.Database.ExecuteSqlRawAsync(sql);
            }
            catch (Exception ex)
            {
                LogError(logger, ex, $"[DbInitializer] EnsureIndexesExistAsync error: {ex.Message}");
            }
        }

        public static async Task MigrateStaticImagesToDatabaseAsync(ApplicationDbContext context, IWebHostEnvironment env, ILogger? logger = null)
        {
            // Kiểm tra cờ/bản ghi: Nếu CSDL đã có MediaAssets và đã có sản phẩm gắn ảnh thì bỏ qua 100% quét đĩa để tối ưu khởi động
            int existingMediaCount = await context.MediaAssets.CountAsync();
            if (existingMediaCount > 0)
            {
                int mappedProductCount = await context.Products.CountAsync(p => p.ThumbnailMediaId != null);
                if (mappedProductCount > 0)
                {
                    LogInfo(logger, $"[DbInitializer] CSDL đã có sẵn {existingMediaCount} MediaAssets và {mappedProductCount} sản phẩm đã gắn ảnh thực tế 1-1. Bỏ qua quét đĩa & nạp lại.");
                    return;
                }
            }

            var imagesPath = !string.IsNullOrEmpty(env?.WebRootPath) ? Path.Combine(env.WebRootPath, "images") : "";
            if (string.IsNullOrEmpty(imagesPath) || !Directory.Exists(imagesPath))
            {
                imagesPath = Path.Combine(env?.ContentRootPath ?? Directory.GetCurrentDirectory(), "wwwroot", "images");
            }
            if (!Directory.Exists(imagesPath))
            {
                imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "Omnichannel.Web", "wwwroot", "images");
            }
            if (!Directory.Exists(imagesPath))
            {
                imagesPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "images");
            }
            if (!Directory.Exists(imagesPath)) return;

            LogInfo(logger, $"[DbInitializer] Bắt đầu quét nạp toàn bộ ảnh tĩnh từ: {imagesPath}");

            var imageFiles = Directory.GetFiles(imagesPath, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)).ToList();

            LogInfo(logger, $"[DbInitializer] Đã phát hiện {imageFiles.Count} tệp ảnh trên hệ thống.");

            existingMediaCount = await context.MediaAssets.CountAsync();
            int mappedProdCount = await context.Products.CountAsync(p => p.ThumbnailMediaId != null);
            if (existingMediaCount >= imageFiles.Count && mappedProdCount >= 80)
            {
                LogInfo(logger, $"[DbInitializer] CSDL đã có đầy đủ {existingMediaCount} MediaAssets và {mappedProdCount} sản phẩm đã gắn ảnh thực tế 1-1. Bỏ qua nạp lại.");
                return;
            }

            var existingMediaMetas = await context.MediaAssets
                .Select(m => new { m.Id, m.FileName, m.ChecksumHash, m.Category })
                .ToListAsync();

            var existingMetaDict = existingMediaMetas
                .GroupBy(m => m.FileName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            int newCount = 0;
            int updatedCount = 0;

            foreach (var filePath in imageFiles)
            {
                var fileName = Path.GetFileName(filePath);
                var bytes = await File.ReadAllBytesAsync(filePath);
                if (bytes.Length < 100) continue; // Bỏ qua tệp rỗng hoặc lỗi

                var ext = Path.GetExtension(filePath).ToLowerInvariant();

                // Kiểm tra Magic Bytes
                if (!ValidateImageMagicBytes(bytes, ext))
                {
                    LogWarning(logger, $"[DbInitializer] Cảnh báo: Tệp {fileName} không đạt Magic Bytes hợp lệ. Bỏ qua.");
                    continue;
                }
                
                using var sha256 = SHA256.Create();
                var hash = BitConverter.ToString(sha256.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();

                // Xác định phân loại Category chuẩn xác theo cây thư mục
                var normalizedPath = filePath.Replace("\\", "/").ToLowerInvariant();
                string category;
                if (normalizedPath.Contains("/products/"))
                {
                    category = "PRODUCT";
                }
                else if (normalizedPath.Contains("/branding/"))
                {
                    category = "THEME";
                }
                else if (normalizedPath.Contains("/banners/"))
                {
                    category = "BANNER";
                }
                else if (normalizedPath.Contains("/categories/"))
                {
                    category = "CATEGORY";
                }
                else
                {
                    category = normalizedPath.Contains("banner") ? "BANNER" : (normalizedPath.Contains("brand") ? "THEME" : "PRODUCT");
                }

                var contentType = DetectImageContentType(bytes, ext);

                if (!existingMetaDict.TryGetValue(fileName, out var existing))
                {
                    var media = new MediaAsset
                    {
                        FileName = fileName,
                        ContentType = contentType,
                        Data = bytes,
                        FileSize = bytes.Length,
                        ChecksumHash = hash,
                        Category = category,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.MediaAssets.Add(media);
                    newCount++;
                }
                else if (existing.ChecksumHash != hash || existing.Category != category)
                {
                    var entityToUpdate = await context.MediaAssets.FirstOrDefaultAsync(m => m.Id == existing.Id);
                    if (entityToUpdate != null)
                    {
                        entityToUpdate.Data = bytes;
                        entityToUpdate.FileSize = bytes.Length;
                        entityToUpdate.ChecksumHash = hash;
                        entityToUpdate.ContentType = contentType;
                        entityToUpdate.Category = category;
                        updatedCount++;
                    }
                }
            }

            if (newCount > 0 || updatedCount > 0)
            {
                LogInfo(logger, $"[DbInitializer] Đang lưu {newCount} ảnh mới và {updatedCount} ảnh cập nhật vào bảng MediaAssets...");
                await context.SaveChangesAsync();
                LogInfo(logger, $"[DbInitializer] Đã lưu thành công dữ liệu nhị phân MediaAssets vào CSDL.");
            }

            // =========================================================================
            // THUẬT TOÁN LIÊN KẾT 1-1 CHÍNH XÁC (STRICT MAPPING - DÙNG METADATA PROJECTION)
            // =========================================================================

            // 1. Ánh xạ Logo và Favicon vào ThemeConfigs
            var logoMediaId = await context.MediaAssets
                .Where(m => m.FileName.Contains("logo-full") || m.FileName.Contains("logo-circle"))
                .OrderBy(m => m.Id)
                .Select(m => (int?)m.Id)
                .FirstOrDefaultAsync();
            var faviconMediaId = await context.MediaAssets
                .Where(m => m.FileName.Contains("favicon-32x32"))
                .OrderBy(m => m.Id)
                .Select(m => (int?)m.Id)
                .FirstOrDefaultAsync();
            var theme = await context.ThemeConfigs.OrderBy(t => t.Id).FirstOrDefaultAsync();
            if (theme != null)
            {
                if (logoMediaId.HasValue) theme.LogoMediaId = logoMediaId.Value;
                if (faviconMediaId.HasValue) theme.FaviconMediaId = faviconMediaId.Value;
            }

            // 2. Ánh xạ Banner vào bảng Banners
            var allBanners = await context.Banners.ToListAsync();
            var bannerMedias = await context.MediaAssets
                .Where(m => m.Category == "BANNER")
                .Select(m => new { m.Id, m.FileName })
                .ToListAsync();
            if (bannerMedias.Any())
            {
                foreach (var b in allBanners)
                {
                    var matched = bannerMedias.FirstOrDefault(m =>
                        (!string.IsNullOrEmpty(b.Title) && (
                            (b.Title.Contains("SUMMER", StringComparison.OrdinalIgnoreCase) && m.FileName.Contains("summer", StringComparison.OrdinalIgnoreCase)) ||
                            (b.Title.Contains("K-BEAUTY", StringComparison.OrdinalIgnoreCase) && m.FileName.Contains("kbeauty", StringComparison.OrdinalIgnoreCase)) ||
                            (b.Title.Contains("FEFO", StringComparison.OrdinalIgnoreCase) && m.FileName.Contains("flash-sale", StringComparison.OrdinalIgnoreCase)) ||
                            m.FileName.Contains(b.Title, StringComparison.OrdinalIgnoreCase))) ||
                        (!string.IsNullOrEmpty(b.DesktopImageUrl) && m.FileName.Contains(Path.GetFileNameWithoutExtension(b.DesktopImageUrl), StringComparison.OrdinalIgnoreCase)));

                    if (matched != null)
                    {
                        b.DesktopMediaId = matched.Id;
                    }
                }
            }

            // 3. Ánh xạ 1-1 Danh mục sản phẩm (Category.MediaAssetId)
            var allCategories = await context.Categories.ToListAsync();
            var categoryMedias = await context.MediaAssets
                .Where(m => m.Category == "CATEGORY")
                .Select(m => new { m.Id, m.FileName })
                .ToListAsync();
            foreach (var cat in allCategories)
            {
                var matched = categoryMedias.FirstOrDefault(m =>
                    (!string.IsNullOrEmpty(cat.Slug) && m.FileName.Contains(cat.Slug, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(cat.Name) && m.FileName.Contains(cat.Name, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(cat.Slug) && Path.GetFileNameWithoutExtension(m.FileName).Contains(cat.Slug, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(cat.Slug) && cat.Slug.Contains(Path.GetFileNameWithoutExtension(m.FileName), StringComparison.OrdinalIgnoreCase)));
                if (matched != null)
                {
                    cat.MediaAssetId = matched.Id;
                }
            }

            // 4. Ánh xạ 1-1 Logo thương hiệu (Brand.LogoMediaId)
            var allBrands = await context.Brands.ToListAsync();
            var brandMedias = await context.MediaAssets
                .Where(m => m.Category == "THEME" && (m.FileName.EndsWith(".png") || m.FileName.Contains("brand-")))
                .Select(m => new { m.Id, m.FileName })
                .ToListAsync();
            foreach (var br in allBrands)
            {
                var matched = brandMedias.FirstOrDefault(m =>
                    (!string.IsNullOrEmpty(br.Slug) && Path.GetFileNameWithoutExtension(m.FileName).Equals(br.Slug, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(br.Slug) && m.FileName.Contains(br.Slug, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(br.Name) && Path.GetFileNameWithoutExtension(m.FileName).Equals(br.Name.Replace(" ", "-").Replace("'", "").ToLowerInvariant(), StringComparison.OrdinalIgnoreCase)));
                if (matched != null)
                {
                    br.LogoMediaId = matched.Id;
                }
            }

            // 5. Ánh xạ 1-1 Sản phẩm (Product.ThumbnailMediaId) dựa trên Slug hoặc SKU
            var allProducts = await context.Products.ToListAsync();
            var productMedias = await context.MediaAssets
                .Where(m => m.Category == "PRODUCT" && m.FileName != "hero-banner.png" && m.FileName != "product-placeholder.svg")
                .Select(m => new { m.Id, m.FileName })
                .ToListAsync();

            LogInfo(logger, $"[DbInitializer] Đang ánh xạ 1-1 hình ảnh cho {allProducts.Count} sản phẩm từ {productMedias.Count} tệp ảnh...");

            int mappedCount = 0;
            foreach (var prod in allProducts)
            {
                // Ưu tiên 1: Khớp chính xác tên file chứa Slug hoặc bắt đầu bằng Sku hoặc Slug chứa tên file không đuôi
                var matchedMedia = productMedias.FirstOrDefault(m =>
                    (!string.IsNullOrEmpty(prod.Slug) && m.FileName.Contains(prod.Slug, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(prod.Sku) && m.FileName.StartsWith(prod.Sku, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(prod.Slug) && prod.Slug.Contains(Path.GetFileNameWithoutExtension(m.FileName), StringComparison.OrdinalIgnoreCase)));

                // Ưu tiên 2: Khớp thông minh theo tập token từ khóa (>= 3 token trùng khớp)
                if (matchedMedia == null && !string.IsNullOrEmpty(prod.Slug))
                {
                    var slugTokens = prod.Slug.Split('-', StringSplitOptions.RemoveEmptyEntries)
                        .Where(t => t.Length > 2)
                        .ToList();

                    int bestScore = 0;
                    int? bestMediaId = null;

                    foreach (var media in productMedias)
                    {
                        var fn = Path.GetFileNameWithoutExtension(media.FileName).ToLowerInvariant();
                        int score = slugTokens.Count(t => fn.Contains(t));
                        if (score > bestScore && score >= 3)
                        {
                            bestScore = score;
                            bestMediaId = media.Id;
                        }
                    }

                    if (bestMediaId.HasValue)
                    {
                        prod.ThumbnailMediaId = bestMediaId.Value;
                        mappedCount++;
                        continue;
                    }
                }

                if (matchedMedia != null)
                {
                    prod.ThumbnailMediaId = matchedMedia.Id;
                    mappedCount++;
                }
                else if (prod.ThumbnailMediaId == 1)
                {
                    // Hủy gán hero-banner.png ngẫu nhiên trước đó nếu không tìm thấy ảnh phù hợp
                    prod.ThumbnailMediaId = null;
                }
            }

            await context.SaveChangesAsync();
            LogInfo(logger, $"[DbInitializer] Hoàn tất ánh xạ: {mappedCount}/{allProducts.Count} sản phẩm đã gắn ảnh thực tế 1-1.");
        }

        private static bool ValidateImageMagicBytes(byte[] data, string ext)
        {
            if (data == null || data.Length < 4) return false;

            // 1. SVG: <svg hoặc <?xml
            if (ext == ".svg")
            {
                var text = System.Text.Encoding.UTF8.GetString(data.Take(Math.Min(data.Length, 256)).ToArray());
                return text.Contains("<svg", StringComparison.OrdinalIgnoreCase) || text.Contains("<?xml", StringComparison.OrdinalIgnoreCase);
            }

            // 2. JPEG: FF D8 FF
            if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF) return true;

            // 3. PNG: 89 50 4E 47
            if (data.Length >= 8 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47) return true;

            // 4. WEBP: RIFF...WEBP
            if (data.Length >= 12 && data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 &&
                data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50) return true;

            // 5. SVG fallback
            var sampleText = System.Text.Encoding.UTF8.GetString(data.Take(Math.Min(data.Length, 256)).ToArray());
            if (sampleText.Contains("<svg", StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }

        private static string DetectImageContentType(byte[] data, string ext)
        {
            if (data != null && data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF) return "image/jpeg";
            if (data != null && data.Length >= 8 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47) return "image/png";
            if (data != null && data.Length >= 12 && data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 &&
                data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50) return "image/webp";
            if (ext == ".svg") return "image/svg+xml";

            return ext switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                _ => "image/jpeg"
            };
        }

        public static async Task EnsureDynamicCmsDataSeededAsync(ApplicationDbContext context, ILogger? logger = null)
        {
            try
            {
                // 1. Seed hoặc Cập nhật Theme "Rose Gold Beauty"
                var defaultTheme = await context.ThemeConfigs.FirstOrDefaultAsync();
                var logoMedia = await context.MediaAssets
                    .Where(m => m.FileName.Contains("logo-full") || m.FileName.Contains("logo-circle"))
                    .OrderBy(m => m.Id)
                    .FirstOrDefaultAsync();
                var faviconMedia = await context.MediaAssets
                    .Where(m => m.FileName.Contains("favicon-32x32"))
                    .OrderBy(m => m.Id)
                    .FirstOrDefaultAsync();

                if (defaultTheme == null)
                {
                    defaultTheme = new ThemeConfig
                    {
                        ThemeName = "Rose Gold Beauty",
                        BrandName = "OMNI BEAUTY",
                        Slogan = "Hệ Thống Bán Lẻ Mỹ Phẩm Cao Cấp Tiên Phong Công Nghệ FEFO",
                        PrimaryColor = "#E11D48",
                        SecondaryColor = "#BE185D",
                        AccentColor = "#F59E0B",
                        FontHeading = "'Playfair Display', serif",
                        FontBody = "'Inter', sans-serif",
                        FontFamily = "'Inter', sans-serif",
                        LogoMediaId = logoMedia?.Id,
                        LogoMediaAssetId = logoMedia?.Id,
                        FaviconMediaId = faviconMedia?.Id,
                        FaviconMediaAssetId = faviconMedia?.Id,
                        AnnouncementBarText = "✨ Chào mừng bạn đến với OMNI BEAUTY - Miễn phí vận chuyển cho đơn hàng từ 300K ✨",
                        AnnouncementBarActive = true,
                        AnnouncementBarBgColor = "#881337",
                        Hotline = "1900 8888",
                        ContactEmail = "hotro@omnibeauty.vn",
                        IsActive = true
                    };
                    context.ThemeConfigs.Add(defaultTheme);
                    await context.SaveChangesAsync();
                    LogInfo(logger, "[DbInitializer] Đã khởi tạo ThemeConfig 'Rose Gold Beauty'.");
                }
                else
                {
                    bool themeUpdated = false;
                    if (string.IsNullOrEmpty(defaultTheme.ThemeName)) { defaultTheme.ThemeName = "Rose Gold Beauty"; themeUpdated = true; }
                    if (string.IsNullOrEmpty(defaultTheme.FontHeading)) { defaultTheme.FontHeading = "'Playfair Display', serif"; themeUpdated = true; }
                    if (string.IsNullOrEmpty(defaultTheme.FontBody)) { defaultTheme.FontBody = "'Inter', sans-serif"; themeUpdated = true; }
                    if (!defaultTheme.LogoMediaAssetId.HasValue && defaultTheme.LogoMediaId.HasValue) { defaultTheme.LogoMediaAssetId = defaultTheme.LogoMediaId; themeUpdated = true; }
                    if (!defaultTheme.FaviconMediaAssetId.HasValue && defaultTheme.FaviconMediaId.HasValue) { defaultTheme.FaviconMediaAssetId = defaultTheme.FaviconMediaId; themeUpdated = true; }
                    if (themeUpdated)
                    {
                        await context.SaveChangesAsync();
                        LogInfo(logger, "[DbInitializer] Đã đồng bộ các Tokens mới cho ThemeConfig hiện tại.");
                    }
                }

                // 2. Seed PageLayout "home_page"
                var homeLayout = await context.PageLayouts.FirstOrDefaultAsync(p => p.PageKey == "home_page");
                if (homeLayout == null)
                {
                    homeLayout = new PageLayout
                    {
                        PageKey = "home_page",
                        Title = "Trang Chủ - OMNI BEAUTY",
                        MetaDescription = "Hệ Thống Bán Lẻ Mỹ Phẩm Cao Cấp Tiên Phong Công Nghệ FEFO",
                        IsPublished = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.PageLayouts.Add(homeLayout);
                    await context.SaveChangesAsync();
                    LogInfo(logger, "[DbInitializer] Đã khởi tạo PageLayout 'home_page'.");
                }

                // 3. Seed LayoutSections & SectionItems
                if (!await context.LayoutSections.AnyAsync(s => s.PageLayoutId == homeLayout.Id))
                {
                    // Lấy các banner media hiện có để liên kết
                    var bannerMedias = await context.MediaAssets
                        .Where(m => m.Category == "BANNER")
                        .OrderBy(m => m.Id)
                        .ToListAsync();

                    // Section 1: Hero Banner Slider (OrderIndex = 1, 3 items)
                    var heroSection = new LayoutSection
                    {
                        PageLayoutId = homeLayout.Id,
                        SectionKey = "hero_banner",
                        DisplayName = "Hero Banner Slider",
                        OrderIndex = 1,
                        IsVisible = true,
                        CustomStylesJson = "{\"height\":\"620px\",\"autoplay\":true,\"interval\":5000}"
                    };
                    context.LayoutSections.Add(heroSection);
                    await context.SaveChangesAsync();

                    var heroItem1 = new SectionItem
                    {
                        LayoutSectionId = heroSection.Id,
                        Title = "Đại Tiệc Skincare Mùa Hè 2026",
                        Subtitle = "Ưu đãi đến 50% cho toàn bộ tinh chất cấp ẩm & phục hồi da",
                        TargetUrl = "/catalog?category=serum",
                        MediaAssetId = bannerMedias.Count > 0 ? bannerMedias[0].Id : null,
                        OrderIndex = 1,
                        PayloadJson = "{\"badgeText\":\"HOT PROMO\",\"buttonText\":\"Khám Phá Ngay\"}"
                    };
                    var heroItem2 = new SectionItem
                    {
                        LayoutSectionId = heroSection.Id,
                        Title = "Son Lì Thuần Chay Dưỡng Ẩm",
                        Subtitle = "Bộ sưu tập son dưỡng hữu cơ chiết xuất hoa hồng Damask",
                        TargetUrl = "/catalog?category=son-moi",
                        MediaAssetId = bannerMedias.Count > 1 ? bannerMedias[1].Id : null,
                        OrderIndex = 2,
                        PayloadJson = "{\"badgeText\":\"NEW ARRIVAL\",\"buttonText\":\"Mua Ngay\"}"
                    };
                    var heroItem3 = new SectionItem
                    {
                        LayoutSectionId = heroSection.Id,
                        Title = "Liệu Trình Chống Lão Hóa Chuẩn Da Liễu",
                        Subtitle = "Công nghệ Retinol vi nang giải phóng chậm, an toàn cho da nhạy cảm",
                        TargetUrl = "/catalog?category=anti-aging",
                        MediaAssetId = bannerMedias.Count > 2 ? bannerMedias[2].Id : null,
                        OrderIndex = 3,
                        PayloadJson = "{\"badgeText\":\"EXCLUSIVE\",\"buttonText\":\"Xem Phác Đồ\"}"
                    };
                    context.SectionItems.AddRange(heroItem1, heroItem2, heroItem3);

                    // Section 2: Flash Sale Giờ Vàng (OrderIndex = 2, 2 items)
                    var flashSaleSection = new LayoutSection
                    {
                        PageLayoutId = homeLayout.Id,
                        SectionKey = "flash_sale",
                        DisplayName = "Flash Sale Độc Quyền Giờ Vàng",
                        OrderIndex = 2,
                        IsVisible = true,
                        CustomStylesJson = "{\"theme\":\"fire_deal\",\"showCountdown\":true}"
                    };
                    context.LayoutSections.Add(flashSaleSection);
                    await context.SaveChangesAsync();

                    var fsItem1 = new SectionItem
                    {
                        LayoutSectionId = flashSaleSection.Id,
                        Title = "Kem Chống Nắng Phổ Rộng SPF50+ PA++++",
                        Subtitle = "Bảo vệ màng tế bào, kiềm dầu 8H",
                        TargetUrl = "/catalog?category=sunscreen",
                        OrderIndex = 1,
                        PayloadJson = "{\"originalPrice\":450000,\"salePrice\":299000,\"stockLimit\":100,\"soldCount\":68}"
                    };
                    var fsItem2 = new SectionItem
                    {
                        LayoutSectionId = flashSaleSection.Id,
                        Title = "Nước Cân Bằng Cấp Ẩm Hyaluronic Acid",
                        Subtitle = "Thẩm thấu 5 tầng biểu bì, dịu da tức thì",
                        TargetUrl = "/catalog?category=toner",
                        OrderIndex = 2,
                        PayloadJson = "{\"originalPrice\":380000,\"salePrice\":245000,\"stockLimit\":80,\"soldCount\":52}"
                    };
                    context.SectionItems.AddRange(fsItem1, fsItem2);

                    // Section 3: Routine Quiz Cá Nhân Hóa (OrderIndex = 3, 2 items)
                    var quizSection = new LayoutSection
                    {
                        PageLayoutId = homeLayout.Id,
                        SectionKey = "routine_quiz",
                        DisplayName = "Trắc Nghiệm Routine Làn Da Cá Nhân Hóa",
                        OrderIndex = 3,
                        IsVisible = true,
                        CustomStylesJson = "{\"layout\":\"interactive_cards\",\"gradient\":\"rose_blush\"}"
                    };
                    context.LayoutSections.Add(quizSection);
                    await context.SaveChangesAsync();

                    var quizItem1 = new SectionItem
                    {
                        LayoutSectionId = quizSection.Id,
                        Title = "Khám Phá Phác Đồ Skincare 4 Bước Trong 30 Giây",
                        Subtitle = "AI Beauty Advisor phân tích loại da & gợi ý combo chuẩn da liễu",
                        TargetUrl = "/tu-van-da",
                        OrderIndex = 1,
                        PayloadJson = "{\"ctaText\":\"Bắt Đầu Trắc Nghiệm\",\"badge\":\"AI BeautyTech\"}"
                    };
                    var quizItem2 = new SectionItem
                    {
                        LayoutSectionId = quizSection.Id,
                        Title = "Combo Routine Tiết Kiệm 15% 1-Click",
                        Subtitle = "Tự động áp dụng chiết khấu combo khi mua trọn bộ 4 bước",
                        TargetUrl = "/tu-van-da/combo",
                        OrderIndex = 2,
                        PayloadJson = "{\"ctaText\":\"Xem Combo Gợi Ý\",\"discountPercent\":15}"
                    };
                    context.SectionItems.AddRange(quizItem1, quizItem2);

                    await context.SaveChangesAsync();
                    LogInfo(logger, "[DbInitializer] Đã seed thành công 3 LayoutSections (Hero Banner, Flash Sale, Routine Quiz) và các SectionItems tương ứng.");
                }
            }
            catch (Exception ex)
            {
                LogError(logger, ex, "[DbInitializer] Lỗi trong quá trình Seed Dynamic CMS & Enterprise Theme.");
            }
        }

        private static async Task EnsureRoutineStepAndComboSeededAsync(ApplicationDbContext context, ILogger? logger)
        {
            try
            {
                // 1. Đảm bảo cột RoutineStep tồn tại trong bảng Products (nếu dùng cơ sở dữ liệu quan hệ SQL Server)
                if (context.Database.IsRelational())
                {
                    const string sql = @"
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Products' AND COLUMN_NAME='RoutineStep')
                        BEGIN
                            ALTER TABLE [Products] ADD [RoutineStep] VARCHAR(50) NULL;
                        END;";
                    await context.Database.ExecuteSqlRawAsync(sql);
                }

                // 2. Đảm bảo Coupon COMBO15 tồn tại
                if (!await context.Coupons.AnyAsync(c => c.CouponCode == "COMBO15"))
                {
                    var comboCoupon = new Coupon
                    {
                        CouponCode = "COMBO15",
                        Description = "Ưu đãi Combo Routine 4 bước chăm sóc da - Giảm 15% trọn bộ",
                        DiscountType = "PERCENTAGE",
                        DiscountValue = 15m,
                        MinOrderValue = 0m,
                        MaxDiscountAmount = 1000000m,
                        ValidFrom = DateTime.UtcNow.AddDays(-30),
                        ValidTo = DateTime.UtcNow.AddYears(2),
                        UsageLimit = 10000,
                        UsageCount = 0,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    await context.Coupons.AddAsync(comboCoupon);
                    await context.SaveChangesAsync();
                    LogInfo(logger, "[DbInitializer] Đã tạo mã coupon COMBO15 (giảm 15% trọn bộ Routine).");
                }

                // 3. Cập nhật RoutineStep cho các sản phẩm hiện có nếu đang null
                var productsWithoutStep = await context.Products
                    .Include(p => p.Category)
                    .Where(p => p.RoutineStep == null)
                    .ToListAsync();

                if (productsWithoutStep.Any())
                {
                    foreach (var p in productsWithoutStep)
                    {
                        var cat = p.Category?.Slug?.ToLowerInvariant() ?? string.Empty;
                        var name = p.Name.ToLowerInvariant();

                        if (cat.Contains("sua-rua-mat") || cat.Contains("nuoc-tay-trang") || name.Contains("rửa mặt") || name.Contains("cleanser") || name.Contains("tẩy trang"))
                        {
                            p.RoutineStep = "1_Cleanser";
                        }
                        else if (cat.Contains("toner") || name.Contains("toner") || name.Contains("hoa hồng") || name.Contains("cân bằng"))
                        {
                            p.RoutineStep = "2_Toner";
                        }
                        else if (cat.Contains("serum") || name.Contains("serum") || name.Contains("tinh chất") || name.Contains("ampoule"))
                        {
                            p.RoutineStep = "3_Serum";
                        }
                        else if (cat.Contains("kem-duong-am") || name.Contains("kem dưỡng") || name.Contains("moisturizer") || name.Contains("khóa ẩm"))
                        {
                            p.RoutineStep = "4_Moisturizer";
                        }
                    }

                    await context.SaveChangesAsync();
                    LogInfo(logger, $"[DbInitializer] Đã gán phân loại RoutineStep cho {productsWithoutStep.Count} sản phẩm.");
                }
            }
            catch (Exception ex)
            {
                LogError(logger, ex, "[DbInitializer] Lỗi trong quá trình khởi tạo RoutineStep và Coupon COMBO15.");
            }
        }

        private static async Task EnsureOrderStatusHistoriesAndReturnsSeededAsync(ApplicationDbContext context, ILogger? logger)
        {
            try
            {
                if (!await context.OrderStatusHistories.AnyAsync())
                {
                    var now = DateTime.UtcNow;
                    var recentOrders = await context.Orders
                        .OrderByDescending(o => o.CreatedAt)
                        .Take(40)
                        .ToListAsync();

                    var historyList = new List<OrderStatusHistory>();
                    foreach (var ord in recentOrders)
                    {
                        string trackCode = $"GHN{ord.CreatedAt:yyMMdd}{(ord.OrderCode.Length >= 6 ? ord.OrderCode[^6..].Replace("-", "").ToUpperInvariant() : ord.Id.ToString("D6"))}";
                        string carrier = ord.ShippingFee == 45000m ? "Giao Hàng Hỏa Tốc 2H (Omni VIP Express)" : "Giao Hàng Nhanh (GHN Express)";

                        historyList.Add(new OrderStatusHistory
                        {
                            OrderId = ord.Id,
                            Status = "PENDING",
                            Title = "Đặt hàng",
                            Description = $"Hệ thống tiếp nhận đơn hàng #{ord.OrderCode} từ kênh {ord.OrderSource}.",
                            ChangedByUserId = ord.UserId,
                            ChangedByName = ord.CustomerName,
                            CreatedAt = ord.CreatedAt
                        });

                        if (ord.OrderStatus == "CANCELLED")
                        {
                            historyList.Add(new OrderStatusHistory
                            {
                                OrderId = ord.Id,
                                Status = "CANCELLED",
                                Title = "Đã hủy đơn hàng & Hoàn tồn FEFO",
                                Description = !string.IsNullOrWhiteSpace(ord.Note) ? ord.Note : "Khách hàng yêu cầu hủy đơn • Đã hoàn trả tồn kho đúng lô FEFO.",
                                ChangedByName = ord.CustomerName,
                                CreatedAt = ord.CreatedAt.AddMinutes(20)
                            });
                            continue;
                        }

                        if (ord.OrderStatus == "CONFIRMED" || ord.OrderStatus == "PROCESSING" || ord.OrderStatus == "SHIPPING" || ord.OrderStatus == "COMPLETED")
                        {
                            historyList.Add(new OrderStatusHistory
                            {
                                OrderId = ord.Id,
                                Status = "CONFIRMED",
                                Title = "Xác nhận",
                                Description = "Bộ phận CSKH đã xác nhận đơn hàng & giữ chỗ lô FEFO.",
                                ChangedByName = "CSKH Omni Beauty",
                                CreatedAt = ord.CreatedAt.AddMinutes(15)
                            });
                        }

                        if (ord.OrderStatus == "PROCESSING" || ord.OrderStatus == "SHIPPING" || ord.OrderStatus == "COMPLETED")
                        {
                            historyList.Add(new OrderStatusHistory
                            {
                                OrderId = ord.Id,
                                Status = "PROCESSING",
                                Title = "Đóng gói (Lô FEFO đã cấp phát)",
                                Description = "Kho GSP đã cấp phát lô mỹ phẩm theo chuẩn FEFO và đóng gói chống sốc.",
                                ChangedByName = "Kho Điều Phối FEFO",
                                CreatedAt = ord.CreatedAt.AddMinutes(50)
                            });
                        }

                        if (ord.OrderStatus == "SHIPPING" || ord.OrderStatus == "COMPLETED")
                        {
                            historyList.Add(new OrderStatusHistory
                            {
                                OrderId = ord.Id,
                                Status = "HANDED_OVER",
                                Title = "Bàn giao vận chuyển",
                                Description = $"Đã bàn giao kiện hàng cho {carrier}. Mã vận đơn: {trackCode}.",
                                TrackingCode = trackCode,
                                CarrierName = carrier,
                                ChangedByName = "Điều Phối Logistics",
                                CreatedAt = ord.CreatedAt.AddHours(2)
                            });

                            historyList.Add(new OrderStatusHistory
                            {
                                OrderId = ord.Id,
                                Status = "SHIPPING",
                                Title = "Đang giao",
                                Description = $"Bưu tá {carrier} đang giao kiện hàng #{trackCode} đến người nhận.",
                                TrackingCode = trackCode,
                                CarrierName = carrier,
                                ChangedByName = carrier,
                                CreatedAt = ord.CreatedAt.AddHours(3)
                            });
                        }

                        if (ord.OrderStatus == "COMPLETED")
                        {
                            var compTime = ord.UpdatedAt ?? ord.CreatedAt.AddHours(8);
                            if (compTime <= ord.CreatedAt.AddHours(3)) compTime = ord.CreatedAt.AddHours(5);
                            historyList.Add(new OrderStatusHistory
                            {
                                OrderId = ord.Id,
                                Status = "COMPLETED",
                                Title = "Giao thành công",
                                Description = "Kiện hàng đã giao tận tay khách hàng thành công & cộng điểm thưởng Beauty Club.",
                                TrackingCode = trackCode,
                                CarrierName = carrier,
                                ChangedByName = carrier,
                                CreatedAt = compTime
                            });
                        }
                    }

                    if (historyList.Any())
                    {
                        await context.OrderStatusHistories.AddRangeAsync(historyList);
                        await context.SaveChangesAsync();
                        LogInfo(logger, $"[DbInitializer] Đã khởi tạo {historyList.Count} mốc OrderStatusHistory cho 40 đơn hàng gần nhất.");
                    }
                }

                if (!await context.OrderReturnRequests.AnyAsync())
                {
                    var now = DateTime.UtcNow;
                    var completedOrder = await context.Orders
                        .Where(o => o.OrderStatus == "COMPLETED")
                        .OrderByDescending(o => o.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (completedOrder != null)
                    {
                        var sampleReturn = new OrderReturnRequest
                        {
                            OrderId = completedOrder.Id,
                            UserId = completedOrder.UserId,
                            RequestCode = $"RET-{now:yyyyMMdd}-DEMO",
                            Reason = "Vỏ hộp bị móp nhẹ trong quá trình vận chuyển",
                            Description = "Nhờ shop hỗ trợ kiểm tra đổi hộp mới giúp mình để làm quà tặng sinh nhật ạ.",
                            EvidenceUrlsJson = "[\"/images/products/serum-b5.jpg\"]",
                            Status = "PENDING",
                            CreatedAt = now.AddHours(-3)
                        };
                        await context.OrderReturnRequests.AddAsync(sampleReturn);
                        await context.SaveChangesAsync();
                        LogInfo(logger, "[DbInitializer] Đã khởi tạo 1 yêu cầu Đổi/Trả mẫu (OrderReturnRequest).");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(logger, ex, "[DbInitializer] Lỗi khi seed OrderStatusHistories & OrderReturnRequests.");
            }
        }

        private static async Task EnsureLoyaltyGamificationSeededAsync(ApplicationDbContext context, ILogger? logger)
        {
            try
            {
                var walletsWithoutCode = await context.LoyaltyWallets
                    .Where(w => w.ReferralCode == null || w.ReferralCode == "")
                    .ToListAsync();

                if (walletsWithoutCode.Any())
                {
                    foreach (var w in walletsWithoutCode)
                    {
                        w.ReferralCode = $"OMNI-REF-{w.UserId:D4}";
                    }
                    await context.SaveChangesAsync();
                    LogInfo(logger, $"[DbInitializer] Đã cấp mã giới thiệu (ReferralCode) cho {walletsWithoutCode.Count} ví thành viên.");
                }

                // Đảm bảo khách hàng mẫu (customer01) có ví điểm >= 850 điểm và sổ cái PointTransactions khớp số dư
                var demoCustomer = await context.Users
                    .FirstOrDefaultAsync(u => u.Username == "customer01" || u.Email == "phuonganh@omnibeauty.vn" || u.UserType == 1);

                if (demoCustomer != null)
                {
                    var wallet = await context.LoyaltyWallets
                        .Include(w => w.Transactions)
                        .FirstOrDefaultAsync(w => w.UserId == demoCustomer.Id);

                    if (wallet == null)
                    {
                        var goldTier = await context.MemberTiers.FirstOrDefaultAsync(t => t.TierName == "Gold")
                                    ?? await context.MemberTiers.OrderBy(t => t.SortOrder).FirstOrDefaultAsync();
                        if (goldTier != null)
                        {
                            wallet = new LoyaltyWallet
                            {
                                UserId = demoCustomer.Id,
                                CurrentTierId = goldTier.Id,
                                CurrentBalance = 850,
                                TotalPointsEarned = 850,
                                TotalSpend12Months = 8550000m,
                                ReferralCode = $"OMNI-REF-{demoCustomer.Id:D4}",
                                CurrentStreak = 0,
                                QrCodeData = $"BEAUTY-MEM-{demoCustomer.Id:D6}",
                                CreatedAt = DateTime.UtcNow.AddMonths(-6)
                            };
                            await context.LoyaltyWallets.AddAsync(wallet);
                            await context.SaveChangesAsync();
                        }
                    }

                    if (wallet != null && !wallet.Transactions.Any())
                    {
                        wallet.CurrentBalance = 850;
                        wallet.TotalPointsEarned = 850;
                        wallet.TotalSpend12Months = Math.Max(wallet.TotalSpend12Months, 8550000m);

                        var now = DateTime.UtcNow;
                        await context.PointTransactions.AddRangeAsync(new[]
                        {
                            new PointTransaction
                            {
                                WalletId = wallet.Id,
                                TransactionType = "BONUS",
                                Points = 100,
                                BalanceBefore = 0,
                                BalanceAfter = 100,
                                Description = "Thưởng chào mừng thành viên mới Omni Beauty Club (+100 điểm)",
                                CreatedAt = now.AddDays(-30)
                            },
                            new PointTransaction
                            {
                                WalletId = wallet.Id,
                                TransactionType = "EARN",
                                Points = 450,
                                BalanceBefore = 100,
                                BalanceAfter = 550,
                                Description = "Tích điểm đơn hàng Nước hoa & Serum cao cấp (+450 điểm)",
                                CreatedAt = now.AddDays(-14)
                            },
                            new PointTransaction
                            {
                                WalletId = wallet.Id,
                                TransactionType = "EARN",
                                Points = 300,
                                BalanceBefore = 550,
                                BalanceAfter = 850,
                                Description = "Tích điểm đơn hàng Bộ đôi Kem chống nắng & Dưỡng ẩm (+300 điểm)",
                                CreatedAt = now.AddDays(-4)
                            }
                        });
                        await context.SaveChangesAsync();
                    }

                    // Seed 1 lượt giới thiệu bạn bè mẫu nếu bảng LoyaltyReferrals đang trống
                    if (!await context.LoyaltyReferrals.AnyAsync() && wallet != null)
                    {
                        var friendUser = await context.Users
                            .Where(u => u.Id != demoCustomer.Id && u.UserType == 1)
                            .OrderByDescending(u => u.Id)
                            .FirstOrDefaultAsync();

                        if (friendUser != null)
                        {
                            await context.LoyaltyReferrals.AddAsync(new LoyaltyReferral
                            {
                                ReferrerUserId = demoCustomer.Id,
                                ReferredUserId = friendUser.Id,
                                ReferralCode = wallet.ReferralCode ?? $"OMNI-REF-{demoCustomer.Id:D4}",
                                Status = "REWARDED",
                                ReferrerPointsAwarded = 100,
                                ReferredPointsAwarded = 50,
                                RewardVoucherCode = "REF50K-VIP01",
                                CreatedAt = DateTime.UtcNow.AddDays(-10),
                                RewardedAt = DateTime.UtcNow.AddDays(-9)
                            });
                            await context.SaveChangesAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(logger, ex, "[DbInitializer] Lỗi khi seed dữ liệu Gamification Loyalty.");
            }
        }

        private static async Task EnsureCleanBeautyColumnsExistAsync(ApplicationDbContext context, ILogger? logger)
        {
            try
            {
                // 1. Đảm bảo các cột Clean Beauty tồn tại trong bảng Products (SQL Server Relational)
                if (context.Database.IsRelational())
                {
                    const string sql = @"
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Products' AND COLUMN_NAME='IsAlcoholFree')
                        BEGIN
                            ALTER TABLE [Products] ADD [IsAlcoholFree] BIT NOT NULL CONSTRAINT DF_Products_IsAlcoholFree DEFAULT 0;
                        END;
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Products' AND COLUMN_NAME='IsFragranceFree')
                        BEGIN
                            ALTER TABLE [Products] ADD [IsFragranceFree] BIT NOT NULL CONSTRAINT DF_Products_IsFragranceFree DEFAULT 0;
                        END;
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Products' AND COLUMN_NAME='IsPregnancySafe')
                        BEGIN
                            ALTER TABLE [Products] ADD [IsPregnancySafe] BIT NOT NULL CONSTRAINT DF_Products_IsPregnancySafe DEFAULT 0;
                        END;
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Products' AND COLUMN_NAME='FullIngredients')
                        BEGIN
                            ALTER TABLE [Products] ADD [FullIngredients] NVARCHAR(MAX) NULL;
                        END;";
                    await context.Database.ExecuteSqlRawAsync(sql);
                }

                // 2. Nạp dữ liệu chuỗi thành phần INCI và các cờ an toàn cho các sản phẩm chưa có
                var productsWithoutIngredients = await context.Products
                    .Where(p => string.IsNullOrWhiteSpace(p.FullIngredients))
                    .ToListAsync();

                if (productsWithoutIngredients.Any())
                {
                    foreach (var p in productsWithoutIngredients)
                    {
                        var name = p.Name.ToLowerInvariant();

                        if (name.Contains("b5") || name.Contains("la roche") || name.Contains("effaclar") || name.Contains("phục hồi"))
                        {
                            p.FullIngredients = "Aqua / Water, Glycerin, Dimethicone, Panthenol (Vitamin B5 5%), Niacinamide (Vitamin B3 2%), Pentylene Glycol, Centella Asiatica (Madecassoside 1%), Sodium Hyaluronate (HA), Zinc Gluconate, Citric Acid, Phenoxyethanol";
                            p.IsAlcoholFree = true;
                            p.IsFragranceFree = true;
                            p.IsPregnancySafe = true;
                        }
                        else if (name.Contains("bha") || name.Contains("obagi") || name.Contains("salicylic") || name.Contains("mụn"))
                        {
                            p.FullIngredients = "Water, Salicylic Acid (BHA 2%), Benzyl Alcohol, Ethoxydiglycol, Chamomilla Recutita (Chamomile) Flower Extract, Aloe Barbadensis (Nha Đam) Leaf Juice, Camellia Sinensis (Trà Xanh) Leaf Extract, Glycerin, Sodium Hydroxide";
                            p.IsAlcoholFree = true;
                            p.IsFragranceFree = true;
                            p.IsPregnancySafe = false; // BHA nồng độ cao khuyến cáo không dùng cho phụ nữ mang thai
                        }
                        else if (name.Contains("retinol") || name.Contains("lão hóa") || name.Contains("collagen"))
                        {
                            p.FullIngredients = "Water, Dimethicone, Retinol 1%, Niacinamide, Ceramide NP, Ceramide AP, Sodium Hyaluronate, Tocopherol (Vitamin E), Allantoin, Xanthan Gum, Phenoxyethanol";
                            p.IsAlcoholFree = true;
                            p.IsFragranceFree = true;
                            p.IsPregnancySafe = false; // Retinol chống chỉ định thai kỳ
                        }
                        else if (name.Contains("cerave") || name.Contains("kem dưỡng") || name.Contains("ẩm") || name.Contains("moisturiz"))
                        {
                            p.FullIngredients = "Aqua / Water, Glycerin, Cetearyl Alcohol, Caprylic/Capric Triglyceride, Ceramide NP, Ceramide AP, Ceramide EOP, Hyaluronic Acid (HA), Cholesterol, Tocopherol (Vitamin E), Carbomer, Phenoxyethanol, Ethylhexylglycerin";
                            p.IsAlcoholFree = true;
                            p.IsFragranceFree = true;
                            p.IsPregnancySafe = true;
                        }
                        else if (name.Contains("chống nắng") || name.Contains("anessa") || name.Contains("sunscreen"))
                        {
                            p.FullIngredients = "Water, Dimethicone, Zinc Oxide, Ethylhexyl Methoxycinnamate, Alcohol Denat, Octocrylene, Titanium Dioxide, Camellia Sinensis (Trà Xanh) Leaf Extract, Sodium Hyaluronate, Collagen Thủy Phân, Fragrance";
                            p.IsAlcoholFree = false;
                            p.IsFragranceFree = false;
                            p.IsPregnancySafe = true;
                        }
                        else if (name.Contains("son") || name.Contains("dior") || name.Contains("lipstick"))
                        {
                            p.FullIngredients = "Polyglyceryl-2 Triisostearate, Hydrogenated Polyisobutene, Octyldodecanol, Synthetic Wax, Punica Granatum Flower Extract, Butyrospermum Parkii (Shea) Butter, Fragrance, CI 77891, CI 15850";
                            p.IsAlcoholFree = true;
                            p.IsFragranceFree = false;
                            p.IsPregnancySafe = true;
                        }
                        else
                        {
                            // Công thức chuẩn an toàn da liễu mặc định
                            p.FullIngredients = "Water, Butylene Glycol, Glycerin, Niacinamide 2%, Panthenol (Vitamin B5), Centella Asiatica (Rau Má) Extract, Sodium Hyaluronate, Allantoin, Carbomer, Arginine, 1,2-Hexanediol, Ethylhexylglycerin";
                            p.IsAlcoholFree = true;
                            p.IsFragranceFree = true;
                            p.IsPregnancySafe = true;
                        }
                    }

                    await context.SaveChangesAsync();
                    LogInfo(logger, $"[DbInitializer] Đã nạp thành phần FullIngredients và Clean Beauty flags cho {productsWithoutIngredients.Count} sản phẩm.");
                }
            }
            catch (Exception ex)
            {
                LogError(logger, ex, "[DbInitializer] Lỗi trong quá trình khởi tạo cột Clean Beauty và FullIngredients.");
            }
        }

        private static async Task EnsureFlashSaleAndShowroomStockSeededAsync(ApplicationDbContext context, ILogger? logger)
        {
            try
            {
                // 1. Tìm Showroom Ba Đình / Cầu Giấy (Hà Nội)
                var cgStore = await context.Stores.FirstOrDefaultAsync(s => s.Code == "SH-BD" || s.Code == "SH-CG");

                // 2. Đảm bảo Chiến dịch Flash Sale hoạt động có hạn dùng tương lai
                var flashPromo = await context.Promotions
                    .Include(p => p.PromotionItems)
                    .FirstOrDefaultAsync(p => p.PromotionType == "FLASH_SALE" || p.PromotionCode == "PROMO-FLASH20");

                if (flashPromo != null)
                {
                    if (flashPromo.ValidTo <= DateTime.UtcNow)
                    {
                        flashPromo.ValidTo = DateTime.UtcNow.AddDays(7);
                        flashPromo.IsActive = true;
                        await context.SaveChangesAsync();
                        LogInfo(logger, "[DbInitializer] Đã gia hạn chiến dịch Flash Sale PROMO-FLASH20 thêm 7 ngày.");
                    }
                }
                else
                {
                    flashPromo = new Promotion
                    {
                        PromotionCode = "PROMO-FLASH20",
                        Name = "Flash Sale Deal Sốc Giờ Vàng - Giảm 20%",
                        Description = "Cơ hội săn mỹ phẩm cao cấp chuẩn hạn dùng FEFO với giá ưu đãi cực lớn",
                        PromotionType = "FLASH_SALE",
                        DiscountValue = 20m,
                        MinOrderValue = 200000m,
                        MaxDiscountAmount = 150000m,
                        ValidFrom = DateTime.UtcNow.AddDays(-1),
                        ValidTo = DateTime.UtcNow.AddDays(7),
                        IsActive = true,
                        AppliesTo = "ALL"
                    };
                    await context.Promotions.AddAsync(flashPromo);
                    await context.SaveChangesAsync();
                    LogInfo(logger, "[DbInitializer] Đã khởi tạo chiến dịch Flash Sale PROMO-FLASH20.");
                }

                // 3. Đảm bảo StoreInventories phân bổ tồn kho khả dụng cho Showroom Ba Đình / Cầu Giấy
                if (cgStore != null)
                {
                    var batches = await context.ProductBatches.Take(40).ToListAsync();
                    if (batches.Any())
                    {
                        var existingInvKeys = await context.StoreInventories
                            .Where(si => si.StoreId == cgStore.Id)
                            .Select(si => si.ProductBatchId)
                            .ToListAsync();

                        var missingBatches = batches.Where(b => !existingInvKeys.Contains(b.Id)).ToList();
                        if (missingBatches.Any())
                        {
                            var newInventories = missingBatches.Select(b => new StoreInventory
                            {
                                StoreId = cgStore.Id,
                                ProductBatchId = b.Id,
                                PhysicalQuantity = 18,
                                ReservedQuantity = 2,
                                LastUpdated = DateTime.UtcNow
                            }).ToList();

                            await context.StoreInventories.AddRangeAsync(newInventories);
                            await context.SaveChangesAsync();
                            LogInfo(logger, $"[DbInitializer] Đã phân bổ tồn kho {newInventories.Count} lô hàng cho Showroom {cgStore.Name}.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(logger, ex, "[DbInitializer] Lỗi trong quá trình khởi tạo tồn kho Showroom và Flash Sale.");
            }
        }

        private static async Task EnsureFefoBatchesSynchronizedAsync(ApplicationDbContext context, ILogger? logger)
        {
            var strategy = context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    var now = DateTime.UtcNow;
                    var minFarDate = now.AddMonths(14);
                    var activeProducts = await context.Products
                        .Include(p => p.Batches)
                        .Where(p => p.IsActive)
                        .OrderByDescending(p => p.IsFeatured)
                        .ThenBy(p => p.Id)
                        .ToListAsync();

                    if (!activeProducts.Any())
                    {
                        await transaction.CommitAsync();
                        return;
                    }

                    var newBatches = new List<ProductBatch>();
                    bool hasBatchUpdates = false;

                    // 1. Đảm bảo toàn bộ sản phẩm IsActive = true đều có ít nhất 1 lô Date xa (> 14 tháng) với tồn kho > 0
                    foreach (var product in activeProducts)
                    {
                        var existingFarBatch = product.Batches
                            .Where(b => b.ExpDate > minFarDate)
                            .OrderByDescending(b => b.ExpDate)
                            .FirstOrDefault();

                        if (existingFarBatch == null)
                        {
                            string standardBatchNo = $"LOT-2028-{product.Id:D3}";
                            var existingByNo = product.Batches.FirstOrDefault(b => b.BatchNumber == standardBatchNo);
                            if (existingByNo != null)
                            {
                                existingByNo.ExpDate = now.AddMonths(22).AddDays(product.Id % 25);
                                existingByNo.CurrentQuantity = Math.Max(120, existingByNo.CurrentQuantity);
                                existingByNo.InitialQuantity = Math.Max(150, existingByNo.InitialQuantity);
                                existingByNo.Status = "ACTIVE";
                                existingByNo.ClearanceDiscountPercent = 0m;
                                hasBatchUpdates = true;
                            }
                            else
                            {
                                var farBatch = new ProductBatch
                                {
                                    ProductId = product.Id,
                                    BatchNumber = standardBatchNo,
                                    ManufacturingDate = now.AddMonths(-2),
                                    ExpDate = now.AddMonths(22).AddDays(product.Id % 25),
                                    ImportCost = product.CostPrice > 0 ? product.CostPrice : Math.Round(product.SellingPrice * 0.55m, 0),
                                    InitialQuantity = 150,
                                    CurrentQuantity = 120,
                                    Status = "ACTIVE",
                                    AlertDaysThreshold = 90,
                                    ClearanceDiscountPercent = 0m,
                                    CreatedAt = now.AddMonths(-2)
                                };
                                newBatches.Add(farBatch);
                            }
                        }
                        else if (existingFarBatch.CurrentQuantity <= 0 || existingFarBatch.Status != "ACTIVE")
                        {
                            existingFarBatch.CurrentQuantity = 120;
                            existingFarBatch.InitialQuantity = Math.Max(150, existingFarBatch.InitialQuantity);
                            existingFarBatch.Status = "ACTIVE";
                            hasBatchUpdates = true;
                        }
                    }

                    // 2. Đảm bảo ít nhất 20 sản phẩm chủ lực có thêm 1 bản ghi ProductBatch cận date thật (4-6 tháng, NEAR_EXPIRATION, giảm 45%, tồn 8)
                    var flagshipProducts = activeProducts.Take(Math.Max(20, activeProducts.Count)).ToList();
                    int syncedNearCount = 0;

                    foreach (var product in flagshipProducts)
                    {
                        var nearBatch = product.Batches
                            .Where(b => b.Status == "NEAR_EXPIRATION" || (b.ClearanceDiscountPercent.HasValue && b.ClearanceDiscountPercent.Value > 0) || (b.ExpDate > now && b.ExpDate <= now.AddMonths(7)))
                            .OrderBy(b => b.ExpDate)
                            .FirstOrDefault();

                        if (nearBatch != null)
                        {
                            bool updated = false;
                            if (nearBatch.ExpDate <= now.AddMonths(3) || nearBatch.ExpDate > now.AddMonths(8))
                            {
                                nearBatch.ExpDate = now.AddMonths(4).AddDays(15 + (product.Id % 35));
                                updated = true;
                            }
                            if (nearBatch.CurrentQuantity <= 0)
                            {
                                nearBatch.CurrentQuantity = 8;
                                nearBatch.InitialQuantity = Math.Max(30, nearBatch.InitialQuantity);
                                updated = true;
                            }
                            if (nearBatch.Status != "NEAR_EXPIRATION")
                            {
                                nearBatch.Status = "NEAR_EXPIRATION";
                                updated = true;
                            }
                            if (!nearBatch.ClearanceDiscountPercent.HasValue || nearBatch.ClearanceDiscountPercent.Value <= 0)
                            {
                                nearBatch.ClearanceDiscountPercent = 45m;
                                updated = true;
                            }
                            if (updated)
                            {
                                hasBatchUpdates = true;
                            }
                            syncedNearCount++;
                        }
                        else if (syncedNearCount < 25)
                        {
                            string nearBatchNo = $"LOT-2027-EXP-{product.Id:D3}";
                            var createdNearBatch = new ProductBatch
                            {
                                ProductId = product.Id,
                                BatchNumber = nearBatchNo,
                                ManufacturingDate = now.AddMonths(-19),
                                ExpDate = now.AddMonths(4).AddDays(15 + (product.Id % 35)),
                                ImportCost = product.CostPrice > 0 ? Math.Round(product.CostPrice * 0.9m, 0) : Math.Round(product.SellingPrice * 0.5m, 0),
                                InitialQuantity = 30,
                                CurrentQuantity = 8,
                                Status = "NEAR_EXPIRATION",
                                AlertDaysThreshold = 90,
                                ClearanceDiscountPercent = 45m,
                                CreatedAt = now.AddMonths(-19)
                            };
                            newBatches.Add(createdNearBatch);
                            syncedNearCount++;
                        }
                    }

                    if (newBatches.Any())
                    {
                        await context.ProductBatches.AddRangeAsync(newBatches);
                        await context.SaveChangesAsync();
                        LogInfo(logger, $"[DbInitializer] Đã tạo mới {newBatches.Count} bản ghi ProductBatch FEFO thật vào CSDL.");

                        // Phân bổ StoreInventories cho các lô hàng mới tại các chi nhánh đang hoạt động
                        var activeStores = await context.Stores.Where(s => s.IsActive).ToListAsync();
                        if (activeStores.Any())
                        {
                            var newStoreInventories = new List<StoreInventory>();
                            foreach (var batch in newBatches)
                            {
                                foreach (var store in activeStores)
                                {
                                    int storeQty = batch.Status == "NEAR_EXPIRATION"
                                        ? Math.Max(2, batch.CurrentQuantity / activeStores.Count)
                                        : Math.Max(25, batch.CurrentQuantity / activeStores.Count);

                                    newStoreInventories.Add(new StoreInventory
                                    {
                                        StoreId = store.Id,
                                        ProductBatchId = batch.Id,
                                        PhysicalQuantity = storeQty,
                                        ReservedQuantity = 0,
                                        LastUpdated = now
                                    });
                                }
                            }
                            await context.StoreInventories.AddRangeAsync(newStoreInventories);
                            await context.SaveChangesAsync();
                        }
                    }
                    else if (hasBatchUpdates)
                    {
                        await context.SaveChangesAsync();
                        LogInfo(logger, "[DbInitializer] Đã cập nhật đồng bộ hạn dùng & tồn kho FEFO thật cho các lô hiện có.");
                    }

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    LogError(logger, ex, "[DbInitializer] Lỗi trong quá trình đồng bộ lô hàng FEFO thật (EnsureFefoBatchesSynchronizedAsync).");
                }
            });
        }
    }
}