using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Sales;

namespace Omnichannel.Application.Interfaces.Sales
{
    public interface IPosService
    {
        Task<List<PosProductDto>> SearchProductsAsync(string keyword, string? storeId = null, CancellationToken cancellationToken = default);
        Task<PosReceiptDto> CheckoutAsync(PosCheckoutDto dto, string cashierUserId, CancellationToken cancellationToken = default);
        Task<PosReceiptDto?> GetReceiptAsync(int orderId, CancellationToken cancellationToken = default);
        Task<PosReceiptDto?> GetReceiptByCodeAsync(string orderCode, CancellationToken cancellationToken = default);
    }
}