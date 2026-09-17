using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Security;
using Omnichannel.Application.Interfaces.Security;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Security;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class PermissionManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<PermissionManagementController> _logger;

        public PermissionManagementController(
            ApplicationDbContext context,
            IPermissionService permissionService,
            ILogger<PermissionManagementController> logger)
        {
            _context = context;
            _permissionService = permissionService;
            _logger = logger;
        }

        [HttpGet]
        [HasPermission("SECURITY", "VIEW")]
        public async Task<IActionResult> Index(string? roleId = null)
        {
            var roles = await _context.Roles
                .AsNoTracking()
                .OrderBy(r => r.RoleName)
                .Select(r => new RoleDto
                {
                    RoleId = r.RoleId,
                    RoleName = r.RoleName,
                    Description = r.Description
                })
                .ToListAsync();

            var currentRoleId = string.IsNullOrEmpty(roleId)
                ? (roles.FirstOrDefault()?.RoleId ?? "SUPER_ADMIN")
                : roleId;

            var currentRole = roles.FirstOrDefault(r => r.RoleId == currentRoleId);

            var allPermissions = await _context.Permissions
                .AsNoTracking()
                .OrderBy(p => p.ModuleCode)
                .ThenBy(p => p.PermissionId)
                .ToListAsync();

            // Nhóm permissions theo Module
            var moduleGroups = allPermissions
                .GroupBy(p => p.ModuleCode)
                .Select(g => new ModulePermissionGroupDto
                {
                    ModuleCode = g.Key,
                    ModuleDisplayName = GetModuleDisplayName(g.Key),
                    IconClass = GetModuleIcon(g.Key),
                    Permissions = g.Select(p => new PermissionDetailDto
                    {
                        PermissionId = p.PermissionId,
                        ActionCode = p.ActionCode,
                        PermissionName = p.PermissionName,
                        Description = p.Description
                    }).ToList()
                })
                .ToList();

            var grantedIds = await _context.RolePermissions
                .AsNoTracking()
                .Where(rp => rp.RoleId == currentRoleId && rp.IsGranted)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            var viewModel = new PermissionMatrixViewModel
            {
                SelectedRoleId = currentRoleId,
                SelectedRoleName = currentRole?.RoleName ?? currentRoleId,
                Roles = roles,
                ModuleGroups = moduleGroups,
                GrantedPermissionIds = new HashSet<int>(grantedIds)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("SECURITY", "EDIT_MATRIX")]
        public async Task<IActionResult> SaveRoleMatrix([FromBody] UpdateRolePermissionsRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RoleId))
            {
                return BadRequest(new { success = false, message = "Mã vai trò không hợp lệ." });
            }

            var currentAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "SYSTEM";

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Xóa cấu hình quyền cũ của vai trò
                var oldPerms = await _context.RolePermissions
                    .Where(rp => rp.RoleId == request.RoleId)
                    .ToListAsync();
                _context.RolePermissions.RemoveRange(oldPerms);

                // Thêm danh sách quyền mới
                var newPerms = request.PermissionIds.Distinct().Select(permId => new RolePermission
                {
                    RoleId = request.RoleId,
                    PermissionId = permId,
                    IsGranted = true,
                    GrantedBy = currentAdminId,
                    GrantedAt = DateTime.UtcNow
                });

                await _context.RolePermissions.AddRangeAsync(newPerms);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Lấy danh sách tất cả người dùng thuộc vai trò này để xóa Cache tức thì
                var affectedUserIds = await _context.UserRoles
                    .AsNoTracking()
                    .Where(ur => ur.RoleId == request.RoleId)
                    .Select(ur => ur.UserId)
                    .ToListAsync();

                foreach (var userId in affectedUserIds)
                {
                    await _permissionService.InvalidateUserPermissionCacheAsync(userId);
                }

                _logger.LogInformation("SuperAdmin {AdminId} đã cập nhật ma trận quyền cho vai trò {RoleId}. Đã xóa Cache cho {Count} người dùng.",
                    currentAdminId, request.RoleId, affectedUserIds.Count);

                return Ok(new
                {
                    success = true,
                    message = $"Đã cập nhật thành công ma trận phân quyền cho vai trò '{request.RoleId}' và đồng bộ Cache hệ thống!"
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình lưu ma trận phân quyền.");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống khi lưu ma trận." });
            }
        }

        private static string GetModuleDisplayName(string moduleCode) => moduleCode switch
        {
            "SECURITY" => "Bảo Mật & Phân Quyền",
            "HR_USER" => "Hồ Sơ Nhân Sự & Tài Khoản",
            "MENU" => "Cấu Trúc Menu Đa Hình",
            "BANNER" => "Chiến Dịch Banner & Ads",
            "CMS_CONTENT" => "Nội Dung Bài Viết & CMS",
            "CATALOG" => "Danh Mục & Sản Phẩm SKU",
            "POS" => "Quầy Thu Ngân Bán Lẻ (POS)",
            "ORDER_OPS" => "Vận Hành Đơn Hàng Đa Kênh",
            "WAREHOUSE" => "Quản Trị Kho Vận & Lô Date FEFO",
            "PROMO_PARTNER" => "Khuyến Mại & Nhà Cung Cấp",
            "REPORT_AUDIT" => "Báo Cáo Doanh Thu & Kiểm Toán",
            _ => moduleCode
        };

        private static string GetModuleIcon(string moduleCode) => moduleCode switch
        {
            "SECURITY" => "fa-solid fa-shield-halved text-purple",
            "HR_USER" => "fa-solid fa-users text-pink",
            "MENU" => "fa-solid fa-bars text-purple",
            "BANNER" => "fa-solid fa-images text-pink",
            "CMS_CONTENT" => "fa-solid fa-newspaper text-purple",
            "CATALOG" => "fa-solid fa-boxes-stacked text-pink",
            "POS" => "fa-solid fa-cash-register text-purple",
            "ORDER_OPS" => "fa-solid fa-truck-fast text-pink",
            "WAREHOUSE" => "fa-solid fa-warehouse text-purple",
            "PROMO_PARTNER" => "fa-solid fa-tags text-pink",
            "REPORT_AUDIT" => "fa-solid fa-chart-line text-purple",
            _ => "fa-solid fa-cube"
        };
    }
}
