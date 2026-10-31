using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Omnichannel.Application.DTOs.Notifications;
using Omnichannel.Application.Interfaces.Notifications;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Services;
using Omnichannel.Tests.Helpers;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class NotificationServiceTests
    {
        [Fact]
        public async Task CreateAndSendNotificationAsync_ValidDto_SavesToDbAndDispatchesSignalR()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var mockDispatcher = new Mock<INotificationDispatcher>();

            var service = new NotificationService(
                context,
                NullLogger<NotificationService>.Instance,
                mockDispatcher.Object);

            var store = new Store { Name = "Chi nhánh Quận 1", Code = "ST-Q1", Address = "123 Lê Lợi" };
            context.Stores.Add(store);
            await context.SaveChangesAsync();

            var dto = new CreateNotificationDto
            {
                Type = "ORDER",
                Severity = "INFO",
                Title = "Đơn hàng mới #DH001",
                Message = "Khách hàng Nguyễn Văn A vừa đặt hàng 500,000đ",
                StoreId = store.Id,
                Role = "StoreManager",
                Url = "/Admin/Order/Details/1"
            };

            // Act
            var result = await service.CreateAndSendNotificationAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            result.Title.Should().Be(dto.Title);
            result.StoreName.Should().Be("Chi nhánh Quận 1");
            result.IsRead.Should().BeFalse();

            var inDb = await context.Notifications.FirstOrDefaultAsync(n => n.Id == result.Id);
            inDb.Should().NotBeNull();
            inDb!.Title.Should().Be(dto.Title);
            inDb.Type.Should().Be("ORDER");

            mockDispatcher.Verify(d => d.DispatchNotificationAsync(
                It.Is<NotificationDto>(p => p.Title == dto.Title && p.Role == "StoreManager"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetNotificationsAsync_FilterByTabs_ReturnsCorrectCategorizedResults()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new NotificationService(context, NullLogger<NotificationService>.Instance);

            context.Notifications.AddRange(
                new Notification { Type = "ORDER", Severity = "INFO", Title = "Đơn 1", Message = "Msg", IsRead = false, CreatedAt = DateTime.UtcNow },
                new Notification { Type = "ORDER", Severity = "INFO", Title = "Đơn 2", Message = "Msg", IsRead = true, CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
                new Notification { Type = "WAREHOUSE", Severity = "WARNING", Title = "Kho 1", Message = "Msg", IsRead = false, CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
                new Notification { Type = "SYSTEM", Severity = "INFO", Title = "Hệ thống 1", Message = "Msg", IsRead = false, CreatedAt = DateTime.UtcNow.AddMinutes(-15) },
                new Notification { Type = "ALERT", Severity = "DANGER", Title = "Cảnh báo AI", Message = "Msg", IsRead = true, CreatedAt = DateTime.UtcNow.AddMinutes(-20) }
            );
            await context.SaveChangesAsync();

            // Act - Tab ORDER
            var orderResult = await service.GetNotificationsAsync(new NotificationFilterDto { Tab = "ORDER" }, currentRole: "SuperAdmin");
            orderResult.Items.Should().HaveCount(2);
            orderResult.OrderCount.Should().Be(2);
            orderResult.WarehouseCount.Should().Be(1);
            orderResult.SystemCount.Should().Be(2); // SYSTEM + ALERT
            orderResult.UnreadCount.Should().Be(3);
            orderResult.TotalCount.Should().Be(5);

            // Act - Tab UNREAD
            var unreadResult = await service.GetNotificationsAsync(new NotificationFilterDto { Tab = "UNREAD" }, currentRole: "SuperAdmin");
            unreadResult.Items.Should().HaveCount(3);
            unreadResult.Items.Should().OnlyContain(n => !n.IsRead);

            // Act - Tab WAREHOUSE
            var warehouseResult = await service.GetNotificationsAsync(new NotificationFilterDto { Tab = "WAREHOUSE" }, currentRole: "SuperAdmin");
            warehouseResult.Items.Should().HaveCount(1);
            warehouseResult.Items[0].Title.Should().Be("Kho 1");
        }

        [Fact]
        public async Task MarkAsReadAsync_UnreadNotification_MarksAsReadAndSetsReadAt()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new NotificationService(context, NullLogger<NotificationService>.Instance);

            var notif = new Notification
            {
                Type = "ORDER",
                Severity = "INFO",
                Title = "Đơn mới",
                Message = "Msg",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            context.Notifications.Add(notif);
            await context.SaveChangesAsync();

            // Act
            var success = await service.MarkAsReadAsync(notif.Id);

            // Assert
            success.Should().BeTrue();
            var updated = await context.Notifications.FindAsync(notif.Id);
            updated!.IsRead.Should().BeTrue();
            updated.ReadAt.Should().NotBeNull();
        }

        [Fact]
        public async Task MarkAllAsReadAsync_MultipleUnread_MarksAllAsRead()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new NotificationService(context, NullLogger<NotificationService>.Instance);

            context.Notifications.AddRange(
                new Notification { Type = "ORDER", Severity = "INFO", Title = "1", Message = "Msg", IsRead = false, CreatedAt = DateTime.UtcNow },
                new Notification { Type = "WAREHOUSE", Severity = "WARNING", Title = "2", Message = "Msg", IsRead = false, CreatedAt = DateTime.UtcNow },
                new Notification { Type = "SYSTEM", Severity = "INFO", Title = "3", Message = "Msg", IsRead = false, CreatedAt = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();

            // Act
            var updatedCount = await service.MarkAllAsReadAsync(currentRole: "SuperAdmin");

            // Assert
            updatedCount.Should().Be(3);
            var unreadRemaining = await service.GetUnreadCountAsync(currentRole: "SuperAdmin");
            unreadRemaining.Should().Be(0);
        }

        [Fact]
        public async Task DeleteNotificationAsync_ExistingNotification_RemovesFromDb()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new NotificationService(context, NullLogger<NotificationService>.Instance);

            var notif = new Notification
            {
                Type = "SYSTEM",
                Severity = "INFO",
                Title = "Thông báo xóa",
                Message = "Msg",
                IsRead = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Notifications.Add(notif);
            await context.SaveChangesAsync();

            // Act
            var result = await service.DeleteNotificationAsync(notif.Id);

            // Assert
            result.Should().BeTrue();
            var exists = await context.Notifications.AnyAsync(n => n.Id == notif.Id);
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task BuildTargetedQuery_BranchStoreManager_SeesOnlyOwnStoreAndRoleNotifications()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new NotificationService(context, NullLogger<NotificationService>.Instance);

            context.Notifications.AddRange(
                new Notification { Type = "ORDER", Severity = "INFO", Title = "Chi nhánh 1", Message = "Msg", StoreId = 1, Role = "StoreManager", CreatedAt = DateTime.UtcNow },
                new Notification { Type = "ORDER", Severity = "INFO", Title = "Chi nhánh 2", Message = "Msg", StoreId = 2, Role = "StoreManager", CreatedAt = DateTime.UtcNow },
                new Notification { Type = "SYSTEM", Severity = "INFO", Title = "Broadcast toàn hệ thống", Message = "Msg", StoreId = null, Role = null, UserId = null, CreatedAt = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();

            // Act - StoreManager ở Chi nhánh 1
            var result = await service.GetNotificationsAsync(new NotificationFilterDto(), currentUserId: 10, currentRole: "StoreManager", currentStoreId: 1);

            // Assert: Thấy Chi nhánh 1 và Broadcast, KHÔNG thấy Chi nhánh 2
            result.Items.Should().HaveCount(2);
            result.Items.Select(x => x.Title).Should().Contain("Chi nhánh 1");
            result.Items.Select(x => x.Title).Should().Contain("Broadcast toàn hệ thống");
            result.Items.Select(x => x.Title).Should().NotContain("Chi nhánh 2");
        }
    }
}
