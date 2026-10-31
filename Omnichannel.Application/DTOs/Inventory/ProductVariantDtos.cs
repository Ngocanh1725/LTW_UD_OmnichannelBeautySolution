using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Inventory
{
    public class SwatchDto
    {
        public int Id { get; set; }
        public int ProductVariantId { get; set; }
        public string SwatchImageUrl { get; set; } = string.Empty;
        public string? SwatchOnSkinImageUrl { get; set; }
        public int SortOrder { get; set; }
    }

    public class IngredientDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Concentration { get; set; }
        public string? Description { get; set; }
    }

    public class SkinTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
    }

    public class ProductVariantItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string VariantName { get; set; } = string.Empty;
        public string? ColorName { get; set; }
        public string? ColorHex { get; set; }
        public string? Volume { get; set; }
        public string? Formulation { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal? DiscountPrice { get; set; }
        public decimal? Weight { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public List<SwatchDto> Swatches { get; set; } = new();
    }

    public class CreateProductVariantDto
    {
        public int ProductId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string VariantName { get; set; } = string.Empty;
        public string? ColorName { get; set; }
        public string? ColorHex { get; set; }
        public string? Volume { get; set; }
        public string? Formulation { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal? DiscountPrice { get; set; }
        public decimal? Weight { get; set; }
        public int SortOrder { get; set; } = 0;
        public string? SwatchImageUrl { get; set; }
        public string? SwatchOnSkinImageUrl { get; set; }
    }

    public class ProductDetailWithVariantsDto
    {
        public int Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? ImagesJson { get; set; }
        public string Unit { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsFeatured { get; set; }
        public bool HasVariants { get; set; }
        public string ProductType { get; set; } = "STANDARD";
        public string? CosmeticNotificationNumber { get; set; }
        public string? ManufacturerName { get; set; }
        public string? CountryOfOrigin { get; set; }
        public string? ImporterName { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }

        public List<ProductVariantItemDto> Variants { get; set; } = new();
        public List<IngredientDto> Ingredients { get; set; } = new();
        public List<SkinTypeDto> SkinTypes { get; set; } = new();
        public List<ProductBatchItemDto> Batches { get; set; } = new();

        public int TotalStock => Batches.Sum(b => b.CurrentQuantity);
    }

    public class ProductBatchItemDto
    {
        public int Id { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime ManufacturingDate { get; set; }
        public DateTime ExpDate { get; set; }
        public decimal ImportCost { get; set; }
        public int InitialQuantity { get; set; }
        public int CurrentQuantity { get; set; }
        public string Status { get; set; } = "ACTIVE";
        public int DaysRemaining => (int)(ExpDate.Date - DateTime.UtcNow.Date).TotalDays;
    }
}

