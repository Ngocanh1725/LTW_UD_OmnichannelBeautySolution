using System;
using System.Collections.Generic;
using Omnichannel.Domain.Entities;

namespace Omnichannel.Web.Models
{
    public class CategoryViewModel
    {
        public Category? CurrentCategory { get; set; }
        public string? CategorySlug { get; set; }
        public int? CategoryId { get; set; }
        public string? Query { get; set; }

        // Faceted Filters
        public string FefoOption { get; set; } = "all"; // all, fresh, good, clearance
        public decimal MinPrice { get; set; } = 50000m;
        public decimal MaxPrice { get; set; } = 10000000m;
        public List<string> SelectedBrands { get; set; } = new();
        public string SkinType { get; set; } = "all"; // all, oily, dry, sensitive, combination
        public int? MinRating { get; set; } // null, 4, 5

        // Clean Beauty Filters
        public bool IsAlcoholFree { get; set; } = false;
        public bool IsFragranceFree { get; set; } = false;
        public bool IsPregnancySafe { get; set; } = false;

        // Sorting & View Mode
        public string Sort { get; set; } = "newest"; // newest, bestseller, price-asc, price-desc, fefo-longest, discount-desc
        public string ViewMode { get; set; } = "grid"; // grid, list

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalPages { get; set; } = 1;
        public int TotalProducts { get; set; } = 0;

        // Data collections
        public List<Product> Products { get; set; } = new();
        public List<CategoryFilterItem> Categories { get; set; } = new();
        public List<BrandFilterItem> Brands { get; set; } = new();

        // Helper metadata for rendering badges
        public Dictionary<int, ProductFefoMeta> FefoMeta { get; set; } = new();
        public Dictionary<int, double> Ratings { get; set; } = new();
        public Dictionary<int, List<string>> ActiveIngredients { get; set; } = new();
    }

    public class ProductFefoMeta
    {
        public DateTime ExpDate { get; set; }
        public int DaysLeft { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public string BadgeClass { get; set; } = string.Empty;
        public string BadgeText { get; set; } = string.Empty;
        public string StatusKey { get; set; } = "fresh"; // fresh, good, clearance
        public decimal DiscountPercent { get; set; }
    }
}
