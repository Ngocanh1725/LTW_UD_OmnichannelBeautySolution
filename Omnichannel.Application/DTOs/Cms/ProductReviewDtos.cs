using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Cms
{
    public class ProductReviewDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public string UserName { get; set; } = "Khách hàng";
        public int? OrderId { get; set; }
        public int Rating { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public List<string> Images { get; set; } = new();
        public bool IsVerifiedPurchase { get; set; }
        public bool IsApproved { get; set; }
        public bool IsHidden { get; set; }
        public string? AdminNote { get; set; }
        public string? SkinType { get; set; }
        public string? AgeRange { get; set; }

        // --- Sub-criteria & Social Proof ---
        public string? Scent { get; set; }
        public string? Longevity { get; set; }
        public string? SkinFeel { get; set; }
        public int HelpfulCount { get; set; }

        // --- Official CSKH response ---
        public string? AdminReply { get; set; }
        public DateTime? AdminRepliedAt { get; set; }
        public string? AdminReplyBy { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class CreateReviewDto
    {
        public int ProductId { get; set; }
        public int? OrderId { get; set; }
        public int Rating { get; set; } = 5;
        public string? Title { get; set; }
        public string? Content { get; set; }
        public List<string>? Images { get; set; }
        public string? SkinType { get; set; }
        public string? AgeRange { get; set; }

        // --- Sub-criteria ---
        public string? Scent { get; set; }
        public string? Longevity { get; set; }
        public string? SkinFeel { get; set; }
    }

    public class ReviewFilterDto
    {
        public int? ProductId { get; set; }
        public int? Rating { get; set; }
        public bool? IsApproved { get; set; }
        public bool? IsHidden { get; set; }
        public bool? IsVerifiedPurchase { get; set; }
        public bool? HasImages { get; set; }
        public string? Tab { get; set; } // "all", "images", "verified", "5star"
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class ReviewStatsDto
    {
        public int ProductId { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int WithImagesCount { get; set; }
        public int VerifiedCount { get; set; }

        public int Rating5Count { get; set; }
        public double Rating5Percentage { get; set; }

        public int Rating4Count { get; set; }
        public double Rating4Percentage { get; set; }

        public int Rating3Count { get; set; }
        public double Rating3Percentage { get; set; }

        public int Rating2Count { get; set; }
        public double Rating2Percentage { get; set; }

        public int Rating1Count { get; set; }
        public double Rating1Percentage { get; set; }
    }
}
