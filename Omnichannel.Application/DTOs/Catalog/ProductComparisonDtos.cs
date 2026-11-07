using System;
using System.Collections.Generic;
using Omnichannel.Application.DTOs.BeautyTech;

namespace Omnichannel.Application.DTOs.Catalog
{
    /// <summary>
    /// DTO rút gọn cho Khay so sánh nổi (Floating Compare Drawer) và xem nhanh
    /// </summary>
    public class CompareItemSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public decimal FinalPrice { get; set; }
        public decimal OriginalPrice { get; set; }
        public bool HasDiscount { get; set; }
        public int DiscountPercent { get; set; }
        public int CategoryId { get; set; }
        public string? BrandName { get; set; }
        public string? CategoryName { get; set; }
        public string? RoutineStep { get; set; }
        public decimal? VolumeAmount { get; set; }
        public string VolumeUnit { get; set; } = "ml";
        public string VolumeDisplay => VolumeAmount.HasValue && VolumeAmount.Value > 0 ? $"{VolumeAmount.Value} {VolumeUnit}" : "——";
        public decimal? UnitPriceRatio { get; set; }
        public string? UnitPriceRatioFormatted { get; set; }
        public bool IsInStock { get; set; } = true;
    }

    /// <summary>
    /// Hoạt chất chính và nồng độ phục vụ đối chiếu dược mỹ phẩm
    /// </summary>
    public class ComparisonActiveIngredientDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Concentration { get; set; }
        public string Function { get; set; } = string.Empty;
        public int EwgScore { get; set; } = 1;
        public string EwgRating { get; set; } = "Xanh lá";
    }

    /// <summary>
    /// DTO chi tiết của từng sản phẩm trong bảng so sánh ma trận đối chiếu Side-by-Side
    /// </summary>
    public class ProductComparisonDetailDto
    {
        // Thông tin định danh & cơ bản
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public string? ShortDescription { get; set; }
        public int CategoryId { get; set; }
        public string? BrandName { get; set; }
        public string? CategoryName { get; set; }
        public string? OriginCountry { get; set; }
        public string? ManufacturerName { get; set; }
        public string? RoutineStep { get; set; }
        public string? RoutineStepDisplayName { get; set; }
        public string? StorageCondition { get; set; }
        public int? PaoMonths { get; set; }
        public string PaoMonthsText => PaoMonths.HasValue ? $"{PaoMonths.Value} tháng sau mở nắp" : "12 tháng";

        // Giá cả & Dung tích
        public decimal FinalPrice { get; set; }
        public decimal OriginalPrice { get; set; }
        public bool HasDiscount { get; set; }
        public int DiscountPercent { get; set; }
        public decimal? VolumeAmount { get; set; }
        public string VolumeUnit { get; set; } = "ml";
        public string VolumeDisplay => VolumeAmount.HasValue && VolumeAmount.Value > 0 ? $"{VolumeAmount.Value} {VolumeUnit}" : "——";
        public decimal? UnitPriceRatio { get; set; }
        public string? UnitPriceRatioFormatted { get; set; }
        public bool IsBestValuePriceRatio { get; set; } // Đơn giá / ml hoặc / g tiết kiệm nhất trong nhóm so sánh (Badge "Kinh tế nhất")
        public bool IsBestValuePerMl => IsBestValuePriceRatio; // Alias thuận tiện

        // Điểm an toàn EWG & Clean Beauty
        public int AverageEwgScore { get; set; } = 1;
        public string EwgRatingText { get; set; } = "Cực kỳ lành tính (Xanh lá)";
        public string EwgBadgeColor { get; set; } = "bg-emerald-50 text-emerald-700 border-emerald-200";
        public bool IsAlcoholFree { get; set; }
        public bool IsFragranceFree { get; set; }
        public bool IsPregnancySafe { get; set; }
        public int TotalIngredientsCount { get; set; }
        public List<ComparisonActiveIngredientDto> KeyActives { get; set; } = new();
        public List<string> ParsedKeyActiveNames { get; set; } = new();

        // Loại da & Vấn đề da
        public List<string> SuitableSkinTypes { get; set; } = new();
        public List<string> TargetConcerns { get; set; } = new();

        // Hạn sử dụng FEFO & Tồn kho
        public bool IsInStock { get; set; } = true;
        public int TotalStockQuantity { get; set; }
        public DateTime? NearestExpiryDate { get; set; }
        public int? DaysUntilExpiry { get; set; }
        public string FefoStatusText { get; set; } = "Date Mới 100%";
        public string FefoBadgeClass { get; set; } = "bg-emerald-50 text-emerald-700 border-emerald-200";
        public string? NearestBatchNumber { get; set; }
        public bool IsLongestFefo { get; set; } // Badge "Date mới nhất" (Hạn dùng FEFO xa nhất)

        // Đánh giá cộng đồng
        public double AverageRating { get; set; } = 5.0;
        public int ReviewCount { get; set; } = 0;
    }

    /// <summary>
    /// Kết quả toàn diện của Ma trận so sánh mỹ phẩm (Cosmetics Comparison Matrix)
    /// </summary>
    public class ProductComparisonMatrixDto
    {
        public List<ProductComparisonDetailDto> Products { get; set; } = new();
        public int TotalProducts => Products.Count;

        /// <summary>
        /// Cảnh báo nếu các sản phẩm được chọn thuộc nhiều nhóm danh mục khác nhau
        /// </summary>
        public bool HasCategoryMismatch { get; set; }
        public List<string> DistinctCategories { get; set; } = new();

        /// <summary>
        /// Danh sách tương kỵ dược lý hoặc hiệp đồng da liễu nếu phối hợp các sản phẩm này
        /// </summary>
        public List<IngredientInteractionFindingDto> Interactions { get; set; } = new();

        /// <summary>
        /// Tên sản phẩm có đơn giá / dung tích tiết kiệm nhất (Kinh tế nhất)
        /// </summary>
        public string? BestValueProductName { get; set; }

        /// <summary>
        /// Tên sản phẩm có hạn dùng FEFO xa nhất (Date mới nhất)
        /// </summary>
        public string? LongestFefoProductName { get; set; }

        /// <summary>
        /// Tên sản phẩm có độ an toàn và điểm EWG tối ưu nhất
        /// </summary>
        public string? SafestProductName { get; set; }

        /// <summary>
        /// Nhận xét & khuyến nghị tổng quan từ hệ thống Dược Mỹ Phẩm AI
        /// </summary>
        public string RecommendationSummary { get; set; } = string.Empty;
    }
}
