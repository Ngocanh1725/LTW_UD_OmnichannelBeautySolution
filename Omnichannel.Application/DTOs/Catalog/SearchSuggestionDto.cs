using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Catalog
{
    /// <summary>
    /// DTO chứa toàn bộ kết quả gợi ý tìm kiếm tức thì theo chuẩn Sephora / Hasaki
    /// </summary>
    public class SearchSuggestionDto
    {
        public string Query { get; set; } = string.Empty;
        public string NormalizedQuery { get; set; } = string.Empty;
        public int TotalMatches { get; set; }
        public List<SearchProductSuggestionDto> Products { get; set; } = new();
        public List<SearchBrandSuggestionDto> Brands { get; set; } = new();
        public List<SearchCategorySuggestionDto> Categories { get; set; } = new();
        public List<string> HotKeywords { get; set; } = new();
    }

    /// <summary>
    /// Gợi ý sản phẩm với thông tin giá, ảnh, thương hiệu và xếp hạng
    /// </summary>
    public class SearchProductSuggestionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public decimal? SalePrice { get; set; }
        public string? BrandName { get; set; }
        public string? CategoryName { get; set; }
        public string? RoutineStep { get; set; }
        public bool IsFeatured { get; set; }
        public int MatchScore { get; set; }
    }

    /// <summary>
    /// Gợi ý thương hiệu mỹ phẩm
    /// </summary>
    public class SearchBrandSuggestionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public int ProductCount { get; set; }
    }

    /// <summary>
    /// Gợi ý danh mục mỹ phẩm
    /// </summary>
    public class SearchCategorySuggestionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int ProductCount { get; set; }
    }
}

