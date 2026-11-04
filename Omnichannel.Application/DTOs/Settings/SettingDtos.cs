using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Omnichannel.Application.DTOs.Settings
{
    public class SettingDto
    {
        public int Id { get; set; }
        public string SettingKey { get; set; } = string.Empty;
        public string SettingValue { get; set; } = string.Empty;
        public string SettingGroup { get; set; } = "GENERAL"; // GENERAL, SMTP, PAYMENT, SHIPPING
        public string? Description { get; set; }
        public string DataType { get; set; } = "STRING"; // STRING, INT, BOOL, JSON
        public bool IsEncrypted { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public string? UpdatedByName { get; set; }
    }

    public class UpdateSettingItemDto
    {
        [Required]
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class SettingsGroupViewModel
    {
        public string ActiveGroup { get; set; } = "GENERAL";
        public List<SettingDto> GeneralSettings { get; set; } = new();
        public List<SettingDto> SmtpSettings { get; set; } = new();
        public List<SettingDto> PaymentSettings { get; set; } = new();
        public List<SettingDto> ShippingSettings { get; set; } = new();
    }

    public class DemoResetResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int DeletedOrdersCount { get; set; }
        public int PurgedCartsCount { get; set; }
        public int RestoredBatchesCount { get; set; }
        public DateTime ResetAt { get; set; } = DateTime.UtcNow;
    }
}
