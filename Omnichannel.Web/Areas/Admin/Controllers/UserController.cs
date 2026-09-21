using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Account;
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

        public UserController(
            ApplicationDbContext context,
            IPasswordHasherService passwordHasher,
            IPermissionService permissionService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _permissionService = permissionService;
        }

        [HttpGet]
        [HasPermission("HR_USER", "VIEW")]
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .AsNoTracking()
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new UserItemViewModel
                {
                    UserId = u.Id.ToString(),
                    Username = u.Username,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    UserType = u.UserType,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    AssignedRoles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
                })
                .ToListAsync();

            return View(users);
        }

        [HttpGet]
        [HasPermission("HR_USER", "CREATE")]
        public async Task<IActionResult> Create()
        {
            var roles = await _context.Roles.AsNoTracking().ToListAsync();
            ViewBag.Roles = new SelectList(roles, "RoleId", "RoleName");
            return View(new CreateUserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("HR_USER", "CREATE")]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var roles = await _context.Roles.AsNoTracking().ToListAsync();
                ViewBag.Roles = new SelectList(roles, "RoleId", "RoleName");
                return View(model);
            }

            string normalized = model.Username.Trim().ToUpperInvariant();

            // Kiểm tra trùng username hoặc email
            if (await _context.Users.AnyAsync(u => u.Username.ToUpper() == normalized))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập này đã tồn tại.");
                var roles = await _context.Roles.AsNoTracking().ToListAsync();
                ViewBag.Roles = new SelectList(roles, "Id", "Name");
                return View(model);
            }

            if (await _context.Users.AnyAsync(u => u.Email.ToUpper() == model.Email.Trim().ToUpper()))
            {
                ModelState.AddModelError("Email", "Địa chỉ Email này đã được sử dụng.");
                var roles = await _context.Roles.AsNoTracking().ToListAsync();
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
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(newUser);

            // Gán vai trò ban đầu
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
        [HasPermission("HR_USER", "LOCK_ACCOUNT")]
        public async Task<IActionResult> ToggleStatus(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Không cho phép tự khóa tài khoản của chính mình
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (user.Id.ToString() == currentUserId)
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
    }
}
