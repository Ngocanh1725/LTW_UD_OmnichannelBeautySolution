using System;
using System.ComponentModel.DataAnnotations;

namespace Omnichannel.Application.DTOs.Security
{
    public class StepUpRequestDto
    {
        [Required(ErrorMessage = "Vui lòng nhập tài khoản hoặc quét mã QR Quản lý.")]
        public string ManagerUsernameOrQr { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mã PIN hoặc mật khẩu Quản lý.")]
        public string PinCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã hành động cần phê duyệt là bắt buộc.")]
        public string ActionCode { get; set; } = string.Empty; // POS_ORDER_CANCEL, POS_DRAWER_NO_SALE, POS_MANUAL_DISCOUNT

        [Required(ErrorMessage = "Định danh đối tượng thao tác là bắt buộc.")]
        public string TargetEntityId { get; set; } = string.Empty; // OrderCode, ShiftId, DrawerId

        [MaxLength(250)]
        public string? Reason { get; set; }
    }

    public record StepUpTokenPayload(
        int CashierUserId,
        int ApproverManagerId,
        string ApproverFullName,
        int StoreId,
        string ActionCode,
        string TargetEntityId,
        string? Reason,
        string Nonce,
        DateTimeOffset ExpiresAtUtc);

    public class StepUpResultDto
    {
        public bool Success { get; set; }
        public string? ElevationToken { get; set; }
        public string? ApproverFullName { get; set; }
        public string? Message { get; set; }
    }
}
