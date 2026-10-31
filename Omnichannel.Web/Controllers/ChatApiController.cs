using System;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Chat;
using Omnichannel.Application.Interfaces.Chat;

namespace Omnichannel.Web.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatApiController : ControllerBase
    {
        private readonly IChatbotService _chatbotService;
        private readonly ILogger<ChatApiController> _logger;

        // In-memory rate limiting: IP/Key -> (Count, WindowStartTime)
        private static readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)> _rateLimits = new();
        private const int MaxRequestsPerMinute = 25;

        public ChatApiController(
            IChatbotService chatbotService,
            ILogger<ChatApiController> logger)
        {
            _chatbotService = chatbotService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { success = false, message = "Nội dung tin nhắn không được để trống." });
            }

            // 1. Rate Limiting Check
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var rateKey = $"{clientIp}_{request.SessionKey}";
            var now = DateTime.UtcNow;

            var limitInfo = _rateLimits.AddOrUpdate(rateKey,
                addValueFactory: _ => (1, now),
                updateValueFactory: (_, current) =>
                {
                    if (now - current.WindowStart > TimeSpan.FromMinutes(1))
                    {
                        return (1, now);
                    }
                    return (current.Count + 1, current.WindowStart);
                });

            if (limitInfo.Count > MaxRequestsPerMinute)
            {
                return StatusCode(429, new
                {
                    success = false,
                    message = "Bạn đang gửi tin nhắn quá nhanh. Vui lòng chờ vài giây trước khi gửi tiếp nhé!"
                });
            }

            // 2. Identify authenticated user if any
            int? userId = null;
            string? userName = null;

            if (User?.Identity?.IsAuthenticated == true)
            {
                var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(idClaim, out var parsedId))
                {
                    userId = parsedId;
                }
                userName = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name);
            }

            try
            {
                var response = await _chatbotService.ProcessMessageAsync(request, userId, userName);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình xử lý tin nhắn Chatbot AI.");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Xin lỗi bạn, trợ lý AI tạm thời gặp sự cố kết nối. Vui lòng thử lại sau giây lát!"
                });
            }
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { status = "active", time = DateTime.UtcNow });
        }
    }
}
