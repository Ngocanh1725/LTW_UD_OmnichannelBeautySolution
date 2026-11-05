using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.BeautyTech;
using Omnichannel.Application.Interfaces.Beauty;

namespace Omnichannel.Infrastructure.Services.Beauty
{
    /// <summary>
    /// Triển khai phân tích da bằng Gemini Vision API (đọc API key từ IConfiguration / biến môi trường GEMINI_API_KEY).
    /// Tự động timeout sau 15 giây hoặc khi không có mạng / không có API key để chuyển mượt mà về MockSkinAnalysisService.
    /// </summary>
    public class AiVisionSkinAnalysisService : ISkinAnalysisService
    {
        public static readonly TimeSpan VisionApiTimeout = TimeSpan.FromSeconds(15);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly MockSkinAnalysisService _mockFallbackService;
        private readonly ILogger<AiVisionSkinAnalysisService> _logger;

        public AiVisionSkinAnalysisService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            MockSkinAnalysisService mockFallbackService,
            ILogger<AiVisionSkinAnalysisService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _mockFallbackService = mockFallbackService;
            _logger = logger;
        }

        public async Task<SkinAnalysisResultDto> AnalyzeSkinImageAsync(
            byte[] imageBytes,
            string? fileName = null,
            string? contentType = null,
            CancellationToken ct = default)
        {
            // 1. Kiểm tra định dạng Magic Bytes & giới hạn <= 5MB trước tiên
            var validation = MockSkinAnalysisService.ValidateImageBytes(imageBytes, fileName, contentType);
            if (!validation.IsValid)
            {
                return new SkinAnalysisResultDto
                {
                    Success = false,
                    Message = validation.ErrorMessage,
                    ProviderUsed = "ValidationGuard"
                };
            }

            // 2. Lấy API Key từ cấu hình / User Secrets / Biến môi trường (Tuyệt đối không hardcode)
            var apiKey = _configuration["Gemini:ApiKey"]
                         ?? _configuration["GEMINI_API_KEY"]
                         ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogInformation("Không tìm thấy cấu hình Gemini:ApiKey / GEMINI_API_KEY. Tự động sử dụng MockSkinAnalysisService (Deterministic SHA-256).");
                return await _mockFallbackService.AnalyzeSkinImageAsync(imageBytes, fileName, contentType, ct);
            }

            // 3. Gọi Gemini Vision API với timeout 15 giây
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(VisionApiTimeout);

            try
            {
                var base64Image = Convert.ToBase64String(imageBytes);
                var prompt =
                    "Bạn là chuyên gia phân tích da liễu AI của Canmake Beauty. Hãy phân tích ảnh khuôn mặt này và CHỈ trả về đúng một chuỗi JSON hợp lệ theo cấu trúc: " +
                    "{ \"skinType\": \"Da dầu | Da khô | Da hỗn hợp | Da nhạy cảm\", " +
                    "\"concerns\": [\"Vấn đề 1\", \"Vấn đề 2\", \"Vấn đề 3\"], " +
                    "\"scores\": { \"oil\": 0-100, \"hydration\": 0-100, \"acne\": 0-100, \"pigmentation\": 0-100, \"sensitivity\": 0-100 } }";

                var payload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = prompt },
                                new
                                {
                                    inline_data = new
                                    {
                                        mime_type = validation.DetectedMimeType,
                                        data = base64Image
                                    }
                                }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.2,
                        responseMimeType = "application/json"
                    }
                };

                var client = _httpClientFactory.CreateClient("GeminiVisionClient");
                var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={Uri.EscapeDataString(apiKey.Trim())}";

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };

                using var response = await client.SendAsync(request, timeoutCts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Gemini Vision API trả về mã lỗi HTTP {StatusCode}. Chuyển sang MockSkinAnalysisService.", (int)response.StatusCode);
                    return await _mockFallbackService.AnalyzeSkinImageAsync(imageBytes, fileName, contentType, ct);
                }

                var rawBody = await response.Content.ReadAsStringAsync(timeoutCts.Token);
                using var doc = JsonDocument.Parse(rawBody);
                var textJson = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrWhiteSpace(textJson))
                {
                    return await _mockFallbackService.AnalyzeSkinImageAsync(imageBytes, fileName, contentType, ct);
                }

                // Làm sạch markdown fence nếu có
                var cleanJson = textJson.Trim();
                if (cleanJson.StartsWith("```"))
                {
                    var firstNewline = cleanJson.IndexOf('\n');
                    if (firstNewline > 0) cleanJson = cleanJson[(firstNewline + 1)..];
                    if (cleanJson.EndsWith("```")) cleanJson = cleanJson[..^3];
                    cleanJson = cleanJson.Trim();
                }

                var parsed = JsonSerializer.Deserialize<SkinAnalysisResultDto>(cleanJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (parsed?.Scores == null)
                {
                    return await _mockFallbackService.AnalyzeSkinImageAsync(imageBytes, fileName, contentType, ct);
                }

                var hashHex = Convert.ToHexString(SHA256.HashData(imageBytes));
                return await _mockFallbackService.BuildEnrichedResultAsync(
                    parsed.Scores,
                    hashHex,
                    providerUsed: "GeminiVisionAI",
                    isFallback: false,
                    customSkinTypeLabel: parsed.SkinType,
                    customConcerns: parsed.Concerns,
                    ct: ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi hoặc quá thời gian chờ (15s) khi gọi Gemini Vision API. Tự động fallback sang MockSkinAnalysisService.");
                return await _mockFallbackService.AnalyzeSkinImageAsync(imageBytes, fileName, contentType, ct);
            }
        }
    }
}
