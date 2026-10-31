using System;

namespace Omnichannel.Application.DTOs.Security
{
    public class AuditLogDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AuditLogFilterDto
    {
        public string? EntityType { get; set; }
        public int? UserId { get; set; }
        public string? Action { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
