using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Sales;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Security;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class PromotionController : Controller
    {
        private readonly IPromotionEngine _promotionEngine;
        private readonly ApplicationDbContext _context;

        public PromotionController(IPromotionEngine promotionEngine, ApplicationDbContext context)
        {
            _promotionEngine = promotionEngine;
            _context = context;
        }

        [HttpGet]
        [HasPermission("PROMOTION_MANAGE")]
        public async Task<IActionResult> Index(string? tab, CancellationToken cancellationToken)
        {
            // Lấy toàn bộ CTKM (Bao gồm Đang chạy, Sắp tới, Đã hết hạn)
            var promoEntities = await _context.Promotions
                .AsNoTracking()
                .Include(p => p.PromotionItems)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);

            var promotions = promoEntities.Select(p => new PromotionDto
            {
                Id = p.Id,
                PromotionCode = p.PromotionCode,
                Name = p.Name,
                Description = p.Description,
                PromotionType = p.PromotionType,
                DiscountValue = p.DiscountValue,
                MinOrderValue = p.MinOrderValue,
                MaxDiscountAmount = p.MaxDiscountAmount,
                ValidFrom = p.ValidFrom,
                ValidTo = p.ValidTo,
                UsageLimit = p.UsageLimit,
                UsageCount = p.UsageCount,
                IsActive = p.IsActive,
                AppliesTo = p.AppliesTo,
                Items = p.PromotionItems.Select(i => new PromotionItemDto
                {
                    Id = i.Id,
                    PromotionId = i.PromotionId,
                    ProductId = i.ProductId,
                    CategoryId = i.CategoryId,
                    GiftProductId = i.GiftProductId,
                    GiftQuantity = i.GiftQuantity
                }).ToList()
            }).ToList();

            // Lấy danh mục Coupon
            var coupons = await _context.Coupons
                .AsNoTracking()
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync(cancellationToken);

            // Lấy danh mục Voucher đã cấp phát
            var vouchers = await _context.Vouchers
                .AsNoTracking()
                .Include(v => v.User)
                .Include(v => v.UsedOrder)
                .OrderByDescending(v => v.CreatedAt)
                .Take(150)
                .ToListAsync(cancellationToken);

            // Lấy danh sách khách hàng VIP phục vụ Modal phát hành Voucher
            var vipWallets = await _context.LoyaltyWallets
                .AsNoTracking()
                .Include(w => w.User)
                .Include(w => w.CurrentTier)
                .Where(w => w.CurrentTier.TierName == "Diamond" || w.CurrentTier.TierName == "Gold")
                .ToListAsync(cancellationToken);

            var customers = await _context.Users
                .AsNoTracking()
                .Where(u => u.UserType == 1 || u.UserRoles.Any(ur => ur.Role.Name == "Customer"))
                .OrderBy(u => u.FullName)
                .Take(100)
                .ToListAsync(cancellationToken);

            ViewBag.Promotions = promotions;
            ViewBag.Coupons = coupons;
            ViewBag.Vouchers = vouchers;
            ViewBag.VipWallets = vipWallets;
            ViewBag.Customers = customers;
            ViewBag.CurrentTab = tab ?? "promotions";

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("PROMOTION_MANAGE")]
        public async Task<IActionResult> CreatePromotion([FromForm] CreatePromotionDto dto, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(dto.PromotionCode) || string.IsNullOrWhiteSpace(dto.Name))
            {
                TempData["ErrorMessage"] = "Mã khuyến mãi và tên chương trình không được để trống.";
                return RedirectToAction(nameof(Index), new { tab = "promotions" });
            }

            try
            {
                await _promotionEngine.CreatePromotionAsync(dto, cancellationToken);
                TempData["SuccessMessage"] = $"Tạo chương trình khuyến mãi '{dto.Name}' thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi tạo khuyến mãi: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { tab = "promotions" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("COUPON_MANAGE")]
        public async Task<IActionResult> CreateCoupon([FromForm] CreateCouponDto dto, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(dto.CouponCode))
            {
                TempData["ErrorMessage"] = "Mã Coupon không được để trống.";
                return RedirectToAction(nameof(Index), new { tab = "coupons" });
            }

            try
            {
                await _promotionEngine.CreateCouponAsync(dto, cancellationToken);
                TempData["SuccessMessage"] = $"Tạo mã giảm giá '{dto.CouponCode}' thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi tạo coupon: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { tab = "coupons" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("VOUCHER_MANAGE")]
        public async Task<IActionResult> IssueVoucher([FromForm] IssueVoucherDto dto, CancellationToken cancellationToken)
        {
            if (dto.DiscountValue <= 0 || string.IsNullOrWhiteSpace(dto.Description))
            {
                TempData["ErrorMessage"] = "Giá trị giảm và mô tả voucher không được để trống.";
                return RedirectToAction(nameof(Index), new { tab = "vouchers" });
            }

            try
            {
                var targetUserIds = new List<int>();

                if (dto.TargetGroup == "VIP_DIAMOND")
                {
                    targetUserIds = await _context.LoyaltyWallets
                        .Where(w => w.CurrentTier.TierName == "Diamond")
                        .Select(w => w.UserId)
                        .ToListAsync(cancellationToken);
                }
                else if (dto.TargetGroup == "VIP_GOLD_DIAMOND")
                {
                    targetUserIds = await _context.LoyaltyWallets
                        .Where(w => w.CurrentTier.TierName == "Diamond" || w.CurrentTier.TierName == "Gold")
                        .Select(w => w.UserId)
                        .ToListAsync(cancellationToken);
                }
                else if (dto.TargetGroup == "SPECIFIC_USER" && dto.SpecificUserId.HasValue)
                {
                    targetUserIds.Add(dto.SpecificUserId.Value);
                }
                else
                {
                    // Phát hành cho tất cả khách hàng
                    targetUserIds = await _context.Users
                        .Where(u => u.UserType == 1 || u.UserRoles.Any(ur => ur.Role.Name == "Customer"))
                        .Select(u => u.Id)
                        .Take(200)
                        .ToListAsync(cancellationToken);
                }

                if (!targetUserIds.Any())
                {
                    TempData["ErrorMessage"] = "Không tìm thấy khách hàng nào thuộc nhóm đối tượng đã chọn.";
                    return RedirectToAction(nameof(Index), new { tab = "vouchers" });
                }

                var now = DateTime.UtcNow;
                var validTo = now.AddDays(Math.Max(1, dto.ValidDays));
                string prefix = !string.IsNullOrWhiteSpace(dto.VoucherCodePrefix) ? dto.VoucherCodePrefix.Trim().ToUpperInvariant() : "VIP";

                var newVouchers = targetUserIds.Select(uid => new Voucher
                {
                    UserId = uid,
                    VoucherCode = $"{prefix}-{Random.Shared.Next(1000, 9999)}-{uid:D3}",
                    Description = dto.Description.Trim(),
                    DiscountType = dto.DiscountType,
                    DiscountValue = dto.DiscountValue,
                    MinOrderValue = dto.MinOrderValue,
                    MaxDiscountAmount = dto.MaxDiscountAmount,
                    ValidFrom = now,
                    ValidTo = validTo,
                    IsUsed = false,
                    CreatedAt = now
                }).ToList();

                await _context.Vouchers.AddRangeAsync(newVouchers, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                TempData["SuccessMessage"] = $"Đã phát hành thành công {newVouchers.Count} voucher ưu đãi cho khách hàng!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi phát hành voucher: {ex.Message}";
            }

            return RedirectToAction(nameof(Index), new { tab = "vouchers" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("PROMOTION_MANAGE")]
        public async Task<IActionResult> TogglePromotion(int id, CancellationToken cancellationToken)
        {
            var promo = await _context.Promotions.FindAsync(new object[] { id }, cancellationToken);
            if (promo != null)
            {
                promo.IsActive = !promo.IsActive;
                promo.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                TempData["SuccessMessage"] = $"Đã cập nhật trạng thái CTKM {promo.PromotionCode}.";
            }

            return RedirectToAction(nameof(Index), new { tab = "promotions" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("COUPON_MANAGE")]
        public async Task<IActionResult> ToggleCoupon(int id, CancellationToken cancellationToken)
        {
            var coupon = await _context.Coupons.FindAsync(new object[] { id }, cancellationToken);
            if (coupon != null)
            {
                coupon.IsActive = !coupon.IsActive;
                await _context.SaveChangesAsync(cancellationToken);
                TempData["SuccessMessage"] = $"Đã cập nhật trạng thái Coupon {coupon.CouponCode}.";
            }

            return RedirectToAction(nameof(Index), new { tab = "coupons" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("PROMOTION_MANAGE")]
        public async Task<IActionResult> DeletePromotion(int id, CancellationToken cancellationToken)
        {
            var promo = await _context.Promotions.Include(p => p.PromotionItems).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (promo != null)
            {
                _context.PromotionItems.RemoveRange(promo.PromotionItems);
                _context.Promotions.Remove(promo);
                await _context.SaveChangesAsync(cancellationToken);
                TempData["SuccessMessage"] = "Đã xóa chương trình khuyến mãi.";
            }

            return RedirectToAction(nameof(Index), new { tab = "promotions" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("COUPON_MANAGE")]
        public async Task<IActionResult> DeleteCoupon(int id, CancellationToken cancellationToken)
        {
            var coupon = await _context.Coupons.FindAsync(new object[] { id }, cancellationToken);
            if (coupon != null)
            {
                _context.Coupons.Remove(coupon);
                await _context.SaveChangesAsync(cancellationToken);
                TempData["SuccessMessage"] = "Đã xóa mã coupon.";
            }

            return RedirectToAction(nameof(Index), new { tab = "coupons" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("VOUCHER_MANAGE")]
        public async Task<IActionResult> DeleteVoucher(int id, CancellationToken cancellationToken)
        {
            var voucher = await _context.Vouchers.FindAsync(new object[] { id }, cancellationToken);
            if (voucher != null)
            {
                _context.Vouchers.Remove(voucher);
                await _context.SaveChangesAsync(cancellationToken);
                TempData["SuccessMessage"] = "Đã thu hồi voucher thành công.";
            }

            return RedirectToAction(nameof(Index), new { tab = "vouchers" });
        }

        [HttpGet]
        [HasPermission("PROMOTION_MANAGE")]
        public async Task<IActionResult> ValidateCouponApi(string couponCode, decimal orderTotal, CancellationToken cancellationToken)
        {
            var isValid = await _promotionEngine.ValidateCouponAsync(couponCode, null, orderTotal, cancellationToken);
            var coupon = await _promotionEngine.GetCouponByCodeAsync(couponCode, cancellationToken);

            return Json(new
            {
                isValid,
                coupon
            });
        }
    }
}
