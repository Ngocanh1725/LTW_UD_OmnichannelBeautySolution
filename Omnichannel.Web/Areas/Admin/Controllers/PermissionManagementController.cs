using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Security;
using Omnichannel.Application.Interfaces.Security;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Security;
using Omnichannel.Web.Extensions;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,SUPER_ADMIN")]
    [HasPermission("PERMISSION_MANAGE")]
    public class PermissionManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPermissionService _permissionService;
        private readonly IAuditLogService _auditLogService;

        public PermissionManagementController(
            ApplicationDbContext context,
            IPermissionService permissionService,
            IAuditLogService auditLogService)
        {
            _context = context;
            _permissionService = permissionService;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? roleId)
        {
            var roles = await _context.Roles
                .AsNoTracking()
                .Select(r => new RoleDto { Id = r.Id, Name = r.Name, Description = r.Description })
                .ToListAsync();

            var users = await _context.Users
                .AsNoTracking()
                .Where(u => u.IsActive)
                .Select(u => new UserSummaryDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    FullName = u.FullName,
                    Email = u.Email,
                    StoreName = u.Store != null ? u.Store.Name : "Toàn hệ thống"
                })
                .ToListAsync();

            var selectedRoleId = roleId ?? roles.FirstOrDefault()?.Id ?? 0;

            var permissions = await _context.Permissions
                .AsNoTracking()
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Code)
                .ToListAsync();

            var moduleGroups = permissions
                .GroupBy(p => p.Module)
                .Select(g => new ModulePermissionGroupDto
                {
                    ModuleCode = g.Key,
                    ModuleName = GetModuleDisplayName(g.Key),
                    Permissions = g.Select(p => new PermissionItemDto
                    {
                        Id = p.Id,
                        Code = p.Code,
                        Name = p.Name,
                        Action = ExtractAction(p.Code),
                        Description = p.Description
                    }).ToList()
                })
                .ToList();

            var activeRolePermIds = await _context.RolePermissions
                .AsNoTracking()
                .Where(rp => rp.RoleId == selectedRoleId)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            var vm = new PermissionMatrixViewModel
            {
                Roles = roles,
                Users = users,
                ModuleGroups = moduleGroups,
                SelectedRoleId = selectedRoleId,
                ActivePermissionIdsForRole = activeRolePermIds
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> GetRolePermissions(int roleId)
        {
            var permissionIds = await _context.RolePermissions
                .AsNoTracking()
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            return Json(new { success = true, roleId, permissionIds });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveRoleMatrix([FromBody] RolePermissionUpdateDto dto)
        {
            if (dto == null || dto.RoleId <= 0)
            {
                return Json(new { success = false, message = "Dữ liệu yêu cầu không hợp lệ." });
            }

            var role = await _context.Roles.FindAsync(dto.RoleId);
            if (role == null)
            {
                return Json(new { success = false, message = "Vai trò không tồn tại trong hệ thống." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Xóa toàn bộ mapping quyền hiện tại của Role
                var existing = await _context.RolePermissions.Where(rp => rp.RoleId == dto.RoleId).ToListAsync();
                _context.RolePermissions.RemoveRange(existing);

                // Thêm danh sách quyền mới được tick
                if (dto.PermissionIds != null && dto.PermissionIds.Any())
                {
                    var newMappings = dto.PermissionIds.Distinct().Select(pid => new RolePermission
                    {
                        RoleId = dto.RoleId,
                        PermissionId = pid
                    });
                    await _context.RolePermissions.AddRangeAsync(newMappings);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Evict Cache tức thì của toàn bộ người dùng thuộc Role này
                await _permissionService.InvalidateRolePermissionCacheAsync(dto.RoleId);

                // Ghi Audit Log thay đổi ma trận phân quyền
                string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                string userAgent = Request.Headers["User-Agent"].ToString();
                await _auditLogService.LogAsync(
                    action: "PERMISSION_ROLE_UPDATE",
                    entityType: "Role",
                    entityId: role.Id.ToString(),
                    oldValues: null,
                    newValues: $"Cập nhật phân quyền cho vai trò '{role.Name}'. Số quyền gán: {dto.PermissionIds?.Count ?? 0}.",
                    userId: User.GetUserId(),
                    userName: User.Identity?.Name,
                    ipAddress: ipAddress,
                    userAgent: userAgent
                );

                return Json(new { success = true, message = $"Cập nhật ma trận phân quyền cho vai trò '{role.Name}' thành công!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = $"Lỗi khi lưu phân quyền: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserOverrides(int userId)
        {
            var breakdown = await _permissionService.GetUserPermissionBreakdownAsync(userId);
            return Json(new { success = true, userId, breakdown });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveUserOverride([FromBody] UserOverrideDto dto)
        {
            if (dto == null || dto.UserId <= 0 || dto.PermissionId <= 0)
            {
                return Json(new { success = false, message = "Tham số đặc cách phân quyền không hợp lệ." });
            }

            var existingOverride = await _context.UserPermissions
                .FirstOrDefaultAsync(up => up.UserId == dto.UserId && up.PermissionId == dto.PermissionId);

            if (!dto.OverrideStatus.HasValue)
            {
                // Xóa đặc cách (trả về thừa kế từ Role)
                if (existingOverride != null)
                {
                    _context.UserPermissions.Remove(existingOverride);
                }
            }
            else
            {
                // Đặt cờ Granted hoặc Revoked
                if (existingOverride != null)
                {
                    existingOverride.IsGranted = dto.OverrideStatus.Value;
                }
                else
                {
                    await _context.UserPermissions.AddAsync(new UserPermission
                    {
                        UserId = dto.UserId,
                        PermissionId = dto.PermissionId,
                        IsGranted = dto.OverrideStatus.Value
                    });
                }
            }

            await _context.SaveChangesAsync();
            await _permissionService.InvalidateUserPermissionCacheAsync(dto.UserId);

            // Ghi Audit Log đặc cách quyền cá nhân
            string ipAddr = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string uAgent = Request.Headers["User-Agent"].ToString();
            await _auditLogService.LogAsync(
                action: "PERMISSION_USER_OVERRIDE",
                entityType: "UserPermission",
                entityId: $"User:{dto.UserId}-Perm:{dto.PermissionId}",
                oldValues: null,
                newValues: $"Đặc cách người dùng ID {dto.UserId}: Quyền ID {dto.PermissionId} -> {(dto.OverrideStatus == true ? "CẤP THÊM (GRANTED)" : dto.OverrideStatus == false ? "TƯỚC BỎ (REVOKED)" : "TRẢ VỀ MẶC ĐỊNH (RESET)")}.",
                userId: User.GetUserId(),
                userName: User.Identity?.Name,
                ipAddress: ipAddr,
                userAgent: uAgent
            );

            return Json(new { success = true, message = "Cập nhật đặc cách quyền cá nhân thành công!" });
        }

        #region Helpers
        private static string ExtractAction(string code)
        {
            var parts = code.Split(new[] { '_', ':' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 1 ? parts[^1].ToUpperInvariant() : "ACCESS";
        }

        private static string GetModuleDisplayName(string moduleCode) => moduleCode.ToUpperInvariant() switch
        {
            "DASHBOARD" => "Tổng Quan Hệ Thống (Dashboard)",
            "CATALOG" or "PRODUCT" => "Danh Mục & Sản Phẩm (Product)",
            "INVENTORY" => "Quản Lý Kho & Lô Hàng FEFO (Inventory)",
            "POS" => "Bán Lẻ Điểm Bán (POS)",
            "ORDER" => "Quản Lý Đơn Hàng Đa Kênh (Order)",
            "CUSTOMER" or "CRM" => "Khách Hàng & Hồ Sơ CRM (Customer)",
            "CMS" => "Biên Tập Nội Dung & Banner (CMS)",
            "REPORT" => "Báo Cáo & Thống Kê (Report)",
            "FINANCE" => "Tài Chính & Kế Toán Sổ Cái (Finance)",
            "PROMOTION" => "Khuyến Mãi & Voucher (Promotion)",
            "LOYALTY" => "Ví Điểm & Hạng Thành Viên (Loyalty)",
            "STOCK_TRANSFER" or "TRANSFER" => "Điều Chuyển Kho Liên Chi Nhánh (Stock Transfer)",
            "SETTINGS" => "Cài Đặt Hệ Thống (Settings)",
            "SECURITY" => "Định Danh & Phân Quyền Bảo Mật (Security)",
            _ => moduleCode
        };
        #endregion
    }
}