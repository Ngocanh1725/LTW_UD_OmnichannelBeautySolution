using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Notifications
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string? Role { get; set; }
        public int? StoreId { get; set; }
        public string? StoreName { get; set; }
        public string Type { get; set; } = "SYSTEM"; // ORDER, WAREHOUSE, SYSTEM, ALERT
        public string Severity { get; set; } = "INFO"; // INFO, SUCCESS, WARNING, DANGER
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Url { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }

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

        public string TypeName => Type switch
        {
            "ORDER" => "Đơn hàng",
            "WAREHOUSE" => "Kho hàng",
            "ALERT" => "Cảnh báo AI",
            _ => "Hệ thống"
        };

        public string TypeIconClass => Type switch
        {
            "ORDER" => "fa-solid fa-box text-rose-500",
            "WAREHOUSE" => "fa-solid fa-boxes-stacked text-amber-500",
            "ALERT" => "fa-solid fa-triangle-exclamation text-rose-500",
            _ => "fa-solid fa-bell text-sky-500"
        };

        public string TypeBadgeClass => Type switch
        {
            "ORDER" => "bg-rose-50 text-rose-700 dark:bg-rose-950/50 dark:text-rose-400 border border-rose-200 dark:border-rose-800",
            "WAREHOUSE" => "bg-amber-50 text-amber-800 dark:bg-amber-950/50 dark:text-amber-400 border border-amber-200 dark:border-amber-800",
            "ALERT" => "bg-purple-50 text-purple-700 dark:bg-purple-950/50 dark:text-purple-400 border border-purple-200 dark:border-purple-800",
            _ => "bg-sky-50 text-sky-700 dark:bg-sky-950/50 dark:text-sky-400 border border-sky-200 dark:border-sky-800"
        };

        public string SeverityBorderClass => Severity switch
        {
            "DANGER" => "border-l-4 border-l-rose-500",
            "WARNING" => "border-l-4 border-l-amber-500",
            "SUCCESS" => "border-l-4 border-l-emerald-500",
            _ => "border-l-4 border-l-sky-500"
        };
    }

    public class CreateNotificationDto
    {
        public int? UserId { get; set; }
        public string? Role { get; set; }
        public int? StoreId { get; set; }
        public string Type { get; set; } = "SYSTEM";
        public string Severity { get; set; } = "INFO";
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Url { get; set; }
    }

    public class NotificationFilterDto
    {
        public string Tab { get; set; } = "ALL"; // ALL, UNREAD, ORDER, WAREHOUSE, SYSTEM
        public string? Type { get; set; }
        public string? Severity { get; set; }
        public bool? IsRead { get; set; }
        public string? SearchTerm { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class PagedNotificationResultDto
    {
        public List<NotificationDto> Items { get; set; } = new();
        public int TotalItems { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalItems / PageSize) : 1;
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
        public int OrderCount { get; set; }
        public int WarehouseCount { get; set; }
        public int SystemCount { get; set; }
    }
}
