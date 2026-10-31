using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Messages
{
    public class InternalMessageDto
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string? SenderAvatar { get; set; }
        public string SenderRole { get; set; } = string.Empty;

        public int ReceiverId { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string? ReceiverAvatar { get; set; }
        public string ReceiverRole { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? ReadAt { get; set; }

        public string TimeAgo
        {
            get
            {
                var diff = DateTime.UtcNow - SentAt;
                if (diff.TotalSeconds < 60) return "Vừa xong";
                if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} phút trước";
                if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} giờ trước";
                if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} ngày trước";
                return SentAt.ToString("dd/MM/yyyy HH:mm");
            }
        }

        public string PreviewText
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Body)) return string.Empty;
                var singleLine = Body.Replace("\r\n", " ").Replace("\n", " ").Trim();
                return singleLine.Length > 90 ? singleLine.Substring(0, 87) + "..." : singleLine;
            }
        }

        public string SenderInitials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SenderName)) return "U";
                var parts = SenderName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
                return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
            }
        }
    }

    public class ComposeMessageDto
    {
        public int ReceiverId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }

    public class MessageFolderDto
    {
        public string Folder { get; set; } = "inbox"; // "inbox" | "sent"
        public int UnreadCount { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;
        public List<InternalMessageDto> Items { get; set; } = new();
    }

    public class UserRecipientDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? StoreName { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
