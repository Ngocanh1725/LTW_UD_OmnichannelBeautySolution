using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Omnichannel.Domain.Entities
{
    [Table("SectionItems")]
    public class SectionItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int LayoutSectionId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Subtitle { get; set; }

        [MaxLength(500)]
        [Column(TypeName = "varchar(500)")]
        public string? TargetUrl { get; set; }

        public int? MediaAssetId { get; set; }

        public int OrderIndex { get; set; } = 0;

        public string? PayloadJson { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual LayoutSection LayoutSection { get; set; } = null!;
        public virtual MediaAsset? MediaAsset { get; set; }
    }
}

