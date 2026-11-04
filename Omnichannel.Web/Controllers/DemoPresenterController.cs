using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.Common;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Application.Interfaces.Security;
using Omnichannel.Application.Interfaces.Settings;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Web.Controllers
{
    [ApiController]
    [Route("api/demo")]
    [IgnoreAntiforgeryToken]
    public class DemoPresenterController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;
        private readonly ICartService _cartService;
        private readonly IDemoResetService _demoResetService;
        private readonly IAuditLogService? _auditLogService;
        private readonly ILogger<DemoPresenterController> _logger;

        private const string CartCookieKey = "OmnichannelBeauty_CartId";

        public DemoPresenterController(
            IConfiguration configuration,
            IWebHostEnvironment env,
            ApplicationDbContext context,
            ICartService cartService,
            IDemoResetService demoResetService,
            ILogger<DemoPresenterController> logger,
            IAuditLogService? auditLogService = null)
        {
            _configuration = configuration;
            _env = env;
            _context = context;
            _cartService = cartService;
            _demoResetService = demoResetService;
            _logger = logger;
            _auditLogService = auditLogService;
        }

        private bool IsDemoModeActive()
        {
            bool isConfigEnabled = _configuration.GetValue<bool>("DemoMode:Enabled");
            // Chỉ hoạt động khi cấu hình DemoMode:Enabled = true VÀ (trong môi trường Development HOẶC cấu hình cho phép)
            return isConfigEnabled;
        }

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            if (!IsDemoModeActive())
            {
                return NotFound(new { message = "Chế độ Demo không khả dụng ở môi trường hiện tại." });
            }

            string currentIdentity = User.Identity?.Name ?? "Khách vãng lai";
            string currentRole = "Guest";
            if (User.IsInRole("SuperAdmin") || User.IsInRole("SUPER_ADMIN")) currentRole = "SuperAdmin";
            else if (User.IsInRole("CASHIER") || User.HasClaim("UserType", "3")) currentRole = "Cashier";
            else if (User.Identity?.IsAuthenticated == true) currentRole = "Customer";

            return Ok(new
            {
                demoMode = true,
                environment = _env.EnvironmentName,
                user = currentIdentity,
                role = currentRole
            });
        }

        [HttpPost("quick-switch-persona")]
        public async Task<IActionResult> QuickSwitchPersona([FromBody] QuickSwitchPersonaRequestDto request)
        {
            if (!IsDemoModeActive())
            {
                return NotFound(new { message = "Chế độ Demo không khả dụng." });
            }

            string targetRole = (request?.Persona ?? "customer").Trim().ToLowerInvariant();

            if (targetRole == "customer")
            {
                var customer = await _context.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Username == "customer_vip" || u.Username == "customer01" || u.UserType == 1);

                if (customer == null)
                {
                    return BadRequest(new { success = false, message = "Không tìm thấy tài khoản khách hàng mẫu trong CSDL." });
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString()),
                    new Claim(ClaimTypes.Name, customer.Username),
                    new Claim(ClaimTypes.GivenName, customer.FullName ?? "Lê Ngọc Linh (VIP)"),
                    new Claim(ClaimTypes.Email, customer.Email ?? "customer.vip@gmail.com"),
                    new Claim(ClaimTypes.Role, "CUSTOMER"),
                    new Claim("UserType", "1")
                };

                var identity = new ClaimsIdentity(claims, AuthConstants.CustomerCookieScheme);
                await HttpContext.SignInAsync(
                    AuthConstants.CustomerCookieScheme,
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(4) });

                if (_auditLogService != null)
                {
                    await _auditLogService.LogAsync(
                        action: "DEMO_QUICK_SWITCH_PERSONA",
                        entityType: "User",
                        entityId: customer.Id.ToString(),
                        oldValues: null,
                        newValues: $"Chuyển nhanh sang vai trò Khách hàng mẫu: {customer.Username} ({customer.FullName})",
                        userName: "DemoPresenterHUD");
                }

                return Ok(new
                {
                    success = true,
                    persona = "customer",
                    username = customer.Username,
                    displayName = customer.FullName ?? "Lê Ngọc Linh (VIP)",
                    redirectUrl = "/"
                });
            }
            else if (targetRole == "cashier")
            {
                var cashier = await _context.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Username == "cashier01" || u.UserType == 3);

                if (cashier == null)
                {
                    return BadRequest(new { success = false, message = "Không tìm thấy tài khoản thu ngân mẫu trong CSDL." });
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, cashier.Id.ToString()),
                    new Claim(ClaimTypes.Name, cashier.Username),
                    new Claim(ClaimTypes.GivenName, cashier.FullName ?? "Thu Ngân Showroom 01"),
                    new Claim(ClaimTypes.Email, cashier.Email ?? "cashier01@omnichannelbeauty.vn"),
                    new Claim(ClaimTypes.Role, "CASHIER"),
                    new Claim("UserType", cashier.UserType.ToString()),
                    new Claim("IsStaff", "true"),
                    new Claim("StoreId", cashier.StoreId?.ToString() ?? "1")
                };

                var identity = new ClaimsIdentity(claims, AuthConstants.AdminCookieScheme);
                await HttpContext.SignInAsync(
                    AuthConstants.AdminCookieScheme,
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(4) });

                if (_auditLogService != null)
                {
                    await _auditLogService.LogAsync(
                        action: "DEMO_QUICK_SWITCH_PERSONA",
                        entityType: "User",
                        entityId: cashier.Id.ToString(),
                        oldValues: null,
                        newValues: $"Chuyển nhanh sang vai trò Thu ngân POS: {cashier.Username} ({cashier.FullName})",
                        userName: "DemoPresenterHUD");
                }

                return Ok(new
                {
                    success = true,
                    persona = "cashier",
                    username = cashier.Username,
                    displayName = cashier.FullName ?? "Thu Ngân Showroom 01",
                    redirectUrl = "/Admin/Pos"
                });
            }
            else if (targetRole == "superadmin")
            {
                var admin = await _context.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Username == "superadmin" || u.Username == "admin");

                if (admin == null)
                {
                    return BadRequest(new { success = false, message = "Không tìm thấy tài khoản SuperAdmin mẫu trong CSDL." });
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
                    new Claim(ClaimTypes.Name, admin.Username),
                    new Claim(ClaimTypes.GivenName, admin.FullName ?? "Nguyễn Quản Trị (SuperAdmin)"),
                    new Claim(ClaimTypes.Email, admin.Email ?? "superadmin@omnichannelbeauty.vn"),
                    new Claim(ClaimTypes.Role, "SuperAdmin"),
                    new Claim(ClaimTypes.Role, "SUPER_ADMIN"),
                    new Claim("UserType", admin.UserType.ToString()),
                    new Claim("IsStaff", "true")
                };

                var identity = new ClaimsIdentity(claims, AuthConstants.AdminCookieScheme);
                await HttpContext.SignInAsync(
                    AuthConstants.AdminCookieScheme,
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(4) });

                if (_auditLogService != null)
                {
                    await _auditLogService.LogAsync(
                        action: "DEMO_QUICK_SWITCH_PERSONA",
                        entityType: "User",
                        entityId: admin.Id.ToString(),
                        oldValues: null,
                        newValues: $"Chuyển nhanh sang vai trò Quản trị viên tối cao SuperAdmin: {admin.Username}",
                        userName: "DemoPresenterHUD");
                }

                return Ok(new
                {
                    success = true,
                    persona = "superadmin",
                    username = admin.Username,
                    displayName = admin.FullName ?? "SuperAdmin",
                    redirectUrl = "/Admin/Dashboard"
                });
            }

            return BadRequest(new { success = false, message = $"Vai trò '{targetRole}' không được hỗ trợ." });
        }

        [HttpPost("seed-cart-routine")]
        public async Task<IActionResult> SeedCartRoutine(CancellationToken cancellationToken)
        {
            if (!IsDemoModeActive())
            {
                return NotFound(new { message = "Chế độ Demo không khả dụng." });
            }

            string cartId = GetOrCreateCartSessionId();
            string? userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Tìm 3 sản phẩm đại diện Routine 3 bước (Sữa rửa mặt + BHA / Serum phục hồi + Kem B5)
            var activeProducts = await _context.Products.AsNoTracking()
                .Include(p => p.Batches)
                .Where(p => p.IsActive)
                .ToListAsync(cancellationToken);

            var cleanser = activeProducts.FirstOrDefault(p =>
                p.Name.Contains("Cetaphil", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("CeraVe", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("Sữa rửa mặt", StringComparison.OrdinalIgnoreCase));

            var bhaOrSerum = activeProducts.FirstOrDefault(p =>
                p.Name.Contains("BHA", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("Salicylic", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("Serum", StringComparison.OrdinalIgnoreCase));

            var b5Cream = activeProducts.FirstOrDefault(p =>
                p.Name.Contains("B5", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("Cicaplast", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("Kem dưỡng", StringComparison.OrdinalIgnoreCase));

            // Fallback lấy bất kỳ 3 sản phẩm active khác nhau nếu không khớp tên
            var productsToSeed = new List<Product>();
            if (cleanser != null) productsToSeed.Add(cleanser);
            if (bhaOrSerum != null && !productsToSeed.Any(p => p.Id == bhaOrSerum.Id)) productsToSeed.Add(bhaOrSerum);
            if (b5Cream != null && !productsToSeed.Any(p => p.Id == b5Cream.Id)) productsToSeed.Add(b5Cream);

            foreach (var p in activeProducts)
            {
                if (productsToSeed.Count >= 3) break;
                if (!productsToSeed.Any(x => x.Id == p.Id)) productsToSeed.Add(p);
            }

            int addedCount = 0;
            foreach (var prod in productsToSeed)
            {
                var validBatch = prod.Batches.FirstOrDefault(b => b.CurrentQuantity > 0);
                await _cartService.AddToCartAsync(cartId, userIdStr, prod.Id, validBatch?.Id, 1, cancellationToken);
                addedCount++;
            }

            return Ok(new
            {
                success = true,
                count = addedCount,
                message = $"Đã nạp combo Routine {addedCount} bước (Sữa rửa mặt + BHA + Kem B5) vào giỏ hàng!",
                cartUrl = "/cart"
            });
        }

        [HttpPost("seed-cart-conflict")]
        public async Task<IActionResult> SeedCartConflict(CancellationToken cancellationToken)
        {
            if (!IsDemoModeActive())
            {
                return NotFound(new { message = "Chế độ Demo không khả dụng." });
            }

            string cartId = GetOrCreateCartSessionId();
            string? userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var activeProducts = await _context.Products.AsNoTracking()
                .Include(p => p.Batches)
                .Where(p => p.IsActive)
                .ToListAsync(cancellationToken);

            // Tìm 1 sản phẩm có Retinol và 1 sản phẩm có BHA (hoặc Salicylic Acid)
            var retinolProd = activeProducts.FirstOrDefault(p =>
                p.Name.Contains("Retinol", StringComparison.OrdinalIgnoreCase) ||
                (p.FullIngredients != null && p.FullIngredients.Contains("Retinol", StringComparison.OrdinalIgnoreCase)));

            var bhaProd = activeProducts.FirstOrDefault(p =>
                p.Name.Contains("BHA", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("Salicylic", StringComparison.OrdinalIgnoreCase) ||
                (p.FullIngredients != null && p.FullIngredients.Contains("Salicylic Acid", StringComparison.OrdinalIgnoreCase)));

            var conflictList = new List<Product>();
            if (retinolProd != null) conflictList.Add(retinolProd);
            if (bhaProd != null && !conflictList.Any(p => p.Id == bhaProd.Id)) conflictList.Add(bhaProd);

            // Fallback lấy 2 sản phẩm đầu tiên nếu thiếu hoạt chất
            if (conflictList.Count < 2)
            {
                conflictList = activeProducts.Take(2).ToList();
            }

            int added = 0;
            foreach (var p in conflictList)
            {
                var batch = p.Batches.FirstOrDefault(b => b.CurrentQuantity > 0);
                await _cartService.AddToCartAsync(cartId, userIdStr, p.Id, batch?.Id, 1, cancellationToken);
                added++;
            }

            return Ok(new
            {
                success = true,
                count = added,
                message = "Đã nạp 2 sản phẩm Retinol 1% & BHA 2% vào giỏ hàng. Mở giỏ hàng để kích hoạt Cảnh báo Tương kỵ Dược lý AI!",
                cartUrl = "/cart"
            });
        }

        [HttpPost("reset-data")]
        public async Task<IActionResult> ResetData(CancellationToken cancellationToken)
        {
            if (!IsDemoModeActive())
            {
                return NotFound(new { message = "Chế độ Demo không khả dụng." });
            }

            int? uid = null;
            if (int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedUid))
            {
                uid = parsedUid;
            }

            var result = await _demoResetService.ResetDemoDataAsync(uid, User.Identity?.Name ?? "DemoPresenterHUD", cancellationToken);

            return Ok(new
            {
                success = result.Success,
                message = result.Message,
                details = result
            });
        }

        private string GetOrCreateCartSessionId()
        {
            if (Request.Cookies.TryGetValue(CartCookieKey, out string? cartId) && !string.IsNullOrEmpty(cartId))
            {
                return cartId;
            }

            string newCartId = "DEMO-GUEST-" + Guid.NewGuid().ToString("N");
            Response.Cookies.Append(CartCookieKey, newCartId, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                HttpOnly = true,
                SameSite = SameSiteMode.Lax
            });
            return newCartId;
        }
    }

    public class QuickSwitchPersonaRequestDto
    {
        public string Persona { get; set; } = "customer";
    }
}
