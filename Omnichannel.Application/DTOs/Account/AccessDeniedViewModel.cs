namespace Omnichannel.Application.DTOs.Account
{
    public class AccessDeniedViewModel
    {
        public string? RequiredPermission { get; set; }
        public string? AttemptedPath { get; set; }
        public string Message { get; set; } = "Bạn không có quyền thực hiện thao tác hoặc truy cập vào trang này.";
    }
}