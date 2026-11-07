using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Domain.Entities;

namespace Omnichannel.Infrastructure.Data
{
    public static partial class DbInitializer
    {
        public static async Task EnsureDelegatedRbacAndDepartmentsSeededAsync(ApplicationDbContext context, ILogger? logger = null)
        {
            try
            {
                // =========================================================================
                // 1. SEED 9 PHÒNG BAN CHUẨN (IDEMPOTENT THEO CODE)
                // =========================================================================
                var deptSeeds = new List<(string Code, string Name, string Description, int SortOrder)>
                {
                    ("BAN_GIAM_DOC", "Ban Giám Đốc & Quản Trị Tối Cao", "Điều hành chiến lược toàn hệ thống và phân bổ thẩm quyền cho các Admin phòng ban", 1),
                    ("PHONG_NHAN_SU", "Phòng Hành Chính - Nhân Sự", "Quản lý hồ sơ nhân sự, tuyển dụng, phân công vai trò và điều phối nhân viên các phòng ban", 2),
                    ("PHONG_SALE", "Phòng Kinh Doanh & Chốt Đơn", "Quản lý đơn hàng đa kênh, tư vấn chốt đơn và chăm sóc khách hàng", 3),
                    ("PHONG_MARKETING", "Phòng Tiếp Thị, Khuyến Mãi & CRM", "Chiến dịch khuyến mãi, phát hành voucher, quản trị thành viên Loyalty & CRM", 4),
                    ("PHONG_GIAO_DIEN_CMS", "Phòng Giao Diện, Menu & Trải Nghiệm Web", "Quản trị cây menu điều hướng, banner quảng cáo, bố cục trang chủ và giao diện thương hiệu", 5),
                    ("PHONG_DU_LIEU_SAN_PHAM", "Phòng Nội Dung & Dữ Liệu Sản Phẩm", "Quản trị danh mục mỹ phẩm, thuộc tính AI Advisor, bài viết cẩm nang và kiểm duyệt đánh giá", 6),
                    ("PHONG_KHO_VAN", "Phòng Kho Vận & Kiểm Soát Hạn Dùng FEFO", "Nhập xuất tồn kho theo lô hạn dùng FEFO và điều chuyển hàng hóa liên chi nhánh", 7),
                    ("PHONG_KE_TOAN", "Phòng Tài Chính - Kế Toán", "Quản lý sổ cái thu chi, công nợ nhà cung cấp, đối soát COD và báo cáo P&L", 8),
                    ("CUA_HANG_BAN_LE", "Hệ Thống Cửa Hàng & Thu Ngân POS", "Vận hành điểm bán lẻ showroom, ca thu ngân POS và phục vụ khách hàng tại quầy", 9)
                };

                var existingDepts = await context.Departments
                    .IgnoreQueryFilters()
                    .ToDictionaryAsync(d => d.Code, StringComparer.OrdinalIgnoreCase);

                bool deptChanged = false;
                foreach (var seed in deptSeeds)
                {
                    if (!existingDepts.TryGetValue(seed.Code, out var dept))
                    {
                        dept = new Department
                        {
                            Code = seed.Code,
                            Name = seed.Name,
                            Description = seed.Description,
                            SortOrder = seed.SortOrder,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        await context.Departments.AddAsync(dept);
                        existingDepts[seed.Code] = dept;
                        deptChanged = true;
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(dept.Description))
                        {
                            dept.Description = seed.Description;
                            deptChanged = true;
                        }
                        if (dept.SortOrder == 0)
                        {
                            dept.SortOrder = seed.SortOrder;
                            deptChanged = true;
                        }
                    }
                }

                if (deptChanged)
                {
                    await context.SaveChangesAsync();
                }

                var deptsByCode = await context.Departments
                    .IgnoreQueryFilters()
                    .ToDictionaryAsync(d => d.Code, StringComparer.OrdinalIgnoreCase);

                if (deptsByCode.TryGetValue("BAN_GIAM_DOC", out var hqDept))
                {
                    bool parentUpdated = false;
                    foreach (var kvp in deptsByCode)
                    {
                        if (!kvp.Key.Equals("BAN_GIAM_DOC", StringComparison.OrdinalIgnoreCase) && kvp.Value.ParentId == null)
                        {
                            kvp.Value.ParentId = hqDept.Id;
                            parentUpdated = true;
                        }
                    }
                    if (parentUpdated)
                    {
                        await context.SaveChangesAsync();
                    }
                }

                // =========================================================================
                // 2. SEED 14 QUYỀN HẠT NHÂN MỚI CHO HR, DELEGATION, CMS & ORDER
                // =========================================================================
                var newPermissions = new List<Permission>
                {
                    new() { Code = "USER_VIEW", Name = "Xem danh sách & hồ sơ nhân sự", Module = "HR", Description = "Xem danh sách và chi tiết hồ sơ nhân sự trong phạm vi phòng ban" },
                    new() { Code = "USER_CREATE", Name = "Thêm mới nhân sự", Module = "HR", Description = "Tạo mới hồ sơ và tài khoản nhân sự trong phạm vi được giao" },
                    new() { Code = "USER_EDIT", Name = "Chỉnh sửa & Khóa/Mở khóa nhân sự", Module = "HR", Description = "Cập nhật thông tin công việc, tạm khóa hoặc mở khóa tài khoản nhân sự" },
                    new() { Code = "USER_DELETE", Name = "Xóa mềm & Khôi phục nhân sự", Module = "HR", Description = "Đưa nhân sự vào thùng rác hoặc khôi phục nhân sự đã xóa mềm" },
                    new() { Code = "USER_RESET_PASSWORD", Name = "Đặt lại mật khẩu nhân sự", Module = "HR", Description = "Cấp lại mật khẩu mới cho nhân viên cấp dưới" },
                    new() { Code = "USER_EXPORT", Name = "Xuất dữ liệu nhân sự (Excel/CSV)", Module = "HR", Description = "Xuất danh sách nhân sự ra file báo cáo CSV/Excel" },
                    new() { Code = "USER_ASSIGN_ROLE", Name = "Gán vai trò cho nhân sự", Module = "HR", Description = "Gán hoặc thay đổi vai trò của nhân viên trong phạm vi cấp bậc cho phép" },
                    new() { Code = "DEPARTMENT_VIEW", Name = "Xem sơ đồ phòng ban", Module = "HR", Description = "Xem danh sách và cây cơ cấu tổ chức phòng ban" },
                    new() { Code = "DEPARTMENT_MANAGE", Name = "Quản lý cơ cấu phòng ban", Module = "HR", Description = "Thêm, sửa, chỉ định trưởng phòng và sắp xếp phòng ban" },
                    new() { Code = "ROLE_VIEW", Name = "Xem danh sách vai trò", Module = "SECURITY", Description = "Xem thông tin các vai trò trong hệ thống" },
                    new() { Code = "ROLE_MANAGE_SCOPED", Name = "Quản lý vai trò cấp phòng ban", Module = "SECURITY", Description = "Cấu hình vai trò cấp dưới trong phạm vi phòng ban phụ trách" },
                    new() { Code = "PERMISSION_DELEGATE", Name = "Ủy quyền & Chia quyền cấp dưới", Module = "SECURITY", Description = "Tích cấp hoặc thu hồi quyền cá nhân trong phạm vi Scope cho nhân viên dưới quyền" },
                    new() { Code = "UI_SECTION_MANAGE", Name = "Quản lý bố cục khung trang chủ", Module = "CMS", Description = "Ẩn/hiện, sắp xếp và chỉnh sửa các khối giao diện trên trang chủ" },
                    new() { Code = "ORDER_CONFIRM", Name = "Xác nhận & Chốt đơn hàng", Module = "ORDER", Description = "Xác nhận đơn hàng mới, chốt đơn và chuyển trạng thái xử lý" }
                };

                var existingPermCodes = await context.Permissions
                    .Select(p => p.Code)
                    .ToListAsync();
                var existingPermSet = new HashSet<string>(existingPermCodes, StringComparer.OrdinalIgnoreCase);

                bool permAdded = false;
                foreach (var np in newPermissions)
                {
                    if (!existingPermSet.Contains(np.Code))
                    {
                        await context.Permissions.AddAsync(np);
                        existingPermSet.Add(np.Code);
                        permAdded = true;
                    }
                }

                if (permAdded)
                {
                    await context.SaveChangesAsync();
                }

                var permsByCode = await context.Permissions
                    .AsNoTracking()
                    .ToDictionaryAsync(p => p.Code, p => p.Id, StringComparer.OrdinalIgnoreCase);

                // =========================================================================
                // 3. SEED 8 ROLE MỚI & CẬP NHẬT 6 ROLE CỐT LÕI (TỔNG 14 ROLE)
                // =========================================================================
                var allRoleDefinitions = new List<(string Name, int Level, string DisplayName, string Color, string? DeptCode, string Description)>
                {
                    ("SuperAdmin", 100, "Quản Trị Viên Tối Cao (Admin Tổng)", "purple", "BAN_GIAM_DOC", "Quản trị viên toàn quyền hệ thống, phân bổ phạm vi ủy quyền cho các Admin nhỏ"),
                    ("UiAdmin", 50, "Admin Giao diện & Menu", "pink", "PHONG_GIAO_DIEN_CMS", "Admin phụ trách giao diện, menu điều hướng, banner, khối trang chủ và bảng màu thương hiệu"),
                    ("DataAdmin", 50, "Admin Nội dung & Dữ liệu", "indigo", "PHONG_DU_LIEU_SAN_PHAM", "Admin phụ trách dữ liệu sản phẩm, danh mục, thuộc tính, bài viết, đánh giá và tra cứu tồn kho"),
                    ("HrAdmin", 50, "Trưởng phòng Nhân sự", "teal", "PHONG_NHAN_SU", "Quản trị viên phòng ban nhân sự: thêm, sửa, xóa, khóa nhân viên, gán vai trò và phân việc"),
                    ("SalesAdmin", 50, "Giám đốc Sale & Marketing", "rose", "PHONG_SALE", "Giám đốc phụ trách đơn hàng, chốt đơn, CRM khách hàng, khuyến mãi, Loyalty và báo cáo doanh số"),
                    ("StoreManager", 30, "Cửa Hàng Trưởng (Quản Lý Chi Nhánh)", "blue", "CUA_HANG_BAN_LE", "Quản lý chi nhánh showroom, điều hành POS và kho chi nhánh"),
                    ("SalesStaff", 10, "Nhân viên Sale / Chốt đơn", "orange", "PHONG_SALE", "Nhân viên kinh doanh: xem đơn hàng, xác nhận chốt đơn, cập nhật trạng thái xử lý và tạo hồ sơ khách hàng"),
                    ("MarketingStaff", 10, "Nhân viên Marketing", "fuchsia", "PHONG_MARKETING", "Nhân viên tiếp thị: quản lý chiến dịch khuyến mãi, coupon, banner quảng cáo và tra cứu CRM/Loyalty"),
                    ("HrStaff", 10, "Nhân viên Nhân sự", "cyan", "PHONG_NHAN_SU", "Chuyên viên hành chính nhân sự: xem, tạo mới và cập nhật hồ sơ nhân viên (không xóa, không gán role)"),
                    ("DataStaff", 10, "Nhân viên Nhập liệu sản phẩm", "violet", "PHONG_DU_LIEU_SAN_PHAM", "Chuyên viên nhập liệu: xem, thêm mới và chỉnh sửa thông tin sản phẩm, bài viết (không xóa)"),
                    ("ContentEditor", 10, "Biên Tập Viên Nội Dung", "emerald", "PHONG_GIAO_DIEN_CMS", "Biên tập viên nội dung CMS, banner, bài viết và kiểm duyệt đánh giá"),
                    ("Cashier", 10, "Thu Ngân Điểm Bán (POS)", "pink", "CUA_HANG_BAN_LE", "Thu ngân điểm bán lẻ POS, tạo đơn tại quầy và tra cứu tồn kho"),
                    ("WarehouseStaff", 10, "Nhân Viên Kho & Kiểm Soát FEFO", "amber", "PHONG_KHO_VAN", "Thủ kho: nhập xuất kho theo lô FEFO, kiểm kê và điều chuyển hàng hóa"),
                    ("Customer", 0, "Khách Hàng Thành Viên", "slate", null, "Khách hàng mua sắm đa kênh tại Storefront và tích điểm Loyalty")
                };

                var existingRoles = await context.Roles
                    .IgnoreQueryFilters()
                    .ToDictionaryAsync(r => r.Name, StringComparer.OrdinalIgnoreCase);

                bool roleChanged = false;
                foreach (var def in allRoleDefinitions)
                {
                    int? targetDeptId = def.DeptCode != null && deptsByCode.TryGetValue(def.DeptCode, out var dObj) ? dObj.Id : null;

                    if (!existingRoles.TryGetValue(def.Name, out var role))
                    {
                        role = new Role
                        {
                            Name = def.Name,
                            DisplayName = def.DisplayName,
                            Description = def.Description,
                            Level = def.Level,
                            DepartmentId = targetDeptId,
                            Color = def.Color,
                            IsSystem = true,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        await context.Roles.AddAsync(role);
                        existingRoles[def.Name] = role;
                        roleChanged = true;
                    }
                    else
                    {
                        if (role.Level != def.Level ||
                            role.DisplayName != def.DisplayName ||
                            !role.IsSystem ||
                            role.Color != def.Color ||
                            role.DepartmentId != targetDeptId)
                        {
                            role.Level = def.Level;
                            role.DisplayName = def.DisplayName;
                            role.IsSystem = true;
                            role.Color = def.Color;
                            role.DepartmentId = targetDeptId;
                            if (string.IsNullOrWhiteSpace(role.Description))
                            {
                                role.Description = def.Description;
                            }
                            role.UpdatedAt = DateTime.UtcNow;
                            roleChanged = true;
                        }
                    }
                }

                if (roleChanged)
                {
                    await context.SaveChangesAsync();
                }

                var rolesByName = await context.Roles
                    .IgnoreQueryFilters()
                    .ToDictionaryAsync(r => r.Name, StringComparer.OrdinalIgnoreCase);

                // =========================================================================
                // 4. GÁN QUYỀN CHO SUPERADMIN (100%) & 8 ROLE MỚI THEO LEAST PRIVILEGE
                // =========================================================================
                var allRolePerms = await context.RolePermissions
                    .AsNoTracking()
                    .ToListAsync();
                var rolePermLookup = allRolePerms
                    .GroupBy(rp => rp.RoleId)
                    .ToDictionary(g => g.Key, g => g.Select(rp => rp.PermissionId).ToHashSet());

                async Task EnsureRoleHasPermissionsAsync(string roleName, IEnumerable<string> permCodes)
                {
                    if (!rolesByName.TryGetValue(roleName, out var roleObj)) return;
                    if (!rolePermLookup.TryGetValue(roleObj.Id, out var currentSet))
                    {
                        currentSet = new HashSet<int>();
                        rolePermLookup[roleObj.Id] = currentSet;
                    }

                    foreach (var code in permCodes)
                    {
                        if (permsByCode.TryGetValue(code, out var pid) && !currentSet.Contains(pid))
                        {
                            await context.RolePermissions.AddAsync(new RolePermission
                            {
                                RoleId = roleObj.Id,
                                PermissionId = pid
                            });
                            currentSet.Add(pid);
                        }
                    }
                }

                // 4.1. SuperAdmin: 100% quyền trong DB
                await EnsureRoleHasPermissionsAsync("SuperAdmin", permsByCode.Keys);

                // 4.2. UiAdmin (ad1): Giao diện, Menu, Banner, Khung trang chủ, Theme, Ủy quyền cấp dưới
                await EnsureRoleHasPermissionsAsync("UiAdmin", new[]
                {
                    "DASHBOARD_VIEW",
                    "CMS_MENU_MANAGE", "CMS_BANNER_MANAGE", "THEME_MANAGE", "CMS_STATIC_PAGE_MANAGE", "UI_SECTION_MANAGE",
                    "PERMISSION_DELEGATE", "USER_VIEW"
                });

                // 4.3. DataAdmin (ad2): Sản phẩm, Bài viết, Đánh giá, Xem kho, Ủy quyền cấp dưới
                await EnsureRoleHasPermissionsAsync("DataAdmin", new[]
                {
                    "DASHBOARD_VIEW",
                    "PRODUCT_VIEW", "PRODUCT_CREATE", "PRODUCT_EDIT", "PRODUCT_DELETE", "PRODUCT_IMPORT",
                    "CMS_POST_MANAGE", "CMS_REVIEW_MODERATE",
                    "INVENTORY_VIEW",
                    "PERMISSION_DELEGATE", "USER_VIEW"
                });

                // 4.4. HrAdmin (ad3): Quản trị Nhân sự toàn diện, Phòng ban, Gán vai trò, Ủy quyền cấp dưới
                await EnsureRoleHasPermissionsAsync("HrAdmin", new[]
                {
                    "DASHBOARD_VIEW",
                    "USER_VIEW", "USER_CREATE", "USER_EDIT", "USER_DELETE", "USER_RESET_PASSWORD", "USER_EXPORT", "USER_ASSIGN_ROLE",
                    "DEPARTMENT_VIEW", "DEPARTMENT_MANAGE",
                    "ROLE_VIEW", "ROLE_MANAGE_SCOPED",
                    "PERMISSION_DELEGATE"
                });

                // 4.5. SalesAdmin (ad4): Đơn hàng, Chốt đơn, CRM, Khuyến mãi, Loyalty, Báo cáo doanh số, Ủy quyền cấp dưới
                await EnsureRoleHasPermissionsAsync("SalesAdmin", new[]
                {
                    "DASHBOARD_VIEW", "DASHBOARD_ANALYTICS",
                    "ORDER_VIEW", "ORDER_CREATE", "ORDER_EDIT", "ORDER_CANCEL", "ORDER_CONFIRM",
                    "CUSTOMER_VIEW", "CUSTOMER_CREATE", "CUSTOMER_EDIT", "CUSTOMER_PROFILE_VIEW",
                    "PROMOTION_MANAGE", "COUPON_MANAGE", "VOUCHER_MANAGE",
                    "LOYALTY_VIEW", "LOYALTY_ADJUST_POINTS", "LOYALTY_TIER_MANAGE",
                    "REPORT_SALES",
                    "PERMISSION_DELEGATE", "USER_VIEW"
                });

                // 4.6. SalesStaff (sale01, sale02): Xem/Tạo/Chốt/Cập nhật trạng thái đơn hàng, Xem/Tạo khách hàng (không hủy/xóa)
                await EnsureRoleHasPermissionsAsync("SalesStaff", new[]
                {
                    "ORDER_VIEW", "ORDER_CREATE", "ORDER_EDIT", "ORDER_CONFIRM",
                    "CUSTOMER_VIEW", "CUSTOMER_CREATE", "CUSTOMER_PROFILE_VIEW",
                    "PRODUCT_VIEW"
                });

                // 4.7. MarketingStaff (mkt01): Khuyến mãi, Coupon, Voucher, Banner, Xem CRM & Loyalty
                await EnsureRoleHasPermissionsAsync("MarketingStaff", new[]
                {
                    "PROMOTION_MANAGE", "COUPON_MANAGE", "VOUCHER_MANAGE",
                    "CMS_BANNER_MANAGE",
                    "CUSTOMER_VIEW", "CUSTOMER_PROFILE_VIEW",
                    "LOYALTY_VIEW", "PRODUCT_VIEW"
                });

                // 4.8. HrStaff (hr01): Xem, Thêm, Sửa nhân sự & Xem phòng ban (không xóa, không gán role, không reset mật khẩu)
                await EnsureRoleHasPermissionsAsync("HrStaff", new[]
                {
                    "USER_VIEW", "USER_CREATE", "USER_EDIT",
                    "DEPARTMENT_VIEW", "ROLE_VIEW"
                });

                // 4.9. DataStaff (data01): Xem, Thêm, Sửa sản phẩm, Bài viết, Xem tồn kho (không xóa sản phẩm)
                await EnsureRoleHasPermissionsAsync("DataStaff", new[]
                {
                    "PRODUCT_VIEW", "PRODUCT_CREATE", "PRODUCT_EDIT",
                    "CMS_POST_MANAGE", "INVENTORY_VIEW"
                });

                // 4.10. Bảo toàn quyền cốt lõi của các vai trò hiện có (StoreManager, Cashier, WarehouseStaff, ContentEditor, Customer)
                await EnsureRoleHasPermissionsAsync("StoreManager", new[]
                {
                    "DASHBOARD_VIEW", "PRODUCT_VIEW",
                    "INVENTORY_VIEW", "INVENTORY_ADJUST", "INVENTORY_STOCKTAKE",
                    "POS_ACCESS", "POS_CHECKOUT", "POS_REFUND", "POS_SHIFT_OPEN", "POS_SHIFT_CLOSE",
                    "ORDER_VIEW", "ORDER_CREATE", "ORDER_EDIT", "ORDER_CANCEL",
                    "CUSTOMER_VIEW", "CUSTOMER_CREATE", "CUSTOMER_EDIT", "CUSTOMER_PROFILE_VIEW",
                    "REPORT_SALES", "REPORT_INVENTORY",
                    "FINANCE_LEDGER_VIEW", "FINANCE_PNL_VIEW",
                    "VOUCHER_MANAGE", "LOYALTY_VIEW", "LOYALTY_ADJUST_POINTS",
                    "TRANSFER_CREATE", "TRANSFER_RECEIVE"
                });

                await EnsureRoleHasPermissionsAsync("Cashier", new[]
                {
                    "PRODUCT_VIEW", "INVENTORY_VIEW",
                    "POS_ACCESS", "POS_CHECKOUT", "POS_SHIFT_OPEN", "POS_SHIFT_CLOSE",
                    "ORDER_VIEW", "ORDER_CREATE",
                    "CUSTOMER_VIEW", "CUSTOMER_CREATE", "LOYALTY_VIEW"
                });

                await EnsureRoleHasPermissionsAsync("WarehouseStaff", new[]
                {
                    "PRODUCT_VIEW", "PRODUCT_IMPORT",
                    "INVENTORY_VIEW", "INVENTORY_IMPORT", "INVENTORY_EXPORT", "INVENTORY_ADJUST", "INVENTORY_STOCKTAKE",
                    "REPORT_INVENTORY", "TRANSFER_CREATE", "TRANSFER_RECEIVE"
                });

                await EnsureRoleHasPermissionsAsync("ContentEditor", new[]
                {
                    "PRODUCT_VIEW",
                    "CMS_POST_MANAGE", "CMS_BANNER_MANAGE", "CMS_MENU_MANAGE", "CMS_REVIEW_MODERATE", "CMS_STATIC_PAGE_MANAGE", "THEME_MANAGE"
                });

                await EnsureRoleHasPermissionsAsync("Customer", new[]
                {
                    "ORDER_VIEW", "ORDER_CREATE", "ORDER_CANCEL",
                    "CUSTOMER_PROFILE_VIEW", "LOYALTY_VIEW"
                });

                await context.SaveChangesAsync();

                // =========================================================================
                // 5. SEED TÀI KHOẢN ADMIN NHỎ (ad1..ad4) & NHÂN VIÊN MẪU (sale01, sale02, mkt01, hr01, data01)
                // =========================================================================
                var defaultPasswordHash = HashPassword("Admin@123");

                var newStaffSeeds = new List<(string Username, string FullName, string Email, string Phone, string RoleName, string DeptCode, string ManagerUsername, string JobTitle, string Gender)>
                {
                    ("ad1", "Lê Hoàng Minh (Admin Giao Diện)", "ad1@canmakebeauty.vn", "0908100001", "UiAdmin", "PHONG_GIAO_DIEN_CMS", "superadmin", "Trưởng Phòng Giao Diện & Menu (Admin 1)", "Nam"),
                    ("ad2", "Trần Thu Thủy (Admin Dữ Liệu)", "ad2@canmakebeauty.vn", "0908100002", "DataAdmin", "PHONG_DU_LIEU_SAN_PHAM", "superadmin", "Trưởng Phòng Nội Dung & Dữ Liệu (Admin 2)", "Nữ"),
                    ("ad3", "Nguyễn Quốc Bảo (Trưởng Phòng Nhân Sự)", "ad3@canmakebeauty.vn", "0908100003", "HrAdmin", "PHONG_NHAN_SU", "superadmin", "Trưởng Phòng Hành Chính - Nhân Sự (Admin 3)", "Nam"),
                    ("ad4", "Phạm Khánh Linh (Giám Đốc Sale)", "ad4@canmakebeauty.vn", "0908100004", "SalesAdmin", "PHONG_SALE", "superadmin", "Giám Đốc Kinh Doanh & Tiếp Thị (Admin 4)", "Nữ"),
                    ("sale01", "Vũ Hải Đăng (NV Sale 01)", "sale01@canmakebeauty.vn", "0908200001", "SalesStaff", "PHONG_SALE", "ad4", "Chuyên Viên Tư Vấn & Chốt Đơn (Ca Sáng)", "Nam"),
                    ("sale02", "Đỗ Mai Phương (NV Sale 02)", "sale02@canmakebeauty.vn", "0908200002", "SalesStaff", "PHONG_SALE", "ad4", "Chuyên Viên Tư Vấn & Chốt Đơn (Ca Chiều)", "Nữ"),
                    ("mkt01", "Hoàng Ngọc Trâm (NV Marketing)", "mkt01@canmakebeauty.vn", "0908200003", "MarketingStaff", "PHONG_MARKETING", "ad4", "Chuyên Viên Chiến Dịch Khuyến Mãi & CRM", "Nữ"),
                    ("hr01", "Bùi Thanh Tâm (NV Nhân Sự)", "hr01@canmakebeauty.vn", "0908200004", "HrStaff", "PHONG_NHAN_SU", "ad3", "Chuyên Viên Hồ Sơ Nhân Sự & Tuyển Dụng", "Nữ"),
                    ("data01", "Đặng Gia Huy (NV Nhập Liệu)", "data01@canmakebeauty.vn", "0908200005", "DataStaff", "PHONG_DU_LIEU_SAN_PHAM", "ad2", "Chuyên Viên Dữ Liệu Mỹ Phẩm & Danh Mục", "Nam")
                };

                var usersByUsername = await context.Users
                    .IgnoreQueryFilters()
                    .ToDictionaryAsync(u => u.Username, StringComparer.OrdinalIgnoreCase);

                bool newUsersAdded = false;
                foreach (var seed in newStaffSeeds)
                {
                    int? deptId = deptsByCode.TryGetValue(seed.DeptCode, out var dObj) ? dObj.Id : null;

                    if (!usersByUsername.TryGetValue(seed.Username, out var userObj))
                    {
                        userObj = new User
                        {
                            Username = seed.Username,
                            NormalizedUsername = seed.Username.ToUpperInvariant(),
                            Email = seed.Email,
                            FullName = seed.FullName,
                            PhoneNumber = seed.Phone,
                            PasswordHash = defaultPasswordHash,
                            UserType = 2,
                            DepartmentId = deptId,
                            JobTitle = seed.JobTitle,
                            Gender = seed.Gender,
                            HireDate = new DateTime(2025, 2, 1),
                            IsActive = true,
                            IsDeleted = false,
                            SecurityStamp = Guid.NewGuid().ToString("N"),
                            CreatedAt = DateTime.UtcNow
                        };
                        await context.Users.AddAsync(userObj);
                        usersByUsername[seed.Username] = userObj;
                        newUsersAdded = true;
                    }
                    else
                    {
                        if (userObj.UserType != 2 || !userObj.IsActive || userObj.IsDeleted || userObj.DepartmentId != deptId || userObj.JobTitle != seed.JobTitle)
                        {
                            userObj.UserType = 2;
                            userObj.IsActive = true;
                            userObj.IsDeleted = false;
                            userObj.DepartmentId = deptId;
                            userObj.JobTitle = seed.JobTitle;
                            userObj.Gender ??= seed.Gender;
                            userObj.HireDate ??= new DateTime(2025, 2, 1);
                            newUsersAdded = true;
                        }
                    }
                }

                if (newUsersAdded)
                {
                    await context.SaveChangesAsync();
                }

                // Đồng bộ lại danh sách nhân sự nội bộ (EmployeeCode, DepartmentId, ManagerId, UserRoles)
                var allStaffUsers = await context.Users
                    .IgnoreQueryFilters()
                    .Where(u => u.UserType >= 2)
                    .OrderBy(u => u.Id)
                    .ToListAsync();

                usersByUsername = allStaffUsers.ToDictionary(u => u.Username, StringComparer.OrdinalIgnoreCase);
                var superAdminUser = usersByUsername.TryGetValue("superadmin", out var sa) ? sa : allStaffUsers.FirstOrDefault();
                var managerHqUser = usersByUsername.TryGetValue("manager_hq", out var mhq) ? mhq : null;
                var managerCgUser = usersByUsername.TryGetValue("manager_cg", out var mcg) ? mcg : null;
                var ad1User = usersByUsername.TryGetValue("ad1", out var uAd1) ? uAd1 : null;
                var ad2User = usersByUsername.TryGetValue("ad2", out var uAd2) ? uAd2 : null;
                var ad3User = usersByUsername.TryGetValue("ad3", out var uAd3) ? uAd3 : null;
                var ad4User = usersByUsername.TryGetValue("ad4", out var uAd4) ? uAd4 : null;

                var usedCodes = new HashSet<string>(
                    allStaffUsers.Where(u => !string.IsNullOrWhiteSpace(u.EmployeeCode)).Select(u => u.EmployeeCode!),
                    StringComparer.OrdinalIgnoreCase);

                int nextSeq = 1;
                bool staffMetaChanged = false;

                foreach (var u in allStaffUsers)
                {
                    if (string.IsNullOrWhiteSpace(u.EmployeeCode))
                    {
                        while (usedCodes.Contains($"NV{nextSeq:D4}"))
                        {
                            nextSeq++;
                        }
                        u.EmployeeCode = $"NV{nextSeq:D4}";
                        usedCodes.Add(u.EmployeeCode);
                        staffMetaChanged = true;
                    }

                    if (!u.HireDate.HasValue)
                    {
                        u.HireDate = new DateTime(2025, 1, 15);
                        staffMetaChanged = true;
                    }

                    string uname = u.Username.ToLowerInvariant();
                    string targetDeptCode = uname switch
                    {
                        "superadmin" or "admin" => "BAN_GIAM_DOC",
                        "ad1" or "editor01" => "PHONG_GIAO_DIEN_CMS",
                        "ad2" or "data01" => "PHONG_DU_LIEU_SAN_PHAM",
                        "ad3" or "hr01" => "PHONG_NHAN_SU",
                        "ad4" or "sale01" or "sale02" => "PHONG_SALE",
                        "mkt01" => "PHONG_MARKETING",
                        "warehouse01" or "warehouse02" => "PHONG_KHO_VAN",
                        "manager_hq" or "manager_cg" or "cashier01" or "cashier02" => "CUA_HANG_BAN_LE",
                        _ => "CUA_HANG_BAN_LE"
                    };

                    if (!u.DepartmentId.HasValue && deptsByCode.TryGetValue(targetDeptCode, out var deptEntity))
                    {
                        u.DepartmentId = deptEntity.Id;
                        staffMetaChanged = true;
                    }

                    if (string.IsNullOrWhiteSpace(u.JobTitle))
                    {
                        u.JobTitle = uname switch
                        {
                            "superadmin" => "Tổng Giám Đốc Hệ Thống (SuperAdmin)",
                            "admin" => "Quản Trị Viên Hệ Thống",
                            "manager_hq" => "Cửa Hàng Trưởng Flagship Quận 1",
                            "manager_cg" => "Cửa Hàng Trưởng Chi Nhánh Cầu Giấy",
                            "cashier01" => "Chuyên Viên Thu Ngân POS (Q1)",
                            "cashier02" => "Chuyên Viên Thu Ngân POS (Cầu Giấy)",
                            "warehouse01" => "Chuyên Viên Kho Trung Tâm & FEFO",
                            "warehouse02" => "Chuyên Viên Kho Chi Nhánh Cà Giấy",
                            "editor01" => "Biên Tập Viên Nội Dung & Giao Diện CMS",
                            _ => "Chuyên Viên Nội Bộ"
                        };
                        staffMetaChanged = true;
                    }

                    // Gán người quản lý trực tiếp (ManagerId) đúng theo sơ đồ phân cấp Phiên 4
                    int? expectedManagerId = uname switch
                    {
                        "superadmin" => null,
                        "admin" or "ad1" or "ad2" or "ad3" or "ad4" or "manager_hq" or "manager_cg" => superAdminUser?.Id,
                        "editor01" => ad1User?.Id ?? superAdminUser?.Id,
                        "data01" => ad2User?.Id ?? superAdminUser?.Id,
                        "hr01" => ad3User?.Id ?? superAdminUser?.Id,
                        "sale01" or "sale02" or "mkt01" => ad4User?.Id ?? superAdminUser?.Id,
                        "cashier01" or "warehouse01" => managerHqUser?.Id ?? superAdminUser?.Id,
                        "cashier02" or "warehouse02" => managerCgUser?.Id ?? superAdminUser?.Id,
                        _ => u.ManagerId ?? superAdminUser?.Id
                    };

                    if (expectedManagerId.HasValue && expectedManagerId.Value != u.Id && u.ManagerId != expectedManagerId)
                    {
                        u.ManagerId = expectedManagerId;
                        staffMetaChanged = true;
                    }
                }

                if (staffMetaChanged)
                {
                    await context.SaveChangesAsync();
                }

                // Đảm bảo gán UserRoles cho 9 tài khoản mới (idempotent)
                var existingUserRoles = await context.UserRoles.AsNoTracking().ToListAsync();
                var userRoleSet = existingUserRoles.Select(ur => (ur.UserId, ur.RoleId)).ToHashSet();
                bool userRoleAdded = false;

                foreach (var seed in newStaffSeeds)
                {
                    if (usersByUsername.TryGetValue(seed.Username, out var uObj) &&
                        rolesByName.TryGetValue(seed.RoleName, out var rObj))
                    {
                        if (!userRoleSet.Contains((uObj.Id, rObj.Id)))
                        {
                            int? assignedBy = usersByUsername.TryGetValue(seed.ManagerUsername, out var mgrObj)
                                ? mgrObj.Id
                                : superAdminUser?.Id;

                            await context.UserRoles.AddAsync(new UserRole
                            {
                                UserId = uObj.Id,
                                RoleId = rObj.Id,
                                AssignedByUserId = assignedBy,
                                AssignedAt = DateTime.UtcNow
                            });
                            userRoleSet.Add((uObj.Id, rObj.Id));
                            userRoleAdded = true;
                        }
                    }
                }

                if (userRoleAdded)
                {
                    await context.SaveChangesAsync();
                }

                // =========================================================================
                // 6. CẬP NHẬT TRƯỞNG PHÒNG (Department.HeadUserId)
                // =========================================================================
                var headMapping = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["BAN_GIAM_DOC"] = superAdminUser?.Id,
                    ["PHONG_GIAO_DIEN_CMS"] = ad1User?.Id,
                    ["PHONG_DU_LIEU_SAN_PHAM"] = ad2User?.Id,
                    ["PHONG_NHAN_SU"] = ad3User?.Id,
                    ["PHONG_SALE"] = ad4User?.Id,
                    ["PHONG_MARKETING"] = ad4User?.Id,
                    ["CUA_HANG_BAN_LE"] = managerHqUser?.Id
                };

                bool headChanged = false;
                foreach (var kvp in headMapping)
                {
                    if (kvp.Value.HasValue && deptsByCode.TryGetValue(kvp.Key, out var dEntity))
                    {
                        if (dEntity.HeadUserId != kvp.Value.Value)
                        {
                            dEntity.HeadUserId = kvp.Value.Value;
                            dEntity.UpdatedAt = DateTime.UtcNow;
                            headChanged = true;
                        }
                    }
                }

                if (headChanged)
                {
                    await context.SaveChangesAsync();
                }

                // =========================================================================
                // 7. SEED PHẠM VI ỦY QUYỀN (AdminDelegationScope) CHO ad1, ad2, ad3, ad4
                // =========================================================================
                async Task EnsureAdminScopeAsync(
                    User? adminUser,
                    bool canCreateUser,
                    bool canEditUser,
                    bool canDeleteUser,
                    bool canResetPassword,
                    bool canAssignRole,
                    bool canDelegateFurther,
                    int maxGrantLevel,
                    IEnumerable<string> deptCodes,
                    IEnumerable<string> permCodes)
                {
                    if (adminUser == null) return;

                    var scope = await context.AdminDelegationScopes
                        .Include(s => s.AllowedPermissions)
                        .Include(s => s.AllowedDepartments)
                        .FirstOrDefaultAsync(s => s.AdminUserId == adminUser.Id);

                    if (scope == null)
                    {
                        scope = new AdminDelegationScope
                        {
                            AdminUserId = adminUser.Id,
                            CanCreateUser = canCreateUser,
                            CanEditUser = canEditUser,
                            CanDeleteUser = canDeleteUser,
                            CanResetPassword = canResetPassword,
                            CanAssignRole = canAssignRole,
                            CanDelegateFurther = canDelegateFurther,
                            MaxGrantLevel = maxGrantLevel,
                            GrantedByUserId = superAdminUser?.Id,
                            CreatedAt = DateTime.UtcNow
                        };
                        await context.AdminDelegationScopes.AddAsync(scope);
                        await context.SaveChangesAsync();
                    }

                    var currentDeptIds = scope.AllowedDepartments.Select(d => d.DepartmentId).ToHashSet();
                    foreach (var dCode in deptCodes)
                    {
                        if (deptsByCode.TryGetValue(dCode, out var dObj) && !currentDeptIds.Contains(dObj.Id))
                        {
                            await context.AdminDelegationDepartments.AddAsync(new AdminDelegationDepartment
                            {
                                AdminDelegationScopeId = scope.Id,
                                DepartmentId = dObj.Id
                            });
                            currentDeptIds.Add(dObj.Id);
                        }
                    }

                    var currentPermIds = scope.AllowedPermissions.Select(p => p.PermissionId).ToHashSet();
                    foreach (var pCode in permCodes)
                    {
                        if (permsByCode.TryGetValue(pCode, out var pId) && !currentPermIds.Contains(pId))
                        {
                            await context.AdminDelegationPermissions.AddAsync(new AdminDelegationPermission
                            {
                                AdminDelegationScopeId = scope.Id,
                                PermissionId = pId
                            });
                            currentPermIds.Add(pId);
                        }
                    }

                    await context.SaveChangesAsync();
                }

                // 7.1. Scope cho ad1 (UiAdmin - Phòng Giao Diện & Menu):
                // Quản lý PHONG_GIAO_DIEN_CMS, gán role ContentEditor (Level 10 < MaxGrantLevel 50), không tạo/xóa user
                await EnsureAdminScopeAsync(
                    adminUser: ad1User,
                    canCreateUser: false,
                    canEditUser: true,
                    canDeleteUser: false,
                    canResetPassword: true,
                    canAssignRole: true,
                    canDelegateFurther: false,
                    maxGrantLevel: 50,
                    deptCodes: new[] { "PHONG_GIAO_DIEN_CMS" },
                    permCodes: new[]
                    {
                        "DASHBOARD_VIEW",
                        "CMS_MENU_MANAGE", "CMS_BANNER_MANAGE", "THEME_MANAGE", "CMS_STATIC_PAGE_MANAGE", "UI_SECTION_MANAGE"
                    });

                // 7.2. Scope cho ad2 (DataAdmin - Phòng Nội Dung & Dữ Liệu):
                // Quản lý PHONG_DU_LIEU_SAN_PHAM, gán role DataStaff (Level 10 < MaxGrantLevel 50), cấp quyền PRODUCT/CMS_POST/REVIEW
                await EnsureAdminScopeAsync(
                    adminUser: ad2User,
                    canCreateUser: false,
                    canEditUser: true,
                    canDeleteUser: false,
                    canResetPassword: true,
                    canAssignRole: true,
                    canDelegateFurther: false,
                    maxGrantLevel: 50,
                    deptCodes: new[] { "PHONG_DU_LIEU_SAN_PHAM" },
                    permCodes: new[]
                    {
                        "DASHBOARD_VIEW",
                        "PRODUCT_VIEW", "PRODUCT_CREATE", "PRODUCT_EDIT", "PRODUCT_DELETE", "PRODUCT_IMPORT",
                        "CMS_POST_MANAGE", "CMS_REVIEW_MODERATE", "INVENTORY_VIEW"
                    });

                // 7.3. Scope cho ad3 (HrAdmin - Trưởng Phòng Nhân Sự):
                // CanCreate/Edit/Delete/ResetPassword/AssignRole cho tất cả phòng ban TRỪ BAN_GIAM_DOC; chỉ gán role Level <= 30 (MaxGrantLevel = 50 -> Level < 50)
                await EnsureAdminScopeAsync(
                    adminUser: ad3User,
                    canCreateUser: true,
                    canEditUser: true,
                    canDeleteUser: true,
                    canResetPassword: true,
                    canAssignRole: true,
                    canDelegateFurther: false,
                    maxGrantLevel: 50,
                    deptCodes: new[]
                    {
                        "PHONG_NHAN_SU", "PHONG_SALE", "PHONG_MARKETING", "PHONG_GIAO_DIEN_CMS",
                        "PHONG_DU_LIEU_SAN_PHAM", "PHONG_KHO_VAN", "PHONG_KE_TOAN", "CUA_HANG_BAN_LE"
                    },
                    permCodes: new[]
                    {
                        "DASHBOARD_VIEW",
                        "USER_VIEW", "USER_CREATE", "USER_EDIT", "USER_DELETE", "USER_RESET_PASSWORD", "USER_EXPORT", "USER_ASSIGN_ROLE",
                        "DEPARTMENT_VIEW", "DEPARTMENT_MANAGE", "ROLE_VIEW", "ROLE_MANAGE_SCOPED"
                    });

                // 7.4. Scope cho ad4 (SalesAdmin - Giám Đốc Sale & Marketing):
                // Quản lý PHONG_SALE + PHONG_MARKETING; cấp quyền ORDER/CUSTOMER/PROMOTION/LOYALTY/REPORT; CanAssignRole (SalesStaff, MarketingStaff); CanCreateUser = false (tách bạch nhiệm vụ tuyển dụng cho HR ad3)
                await EnsureAdminScopeAsync(
                    adminUser: ad4User,
                    canCreateUser: false,
                    canEditUser: true,
                    canDeleteUser: false,
                    canResetPassword: true,
                    canAssignRole: true,
                    canDelegateFurther: false,
                    maxGrantLevel: 50,
                    deptCodes: new[] { "PHONG_SALE", "PHONG_MARKETING" },
                    permCodes: new[]
                    {
                        "DASHBOARD_VIEW", "DASHBOARD_ANALYTICS",
                        "ORDER_VIEW", "ORDER_CREATE", "ORDER_EDIT", "ORDER_CANCEL", "ORDER_CONFIRM",
                        "CUSTOMER_VIEW", "CUSTOMER_CREATE", "CUSTOMER_EDIT", "CUSTOMER_PROFILE_VIEW",
                        "PROMOTION_MANAGE", "COUPON_MANAGE", "VOUCHER_MANAGE",
                        "LOYALTY_VIEW", "LOYALTY_ADJUST_POINTS", "LOYALTY_TIER_MANAGE",
                        "REPORT_SALES"
                    });

                LogInfo(logger, "[DbInitializer] Đã đồng bộ hoàn tất 9 Phòng ban, 14 Vai trò phân cấp, 9 tài khoản mới (ad1..ad4, sale01/02, mkt01, hr01, data01) và AdminDelegationScopes.");
            }
            catch (Exception ex)
            {
                LogWarning(logger, $"[DbInitializer] Cảnh báo khi seed Delegated RBAC: {ex.Message}");
            }
        }
    }
}
