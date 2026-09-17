using System.Collections.Generic;

namespace Omnichannel.Domain.Entities
{
    public class PostCategory
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }

        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}