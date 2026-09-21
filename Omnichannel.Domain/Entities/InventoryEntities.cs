using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Omnichannel.Domain.Entities
{
    [Table("Categories")]
    public class Category
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? ParentId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        [Column(TypeName = "varchar(150)")]
        public string Slug { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(500)]
        [Column(TypeName = "varchar(500)")]
        public string? ImageUrl { get; set; }

        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        // Navigation
        public virtual Category? Parent { get; set; }
        public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }

    [Table("Suppliers")]
    public class Supplier
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ContactPerson { get; set; }

        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string? PhoneNumber { get; set; }

        [MaxLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string? Email { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }

    [Table("Products")]
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int CategoryId { get; set; }
        public int? SupplierId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string Sku { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string Barcode { get; set; } = string.Empty; // Chuáº©n Barcode EAN-13

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(250)]
        [Column(TypeName = "varchar(250)")]
        public string Slug { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ShortDescription { get; set; }

        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SellingPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountPrice { get; set; }

        [MaxLength(500)]
        [Column(TypeName = "varchar(500)")]
        public string? ThumbnailUrl { get; set; }

        public string? ImagesJson { get; set; }

        [Required]
        [MaxLength(50)]
        public string Unit { get; set; } = "Chai"; // Chai, Há»™p, Thá»i, TuÃ½p

        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public virtual Category Category { get; set; } = null!;
        public virtual Supplier? Supplier { get; set; }
        public virtual ICollection<ProductBatch> Batches { get; set; } = new List<ProductBatch>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }

    [Table("ProductBatches")]
    public class ProductBatch
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int ProductId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string BatchNumber { get; set; } = string.Empty; // MÃ£ lÃ´ sáº£n xuáº¥t

        [Column(TypeName = "datetime2(7)")]
        public DateTime ManufacturingDate { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime ExpDate { get; set; } // NgÃ y háº¿t háº¡n (FEFO cá»‘t lÃµi)

        [Column(TypeName = "decimal(18,2)")]
        public decimal ImportCost { get; set; }

        public int InitialQuantity { get; set; }
        public int CurrentQuantity { get; set; }

        [Required]
        [MaxLength(30)]
        [Column(TypeName = "varchar(30)")]
        public string Status { get; set; } = "ACTIVE"; // ACTIVE, NEAR_EXPIRATION, EXPIRED, DEPLETED

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Product Product { get; set; } = null!;
        public virtual ICollection<StoreInventory> StoreInventories { get; set; } = new List<StoreInventory>();
        public virtual ICollection<InventoryLog> InventoryLogs { get; set; } = new List<InventoryLog>();
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

        /// <summary>
        /// Domain Method: Kiá»ƒm tra lÃ´ hÃ ng cÃ³ cáº­n date so vá»›i ngÆ°á»¡ng ngÃ y quy Ä‘á»‹nh hay khÃ´ng
        /// </summary>
        /// <param name="daysThreshold">Sá»‘ ngÃ y cáº£nh bÃ¡o (máº·c Ä‘á»‹nh 60 ngÃ y cho ngÃ nh má»¹ pháº©m)</param>
        public bool IsNearExpiration(int daysThreshold = 60)
        {
            var remainingDays = (ExpDate.Date - DateTime.UtcNow.Date).TotalDays;
            return remainingDays <= daysThreshold && remainingDays >= 0;
        }

        /// <summary>
        /// Domain Method: Kiá»ƒm tra lÃ´ hÃ ng Ä‘Ã£ quÃ¡ háº¡n hoÃ n toÃ n
        /// </summary>
        public bool IsExpired()
        {
            return DateTime.UtcNow.Date > ExpDate.Date;
        }
    }

    [Table("Stores")]
    public class Store
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Address { get; set; } = string.Empty;

        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string? PhoneNumber { get; set; }

        [MaxLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string? Email { get; set; }

        public bool IsHeadquarters { get; set; } = false;
        public bool IsActive { get; set; } = true;

        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<StoreInventory> StoreInventories { get; set; } = new List<StoreInventory>();
        public virtual ICollection<InventoryLog> InventoryLogs { get; set; } = new List<InventoryLog>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }

    [Table("StoreInventories")]
    public class StoreInventory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int StoreId { get; set; }
        public int ProductBatchId { get; set; }

        public int PhysicalQuantity { get; set; } // Tá»“n thá»±c táº¿ Ä‘áº¿m Ä‘Æ°á»£c trong kho
        public int ReservedQuantity { get; set; } // Äang táº¡m giá»¯ bá»Ÿi giá» hÃ ng/Ä‘Æ¡n chá» láº¥y

        [Column(TypeName = "datetime2(7)")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public virtual Store Store { get; set; } = null!;
        public virtual ProductBatch ProductBatch { get; set; } = null!;

        /// <summary>
        /// Domain Method: Tráº£ vá» sá»‘ lÆ°á»£ng kháº£ dá»¥ng thá»±c táº¿ Ä‘á»ƒ bÃ¡n (Physical - Reserved)
        /// </summary>
        public int GetAvailableQuantity()
        {
            var available = PhysicalQuantity - ReservedQuantity;
            return available > 0 ? available : 0;
        }
    }

    [Table("InventoryLogs")]
    public class InventoryLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int StoreId { get; set; }
        public int ProductBatchId { get; set; }
        public int? UserId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string TransactionType { get; set; } = string.Empty; // IMPORT, POS_SALE, ONLINE_SALE, DISCARD_EXPIRED, ADJUSTMENT

        public int QuantityChanged { get; set; }
        public int QuantityBefore { get; set; }
        public int QuantityAfter { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Store Store { get; set; } = null!;
        public virtual ProductBatch ProductBatch { get; set; } = null!;
        public virtual User? User { get; set; }
    }
}
