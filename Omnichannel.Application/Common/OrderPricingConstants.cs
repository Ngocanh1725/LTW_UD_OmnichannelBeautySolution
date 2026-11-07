using System;
using System.Globalization;

namespace Omnichannel.Application.Common
{
    /// <summary>
    /// Nguồn chân lý duy nhất (Single Source of Truth) cho các hằng số và quy tắc tính toán phí đơn hàng
    /// Áp dụng đồng bộ cho Cart, Checkout, OrderWorkflow và POS.
    /// </summary>
    public static class OrderPricingConstants
    {
        /// <summary>
        /// Ngưỡng miễn phí vận chuyển toàn quốc: 500.000 ₫
        /// </summary>
        public const decimal FreeShippingThreshold = 500000m;

        /// <summary>
        /// Cước phí vận chuyển tiêu chuẩn toàn quốc: 30.000 ₫
        /// </summary>
        public const decimal StandardShippingFee = 30000m;

        /// <summary>
        /// Cước phí giao hàng hỏa tốc 2H (nội thành): 45.000 ₫
        /// </summary>
        public const decimal ExpressShippingFee = 45000m;

        /// <summary>
        /// Phí dịch vụ đóng gói quà tặng thượng lưu: 50.000 ₫
        /// </summary>
        public const decimal LuxuryGiftWrapFee = 50000m;

        /// <summary>
        /// Tính toán cước phí vận chuyển tiêu chuẩn dựa trên giá trị giỏ hàng (sau chiết khấu)
        /// </summary>
        /// <param name="cartTotalAfterDiscount">Tổng tiền hàng sau khi đã áp dụng mọi khuyến mãi</param>
        /// <returns>0đ nếu từ 500.000đ trở lên, ngược lại 30.000đ</returns>
        public static decimal CalculateStandardShippingFee(decimal cartTotalAfterDiscount)
        {
            if (cartTotalAfterDiscount <= 0) return 0m;
            return cartTotalAfterDiscount >= FreeShippingThreshold ? 0m : StandardShippingFee;
        }

        /// <summary>
        /// Tính toán số tiền còn thiếu để đạt mốc miễn phí vận chuyển 500.000 ₫
        /// </summary>
        public static decimal CalculateRemainingForFreeShipping(decimal cartTotalAfterDiscount)
        {
            return Math.Max(0m, FreeShippingThreshold - Math.Max(0m, cartTotalAfterDiscount));
        }

        /// <summary>
        /// Tính toán phần trăm tiến trình đạt mốc miễn phí vận chuyển (0% - 100%)
        /// </summary>
        public static decimal CalculateFreeShippingProgress(decimal cartTotalAfterDiscount)
        {
            if (cartTotalAfterDiscount <= 0m) return 0m;
            if (cartTotalAfterDiscount >= FreeShippingThreshold) return 100m;
            return Math.Round(cartTotalAfterDiscount / FreeShippingThreshold * 100m, 1);
        }

        /// <summary>
        /// Định dạng tiền tệ theo tiêu chuẩn Việt Nam Đồng (VD: "500.000 ₫")
        /// </summary>
        public static string FormatVnd(decimal amount)
        {
            return amount.ToString("N0", new CultureInfo("vi-VN")) + " ₫";
        }

        /// <summary>
        /// Định dạng hiển thị phí giao hàng ("Miễn phí" nếu bằng 0, hoặc "30.000 ₫")
        /// </summary>
        public static string FormatShippingFee(decimal fee)
        {
            return fee == 0m ? "Miễn phí" : FormatVnd(fee);
        }

        /// <summary>
        /// Định dạng hiển thị phí đóng gói quà tặng ("0 ₫" nếu bằng 0, hoặc "+50.000 ₫")
        /// </summary>
        public static string FormatGiftWrapFee(decimal fee)
        {
            return fee > 0m ? "+" + FormatVnd(fee) : "0 ₫";
        }
    }
}
