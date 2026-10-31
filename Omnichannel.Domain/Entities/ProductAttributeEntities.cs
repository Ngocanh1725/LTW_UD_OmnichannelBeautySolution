using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Omnichannel.Domain.Entities
{
    [Table("ProductVariants")]
    public class ProductVariant
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int ProductId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string Sku { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string Barcode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string VariantName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? ColorName { get; set; }

        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string? ColorHex { get; set; } // e.g. #C94C3A

        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string? SwatchColorHex { get; set; } // Mã màu Hex thực tế cho color swatch

        [MaxLength(500)]
        [Column(TypeName = "varchar(500)")]
        public string? SwatchImageUrl { get; set; } // Ảnh Swatch cận cảnh riêng của biến thể

        [MaxLength(50)]
        public string? Volume { get; set; } // e.g. 5.5g, 30ml, 50ml

        [MaxLength(50)]
        public string? Formulation { get; set; } // e.g. Cream, Serum, Lotion, Matte

        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SellingPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceAdjustment { get; set; } = 0; // Phụ thu biến thể so với giá gốc sản phẩm

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Weight { get; set; } // Khối lượng tịnh (g) tính phí vận chuyển

        [Column(TypeName = "decimal(10,2)")]
        public decimal? WeightGrams { get; set; } // Trọng lượng gửi hàng chuẩn hóa (g)

        public int StockQuantity { get; set; } = 0; // Tồn kho biến thể khả dụng

        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Product Product { get; set; } = null!;
        public virtual ICollection<ProductSwatch> Swatches { get; set; } = new List<ProductSwatch>();
        public virtual ICollection<ProductBatch> Batches { get; set; } = new List<ProductBatch>();
        public virtual ICollection<ProductVariantAttributeValue> AttributeValues { get; set; } = new List<ProductVariantAttributeValue>();
    }

    [Table("ProductSwatches")]
    public class ProductSwatch
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int ProductVariantId { get; set; }

        [Required]
        [MaxLength(500)]
        [Column(TypeName = "varchar(500)")]
        public string SwatchImageUrl { get; set; } = string.Empty;

        [MaxLength(500)]
        [Column(TypeName = "varchar(500)")]
        public string? SwatchOnSkinImageUrl { get; set; } // Ảnh swatch trên nền da thực tế

        public int SortOrder { get; set; } = 0;

        // Navigation
        public virtual ProductVariant ProductVariant { get; set; } = null!;
    }

    [Table("ActiveIngredients")]
    public class ActiveIngredient
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // Niacinamide, Retinol, BHA...

        [Required]
        [MaxLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string Slug { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        public virtual ICollection<ProductIngredient> ProductIngredients { get; set; } = new List<ProductIngredient>();
    }

    [Table("ProductIngredients")]
    public class ProductIngredient
    {
        public int ProductId { get; set; }
        public int ActiveIngredientId { get; set; }

        [MaxLength(50)]
        public string? Concentration { get; set; } // e.g. 2%, 0.3%, 500ppm

        // Navigation
        public virtual Product Product { get; set; } = null!;
        public virtual ActiveIngredient ActiveIngredient { get; set; } = null!;
    }

    [Table("SkinTypes")]
    public class SkinType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty; // Da dầu, Da khô, Da nhạy cảm, Da hỗn hợp

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string Slug { get; set; } = string.Empty;

        // Navigation
        public virtual ICollection<ProductSkinType> ProductSkinTypes { get; set; } = new List<ProductSkinType>();
    }

    [Table("ProductSkinTypes")]
    public class ProductSkinType
    {
        public int ProductId { get; set; }
        public int SkinTypeId { get; set; }

        // Navigation
        public virtual Product Product { get; set; } = null!;
        public virtual SkinType SkinType { get; set; } = null!;
    }

    [Table("SkinConcerns")]
    public class SkinConcern
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // Dưỡng ẩm, Chống nắng, Trị mụn, Chống lão hóa, Làm sáng da, Se khít lỗ chân lông

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string Slug { get; set; } = string.Empty; // hydration, sun_protection, acne, anti_aging, brightening, pore_minimizing

        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string? Icon { get; set; } // fa-droplet, fa-sun, fa-shield-virus, fa-clock-rotate-left, fa-wand-magic-sparkles, fa-circle-dot

        [MaxLength(255)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;

        // Navigation
        public virtual ICollection<ProductConcern> ProductConcerns { get; set; } = new List<ProductConcern>();
    }

    [Table("ProductConcerns")]
    public class ProductConcern
    {
        public int ProductId { get; set; }
        public int SkinConcernId { get; set; }

        // Navigation
        public virtual Product Product { get; set; } = null!;
        public virtual SkinConcern SkinConcern { get; set; } = null!;
    }
}

