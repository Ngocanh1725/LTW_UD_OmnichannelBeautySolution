using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
//Định nghĩa các thuộc tính mở rộng (Tone màu da, Nơi sản xuất, Mùi hương).
namespace Omnichannel.Domain.Entities
{
    [Table("ProductAttributes")]
    public class ProductAttribute
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // e.g. "Màu Sắc", "Dung Tích", "Kích Cỡ"

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string Code { get; set; } = string.Empty; // COLOR, VOLUME, SIZE

        [MaxLength(30)]
        [Column(TypeName = "varchar(30)")]
        public string DisplayType { get; set; } = "SWATCH"; // SWATCH, BUTTON, DROPDOWN, RADIO

        [MaxLength(255)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;

        public virtual ICollection<ProductAttributeValue> Values { get; set; } = new List<ProductAttributeValue>();
    }

    [Table("ProductAttributeValues")]
    public class ProductAttributeValue
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int ProductAttributeId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Value { get; set; } = string.Empty; // e.g. "Chili", "Ruby Woo", "30ml", "50ml", "100ml"

        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string? ColorHex { get; set; } // #A8332A

        [MaxLength(500)]
        [Column(TypeName = "varchar(500)")]
        public string? ImageUrl { get; set; }

        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        public virtual ProductAttribute ProductAttribute { get; set; } = null!;
        public virtual ICollection<ProductVariantAttributeValue> VariantMappings { get; set; } = new List<ProductVariantAttributeValue>();
    }

    [Table("ProductVariantAttributeValues")]
    public class ProductVariantAttributeValue
    {
        public int ProductVariantId { get; set; }
        public int ProductAttributeValueId { get; set; }

        public virtual ProductVariant ProductVariant { get; set; } = null!;
        public virtual ProductAttributeValue ProductAttributeValue { get; set; } = null!;
    }
}
