namespace Omnichannel.Application.Interfaces.Security
{
    public interface IPasswordHasherService
    {
        /// <summary>
        /// Băm mật khẩu dạng văn bản thuần sang chuỗi băm bảo mật kèm salt ngẫu nhiên
        /// </summary>
        // Chỉ cần đưa chữ "Admin@123", máy tự nhả ra chuỗi mã hóa bảo mật
        string HashPassword(string plainPassword);

        /// <summary>
        /// Xác thực mật khẩu nhập vào với chuỗi băm lưu trữ trong CSDL
        /// </summary>
        bool VerifyPassword(string hashedPassword, string providedPassword);

        /// <summary>
        /// Kiểm tra chính sách mật khẩu: Tối thiểu 8 ký tự, có chữ hoa + thường + số + ký tự đặc biệt
        /// </summary>
        (bool IsValid, string? ErrorMessage) ValidatePasswordPolicy(string password);
    }
}