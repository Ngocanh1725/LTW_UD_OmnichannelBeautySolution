using System.Collections.Generic;
using System.Threading.Tasks;
using Omnichannel.Domain.Entities;

namespace Omnichannel.Application.Interfaces.Sales
{
    public interface IShippingService
    {
        Task<decimal> CalculateShippingFeeAsync(string toDistrictId, string toWardCode, int weightGram = 500);
        Task<IEnumerable<dynamic>> GetProvincesAsync();
        Task<IEnumerable<dynamic>> GetDistrictsAsync(string provinceId);
        Task<IEnumerable<dynamic>> GetWardsAsync(string districtId);
        Task<(bool Success, string TrackingCode)> PushOrderToGhnAsync(string orderCode, string toName, string toPhone, string address, string districtId, string wardCode, int weightGram = 500);
        Task<(bool Success, string TrackingCode, string? PrintUrl, string Message)> CreateShippingOrderAsync(Order order, int weightGram = 500);
        Task<string> GetPrintOrderUrlAsync(string trackingCode);
    }
}
