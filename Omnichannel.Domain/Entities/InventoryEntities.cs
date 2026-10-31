using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Omnichannel.Domain.Common;

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

        public int? MediaAssetId { get; set; }

        [MaxLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string? Icon { get; set; }

        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        // Navigation
        public virtual Category? Parent { get; set; }
        public virtual MediaAsset? MediaAsset { get; set; }
        public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }

    [Table("Brands")]
    public class Brand
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        [Column(TypeName = "varchar(150)")]
        public string Slug { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string CountryOfOrigin { get; set; } = string.Empty;

        [MaxLength(500)]
        [Column(TypeName = "varchar(500)")]
        public string? LogoUrl { get; set; }

        public int? LogoMediaId { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsFeatured { get; set; } = true;
        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual MediaAsset? LogoMedia { get; set; }
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
        public virtual ICollection<SupplierDebt> SupplierDebts { get; set; } = new List<SupplierDebt>();
        public virtual ICollection<FinancialLedger> FinancialLedgers { get; set; } = new List<FinancialLedger>();
    }

    [Table("Products")]
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int CategoryId { get; set; }
        public int? BrandId { get; set; }
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

        public int? ThumbnailMediaId { get; set; }

        public string? ImagesJson { get; set; }

        [Required]
        [MaxLength(50)]
        public string Unit { get; set; } = "Chai"; // Chai, Há»™p, Thá»i, TuÃ½p

        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;

        public bool HasVariants { get; set; } = false;

        [Required]
        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string ProductType { get; set; } = "STANDARD"; // STANDARD, TESTER, SAMPLE, GWP

        [MaxLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string? CosmeticNotificationNumber { get; set; } // Số phiếu tiếp nhận công bố BYT

        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string? CosmeticNotificationNo { get; set; }

        [Column(TypeName = "date")]
        public DateTime? CosmeticNotificationExpiry { get; set; }

        public int? PAOMonths { get; set; } // Hạn dùng sau khi mở nắp (Period After Opening: 3, 6, 9, 12, 18, 24, 36)

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string StorageCondition { get; set; } = "Ambient_15_25C"; // Ambient_15_25C, Cool_8_15C, Refrigerated_2_8C

        [NotMapped]
        public string? NotificationNumber
        {
            get => CosmeticNotificationNo ?? CosmeticNotificationNumber;
            set { CosmeticNotificationNo = value; CosmeticNotificationNumber = value; }
        }

        [NotMapped]
        public int? PaoMonths
        {
            get => PAOMonths;
            set => PAOMonths = value;
        }

        [MaxLength(200)]
        public string? ManufacturerName { get; set; } // Nhà sản xuất

        [MaxLength(100)]
        public string? CountryOfOrigin { get; set; } // Nước xuất xứ

        [MaxLength(200)]
        public string? ImporterName { get; set; } // Đơn vị nhập khẩu

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "datetime2(7)")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public virtual Category Category { get; set; } = null!;
        public virtual Brand? Brand { get; set; }
        public virtual Supplier? Supplier { get; set; }
        public virtual MediaAsset? ThumbnailMedia { get; set; }
        public virtual ICollection<ProductBatch> Batches { get; set; } = new List<ProductBatch>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public virtual ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        public virtual ICollection<ProductIngredient> ProductIngredients { get; set; } = new List<ProductIngredient>();
        public virtual ICollection<ProductSkinType> ProductSkinTypes { get; set; } = new List<ProductSkinType>();
        public virtual ICollection<ProductConcern> ProductConcerns { get; set; } = new List<ProductConcern>();
        public virtual ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
    }

    [Table("ProductBatches")]
    public class ProductBatch : AggregateRoot
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        [MaxLength(128)]
        [Column(TypeName = "varchar(128)")]
        public string? Gs1ApplicationIdentifier { get; set; }

        [MaxLength(500)]
        [Column(TypeName = "varchar(500)")]
        public string? QualityCertificateUrl { get; set; }

        public int ProductId { get; set; }
        public int? ProductVariantId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string BatchNumber { get; set; } = string.Empty; // Mã lô sản xuất

        [Column(TypeName = "datetime2(7)")]
        public DateTime ManufacturingDate { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime ExpDate { get; set; } // Ngày hết hạn (FEFO cốt lõi)

        [Column(TypeName = "decimal(18,2)")]
        public decimal ImportCost { get; set; }

        public int InitialQuantity { get; set; }
        public int CurrentQuantity { get; set; }

        [Required]
        [MaxLength(30)]
        [Column(TypeName = "varchar(30)")]
        public string Status { get; set; } = "ACTIVE"; // ACTIVE, NEAR_EXPIRATION, EXPIRED, DEPLETED

        public int AlertDaysThreshold { get; set; } = 90; // Ngưỡng cảnh báo cận date FEFO (mặc định 90 ngày cho tiêu chuẩn dược mỹ phẩm)

        [Column(TypeName = "decimal(5,2)")]
        public decimal? ClearanceDiscountPercent { get; set; } // % Chiết khấu xả hàng FEFO (40% - 70%)

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Product Product { get; set; } = null!;
        public virtual ProductVariant? ProductVariant { get; set; }
        public virtual ICollection<StoreInventory> StoreInventories { get; set; } = new List<StoreInventory>();
        public virtual ICollection<InventoryLog> InventoryLogs { get; set; } = new List<InventoryLog>();
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

        /// <summary>
        /// Domain Method: Kiểm tra lô hàng có cận date so với ngưỡng ngày quy định hay không
        /// </summary>
        /// <param name="overrideThreshold">Số ngày cảnh báo tùy chọn (mặc định lấy theo AlertDaysThreshold = 90 ngày)</param>
        public bool IsNearExpiration(int? overrideThreshold = null)
        {
            var threshold = overrideThreshold ?? (AlertDaysThreshold > 0 ? AlertDaysThreshold : 90);
            var remainingDays = (ExpDate.Date - DateTime.UtcNow.Date).TotalDays;
            return remainingDays <= threshold && remainingDays >= 0;
        }

        /// <summary>
        /// Domain Method: Kiểm tra lô hàng đã quá hạn hoàn toàn
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

        [Column(TypeName = "decimal(18,2)")]
        public decimal Area { get; set; } = 120m; // Diện tích mặt bằng m² để tính Sales per sqft

        public bool IsHeadquarters { get; set; } = false;
        public bool IsActive { get; set; } = true;

        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<StoreInventory> StoreInventories { get; set; } = new List<StoreInventory>();
        public virtual ICollection<InventoryLog> InventoryLogs { get; set; } = new List<InventoryLog>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<StockTransfer> OutgoingTransfers { get; set; } = new List<StockTransfer>();
        public virtual ICollection<StockTransfer> IncomingTransfers { get; set; } = new List<StockTransfer>();
        public virtual ICollection<FinancialLedger> FinancialLedgers { get; set; } = new List<FinancialLedger>();
    }

    [Table("StoreInventories")]
    public class StoreInventory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }

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
