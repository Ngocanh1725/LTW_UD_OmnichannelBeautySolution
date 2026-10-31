using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Sales;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services
{
    public class PromotionEngine : IPromotionEngine
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PromotionEngine> _logger;

        public PromotionEngine(ApplicationDbContext context, ILogger<PromotionEngine> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PromotionResultDto> EvaluateCartAsync(
            List<CartItemDto> items,
            string? couponCode = null,
            int? userId = null,
            CancellationToken cancellationToken = default)
        {
            var result = new PromotionResultDto();

            if (items == null || !items.Any())
            {
                return result;
            }

            decimal subTotal = items.Sum(i => i.UnitPrice * i.Quantity);
            result.SubTotal = subTotal;

            var now = DateTime.UtcNow;

            // =========================================================================
            // 1. KHUYẾN MÃI THEO HẠNG THÀNH VIÊN (LOYALTY TIER DISCOUNT)
            // Silver: -5%, Gold: -10%, Diamond: -15% tự động không cần nhập mã
            // =========================================================================
            if (userId.HasValue && userId.Value > 0)
            {
                try
                {
                    var wallet = await _context.LoyaltyWallets
                        .AsNoTracking()
                        .Include(w => w.CurrentTier)
                        .FirstOrDefaultAsync(w => w.UserId == userId.Value, cancellationToken);

                    if (wallet?.CurrentTier != null && wallet.CurrentTier.IsActive)
                    {
                        string tierName = (wallet.CurrentTier.TierName ?? "").Trim().ToUpperInvariant();
                        decimal tierPercent = 0m;

                        if (tierName.Contains("DIAMOND") || tierName.Contains("KIM CƯƠNG"))
                        {
                            tierPercent = MemberTierDiscounts.DiamondPercent; // 15%
                        }
                        else if (tierName.Contains("PLATINUM") || tierName.Contains("BẠCH KIM"))
                        {
                            tierPercent = 12m; // 12%
                        }
                        else if (tierName.Contains("GOLD") || tierName.Contains("VÀNG"))
                        {
                            tierPercent = MemberTierDiscounts.GoldPercent; // 10%
                        }
                        else if (tierName.Contains("SILVER") || tierName.Contains("BẠC"))
                        {
                            tierPercent = MemberTierDiscounts.SilverPercent; // 5%
                        }

                        if (tierPercent > 0)
                        {
                            result.MemberTierName = wallet.CurrentTier.TierName;
                            result.MemberDiscountPercent = tierPercent;
                            result.MemberTierDiscount = Math.Round(subTotal * (tierPercent / 100m), 0);
                            result.AppliedRulesSummary.Add($"Đặc quyền Thành viên {wallet.CurrentTier.TierName}: Giảm thêm {tierPercent:0.#}% (-{result.MemberTierDiscount:N0}₫)");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi khi tra cứu hạng thành viên cho UserId {UserId}", userId);
                }
            }

            // =========================================================================
            // 2. KHUYẾN MÃI GIẢM GIÁ THEO LÔ HẠN DÙNG (FEFO CLEARANCE)
            // Giảm giá riêng cho sản phẩm có hạn dùng cận date (mặc định 20% hoặc theo CTKM)
            // =========================================================================
            decimal fefoClearancePercent = 20m;
            var fefoPromo = await _context.Promotions
                .AsNoTracking()
                .Where(p => p.IsActive && p.PromotionType == PromotionTypes.FefoClearance && p.ValidFrom <= now && p.ValidTo >= now)
                .OrderByDescending(p => p.DiscountValue)
                .FirstOrDefaultAsync(cancellationToken);

            if (fefoPromo != null && fefoPromo.DiscountValue > 0)
            {
                fefoClearancePercent = fefoPromo.DiscountValue;
            }

            foreach (var item in items)
            {
                bool isNearDate = item.IsNearExpiration;
                if (!isNearDate && item.ExpDate.HasValue)
                {
                    var days = (item.ExpDate.Value - now).TotalDays;
                    if (days <= 180 && days >= 0)
                    {
                        isNearDate = true;
                    }
                }

                if (isNearDate)
                {
                    decimal itemTotal = item.UnitPrice * item.Quantity;
                    decimal itemDiscount = Math.Round(itemTotal * (fefoClearancePercent / 100m), 0);
                    result.FefoDiscount += itemDiscount;

                    int daysLeft = item.DaysLeftToExpiry > 0 ? item.DaysLeftToExpiry : (item.ExpDate.HasValue ? Math.Max(0, (int)(item.ExpDate.Value - now).TotalDays) : 30);
                    result.FefoClearanceItems.Add(new FefoClearanceItemDto
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        BatchNumber = item.BatchNumber ?? (string.IsNullOrEmpty(item.NearestExpDateStr) ? "FEFO-LOT" : item.NearestExpDateStr),
                        ExpDate = item.ExpDate,
                        DaysLeftToExpiry = daysLeft,
                        OriginalPrice = item.UnitPrice,
                        DiscountPercent = fefoClearancePercent,
                        DiscountAmount = itemDiscount,
                        Quantity = item.Quantity
                    });
                }
            }

            if (result.FefoDiscount > 0)
            {
                result.FefoDiscountDescription = $"Xả kho cận date FEFO: Giảm {fefoClearancePercent:0.#}% cho {result.FefoClearanceItems.Count} dòng sản phẩm có hạn dùng cận ngày.";
                result.AppliedRulesSummary.Add($"Xả kho FEFO cận date: Giảm {fefoClearancePercent:0.#}% (-{result.FefoDiscount:N0}₫)");
            }

            // =========================================================================
            // 3. CÁC CHƯƠNG TRÌNH KHUYẾN MÃI TỰ ĐỘNG (PERCENTAGE, FIXED_AMOUNT, FLASH_SALE, BUY_X_GET_Y, TIERED_GWP)
            // =========================================================================
            var activePromotions = await _context.Promotions
                .AsNoTracking()
                .Include(p => p.PromotionItems)
                    .ThenInclude(pi => pi.GiftProduct)
                .Where(p => p.IsActive && p.ValidFrom <= now && p.ValidTo >= now)
                .Where(p => p.PromotionType != PromotionTypes.MemberTier && p.PromotionType != PromotionTypes.FefoClearance)
                .Where(p => !p.UsageLimit.HasValue || p.UsageCount < p.UsageLimit.Value)
                .OrderByDescending(p => p.DiscountValue)
                .ToListAsync(cancellationToken);

            Promotion? bestPromotion = null;
            decimal maxPromoDiscount = 0m;
            List<PromotionGiftItemDto> bestGifts = new();

            foreach (var promo in activePromotions)
            {
                if (promo.MinOrderValue.HasValue && subTotal < promo.MinOrderValue.Value)
                {
                    continue;
                }

                decimal currentDiscount = 0m;
                var currentGifts = new List<PromotionGiftItemDto>();

                switch (promo.PromotionType.ToUpperInvariant())
                {
                    case "PERCENTAGE":
                    case "FLASH_SALE":
                        if (promo.AppliesTo == "ALL")
                        {
                            currentDiscount = subTotal * (promo.DiscountValue / 100m);
                        }
                        else if (promo.AppliesTo == "SPECIFIC_PRODUCTS")
                        {
                            var applicableProductIds = promo.PromotionItems
                                .Where(pi => pi.ProductId.HasValue)
                                .Select(pi => pi.ProductId!.Value)
                                .ToHashSet();

                            var applicableTotal = items
                                .Where(i => applicableProductIds.Contains(i.ProductId))
                                .Sum(i => i.UnitPrice * i.Quantity);

                            currentDiscount = applicableTotal * (promo.DiscountValue / 100m);
                        }

                        if (promo.MaxDiscountAmount.HasValue && currentDiscount > promo.MaxDiscountAmount.Value)
                        {
                            currentDiscount = promo.MaxDiscountAmount.Value;
                        }
                        break;

                    case "FIXED_AMOUNT":
                        currentDiscount = Math.Min(subTotal, promo.DiscountValue);
                        break;

                    case "BUY_X_GET_Y":
                        var buyItemConfigs = promo.PromotionItems
                            .Where(pi => pi.ProductId.HasValue && pi.GiftProductId.HasValue)
                            .ToList();

                        foreach (var cfg in buyItemConfigs)
                        {
                            var cartItem = items.FirstOrDefault(i => i.ProductId == cfg.ProductId!.Value);
                            if (cartItem != null && cartItem.Quantity > 0)
                            {
                                int giftCount = cfg.GiftQuantity * cartItem.Quantity;
                                currentGifts.Add(new PromotionGiftItemDto
                                {
                                    ProductId = cfg.GiftProductId!.Value,
                                    ProductName = cfg.GiftProduct?.Name ?? "Quà tặng kèm",
                                    ProductImageUrl = cfg.GiftProduct?.ThumbnailUrl,
                                    Quantity = giftCount,
                                    GiftReason = $"Tặng kèm khi mua {cartItem.ProductName}"
                                });
                            }
                        }
                        currentDiscount = promo.DiscountValue;
                        break;

                    case "TIERED_GWP":
                        foreach (var pi in promo.PromotionItems.Where(pi => pi.GiftProductId.HasValue))
                        {
                            currentGifts.Add(new PromotionGiftItemDto
                            {
                                ProductId = pi.GiftProductId!.Value,
                                ProductName = pi.GiftProduct?.Name ?? "Quà tặng đơn hàng",
                                ProductImageUrl = pi.GiftProduct?.ThumbnailUrl,
                                Quantity = pi.GiftQuantity,
                                GiftReason = $"Quà tặng bậc thang cho đơn từ {promo.MinOrderValue:N0} VNĐ"
                            });
                        }
                        break;
                }

                if (currentDiscount > maxPromoDiscount || (currentDiscount == maxPromoDiscount && currentGifts.Count > bestGifts.Count))
                {
                    maxPromoDiscount = currentDiscount;
                    bestPromotion = promo;
                    bestGifts = currentGifts;
                }
            }

            if (bestPromotion != null)
            {
                result.AppliedPromotionId = bestPromotion.Id;
                result.AppliedPromotionCode = bestPromotion.PromotionCode;
                result.AppliedPromotionName = bestPromotion.Name;
                result.PromotionDiscount = Math.Min(subTotal, maxPromoDiscount);
                result.GiftItems.AddRange(bestGifts);
                result.AppliedRulesSummary.Add($"CTKM '{bestPromotion.Name}': Giảm -{result.PromotionDiscount:N0}₫");
            }

            // =========================================================================
            // 4. QUY TẮC MUA X TẶNG Y ĐẶC QUYỀN:
            // "Mua 1 chai kem chống nắng tặng 1 son mini hoặc bông tẩy trang"
            // =========================================================================
            var sunscreenItems = items.Where(i =>
            {
                string name = (i.ProductName ?? "").ToLowerInvariant();
                return name.Contains("chống nắng") || name.Contains("sunscreen") || name.Contains("kem chong nang");
            }).ToList();

            if (sunscreenItems.Any())
            {
                int totalSunscreenQty = sunscreenItems.Sum(i => i.Quantity);
                bool alreadyHasSunscreenGift = result.GiftItems.Any(g => g.GiftReason.Contains("chống nắng") || g.GiftReason.Contains("Sunscreen"));

                if (!alreadyHasSunscreenGift && totalSunscreenQty > 0)
                {
                    result.GiftItems.Add(new PromotionGiftItemDto
                    {
                        ProductId = 8881,
                        ProductName = "Bông tẩy trang Silcot Silky cao cấp (hoặc Son Romand Juicy Tint Mini 2g)",
                        ProductImageUrl = "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=200&h=200&fit=crop",
                        Quantity = totalSunscreenQty,
                        GiftReason = $"Ưu đãi Mua X Tặng Y: Mua 1 Kem chống nắng tặng 1 Son mini hoặc Bông tẩy trang (x{totalSunscreenQty})"
                    });
                    result.AppliedRulesSummary.Add($"Mua X Tặng Y: Tặng {totalSunscreenQty} phần quà khi mua Kem chống nắng");
                }
            }

            // =========================================================================
            // 5. ĐÁNH GIÁ MÃ COUPON VỚI BỘ VALIDATION NGẶT NGHÈO
            // =========================================================================
            if (!string.IsNullOrWhiteSpace(couponCode))
            {
                var validation = await ValidateCouponDetailAsync(couponCode, userId, subTotal, cancellationToken);
                if (!validation.IsValid)
                {
                    result.Message = validation.Message;
                    result.IsSuccess = false;
                }
                else
                {
                    decimal currentDiscountSoFar = result.PromotionDiscount + result.MemberTierDiscount + result.FefoDiscount;
                    decimal remainingTotal = Math.Max(0, subTotal - currentDiscountSoFar);
                    decimal couponDiscount = 0m;

                    if (validation.DiscountType == "PERCENTAGE")
                    {
                        couponDiscount = remainingTotal * (validation.DiscountValue / 100m);
                        if (validation.MaxDiscountAmount.HasValue && couponDiscount > validation.MaxDiscountAmount.Value)
                        {
                            couponDiscount = validation.MaxDiscountAmount.Value;
                        }
                    }
                    else
                    {
                        couponDiscount = Math.Min(remainingTotal, validation.DiscountValue);
                    }

                    couponDiscount = Math.Round(couponDiscount, 0);
                    result.AppliedCouponCode = validation.CouponCode;
                    result.CouponDiscount = couponDiscount;
                    result.Message = $"Áp dụng mã giảm giá '{validation.CouponCode}' thành công!";
                    result.IsSuccess = true;
                    result.AppliedRulesSummary.Add($"Mã giảm giá {validation.CouponCode}: Giảm -{couponDiscount:N0}₫");
                }
            }

            // =========================================================================
            // 6. TỔNG HỢP VÀ CHỐNG VƯỢT QUÁ TỔNG TIỀN ĐƠN HÀNG
            // =========================================================================
            if (result.TotalDiscount > subTotal)
            {
                // Điều chỉnh tỷ lệ nếu tổng chiết khấu vượt giá trị hàng
                decimal excess = result.TotalDiscount - subTotal;
                if (result.CouponDiscount >= excess)
                {
                    result.CouponDiscount -= excess;
                }
                else
                {
                    excess -= result.CouponDiscount;
                    result.CouponDiscount = 0;
                    if (result.MemberTierDiscount >= excess)
                    {
                        result.MemberTierDiscount -= excess;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Kiểm tra tính hợp lệ của mã coupon (boolean wrapper cho backward compatibility)
        /// </summary>
        public async Task<bool> ValidateCouponAsync(
            string couponCode,
            int? userId,
            decimal orderTotal,
            CancellationToken cancellationToken = default)
        {
            var detail = await ValidateCouponDetailAsync(couponCode, userId, orderTotal, cancellationToken);
            return detail.IsValid;
        }

        /// <summary>
        /// Kiểm tra điều kiện ngặt nghèo của mã coupon và trả về thông báo lỗi rõ ràng
        /// </summary>
        public async Task<PromotionValidationResultDto> ValidateCouponDetailAsync(
            string couponCode,
            int? userId,
            decimal orderTotal,
            CancellationToken cancellationToken = default)
        {
            var res = new PromotionValidationResultDto { CouponCode = couponCode };

            if (string.IsNullOrWhiteSpace(couponCode))
            {
                res.IsValid = false;
                res.ErrorCode = "EMPTY_CODE";
                res.Message = "Vui lòng nhập mã giảm giá.";
                return res;
            }

            var cleanCode = couponCode.Trim().ToUpperInvariant();
            res.CouponCode = cleanCode;
            var now = DateTime.UtcNow;

            var coupon = await _context.Coupons
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CouponCode == cleanCode, cancellationToken);

            // 1. Kiểm tra tồn tại và kích hoạt
            if (coupon == null || !coupon.IsActive)
            {
                res.IsValid = false;
                res.ErrorCode = "NOT_FOUND_OR_INACTIVE";
                res.Message = "Mã giảm giá không tồn tại hoặc đã bị vô hiệu hóa.";
                return res;
            }

            res.DiscountType = coupon.DiscountType;
            res.DiscountValue = coupon.DiscountValue;
            res.MaxDiscountAmount = coupon.MaxDiscountAmount;
            res.MinOrderValue = coupon.MinOrderValue;
            res.UsageLimit = coupon.UsageLimit;
            res.UsageCount = coupon.UsageCount;
            res.UsageLimitPerUser = coupon.UsageLimitPerUser;
            res.ValidFrom = coupon.ValidFrom;
            res.ValidTo = coupon.ValidTo;

            // 2. Kiểm tra ngày bắt đầu
            if (now < coupon.ValidFrom)
            {
                res.IsValid = false;
                res.ErrorCode = "NOT_STARTED";
                res.Message = $"Mã giảm giá chưa đến ngày áp dụng (Hiệu lực từ {coupon.ValidFrom.ToLocalTime():dd/MM/yyyy HH:mm}).";
                return res;
            }

            // 3. Kiểm tra ngày kết thúc
            if (now > coupon.ValidTo)
            {
                res.IsValid = false;
                res.ErrorCode = "EXPIRED";
                res.Message = $"Mã giảm giá đã hết hạn sử dụng từ ngày {coupon.ValidTo.ToLocalTime():dd/MM/yyyy HH:mm}.";
                return res;
            }

            // 4. Kiểm tra giới hạn số lần sử dụng tổng thể toàn hệ thống
            if (coupon.UsageLimit.HasValue && coupon.UsageCount >= coupon.UsageLimit.Value)
            {
                res.IsValid = false;
                res.ErrorCode = "USAGE_LIMIT_EXCEEDED";
                res.Message = $"Mã giảm giá đã hết lượt sử dụng trên toàn hệ thống ({coupon.UsageCount}/{coupon.UsageLimit.Value} lượt).";
                return res;
            }

            // 5. Kiểm tra giá trị đơn hàng tối thiểu
            if (coupon.MinOrderValue.HasValue && orderTotal < coupon.MinOrderValue.Value)
            {
                var viCulture = new System.Globalization.CultureInfo("vi-VN");
                decimal missing = coupon.MinOrderValue.Value - orderTotal;
                res.MissingAmount = missing;
                res.IsValid = false;
                res.ErrorCode = "MIN_ORDER_NOT_MET";
                res.Message = $"Mã này chỉ áp dụng cho đơn từ {coupon.MinOrderValue.Value.ToString("N0", viCulture)}₫ (Đơn hiện tại: {orderTotal.ToString("N0", viCulture)}₫, còn thiếu {missing.ToString("N0", viCulture)}₫).";
                return res;
            }

            // 6. Kiểm tra giới hạn số lần sử dụng trên mỗi khách hàng
            if (userId.HasValue && coupon.UsageLimitPerUser.HasValue)
            {
                var userUsageCount = await _context.CouponUsages
                    .CountAsync(cu => cu.CouponId == coupon.Id && cu.UserId == userId.Value, cancellationToken);

                res.UserUsageCount = userUsageCount;
                if (userUsageCount >= coupon.UsageLimitPerUser.Value)
                {
                    res.IsValid = false;
                    res.ErrorCode = "USER_LIMIT_EXCEEDED";
                    res.Message = $"Bạn đã sử dụng hết số lần cho phép ({coupon.UsageLimitPerUser.Value} lần) của mã này.";
                    return res;
                }
            }

            res.IsValid = true;
            res.ErrorCode = null;
            res.Message = "Mã giảm giá hợp lệ và đã sẵn sàng áp dụng!";
            return res;
        }

        public async Task ApplyCouponUsageAsync(
            string couponCode,
            int? userId,
            int orderId,
            decimal discount,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(couponCode) || discount <= 0) return;

            var cleanCode = couponCode.Trim().ToUpperInvariant();
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.CouponCode == cleanCode, cancellationToken);

            if (coupon == null) return;

            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                coupon.UsageCount++;

                var usage = new CouponUsage
                {
                    CouponId = coupon.Id,
                    UserId = userId,
                    OrderId = orderId,
                    DiscountApplied = discount,
                    UsedAt = DateTime.UtcNow
                };

                await _context.CouponUsages.AddAsync(usage, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Ghi nhận sử dụng Coupon {CouponCode} cho OrderId {OrderId} với mức giảm {Discount} VNĐ",
                    couponCode, orderId, discount);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Lỗi khi ghi nhận CouponUsage cho mã {CouponCode}", couponCode);
                throw;
            }
        }

        public async Task<List<PromotionDto>> GetActivePromotionsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var promotions = await _context.Promotions
                .AsNoTracking()
                .Include(p => p.PromotionItems)
                    .ThenInclude(pi => pi.Product)
                .Include(p => p.PromotionItems)
                    .ThenInclude(pi => pi.Category)
                .Include(p => p.PromotionItems)
                    .ThenInclude(pi => pi.GiftProduct)
                .Where(p => p.IsActive && p.ValidFrom <= now && p.ValidTo >= now)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);

            return promotions.Select(p => new PromotionDto
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
                Items = p.PromotionItems.Select(pi => new PromotionItemDto
                {
                    Id = pi.Id,
                    PromotionId = pi.PromotionId,
                    ProductId = pi.ProductId,
                    ProductName = pi.Product?.Name,
                    CategoryId = pi.CategoryId,
                    CategoryName = pi.Category?.Name,
                    GiftProductId = pi.GiftProductId,
                    GiftProductName = pi.GiftProduct?.Name,
                    GiftQuantity = pi.GiftQuantity
                }).ToList()
            }).ToList();
        }

        public async Task<CouponDto?> GetCouponByCodeAsync(string couponCode, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(couponCode)) return null;

            var cleanCode = couponCode.Trim().ToUpperInvariant();
            var c = await _context.Coupons
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CouponCode == cleanCode, cancellationToken);

            if (c == null) return null;

            return new CouponDto
            {
                Id = c.Id,
                CouponCode = c.CouponCode,
                Description = c.Description,
                DiscountType = c.DiscountType,
                DiscountValue = c.DiscountValue,
                MinOrderValue = c.MinOrderValue,
                MaxDiscountAmount = c.MaxDiscountAmount,
                ValidFrom = c.ValidFrom,
                ValidTo = c.ValidTo,
                UsageLimit = c.UsageLimit,
                UsageLimitPerUser = c.UsageLimitPerUser,
                UsageCount = c.UsageCount,
                IsActive = c.IsActive
            };
        }

        public async Task<PromotionDto> CreatePromotionAsync(CreatePromotionDto dto, CancellationToken cancellationToken = default)
        {
            var promo = new Promotion
            {
                PromotionCode = dto.PromotionCode.Trim().ToUpperInvariant(),
                Name = dto.Name,
                Description = dto.Description,
                PromotionType = dto.PromotionType,
                DiscountValue = dto.DiscountValue,
                MinOrderValue = dto.MinOrderValue,
                MaxDiscountAmount = dto.MaxDiscountAmount,
                ValidFrom = dto.ValidFrom,
                ValidTo = dto.ValidTo,
                UsageLimit = dto.UsageLimit,
                UsageCount = 0,
                IsActive = true,
                AppliesTo = dto.AppliesTo,
                CreatedAt = DateTime.UtcNow
            };

            if (dto.Items != null && dto.Items.Any())
            {
                foreach (var it in dto.Items)
                {
                    promo.PromotionItems.Add(new PromotionItem
                    {
                        ProductId = it.ProductId,
                        CategoryId = it.CategoryId,
                        GiftProductId = it.GiftProductId,
                        GiftQuantity = it.GiftQuantity
                    });
                }
            }

            await _context.Promotions.AddAsync(promo, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return (await GetActivePromotionsAsync(cancellationToken)).First(p => p.Id == promo.Id);
        }

        public async Task<CouponDto> CreateCouponAsync(CreateCouponDto dto, CancellationToken cancellationToken = default)
        {
            var coupon = new Coupon
            {
                CouponCode = dto.CouponCode.Trim().ToUpperInvariant(),
                Description = dto.Description,
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                MinOrderValue = dto.MinOrderValue,
                MaxDiscountAmount = dto.MaxDiscountAmount,
                ValidFrom = dto.ValidFrom,
                ValidTo = dto.ValidTo,
                UsageLimit = dto.UsageLimit,
                UsageLimitPerUser = dto.UsageLimitPerUser,
                UsageCount = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Coupons.AddAsync(coupon, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return new CouponDto
            {
                Id = coupon.Id,
                CouponCode = coupon.CouponCode,
                Description = coupon.Description,
                DiscountType = coupon.DiscountType,
                DiscountValue = coupon.DiscountValue,
                MinOrderValue = coupon.MinOrderValue,
                MaxDiscountAmount = coupon.MaxDiscountAmount,
                ValidFrom = coupon.ValidFrom,
                ValidTo = coupon.ValidTo,
                UsageLimit = coupon.UsageLimit,
                UsageLimitPerUser = coupon.UsageLimitPerUser,
                UsageCount = coupon.UsageCount,
                IsActive = coupon.IsActive
            };
        }
    }
}
