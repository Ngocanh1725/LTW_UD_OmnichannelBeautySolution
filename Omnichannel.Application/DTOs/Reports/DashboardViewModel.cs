using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Reports
{
    /// <summary>
    /// ViewModel trung tâm điều hành toàn diện dành cho Admin Dashboard
    /// </summary>
    public class DashboardViewModel
    {
        // =========================================================================
        // 1. CÁC THẺ CHỈ SỐ KPI CHÍNH (4 EXECUTIVE METRIC CARDS)
        // =========================================================================
        public decimal TodayRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public int TodayOrderCount { get; set; }
        public int NewCustomersThisMonth { get; set; }
        public int TotalProducts { get; set; }
        public int TotalStores { get; set; }

        // Bổ sung theo tiêu chuẩn Executive BI Dashboard
        public int MonthTotalOrders { get; set; }
        public int MonthOnlineOrders { get; set; }
        public int MonthPosOrders { get; set; }
        public decimal MonthOnlinePercentage => MonthTotalOrders > 0 ? Math.Round((decimal)MonthOnlineOrders / MonthTotalOrders * 100m, 1) : 0;
        public decimal MonthPosPercentage => MonthTotalOrders > 0 ? Math.Round((decimal)MonthPosOrders / MonthTotalOrders * 100m, 1) : 0;
        
        public decimal CustomerRetentionRate { get; set; } // Tỷ lệ khách hàng quay lại mua sắm (Retention Rate %)
        public decimal FefoRiskInventoryValue { get; set; } // Giá trị tiền hàng có nguy cơ hết hạn trong 90 ngày tới
        public int FefoRiskBatchCount { get; set; } // Số lượng lô hàng cận date 90 ngày
        public decimal TotalInventoryValue { get; set; } // Tổng giá trị tồn kho toàn hệ thống
        public string TotalInventoryValueFormatted => TotalInventoryValue.ToString("N0") + " đ";

        // =========================================================================
        // 2. SO SÁNH TĂNG / GIẢM %
        // =========================================================================
        public decimal TodayVsYesterdayRevenueGrowth { get; set; }
        public decimal MonthVsLastMonthRevenueGrowth { get; set; }
        public decimal TodayVsYesterdayOrderGrowth { get; set; }

        // =========================================================================
        // 3. BIỂU ĐỒ DOANH THU 30 NGÀY GẦN NHẤT (Line Chart: Online vs Offline)
        // =========================================================================
        public List<DailyChannelRevenueDto> DailyChannelRevenues { get; set; } = new();
        public List<DailyRevenueDto> DailyRevenues { get; set; } = new(); // Tương thích ngược

        // =========================================================================
        // 4. BIỂU ĐỒ TRÒN TỶ TRỌNG DOANH THU THEO DANH MỤC (Doughnut Chart)
        // =========================================================================
        public List<CategoryRevenueShareDto> CategoryRevenues { get; set; } = new();

        // =========================================================================
        // 5. TOP 5 SẢN PHẨM BÁN CHẠY NHẤT & ĐƠN HÀNG MỚI
        // =========================================================================
        public List<TopProductDto> TopProducts { get; set; } = new();
        public List<RecentOrderDto> RecentOrders { get; set; } = new();

        // =========================================================================
        // 6. CẢNH BÁO LÔ HÀNG CẬN DATE <= 90 NGÀY (Top 10 Sản Phẩm - FEFO Risk Monitor)
        // =========================================================================
        public List<NearExpiryBatchAlertDto> NearExpiryBatches { get; set; } = new();

        // =========================================================================
        // 7. PHÂN BỔ DOANH THU THEO KÊNH BÁN & CHI NHÁNH & SO SÁNH 7 NGÀY
        // =========================================================================
        public List<ChannelRevenueDistributionDto> ChannelDistributions { get; set; } = new();
        public List<StoreRevenueDistributionDto> StoreDistributions { get; set; } = new();
        public Revenue7DaysComparisonDto Revenue7DaysComparison { get; set; } = new();

        // =========================================================================
        // 8. THÔNG TIN PHẠM VI CHI NHÁNH (STORE SCOPING)
        // =========================================================================
        public bool IsStoreScoped { get; set; }
        public int? CurrentStoreId { get; set; }
        public string? CurrentStoreName { get; set; }
    }

    /// <summary>
    /// DTO so sánh doanh thu 7 ngày gần nhất với tuần trước đó
    /// </summary>
    public class Revenue7DaysComparisonDto
    {
        public List<string> DayLabels { get; set; } = new();
        public List<decimal> CurrentWeekRevenues { get; set; } = new();
        public List<decimal> PreviousWeekRevenues { get; set; } = new();
        public decimal CurrentWeekTotal { get; set; }
        public decimal PreviousWeekTotal { get; set; }
        public decimal GrowthRate { get; set; }
    }

    /// <summary>
    /// DTO doanh thu theo ngày phân tách kênh Online vs POS trong 30 ngày
    /// </summary>
    public class DailyChannelRevenueDto
    {
        public string Date { get; set; } = string.Empty; // dd/MM
        public string FullDate { get; set; } = string.Empty; // dd/MM/yyyy
        public decimal OnlineRevenue { get; set; }
        public decimal PosRevenue { get; set; }
        public decimal TotalRevenue => OnlineRevenue + PosRevenue;
        public int OnlineOrderCount { get; set; }
        public int PosOrderCount { get; set; }
        public int TotalOrderCount => OnlineOrderCount + PosOrderCount;
    }

    /// <summary>
    /// DTO phân bổ doanh thu theo danh mục sản phẩm (Doughnut Chart)
    /// </summary>
    public class CategoryRevenueShareDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal Percentage { get; set; }
        public int QuantitySold { get; set; }
        public string Color { get; set; } = "#e11d48";
    }

    /// <summary>
    /// DTO cảnh báo lô hàng mỹ phẩm cận date <= 90 ngày (FEFO Risk Monitor)
    /// </summary>
    public class NearExpiryBatchAlertDto
    {
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public int CurrentQuantity { get; set; }
        public DateTime ManufacturingDate { get; set; }
        public DateTime ExpDate { get; set; }
        public int DaysRemaining { get; set; }
        public decimal ImportCost { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal TotalRiskValue => CurrentQuantity * (SellingPrice > 0 ? SellingPrice : ImportCost);
        public string StoreName { get; set; } = string.Empty;
        public string Status { get; set; } = "NEAR_EXPIRATION";
        public decimal? ClearanceDiscountPercent { get; set; }
    }

    /// <summary>
    /// DTO phân bổ doanh số theo kênh bán hàng
    /// </summary>
    public class ChannelRevenueDistributionDto
    {
        public string ChannelKey { get; set; } = string.Empty; // POS, WEBSITE, MOBILE_APP
        public string ChannelName { get; set; } = string.Empty; // Điểm bán lẻ POS, Cửa hàng trực tuyến Web, v.v.
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// DTO phân bổ doanh số theo chi nhánh
    /// </summary>
    public class StoreRevenueDistributionDto
    {
        public int StoreId { get; set; }
        public string StoreCode { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    /// <summary>
    /// DTO phản hồi API thời gian thực phục vụ HTMX polling mỗi 30s
    /// </summary>
    public class RealtimeKpiDto
    {
        public decimal TodayRevenue { get; set; }
        public string TodayRevenueFormatted { get; set; } = "0 đ";
        public decimal MonthRevenue { get; set; }
        public string MonthRevenueFormatted { get; set; } = "0 đ";
        public int TodayOrderCount { get; set; }
        public int MonthTotalOrders { get; set; }
        public int MonthOnlineOrders { get; set; }
        public int MonthPosOrders { get; set; }
        public decimal CustomerRetentionRate { get; set; }
        public decimal FefoRiskInventoryValue { get; set; }
        public string FefoRiskInventoryValueFormatted { get; set; } = "0 đ";
        public int FefoRiskBatchCount { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public string TotalInventoryValueFormatted { get; set; } = "0 đ";
        public int NewCustomersThisMonth { get; set; }
        public int TotalProducts { get; set; }
        public int TotalStores { get; set; }
        public decimal TodayVsYesterdayRevenueGrowth { get; set; }
        public decimal MonthVsLastMonthRevenueGrowth { get; set; }
        public decimal TodayVsYesterdayOrderGrowth { get; set; }
        public decimal MonthRevenueGrowth { get => MonthVsLastMonthRevenueGrowth; set => MonthVsLastMonthRevenueGrowth = value; }
        public decimal TodayRevenueGrowth { get => TodayVsYesterdayRevenueGrowth; set => TodayVsYesterdayRevenueGrowth = value; }
        public decimal TodayOrderGrowth { get => TodayVsYesterdayOrderGrowth; set => TodayVsYesterdayOrderGrowth = value; }
        public decimal MonthOnlinePercentage => MonthTotalOrders > 0 ? Math.Round((decimal)MonthOnlineOrders / MonthTotalOrders * 100m, 1) : 0;
        public decimal MonthPosPercentage => MonthTotalOrders > 0 ? Math.Round((decimal)MonthPosOrders / MonthTotalOrders * 100m, 1) : 0;
        public string LastUpdated { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO yêu cầu kích hoạt Flash Sale Smart Clearance cho lô hàng FEFO
    /// </summary>
    public class PushFlashSaleRequestDto
    {
        public int BatchId { get; set; }
        public decimal? DiscountPercent { get; set; } = 50;
    }
}

