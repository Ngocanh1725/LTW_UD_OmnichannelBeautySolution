using Microsoft.AspNetCore.Mvc;
using Omnichannel.Application.Interfaces.Sales;
using System.Threading.Tasks;

namespace Omnichannel.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationController : ControllerBase
    {
        private readonly IShippingService _shippingService;

        public LocationController(IShippingService shippingService)
        {
            _shippingService = shippingService;
        }

        [HttpGet("provinces")]
        public async Task<IActionResult> GetProvinces()
        {
            var provinces = await _shippingService.GetProvincesAsync();
            return Ok(provinces);
        }

        [HttpGet("districts/{provinceId}")]
        public async Task<IActionResult> GetDistricts(string provinceId)
        {
            var districts = await _shippingService.GetDistrictsAsync(provinceId);
            return Ok(districts);
        }

        [HttpGet("wards/{districtId}")]
        public async Task<IActionResult> GetWards(string districtId)
        {
            var wards = await _shippingService.GetWardsAsync(districtId);
            return Ok(wards);
        }

        [HttpGet("fee")]
        public async Task<IActionResult> CalculateFee(string districtId, string wardCode)
        {
            var fee = await _shippingService.CalculateShippingFeeAsync(districtId, wardCode);
            return Ok(new { fee });
        }
    }
}
