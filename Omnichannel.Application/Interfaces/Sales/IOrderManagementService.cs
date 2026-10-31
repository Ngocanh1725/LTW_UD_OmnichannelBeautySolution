using System.Threading;
using System.Threading.Tasks;
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
    }
}
