using System;

namespace Omnichannel.Domain.Entities
{
    public class StaticPage
    {
        public int PageId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string ContentHtml { get; set; } = string.Empty;
        public bool IsSystemLocked { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

