using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Reports
{
    /// <summary>
    /// ViewModel trung tâm ra quyết định toàn diện dành cho Admin Dashboard (Phiên D2 - GAP-11)
    /// </summary>
    public class DashboardViewModel
    {
        // =========================================================================
        // 0. BỘ LỌC TOÀN CỤC (GLOBAL FILTER BAR)
        // =========================================================================
        public int SelectedDays { get; set; } = 30; // 7, 30, 90
        public string SelectedPeriodType { get; set; } = "30"; // "7", "30", "90", "custom"
        public DateTime FilterFromDate { get; set; }
        public DateTime FilterToDate { get; set; }
        public string SelectedChannel { get; set; } = "ALL"; // ALL, ONLINE, POS
        public int? SelectedStoreId { get; set; }
        public List<StoreSelectDto> AvailableStores { get; set; } = new();

        // =========================================================================
        // 1. 6 THẺ KPI TRUNG TÂM CÓ SO SÁNH KỲ TRƯỚC (▲▼ %)
        // =========================================================================
        // KPI 1: Doanh thu thuần (Net Revenue - đơn không hủy trong kỳ lọc)
        public decimal PeriodNetRevenue { get; set; }
        public decimal PrevPeriodNetRevenue { get; set; }
        public decimal PeriodNetRevenueGrowth { get; set; }

        // KPI 2: Tổng số đơn hàng hợp lệ (Total Orders)
        public int PeriodOrderCount { get; set; }
        public int PrevPeriodOrderCount { get; set; }
        public decimal PeriodOrderCountGrowth { get; set; }

        // KPI 3: Giá trị đơn hàng trung bình (AOV = NetRevenue / OrderCount)
        public decimal PeriodAov { get; set; }
        public decimal PrevPeriodAov { get; set; }
        public decimal PeriodAovGrowth { get; set; }

        // KPI 4: Tỷ lệ hủy đơn (Cancellation Rate %)
        public int PeriodCancelledOrders { get; set; }
        public int PeriodAllOrdersIncludingCancelled { get; set; }
        public decimal PeriodCancellationRate { get; set; }
        public decimal PrevPeriodCancellationRate { get; set; }
        public decimal PeriodCancellationRateDiff { get; set; } // Điểm % thay đổi so với kỳ trước

        // KPI 5: Khách hàng mới trong kỳ (New Customers)
        public int PeriodNewCustomers { get; set; }
        public int PrevPeriodNewCustomers { get; set; }
        public decimal PeriodNewCustomersGrowth { get; set; }

        // KPI 6: Giá trị hàng cận date FEFO (Near-Expiry FEFO Value <= 90 ngày)
        public decimal FefoRiskInventoryValue { get; set; }
        public int FefoRiskBatchCount { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public string TotalInventoryValueFormatted => TotalInventoryValue.ToString("N0") + " đ";

        // =========================================================================
        // TƯƠNG THÍCH NGƯỢC VỚI CÁC PROPERTY CŨ
        // =========================================================================
        public decimal TodayRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public int TodayOrderCount { get; set; }
        public int NewCustomersThisMonth { get; set; }
        public int TotalProducts { get; set; }
        public int TotalStores { get; set; }

        public int MonthTotalOrders { get; set; }
        public int MonthOnlineOrders { get; set; }
        public int MonthPosOrders { get; set; }
        public decimal MonthOnlinePercentage => MonthTotalOrders > 0 ? Math.Round((decimal)MonthOnlineOrders / MonthTotalOrders * 100m, 1) : 0;
        public decimal MonthPosPercentage => MonthTotalOrders > 0 ? Math.Round((decimal)MonthPosOrders / MonthTotalOrders * 100m, 1) : 0;

        public decimal CustomerRetentionRate { get; set; }

        public decimal TodayVsYesterdayRevenueGrowth { get; set; }
        public decimal MonthVsLastMonthRevenueGrowth { get; set; }
        public decimal TodayVsYesterdayOrderGrowth { get; set; }

        // =========================================================================
        // 3. BIỂU ĐỒ DOANH THU THEO NGÀY & THEO KÊNH (Line Chart)
        // =========================================================================
        public List<DailyChannelRevenueDto> DailyChannelRevenues { get; set; } = new();
        public List<DailyRevenueDto> DailyRevenues { get; set; } = new();

        // =========================================================================
        // 4. CƠ CẤU DOANH THU THEO DANH MỤC (Doughnut Chart)
        // =========================================================================
        public List<CategoryRevenueShareDto> CategoryRevenues { get; set; } = new();

        // =========================================================================
        // 5. TOP 10 SẢN PHẨM & TOP 10 THƯƠNG HIỆU BÁN CHẠY (Bar Chart & Table)
        // =========================================================================
        public List<TopProductDto> TopProducts { get; set; } = new();
        public List<TopBrandRevenueDto> TopBrands { get; set; } = new();
        public List<RecentOrderDto> RecentOrders { get; set; } = new();

        // =========================================================================
        // 6. HEATMAP GIỜ × THỨ ĐẶT HÀNG (7 Thứ × 6 Khung giờ / 24h)
        // =========================================================================
        public List<OrderHeatmapCellDto> OrderHeatmapCells { get; set; } = new();
        public string PeakOrderTimeWindow { get; set; } = string.Empty;

        // =========================================================================
        // 7. PHỄU CHUYỂN ĐỔI 5 BƯỚC (CONVERSION FUNNEL)
        // =========================================================================
        public ConversionFunnelDto ConversionFunnel { get; set; } = new();

        // =========================================================================
        // 8. PHÂN KHÚC KHÁCH HÀNG RFM (1-5 SCORING)
        // =========================================================================
        public List<RfmSegmentSummaryDto> RfmSegments { get; set; } = new();
        public List<RfmCustomerItemDto> RfmCustomers { get; set; } = new();

        // =========================================================================
        // 9. CẢNH BÁO THÔNG MINH & GỢI Ý ĐIỀU CHUYỂN KHO (SMART ALERTS)
        // =========================================================================
        public List<NearExpiryBatchAlertDto> NearExpiryBatches { get; set; } = new();
        public List<LowStockAlertDto> LowStockAlerts { get; set; } = new();
        public List<SmartTransferSuggestionDto> SmartTransferSuggestions { get; set; } = new();

        // =========================================================================
        // 10. PHÂN BỔ DOANH THU THEO KÊNH BÁN & CHI NHÁNH
        // =========================================================================
        public List<ChannelRevenueDistributionDto> ChannelDistributions { get; set; } = new();
        public List<StoreRevenueDistributionDto> StoreDistributions { get; set; } = new();
        public Revenue7DaysComparisonDto Revenue7DaysComparison { get; set; } = new();

        // =========================================================================
        // 11. THÔNG TIN PHẠM VI CHI NHÁNH (STORE ISOLATION)
        // =========================================================================
        public bool IsStoreScoped { get; set; }
        public int? CurrentStoreId { get; set; }
        public string? CurrentStoreName { get; set; }
    }

    public class TopBrandRevenueDto
    {
        public int BrandId { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class OrderHeatmapCellDto
    {
        /// <summary>
        /// 0 = Thứ 2, 1 = Thứ 3, ..., 6 = Chủ Nhật
        /// </summary>
        public int DayIndex { get; set; }
        public string DayName { get; set; } = string.Empty;
        /// <summary>
        /// 0 = 00-04h, 1 = 04-08h, 2 = 08-12h, 3 = 12-16h, 4 = 16-20h, 5 = 20-24h
        /// </summary>
        public int SlotIndex { get; set; }
        public string SlotLabel { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
        public int IntensityPercent { get; set; } // 0..100
    }

    public class ConversionFunnelDto
    {
        public int ProductViews { get; set; }
        public int AddToCartCount { get; set; }
        public int CheckoutCount { get; set; }
        public int OrdersPlacedCount { get; set; }
        public int OrdersCompletedCount { get; set; }

        public decimal ViewToCartRate => ProductViews > 0 ? Math.Round((decimal)AddToCartCount / ProductViews * 100m, 1) : 0;
        public decimal CartToCheckoutRate => AddToCartCount > 0 ? Math.Round((decimal)CheckoutCount / AddToCartCount * 100m, 1) : 0;
        public decimal CheckoutToOrderRate => CheckoutCount > 0 ? Math.Round((decimal)OrdersPlacedCount / CheckoutCount * 100m, 1) : 0;
        public decimal OrderToCompleteRate => OrdersPlacedCount > 0 ? Math.Round((decimal)OrdersCompletedCount / OrdersPlacedCount * 100m, 1) : 0;
        public decimal OverallConversionRate => ProductViews > 0 ? Math.Round((decimal)OrdersCompletedCount / ProductViews * 100m, 2) : 0;

        public string MethodologyNote { get; set; } =
            "Tính toán từ dữ liệu thực trong DB: Hoàn tất (Orders COMPLETED), Đặt hàng (Orders trong kỳ), Checkout (Orders + Giỏ hàng đang hoạt động), Thêm giỏ (CartItems + Orders), và Lượt xem sản phẩm ước lượng theo tín hiệu tương tác thực tế (Orders, Carts, Reviews, Q&A).";
    }

    public class RfmSegmentSummaryDto
    {
        public string SegmentCode { get; set; } = string.Empty; // VIP, LOYAL, AT_RISK, HIBERNATING
        public string SegmentName { get; set; } = string.Empty; // Khách VIP, Trung thành, Có nguy cơ rời bỏ, Ngủ đông
        public string Description { get; set; } = string.Empty;
        public string BadgeColor { get; set; } = "rose";
        public int CustomerCount { get; set; }
        public decimal SharePercentage { get; set; }
        public decimal TotalMonetary { get; set; }
        public decimal AvgMonetary => CustomerCount > 0 ? Math.Round(TotalMonetary / CustomerCount, 0) : 0;
        public string SuggestedAction { get; set; } = string.Empty;
        public decimal SuggestedDiscountPercent { get; set; } = 15m;
    }

    public class RfmCustomerItemDto
    {
        public int? UserId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime LastOrderDate { get; set; }
        public int RecencyDays { get; set; }
        public int FrequencyOrders { get; set; }
        public decimal MonetaryTotal { get; set; }
        public int RScore { get; set; } // 1..5
        public int FScore { get; set; } // 1..5
        public int MScore { get; set; } // 1..5
        public string RfmScoreText => $"{RScore}{FScore}{MScore}";
        public string SegmentCode { get; set; } = string.Empty;
        public string SegmentName { get; set; } = string.Empty;
    }

    public class LowStockAlertDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public int AvailableQuantity { get; set; }
        public int ReorderPoint { get; set; } = 15;
    }

    public class SmartTransferSuggestionDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public int FromStoreId { get; set; }
        public string FromStoreName { get; set; } = string.Empty;
        public int FromStoreAvailable { get; set; }
        public int ToStoreId { get; set; }
        public string ToStoreName { get; set; } = string.Empty;
        public int ToStoreAvailable { get; set; }
        public int SuggestedTransferQty { get; set; }
        public string ActionUrl => $"/Admin/StockTransfer?fromStoreId={FromStoreId}&toStoreId={ToStoreId}&productId={ProductId}";
    }

    public class CreateTargetedVoucherRequestDto
    {
        public string SegmentCode { get; set; } = "AT_RISK";
        public decimal? DiscountPercent { get; set; }
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
    /// DTO doanh thu theo ngày phân tách kênh Online vs POS
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
        public string ChannelKey { get; set; } = string.Empty;
        public string ChannelName { get; set; } = string.Empty;
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
