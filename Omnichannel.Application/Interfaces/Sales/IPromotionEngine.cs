using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Sales;

namespace Omnichannel.Application.Interfaces.Sales
{
    public interface IPromotionEngine
    {
        /// <summary>
        /// Đánh giá giỏ hàng để tìm chương trình khuyến mãi tốt nhất (Flash Sale, Chiết khấu %, Giảm tiền, Mua X tặng Y, Quà tặng GWP)
        /// và áp dụng mã Coupon nếu có (quy tắc không chồng chéo: tối đa 1 CTKM tự động + 1 Coupon).
        /// </summary>
        Task<PromotionResultDto> EvaluateCartAsync(
            List<CartItemDto> items, 
            string? couponCode = null, 
            int? userId = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Kiểm tra tính hợp lệ của mã coupon đối với người dùng và tổng giá trị đơn hàng
        /// </summary>
        Task<bool> ValidateCouponAsync(
            string couponCode, 
            int? userId, 
            decimal orderTotal, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Kiểm tra điều kiện ngặt nghèo của mã coupon và trả về chi tiết lý do (ngày hiệu lực, min order, giới hạn lượt dùng)
        /// </summary>
        Task<PromotionValidationResultDto> ValidateCouponDetailAsync(
            string couponCode, 
            int? userId, 
            decimal orderTotal, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Ghi nhận lịch sử sử dụng Coupon và tăng số lần đã dùng khi đơn hàng được tạo/chốt thành công
        /// </summary>
        Task ApplyCouponUsageAsync(
            string couponCode, 
            int? userId, 
            int orderId, 
            decimal discount, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy danh sách các chương trình khuyến mãi đang có hiệu lực
        /// </summary>
        Task<List<PromotionDto>> GetActivePromotionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Tra cứu thông tin chi tiết của Coupon theo mã
        /// </summary>
        Task<CouponDto?> GetCouponByCodeAsync(string couponCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tạo mới chương trình khuyến mãi (dành cho Admin)
        /// </summary>
        Task<PromotionDto> CreatePromotionAsync(CreatePromotionDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tạo mới mã coupon giảm giá (dành cho Admin)
        /// </summary>
        Task<CouponDto> CreateCouponAsync(CreateCouponDto dto, CancellationToken cancellationToken = default);
    }
}
