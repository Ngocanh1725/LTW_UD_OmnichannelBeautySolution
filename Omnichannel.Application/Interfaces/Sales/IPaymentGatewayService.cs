using Omnichannel.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnichannel.Application.Interfaces.Sales
{
    public interface IPaymentGatewayService
    {
        string CreatePaymentUrl(Order order, string ipAddress);
        Task<(bool Success, string OrderCode, decimal Amount)> ProcessCallbackAsync(IDictionary<string, string> queryData);
    }
}
