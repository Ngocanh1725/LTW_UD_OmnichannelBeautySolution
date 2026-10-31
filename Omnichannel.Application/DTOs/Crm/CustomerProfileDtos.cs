using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Crm
{
    public class SkinHistoryDto
    {
        public int Id { get; set; }
        public int CustomerProfileId { get; set; }
        public DateTime RecordedAt { get; set; }
        public string SkinType { get; set; } = string.Empty;
        public string? SkinCondition { get; set; }
        public string? TreatmentNote { get; set; }
        public int? RecordedBy { get; set; }
        public string? RecordedByName { get; set; }
        public List<string> ImageUrls { get; set; } = new();
    }

    public class AddSkinHistoryDto
    {
        public string SkinType { get; set; } = string.Empty;
        public string? SkinCondition { get; set; }
        public string? TreatmentNote { get; set; }
        public List<string>? ImageUrls { get; set; }
    }

    public class CustomerProfileDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? CurrentSkinType { get; set; }
        public List<string> SkinConcerns { get; set; } = new();
        public List<string> KnownAllergies { get; set; } = new();
        public List<string> PreferredBrands { get; set; } = new();
        public string? Note { get; set; }
        public DateTime? LastConsultationDate { get; set; }
        public int? ConsultantUserId { get; set; }
        public string? ConsultantName { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<SkinHistoryDto> RecentSkinHistories { get; set; } = new();
    }

    public class CreateCustomerProfileDto
    {
        public int UserId { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? CurrentSkinType { get; set; }
        public List<string>? SkinConcerns { get; set; }
        public List<string>? KnownAllergies { get; set; }
        public List<string>? PreferredBrands { get; set; }
        public string? Note { get; set; }
        public int? ConsultantUserId { get; set; }
    }

    public class RepurchaseAlertDto
    {
        public int ScheduleId { get; set; }
        public int CustomerProfileId { get; set; }
        public int UserId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductThumbnailUrl { get; set; }
        public DateTime LastPurchaseDate { get; set; }
        public decimal EstimatedVolumeMl { get; set; }
        public decimal DailyUsageMl { get; set; }
        public DateTime EstimatedDepletionDate { get; set; }
        public int DaysRemaining { get; set; }
        public bool IsNotified { get; set; }
    }
}
