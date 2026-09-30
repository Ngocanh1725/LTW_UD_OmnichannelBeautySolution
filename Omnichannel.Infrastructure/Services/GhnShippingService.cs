using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Omnichannel.Application.Interfaces.Sales;

namespace Omnichannel.Infrastructure.Services
{
    public class GhnShippingService : IShippingService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public GhnShippingService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<IEnumerable<dynamic>> GetProvincesAsync()
        {
            // MOCK DATA để mô phỏng danh sách Tỉnh/Thành từ GHN
            var provinces = new List<dynamic>
            {
                new { ProvinceID = 201, ProvinceName = "Hà Nội" },
                new { ProvinceID = 202, ProvinceName = "Hồ Chí Minh" },
                new { ProvinceID = 203, ProvinceName = "Đà Nẵng" }
            };
            return await Task.FromResult(provinces);
        }

        public async Task<IEnumerable<dynamic>> GetDistrictsAsync(string provinceId)
        {
            // MOCK DATA để mô phỏng danh sách Quận/Huyện từ GHN
            var districts = new List<dynamic>();
            if (provinceId == "201") // Hà Nội
            {
                districts.Add(new { DistrictID = 1482, DistrictName = "Quận Hoàn Kiếm" });
                districts.Add(new { DistrictID = 1483, DistrictName = "Quận Ba Đình" });
            }
            else if (provinceId == "202") // HCM
            {
                districts.Add(new { DistrictID = 1442, DistrictName = "Quận 1" });
                districts.Add(new { DistrictID = 1443, DistrictName = "Quận 3" });
            }
            return await Task.FromResult(districts);
        }

        public async Task<IEnumerable<dynamic>> GetWardsAsync(string districtId)
        {
            // MOCK DATA
            var wards = new List<dynamic>
            {
                new { WardCode = "1A01", WardName = "Phường 1" },
                new { WardCode = "1A02", WardName = "Phường 2" }
            };
            return await Task.FromResult(wards);
        }

        public async Task<decimal> CalculateShippingFeeAsync(string toDistrictId, string toWardCode, int weightGram = 500)
        {
            // MOCK logic tính phí ship.
            // Trong thực tế sẽ gọi: POST https://dev-online-gateway.ghn.vn/shiip/public-api/v2/shipping-order/fee
            if (string.IsNullOrEmpty(toDistrictId)) return 0;
            
            if (toDistrictId == "1482" || toDistrictId == "1483")
                return await Task.FromResult(25000m);
            if (toDistrictId == "1442" || toDistrictId == "1443")
                return await Task.FromResult(22000m);
                
            return await Task.FromResult(35000m);
        }

        public async Task<(bool Success, string TrackingCode)> PushOrderToGhnAsync(string orderCode, string toName, string toPhone, string address, string districtId, string wardCode, int weightGram = 500)
        {
            // Mock logic đẩy đơn GHN thành công
            string mockTracking = "GHN" + System.DateTime.Now.Ticks.ToString().Substring(0, 8);
            return await Task.FromResult((true, mockTracking));
        }
    }
}
