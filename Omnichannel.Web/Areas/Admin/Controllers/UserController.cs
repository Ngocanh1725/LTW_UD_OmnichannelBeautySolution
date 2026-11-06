using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.Common;
using Omnichannel.Application.DTOs.HR;
using Omnichannel.Application.DTOs.Security;
using Omnichannel.Application.Interfaces.HR;
using Omnichannel.Application.Interfaces.Security;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Security;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStaffManagementService _staffService;
        private readonly IDelegationService _delegationService;
        private readonly IPermissionService _permissionService;

        public UserController(
            ApplicationDbContext context,
            IStaffManagementService staffService,
            IDelegationService delegationService,
            IPermissionService permissionService)
        {
            _context = context;
            _staffService = staffService;
            _delegationService = delegationService;
            _permissionService = permissionService;
        }

        // =========================================================================
        // 1. DANH SÁCH NHÂN SỰ NỘI BỘ (UserType = 2) - LỌC, PHÂN TRANG & HTMX
        // =========================================================================
        [HttpGet]
        [HasPermission("USER_VIEW")]
        public async Task<IActionResult> Index([FromQuery] StaffFilterQuery query)
        {
            int actorId = GetCurrentActorId();
            var actorCtx = await _delegationService.GetContextAsync(actorId);

            var paged = await _staffService.GetPagedAsync(actorId, query);

            if (Request.Headers["HX-Request"] == "true")
            {
                return PartialView("_StaffTable", paged);
            }

            var stats = await _staffService.GetStatsAsync(actorId);
            var metadata = await LoadFilterMetadataAsync(actorCtx);

            int customerCount = await _context.Users.CountAsync(u => u.UserType == 1 && !u.IsDeleted);
            int adminCount = stats.StaffByRole
                .Where(kv => kv.Key.Contains("Admin") || kv.Key.Contains("Quản trị") || kv.Key.Contains("Giám đốc") || kv.Key.Contains("Trưởng phòng"))
                .Sum(kv => kv.Value);

            var vm = new StaffIndexViewModel
            {
                StaffPaged = paged,
                Stats = stats,
                Filter = query,
                ScopedDepartments = metadata.Departments,
                AssignableRoles = metadata.Roles,
                ScopedStores = metadata.Stores,
                PotentialManagers = metadata.Managers,
                ActorContext = actorCtx,
                CustomerCount = customerCount,
                TotalAdmins = adminCount,
                TotalDepartments = stats.StaffByDepartment.Count(k => k.Key != "Chưa phân phòng")
            };

            return View(vm);
        }

        // =========================================================================
        // 2. DANH SÁCH KHÁCH HÀNG (UserType = 1) - XEM & KHÓA/MỞ KHÓA
        // =========================================================================
        [HttpGet]
        [HasPermission("CUSTOMER_VIEW")]
        public async Task<IActionResult> Customers([FromQuery] CustomerUserFilterQuery query)
        {
            var paged = await _staffService.GetCustomersPagedAsync(query);

            if (Request.Headers["HX-Request"] == "true")
            {
                return PartialView("_CustomerTable", paged);
            }

            int totalCust = await _context.Users.CountAsync(u => u.UserType == 1 && !u.IsDeleted);
            int activeCust = await _context.Users.CountAsync(u => u.UserType == 1 && !u.IsDeleted && u.IsActive);
            int lockedCust = totalCust - activeCust;

            var vm = new CustomerUserIndexViewModel
            {
                CustomersPaged = paged,
                Filter = query,
                TotalCustomers = totalCust,
                ActiveCustomers = activeCust,
                LockedCustomers = lockedCust
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("CUSTOMER_VIEW")]
        public async Task<IActionResult> ToggleCustomerStatus(int id)
        {
            int actorId = GetCurrentActorId();
            var result = await _staffService.ToggleCustomerStatusAsync(actorId, id);

            if (IsAjaxRequest())
            {
                return Json(result);
            }

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Customers));
        }

        // =========================================================================
        // 3. XEM CHI TIẾT HỒ SƠ 360 & LỊCH SỬ KIỂM TOÁN CỦA NHÂN SỰ
        // =========================================================================
        [HttpGet]
        [HasPermission("USER_VIEW")]
        public async Task<IActionResult> Details(int id)
        {
            int actorId = GetCurrentActorId();
            var detail = await _staffService.GetDetailAsync(actorId, id);

            if (detail == null)
            {
                return NotFound();
            }

            return View(detail);
        }

        // =========================================================================
        // 4. THÊM MỚI NHÂN SỰ NỘI BỘ (Create)
        // =========================================================================
        [HttpGet]
        [HasPermission("USER_CREATE")]
        public async Task<IActionResult> Create()
        {
            int actorId = GetCurrentActorId();
            var actorCtx = await _delegationService.GetContextAsync(actorId);

            if (!actorCtx.IsSuperAdmin && !actorCtx.CanCreateUser)
            {
                return Forbid();
            }

            var metadata = await LoadFilterMetadataAsync(actorCtx);

            var vm = new StaffFormViewModel
            {
                IsEditMode = false,
                CreateRequest = new CreateStaffRequest
                {
                    HireDate = DateTime.Today
                },
                ScopedDepartments = metadata.Departments,
                AssignableRoles = metadata.Roles,
                ScopedStores = metadata.Stores,
                PotentialManagers = metadata.Managers,
                ActorContext = actorCtx
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("USER_CREATE")]
        public async Task<IActionResult> Create(StaffFormViewModel model)
        {
            int actorId = GetCurrentActorId();
            var actorCtx = await _delegationService.GetContextAsync(actorId);

            if (!actorCtx.IsSuperAdmin && !actorCtx.CanCreateUser)
            {
                return Forbid();
            }

            // Loại bỏ validation errors của UpdateRequest khi đang tạo mới nhân sự
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("UpdateRequest.")).ToList())
            {
                ModelState.Remove(key);
            }

            if (!ModelState.IsValid)
            {
                var metadata = await LoadFilterMetadataAsync(actorCtx);
                model.ScopedDepartments = metadata.Departments;
                model.AssignableRoles = metadata.Roles;
                model.ScopedStores = metadata.Stores;
                model.PotentialManagers = metadata.Managers;
                model.ActorContext = actorCtx;
                return View(model);
            }

            var result = await _staffService.CreateAsync(actorId, model.CreateRequest);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                var metadata = await LoadFilterMetadataAsync(actorCtx);
                model.ScopedDepartments = metadata.Departments;
                model.AssignableRoles = metadata.Roles;
                model.ScopedStores = metadata.Stores;
                model.PotentialManagers = metadata.Managers;
                model.ActorContext = actorCtx;
                return View(model);
            }

            if (IsAjaxRequest())
            {
                return Json(new { success = true, message = result.Message, staffId = result.Data });
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // =========================================================================
        // 5. CHỈNH SỬA HỒ SƠ NHÂN SỰ (Edit)
        // =========================================================================
        [HttpGet]
        [HasPermission("USER_EDIT")]
        public async Task<IActionResult> Edit(int id)
        {
            int actorId = GetCurrentActorId();
            var actorCtx = await _delegationService.GetContextAsync(actorId);

            var detail = await _staffService.GetDetailAsync(actorId, id);
            if (detail == null)
            {
                return NotFound();
            }

            if (!detail.CanActorManage)
            {
                return Forbid();
            }

            var metadata = await LoadFilterMetadataAsync(actorCtx);

            var vm = new StaffFormViewModel
            {
                IsEditMode = true,
                StaffId = id,
                Username = detail.Username,
                UpdateRequest = new UpdateStaffRequest
                {
                    FullName = detail.FullName,
                    Email = detail.Email,
                    PhoneNumber = detail.PhoneNumber,
                    EmployeeCode = detail.EmployeeCode,
                    DepartmentId = detail.DepartmentId,
                    JobTitle = detail.JobTitle,
                    StoreId = detail.StoreId,
                    ManagerId = detail.ManagerId,
                    Gender = detail.Gender,
                    DateOfBirth = detail.DateOfBirth,
                    HireDate = detail.HireDate,
                    Address = detail.Address,
                    Notes = detail.Notes,
                    IsActive = detail.IsActive,
                    RoleIds = detail.Roles.Select(r => r.Id).ToList()
                },
                ScopedDepartments = metadata.Departments,
                AssignableRoles = metadata.Roles,
                ScopedStores = metadata.Stores,
                PotentialManagers = metadata.Managers.Where(m => m.Id != id).ToList(),
                ActorContext = actorCtx
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("USER_EDIT")]
        public async Task<IActionResult> Edit(int id, StaffFormViewModel model)
        {
            int actorId = GetCurrentActorId();
            var actorCtx = await _delegationService.GetContextAsync(actorId);

            // Loại bỏ validation errors của CreateRequest khi đang chỉnh sửa nhân sự
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("CreateRequest.")).ToList())
            {
                ModelState.Remove(key);
            }

            if (!ModelState.IsValid)
            {
                var metadata = await LoadFilterMetadataAsync(actorCtx);
                model.IsEditMode = true;
                model.StaffId = id;
                model.ScopedDepartments = metadata.Departments;
                model.AssignableRoles = metadata.Roles;
                model.ScopedStores = metadata.Stores;
                model.PotentialManagers = metadata.Managers.Where(m => m.Id != id).ToList();
                model.ActorContext = actorCtx;
                return View(model);
            }

            var result = await _staffService.UpdateAsync(actorId, id, model.UpdateRequest);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                var metadata = await LoadFilterMetadataAsync(actorCtx);
                model.IsEditMode = true;
                model.StaffId = id;
                model.ScopedDepartments = metadata.Departments;
                model.AssignableRoles = metadata.Roles;
                model.ScopedStores = metadata.Stores;
                model.PotentialManagers = metadata.Managers.Where(m => m.Id != id).ToList();
                model.ActorContext = actorCtx;
                return View(model);
            }

            if (IsAjaxRequest())
            {
                return Json(new { success = true, message = result.Message });
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // =========================================================================
        // 6. XÓA MỀM (Soft Delete) & THÙNG RÁC (Trash) & KHÔI PHỤC (Restore)
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("USER_DELETE")]
        public async Task<IActionResult> Delete(int id)
        {
            int actorId = GetCurrentActorId();
            var result = await _staffService.SoftDeleteAsync(actorId, id);

            if (IsAjaxRequest())
            {
                return Json(result);
            }

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [HasPermission("USER_DELETE")]
        public async Task<IActionResult> Trash([FromQuery] StaffFilterQuery query)
        {
            int actorId = GetCurrentActorId();
            var actorCtx = await _delegationService.GetContextAsync(actorId);

            var paged = await _staffService.GetTrashPagedAsync(actorId, query);

            if (Request.Headers["HX-Request"] == "true")
            {
                return PartialView("_TrashTable", paged);
            }

            var metadata = await LoadFilterMetadataAsync(actorCtx);

            var vm = new StaffTrashViewModel
            {
                TrashPaged = paged,
                Filter = query,
                ScopedDepartments = metadata.Departments,
                ActorContext = actorCtx
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("USER_DELETE")]
        public async Task<IActionResult> Restore(int id)
        {
            int actorId = GetCurrentActorId();
            var result = await _staffService.RestoreAsync(actorId, id);

            if (IsAjaxRequest())
            {
                return Json(result);
            }

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Trash));
        }

        // =========================================================================
        // 7. KHÓA / MỞ KHÓA TÀI KHOẢN (ToggleStatus)
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("USER_EDIT")]
        public async Task<IActionResult> ToggleStatus(int userId)
        {
            int actorId = GetCurrentActorId();
            var result = await _staffService.ToggleStatusAsync(actorId, userId);

            if (IsAjaxRequest())
            {
                return Json(result);
            }

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================================
        // 8. ĐẶT LẠI MẬT KHẨU (ResetPassword)
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("USER_RESET_PASSWORD")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (request == null || request.StaffId <= 0)
            {
                return Json(OperationResult<string>.Fail("Yêu cầu đặt lại mật khẩu không hợp lệ."));
            }

            int actorId = GetCurrentActorId();
            var result = await _staffService.ResetPasswordAsync(actorId, request);
            return Json(result);
        }

        // =========================================================================
        // 9. THAO TÁC HÀNG LOẠT (Bulk Actions)
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Bulk([FromBody] BulkStaffActionRequest request)
        {
            if (request == null || request.StaffIds.Count == 0)
            {
                return Json(new BulkStaffActionResult { Messages = new List<string> { "Dữ liệu yêu cầu rỗng." } });
            }

            int actorId = GetCurrentActorId();
            var actorCtx = await _delegationService.GetContextAsync(actorId);

            // Kiểm tra quyền theo từng loại hành động
            string action = request.Action.ToLowerInvariant();
            if (action == "delete" && !actorCtx.IsSuperAdmin && !actorCtx.CanDeleteUser)
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
                return Json(new { success = false, message = "Bạn không có quyền xóa hàng loạt nhân sự." });
            }

            if ((action == "lock" || action == "unlock" || action == "assigndepartment" || action == "assignrole") &&
                !actorCtx.IsSuperAdmin && !actorCtx.CanEditUser)
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;
                return Json(new { success = false, message = "Bạn không có quyền chỉnh sửa hàng loạt nhân sự." });
            }

            var result = await _staffService.BulkAsync(actorId, request);
            return Json(new
            {
                success = result.SucceededCount > 0,
                succeededCount = result.SucceededCount,
                failedCount = result.FailedCount,
                totalRequested = result.TotalRequested,
                messages = result.Messages,
                message = $"Đã xử lý {result.SucceededCount}/{result.TotalRequested} nhân sự được chọn."
            });
        }

        // =========================================================================
        // 10. KẾT XUẤT BÁO CÁO (Export Excel / CSV)
        // =========================================================================
        [HttpGet]
        [HasPermission("USER_EXPORT")]
        public async Task<IActionResult> Export([FromQuery] StaffFilterQuery query, [FromQuery] string format = "xlsx")
        {
            int actorId = GetCurrentActorId();
            byte[] fileBytes = await _staffService.ExportAsync(actorId, query, format);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            {
                return File(fileBytes, "text/csv; charset=utf-8", $"Canmake_NhanSu_{timestamp}.csv");
            }

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Canmake_NhanSu_{timestamp}.xlsx");
        }

        // =========================================================================
        // 11. PHÂN QUYỀN CÁ NHÂN (Permissions) - GIỮ NGUYÊN TỪ PHIÊN 5
        // =========================================================================
        [HttpGet]
        [HasPermission("PERMISSION_DELEGATE")]
        public async Task<IActionResult> Permissions(int id)
        {
            var actorId = GetCurrentActorId();
            var actorCtx = await _delegationService.GetContextAsync(actorId);

            if (!actorCtx.IsSuperAdmin && !await _delegationService.CanManageUserAsync(actorId, id))
            {
                return Forbid();
            }

            var targetUser = await _context.Users
                .AsNoTracking()
                .Include(u => u.Department)
                .Include(u => u.Manager)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id && u.UserType >= 2 && !u.IsDeleted);

            if (targetUser == null)
            {
                return NotFound();
            }

            var assignableRoleIds = (await _delegationService.GetAssignableRoleIdsAsync(actorId)).ToHashSet();
            var grantablePermissionIds = (await _delegationService.GetGrantablePermissionIdsAsync(actorId)).ToHashSet();

            var allInternalRoles = await _context.Roles
                .AsNoTracking()
                .Include(r => r.Department)
                .Where(r => r.IsActive && r.Level > 0 && r.Name != "Customer" && r.Name != "CUSTOMER")
                .OrderByDescending(r => r.Level)
                .ThenBy(r => r.Name)
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    DisplayName = !string.IsNullOrWhiteSpace(r.DisplayName) ? r.DisplayName : r.Name,
                    Description = r.Description,
                    Level = r.Level,
                    DepartmentId = r.DepartmentId,
                    DepartmentName = r.Department != null ? r.Department.Name : null,
                    IsSystem = r.IsSystem,
                    Color = r.Color
                })
                .ToListAsync();

            var breakdown = await _permissionService.GetUserPermissionBreakdownAsync(id);
            foreach (var item in breakdown)
            {
                item.CanActorGrant = actorCtx.IsSuperAdmin || grantablePermissionIds.Contains(item.PermissionId);
                item.ModuleDisplayName = TranslateModuleName(item.Module);
            }

            var currentRoleIds = targetUser.UserRoles
                .Where(ur => ur.Role != null && ur.Role.Name != "Customer")
                .Select(ur => ur.RoleId)
                .ToList();

            var vm = new UserPermissionPageViewModel
            {
                TargetUserId = targetUser.Id,
                EmployeeCode = targetUser.EmployeeCode,
                Username = targetUser.Username,
                FullName = targetUser.FullName,
                Email = targetUser.Email,
                JobTitle = targetUser.JobTitle,
                DepartmentName = targetUser.Department?.Name,
                ManagerName = targetUser.Manager?.FullName,
                IsActive = targetUser.IsActive,
                CurrentRoleIds = currentRoleIds,
                CurrentRoles = allInternalRoles.Where(r => currentRoleIds.Contains(r.Id)).ToList(),
                AssignableRoles = allInternalRoles.Where(r => assignableRoleIds.Contains(r.Id) || currentRoleIds.Contains(r.Id)).ToList(),
                CanActorAssignRoles = actorCtx.IsSuperAdmin || actorCtx.CanAssignRole,
                Permissions = breakdown,
                ActorContext = actorCtx
            };

            return View(vm);
        }

        // =========================================================================
        // 12. APIS KIỂM TRA TRÙNG LẶP REALTIME (Validation Endpoints)
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> CheckUsername(string username, int? excludeId = null)
        {
            bool exists = await _staffService.CheckUsernameExistsAsync(username, excludeId);
            return Json(!exists);
        }

        [HttpGet]
        public async Task<IActionResult> CheckEmail(string email, int? excludeId = null)
        {
            bool exists = await _staffService.CheckEmailExistsAsync(email, excludeId);
            return Json(!exists);
        }

        [HttpGet]
        public async Task<IActionResult> CheckEmployeeCode(string code, int? excludeId = null)
        {
            bool exists = await _staffService.CheckEmployeeCodeExistsAsync(code, excludeId);
            return Json(!exists);
        }

        #region Private Helpers

        private int GetCurrentActorId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdStr, out var id) ? id : 0;
        }

        private bool IsAjaxRequest()
        {
            return string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(Request.Headers["Accept"], "application/json", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<(List<DepartmentOptionDto> Departments, List<RoleDto> Roles, List<StoreOptionDto> Stores, List<UserSummaryDto> Managers)> LoadFilterMetadataAsync(DelegationContextDto actorCtx)
        {
            var deptQuery = _context.Departments.AsNoTracking().Where(d => d.IsActive);
            if (!actorCtx.IsSuperAdmin)
            {
                deptQuery = deptQuery.Where(d => actorCtx.ManagedDepartmentIds.Contains(d.Id));
            }
            var rawDepts = await deptQuery
                .OrderBy(d => d.SortOrder)
                .Select(d => new { d.Id, d.Code, d.Name, d.ParentId })
                .ToListAsync();

            // Xây dựng cây phòng ban (Hierarchical Tree) hiển thị thụt đầu dòng
            var departments = new List<DepartmentOptionDto>();
            void AddDeptNodes(int? parentId, int level)
            {
                var children = rawDepts.Where(d => d.ParentId == parentId).ToList();
                foreach (var c in children)
                {
                    string indent = level switch
                    {
                        0 => "",
                        1 => "— ",
                        2 => "— — ",
                        _ => new string('—', level) + " "
                    };
                    departments.Add(new DepartmentOptionDto
                    {
                        Id = c.Id,
                        Code = c.Code,
                        Name = c.Name,
                        ParentId = c.ParentId,
                        Level = level,
                        DisplayTreeName = indent + c.Name
                    });
                    AddDeptNodes(c.Id, level + 1);
                }
            }
            AddDeptNodes(null, 0);
            foreach (var rem in rawDepts.Where(d => !departments.Any(x => x.Id == d.Id)))
            {
                departments.Add(new DepartmentOptionDto
                {
                    Id = rem.Id,
                    Code = rem.Code,
                    Name = rem.Name,
                    ParentId = rem.ParentId,
                    Level = 0,
                    DisplayTreeName = rem.Name
                });
            }

            var assignableRoleIds = (await _delegationService.GetAssignableRoleIdsAsync(actorCtx.ActorId)).ToHashSet();
            var roles = await _context.Roles
                .AsNoTracking()
                .Where(r => r.IsActive && r.Level > 0 && r.Name != "Customer" && r.Name != "CUSTOMER")
                .Where(r => actorCtx.IsSuperAdmin || assignableRoleIds.Contains(r.Id))
                .OrderByDescending(r => r.Level)
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    DisplayName = r.DisplayName ?? r.Name,
                    Description = r.Description,
                    Level = r.Level,
                    Color = r.Color
                })
                .ToListAsync();

            var storeQuery = _context.Stores.AsNoTracking().Where(s => s.IsActive);
            if (!actorCtx.IsSuperAdmin)
            {
                storeQuery = storeQuery.Where(s => actorCtx.ManagedStoreIds.Contains(s.Id));
            }
            var stores = await storeQuery
                .OrderBy(s => s.Id)
                .Select(s => new StoreOptionDto { Id = s.Id, Code = s.Code, Name = s.Name, IsHeadquarters = s.Code == "STORE_HQ" })
                .ToListAsync();

            var managerQuery = _context.Users
                .AsNoTracking()
                .Include(u => u.Department)
                .Where(u => u.UserType >= 2 && !u.IsDeleted && u.IsActive);
            managerQuery = _delegationService.ApplyUserScope(managerQuery, actorCtx);
            var managers = await managerQuery
                .OrderBy(u => u.FullName)
                .Select(u => new UserSummaryDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Username = u.Username,
                    EmployeeCode = u.EmployeeCode,
                    JobTitle = u.JobTitle,
                    DepartmentId = u.DepartmentId,
                    DepartmentName = u.Department != null ? u.Department.Name : null
                })
                .ToListAsync();

            return (departments, roles, stores, managers);
        }

        private static string TranslateModuleName(string moduleCode)
        {
            return moduleCode switch
            {
                "DASHBOARD" => "Bảng Điều Khiển & Báo Cáo",
                "POS" => "Bán Hàng Tại Quầy (POS)",
                "ORDER" => "Đơn Hàng Đa Kênh (OMS)",
                "PRODUCT" => "Sản Phẩm & Danh Mục",
                "INVENTORY" => "Kho Hàng & Lô Hạn FEFO",
                "CUSTOMER" => "Khách Hàng & Hồ Sơ CRM",
                "PROMOTION" => "Khuyến Mãi & Loyalty",
                "CMS" => "Giao Diện, Menu & Nội Dung (CMS)",
                "FINANCE" => "Tài Chính & Đối Soát Lãi Lỗ",
                "USER" => "Nhân Sự, Phòng Ban & Phân Quyền",
                "SYSTEM" => "Cấu Hình Hệ Thống & Bảo Mật",
                _ => moduleCode
            };
        }

        #endregion
    }
}
