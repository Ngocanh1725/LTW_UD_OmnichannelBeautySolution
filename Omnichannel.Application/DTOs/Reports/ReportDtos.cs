using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Reports
{
    /// <summary>
    /// ViewModel tổng hợp toàn bộ số liệu thống kê Dashboard & Báo cáo doanh thu đa kênh
    /// </summary>
    public class RevenueDashboardViewModel
    {
        // Doanh thu & số đơn hàng phát sinh trong ngày hôm nay
        public decimal TodayRevenue { get; set; }
        public int TodayOrderCount { get; set; }
        public int TodayOrdersCount { get; set; }

        // Doanh thu & số đơn hàng phát sinh trong tháng hiện tại
        public decimal MonthRevenue { get; set; }
        public int MonthOrderCount { get; set; }
        public int MonthOrdersCount { get; set; }

        // Tổng lũy kế toàn hệ thống
        public decimal TotalRevenue { get; set; }
        public int TotalOrderCount { get; set; }
        public int TotalOrdersCount { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalProducts { get; set; }

        // =========================================================================
        // THÔNG TIN BỘ LỌC BÁO CÁO
        // =========================================================================
        public string Period { get; set; } = "7days"; // today, 7days, 30days, custom
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? SelectedStoreId { get; set; }
        public string SelectedStoreName { get; set; } = "Toàn hệ thống";
        public List<StoreSelectDto> Stores { get; set; } = new();

        // =========================================================================
        // CÁC CHỈ SỐ KPI THEO KỲ ĐƯỢC LỌC
        // =========================================================================
        public decimal FilteredRevenue { get; set; }
        public int FilteredOrderCount { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal EstimatedGrossProfit { get; set; }

        // =========================================================================
        // BIỂU ĐỒ DOANH THU ĐA CHẾ ĐỘ: NGÀY / TUẦN / THÁNG
        // =========================================================================
        public List<DailyRevenueDto> DailyRevenues { get; set; } = new();
        public List<WeeklyRevenueDto> WeeklyRevenues { get; set; } = new();
        public List<MonthlyRevenueDto> MonthlyRevenues { get; set; } = new();

        // =========================================================================
        // BIỂU ĐỒ BAR: SO SÁNH DOANH THU GIỮA CÁC CHI NHÁNH
        // =========================================================================
        public List<StoreRevenueDistributionDto> StoreDistributions { get; set; } = new();

        // =========================================================================
        // BIỂU ĐỒ DOUGHNUT: TỶ TRỌNG KÊNH BÁN (POS VS WEBSITE)
        // =========================================================================
        public List<ChannelRevenueDistributionDto> ChannelDistributions { get; set; } = new();

        // =========================================================================
        // BIỂU ĐỒ AREA / BAR: TOP DANH MỤC SẢN PHẨM BÁN CHẠY
        // =========================================================================
        public List<CategoryRevenueDto> TopCategories { get; set; } = new();

        // Top 5 sản phẩm bán chạy nhất
        public List<TopProductDto> TopSellingProducts { get; set; } = new();
        public List<TopProductDto> TopProducts { get; set; } = new();

        // Danh sách 10 đơn hàng gần nhất (trên Dashboard)
        public List<RecentOrderDto> RecentOrders { get; set; } = new();

        // Danh sách toàn bộ đơn hàng chi tiết trong kỳ lọc (cho bảng chi tiết & Export Excel/CSV)
        public List<OrderReportItemDto> DetailedOrders { get; set; } = new();

        // Cảnh báo các lô hàng cận date & quá hạn theo thuật toán FEFO
        public List<ExpiringBatchDto> ExpiringBatches { get; set; } = new();
        public List<ExpiringBatchDto> FefoAlertBatches { get; set; } = new();
    }

    /// <summary>
    /// DTO thể hiện doanh thu và số lượng đơn hàng theo từng ngày
    /// </summary>
    public class DailyRevenueDto
    {
        public string Date { get; set; } = string.Empty; // dd/MM
        public string DateLabel { get; set; } = string.Empty; // dd/MM/yyyy
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    /// <summary>
    /// DTO thể hiện sản phẩm bán chạy nhất theo số lượng và doanh thu
    /// </summary>
    public class TopProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    /// <summary>
    /// DTO hiển thị danh sách đơn hàng gần đây trên Dashboard
    /// </summary>
    public class RecentOrderDto
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string OrderSource { get; set; } = "POS";
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO thể hiện thông tin các lô hàng cận date cần ưu tiên xuất kho theo thuật toán FEFO
    /// </summary>
    public class ExpiringBatchDto
    {
        public int BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public DateTime ExpDate { get; set; }
        public int DaysRemaining { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; } = string.Empty; // ACTIVE, NEAR_EXPIRATION, EXPIRED
    }

    public class StoreSelectDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class OrderReportItemDto
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string OrderSource { get; set; } = "POS"; // POS, WEBSITE, MOBILE_APP
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string OrderStatus { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }

    public class CategoryRevenueDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int QuantitySold { get; set; }
        public decimal Percentage { get; set; }
    }

    public class WeeklyRevenueDto
    {
        public string WeekLabel { get; set; } = string.Empty; // e.g. "Tuần 39 (21/09 - 27/09)"
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class MonthlyRevenueDto
    {
        public string MonthLabel { get; set; } = string.Empty; // e.g. "Tháng 09/2026"
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }
}