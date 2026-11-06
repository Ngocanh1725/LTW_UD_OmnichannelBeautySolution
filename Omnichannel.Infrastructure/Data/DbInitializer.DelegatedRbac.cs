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

                // Gán ParentId (cây phòng ban trực thuộc BAN_GIAM_DOC)
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

                // =========================================================================
                // 3. CẬP NHẬT LEVEL, DISPLAYNAME, ISSYSTEM, DEPARTMENT CHO 6 ROLE CỐT LÕI
                // =========================================================================
                var rolesList = await context.Roles.IgnoreQueryFilters().ToListAsync();
                var roleMetaMap = new Dictionary<string, (int Level, string DisplayName, string Color, string? DeptCode)>(StringComparer.OrdinalIgnoreCase)
                {
                    ["SuperAdmin"] = (100, "Quản Trị Viên Tối Cao (Admin Tổng)", "purple", "BAN_GIAM_DOC"),
                    ["StoreManager"] = (30, "Cửa Hàng Trưởng (Quản Lý Chi Nhánh)", "blue", "CUA_HANG_BAN_LE"),
                    ["Cashier"] = (10, "Thu Ngân Điểm Bán (POS)", "pink", "CUA_HANG_BAN_LE"),
                    ["WarehouseStaff"] = (10, "Nhân Viên Kho & Kiểm Soát FEFO", "amber", "PHONG_KHO_VAN"),
                    ["ContentEditor"] = (10, "Biên Tập Viên Nội Dung", "emerald", "PHONG_GIAO_DIEN_CMS"),
                    ["Customer"] = (0, "Khách Hàng Thành Viên", "slate", null)
                };

                bool roleUpdated = false;
                foreach (var r in rolesList)
                {
                    if (roleMetaMap.TryGetValue(r.Name, out var meta))
                    {
                        int? targetDeptId = meta.DeptCode != null && deptsByCode.TryGetValue(meta.DeptCode, out var dObj) ? dObj.Id : null;
                        if (r.Level != meta.Level || r.DisplayName != meta.DisplayName || !r.IsSystem || r.Color != meta.Color || r.DepartmentId != targetDeptId)
                        {
                            r.Level = meta.Level;
                            r.DisplayName = meta.DisplayName;
                            r.IsSystem = true;
                            r.Color = meta.Color;
                            r.DepartmentId = targetDeptId;
                            r.UpdatedAt = DateTime.UtcNow;
                            roleUpdated = true;
                        }
                    }
                }

                if (roleUpdated)
                {
                    await context.SaveChangesAsync();
                }

                // =========================================================================
                // 4. ĐẢM BẢO SUPERADMIN CÓ ĐẦY ĐỦ 100% QUYỀN HẠT NHÂN
                // =========================================================================
                var superRole = rolesList.FirstOrDefault(r => r.Name.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase));
                if (superRole != null)
                {
                    var allPermIds = await context.Permissions.Select(p => p.Id).ToListAsync();
                    var superPermIds = await context.RolePermissions
                        .Where(rp => rp.RoleId == superRole.Id)
                        .Select(rp => rp.PermissionId)
                        .ToListAsync();
                    var superPermSet = new HashSet<int>(superPermIds);

                    bool superPermAdded = false;
                    foreach (var pid in allPermIds)
                    {
                        if (!superPermSet.Contains(pid))
                        {
                            await context.RolePermissions.AddAsync(new RolePermission
                            {
                                RoleId = superRole.Id,
                                PermissionId = pid
                            });
                            superPermAdded = true;
                        }
                    }

                    if (superPermAdded)
                    {
                        await context.SaveChangesAsync();
                    }
                }

                // =========================================================================
                // 5. CẬP NHẬT PHÒNG BAN, MÃ NHÂN VIÊN, CHỨC DANH CHO TÀI KHOẢN NỘI BỘ CŨ
                // =========================================================================
                var staffUsers = await context.Users
                    .IgnoreQueryFilters()
                    .Where(u => u.UserType >= 2)
                    .OrderBy(u => u.Id)
                    .ToListAsync();

                var superAdminUser = staffUsers.FirstOrDefault(u => u.Username.Equals("superadmin", StringComparison.OrdinalIgnoreCase))
                                     ?? staffUsers.FirstOrDefault();
                var managerHqUser = staffUsers.FirstOrDefault(u => u.Username.Equals("manager_hq", StringComparison.OrdinalIgnoreCase));
                var managerCgUser = staffUsers.FirstOrDefault(u => u.Username.Equals("manager_cg", StringComparison.OrdinalIgnoreCase));

                bool staffUpdated = false;
                int empSeq = 1;

                foreach (var u in staffUsers)
                {
                    string expectedCode = $"NV{empSeq:D4}";
                    empSeq++;

                    if (string.IsNullOrWhiteSpace(u.EmployeeCode))
                    {
                        u.EmployeeCode = expectedCode;
                        staffUpdated = true;
                    }

                    if (!u.HireDate.HasValue)
                    {
                        u.HireDate = new DateTime(2025, 1, 15);
                        staffUpdated = true;
                    }

                    string uname = u.Username.ToLowerInvariant();
                    string targetDeptCode = uname switch
                    {
                        "superadmin" or "admin" => "BAN_GIAM_DOC",
                        "editor01" => "PHONG_GIAO_DIEN_CMS",
                        "warehouse01" or "warehouse02" => "PHONG_KHO_VAN",
                        "manager_hq" or "manager_cg" or "cashier01" or "cashier02" => "CUA_HANG_BAN_LE",
                        _ => "CUA_HANG_BAN_LE"
                    };

                    if (!u.DepartmentId.HasValue && deptsByCode.TryGetValue(targetDeptCode, out var deptEntity))
                    {
                        u.DepartmentId = deptEntity.Id;
                        staffUpdated = true;
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
                            "warehouse02" => "Chuyên Viên Kho Chi Nhánh Cầu Giấy",
                            "editor01" => "Biên Tập Viên Nội Dung & Giao Diện CMS",
                            _ => "Chuyên Viên Nội Bộ"
                        };
                        staffUpdated = true;
                    }

                    if (!u.ManagerId.HasValue && superAdminUser != null && u.Id != superAdminUser.Id)
                    {
                        if (uname is "cashier01" or "warehouse01" && managerHqUser != null)
                        {
                            u.ManagerId = managerHqUser.Id;
                        }
                        else if (uname is "cashier02" or "warehouse02" && managerCgUser != null)
                        {
                            u.ManagerId = managerCgUser.Id;
                        }
                        else
                        {
                            u.ManagerId = superAdminUser.Id;
                        }
                        staffUpdated = true;
                    }
                }

                if (staffUpdated)
                {
                    await context.SaveChangesAsync();
                }

                // Gán HeadUserId cho BAN_GIAM_DOC nếu chưa có
                if (superAdminUser != null && deptsByCode.TryGetValue("BAN_GIAM_DOC", out var bgd) && !bgd.HeadUserId.HasValue)
                {
                    bgd.HeadUserId = superAdminUser.Id;
                    await context.SaveChangesAsync();
                }

                LogInfo(logger, "[DbInitializer] Đã đồng bộ Schema & Seed Phòng ban, Cấp bậc Role và Quyền Ủy quyền (Phiên 2).");
            }
            catch (Exception ex)
            {
                LogError(logger, ex, "[DbInitializer] Lỗi khi seed dữ liệu Delegated RBAC & Departments.");
                throw;
            }
        }
    }
}

