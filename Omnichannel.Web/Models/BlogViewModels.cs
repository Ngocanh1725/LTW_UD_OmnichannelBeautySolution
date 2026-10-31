using System.Collections.Generic;
using Omnichannel.Domain.Entities;

namespace Omnichannel.Web.Models
{
    public class BlogIndexViewModel
    {
        public List<Post> Posts { get; set; } = new();
        public List<PostCategory> Categories { get; set; } = new();
        public Post? FeaturedPost { get; set; }
        public List<Post> RecentPosts { get; set; } = new();
        public string? SelectedCategorySlug { get; set; }
        public string? Query { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalPosts { get; set; } = 0;
    }

    public class BlogDetailViewModel
    {
        public Post Post { get; set; } = null!;
        public List<Post> RelatedPosts { get; set; } = new();
        public List<Product> RelatedProducts { get; set; } = new();
    }
}
