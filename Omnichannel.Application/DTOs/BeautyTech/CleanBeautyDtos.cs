using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.BeautyTech
{
    public class ParsedIngredientDto
    {
        public string Name { get; set; } = string.Empty;
        public string InciCategory { get; set; } = "BASE"; // KEY_ACTIVE, NATURAL_EXTRACT, BASE
        public string BadgeColorClass { get; set; } = "bg-slate-100 text-slate-700 border-slate-200";
        public string BadgeIcon { get; set; } = "fa-solid fa-flask";
        public string Function { get; set; } = "Thành phần dưỡng ẩm & dung môi";
        public int EwgScore { get; set; } = 1;
        public bool IsKeyActive => InciCategory == "KEY_ACTIVE";
        public bool IsNaturalExtract => InciCategory == "NATURAL_EXTRACT";
    }

    public class CleanBeautySummaryDto
    {
        public bool IsAlcoholFree { get; set; }
        public bool IsFragranceFree { get; set; }
        public bool IsPregnancySafe { get; set; }
        public int KeyActivesCount { get; set; }
        public int NaturalExtractsCount { get; set; }
        public int TotalIngredientsCount { get; set; }
        public List<ParsedIngredientDto> Ingredients { get; set; } = new();
    }
}

