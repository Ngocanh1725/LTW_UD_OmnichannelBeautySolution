using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Catalog
{
    /// <summary>
    /// DTO sản phẩm đã xem gần đây (Recently Viewed Product)
    /// </summary>
    public class RecentlyViewedProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public decimal? SalePrice { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? RoutineStep { get; set; }
        public double Rating { get; set; } = 5.0;
        public int ReviewCount { get; set; }
        public bool IsFeatured { get; set; }
        public bool InStock { get; set; } = true;
    }

    /// <summary>
    /// DTO gói sản phẩm thường mua cùng nhau (Frequently Bought Together Bundle)
    /// </summary>
    public class FrequentlyBoughtTogetherDto
    {
        /// <summary>
        /// Sản phẩm đang xem hiện tại (luôn nằm đầu combo)
        /// </summary>
        public FbtProductItemDto CurrentProduct { get; set; } = null!;

        /// <summary>
        /// Danh sách các sản phẩm gợi ý mua kèm (thường 1-2 sản phẩm)
        /// </summary>
        public List<FbtProductItemDto> BundleProducts { get; set; } = new();

        /// <summary>
        /// Tổng giá gốc của toàn bộ sản phẩm trong combo
        /// </summary>
        public decimal TotalOriginalPrice { get; set; }

        /// <summary>
        /// Giá combo thực tế sau khi tính giá khuyến mãi / chiết khấu combo
        /// </summary>
        public decimal TotalBundlePrice { get; set; }

        /// <summary>
        /// Số tiền tiết kiệm được khi mua trọn bộ combo
        /// </summary>
        public decimal TotalDiscount { get; set; }

        /// <summary>
        /// Số đơn hàng hoàn tất chứa cả các sản phẩm này
        /// </summary>
        public int BundleOrderCount { get; set; }

        /// <summary>
        /// Tổng số đơn hàng hoàn tất được phân tích
        /// </summary>
        public int TotalOrdersAnalyzed { get; set; }

        /// <summary>
        /// Lời giải thích trực quan tăng tính thuyết phục (Social Proof / Skincare Routine)
        /// </summary>
        public string Explanation { get; set; } = string.Empty;

        /// <summary>
        /// true nếu tính từ thuật toán đồng xuất hiện (Co-occurrence); false nếu fallback theo Routine Skincare
        /// </summary>
        public bool IsAlgorithmic { get; set; }
    }

    /// <summary>
    /// DTO từng sản phẩm trong gói Frequently Bought Together
    /// </summary>
    public class FbtProductItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public decimal? SalePrice { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? RoutineStep { get; set; }
        public int CoOccurrenceCount { get; set; }
        public double Confidence { get; set; }
        public double Lift { get; set; }
        public string RecommendationReason { get; set; } = string.Empty;
        public bool IsCurrent { get; set; }
        public bool IsSelected { get; set; } = true;
    }
}

