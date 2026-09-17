using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Sales;

namespace Omnichannel.Application.Interfaces.Sales
{
    public interface IOrderWorkflowService
    {
        /// <summary>
        /// PHA 1: Khóa tạm tồn kho qua FEFO (Soft Reservation), tạo đơn hàng và sinh mã VietQR
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
    }
}