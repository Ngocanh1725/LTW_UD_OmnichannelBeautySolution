using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Omnichannel.Domain.Entities
{
    [Table("LayoutSections")]
    public class LayoutSection
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int PageLayoutId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "varchar(50)")]
        public string SectionKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string DisplayName { get; set; } = string.Empty;

        public int OrderIndex { get; set; } = 0;

        public bool IsVisible { get; set; } = true;

        public string? CustomStylesJson { get; set; }

        [Column(TypeName = "datetime2(7)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual PageLayout PageLayout { get; set; } = null!;
        public virtual ICollection<SectionItem> SectionItems { get; set; } = new List<SectionItem>();
    }
}

