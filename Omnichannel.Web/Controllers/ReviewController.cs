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

        private string GetOrCreateAnonymousVoteKey()
        {
            const string cookieName = "omni_anon_vote_key";
            if (Request.Cookies.TryGetValue(cookieName, out var existingKey) && !string.IsNullOrWhiteSpace(existingKey))
            {
                return existingKey;
            }

            var newKey = $"anon_{Guid.NewGuid():N}";
            Response.Cookies.Append(cookieName, newKey, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(90)
            });
            return newKey;
        }

        /// <summary>
        /// Kiểm tra định dạng tệp ảnh an toàn bằng cả Extension, MIME Type và Magic Bytes (File Signature)
        /// Chặn đứng tệp thực thi .exe đổi tên thành .jpg
        /// </summary>
        public static bool IsValidImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0 || file.Length > 2 * 1024 * 1024) // Tối đa 2MB/ảnh
            {
                return false;
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
            {
                return false;
            }

            var allowedMimeTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
            var mime = file.ContentType?.ToLowerInvariant();
            if (string.IsNullOrEmpty(mime) || !allowedMimeTypes.Contains(mime))
            {
                return false;
            }

            try
            {
                using var stream = file.OpenReadStream();
                if (stream.Length < 4) return false;

                var header = new byte[12];
                int bytesRead = stream.Read(header, 0, Math.Min(12, (int)stream.Length));
                if (bytesRead < 4) return false;

                // Chặn tệp thực thi Windows PE / MZ (.exe, .dll)
                if (header[0] == 0x4D && header[1] == 0x5A) // "MZ"
                {
                    return false;
                }

                // Kiểm tra JPEG (FF D8 FF)
                if (ext == ".jpg" || ext == ".jpeg")
                {
                    return header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
                }

                // Kiểm tra PNG (89 50 4E 47 0D 0A 1A 0A)
                if (ext == ".png")
                {
                    return bytesRead >= 8 &&
                           header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
                           header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A;
                }

                // Kiểm tra WEBP ("RIFF" .... "WEBP")
                if (ext == ".webp")
                {
                    return bytesRead >= 12 &&
                           header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
                           header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// API Lấy danh sách đánh giá sản phẩm có phân trang, bộ lọc Skin Twin và sắp xếp
        /// </summary>
        [HttpGet]
        [Route("Review/GetReviews")]
        public async Task<IActionResult> GetReviews(
            [FromQuery] int productId,
            [FromQuery] string? filter = "all",
            [FromQuery] int? rating = null,
            [FromQuery] string? skinType = null,
            [FromQuery] string? sortBy = "newest",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (productId <= 0)
            {
                return BadRequest(new { success = false, message = "ProductId không hợp lệ" });
            }

            try
            {
                var reviews = await _reviewService.GetProductReviewsAsync(productId, page, pageSize, rating, filter, skinType, sortBy);
                var stats = await _reviewService.GetReviewStatsAsync(productId);

                return Json(new
                {
                    success = true,
                    productId,
                    filter = filter ?? "all",
                    skinType = skinType ?? "all",
                    sortBy = sortBy ?? "newest",
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
        /// API Lấy bộ sưu tập ảnh thực tế từ khách hàng (Customer Swatch Photo Gallery)
        /// </summary>
        [HttpGet]
        [Route("Review/GetGallery")]
        public async Task<IActionResult> GetGallery([FromQuery] int productId, [FromQuery] int limit = 12)
        {
            if (productId <= 0)
            {
                return BadRequest(new { success = false, message = "ProductId không hợp lệ" });
            }

            var photos = await _reviewService.GetCustomerReviewImagesAsync(productId, limit);
            return Json(new { success = true, productId, total = photos.Count, photos });
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
        /// API Gửi đánh giá sản phẩm mới kèm tải lên tối đa 3 ảnh chụp thực tế (Giới hạn 2MB, kiểm tra Magic Bytes)
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
            var uploadedImageUrls = new List<string>();

            if (request.ExistingImages != null && request.ExistingImages.Any())
            {
                uploadedImageUrls.AddRange(request.ExistingImages.Take(3));
            }

            if (request.Photos != null && request.Photos.Count > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "reviews");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                int remainingSlots = Math.Max(0, 3 - uploadedImageUrls.Count);
                foreach (var file in request.Photos.Take(remainingSlots))
                {
                    if (file == null || file.Length == 0) continue;

                    if (!IsValidImageFile(file))
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = $"Tệp '{file.FileName}' không hợp lệ! Chỉ chấp nhận ảnh thật (.jpg, .png, .webp) tối đa 2MB."
                        });
                    }

                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
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
        /// API Bình chọn hữu ích (Helpful thumbs-up vote) có chống trùng lặp
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
                int? userId = GetCurrentUserId();
                string? anonKey = userId.HasValue ? null : GetOrCreateAnonymousVoteKey();

                var result = await _reviewService.VoteHelpfulWithCheckAsync(request.ReviewId, userId, anonKey);
                return Json(new
                {
                    success = result.Success,
                    alreadyVoted = !result.Success,
                    reviewId = request.ReviewId,
                    helpfulCount = result.NewCount,
                    message = result.Message
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
        public string? Scent { get; set; }
        public string? Longevity { get; set; }
        public string? SkinFeel { get; set; }
        public List<IFormFile>? Photos { get; set; }
        public List<string>? ExistingImages { get; set; }
    }

    public class VoteHelpfulRequest
    {
        public int ReviewId { get; set; }
    }
}
