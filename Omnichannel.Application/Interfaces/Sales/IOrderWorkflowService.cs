using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Sales;

namespace Omnichannel.Application.Interfaces.Sales
{
    public interface IOrderWorkflowService
    {
        /// <summary>
        /// PHA 1: Khóa tạm tồn kho qua FEFO (Soft Reservation), tạo đơn hàng và sinh mã VietQR hoặc Vé Click &amp; Collect
        /// </summary>
        Task<(bool Success, string Message, OrderSuccessViewModel? Result)> CreateOrderWithSoftReserveAsync(
            string cartId, CheckoutViewModel model, string? customerUserId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// PHA 2: Chốt cứng (Hard Deduction) khi nhận Webhook thanh toán thành công hoặc Thu ngân xác nhận
        /// </summary>
        Task<(bool Success, string Message)> ConfirmPaymentAndDeductPhysicalStockAsync(
            BankCallbackDto callback, CancellationToken cancellationToken = default);

        /// <summary>
        /// Hủy đơn hàng và hoàn trả số lượng tạm khóa (ReservedQuantity) vào kho
        /// </summary>
        Task<bool> CancelOrderAndReleaseReserveAsync(string orderId, string reason, CancellationToken cancellationToken = default);

        /// <summary>
        /// PHIÊN D1 (O2O Click &amp; Collect): Tính danh sách các Showroom và kiểm tra cửa hàng nào CÒN ĐỦ HÀNG cho toàn bộ giỏ theo lô FEFO
        /// </summary>
        Task<List<StorePickupCandidateDto>> GetStorePickupAvailabilityForCartAsync(
            string cartId, string? customerUserId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// PHIÊN D1 (O2O Click &amp; Collect): Thu ngân tại POS xác thực Mã PIN 6 số và bàn giao hàng (Store Isolation, chỉ trừ tồn kho vật lý 1 lần, ghi vào ca POS, cộng điểm Loyalty)
        /// </summary>
        Task<(bool Success, string Message, OrderSuccessViewModel? HandedOverOrder)> HandoverPickupOrderAtPosAsync(
            HandoverPickupOrderRequestDto request, CancellationToken cancellationToken = default);

        /// <summary>
        /// PHIÊN D1 (O2O Click &amp; Collect): Tự động hủy giữ hàng và hoàn tồn kho FEFO cho các đơn Click &amp; Collect quá 48h chưa đến nhận
        /// </summary>
        Task<int> ReleaseExpiredPickupOrdersAsync(CancellationToken cancellationToken = default);
    }
}
