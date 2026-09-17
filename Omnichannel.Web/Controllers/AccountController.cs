using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Account;
using Omnichannel.Application.Interfaces.Security;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IPasswordHasherService _passwordHasher;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            ApplicationDbContext dbContext,
            IPasswordHasherService passwordHasher,
            ILogger<AccountController> logger)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToLocal(returnUrl);
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string cleanInput = model.Username.Trim();
            string normalized = cleanInput.ToUpperInvariant();

            // Tìm user theo Username hoặc Email
            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.NormalizedUsername == normalized || u.Email.ToUpper() == normalized);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không chính xác.");
                return View(model);
            }

            // Kiểm tra trạng thái khóa tài khoản
            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản của bạn đang bị tạm khóa. Vui lòng liên hệ quản trị viên.");
                return View(model);
            }

            // Kiểm tra mật khẩu băm
            bool isPasswordValid = _passwordHasher.VerifyPassword(user.PasswordHash, model.Password);
            if (!isPasswordValid)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không chính xác.");
                return View(model);
            }

            // Thiết lập danh sách Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.GivenName, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("UserType", user.UserType.ToString())
            };

            // Nạp các vai trò vào Claims
            foreach (var userRole in user.UserRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole.RoleId));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(14) : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogInformation("Người dùng {Username} ({UserId}) đã đăng nhập thành công.", user.Username, user.UserId);

            return RedirectToLocal(model.ReturnUrl, user.UserType);
        }

        // POST: /Account/Logout
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            string username = User.Identity?.Name ?? "Ẩn danh";
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("Người dùng {Username} đã đăng xuất khỏi hệ thống.", username);

            return RedirectToAction(nameof(Login));
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied(string? returnUrl = null)
        {
            var vm = new AccessDeniedViewModel
            {
                AttemptedPath = returnUrl ?? Request.Headers["Referer"].ToString(),
                Message = "Bạn không có quyền hạn hạt nhân (Permission) để truy cập vào tài nguyên hoặc thực hiện hành động này."
            };

            return View(vm);
        }

        private IActionResult RedirectToLocal(string? returnUrl, int userType = 1)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Nếu là tài khoản Quản trị/Nhân sự (UserType >= 2) -> Điều hướng vào Admin
            if (userType >= 2)
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            return RedirectToAction("Index", "Home");
        }
    }
}