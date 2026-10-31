using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Omnichannel.Application.DTOs.Chat;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Services;
using Omnichannel.Tests.Helpers;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class ChatbotServiceTests
    {
        private IConfiguration CreateEmptyConfiguration()
        {
            var myConfiguration = new Dictionary<string, string?>
            {
                { "ChatBot:ApiKey", "" }
            };
            return new ConfigurationBuilder()
                .AddInMemoryCollection(myConfiguration)
                .Build();
        }

        [Fact]
        public async Task ProcessMessageAsync_EmptyMessage_ReturnsFailure()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var config = CreateEmptyConfiguration();
            var service = new ChatbotService(context, config, NullLogger<ChatbotService>.Instance);

            var request = new ChatRequestDto
            {
                SessionKey = "sess-test-01",
                Message = "   "
            };

            // Act
            var result = await service.ProcessMessageAsync(request);

            // Assert
            result.Success.Should().BeFalse();
            result.Intent.Should().Be("empty");
        }

        [Fact]
        public async Task ProcessMessageAsync_SkincareKeyword_ReturnsSkincareResponseAndLinks()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var config = CreateEmptyConfiguration();
            var service = new ChatbotService(context, config, NullLogger<ChatbotService>.Instance);

            var request = new ChatRequestDto
            {
                SessionKey = "sess-test-skin",
                Message = "Cho mình hỏi về quy trình skincare cho người mới bắt đầu?"
            };

            // Act
            var result = await service.ProcessMessageAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Intent.Should().Be("skincare");
            result.ActionUrl.Should().Contain("skincare");
            result.Reply.Should().Contain("Quy trình chăm sóc da");
            result.QuickReplies.Should().Contain(r => r.Contains("da dầu"));
        }

        [Fact]
        public async Task ProcessMessageAsync_OrderKeyword_ReturnsTrackingGuide()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var config = CreateEmptyConfiguration();
            var service = new ChatbotService(context, config, NullLogger<ChatbotService>.Instance);

            var request = new ChatRequestDto
            {
                SessionKey = "sess-test-order",
                Message = "Làm sao để tra cứu đơn hàng và xem tracking vận chuyển?"
            };

            // Act
            var result = await service.ProcessMessageAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Intent.Should().Be("order");
            result.ActionUrl.Should().Be("/Account/Orders");
            result.Reply.Should().Contain("Lịch Sử Đơn Hàng");
        }

        [Fact]
        public async Task ProcessMessageAsync_PromotionKeyword_ReturnsPromotions()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var config = CreateEmptyConfiguration();
            var service = new ChatbotService(context, config, NullLogger<ChatbotService>.Instance);

            // Seed active promotion
            context.Promotions.Add(new Promotion
            {
                PromotionCode = "TESTPROMO30",
                Name = "Giảm 30% Mừng Khai Trương",
                PromotionType = "PERCENTAGE",
                DiscountValue = 30,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(10),
                IsActive = true
            });
            await context.SaveChangesAsync();

            var request = new ChatRequestDto
            {
                SessionKey = "sess-test-promo",
                Message = "Shop có chương trình khuyến mãi hay voucher giảm giá nào không?"
            };

            // Act
            var result = await service.ProcessMessageAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Intent.Should().Be("promotion");
            result.ActionUrl.Should().Be("/Promotion");
            result.Reply.Should().Contain("TESTPROMO30");
        }

        [Fact]
        public async Task ProcessMessageAsync_OilySkinKeyword_ReturnsTargetedAdvice()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var config = CreateEmptyConfiguration();
            var service = new ChatbotService(context, config, NullLogger<ChatbotService>.Instance);

            var request = new ChatRequestDto
            {
                SessionKey = "sess-test-oily",
                Message = "Mình có da dầu mụn nhiều dầu ở trán thì nên dùng gì?"
            };

            // Act
            var result = await service.ProcessMessageAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Intent.Should().Be("consultation");
            result.Reply.Should().Contain("Da Dầu / Da Mụn");
            result.Reply.Should().Contain("Niacinamide");
            result.Reply.Should().Contain("Salicylic Acid");
        }

        [Fact]
        public async Task ProcessMessageAsync_ContactKeyword_ReturnsHotlineAndAddresses()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var config = CreateEmptyConfiguration();
            var service = new ChatbotService(context, config, NullLogger<ChatbotService>.Instance);

            var request = new ChatRequestDto
            {
                SessionKey = "sess-test-contact",
                Message = "Cho mình xin số hotline và địa chỉ showroom gần nhất"
            };

            // Act
            var result = await service.ProcessMessageAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.Intent.Should().Be("contact");
            result.Reply.Should().Contain("1900 6868");
            result.Reply.Should().Contain("Vincom Center Bà Triệu");
            result.Reply.Should().Contain("Lê Thánh Tôn");
        }

        [Fact]
        public async Task ProcessMessageAsync_SavesSessionAndMessageToDatabase()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var config = CreateEmptyConfiguration();
            var service = new ChatbotService(context, config, NullLogger<ChatbotService>.Instance);

            var sessionKey = "sess-persist-test";
            var request = new ChatRequestDto
            {
                SessionKey = sessionKey,
                Message = "Chào bạn, mình cần hỗ trợ",
                CustomerName = "Nguyễn Văn Test"
            };

            // Act
            var result = await service.ProcessMessageAsync(request);

            // Assert
            result.Success.Should().BeTrue();

            var session = await context.ChatSessions.FirstOrDefaultAsync(s => s.SessionKey == sessionKey);
            session.Should().NotBeNull();
            session!.CustomerName.Should().Be("Nguyễn Văn Test");
            session.MessageCount.Should().Be(2); // 1 user + 1 bot

            var messages = await context.ChatMessages.Where(m => m.SessionKey == sessionKey).ToListAsync();
            messages.Should().HaveCount(2);
            messages.Should().Contain(m => m.Sender == "USER" && m.Content == "Chào bạn, mình cần hỗ trợ");
            messages.Should().Contain(m => m.Sender == "BOT");
        }

        [Fact]
        public async Task GetChatHistoryAsync_FiltersByTopic_ReturnsCorrectSessions()
        {
            // Arrange
            using var context = TestDbContextFactory.Create();
            var config = CreateEmptyConfiguration();
            var service = new ChatbotService(context, config, NullLogger<ChatbotService>.Instance);

            context.ChatSessions.AddRange(
                new ChatSession { SessionKey = "sess-1", DetectedTopic = "SKINCARE", MessageCount = 3 },
                new ChatSession { SessionKey = "sess-2", DetectedTopic = "ORDER", MessageCount = 2 },
                new ChatSession { SessionKey = "sess-3", DetectedTopic = "SKINCARE", MessageCount = 5 }
            );
            await context.SaveChangesAsync();

            // Act
            var history = await service.GetChatHistoryAsync(new ChatHistoryFilterDto { Topic = "SKINCARE" });

            // Assert
            history.Sessions.Should().HaveCount(2);
            history.Sessions.Should().OnlyContain(s => s.DetectedTopic == "SKINCARE");
            history.TotalItems.Should().Be(2);
        }
    }
}
