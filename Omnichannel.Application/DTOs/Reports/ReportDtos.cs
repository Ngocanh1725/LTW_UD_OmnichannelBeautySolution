using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Reports
{
    public class RevenueDashboardViewModel
    {
        public decimal TodayRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public int TodayOrderCount { get; set; }
        public decimal TodayProfitEstimate { get; set; }

        public decimal PosRevenueRatio { get; set; }
        public decimal OnlineRevenueRatio { get; set; }

        public List<DailyRevenuePointDto> RevenueLast7Days { get; set; } = new();
        public List<TopProductDto> TopSellingProducts { get; set; } = new();
        public List<NearExpiryProductDto> CriticalExpiringBatches { get; set; } = new();
    }

    public class DailyRevenuePointDto
    {
        public string DateLabel { get; set; } = string.Empty; // dd/MM
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class TopProductDto
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int SoldQuantity { get; set; }
        public decimal TotalSales { get; set; }
    }

    public class NearExpiryProductDto
    {
        public string StoreName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime ExpDate { get; set; }
        public int DaysRemaining => (ExpDate.Date - DateTime.Today).Days;
        public int RemainingQuantity { get; set; }
    }
}