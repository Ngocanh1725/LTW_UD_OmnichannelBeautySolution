using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Omnichannel.Domain.Entities
{
    [Table("ChatSessions")]
    public class ChatSession
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(64)]
        [Column(TypeName = "varchar(64)")]
        public string SessionKey { get; set; } = string.Empty;

        public int? UserId { get; set; }

        [MaxLength(100)]
        public string? CustomerName { get; set; }

        [MaxLength(100)]
        public string? CustomerEmail { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

        public int MessageCount { get; set; } = 0;

        [MaxLength(50)]
        public string? DetectedTopic { get; set; } = "GENERAL"; // SKINCARE, ORDER, PROMOTION, CONSULTATION, CONTACT, GENERAL

        public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }

    [Table("ChatMessages")]
    public class ChatMessage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(64)]
        [Column(TypeName = "varchar(64)")]
        public string SessionKey { get; set; } = string.Empty;

        public int? SessionId { get; set; }

        [ForeignKey("SessionId")]
        public virtual ChatSession? Session { get; set; }

        [Required]
        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string Sender { get; set; } = "USER"; // USER, BOT, SYSTEM

        [Required]
        public string Content { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Intent { get; set; } // skincare, order, promotion, consultation, contact, greeting, fallback

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
