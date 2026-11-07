using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Omnichannel.Application.DTOs.BeautyTech
{
    public class SkinAnalysisScoresDto
    {
        [JsonPropertyName("oil")]
        public int Oil { get; set; }

        [JsonPropertyName("hydration")]
        public int Hydration { get; set; }

        [JsonPropertyName("acne")]
        public int Acne { get; set; }

        [JsonPropertyName("pigmentation")]
        public int Pigmentation { get; set; }

        [JsonPropertyName("sensitivity")]
        public int Sensitivity { get; set; }
    }

    public class SkinAnalysisResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ProviderUsed { get; set; } = "DeterministicMockFallback";
        public bool IsFallback { get; set; }
        public string ImageSha256Hash { get; set; } = string.Empty;

        [JsonPropertyName("skinType")]
        public string SkinType { get; set; } = string.Empty; // Display label e.g. "Da dầu (Oily Skin)"

        public string SkinTypeCode { get; set; } = string.Empty; // da-dau, da-kho, da-nhay-cam, da-hon-hop, da-thuong
        public string PrimaryConcernCode { get; set; } = string.Empty; // acne, anti_aging, brightening, hydration, pore_minimizing

        [JsonPropertyName("concerns")]
        public List<string> Concerns { get; set; } = new();

        [JsonPropertyName("scores")]
        public SkinAnalysisScoresDto Scores { get; set; } = new();

        public string SummaryAssessment { get; set; } = string.Empty;
        public string PrivacyNotice { get; set; } = "Ảnh selfie của bạn chỉ được xử lý tức thì trong bộ nhớ RAM và đã được xóa vĩnh viễn ngay sau khi hoàn tất phân tích (không lưu trên máy chủ).";
        public string MedicalDisclaimer { get; set; } = "Kết quả mang tính tham khảo, không thay thế chẩn đoán da liễu.";

        public RoutineRecommendationResultDto? RecommendedRoutine { get; set; }
    }
}
