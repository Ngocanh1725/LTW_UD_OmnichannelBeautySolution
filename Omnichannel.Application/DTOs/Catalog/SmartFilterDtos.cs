using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Catalog
{
    public class SmartFilterCriteriaDto
    {
        public string? SkinType { get; set; } // e.g. "da-dau", "da-kho", "da-hon-hop", "da-nhay-cam", "da-thuong", "oily", "dry"...
        public List<string> Concerns { get; set; } = new(); // e.g. "hydration", "sun_protection", "acne", "anti_aging", "brightening", "pore_minimizing"
        public decimal? Budget { get; set; } // e.g. 500000
        public string? BudgetBracket { get; set; } // "under_300k", "300k_500k", "500k_1m", "above_1m"
        public int Limit { get; set; } = 12;
    }

    public class SmartFilterProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public string? ThumbnailUrl { get; set; }
        public double Rating { get; set; } = 5.0;
        public int ReviewCount { get; set; } = 0;
        public int MatchScore { get; set; } = 95; // e.g. 98%, 95%, 90%
        public List<string> MatchReasons { get; set; } = new();
        public List<string> MatchedSkinTypes { get; set; } = new();
        public List<string> MatchedConcerns { get; set; } = new();
        public List<string> KeyIngredients { get; set; } = new();
    }

    public class SmartFilterResultDto
    {
        public bool Success { get; set; } = true;
        public int TotalMatched { get; set; }
        public SmartFilterCriteriaDto Criteria { get; set; } = new();
        public List<SmartFilterProductDto> Products { get; set; } = new();
        public List<string> RecommendedRoutine { get; set; } = new();
    }

    public class ProductAttributeManageDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ThumbnailUrl { get; set; }
        public List<int> AssignedSkinTypeIds { get; set; } = new();
        public List<string> AssignedSkinTypeNames { get; set; } = new();
        public List<int> AssignedConcernIds { get; set; } = new();
        public List<string> AssignedConcernNames { get; set; } = new();
    }

    public class BulkUpdateAttributesDto
    {
        public List<int> ProductIds { get; set; } = new();
        public List<int> SkinTypeIds { get; set; } = new();
        public List<int> ConcernIds { get; set; } = new();
        public string Mode { get; set; } = "ADD"; // "ADD" (thêm vào thuộc tính hiện tại) hoặc "REPLACE" (ghi đè toàn bộ)
    }

    public class ProductAttributesViewModel
    {
        public List<ProductAttributeManageDto> Products { get; set; } = new();
        public List<SkinTypeDto> AvailableSkinTypes { get; set; } = new();
        public List<SkinConcernDto> AvailableConcerns { get; set; } = new();
        public int TotalProducts { get; set; }
    }

    public class SkinTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
    }

    public class SkinConcernDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Description { get; set; }
    }

    // --- CÁC DTO CHẨN ĐOÁN LÀN DA AI (BEAUTY TECH) ---
    public class SkinDiagnosticRequestDto
    {
        public string NoonFeeling { get; set; } = "tzone_oily"; // oily_all, dry_tight, tzone_oily, normal_smooth
        public string PrimaryConcern { get; set; } = "acne_pore"; // acne_pore, pigmentation, wrinkle_aging, sensitive_redness
        public string AgeGroup { get; set; } = "20_29"; // under_20, 20_29, 30_39, above_40
        public string Environment { get; set; } = "office_ac"; // office_ac, outdoor_sun, screen_blue_light, fresh_clean
        public string BudgetBracket { get; set; } = "300k_500k"; // under_300k, 300k_500k, 500k_1m, above_1m
        public bool IsPregnantOrNursing { get; set; } = false;
    }

    public class SkinRadarScoresDto
    {
        public int Hydration { get; set; } = 70;      // Độ ẩm tầng sâu (0-100)
        public int SebumBalance { get; set; } = 65;   // Cân bằng tuyến dầu (0-100)
        public int Firmness { get; set; } = 80;       // Độ đàn hồi săn chắc (0-100)
        public int BarrierStrength { get; set; } = 60;// Sức đề kháng hàng rào bảo vệ (0-100)
        public int Radiance { get; set; } = 75;       // Độ sáng mịn đều màu (0-100)
    }

    public class IngredientAdviceDto
    {
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Icon { get; set; } = "fa-check";
    }

    public class SkinDiagnosticResultDto
    {
        public int OverallHealthScore { get; set; } = 75;
        public string SkinClassification { get; set; } = string.Empty;
        public string SkinSubtype { get; set; } = string.Empty;
        public string ExpertSummary { get; set; } = string.Empty;
        public bool IsPregnancySafeApplied { get; set; }
        public string BudgetBracket { get; set; } = "300k_500k";
        public SkinRadarScoresDto RadarScores { get; set; } = new();
        public List<IngredientAdviceDto> RecommendedIngredients { get; set; } = new();
        public List<IngredientAdviceDto> AvoidIngredients { get; set; } = new();
        public List<SmartFilterProductDto> RecommendedProducts { get; set; } = new();
        public Omnichannel.Application.DTOs.BeautyTech.SkincareRoutineAnalysisDto? ConflictAnalysis { get; set; }
        public string RoutineMorning { get; set; } = string.Empty;
        public string RoutineEvening { get; set; } = string.Empty;
    }

    // --- PERSONALIZED ROUTINE BUILDER DTOs ---
    public class PersonalizedRoutineRequestDto
    {
        public string SkinType { get; set; } = "da-dau"; // da-dau, da-kho, da-hon-hop, da-nhay-cam, da-thuong
        public string Concern { get; set; } = "acne_pore"; // acne_pore, pigmentation, aging, recovery
        public string BudgetTier { get; set; } = "standard"; // economy, standard, luxury
    }

    public class RoutineStepItemDto
    {
        public int StepNumber { get; set; }
        public string StepTitle { get; set; } = string.Empty;
        public string StepCategory { get; set; } = string.Empty;
        public string TimeOfDay { get; set; } = "morning"; // morning / night
        public string ClinicalTip { get; set; } = string.Empty;
        public string BestUsage { get; set; } = string.Empty;
        public SmartFilterProductDto? Product { get; set; }
        public bool IsSharedWithOtherRoutine { get; set; } = false;
    }

    public class PersonalizedRoutineResponseDto
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public string SkinTypeName { get; set; } = string.Empty;
        public string ConcernName { get; set; } = string.Empty;
        public string BudgetTierKey { get; set; } = "standard";
        public string BudgetTierName { get; set; } = string.Empty;
        public string BudgetTierDescription { get; set; } = string.Empty;
        public string RoutinePhilosophy { get; set; } = string.Empty;
        public List<RoutineStepItemDto> MorningRoutine { get; set; } = new();
        public List<RoutineStepItemDto> NightRoutine { get; set; } = new();
        public List<SmartFilterProductDto> DistinctProducts { get; set; } = new();
        public int TotalItemsCount { get; set; }
        public decimal TotalOriginalPrice { get; set; }
        public decimal DiscountComboPercent { get; set; } = 10m;
        public decimal DiscountComboAmount { get; set; }
        public decimal ComboFinalPrice { get; set; }
    }

    public class SaveRoutineRequestDto
    {
        public string RoutineName { get; set; } = string.Empty;
        public string SkinType { get; set; } = string.Empty;
        public string Concern { get; set; } = string.Empty;
        public string BudgetTier { get; set; } = string.Empty;
        public List<int> ProductIds { get; set; } = new();
    }

    public class AddRoutineToCartRequestDto
    {
        public List<int> ProductIds { get; set; } = new();
        public decimal DiscountPercent { get; set; } = 10m;
    }
}
