using System.Collections.Generic;

namespace Omnichannel.Web.Models
{
    public class LiveSearchViewModel
    {
        public string Query { get; set; } = string.Empty;
        public List<LiveSearchProductItem> Products { get; set; } = new();
        public List<string> TrendingKeywords { get; set; } = new();
        public int TotalFound { get; set; }
        public bool HasSearched => !string.IsNullOrWhiteSpace(Query) && Query.Trim().Length >= 2;
    }

    public class LiveSearchProductItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal OriginalPrice { get; set; }
        public string? CategoryName { get; set; }
        public string? ManufacturerName { get; set; }
        public string FefoStatus { get; set; } = string.Empty;
        public string FefoBadgeClass { get; set; } = string.Empty;
        public double Rating { get; set; } = 5.0;
        public int DaysLeft { get; set; } = 365;
    }
}
