using System.Collections.Generic;
using Omnichannel.Domain.Entities;

namespace Omnichannel.Web.Models
{
    public class ProductSearchViewModel
    {
        public string? Query { get; set; }
        public int? CategoryId { get; set; }
        public string? CategorySlug { get; set; }
        public string? Brand { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MinRating { get; set; }
        public string? SkinType { get; set; }
        public string Sort { get; set; } = "newest";
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalPages { get; set; } = 1;
        public int TotalProducts { get; set; } = 0;

        public List<Product> Products { get; set; } = new();
        public List<CategoryFilterItem> Categories { get; set; } = new();
        public List<BrandFilterItem> Brands { get; set; } = new();
        public List<string> AvailableSkinTypes { get; set; } = new();
    }

    public class CategoryFilterItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int Count { get; set; }
        public bool IsSelected { get; set; }
    }

    public class BrandFilterItem
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public bool IsSelected { get; set; }
    }
}
