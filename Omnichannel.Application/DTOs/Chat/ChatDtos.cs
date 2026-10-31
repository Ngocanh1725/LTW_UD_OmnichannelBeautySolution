using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Chat
{
    public class ChatRequestDto
    {
        public string SessionKey { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
    }

    public class ChatProductSuggestionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public string? ImageUrl { get; set; }
        public string? Category { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? Badge { get; set; }
        public string? Subtitle { get; set; }
    }

    public class ChatBotResponseDto
    {
        public bool Success { get; set; } = true;
        public string SessionKey { get; set; } = string.Empty;
        public string Reply { get; set; } = string.Empty;
        public string Intent { get; set; } = "general";
        public List<string> QuickReplies { get; set; } = new();
        public List<ChatProductSuggestionDto> ProductSuggestions { get; set; } = new();
        public string? ActionUrl { get; set; }
        public string? ActionText { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ChatMessageDto
    {
        public int Id { get; set; }
        public string Sender { get; set; } = "USER";
        public string Content { get; set; } = string.Empty;
        public string? Intent { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ChatSessionSummaryDto
    {
        public int Id { get; set; }
        public string SessionKey { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime LastMessageAt { get; set; }
        public int MessageCount { get; set; }
        public string DetectedTopic { get; set; } = "GENERAL";
        public string? FirstMessage { get; set; }
        public string? LastMessage { get; set; }
    }

    public class ChatSessionDetailDto
    {
        public ChatSessionSummaryDto Session { get; set; } = new();
        public List<ChatMessageDto> Messages { get; set; } = new();
    }

    public class ChatSummaryStatsDto
    {
        public int TotalSessions { get; set; }
        public int TodaySessions { get; set; }
        public int TotalMessages { get; set; }
        public Dictionary<string, int> TopicBreakdown { get; set; } = new();
        public List<KeyValuePair<string, int>> MostCommonQuestions { get; set; } = new();
    }

    public class ChatHistoryFilterDto
    {
        public string? Search { get; set; }
        public string? Topic { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
    }

    public class ChatHistoryViewModel
    {
        public List<ChatSessionSummaryDto> Sessions { get; set; } = new();
        public ChatSummaryStatsDto Stats { get; set; } = new();
        public ChatHistoryFilterDto Filter { get; set; } = new();
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / Math.Max(1, Filter.PageSize));
    }
}
