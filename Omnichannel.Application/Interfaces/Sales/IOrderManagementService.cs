using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Customer;
using Omnichannel.Application.DTOs.Sales;
using Omnichannel.Domain.Entities;

namespace Omnichannel.Application.Interfaces.Sales
{
    public interface IOrderManagementService
    {
        Task<AdminOrderPagedListViewModel> GetOrdersPagedAsync(AdminOrderFilterRequest filter, CancellationToken cancellationToken = default);
        Task<AdminOrderDetailViewModel?> GetOrderDetailAsync(int id, CancellationToken cancellationToken = default);
        Task<(bool Success, string Message)> UpdateOrderStatusAsync(int id, string newStatus, string? note = null, string? reason = null, CancellationToken cancellationToken = default);
        Task<Invoice?> GetInvoiceForOrderAsync(int orderId, CancellationToken cancellationToken = default);

        // GAP-07 (Phiên C6): Minh bạch hậu mua hàng – Order Tracking Timeline, Hủy đơn & Đổi trả
        Task<(bool Allowed, string? ErrorMessage, CustomerOrderDetailViewModel? OrderDetail)> GetCustomerOrderDetailAsync(
            int orderId, string? orderCode, int? currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default);

        Task<GuestOrderLookupViewModel> LookupGuestOrderAsync(
            string orderCode, string phoneNumber, CancellationToken cancellationToken = default);

        Task<(bool Success, string Message)> CancelOrderByCustomerAsync(
            int orderId, int currentUserId, string reason, string? note = null, CancellationToken cancellationToken = default);

        Task<(bool Success, string Message, OrderReturnRequestViewModel? ReturnRequest)> SubmitReturnRequestAsync(
            int orderId, int currentUserId, string reason, string? description, List<string>? evidenceUrls = null, CancellationToken cancellationToken = default);

        Task<(bool Success, string Message)> ProcessReturnRequestAsync(
            int returnRequestId, bool isApproved, string? adminNote, int? adminUserId = null, CancellationToken cancellationToken = default);
    }
}
