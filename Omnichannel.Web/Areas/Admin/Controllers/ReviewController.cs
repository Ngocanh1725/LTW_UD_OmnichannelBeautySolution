using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Cms;
using Omnichannel.Application.Interfaces.Cms;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Security;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly IProductReviewService _reviewService;
        private readonly ApplicationDbContext _context;

        public ReviewController(IProductReviewService reviewService, ApplicationDbContext context)
        {
            _reviewService = reviewService;
            _context = context;
        }

        [HttpGet]
        [HasPermission("CMS_REVIEW_MODERATE")]
        public async Task<IActionResult> Index([FromQuery] ReviewFilterDto filter, [FromQuery] string? tab = null)
        {
            filter ??= new ReviewFilterDto();

            // Áp dụng bộ lọc theo tab nếu có
            if (!string.IsNullOrEmpty(tab))
            {
                switch (tab.ToLowerInvariant())
                {
                    case "pending":
                        filter.IsApproved = false;
                        filter.IsHidden = false;
                        break;
                    case "approved":
                        filter.IsApproved = true;
                        filter.IsHidden = false;
                        break;
                    case "hidden":
                        filter.IsHidden = true;
                        filter.IsApproved = null;
                        break;
                }
            }

            var reviews = await _reviewService.GetAllReviewsAsync(filter);

            // Thống kê đếm số lượng theo trạng thái
            ViewBag.TotalCount = await _context.ProductReviews.CountAsync();
            ViewBag.PendingCount = await _context.ProductReviews.CountAsync(r => !r.IsApproved && !r.IsHidden);
            ViewBag.ApprovedCount = await _context.ProductReviews.CountAsync(r => r.IsApproved && !r.IsHidden);
            ViewBag.HiddenCount = await _context.ProductReviews.CountAsync(r => r.IsHidden);

            ViewBag.Filter = filter;
            ViewBag.CurrentTab = tab ?? "all";

            return View(reviews);
        }

        [HttpGet]
        [HasPermission("CMS_REVIEW_MODERATE")]
        public async Task<IActionResult> Pending([FromQuery] int page = 1)
        {
            return RedirectToAction(nameof(Index), new { tab = "pending", page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("CMS_REVIEW_MODERATE")]
        public async Task<IActionResult> Approve(int reviewId, bool isApproved, string? adminNote = null)
        {
            var success = await _reviewService.ApproveReviewAsync(reviewId, isApproved, adminNote);
            if (!success)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "Không tìm thấy đánh giá để phê duyệt." });

                TempData["ErrorMessage"] = "Không tìm thấy đánh giá để phê duyệt.";
                return RedirectToAction(nameof(Index));
            }

            string msg = isApproved ? "Đã phê duyệt đánh giá thành công! Đánh giá sẽ xuất hiện trên Storefront." : "Đã chuyển đánh giá sang trạng thái từ chối.";
            
            if (IsAjaxRequest())
                return Json(new { success = true, message = msg });

            TempData["SuccessMessage"] = msg;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("CMS_REVIEW_MODERATE")]
        public async Task<IActionResult> ToggleHide(int reviewId, bool isHidden, string? adminNote = null)
        {
            var success = await _reviewService.ToggleHideReviewAsync(reviewId, isHidden, adminNote);
            if (!success)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "Không tìm thấy đánh giá." });

                TempData["ErrorMessage"] = "Không tìm thấy đánh giá.";
                return RedirectToAction(nameof(Index));
            }

            string msg = isHidden ? "Đã ẩn đánh giá khỏi giao diện website." : "Đã mở lại đánh giá thành công.";

            if (IsAjaxRequest())
                return Json(new { success = true, message = msg });

            TempData["SuccessMessage"] = msg;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("CMS_REVIEW_MODERATE")]
        public async Task<IActionResult> Delete(int reviewId)
        {
            var success = await _reviewService.DeleteReviewAsync(reviewId);
            if (!success)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "Không tìm thấy đánh giá để xóa." });

                TempData["ErrorMessage"] = "Không tìm thấy đánh giá để xóa.";
                return RedirectToAction(nameof(Index));
            }

            if (IsAjaxRequest())
                return Json(new { success = true, message = "Đã xóa vĩnh viễn đánh giá khỏi hệ thống!" });

            TempData["SuccessMessage"] = "Đã xóa vĩnh viễn đánh giá khỏi hệ thống!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [HasPermission("CMS_REVIEW_MODERATE")]
        public async Task<IActionResult> Stats(int productId)
        {
            var stats = await _reviewService.GetReviewStatsAsync(productId);
            return Json(stats);
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                   Request.Headers["Accept"].ToString().Contains("application/json") ||
                   Request.ContentType?.Contains("application/json") == true;
        }
    }
}
