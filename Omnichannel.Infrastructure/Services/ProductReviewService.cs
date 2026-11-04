using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Cms;
using Omnichannel.Application.Interfaces.Cms;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services
{
    public class ProductReviewService : IProductReviewService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductReviewService> _logger;

        public ProductReviewService(ApplicationDbContext context, ILogger<ProductReviewService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> SubmitReviewAsync(CreateReviewDto dto, int? userId = null, CancellationToken ct = default)
        {
            var productExists = await _context.Products.AnyAsync(p => p.Id == dto.ProductId, ct);
            if (!productExists)
            {
                throw new InvalidOperationException($"Không tìm thấy sản phẩm với ID {dto.ProductId}.");
            }

            // Tự động kiểm tra OrderId xem User đó có thực sự mua Product không -> set IsVerifiedPurchase
            bool isVerified = false;
            int? matchedOrderId = dto.OrderId;

            if (userId.HasValue)
            {
                if (matchedOrderId.HasValue)
                {
                    isVerified = await _context.Orders
                        .AnyAsync(o => o.Id == matchedOrderId.Value
                                    && o.UserId == userId.Value
                                    && o.OrderDetails.Any(od => od.ProductId == dto.ProductId)
                                    && (o.OrderStatus == "COMPLETED" || o.PaymentStatus == "PAID"), ct);
                }
                else
                {
                    var previousOrder = await _context.Orders
                        .AsNoTracking()
                        .Where(o => o.UserId == userId.Value
                                 && (o.OrderStatus == "COMPLETED" || o.PaymentStatus == "PAID")
                                 && o.OrderDetails.Any(od => od.ProductId == dto.ProductId))
                        .OrderByDescending(o => o.CreatedAt)
                        .FirstOrDefaultAsync(ct);

                    if (previousOrder != null)
                    {
                        matchedOrderId = previousOrder.Id;
                        isVerified = true;
                    }
                }
            }

            string? imagesJson = null;
            if (dto.Images != null && dto.Images.Any())
            {
                imagesJson = JsonSerializer.Serialize(dto.Images);
            }

            var review = new ProductReview
            {
                ProductId = dto.ProductId,
                UserId = userId,
                OrderId = matchedOrderId,
                Rating = Math.Clamp(dto.Rating, 1, 5),
                Title = dto.Title?.Trim(),
                Content = dto.Content?.Trim(),
                ImagesJson = imagesJson,
                IsVerifiedPurchase = isVerified,
                IsApproved = true, // Tự động duyệt hiển thị tức thì trên Storefront
                IsHidden = false,
                SkinType = dto.SkinType?.Trim(),
                AgeRange = dto.AgeRange?.Trim(),
                Scent = dto.Scent?.Trim(),
                Longevity = dto.Longevity?.Trim(),
                SkinFeel = dto.SkinFeel?.Trim(),
                HelpfulCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProductReviews.Add(review);
            var result = await _context.SaveChangesAsync(ct) > 0;

            _logger.LogInformation("Khách hàng {UserId} đã gửi đánh giá cho sản phẩm {ProductId}. VerifiedPurchase: {Verified}",
                userId, dto.ProductId, isVerified);

            return result;
        }

        public async Task<bool> ApproveReviewAsync(int reviewId, bool isApproved, string? adminNote = null, CancellationToken ct = default)
        {
            var review = await _context.ProductReviews.FindAsync(new object[] { reviewId }, ct);
            if (review == null) return false;

            review.IsApproved = isApproved;
            if (!string.IsNullOrWhiteSpace(adminNote))
            {
                review.AdminNote = adminNote.Trim();
            }
            review.UpdatedAt = DateTime.UtcNow;

            return await _context.SaveChangesAsync(ct) > 0;
        }

        public async Task<bool> ToggleHideReviewAsync(int reviewId, bool isHidden, string? adminNote = null, CancellationToken ct = default)
        {
            var review = await _context.ProductReviews.FindAsync(new object[] { reviewId }, ct);
            if (review == null) return false;

            review.IsHidden = isHidden;
            if (!string.IsNullOrWhiteSpace(adminNote))
            {
                review.AdminNote = adminNote.Trim();
            }
            review.UpdatedAt = DateTime.UtcNow;

            return await _context.SaveChangesAsync(ct) > 0;
        }

        public Task<List<ProductReviewDto>> GetProductReviewsAsync(int productId, int page, int pageSize, int? ratingFilter, string? filterTab, CancellationToken ct)
        {
            return GetProductReviewsAsync(productId, page, pageSize, ratingFilter, filterTab, skinTypeFilter: null, sortBy: null, ct: ct);
        }

        public async Task<List<ProductReviewDto>> GetProductReviewsAsync(int productId, int page = 1, int pageSize = 10, int? ratingFilter = null, string? filterTab = null, string? skinTypeFilter = null, string? sortBy = null, CancellationToken ct = default)
        {
            var query = _context.ProductReviews
                .AsNoTracking()
                .Include(r => r.Product)
                .Include(r => r.User)
                .Where(r => r.ProductId == productId && r.IsApproved && !r.IsHidden);

            if (ratingFilter.HasValue && ratingFilter.Value >= 1 && ratingFilter.Value <= 5)
            {
                query = query.Where(r => r.Rating == ratingFilter.Value);
            }

            if (!string.IsNullOrWhiteSpace(filterTab))
            {
                switch (filterTab.Trim().ToLowerInvariant())
                {
                    case "images":
                    case "with_images":
                        query = query.Where(r => !string.IsNullOrEmpty(r.ImagesJson) && r.ImagesJson != "[]" && r.ImagesJson != "null");
                        break;
                    case "verified":
                        query = query.Where(r => r.IsVerifiedPurchase);
                        break;
                    case "5star":
                    case "star5":
                        query = query.Where(r => r.Rating == 5);
                        break;
                }
            }

            // Bộ lọc loại da Skin Twin
            if (!string.IsNullOrWhiteSpace(skinTypeFilter) && !skinTypeFilter.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                var stLower = skinTypeFilter.Trim().ToLower();
                query = query.Where(r => r.SkinType != null && r.SkinType.ToLower().Contains(stLower));
            }

            // Sắp xếp
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                switch (sortBy.Trim().ToLowerInvariant())
                {
                    case "helpful":
                    case "most_helpful":
                        query = query.OrderByDescending(r => r.HelpfulCount).ThenByDescending(r => r.CreatedAt);
                        break;
                    case "highest":
                    case "highest_rating":
                    case "rating_desc":
                        query = query.OrderByDescending(r => r.Rating).ThenByDescending(r => r.CreatedAt);
                        break;
                    case "lowest":
                    case "lowest_rating":
                        query = query.OrderBy(r => r.Rating).ThenByDescending(r => r.CreatedAt);
                        break;
                    case "newest":
                    default:
                        query = query.OrderByDescending(r => r.CreatedAt);
                        break;
                }
            }
            else
            {
                query = query.OrderByDescending(r => r.CreatedAt);
            }

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var reviews = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var dtos = reviews.Select(MapToDto).ToList();

            if (!string.IsNullOrWhiteSpace(skinTypeFilter) && !skinTypeFilter.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var dto in dtos)
                {
                    dto.IsSkinTwinMatch = !string.IsNullOrEmpty(dto.SkinType) &&
                        dto.SkinType.ToLower().Contains(skinTypeFilter.Trim().ToLower());
                }
            }

            return dtos;
        }

        public async Task<ReviewStatsDto> GetReviewStatsAsync(int productId, CancellationToken ct = default)
        {
            var approvedReviews = await _context.ProductReviews
                .AsNoTracking()
                .Where(r => r.ProductId == productId && r.IsApproved && !r.IsHidden)
                .Select(r => new { r.Rating, r.IsVerifiedPurchase, r.ImagesJson })
                .ToListAsync(ct);

            var ratings = approvedReviews.Select(r => r.Rating).ToList();

            var stats = new ReviewStatsDto
            {
                ProductId = productId,
                TotalReviews = ratings.Count,
                AverageRating = ratings.Any() ? Math.Round(ratings.Average(), 1) : 0,
                WithImagesCount = approvedReviews.Count(r => !string.IsNullOrEmpty(r.ImagesJson) && r.ImagesJson != "[]" && r.ImagesJson != "null"),
                VerifiedCount = approvedReviews.Count(r => r.IsVerifiedPurchase),
                Rating5Count = ratings.Count(r => r == 5),
                Rating4Count = ratings.Count(r => r == 4),
                Rating3Count = ratings.Count(r => r == 3),
                Rating2Count = ratings.Count(r => r == 2),
                Rating1Count = ratings.Count(r => r == 1)
            };

            if (stats.TotalReviews > 0)
            {
                stats.Rating5Percentage = Math.Round((double)stats.Rating5Count / stats.TotalReviews * 100, 1);
                stats.Rating4Percentage = Math.Round((double)stats.Rating4Count / stats.TotalReviews * 100, 1);
                stats.Rating3Percentage = Math.Round((double)stats.Rating3Count / stats.TotalReviews * 100, 1);
                stats.Rating2Percentage = Math.Round((double)stats.Rating2Count / stats.TotalReviews * 100, 1);
                stats.Rating1Percentage = Math.Round((double)stats.Rating1Count / stats.TotalReviews * 100, 1);
            }

            return stats;
        }

        public async Task<int> VoteHelpfulAsync(int reviewId, CancellationToken ct = default)
        {
            var result = await VoteHelpfulWithCheckAsync(reviewId, null, null, ct);
            return result.NewCount;
        }

        public async Task<(bool Success, int NewCount, string Message)> VoteHelpfulWithCheckAsync(int reviewId, int? userId, string? anonymousKey, CancellationToken ct = default)
        {
            var review = await _context.ProductReviews.FindAsync(new object[] { reviewId }, ct);
            if (review == null)
            {
                return (false, 0, "Không tìm thấy đánh giá.");
            }

            // Kiểm tra trùng lặp: nếu user đã đăng nhập, kiểm tra theo userId; nếu vãng lai, kiểm tra theo anonymousKey
            bool alreadyVoted = false;
            if (userId.HasValue)
            {
                alreadyVoted = await _context.ReviewHelpfulVotes
                    .AnyAsync(v => v.ReviewId == reviewId && v.UserId == userId.Value, ct);
            }
            else if (!string.IsNullOrWhiteSpace(anonymousKey))
            {
                alreadyVoted = await _context.ReviewHelpfulVotes
                    .AnyAsync(v => v.ReviewId == reviewId && v.AnonymousKey == anonymousKey, ct);
            }

            if (alreadyVoted)
            {
                return (false, review.HelpfulCount, "Bạn đã bình chọn đánh giá này hữu ích rồi.");
            }

            var vote = new ReviewHelpfulVote
            {
                ReviewId = reviewId,
                UserId = userId,
                AnonymousKey = string.IsNullOrWhiteSpace(anonymousKey) ? null : anonymousKey.Trim(),
                VotedAt = DateTime.UtcNow
            };

            _context.ReviewHelpfulVotes.Add(vote);
            review.HelpfulCount += 1;
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Người dùng {UserId} (Key: {AnonKey}) đã vote helpful thành công cho review {ReviewId}. Tổng vote: {Count}",
                userId, anonymousKey, reviewId, review.HelpfulCount);

            return (true, review.HelpfulCount, "Cảm ơn bạn đã ghi nhận đánh giá này hữu ích!");
        }

        public async Task<List<ReviewPhotoGalleryDto>> GetCustomerReviewImagesAsync(int productId, int limit = 12, CancellationToken ct = default)
        {
            limit = Math.Clamp(limit, 1, 30);

            var reviewsWithPhotos = await _context.ProductReviews
                .AsNoTracking()
                .Include(r => r.User)
                .Where(r => r.ProductId == productId && r.IsApproved && !r.IsHidden && !string.IsNullOrEmpty(r.ImagesJson) && r.ImagesJson != "[]" && r.ImagesJson != "null")
                .OrderByDescending(r => r.CreatedAt)
                .Take(limit * 2)
                .ToListAsync(ct);

            var gallery = new List<ReviewPhotoGalleryDto>();
            foreach (var r in reviewsWithPhotos)
            {
                try
                {
                    var images = !string.IsNullOrEmpty(r.ImagesJson) ? JsonSerializer.Deserialize<List<string>>(r.ImagesJson) ?? new() : new();
                    foreach (var img in images)
                    {
                        if (string.IsNullOrWhiteSpace(img)) continue;
                        gallery.Add(new ReviewPhotoGalleryDto
                        {
                            ReviewId = r.Id,
                            ImageUrl = img,
                            Rating = r.Rating,
                            UserName = r.User?.FullName ?? r.User?.Username ?? "Khách hàng",
                            SkinType = r.SkinType,
                            CreatedAt = r.CreatedAt
                        });

                        if (gallery.Count >= limit) break;
                    }
                }
                catch
                {
                    // bỏ qua JSON không hợp lệ
                }

                if (gallery.Count >= limit) break;
            }

            return gallery;
        }

        public async Task<bool> AddAdminReplyAsync(int reviewId, string replyContent, string replyBy = "Chuyên viên CSKH Omni Beauty", CancellationToken ct = default)
        {
            var review = await _context.ProductReviews.FindAsync(new object[] { reviewId }, ct);
            if (review == null) return false;

            review.AdminReply = replyContent.Trim();
            review.AdminReplyBy = string.IsNullOrWhiteSpace(replyBy) ? "Chuyên viên CSKH Omni Beauty" : replyBy.Trim();
            review.AdminRepliedAt = DateTime.UtcNow;
            review.UpdatedAt = DateTime.UtcNow;

            return await _context.SaveChangesAsync(ct) > 0;
        }

        public async Task<List<ProductReviewDto>> GetPendingReviewsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var reviews = await _context.ProductReviews
                .AsNoTracking()
                .Include(r => r.Product)
                .Include(r => r.User)
                .Where(r => !r.IsApproved && !r.IsHidden)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return reviews.Select(MapToDto).ToList();
        }

        public async Task<List<ProductReviewDto>> GetAllReviewsAsync(ReviewFilterDto filter, CancellationToken ct = default)
        {
            var query = _context.ProductReviews
                .AsNoTracking()
                .Include(r => r.Product)
                .Include(r => r.User)
                .AsQueryable();

            if (filter.ProductId.HasValue)
            {
                query = query.Where(r => r.ProductId == filter.ProductId.Value);
            }

            if (filter.Rating.HasValue)
            {
                query = query.Where(r => r.Rating == filter.Rating.Value);
            }

            if (filter.IsApproved.HasValue)
            {
                query = query.Where(r => r.IsApproved == filter.IsApproved.Value);
            }

            if (filter.IsHidden.HasValue)
            {
                query = query.Where(r => r.IsHidden == filter.IsHidden.Value);
            }

            if (filter.IsVerifiedPurchase.HasValue)
            {
                query = query.Where(r => r.IsVerifiedPurchase == filter.IsVerifiedPurchase.Value);
            }

            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, 100);

            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return reviews.Select(MapToDto).ToList();
        }

        public async Task<bool> DeleteReviewAsync(int reviewId, CancellationToken ct = default)
        {
            var review = await _context.ProductReviews.FindAsync(new object[] { reviewId }, ct);
            if (review == null) return false;

            _context.ProductReviews.Remove(review);
            var result = await _context.SaveChangesAsync(ct) > 0;

            _logger.LogInformation("Đã xóa vĩnh viễn đánh giá #{ReviewId} của sản phẩm #{ProductId}", reviewId, review.ProductId);
            return result;
        }

        private static ProductReviewDto MapToDto(ProductReview r)
        {
            List<string> images = new();
            if (!string.IsNullOrWhiteSpace(r.ImagesJson))
            {
                try
                {
                    images = JsonSerializer.Deserialize<List<string>>(r.ImagesJson) ?? new();
                }
                catch
                {
                    images = new();
                }
            }

            return new ProductReviewDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                ProductName = r.Product?.Name ?? "Sản phẩm",
                UserId = r.UserId,
                UserName = r.User?.FullName ?? r.User?.Username ?? "Khách hàng",
                OrderId = r.OrderId,
                Rating = r.Rating,
                Title = r.Title,
                Content = r.Content,
                Images = images,
                IsVerifiedPurchase = r.IsVerifiedPurchase,
                IsApproved = r.IsApproved,
                IsHidden = r.IsHidden,
                AdminNote = r.AdminNote,
                SkinType = r.SkinType,
                AgeRange = r.AgeRange,
                Scent = r.Scent,
                Longevity = r.Longevity,
                SkinFeel = r.SkinFeel,
                HelpfulCount = r.HelpfulCount,
                AdminReply = r.AdminReply,
                AdminRepliedAt = r.AdminRepliedAt,
                AdminReplyBy = r.AdminReplyBy,
                CreatedAt = r.CreatedAt
            };
        }
    }
}
