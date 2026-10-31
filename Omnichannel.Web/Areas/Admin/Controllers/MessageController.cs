using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omnichannel.Application.DTOs.Messages;
using Omnichannel.Application.Interfaces.Messages;
using Omnichannel.Web.Extensions;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class MessageController : Controller
    {
        private readonly IInternalMessageService _messageService;

        public MessageController(IInternalMessageService messageService)
        {
            _messageService = messageService;
        }

        [HttpGet]
        [Route("Admin/Messages")]
        [Route("Admin/Messages/Inbox")]
        [Route("Admin/Message")]
        public async Task<IActionResult> Inbox([FromQuery] string? search = null, [FromQuery] int page = 1)
        {
            int? currentUserId = User.GetUserId();
            if (!currentUserId.HasValue) return Challenge();

            var model = await _messageService.GetInboxMessagesAsync(currentUserId.Value, search, page);
            ViewBag.Folder = "inbox";
            ViewBag.Search = search;
            ViewBag.Recipients = await _messageService.GetReceivableUsersAsync(currentUserId.Value);

            return View("Index", model);
        }

        [HttpGet]
        [Route("Admin/Messages/Sent")]
        public async Task<IActionResult> Sent([FromQuery] string? search = null, [FromQuery] int page = 1)
        {
            int? currentUserId = User.GetUserId();
            if (!currentUserId.HasValue) return Challenge();

            var model = await _messageService.GetSentMessagesAsync(currentUserId.Value, search, page);
            ViewBag.Folder = "sent";
            ViewBag.Search = search;
            ViewBag.Recipients = await _messageService.GetReceivableUsersAsync(currentUserId.Value);

            return View("Index", model);
        }

        [HttpGet]
        [Route("Admin/Messages/View/{id:int}")]
        public async Task<IActionResult> ViewMessage(int id, [FromQuery] string folder = "inbox")
        {
            int? currentUserId = User.GetUserId();
            if (!currentUserId.HasValue) return Challenge();

            var message = await _messageService.GetMessageDetailsAsync(id, currentUserId.Value);
            if (message == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thư hoặc bạn không có quyền xem thư này.";
                return RedirectToAction(folder == "sent" ? nameof(Sent) : nameof(Inbox));
            }

            ViewBag.Folder = folder;
            ViewBag.UnreadCount = await _messageService.GetUnreadMessageCountAsync(currentUserId.Value);
            ViewBag.Recipients = await _messageService.GetReceivableUsersAsync(currentUserId.Value);

            return View("View", message);
        }

        [HttpGet]
        [Route("Admin/Messages/Compose")]
        public async Task<IActionResult> Compose([FromQuery] int? replyToUserId = null, [FromQuery] string? subject = null)
        {
            int? currentUserId = User.GetUserId();
            if (!currentUserId.HasValue) return Challenge();

            ViewBag.Recipients = await _messageService.GetReceivableUsersAsync(currentUserId.Value);
            ViewBag.UnreadCount = await _messageService.GetUnreadMessageCountAsync(currentUserId.Value);
            ViewBag.Folder = "compose";

            var dto = new ComposeMessageDto
            {
                ReceiverId = replyToUserId ?? 0,
                Subject = subject != null ? (subject.StartsWith("Re:", StringComparison.OrdinalIgnoreCase) ? subject : $"Re: {subject}") : string.Empty
            };

            return View("Compose", dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/Messages/Compose")]
        public async Task<IActionResult> Compose([FromForm] ComposeMessageDto dto)
        {
            int? currentUserId = User.GetUserId();
            if (!currentUserId.HasValue) return Challenge();

            if (dto.ReceiverId <= 0)
            {
                ModelState.AddModelError("ReceiverId", "Vui lòng chọn người nhận thư.");
            }
            if (string.IsNullOrWhiteSpace(dto.Subject))
            {
                ModelState.AddModelError("Subject", "Tiêu đề thư không được để trống.");
            }
            if (string.IsNullOrWhiteSpace(dto.Body))
            {
                ModelState.AddModelError("Body", "Nội dung thư không được để trống.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Recipients = await _messageService.GetReceivableUsersAsync(currentUserId.Value);
                ViewBag.UnreadCount = await _messageService.GetUnreadMessageCountAsync(currentUserId.Value);
                ViewBag.Folder = "compose";
                return View("Compose", dto);
            }

            try
            {
                await _messageService.SendMessageAsync(currentUserId.Value, dto);
                TempData["SuccessMessage"] = "Đã gửi thư nội bộ thành công!";
                return RedirectToAction(nameof(Sent));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi khi gửi thư: " + ex.Message);
                ViewBag.Recipients = await _messageService.GetReceivableUsersAsync(currentUserId.Value);
                ViewBag.UnreadCount = await _messageService.GetUnreadMessageCountAsync(currentUserId.Value);
                ViewBag.Folder = "compose";
                return View("Compose", dto);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/Messages/MarkAsRead/{id:int}")]
        public async Task<IActionResult> MarkAsRead(int id, [FromQuery] string? returnUrl = null)
        {
            int? currentUserId = User.GetUserId();
            if (currentUserId.HasValue)
            {
                await _messageService.MarkAsReadAsync(id, currentUserId.Value);
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Inbox));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/Messages/Delete/{id:int}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] string folder = "inbox")
        {
            int? currentUserId = User.GetUserId();
            if (currentUserId.HasValue)
            {
                await _messageService.DeleteMessageAsync(id, currentUserId.Value, folder);
                TempData["SuccessMessage"] = "Đã xóa thư thành công.";
            }

            return RedirectToAction(folder.Equals("sent", StringComparison.OrdinalIgnoreCase) ? nameof(Sent) : nameof(Inbox));
        }

        [HttpGet]
        [Route("api/messages/unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            int? currentUserId = User.GetUserId();
            if (!currentUserId.HasValue)
            {
                return Ok(new { success = false, count = 0 });
            }

            int count = await _messageService.GetUnreadMessageCountAsync(currentUserId.Value);
            return Ok(new { success = true, count });
        }
    }
}
