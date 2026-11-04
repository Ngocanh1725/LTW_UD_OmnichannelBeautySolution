using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omnichannel.Application.DTOs.Notifications;
using Omnichannel.Application.Interfaces.Notifications;

namespace Omnichannel.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionController : ControllerBase
    {
        private readonly IInventoryNotificationService _subscriptionService;

        public SubscriptionController(IInventoryNotificationService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claim, out int userId) && userId > 0)
            {
                return userId;
            }
            return null;
        }

        /// <summary>
        /// Đăng ký nhận thông báo khi có hàng lại (Back-in-Stock Alert)
        /// </summary>
        [HttpPost("back-in-stock")]
        public async Task<IActionResult> SubscribeBackInStock([FromBody] SubscribeBackInStockRequest request)
        {
            if (request == null || request.ProductId <= 0)
            {
                return BadRequest(new { success = false, message = "Thông tin sản phẩm không hợp lệ." });
            }

            var userId = GetCurrentUserId();
            var result = await _subscriptionService.SubscribeBackInStockAsync(request, userId);

            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new
            {
                success = true,
                message = result.Message,
                subscriptionId = result.SubscriptionId,
                alreadySubscribed = result.AlreadySubscribed
            });
        }

        /// <summary>
        /// Đăng ký theo dõi giảm giá (Price Drop Alert)
        /// </summary>
        [HttpPost("price-drop")]
        public async Task<IActionResult> SubscribePriceDrop([FromBody] SubscribePriceDropRequest request)
        {
            if (request == null || request.ProductId <= 0 || request.TargetPrice <= 0)
            {
                return BadRequest(new { success = false, message = "Thông tin đăng ký không hợp lệ." });
            }

            var userId = GetCurrentUserId();
            var result = await _subscriptionService.SubscribePriceDropAsync(request, userId);

            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new
            {
                success = true,
                message = result.Message,
                subscriptionId = result.SubscriptionId,
                alreadySubscribed = result.AlreadySubscribed
            });
        }

        /// <summary>
        /// Hủy theo dõi một thông báo
        /// </summary>
        [Authorize]
        [HttpPost("unsubscribe/{id:int}")]
        public async Task<IActionResult> Unsubscribe(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập để thực hiện thao tác này." });
            }

            var success = await _subscriptionService.UnsubscribeAsync(id, userId.Value);
            if (!success)
            {
                return NotFound(new { success = false, message = "Không tìm thấy đăng ký theo dõi này hoặc đã bị hủy." });
            }

            return Ok(new { success = true, message = "Đã hủy theo dõi sản phẩm thành công." });
        }
    }
}

