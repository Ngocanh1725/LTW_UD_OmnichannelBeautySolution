using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Omnichannel.Domain.Entities
{
    /// <summary>
    /// Bảng lưu trữ thông điệp đầu ra đảm bảo tính toàn vẹn giao dịch (Transactional Outbox Pattern).
    /// Giúp chống bài toán Ghi kép (Dual-Write Problem) khi tích hợp GHN, SignalR, Email.
    /// </summary>
    [Table("OutboxMessages")]
    public class OutboxMessage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        [MaxLength(150)]
        [Column(TypeName = "varchar(150)")]
        public string EventType { get; set; } = string.Empty;

        [Required]
        public string PayloadJson { get; set; } = string.Empty;

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? ProcessedAtUtc { get; set; }

        public int RetryCount { get; set; } = 0;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? NextRetryAtUtc { get; set; }

        [MaxLength(1000)]
        public string? LastErrorMessage { get; set; }
    }

    /// <summary>
    /// Bảng kiểm soát tính bất biến của yêu cầu (Idempotency Key Engine) chống trùng lặp giao dịch Đặt hàng & Webhook thanh toán.
    /// </summary>
    [Table("IdempotencyRecords")]
    public class IdempotencyRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(128)]
        [Column(TypeName = "varchar(128)")]
        public string IdempotencyKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string RequestPath { get; set; } = string.Empty;

        [MaxLength(64)]
        [Column(TypeName = "varchar(64)")]
        public string RequestPayloadHash { get; set; } = string.Empty;

        public int ResponseStatusCode { get; set; }

        public string? ResponseBodyJson { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2(7)")]
        public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddHours(24);
    }
}
