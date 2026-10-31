using System.ComponentModel.DataAnnotations;

namespace Omnichannel.Application.DTOs.Account
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Họ và tên.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ và tên từ 2 đến 100 ký tự.")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập Số điện thoại.")]
        [RegularExpression(@"^(03|05|07|08|09)[0-9]{8}$", ErrorMessage = "Số điện thoại Việt Nam không hợp lệ (gồm 10 số, bắt đầu bằng 03, 05, 07, 08, 09).")]
        [Display(Name = "Số điện thoại (Mã tích điểm)")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập Email.")]
        [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ.")]
        [Display(Name = "Địa chỉ Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập Mật khẩu.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự, bao gồm chữ hoa, chữ thường và chữ số.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận Mật khẩu.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Range(typeof(bool), "true", "true", ErrorMessage = "Bạn cần đồng ý với Điều khoản dịch vụ và Chính sách bảo mật.")]
        public bool AcceptTerms { get; set; } = true;
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Email đã đăng ký.")]
        [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ.")]
        [Display(Name = "Email tài khoản")]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập Mã xác thực (OTP / Token).")]
        [Display(Name = "Mã xác thực OTP")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập Mật khẩu mới.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu mới tối thiểu 8 ký tự.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [Display(Name = "Xác nhận mật khẩu mới")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
