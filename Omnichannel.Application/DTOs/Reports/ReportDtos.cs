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

        // Biểu đồ doanh thu 7 ngày gần nhất
        public List<DailyRevenueDto> DailyRevenues { get; set; } = new();

        // Top 5 sản phẩm bán chạy nhất
        public List<TopProductDto> TopSellingProducts { get; set; } = new();
        public List<TopProductDto> TopProducts { get; set; } = new();

        // Danh sách 10 đơn hàng gần nhất
        public List<RecentOrderDto> RecentOrders { get; set; } = new();

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
}