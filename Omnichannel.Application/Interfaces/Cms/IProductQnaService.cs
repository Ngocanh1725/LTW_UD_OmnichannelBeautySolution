using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Cms;

namespace Omnichannel.Application.Interfaces.Cms
{
    public interface IProductQnaService
    {
        /// <summary>
        /// Lấy danh sách câu hỏi và câu trả lời đã được duyệt của sản phẩm cho Storefront
        /// </summary>
        Task<List<ProductQuestionDto>> GetApprovedQuestionsAsync(int productId, int page = 1, int pageSize = 10, CancellationToken ct = default);

        /// <summary>
        /// Khách hàng gửi câu hỏi mới về sản phẩm
        /// </summary>
        Task<ProductQuestionDto> SubmitQuestionAsync(int productId, string content, int? userId, string? authorName = null, CancellationToken ct = default);

        /// <summary>
        /// Gửi câu trả lời cho câu hỏi (Chuyên viên CSKH Canmake Beauty hoặc cộng đồng)
        /// </summary>
        Task<ProductAnswerDto> SubmitAnswerAsync(int questionId, string content, int? userId, string? authorName = null, bool isOfficial = false, CancellationToken ct = default);

        /// <summary>
        /// Phê duyệt hoặc từ chối câu hỏi từ Admin CMS
        /// </summary>
        Task<bool> ApproveQuestionAsync(int questionId, bool isApproved, CancellationToken ct = default);

        /// <summary>
        /// Ẩn hoặc mở lại câu hỏi vi phạm
        /// </summary>
        Task<bool> ToggleHideQuestionAsync(int questionId, bool isHidden, CancellationToken ct = default);

        /// <summary>
        /// Xóa câu hỏi khỏi hệ thống
        /// </summary>
        Task<bool> DeleteQuestionAsync(int questionId, CancellationToken ct = default);

        /// <summary>
        /// Lấy toàn bộ câu hỏi cho Admin kiểm duyệt
        /// </summary>
        Task<List<ProductQuestionDto>> GetAllQuestionsForAdminAsync(int? productId = null, bool? isApproved = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    }
}

