using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.BeautyTech
{
    public class SkinQuizRequestDto
    {
        public string SkinType { get; set; } = string.Empty; // da-dau, da-kho, da-nhay-cam, da-hon-hop, da-thuong
        public string SkinConcern { get; set; } = string.Empty; // acne, anti_aging, brightening, hydration, pore_minimizing
        public string? BudgetTier { get; set; } = "standard"; // economy, standard, premium
    }

    public class RoutineProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public decimal EffectivePrice => DiscountPrice.HasValue && DiscountPrice.Value > 0 && DiscountPrice.Value < Price
            ? DiscountPrice.Value
            : Price;
        public string? BrandName { get; set; }
        public List<string> KeyIngredients { get; set; } = new();
        public List<string> SafetyBadges { get; set; } = new();
        public string StepCode { get; set; } = string.Empty;
    }

    public class RoutineStepDto
    {
        public int StepOrder { get; set; } // 1, 2, 3, 4
        public string StepCode { get; set; } = string.Empty; // 1_Cleanser, 2_Toner, 3_Serum, 4_Moisturizer
        public string StepName { get; set; } = string.Empty; // e.g. "Bước 1: Làm sạch sâu (Cleanser)"
        public string Subtitle { get; set; } = string.Empty; // e.g. "Làm sạch bã nhờn, thông thoáng lỗ chân lông"
        public string UsageGuide { get; set; } = string.Empty; // e.g. "Dùng sáng và tối, mát-xa nhẹ nhàng 60s"
        public RoutineProductDto? SelectedProduct { get; set; }
    }

    public class RoutineRecommendationResultDto
    {
        public string SkinType { get; set; } = string.Empty;
        public string SkinTypeName { get; set; } = string.Empty;
        public string SkinConcern { get; set; } = string.Empty;
        public string SkinConcernName { get; set; } = string.Empty;
        public string RoutineTitle { get; set; } = string.Empty;
        public string DermatologistAdvice { get; set; } = string.Empty;
        public List<RoutineStepDto> Steps { get; set; } = new();

        public decimal OriginalTotalPrice { get; set; }
        public decimal ComboDiscountPercent { get; set; } = 15m;
        public decimal ComboDiscountAmount { get; set; }
        public decimal FinalComboPrice { get; set; }
        public string ComboCouponCode { get; set; } = "COMBO15";
        public int TotalSteps => Steps.Count;
    }

    public class AddRoutineToCartRequestDto
    {
        public List<int> ProductIds { get; set; } = new();
        public string CouponCode { get; set; } = "COMBO15";
    }
}

