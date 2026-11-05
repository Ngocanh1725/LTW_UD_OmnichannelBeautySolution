using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Omnichannel.Application.DTOs.Cms;
using Omnichannel.Application.Interfaces.Cms;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Services;
using Omnichannel.Web.Controllers;
using Omnichannel.Web.Hubs;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class ProductQnaAndReviewEnhancementTests
    {
        private ApplicationDbContext CreateInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        private static IFormFile CreateFormFile(string fileName, string contentType, byte[] content)
        {
            var stream = new MemoryStream(content);
            var formFile = new FormFile(stream, 0, content.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
            return formFile;
        }

        [Fact]
        public void IsValidImageFile_Should_Block_Executable_Disguised_As_Jpg()
        {
            // Arrange: MZ header (0x4D, 0x5A) for Windows PE Executable disguised as .jpg
            var fakeJpgBytes = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00 };
            var file = CreateFormFile("malware.jpg", "image/jpeg", fakeJpgBytes);

            // Act
            bool isValid = ReviewController.IsValidImageFile(file);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValidImageFile_Should_Accept_Valid_Jpeg_Signature()
        {
            // Arrange: Valid JPEG Magic Bytes (FF D8 FF E0)
            var validJpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01 };
            var file = CreateFormFile("swatch.jpg", "image/jpeg", validJpegBytes);

            // Act
            bool isValid = ReviewController.IsValidImageFile(file);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void IsValidImageFile_Should_Accept_Valid_Png_Signature()
        {
            // Arrange: Valid PNG Magic Bytes (89 50 4E 47 0D 0A 1A 0A)
            var validPngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D };
            var file = CreateFormFile("texture.png", "image/png", validPngBytes);

            // Act
            bool isValid = ReviewController.IsValidImageFile(file);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void IsValidImageFile_Should_Accept_Valid_Webp_Signature()
        {
            // Arrange: Valid WebP Magic Bytes ("RIFF" + 4 size bytes + "WEBP")
            var validWebpBytes = new byte[]
            {
                0x52, 0x49, 0x46, 0x46, // RIFF
                0x24, 0x00, 0x00, 0x00, // File size
                0x57, 0x45, 0x42, 0x50  // WEBP
            };
            var file = CreateFormFile("photo.webp", "image/webp", validWebpBytes);

            // Act
            bool isValid = ReviewController.IsValidImageFile(file);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public async Task QnaController_Ask_Should_Return_401_When_User_Not_Authenticated()
        {
            // Arrange
            var qnaServiceMock = new Mock<IProductQnaService>();
            var hubContextMock = new Mock<IHubContext<NotificationHub>>();
            var controller = new QnaController(qnaServiceMock.Object, hubContextMock.Object, NullLogger<QnaController>.Instance);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity()) // Unauthenticated
                }
            };

            var req = new SubmitQuestionRequest
            {
                ProductId = 1,
                Content = "Sản phẩm này dùng cho bà bầu được không ạ?"
            };

            // Act
            var result = await controller.Ask(req);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(401, objectResult.StatusCode);
        }

        [Fact]
        public async Task QnaController_Ask_Should_Create_Question_And_Notify_Managers_When_Authenticated()
        {
            // Arrange
            var qnaServiceMock = new Mock<IProductQnaService>();
            qnaServiceMock.Setup(s => s.SubmitQuestionAsync(6, It.IsAny<string>(), 15, "Minh Anh", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProductQuestionDto
                {
                    Id = 101,
                    ProductId = 6,
                    AuthorName = "Minh Anh",
                    Content = "Dùng sáng hay tối tốt hơn ạ?"
                });

            var hubClientsMock = new Mock<IHubClients>();
            var clientProxyMock = new Mock<IClientProxy>();
            hubClientsMock.Setup(c => c.Group("Managers")).Returns(clientProxyMock.Object);

            var hubContextMock = new Mock<IHubContext<NotificationHub>>();
            hubContextMock.Setup(h => h.Clients).Returns(hubClientsMock.Object);

            var controller = new QnaController(qnaServiceMock.Object, hubContextMock.Object, NullLogger<QnaController>.Instance);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "15"),
                new Claim(ClaimTypes.Name, "Minh Anh")
            };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "CookieAuth"))
                }
            };

            var req = new SubmitQuestionRequest
            {
                ProductId = 6,
                Content = "Dùng sáng hay tối tốt hơn ạ?"
            };

            // Act
            var result = await controller.Ask(req);

            // Assert
            Assert.IsType<JsonResult>(result);
            clientProxyMock.Verify(
                p => p.SendCoreAsync("ReceiveNotification", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ProductQnaService_Should_Encode_Html_To_Prevent_Xss()
        {
            // Arrange
            var db = CreateInMemoryDbContext(nameof(ProductQnaService_Should_Encode_Html_To_Prevent_Xss));
            db.Products.Add(new Product
            {
                Id = 1,
                Name = "Serum B5",
                Slug = "serum-b5",
                Sku = "SB5",
                Barcode = "123",
                IsActive = true
            });
            await db.SaveChangesAsync();

            var service = new ProductQnaService(db, NullLogger<ProductQnaService>.Instance);

            // Act
            var created = await service.SubmitQuestionAsync(1, "<script>alert('xss')</script> Dùng chung với Retinol được không?", 5, "<b>Hacker</b>");

            // Assert
            Assert.NotNull(created);
            Assert.DoesNotContain("<script>", created.Content);
            Assert.Contains("&lt;script&gt;", created.Content);
            Assert.DoesNotContain("<b>", created.AuthorName);
        }

        [Fact]
        public async Task ProductQnaService_Should_Order_Official_Pharmacist_Answers_First()
        {
            // Arrange
            var db = CreateInMemoryDbContext(nameof(ProductQnaService_Should_Order_Official_Pharmacist_Answers_First));
            db.Products.Add(new Product
            {
                Id = 10,
                Name = "Kem Chống Nắng",
                Slug = "kcn",
                Sku = "KCN10",
                Barcode = "1010",
                IsActive = true
            });

            var q = new ProductQuestion
            {
                Id = 1,
                ProductId = 10,
                AuthorName = "Lan Hương",
                Content = "Kem này có nâng tông không?",
                IsApproved = true,
                IsHidden = false,
                CreatedAt = DateTime.UtcNow.AddHours(-5),
                Answers = new List<ProductAnswer>
                {
                    new ProductAnswer
                    {
                        Id = 1,
                        QuestionId = 1,
                        AuthorName = "Khách mua trước",
                        Content = "Nâng nhẹ bạn ơi",
                        IsOfficial = false,
                        IsApproved = true,
                        CreatedAt = DateTime.UtcNow.AddHours(-4)
                    },
                    new ProductAnswer
                    {
                        Id = 2,
                        QuestionId = 1,
                        AuthorName = "Dược sĩ Canmake Beauty",
                        Content = "Chào bạn, dòng này nâng 0.5-1 tông tự nhiên nhé!",
                        IsOfficial = true,
                        IsApproved = true,
                        CreatedAt = DateTime.UtcNow.AddHours(-1)
                    }
                }
            };
            db.ProductQuestions.Add(q);
            await db.SaveChangesAsync();

            var service = new ProductQnaService(db, NullLogger<ProductQnaService>.Instance);

            // Act
            var list = await service.GetApprovedQuestionsAsync(10);

            // Assert
            Assert.Single(list);
            Assert.Equal(2, list[0].Answers.Count);
            Assert.True(list[0].Answers[0].IsOfficial);
            Assert.Equal("Dược sĩ Canmake Beauty", list[0].Answers[0].AuthorName);
        }

        [Fact]
        public async Task VoteHelpfulWithCheck_Should_Prevent_Duplicate_Votes_By_Same_User()
        {
            // Arrange
            var db = CreateInMemoryDbContext(nameof(VoteHelpfulWithCheck_Should_Prevent_Duplicate_Votes_By_Same_User));
            db.ProductReviews.Add(new ProductReview
            {
                Id = 50,
                ProductId = 1,
                Rating = 5,
                Content = "Rất tuyệt vời!",
                HelpfulCount = 3,
                IsApproved = true
            });
            await db.SaveChangesAsync();

            var service = new ProductReviewService(db, NullLogger<ProductReviewService>.Instance);

            // Act 1: First vote by user 99
            var firstVote = await service.VoteHelpfulWithCheckAsync(50, 99, null);

            // Act 2: Duplicate vote by same user 99
            var secondVote = await service.VoteHelpfulWithCheckAsync(50, 99, null);

            // Assert
            Assert.True(firstVote.Success);
            Assert.Equal(4, firstVote.NewCount);

            Assert.False(secondVote.Success);
            Assert.Equal(4, secondVote.NewCount);
            Assert.Contains("đã bình chọn", secondVote.Message);
        }

        [Fact]
        public async Task VoteHelpfulWithCheck_Should_Prevent_Duplicate_Votes_By_Same_AnonymousKey()
        {
            // Arrange
            var db = CreateInMemoryDbContext(nameof(VoteHelpfulWithCheck_Should_Prevent_Duplicate_Votes_By_Same_AnonymousKey));
            db.ProductReviews.Add(new ProductReview
            {
                Id = 60,
                ProductId = 1,
                Rating = 5,
                Content = "Thấm nhanh không bết rít!",
                HelpfulCount = 10,
                IsApproved = true
            });
            await db.SaveChangesAsync();

            var service = new ProductReviewService(db, NullLogger<ProductReviewService>.Instance);

            // Act 1: First vote by anon_token_xyz
            var firstVote = await service.VoteHelpfulWithCheckAsync(60, null, "anon_token_xyz");

            // Act 2: Second vote by same anon_token_xyz
            var secondVote = await service.VoteHelpfulWithCheckAsync(60, null, "anon_token_xyz");

            // Assert
            Assert.True(firstVote.Success);
            Assert.Equal(11, firstVote.NewCount);

            Assert.False(secondVote.Success);
            Assert.Equal(11, secondVote.NewCount);
        }

        [Fact]
        public async Task GetProductReviews_Should_Filter_By_SkinTwin_And_Mark_IsSkinTwinMatch()
        {
            // Arrange
            var db = CreateInMemoryDbContext(nameof(GetProductReviews_Should_Filter_By_SkinTwin_And_Mark_IsSkinTwinMatch));
            db.Products.Add(new Product
            {
                Id = 7,
                Name = "Gel Rửa Mặt",
                Slug = "gel-rua-mat",
                Sku = "GRM7",
                Barcode = "777",
                IsActive = true
            });

            db.ProductReviews.AddRange(
                new ProductReview
                {
                    Id = 1,
                    ProductId = 7,
                    Rating = 5,
                    SkinType = "Da Dầu / Mụn",
                    Content = "Kiềm dầu đỉnh",
                    IsApproved = true,
                    IsHidden = false
                },
                new ProductReview
                {
                    Id = 2,
                    ProductId = 7,
                    Rating = 4,
                    SkinType = "Da Khô",
                    Content = "Hơi căng da nhẹ",
                    IsApproved = true,
                    IsHidden = false
                },
                new ProductReview
                {
                    Id = 3,
                    ProductId = 7,
                    Rating = 5,
                    SkinType = "Da Dầu",
                    Content = "Sạch sâu",
                    IsApproved = true,
                    IsHidden = false
                }
            );
            await db.SaveChangesAsync();

            var service = new ProductReviewService(db, NullLogger<ProductReviewService>.Instance);

            // Act
            var reviews = await service.GetProductReviewsAsync(7, 1, 10, null, null, "Da Dầu", "newest");

            // Assert
            Assert.Equal(2, reviews.Count);
            Assert.All(reviews, r => Assert.True(r.IsSkinTwinMatch));
        }

        [Fact]
        public async Task GetProductReviews_Should_Sort_By_MostHelpful()
        {
            // Arrange
            var db = CreateInMemoryDbContext(nameof(GetProductReviews_Should_Sort_By_MostHelpful));
            db.Products.Add(new Product
            {
                Id = 8,
                Name = "Toner",
                Slug = "toner-8",
                Sku = "TN8",
                Barcode = "888",
                IsActive = true
            });

            db.ProductReviews.AddRange(
                new ProductReview { Id = 1, ProductId = 8, Rating = 5, HelpfulCount = 2, IsApproved = true, CreatedAt = DateTime.UtcNow },
                new ProductReview { Id = 2, ProductId = 8, Rating = 4, HelpfulCount = 25, IsApproved = true, CreatedAt = DateTime.UtcNow.AddDays(-2) },
                new ProductReview { Id = 3, ProductId = 8, Rating = 5, HelpfulCount = 10, IsApproved = true, CreatedAt = DateTime.UtcNow.AddDays(-1) }
            );
            await db.SaveChangesAsync();

            var service = new ProductReviewService(db, NullLogger<ProductReviewService>.Instance);

            // Act
            var sorted = await service.GetProductReviewsAsync(8, 1, 10, null, null, null, "helpful");

            // Assert
            Assert.Equal(3, sorted.Count);
            Assert.Equal(25, sorted[0].HelpfulCount);
            Assert.Equal(10, sorted[1].HelpfulCount);
            Assert.Equal(2, sorted[2].HelpfulCount);
        }

        [Fact]
        public async Task GetCustomerReviewImages_Should_Extract_All_Real_Photos_For_Swatch_Gallery()
        {
            // Arrange
            var db = CreateInMemoryDbContext(nameof(GetCustomerReviewImages_Should_Extract_All_Real_Photos_For_Swatch_Gallery));
            db.ProductReviews.AddRange(
                new ProductReview
                {
                    Id = 10,
                    ProductId = 9,
                    Rating = 5,
                    SkinType = "Da Hỗn Hợp",
                    ImagesJson = JsonSerializer.Serialize(new List<string> { "/uploads/reviews/img1.jpg", "/uploads/reviews/img2.jpg" }),
                    IsApproved = true,
                    IsHidden = false,
                    CreatedAt = DateTime.UtcNow
                },
                new ProductReview
                {
                    Id = 11,
                    ProductId = 9,
                    Rating = 4,
                    SkinType = "Da Khô",
                    ImagesJson = JsonSerializer.Serialize(new List<string> { "/uploads/reviews/img3.webp" }),
                    IsApproved = true,
                    IsHidden = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-2)
                }
            );
            await db.SaveChangesAsync();

            var service = new ProductReviewService(db, NullLogger<ProductReviewService>.Instance);

            // Act
            var gallery = await service.GetCustomerReviewImagesAsync(9, 12);

            // Assert
            Assert.Equal(3, gallery.Count);
            Assert.Contains(gallery, g => g.ImageUrl == "/uploads/reviews/img1.jpg" && g.SkinType == "Da Hỗn Hợp");
            Assert.Contains(gallery, g => g.ImageUrl == "/uploads/reviews/img3.webp" && g.Rating == 4);
        }
    }
}

