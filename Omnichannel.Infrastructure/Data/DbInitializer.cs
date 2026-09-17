using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Security;

namespace Omnichannel.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            await context.Database.EnsureCreatedAsync();

            var passwordHasher = new PasswordHasherService();

            // 1. Kiểm tra và cập nhật tài khoản SuperAdmin
            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
            if (adminUser == null)
            {
                adminUser = new User
                {
                    UserId = "USR-SUPERADMIN-0001",
                    Username = "admin",
                    NormalizedUsername = "ADMIN",
                    Email = "admin@beautyomnichannel.vn",
                    PhoneNumber = "0909999888",
                    PasswordHash = passwordHasher.HashPassword("Admin@123456"),
                    SecurityStamp = Guid.NewGuid().ToString("D"),
                    FullName = "Quản Trị Viên Tối Cao",
                    UserType = 5,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await context.Users.AddAsync(adminUser);
                await context.SaveChangesAsync();
            }
            else
            {
                // Tự động kiểm tra và băm lại mật khẩu chuẩn xác nếu chuỗi hash cũ không khớp
                if (!passwordHasher.VerifyPassword(adminUser.PasswordHash, "Admin@123456"))
                {
                    adminUser.PasswordHash = passwordHasher.HashPassword("Admin@123456");
                    adminUser.IsActive = true;
                    adminUser.SecurityStamp = Guid.NewGuid().ToString("D");
                    await context.SaveChangesAsync();
                }
            }

            // 2. Đảm bảo Role SUPER_ADMIN tồn tại
            var superAdminRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleId == "SUPER_ADMIN");
            if (superAdminRole == null)
            {
                superAdminRole = new Role
                {
                    RoleId = "SUPER_ADMIN",
                    RoleName = "Quản trị viên Tối cao",
                    Description = "Toàn quyền quản trị toàn bộ hệ sinh thái",
                    IsSystemRole = true,
                    CreatedAt = DateTime.UtcNow
                };
                await context.Roles.AddAsync(superAdminRole);
                await context.SaveChangesAsync();
            }

            // 3. Đảm bảo UserRole liên kết
            var userRoleExists = await context.UserRoles.AnyAsync(ur => ur.UserId == adminUser.UserId && ur.RoleId == superAdminRole.RoleId);
            if (!userRoleExists)
            {
                await context.UserRoles.AddAsync(new UserRole
                {
                    UserId = adminUser.UserId,
                    RoleId = superAdminRole.RoleId,
                    AssignedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            // 4. Gán toàn bộ quyền hiện có cho SUPER_ADMIN
            var allPermissions = await context.Permissions.ToListAsync();
            var existingRolePermIds = await context.RolePermissions
                .Where(rp => rp.RoleId == superAdminRole.RoleId)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            foreach (var perm in allPermissions)
            {
                if (!existingRolePermIds.Contains(perm.PermissionId))
                {
                    await context.RolePermissions.AddAsync(new RolePermission
                    {
                        RoleId = superAdminRole.RoleId,
                        PermissionId = perm.PermissionId,
                        IsGranted = true,
                        GrantedBy = adminUser.UserId,
                        GrantedAt = DateTime.UtcNow
                    });
                }
            }

            await context.SaveChangesAsync();
        }
    }
}