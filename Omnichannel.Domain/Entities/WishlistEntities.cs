using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Omnichannel.Domain.Entities
{
    /// <summary>
    /// Danh mục sản phẩm yêu thích của khách hàng (Wishlist Items)
    /// </summary>
    [Table("WishlistItems")]
    public class WishlistItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int UserId { get; set; }

        public int ProductId { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
    }

    /// <summary>
    /// Đăng ký nhận thông báo khi có hàng trở lại (Back-In-Stock Alert)
    /// </summary>
    [Table("StockAlerts")]
    public class StockAlert
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int ProductId { get; set; }

        public int? ProductVariantId { get; set; }

        public int? UserId { get; set; }

        [MaxLength(100)]
        public string? CustomerName { get; set; }

        [MaxLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string? Email { get; set; }

        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Trạng thái: PENDING (Chờ hàng về), NOTIFIED (Đã gửi thông báo), CANCELLED (Đã hủy)
        /// </summary>
        [Required]
        [MaxLength(30)]
        [Column(TypeName = "varchar(30)")]
        public string Status { get; set; } = "PENDING";

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? NotifiedAt { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
