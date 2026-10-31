using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Omnichannel.Application.DTOs.Messages;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Services;
using Omnichannel.Tests.Helpers;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class InternalMessageServiceTests
    {
        [Fact]
        public async Task SendMessageAsync_ValidInput_CreatesMessageAndSavesToDb()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new InternalMessageService(context, NullLogger<InternalMessageService>.Instance);

            var sender = new User { Username = "sender", FullName = "Người Gửi", Email = "sender@gmail.com", PasswordHash = "hash" };
            var receiver = new User { Username = "receiver", FullName = "Người Nhận", Email = "receiver@gmail.com", PasswordHash = "hash" };
            context.Users.AddRange(sender, receiver);
            await context.SaveChangesAsync();

            var dto = new ComposeMessageDto
            {
                ReceiverId = receiver.Id,
                Subject = "Báo cáo doanh số tuần",
                Body = "Kính gửi quản lý, doanh số tuần này tăng 20% so với tuần trước."
            };

            // Act
            var result = await service.SendMessageAsync(sender.Id, dto);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            result.Subject.Should().Be(dto.Subject);
            result.SenderName.Should().Be(sender.FullName);
            result.ReceiverName.Should().Be(receiver.FullName);
            result.IsRead.Should().BeFalse();

            var inDb = await context.InternalMessages.FindAsync(result.Id);
            inDb.Should().NotBeNull();
            inDb!.Subject.Should().Be(dto.Subject);
            inDb.SenderId.Should().Be(sender.Id);
            inDb.ReceiverId.Should().Be(receiver.Id);
        }

        [Fact]
        public async Task GetInboxMessagesAsync_ReturnsOnlyRecipientMessagesAndCalculatesUnread()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new InternalMessageService(context, NullLogger<InternalMessageService>.Instance);

            var user1 = new User { Username = "u1", FullName = "User One", Email = "u1@gmail.com", PasswordHash = "hash" };
            var user2 = new User { Username = "u2", FullName = "User Two", Email = "u2@gmail.com", PasswordHash = "hash" };
            context.Users.AddRange(user1, user2);
            await context.SaveChangesAsync();

            context.InternalMessages.AddRange(
                new InternalMessage { SenderId = user2.Id, ReceiverId = user1.Id, Subject = "Thư 1", Body = "Nội dung 1", IsRead = false, SentAt = DateTime.UtcNow },
                new InternalMessage { SenderId = user2.Id, ReceiverId = user1.Id, Subject = "Thư 2", Body = "Nội dung 2", IsRead = true, SentAt = DateTime.UtcNow.AddMinutes(-10) },
                new InternalMessage { SenderId = user1.Id, ReceiverId = user2.Id, Subject = "Thư khác", Body = "Gửi u2", IsRead = false, SentAt = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();

            // Act
            var inbox = await service.GetInboxMessagesAsync(user1.Id);

            // Assert: user1 nhận 2 thư, trong đó 1 chưa đọc
            inbox.Items.Should().HaveCount(2);
            inbox.TotalCount.Should().Be(2);
            inbox.UnreadCount.Should().Be(1);
            inbox.Items.Select(x => x.Subject).Should().Contain(new[] { "Thư 1", "Thư 2" });
        }

        [Fact]
        public async Task GetSentMessagesAsync_ReturnsOnlySenderMessages()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new InternalMessageService(context, NullLogger<InternalMessageService>.Instance);

            var user1 = new User { Username = "u1", FullName = "User One", Email = "u1@gmail.com", PasswordHash = "hash" };
            var user2 = new User { Username = "u2", FullName = "User Two", Email = "u2@gmail.com", PasswordHash = "hash" };
            context.Users.AddRange(user1, user2);
            await context.SaveChangesAsync();

            context.InternalMessages.AddRange(
                new InternalMessage { SenderId = user1.Id, ReceiverId = user2.Id, Subject = "Thư tôi gửi 1", Body = "Nội dung", IsRead = false, SentAt = DateTime.UtcNow },
                new InternalMessage { SenderId = user1.Id, ReceiverId = user2.Id, Subject = "Thư tôi gửi 2", Body = "Nội dung", IsRead = true, SentAt = DateTime.UtcNow.AddMinutes(-5) },
                new InternalMessage { SenderId = user2.Id, ReceiverId = user1.Id, Subject = "Thư u2 gửi", Body = "Nội dung", IsRead = false, SentAt = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();

            // Act
            var sent = await service.GetSentMessagesAsync(user1.Id);

            // Assert
            sent.Items.Should().HaveCount(2);
            sent.Items.Select(x => x.Subject).Should().Contain(new[] { "Thư tôi gửi 1", "Thư tôi gửi 2" });
        }

        [Fact]
        public async Task MarkAsReadAsync_UnreadMessage_UpdatesStatusAndReadAt()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new InternalMessageService(context, NullLogger<InternalMessageService>.Instance);

            var u1 = new User { Username = "u1", FullName = "User One", Email = "u1@gmail.com", PasswordHash = "hash" };
            var u2 = new User { Username = "u2", FullName = "User Two", Email = "u2@gmail.com", PasswordHash = "hash" };
            context.Users.AddRange(u1, u2);
            await context.SaveChangesAsync();

            var msg = new InternalMessage { SenderId = u2.Id, ReceiverId = u1.Id, Subject = "Chưa đọc", Body = "Body", IsRead = false, SentAt = DateTime.UtcNow };
            context.InternalMessages.Add(msg);
            await context.SaveChangesAsync();

            // Act
            var success = await service.MarkAsReadAsync(msg.Id, u1.Id);

            // Assert
            success.Should().BeTrue();
            var updated = await context.InternalMessages.FindAsync(msg.Id);
            updated!.IsRead.Should().BeTrue();
            updated.ReadAt.Should().NotBeNull();
        }

        [Fact]
        public async Task DeleteMessageAsync_ReceiverDeletes_MarksIsDeletedByReceiver()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var service = new InternalMessageService(context, NullLogger<InternalMessageService>.Instance);

            var u1 = new User { Username = "u1", FullName = "User One", Email = "u1@gmail.com", PasswordHash = "hash" };
            var u2 = new User { Username = "u2", FullName = "User Two", Email = "u2@gmail.com", PasswordHash = "hash" };
            context.Users.AddRange(u1, u2);
            await context.SaveChangesAsync();

            var msg = new InternalMessage { SenderId = u2.Id, ReceiverId = u1.Id, Subject = "Thư xóa", Body = "Body", IsRead = true, SentAt = DateTime.UtcNow };
            context.InternalMessages.Add(msg);
            await context.SaveChangesAsync();

            // Act - Receiver xóa
            var success = await service.DeleteMessageAsync(msg.Id, u1.Id, "inbox");

            // Assert: Chưa bị xóa hẳn khỏi DB vì Sender chưa xóa
            success.Should().BeTrue();
            var inDb = await context.InternalMessages.FindAsync(msg.Id);
            inDb.Should().NotBeNull();
            inDb!.IsDeletedByReceiver.Should().BeTrue();
            inDb.IsDeletedBySender.Should().BeFalse();

            // Act - Sender xóa nốt
            await service.DeleteMessageAsync(msg.Id, u2.Id, "sent");

            // Assert: Cả 2 đã xóa -> message bị xóa hẳn khỏi DB
            var removed = await context.InternalMessages.FindAsync(msg.Id);
            removed.Should().BeNull();
        }
    }
}
