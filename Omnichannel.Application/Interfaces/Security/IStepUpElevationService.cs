using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Security;

namespace Omnichannel.Application.Interfaces.Security
{
    public interface IStepUpElevationService
    {
        /// <summary>
        /// Xác thực thông tin Quản lý (Mã PIN / QR thẻ) và cấp Elevation Token có chữ ký số (hiệu lực 60 giây)
        /// </summary>
        Task<StepUpResultDto> IssueElevationTokenAsync(
            int cashierUserId,
            int cashierStoreId,
            StepUpRequestDto request,
            CancellationToken ct = default);

        /// <summary>
        /// Xác thực chữ ký số, kiểm tra thời hạn và tiêu hủy Nonce (chống Replay Attack), đồng thời ghi vết AuditLog kép
        /// </summary>
        Task<StepUpTokenPayload> ValidateAndConsumeTokenAsync(
            string token,
            int currentCashierUserId,
            string requiredActionCode,
            string targetEntityId,
            CancellationToken ct = default);
    }
}
