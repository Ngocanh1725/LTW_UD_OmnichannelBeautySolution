using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Cms;

namespace Omnichannel.Application.Interfaces.Cms
{
    public interface IProductReviewService
    {
        /// <summary>
        /// Gửi đánh giá sản phẩm mới kèm xác thực đơn hàng mua thực tế (Verified Purchase)
        /// </summary>
        Task<bool> SubmitReviewAsync(CreateReviewDto dto, int? userId = null, CancellationToken ct = default);

        /// <summary>
        /// Phê duyệt hoặc từ chối đánh giá sản phẩm từ quản trị CMS
        /// </summary>
        Task<bool> ApproveReviewAsync(int reviewId, bool isApproved, string? adminNote = null, CancellationToken ct = default);

        /// <summary>
        /// Ẩn hoặc hiện đánh giá sản phẩm nếu phát hiện vi phạm tiêu chuẩn cộng đồng
        /// </summary>
        Task<bool> ToggleHideReviewAsync(int reviewId, bool isHidden, string? adminNote = null, CancellationToken ct = default);

        /// <summary>
        /// Lấy danh sách đánh giá đã được duyệt của sản phẩm cho khách hàng xem trên storefront
        /// </summary>
        Task<List<ProductReviewDto>> GetProductReviewsAsync(int productId, int page = 1, int pageSize = 10, int? ratingFilter = null, string? filterTab = null, CancellationToken ct = default);

        /// <summary>
        /// Thống kê đánh giá của sản phẩm: điểm trung bình và phân bố tỷ lệ từng số sao
        /// </summary>
        Task<ReviewStatsDto> GetReviewStatsAsync(int productId, CancellationToken ct = default);

        /// <summary>
        /// Bình chọn hữu ích cho đánh giá (Helpful thumbs-up vote)
        /// </summary>
        Task<int> VoteHelpfulAsync(int reviewId, CancellationToken ct = default);

        /// <summary>
        /// Thêm phản hồi chính thức từ Chuyên viên CSKH Omni Beauty
        /// </summary>
        Task<bool> AddAdminReplyAsync(int reviewId, string replyContent, string replyBy = "Chuyên viên CSKH Omni Beauty", CancellationToken ct = default);

        /// <summary>
        /// Lấy danh sách các đánh giá đang chờ duyệt (Moderation Queue) cho Admin
        /// </summary>
        Task<List<ProductReviewDto>> GetPendingReviewsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);

        /// <summary>
        /// Lấy danh sách tất cả đánh giá theo bộ lọc chi tiết cho Admin
        /// </summary>
        Task<List<ProductReviewDto>> GetAllReviewsAsync(ReviewFilterDto filter, CancellationToken ct = default);

        /// <summary>
        /// Xóa vĩnh viễn đánh giá sản phẩm khỏi hệ thống
        /// </summary>
        Task<bool> DeleteReviewAsync(int reviewId, CancellationToken ct = default);
    }
}
