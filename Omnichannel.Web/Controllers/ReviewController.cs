using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Cms;
using Omnichannel.Application.Interfaces.Cms;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Web.Controllers
{
    public class ReviewController : Controller
    {
        private readonly IProductReviewService _reviewService;
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ReviewController> _logger;

        public ReviewController(
            IProductReviewService reviewService,
            IWebHostEnvironment env,
            ApplicationDbContext dbContext,
            ILogger<ReviewController> logger)
        {
            _reviewService = reviewService ?? throw new ArgumentNullException(nameof(reviewService));
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(claim) && int.TryParse(claim, out int uid))
            {
                return uid;
            }
            return null;
        }

        /// <summary>
        /// API Lấy danh sách đánh giá sản phẩm có phân trang và bộ lọc nhanh:
        /// all | images | verified | 5star
        /// </summary>
        [HttpGet]
        [Route("Review/GetReviews")]
        public async Task<IActionResult> GetReviews(
            [FromQuery] int productId,
            [FromQuery] string? filter = "all",
            [FromQuery] int? rating = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (productId <= 0)
            {
                return BadRequest(new { success = false, message = "ProductId không hợp lệ" });
            }

            try
            {
                var reviews = await _reviewService.GetProductReviewsAsync(productId, page, pageSize, rating, filter);
                var stats = await _reviewService.GetReviewStatsAsync(productId);

                return Json(new
                {
                    success = true,
                    productId,
                    filter = filter ?? "all",
                    page,
                    pageSize,
                    total = reviews.Count,
                    stats,
                    reviews
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách đánh giá sản phẩm {ProductId}", productId);
                return StatusCode(500, new { success = false, message = "Không thể tải danh sách đánh giá" });
            }
        }

        /// <summary>
        /// API Lấy thống kê số sao và tỷ lệ phần trăm
        /// </summary>
        [HttpGet]
        [Route("Review/Stats")]
        public async Task<IActionResult> GetStats([FromQuery] int productId)
        {
            if (productId <= 0)
            {
                return BadRequest(new { success = false, message = "ProductId không hợp lệ" });
            }

            var stats = await _reviewService.GetReviewStatsAsync(productId);
            return Json(new { success = true, stats });
        }

        /// <summary>
        /// API Gửi đánh giá sản phẩm mới kèm tải lên tối đa 3 ảnh chụp thực tế
        /// Hỗ trợ cả 2 route: /Review/Submit và /Product/SubmitReview
        /// </summary>
        [HttpPost]
        [Route("Review/Submit")]
        [Route("Product/SubmitReview")]
        public async Task<IActionResult> SubmitReview([FromForm] SubmitReviewRequest request)
        {
            if (request == null || request.ProductId <= 0)
            {
                return BadRequest(new { success = false, message = "Thông tin đánh giá không hợp lệ." });
            }

            if (request.Rating < 1 || request.Rating > 5)
            {
                return BadRequest(new { success = false, message = "Vui lòng chọn số sao từ 1 đến 5 sao." });
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new { success = false, message = "Vui lòng nhập nội dung chia sẻ trải nghiệm thực tế." });
            }

            int? userId = GetCurrentUserId();

            // Xử lý lưu ảnh chụp thực tế nếu có
            var uploadedImageUrls = new List<string>();

            // Nếu client đã gửi URL ảnh sẵn
            if (request.ExistingImages != null && request.ExistingImages.Any())
            {
                uploadedImageUrls.AddRange(request.ExistingImages.Take(3));
            }

            // Xử lý tệp tin tải lên (tối đa 3 ảnh)
            if (request.Photos != null && request.Photos.Count > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "reviews");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                int remainingSlots = Math.Max(0, 3 - uploadedImageUrls.Count);
                foreach (var file in request.Photos.Take(remainingSlots))
                {
                    if (file == null || file.Length == 0) continue;

                    // Giới hạn dung lượng 5MB
                    if (file.Length > 5 * 1024 * 1024) continue;

                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(ext)) continue;

                    var uniqueFileName = $"review_{request.ProductId}_{DateTime.UtcNow.Ticks}_{Guid.NewGuid():N}{ext}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    uploadedImageUrls.Add($"/uploads/reviews/{uniqueFileName}");
                }
            }

            var createDto = new CreateReviewDto
            {
                ProductId = request.ProductId,
                Rating = request.Rating,
                Title = string.IsNullOrWhiteSpace(request.Title) ? $"Đánh giá {request.Rating} sao" : request.Title.Trim(),
                Content = request.Content.Trim(),
                Images = uploadedImageUrls,
                SkinType = request.SkinType?.Trim(),
                AgeRange = request.AgeRange?.Trim(),
                Scent = request.Scent?.Trim(),
                Longevity = request.Longevity?.Trim(),
                SkinFeel = request.SkinFeel?.Trim()
            };

            try
            {
                bool success = await _reviewService.SubmitReviewAsync(createDto, userId);
                if (success)
                {
                    var updatedStats = await _reviewService.GetReviewStatsAsync(request.ProductId);
                    return Json(new
                    {
                        success = true,
                        message = "Cảm ơn bạn đã chia sẻ trải nghiệm thực tế! Đánh giá đã được ghi nhận và hiển thị ngay trên hệ thống.",
                        stats = updatedStats
                    });
                }

                return StatusCode(500, new { success = false, message = "Không thể lưu đánh giá, vui lòng thử lại sau." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi đánh giá cho sản phẩm {ProductId}", request.ProductId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// API Bình chọn hữu ích (Helpful thumbs-up vote)
        /// </summary>
        [HttpPost]
        [Route("Review/VoteHelpful")]
        public async Task<IActionResult> VoteHelpful([FromBody] VoteHelpfulRequest request)
        {
            if (request == null || request.ReviewId <= 0)
            {
                return BadRequest(new { success = false, message = "ReviewId không hợp lệ" });
            }

            try
            {
                int newHelpfulCount = await _reviewService.VoteHelpfulAsync(request.ReviewId);
                return Json(new
                {
                    success = true,
                    reviewId = request.ReviewId,
                    helpfulCount = newHelpfulCount,
                    message = "Cảm ơn bạn đã ghi nhận đánh giá này hữu ích!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi vote helpful cho đánh giá {ReviewId}", request.ReviewId);
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi bình chọn" });
            }
        }
    }

    public class SubmitReviewRequest
    {
        public int ProductId { get; set; }
        public int Rating { get; set; } = 5;
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? SkinType { get; set; }
        public string? AgeRange { get; set; }

        // --- 3 Tiêu chí phụ đặc trưng mỹ phẩm ---
        public string? Scent { get; set; }
        public string? Longevity { get; set; }
        public string? SkinFeel { get; set; }

        // --- Ảnh upload ---
        public List<IFormFile>? Photos { get; set; }
        public List<string>? ExistingImages { get; set; }
    }

    public class VoteHelpfulRequest
    {
        public int ReviewId { get; set; }
    }
}
