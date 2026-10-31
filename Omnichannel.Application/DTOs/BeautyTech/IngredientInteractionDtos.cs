using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.BeautyTech
{
    public sealed record IngredientInteractionFindingDto(
        string InteractionType,       // HIGH_CONFLICT | CHEMICAL_DEACTIVATION | REDOX_CONFLICT | GOLD_SYNERGY | BARRIER_SYNERGY
        string SeverityBadge,         // DANGER | WARNING | SUCCESS_SYNERGY
        string IngredientA,
        string ProductAName,
        string IngredientB,
        string ProductBName,
        string ScientificExplanation,
        string RecommendedResolution);

    public sealed record SkincareRoutineAnalysisDto(
        bool HasCriticalConflicts,
        bool HasAllergyAlerts,
        List<string> AllergyWarnings,
        List<IngredientInteractionFindingDto> Interactions,
        List<string> MorningRoutineSteps,
        List<string> EveningRoutineSteps,
        List<string> AlternateNightNotes,
        List<IngredientSynergyCrossSellDto> SynergyCrossSellSuggestions);

    public sealed record IngredientSynergyCrossSellDto(
        int ProductId,
        string ProductName,
        string Slug,
        decimal Price,
        string? ThumbnailUrl,
        string KeyIngredient,
        string SynergyReason);

    public class ConflictCheckRequestDto
    {
        public List<int> ProductIds { get; set; } = new();
        public List<string> IngredientNames { get; set; } = new();
        public int? UserId { get; set; }
    }
}
