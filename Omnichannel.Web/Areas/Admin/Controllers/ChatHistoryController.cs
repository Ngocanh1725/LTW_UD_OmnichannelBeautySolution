using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omnichannel.Application.DTOs.Chat;
using Omnichannel.Application.Interfaces.Chat;
using Omnichannel.Infrastructure.Security;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [HasPermission("CUSTOMER_VIEW")]
    public class ChatHistoryController : Controller
    {
        private readonly IChatbotService _chatbotService;

        public ChatHistoryController(IChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        [HttpGet]
        [HasPermission("CUSTOMER_VIEW")]
        public async Task<IActionResult> Index([FromQuery] ChatHistoryFilterDto filter)
        {
            filter ??= new ChatHistoryFilterDto();
            if (filter.Page <= 0) filter.Page = 1;
            if (filter.PageSize <= 0) filter.PageSize = 15;

            var viewModel = await _chatbotService.GetChatHistoryAsync(filter);
            return View(viewModel);
        }

        [HttpGet]
        [HasPermission("CUSTOMER_VIEW")]
        public async Task<IActionResult> Detail(int id)
        {
            var detail = await _chatbotService.GetSessionDetailAsync(id);
            if (detail == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy phiên hội thoại này." });
            }

            return Json(new { success = true, data = detail });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("CUSTOMER_EDIT")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _chatbotService.DeleteSessionAsync(id);
            if (!deleted)
            {
                TempData["ErrorMessage"] = "Không thể xóa phiên hội thoại hoặc phiên không tồn tại.";
            }
            else
            {
                TempData["SuccessMessage"] = "Đã xóa lịch sử phiên hội thoại thành công.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
