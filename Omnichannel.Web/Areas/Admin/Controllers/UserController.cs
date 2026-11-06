using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Account;
using Omnichannel.Application.DTOs.Security;
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
        private readonly IPasswordHasherService _passwordHasher;
        private readonly IPermissionService _permissionService;
        private readonly IDelegationService _delegationService;

        public UserController(
            ApplicationDbContext context,
            IPasswordHasherService passwordHasher,
            IPermissionService permissionService,
            IDelegationService delegationService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _permissionService = permissionService;
            _delegationService = delegationService;
        }

        [HttpGet]
        [HasPermission("USER_VIEW")]
        public async Task<IActionResult> Index(int? departmentId = null)
        {
            var actorId = GetCurrentActorId();
            var actorCtx = await _delegationService.GetContextAsync(actorId);

            var query = _context.Users
                .AsNoTracking()
                .Include(u => u.Department)
                .Include(u => u.Manager)
                .Include(u => u.Store)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => u.UserType >= 2 && !u.IsDeleted);

            query = _delegationService.ApplyUserScope(query, actorCtx);

            if (departmentId.HasValue)
            {
                query = query.Where(u => u.DepartmentId == departmentId.Value);
            }

            var entities = await query
                .OrderBy(u => u.EmployeeCode ?? "ZZZZ")
                .ThenBy(u => u.FullName)
                .ToListAsync();

            var users = new List<UserItemViewModel>();
            foreach (var u in entities)
            {
                bool canManage = actorCtx.IsSuperAdmin || await _delegationService.CanManageUserAsync(actorId, u.Id);
                users.Add(new UserItemViewModel
                {
                    UserId = u.Id.ToString(),
                    EmployeeCode = u.EmployeeCode,
                    Username = u.Username,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    JobTitle = u.JobTitle,
                    DepartmentId = u.DepartmentId,
                    DepartmentName = u.Department?.Name,
                    ManagerName = u.Manager?.FullName,
                    StoreName = u.Store?.Name,
                    UserType = u.UserType,
                    IsActive = u.IsActive,
                    CanActorManage = canManage,
                    CreatedAt = u.CreatedAt,
                    AssignedRoles = u.UserRoles
                        .Where(ur => ur.Role != null && ur.Role.Name != "Customer")
                        .OrderByDescending(ur => ur.Role.Level)
                        .Select(ur => !string.IsNullOrWhiteSpace(ur.Role.DisplayName) ? ur.Role.DisplayName! : ur.Role.Name)
                        .ToList()
                });
            }

            ViewBag.ActorContext = actorCtx;
            ViewBag.SelectedDepartmentId = departmentId;
            ViewBag.Departments = await _context.Departments
                .AsNoTracking()
                .Where(d => d.IsActive && (actorCtx.IsSuperAdmin || actorCtx.ManagedDepartmentIds.Contains(d.Id)))
                .OrderBy(d => d.SortOrder)
                .ToListAsync();

            return View(users);
        }

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

        [HttpGet]
        [HasPermission("USER_CREATE")]
        public async Task<IActionResult> Create()
        {
            var roles = await _context.Roles
                .AsNoTracking()
                .Where(r => r.IsActive && r.Level > 0 && r.Name != "Customer" && r.Name != "CUSTOMER")
                .ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Id", "Name");
            return View(new CreateUserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("USER_CREATE")]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var roles = await _context.Roles
                    .AsNoTracking()
                    .Where(r => r.IsActive && r.Level > 0 && r.Name != "Customer" && r.Name != "CUSTOMER")
                    .ToListAsync();
                ViewBag.Roles = new SelectList(roles, "Id", "Name");
                return View(model);
            }

            string normalized = model.Username.Trim().ToUpperInvariant();

            if (await _context.Users.AnyAsync(u => u.Username.ToUpper() == normalized))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập này đã tồn tại.");
                var roles = await _context.Roles
                    .AsNoTracking()
                    .Where(r => r.IsActive && r.Level > 0 && r.Name != "Customer" && r.Name != "CUSTOMER")
                    .ToListAsync();
                ViewBag.Roles = new SelectList(roles, "Id", "Name");
                return View(model);
            }

            if (await _context.Users.AnyAsync(u => u.Email.ToUpper() == model.Email.Trim().ToUpper()))
            {
                ModelState.AddModelError("Email", "Địa chỉ Email này đã được sử dụng.");
                var roles = await _context.Roles
                    .AsNoTracking()
                    .Where(r => r.IsActive && r.Level > 0 && r.Name != "Customer" && r.Name != "CUSTOMER")
                    .ToListAsync();
                ViewBag.Roles = new SelectList(roles, "Id", "Name");
                return View(model);
            }

            var newUser = new User
            {
                Username = model.Username.Trim(),
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim().ToLowerInvariant(),
                PhoneNumber = model.PhoneNumber?.Trim(),
                PasswordHash = _passwordHasher.HashPassword(model.Password),
                UserType = 2,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(newUser);

            if (!string.IsNullOrEmpty(model.RoleId) && int.TryParse(model.RoleId, out var roleIdInt))
            {
                await _context.UserRoles.AddAsync(new UserRole
                {
                    UserId = newUser.Id,
                    RoleId = roleIdInt,
                    AssignedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã tạo mới nhân sự '{newUser.FullName}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("USER_EDIT")]
        public async Task<IActionResult> ToggleStatus(int userId)
        {
            var actorId = GetCurrentActorId();
            var actorCtx = await _delegationService.GetContextAsync(actorId);
            if (!actorCtx.IsSuperAdmin && !await _delegationService.CanManageUserAsync(actorId, userId))
            {
                return Forbid();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            if (user.Id == actorId)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự khóa tài khoản đang đăng nhập!";
                return RedirectToAction(nameof(Index));
            }

            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _permissionService.InvalidateUserPermissionCacheAsync(user.Id);

            TempData["SuccessMessage"] = user.IsActive
                ? $"Đã mở khóa tài khoản {user.Username}."
                : $"Đã tạm khóa tài khoản {user.Username}.";

            return RedirectToAction(nameof(Index));
        }

        private int GetCurrentActorId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdStr, out var id) ? id : 0;
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
    }
}
