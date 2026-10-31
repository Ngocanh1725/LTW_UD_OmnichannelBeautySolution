using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Omnichannel.Domain.Entities
{
    [Table("InternalMessages")]
    public class InternalMessage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int SenderId { get; set; }

        [Required]
        public int ReceiverId { get; set; }

        [Required]
        [MaxLength(250)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;

        [Column(TypeName = "datetime2(7)")]
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? ReadAt { get; set; }

        // Đánh dấu xóa từ phía người gửi hoặc người nhận
        public bool IsDeletedBySender { get; set; } = false;
        public bool IsDeletedByReceiver { get; set; } = false;

        // Navigation properties
        public virtual User? Sender { get; set; }
        public virtual User? Receiver { get; set; }
    }
}
