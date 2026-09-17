using System;

namespace Omnichannel.Domain.Entities
{
    public class Post
    {
        public int PostId { get; set; }
        public int CategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string ContentHtml { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
        public int Status { get; set; } // 1: Draft, 2: Pending, 3: Published, 4: Hidden
        public string? MetaTitle { get; set; }
        public string? MetaKeywords { get; set; }
        public string? MetaDescription { get; set; }
        public int ViewCount { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual PostCategory Category { get; set; } = null!;
        public virtual User Author { get; set; } = null!;
    }
}