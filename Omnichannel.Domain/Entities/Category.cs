using System.Collections.Generic;

namespace Omnichannel.Domain.Entities
{
    public class Category
    {
        public int CategoryId { get; set; }
        public int? ParentId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }

        public virtual Category? Parent { get; set; }
        public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
