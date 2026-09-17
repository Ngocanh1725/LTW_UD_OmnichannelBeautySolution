using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Sales;

namespace Omnichannel.Application.Interfaces.Sales
{
    public interface IPosService
    {
        Task<List<PosProductSearchDto>> SearchProductsAsync(string storeId, string keyword, CancellationToken cancellationToken = default);
        Task<PosProductSearchDto?> GetProductByBarcodeAsync(string storeId, string barcode, CancellationToken cancellationToken = default);
        Task<PosCheckoutResultDto> ProcessPosCheckoutAsync(PosCheckoutRequest request, string cashierUserId, CancellationToken cancellationToken = default);
        Task<PosReceiptPrintViewModel?> GetReceiptForPrintAsync(string orderId, CancellationToken cancellationToken = default);
    }
}
