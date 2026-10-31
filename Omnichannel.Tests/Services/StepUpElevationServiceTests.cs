using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Omnichannel.Application.DTOs.Security;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Security;
using Omnichannel.Tests.Helpers;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class StepUpElevationServiceTests
    {
        private (StepUpElevationService Service, Omnichannel.Infrastructure.Data.ApplicationDbContext Db, IMemoryCache Cache) CreateService(
            string hmacSecret = "Test_HMAC_Secret_Key_For_StepUp_Security_2026!")
        {
            var context = TestDbContextFactory.Create();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var permService = new PermissionService(context, memoryCache);
            var passwordHasher = new PasswordHasherService();

            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Security:StepUpHmacSecret", hmacSecret }
            });
            var config = configBuilder.Build();

            var logger = NullLogger<StepUpElevationService>.Instance;
            var service = new StepUpElevationService(context, permService, passwordHasher, memoryCache, config, logger);

            return (service, context, memoryCache);
        }

        [Fact]
        public async Task IssueElevationTokenAsync_ValidManagerPin_ReturnsElevationToken()
        {
            // Arrange
            var (service, db, _) = CreateService();

            var role = new Role { Name = "StoreManager", Description = "Quản lý cửa hàng", IsActive = true };
            db.Roles.Add(role);
            await db.SaveChangesAsync();

            var perm = new Permission { Code = "POS_ORDER_CANCEL", Name = "Hủy đơn POS", Module = "POS" };
            db.Permissions.Add(perm);
            await db.SaveChangesAsync();

            db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = perm.Id });

            var manager = new User
            {
                Username = "manager_hq",
                FullName = "Trần Quản Lý HQ",
                Email = "manager_hq@test.com",
                StoreId = 1,
                IsActive = true
            };
            db.Users.Add(manager);
            await db.SaveChangesAsync();

            db.UserRoles.Add(new UserRole { UserId = manager.Id, RoleId = role.Id });
            await db.SaveChangesAsync();

            var request = new StepUpRequestDto
            {
                ManagerUsernameOrQr = "manager_hq",
                PinCode = "123456",
                ActionCode = "POS_ORDER_CANCEL",
                TargetEntityId = "ORD-20261029-001",
                Reason = "Khách đổi ý trước khi in hóa đơn"
            };

            // Act
            var result = await service.IssueElevationTokenAsync(cashierUserId: 10, cashierStoreId: 1, request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.ElevationToken.Should().NotBeNullOrWhiteSpace();
            result.ApproverFullName.Should().Be("Trần Quản Lý HQ");
        }

        [Fact]
        public async Task IssueElevationTokenAsync_InvalidPin_FailsElevation()
        {
            // Arrange
            var (service, db, _) = CreateService();

            var manager = new User
            {
                Username = "manager_cg",
                FullName = "Lê Quản Lý CG",
                StoreId = 2,
                IsActive = true
            };
            db.Users.Add(manager);
            await db.SaveChangesAsync();

            var request = new StepUpRequestDto
            {
                ManagerUsernameOrQr = "manager_cg",
                PinCode = "WRONG_PIN_999999",
                ActionCode = "POS_ORDER_CANCEL",
                TargetEntityId = "ORD-002"
            };

            // Act
            var result = await service.IssueElevationTokenAsync(cashierUserId: 12, cashierStoreId: 2, request);

            // Assert
            result.Success.Should().BeFalse();
            result.ElevationToken.Should().BeNull();
            result.Message.Should().Contain("không chính xác");
        }

        [Fact]
        public async Task IssueElevationTokenAsync_DifferentStoreManager_FailsCrossBranchElevation()
        {
            // Arrange
            var (service, db, _) = CreateService();

            var managerStore2 = new User
            {
                Username = "manager_store2",
                FullName = "Quản Lý Kho 2",
                StoreId = 2,
                IsActive = true
            };
            db.Users.Add(managerStore2);
            await db.SaveChangesAsync();

            var request = new StepUpRequestDto
            {
                ManagerUsernameOrQr = "manager_store2",
                PinCode = "123456",
                ActionCode = "POS_DRAWER_NO_SALE",
                TargetEntityId = "DRAWER-01"
            };

            // Act - Thu ngân ở Store 1 yêu cầu Quản lý Store 2 phê duyệt
            var result = await service.IssueElevationTokenAsync(cashierUserId: 5, cashierStoreId: 1, request);

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("khác chi nhánh");
        }

        [Fact]
        public async Task ValidateAndConsumeTokenAsync_ValidToken_ConsumesNonceAndLogsDualAudit()
        {
            // Arrange
            var (service, db, _) = CreateService();

            var role = new Role { Name = "SuperAdmin", IsActive = true };
            db.Roles.Add(role);
            await db.SaveChangesAsync();

            var admin = new User
            {
                Username = "superadmin",
                FullName = "Nguyễn Quản Trị",
                StoreId = null,
                IsActive = true
            };
            db.Users.Add(admin);
            await db.SaveChangesAsync();

            db.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = role.Id });
            await db.SaveChangesAsync();

            var request = new StepUpRequestDto
            {
                ManagerUsernameOrQr = "superadmin",
                PinCode = "888888",
                ActionCode = "POS_MANUAL_DISCOUNT",
                TargetEntityId = "DISC-50PCT",
                Reason = "Khách VIP thẻ Bạch Kim sinh nhật"
            };

            var issueResult = await service.IssueElevationTokenAsync(cashierUserId: 7, cashierStoreId: 1, request);
            issueResult.Success.Should().BeTrue();

            // Act - Validate and consume token
            var payload = await service.ValidateAndConsumeTokenAsync(
                token: issueResult.ElevationToken!,
                currentCashierUserId: 7,
                requiredActionCode: "POS_MANUAL_DISCOUNT",
                targetEntityId: "DISC-50PCT");

            // Assert
            payload.Should().NotBeNull();
            payload.CashierUserId.Should().Be(7);
            payload.ApproverManagerId.Should().Be(admin.Id);
            payload.ActionCode.Should().Be("POS_MANUAL_DISCOUNT");

            // Verify Audit Log entry exists
            var auditLog = await db.AuditLogs.FirstOrDefaultAsync(a => a.Action.Contains("STEP_UP_APPROVED:POS_MANUAL_DISCOUNT"));
            auditLog.Should().NotBeNull();
            auditLog!.NewValues.Should().Contain("Nguyễn Quản Trị");
            auditLog.NewValues.Should().Contain("Khách VIP thẻ Bạch Kim");
        }

        [Fact]
        public async Task ValidateAndConsumeTokenAsync_ReplayAttempt_ThrowsSecurityException()
        {
            // Arrange
            var (service, db, _) = CreateService();

            var role = new Role { Name = "SuperAdmin", IsActive = true };
            db.Roles.Add(role);
            await db.SaveChangesAsync();

            var admin = new User
            {
                Username = "admin_sec",
                FullName = "Admin An Ninh",
                IsActive = true
            };
            db.Users.Add(admin);
            await db.SaveChangesAsync();
            db.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = role.Id });
            await db.SaveChangesAsync();

            var request = new StepUpRequestDto
            {
                ManagerUsernameOrQr = "admin_sec",
                PinCode = "123456",
                ActionCode = "POS_DRAWER_NO_SALE",
                TargetEntityId = "DRAWER-01",
                Reason = "Đổi tiền lẻ cho khách"
            };

            var issueResult = await service.IssueElevationTokenAsync(cashierUserId: 3, cashierStoreId: 1, request);
            issueResult.Success.Should().BeTrue();

            // First consumption -> Success
            await service.ValidateAndConsumeTokenAsync(
                token: issueResult.ElevationToken!,
                currentCashierUserId: 3,
                requiredActionCode: "POS_DRAWER_NO_SALE",
                targetEntityId: "DRAWER-01");

            // Second consumption (Replay Attack) -> Must throw SecurityException
            Func<Task> secondCall = async () =>
            {
                await service.ValidateAndConsumeTokenAsync(
                    token: issueResult.ElevationToken!,
                    currentCashierUserId: 3,
                    requiredActionCode: "POS_DRAWER_NO_SALE",
                    targetEntityId: "DRAWER-01");
            };

            await secondCall.Should().ThrowAsync<SecurityException>()
                .WithMessage("*Replay Attack*");
        }
    }
}
