using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Omnichannel.Application.DTOs.BeautyTech;
using Omnichannel.Application.Interfaces.Beauty;
using Omnichannel.Infrastructure.Services.Beauty;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class SkinAnalysisAndTryOnTests
    {
        private static byte[] CreateValidPngBytes(int payloadSeed = 1)
        {
            // Magic bytes chuẩn của PNG: 89 50 4E 47 0D 0A 1A 0A
            var bytes = new byte[64];
            bytes[0] = 0x89;
            bytes[1] = 0x50;
            bytes[2] = 0x4E;
            bytes[3] = 0x47;
            bytes[4] = 0x0D;
            bytes[5] = 0x0A;
            bytes[6] = 0x1A;
            bytes[7] = 0x0A;
            for (int i = 8; i < bytes.Length; i++)
            {
                bytes[i] = (byte)((i * 17 + payloadSeed) % 256);
            }
            return bytes;
        }

        private static Mock<ISkincareConsultantService> CreateMockConsultantService()
        {
            var mock = new Mock<ISkincareConsultantService>();
            mock.Setup(s => s.GenerateRoutineAsync(It.IsAny<SkinQuizRequestDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SkinQuizRequestDto req, CancellationToken _) => new RoutineRecommendationResultDto
                {
                    SkinType = req.SkinType,
                    SkinConcern = req.SkinConcern,
                    RoutineTitle = $"Routine 4 Bước Dành Cho {req.SkinType}",
                    Steps = new List<RoutineStepDto>
                    {
                        new() { StepOrder = 1, StepCode = "1_Cleanser", StepName = "Bước 1: Sữa rửa mặt" },
                        new() { StepOrder = 2, StepCode = "2_Toner", StepName = "Bước 2: Nước cân bằng (Toner)" },
                        new() { StepOrder = 3, StepCode = "3_Serum", StepName = "Bước 3: Tinh chất đặc trị (Serum)" },
                        new() { StepOrder = 4, StepCode = "4_Moisturizer", StepName = "Bước 4: Kem dưỡng khóa ẩm" }
                    }
                });
            return mock;
        }

        [Fact]
        public async Task AnalyzeSkinImage_WhenOfflineOrNetworkFails_FallsBackToDeterministicMockResult()
        {
            // Arrange: Giả lập có cấu hình key qua biến cấu hình tạm nhưng mất kết nối mạng (HttpRequestException)
            var consultantMock = CreateMockConsultantService();
            var mockService = new MockSkinAnalysisService(consultantMock.Object);

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Simulated network offline error"));

            var httpClient = new HttpClient(handlerMock.Object);
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var inMemoryConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Gemini:ApiKey"] = "TEST_DUMMY_ENV_KEY_FOR_FALLBACK_VERIFICATION"
                })
                .Build();

            var aiVisionService = new AiVisionSkinAnalysisService(
                httpClientFactoryMock.Object,
                inMemoryConfig,
                mockService,
                NullLogger<AiVisionSkinAnalysisService>.Instance);

            var pngBytes = CreateValidPngBytes(42);

            // Act
            var result1 = await aiVisionService.AnalyzeSkinImageAsync(pngBytes, "selfie.png", "image/png");
            var result2 = await aiVisionService.AnalyzeSkinImageAsync(pngBytes, "selfie.png", "image/png");

            // Assert: Fallback hoạt động mượt mà và kết quả deterministic (2 lần chạy cùng 1 ảnh ra đúng cùng số điểm)
            Assert.True(result1.Success);
            Assert.True(result1.IsFallback);
            Assert.Equal("DeterministicMockFallback", result1.ProviderUsed);
            Assert.NotNull(result1.Scores);
            Assert.InRange(result1.Scores.Oil, 0, 100);
            Assert.InRange(result1.Scores.Hydration, 0, 100);
            Assert.InRange(result1.Scores.Acne, 0, 100);
            Assert.InRange(result1.Scores.Pigmentation, 0, 100);
            Assert.InRange(result1.Scores.Sensitivity, 0, 100);
            Assert.NotNull(result1.RecommendedRoutine);
            Assert.Equal(4, result1.RecommendedRoutine!.Steps.Count);

            // Cùng 1 bức ảnh -> SHA-256 hash & 5 điểm số giống hệt nhau
            Assert.Equal(result1.ImageSha256Hash, result2.ImageSha256Hash);
            Assert.Equal(result1.Scores.Oil, result2.Scores.Oil);
            Assert.Equal(result1.Scores.Hydration, result2.Scores.Hydration);
            Assert.Equal(result1.Scores.Acne, result2.Scores.Acne);
        }

        [Fact]
        public async Task AnalyzeSkinImage_NonImageFileOrFakeExtension_IsRejected()
        {
            // Arrange
            var consultantMock = CreateMockConsultantService();
            var mockService = new MockSkinAnalysisService(consultantMock.Object);

            // Nội dung text/script giả mạo tên file .png (không có Magic Bytes ảnh)
            var fakeBytes = Encoding.UTF8.GetBytes("This is a plain text file pretending to be an image.");

            // Act
            var resultFakeMagic = await mockService.AnalyzeSkinImageAsync(fakeBytes, "fake_photo.png", "image/png");
            var resultBadExt = await mockService.AnalyzeSkinImageAsync(CreateValidPngBytes(1), "malware.exe", "application/octet-stream");

            // Assert
            Assert.False(resultFakeMagic.Success);
            Assert.Contains("Magic Bytes", resultFakeMagic.Message, StringComparison.OrdinalIgnoreCase);

            Assert.False(resultBadExt.Success);
            Assert.Contains("JPG, PNG hoặc WEBP", resultBadExt.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AnalyzeSkinImage_FileLargerThan5MB_IsRejected()
        {
            // Arrange
            var consultantMock = CreateMockConsultantService();
            var mockService = new MockSkinAnalysisService(consultantMock.Object);

            // Tạo mảng byte 5MB + 1 byte (5,242,881 bytes) có đầu PNG hợp lệ
            var oversizedBytes = new byte[MockSkinAnalysisService.MaxImageSizeBytes + 1];
            oversizedBytes[0] = 0x89;
            oversizedBytes[1] = 0x50;
            oversizedBytes[2] = 0x4E;
            oversizedBytes[3] = 0x47;

            // Act
            var result = await mockService.AnalyzeSkinImageAsync(oversizedBytes, "huge_selfie.png", "image/png");

            // Assert
            Assert.False(result.Success);
            Assert.Contains("5MB", result.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}

