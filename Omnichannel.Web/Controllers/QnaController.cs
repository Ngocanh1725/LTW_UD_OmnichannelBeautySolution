using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Cms;
using Omnichannel.Application.Interfaces.Cms;
using Omnichannel.Web.Hubs;

namespace Omnichannel.Web.Controllers
{
    public class QnaController : Controller
    {
        private readonly IProductQnaService _qnaService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<QnaController> _logger;

        public QnaController(
            IProductQnaService qnaService,
            IHubContext<NotificationHub> hubContext,
            ILogger<QnaController> logger)
        {
            _qnaService = qnaService ?? throw new ArgumentNullException(nameof(qnaService));
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
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
        /// API Lấy danh sách câu hỏi & câu trả lời đã duyệt của sản phẩm cho Storefront
        /// </summary>
        [HttpGet]
        [Route("api/qna/product/{productId:int}")]
        [Route("Qna/GetQuestions")]
        public async Task<IActionResult> GetQuestions(int productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (productId <= 0)
            {
                return BadRequest(new { success = false, message = "ProductId không hợp lệ" });
            }

            try
            {
                var questions = await _qnaService.GetApprovedQuestionsAsync(productId, page, pageSize);
                return Json(new
                {
                    success = true,
                    productId,
                    page,
                    pageSize,
                    total = questions.Count,
                    questions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách Q&A cho sản phẩm {ProductId}", productId);
                return StatusCode(500, new { success = false, message = "Không thể tải danh sách hỏi đáp" });
            }
        }

        /// <summary>
        /// API Khách hàng đặt câu hỏi về sản phẩm (Bắt buộc đăng nhập tài khoản)
        /// </summary>
        [HttpPost]
        [Route("api/qna/ask")]
        [Route("Qna/Ask")]
        public async Task<IActionResult> Ask([FromBody] SubmitQuestionRequest request)
        {
            if (request == null || request.ProductId <= 0)
            {
                return BadRequest(new { success = false, message = "Dữ liệu câu hỏi không hợp lệ." });
            }

            // Kiểm tra xác thực người dùng: User chưa đăng nhập không được đặt câu hỏi
            int? userId = GetCurrentUserId();
            if (!userId.HasValue || User?.Identity?.IsAuthenticated != true)
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new
                {
                    success = false,
                    message = "Vui lòng đăng nhập để đặt câu hỏi cho Chuyên viên Dược Mỹ Phẩm."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Content) || request.Content.Trim().Length < 5)
            {
                return BadRequest(new { success = false, message = "Nội dung câu hỏi phải có tối thiểu 5 ký tự." });
            }

            try
            {
                string safeContent = HtmlEncoder.Default.Encode(request.Content.Trim());
                string? authorName = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.GivenName);

                var question = await _qnaService.SubmitQuestionAsync(request.ProductId, safeContent, userId, authorName);

                // Gửi thông báo Realtime SignalR tới Admin Dashboard
                try
                {
                    string snippet = safeContent.Length > 60 ? safeContent.Substring(0, 57) + "..." : safeContent;
                    await _hubContext.Clients.Group("Managers").SendAsync("ReceiveNotification", new
                    {
                        title = "Câu hỏi mới từ khách hàng",
                        message = $"{question.AuthorName} vừa hỏi: \"{snippet}\"",
                        severity = "INFO",
                        type = "QNA",
                        url = $"/Admin/Review?tab=qna"
                    });
                }
                catch (Exception signalrEx)
                {
                    _logger.LogWarning(signalrEx, "Không thể gửi SignalR notification cho câu hỏi #{QuestionId}", question.Id);
                }

                return Json(new
                {
                    success = true,
                    message = "Câu hỏi của bạn đã được gửi thành công! Chuyên viên Dược Mỹ Phẩm Canmake Beauty sẽ giải đáp trong thời gian sớm nhất.",
                    question
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi câu hỏi cho sản phẩm {ProductId}", request.ProductId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// API Gửi câu trả lời cho câu hỏi
        /// </summary>
        [HttpPost]
        [Route("api/qna/answer")]
        [Route("Qna/Answer")]
        public async Task<IActionResult> Answer([FromBody] SubmitAnswerRequest request)
        {
            if (request == null || request.QuestionId <= 0)
            {
                return BadRequest(new { success = false, message = "Dữ liệu trả lời không hợp lệ." });
            }

            if (string.IsNullOrWhiteSpace(request.Content) || request.Content.Trim().Length < 2)
            {
                return BadRequest(new { success = false, message = "Nội dung câu trả lời không được để trống." });
            }

            int? userId = GetCurrentUserId();
            bool isStaff = User.IsInRole("Admin") || User.IsInRole("SuperAdmin") || User.IsInRole("SUPER_ADMIN") || User.IsInRole("StoreManager");
            bool isOfficial = isStaff || request.IsOfficial;

            try
            {
                string safeContent = HtmlEncoder.Default.Encode(request.Content.Trim());
                string authorName = isOfficial
                    ? "Chuyên viên Dược Mỹ Phẩm Canmake Beauty"
                    : (User.Identity?.Name ?? "Thành viên Canmake Beauty");

                var answer = await _qnaService.SubmitAnswerAsync(request.QuestionId, safeContent, userId, authorName, isOfficial);

                return Json(new
                {
                    success = true,
                    message = "Cảm ơn bạn đã phản hồi câu hỏi!",
                    answer
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi câu trả lời cho câu hỏi #{QuestionId}", request.QuestionId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}

