using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Settings;

namespace Omnichannel.Application.Interfaces.Settings
{
    public interface IDemoResetService
    {
        /// <summary>
        /// Khôi phục toàn bộ dữ liệu demo về trạng thái chuẩn trước giờ thuyết trình / diễn tập.
        /// Xóa các đơn hàng diễn tập phát sinh, làm sạch giỏ hàng tạm, hoàn nguyên tồn kho các lô demo.
        /// </summary>
        Task<DemoResetResultDto> ResetDemoDataAsync(int? performedByUserId, string? userName = null, CancellationToken cancellationToken = default);
    }
}

