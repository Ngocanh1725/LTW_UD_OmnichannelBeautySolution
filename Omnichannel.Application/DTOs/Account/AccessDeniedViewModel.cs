namespace Omnichannel.Application.DTOs.Account
{
    public class AccessDeniedViewModel
    {
        public string? RequiredPermission { get; set; }
        public string? AttemptedPath { get; set; }
        public string Message { get; set; } = "Bạn chưa được cấp quyền chức năng này.";
        public string? ContactPersonName { get; set; }
        public string? ContactPersonTitle { get; set; }
        public string? ContactPersonEmail { get; set; }
        public string? ActorRoleName { get; set; }
        public string? ActorDepartmentName { get; set; }
    }
}
