using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Alerts
{
    public class AlertDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Severity { get; set; } = "INFO"; // DANGER, WARNING, SUCCESS, INFO
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public int? UserId { get; set; }
        public int? StoreId { get; set; }
        public string? StoreName { get; set; }
        public string? ActionUrl { get; set; }
        public string? EntityKey { get; set; }

        public string TimeAgo
        {
            get
            {
                var diff = DateTime.UtcNow - CreatedAt;
                if (diff.TotalSeconds < 60) return "Vừa xong";
                if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} phút trước";
                if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} giờ trước";
                if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} ngày trước";
                return CreatedAt.ToString("dd/MM/yyyy HH:mm");
            }
        }

        public string SeverityBadgeClass => Severity switch
        {
            "DANGER" => "bg-rose-100 text-rose-700 dark:bg-rose-950/50 dark:text-rose-400 border border-rose-200 dark:border-rose-800",
            "WARNING" => "bg-amber-100 text-amber-800 dark:bg-amber-950/50 dark:text-amber-400 border border-amber-200 dark:border-amber-800",
            "SUCCESS" => "bg-emerald-100 text-emerald-700 dark:bg-emerald-950/50 dark:text-emerald-400 border border-emerald-200 dark:border-emerald-800",
            _ => "bg-sky-100 text-sky-700 dark:bg-sky-950/50 dark:text-sky-400 border border-sky-200 dark:border-sky-800"
        };

        public string SeverityBorderClass => Severity switch
        {
            "DANGER" => "border-l-4 border-l-rose-500",
            "WARNING" => "border-l-4 border-l-amber-500",
            "SUCCESS" => "border-l-4 border-l-emerald-500",
            _ => "border-l-4 border-l-sky-500"
        };

        public string TypeIconClass => Type switch
        {
            "OUT_OF_STOCK" => "fa-solid fa-boxes-packing text-rose-500",
            "LOW_STOCK" => "fa-solid fa-triangle-exclamation text-amber-500",
            "EXPIRY_CRITICAL" => "fa-solid fa-calendar-xmark text-rose-500",
            "EXPIRY_WARNING" => "fa-solid fa-clock-rotate-left text-amber-500",
            "REVENUE_ANOMALY" => "fa-solid fa-arrow-trend-up text-emerald-500",
            "PENDING_ORDERS_DELAY" => "fa-solid fa-clock text-sky-500",
            _ => "fa-solid fa-bell text-slate-400"
        };

        public string TypeName => Type switch
        {
            "OUT_OF_STOCK" => "Hết Hàng (Stock = 0)",
            "LOW_STOCK" => "Sắp Hết Hàng (<= Điểm đặt)",
            "EXPIRY_CRITICAL" => "Cận Date Cấp Bách (<= 15 ngày)",
            "EXPIRY_WARNING" => "Cảnh Báo Cận Date (<= 60 ngày)",
            "REVENUE_ANOMALY" => "Đột Biến Doanh Thu",
            "PENDING_ORDERS_DELAY" => "Đơn Hàng Treo Quá 24h",
            _ => "Cảnh báo hệ thống"
        };
    }

    public class AlertFilterDto
    {
        public string? Type { get; set; }
        public string? Severity { get; set; }
        public bool? IsRead { get; set; }
        public int? StoreId { get; set; }
        public string? SearchTerm { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class PagedAlertResultDto
    {
        public List<AlertDto> Items { get; set; } = new();
        public int TotalItems { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalItems / PageSize) : 1;
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public int UnreadCount { get; set; }
        public int DangerCount { get; set; }
        public int WarningCount { get; set; }
        public int SuccessCount { get; set; }
        public int InfoCount { get; set; }
    }

    public class AlertSummaryDto
    {
        public int TotalAlerts { get; set; }
        public int UnreadCount { get; set; }
        public int DangerCount { get; set; }
        public int WarningCount { get; set; }
        public int SuccessCount { get; set; }
        public int InfoCount { get; set; }
        public List<AlertDto> LatestAlerts { get; set; } = new();
    }
}
