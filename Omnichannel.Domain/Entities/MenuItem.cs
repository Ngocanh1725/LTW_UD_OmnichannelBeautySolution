using System.Collections.Generic;

namespace Omnichannel.Domain.Entities
{
    public class MenuItem
    {
        public int MenuItemId { get; set; }
        public int MenuId { get; set; }
        public int? ParentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int TargetType { get; set; } // 1: Category, 2: Product, 3: Post, 4: StaticPage, 5: CustomUrl
        public string? TargetId { get; set; }
        public string? TargetSlug { get; set; }
        public string? CustomUrl { get; set; }
        public int DisplayOrder { get; set; }
        public bool OpenInNewTab { get; set; }
        public string? IconClass { get; set; }
        public string? CssClass { get; set; }
        public bool IsActive { get; set; }

        public virtual Menu Menu { get; set; } = null!;
        public virtual MenuItem? Parent { get; set; }
        public virtual ICollection<MenuItem> Children { get; set; } = new List<MenuItem>();
    }
}