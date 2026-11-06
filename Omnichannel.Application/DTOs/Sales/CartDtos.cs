using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Sales
{
    public class CartDto
    {
        public int Id { get; set; }
        public string? SessionId { get; set; }
        public int? UserId { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public int TotalItems { get; set; }

        public List<CartItemDto> Items { get; set; } = new();
        public List<CartItemDto> CartItems { get; set; } = new();
    }

    // Định nghĩa CartViewModel để tương thích với OrderDtos.cs
    //// Lớp Con (Hiển thị Giỏ Hàng Web) KẾ THỪA từ Lớp Cha (Dữ liệu Giỏ Hàng)
    public class CartViewModel : CartDto
    {// Mốc được miễn phí ship (Ví dụ: 500.000 đ)
        public decimal ShippingFee { get; set; }//thêm phí vận chuyển
        public decimal FinalTotal { get; set; }

        // --- LUXURY GIFT PACKAGING ---
        // TÍNH ĐÓNG GÓI Ở ĐÂY: Thuộc tính này CHỈ CÓ ĐỌC, KHÔNG CHO PHÉP GHI (Không có 'set')
        public bool IsGiftWrapped { get; set; }//Thêm gói quà tặng sang trọng
        public string? GiftMessage { get; set; }
        public decimal GiftWrapFee { get; set; } = Omnichannel.Application.Common.OrderPricingConstants.LuxuryGiftWrapFee;

        // --- FREESHIP METER ---
        public decimal FreeShippingThreshold { get; set; } = Omnichannel.Application.Common.OrderPricingConstants.FreeShippingThreshold;
        public decimal FreeShippingRemaining => Math.Max(0, FreeShippingThreshold - SubTotal);
        public decimal FreeShippingProgress => SubTotal >= FreeShippingThreshold ? 100 : (FreeShippingThreshold > 0 ? Math.Round(SubTotal / FreeShippingThreshold * 100, 1) : 100);
        public bool IsFreeShipping => SubTotal >= FreeShippingThreshold;//// Tự động tính Freeship

        // --- CROSS-SELL CAROUSEL ---
        public List<CrossSellItemDto> CrossSellItems { get; set; } = new();

        // --- PROMOTION & REWARDS ENGINE BREAKDOWN ---
        public string? AppliedCouponCode { get; set; }
        public decimal CouponDiscount { get; set; }
        public decimal PromotionDiscount { get; set; }
        public string? AppliedPromotionName { get; set; }
        public string? MemberTierName { get; set; }
        public decimal MemberDiscountPercent { get; set; }
        public decimal MemberTierDiscount { get; set; }
        public decimal FefoDiscount { get; set; }
        public string? FefoDiscountDescription { get; set; }
        public List<FefoClearanceItemDto> FefoClearanceItems { get; set; } = new();
        public List<PromotionGiftItemDto> GiftItems { get; set; } = new();
        public List<string> AppliedRulesSummary { get; set; } = new();
        public string? PromotionMessage { get; set; }

        // --- CLINICAL INGREDIENT CONFLICT DETECTION (BEAUTY TECH) ---
        public Omnichannel.Application.DTOs.BeautyTech.SkincareRoutineAnalysisDto? ConflictAnalysis { get; set; }

        public static CartViewModel FromDto(CartDto? dto, bool isGiftWrapped = false, string? giftMessage = null)
        {
            if (dto == null)
            {
                return new CartViewModel
                {
                    Items = new List<CartItemDto>(),
                    CartItems = new List<CartItemDto>(),
                    IsGiftWrapped = isGiftWrapped,
                    GiftMessage = giftMessage
                };
            }

            decimal shippingFee = Omnichannel.Application.Common.OrderPricingConstants.CalculateStandardShippingFee(dto.TotalAmount);
            decimal giftFee = isGiftWrapped ? Omnichannel.Application.Common.OrderPricingConstants.LuxuryGiftWrapFee : 0m;
            decimal finalTotal = dto.TotalAmount + shippingFee + giftFee;

            return new CartViewModel
            {
                Id = dto.Id,
                SessionId = dto.SessionId,
                UserId = dto.UserId,
                SubTotal = dto.SubTotal,
                DiscountAmount = dto.DiscountAmount,
                TotalAmount = dto.TotalAmount,
                TotalItems = dto.TotalItems,
                Items = dto.Items ?? new List<CartItemDto>(),
                CartItems = dto.CartItems ?? new List<CartItemDto>(),
                ShippingFee = shippingFee,
                FinalTotal = finalTotal,
                IsGiftWrapped = isGiftWrapped,
                GiftMessage = giftMessage,
                GiftWrapFee = Omnichannel.Application.Common.OrderPricingConstants.LuxuryGiftWrapFee
            };
        }

        public static CartViewModel FromPromotionResult(
            CartDto dto, 
            PromotionResultDto promo, 
            bool isGiftWrapped = false, 
            string? giftMessage = null, 
            int? currentUserId = null)
        {
            decimal shippingFee = Omnichannel.Application.Common.OrderPricingConstants.CalculateStandardShippingFee(promo.FinalTotal);
            decimal giftFee = isGiftWrapped ? Omnichannel.Application.Common.OrderPricingConstants.LuxuryGiftWrapFee : 0m;
            decimal finalCheckoutTotal = promo.FinalTotal + shippingFee + giftFee;

            return new CartViewModel
            {
                Id = dto.Id,
                SessionId = dto.SessionId,
                UserId = currentUserId ?? dto.UserId,
                SubTotal = promo.SubTotal,
                DiscountAmount = promo.TotalDiscount,
                PromotionDiscount = promo.PromotionDiscount,
                AppliedPromotionName = promo.AppliedPromotionName,
                CouponDiscount = promo.CouponDiscount,
                AppliedCouponCode = promo.AppliedCouponCode,
                MemberTierName = promo.MemberTierName,
                MemberDiscountPercent = promo.MemberDiscountPercent,
                MemberTierDiscount = promo.MemberTierDiscount,
                FefoDiscount = promo.FefoDiscount,
                FefoDiscountDescription = promo.FefoDiscountDescription,
                FefoClearanceItems = promo.FefoClearanceItems,
                GiftItems = promo.GiftItems,
                AppliedRulesSummary = promo.AppliedRulesSummary,
                PromotionMessage = promo.Message,
                TotalAmount = promo.FinalTotal,
                TotalItems = dto.TotalItems,
                Items = dto.Items ?? new List<CartItemDto>(),
                CartItems = dto.CartItems ?? new List<CartItemDto>(),
                ShippingFee = shippingFee,
                FinalTotal = finalCheckoutTotal,
                IsGiftWrapped = isGiftWrapped,
                GiftMessage = giftMessage,
                GiftWrapFee = Omnichannel.Application.Common.OrderPricingConstants.LuxuryGiftWrapFee
            };
        }
    }

    public class CrossSellItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal OriginalPrice { get; set; }
        public decimal SpecialPrice { get; set; } = 29000;
        public string BadgeText { get; set; } = "Ưu đãi thêm +29K";
        public string CategoryName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class CartItemDto
    {
        public int Id { get; set; }
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductSku { get; set; }
        public string? ProductImageUrl { get; set; }
        public int? ProductBatchId { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpDate { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal LineTotal { get; set; }
        public string NearestExpDateStr { get; set; } = string.Empty;

        // --- PDP & CART ENHANCEMENTS ---
        public string? VariantName { get; set; } // Biến thể màu sắc/dung tích
        public string? BatchStatusText { get; set; } // Lô & HSD FEFO
        public bool IsNearExpiration { get; set; }
        public int DaysLeftToExpiry { get; set; }
    }

    public class AddToCartRequestDto
    {
        public string? SessionId { get; set; }
        public string? UserId { get; set; }
        public int ProductId { get; set; }
        public int? ProductBatchId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class UpdateCartItemQuantityDto
    {
        public string? SessionId { get; set; }
        public string? UserId { get; set; }
        public int CartItemId { get; set; }
        public int Quantity { get; set; }
    }
}