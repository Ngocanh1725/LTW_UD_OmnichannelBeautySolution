using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.BeautyTech;
using Omnichannel.Application.Interfaces.Beauty;

namespace Omnichannel.Infrastructure.Services.Beauty
{
    /// <summary>
    /// Triển khai phân tích da mô phỏng xác định (Deterministic) theo SHA-256 hash của mảng byte ảnh.
    /// Đảm bảo luôn trả kết quả nhất quán cho cùng 1 bức ảnh ngay cả khi không có API key hoặc mất mạng.
    /// </summary>
    public class MockSkinAnalysisService : ISkinAnalysisService
    {
        public const int MaxImageSizeBytes = 5 * 1024 * 1024; // 5 MB

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        private readonly ISkincareConsultantService _skincareConsultantService;

        public MockSkinAnalysisService(ISkincareConsultantService skincareConsultantService)
        {
            _skincareConsultantService = skincareConsultantService;
        }

        public static (bool IsValid, string ErrorMessage, string DetectedMimeType) ValidateImageBytes(
            byte[]? imageBytes,
            string? fileName = null,
            string? contentType = null)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                return (false, "Vui lòng tải lên một tệp ảnh hợp lệ (tệp trống).", string.Empty);
            }

            if (imageBytes.Length > MaxImageSizeBytes)
            {
                return (false, "Dung lượng ảnh vượt quá giới hạn tối đa 5MB. Vui lòng chọn ảnh nhỏ hơn 5MB.", string.Empty);
            }

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                var ext = Path.GetExtension(fileName);
                if (!string.IsNullOrEmpty(ext) && !AllowedExtensions.Contains(ext))
                {
                    return (false, "Định dạng tệp không được hỗ trợ. Chỉ chấp nhận ảnh JPG, PNG hoặc WEBP.", string.Empty);
                }
            }

            if (!string.IsNullOrWhiteSpace(contentType))
            {
                var normalizedMime = contentType.Trim().ToLowerInvariant();
                if (!normalizedMime.StartsWith("image/jpeg") &&
                    !normalizedMime.StartsWith("image/jpg") &&
                    !normalizedMime.StartsWith("image/png") &&
                    !normalizedMime.StartsWith("image/webp") &&
                    normalizedMime != "application/octet-stream")
                {
                    return (false, "Kiểu MIME không phải là ảnh hợp lệ. Chỉ chấp nhận image/jpeg, image/png hoặc image/webp.", string.Empty);
                }
            }

            // Kiểm tra Magic Bytes thực tế của file ảnh
            var isJpeg = imageBytes.Length >= 3 &&
                         imageBytes[0] == 0xFF &&
                         imageBytes[1] == 0xD8 &&
                         imageBytes[2] == 0xFF;

            var isPng = imageBytes.Length >= 8 &&
                        imageBytes[0] == 0x89 &&
                        imageBytes[1] == 0x50 &&
                        imageBytes[2] == 0x4E &&
                        imageBytes[3] == 0x47 &&
                        imageBytes[4] == 0x0D &&
                        imageBytes[5] == 0x0A &&
                        imageBytes[6] == 0x1A &&
                        imageBytes[7] == 0x0A;

            var isWebp = imageBytes.Length >= 12 &&
                         imageBytes[0] == 0x52 && // R
                         imageBytes[1] == 0x49 && // I
                         imageBytes[2] == 0x46 && // F
                         imageBytes[3] == 0x46 && // F
                         imageBytes[8] == 0x57 && // W
                         imageBytes[9] == 0x45 && // E
                         imageBytes[10] == 0x42 && // B
                         imageBytes[11] == 0x50;   // P

            if (!isJpeg && !isPng && !isWebp)
            {
                return (false, "Tệp tải lên không phải là ảnh thật (sai chữ ký Magic Bytes). Chỉ chấp nhận ảnh JPG, PNG hoặc WEBP.", string.Empty);
            }

            var detectedMime = isJpeg ? "image/jpeg" : (isPng ? "image/png" : "image/webp");
            return (true, string.Empty, detectedMime);
        }

        public async Task<SkinAnalysisResultDto> AnalyzeSkinImageAsync(
            byte[] imageBytes,
            string? fileName = null,
            string? contentType = null,
            CancellationToken ct = default)
        {
            var validation = ValidateImageBytes(imageBytes, fileName, contentType);
            if (!validation.IsValid)
            {
                return new SkinAnalysisResultDto
                {
                    Success = false,
                    Message = validation.ErrorMessage,
                    ProviderUsed = "ValidationGuard"
                };
            }

            // Tính SHA-256 hash để sinh điểm số xác định (Deterministic) theo nội dung ảnh
            var hashBytes = SHA256.HashData(imageBytes);
            var hashHex = Convert.ToHexString(hashBytes);

            // Trích xuất 5 chỉ số trong khoảng 22..92 từ các byte đầu của SHA-256
            int MapScore(byte b, int min = 24, int max = 92) => min + (b % (max - min + 1));

            var oilScore = MapScore(hashBytes[0], 28, 90);
            var hydrationScore = MapScore(hashBytes[1], 30, 88);
            var acneScore = MapScore(hashBytes[2], 18, 86);
            var pigmentationScore = MapScore(hashBytes[3], 20, 85);
            var sensitivityScore = MapScore(hashBytes[4], 22, 88);

            var scores = new SkinAnalysisScoresDto
            {
                Oil = oilScore,
                Hydration = hydrationScore,
                Acne = acneScore,
                Pigmentation = pigmentationScore,
                Sensitivity = sensitivityScore
            };

            return await BuildEnrichedResultAsync(
                scores,
                hashHex,
                providerUsed: "DeterministicMockFallback",
                isFallback: true,
                ct: ct);
        }

        public async Task<SkinAnalysisResultDto> BuildEnrichedResultAsync(
            SkinAnalysisScoresDto scores,
            string imageSha256Hash,
            string providerUsed,
            bool isFallback,
            string? customSkinTypeLabel = null,
            List<string>? customConcerns = null,
            CancellationToken ct = default)
        {
            // Chuẩn hóa giới hạn 0..100
            scores.Oil = Math.Clamp(scores.Oil, 0, 100);
            scores.Hydration = Math.Clamp(scores.Hydration, 0, 100);
            scores.Acne = Math.Clamp(scores.Acne, 0, 100);
            scores.Pigmentation = Math.Clamp(scores.Pigmentation, 0, 100);
            scores.Sensitivity = Math.Clamp(scores.Sensitivity, 0, 100);

            // Suy luận loại da từ 5 chỉ số
            string skinTypeCode;
            string skinTypeLabel;

            if (scores.Sensitivity >= 72)
            {
                skinTypeCode = "da-nhay-cam";
                skinTypeLabel = "Da nhạy cảm dễ kích ứng (Sensitive Skin)";
            }
            else if (scores.Oil >= 68 && scores.Hydration < 55)
            {
                skinTypeCode = "da-dau";
                skinTypeLabel = "Da dầu thiếu ẩm (Dehydrated Oily Skin)";
            }
            else if (scores.Oil >= 65)
            {
                skinTypeCode = "da-dau";
                skinTypeLabel = "Da dầu (Oily Skin)";
            }
            else if (scores.Hydration <= 45 && scores.Oil <= 45)
            {
                skinTypeCode = "da-kho";
                skinTypeLabel = "Da khô thiếu ẩm (Dry Skin)";
            }
            else
            {
                skinTypeCode = "da-hon-hop";
                skinTypeLabel = "Da hỗn hợp thiên dầu vùng chữ T (Combination Skin)";
            }

            if (!string.IsNullOrWhiteSpace(customSkinTypeLabel))
            {
                skinTypeLabel = customSkinTypeLabel;
            }

            // Xác định danh sách vấn đề nổi bật & mối quan tâm chính để ghép với Routine 4 bước
            var concerns = new List<string>();
            string primaryConcernCode = "hydration";

            var maxIssueScore = new[]
            {
                (Code: "acne", Score: scores.Acne, Label: "Mụn ẩn & Bít tắc lỗ chân lông vùng chữ T"),
                (Code: "brightening", Score: scores.Pigmentation, Label: "Thâm mụn & Sắc tố da không đều màu"),
                (Code: "pore_minimizing", Score: scores.Oil, Label: "Tuyến bã nhờn hoạt động mạnh & Lỗ chân lông to"),
                (Code: "hydration", Score: 100 - scores.Hydration, Label: "Độ ẩm biểu bì thiếu hụt & Hàng rào bảo vệ suy yếu"),
                (Code: "anti_aging", Score: scores.Sensitivity, Label: "Nền da nhạy cảm, dễ ửng đỏ trước tia UV & môi trường")
            }.OrderByDescending(x => x.Score).ToList();

            primaryConcernCode = maxIssueScore[0].Code;

            if (customConcerns != null && customConcerns.Count > 0)
            {
                concerns.AddRange(customConcerns.Where(c => !string.IsNullOrWhiteSpace(c)).Take(4));
            }
            else
            {
                foreach (var item in maxIssueScore.Take(3))
                {
                    concerns.Add(item.Label);
                }
            }

            // Gọi ISkincareConsultantService để lấy Routine 4 bước cá nhân hóa
            var routineRequest = new SkinQuizRequestDto
            {
                SkinType = skinTypeCode,
                SkinConcern = primaryConcernCode,
                BudgetTier = "standard"
            };

            var routine = await _skincareConsultantService.GenerateRoutineAsync(routineRequest, ct);

            var summaryAssessment =
                $"Phân tích đa điểm ảnh cho thấy nền da của bạn thuộc nhóm {skinTypeLabel} " +
                $"với chỉ số Dầu {scores.Oil}/100, Độ ẩm {scores.Hydration}/100, Mụn {scores.Acne}/100, " +
                $"Sắc tố {scores.Pigmentation}/100 và Độ nhạy cảm {scores.Sensitivity}/100. " +
                $"Ưu tiên hàng đầu là cân bằng dầu - nước và xử lý trọng tâm: {concerns.FirstOrDefault()}.";

            return new SkinAnalysisResultDto
            {
                Success = true,
                Message = "Phân tích làn da hoàn tất thành công.",
                ProviderUsed = providerUsed,
                IsFallback = isFallback,
                ImageSha256Hash = imageSha256Hash,
                SkinType = skinTypeLabel,
                SkinTypeCode = skinTypeCode,
                PrimaryConcernCode = primaryConcernCode,
                Concerns = concerns,
                Scores = scores,
                SummaryAssessment = summaryAssessment,
                RecommendedRoutine = routine
            };
        }
    }
}
