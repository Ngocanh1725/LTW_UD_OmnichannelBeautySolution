using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omnichannel.Application.DTOs.Chat;
using Omnichannel.Application.Interfaces.Chat;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class ChatHistoryController : Controller
    {
        private readonly IChatbotService _chatbotService;

        public ChatHistoryController(IChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] ChatHistoryFilterDto filter)
        {
            // Kiểm tra phân quyền: SuperAdmin hoặc nhân viên có quyền quản lý khách hàng / cài đặt / CMS
            if (!User.IsInRole("SuperAdmin") && !User.IsInRole("SUPER_ADMIN") && 
                !User.HasClaim("Permission", "CUSTOMER_VIEW") && 
                !User.HasClaim("Permission", "SETTINGS_VIEW"))
            {
                return Forbid();
            }

            filter ??= new ChatHistoryFilterDto();
            if (filter.Page <= 0) filter.Page = 1;
            if (filter.PageSize <= 0) filter.PageSize = 15;

            var viewModel = await _chatbotService.GetChatHistoryAsync(filter);
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            if (!User.IsInRole("SuperAdmin") && !User.IsInRole("SUPER_ADMIN") && 
                !User.HasClaim("Permission", "CUSTOMER_VIEW") && 
                !User.HasClaim("Permission", "SETTINGS_VIEW"))
            {
                return Forbid();
            }

            var detail = await _chatbotService.GetSessionDetailAsync(id);
            if (detail == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy phiên hội thoại này." });
            }

            return Json(new { success = true, data = detail });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!User.IsInRole("SuperAdmin") && !User.IsInRole("SUPER_ADMIN") && 
                !User.HasClaim("Permission", "SETTINGS_MANAGE"))
            {
                return Forbid();
            }

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
