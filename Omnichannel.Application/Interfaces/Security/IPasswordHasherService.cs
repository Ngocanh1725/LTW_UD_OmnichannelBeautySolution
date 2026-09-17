namespace Omnichannel.Application.Interfaces.Security
{
    public interface IPasswordHasherService
    {
        /// <summary>
        /// Băm mật khẩu dạng văn bản thuần sang chuỗi băm bảo mật kèm salt ngẫu nhiên
        /// </summary>
        string HashPassword(string plainPassword);

        /// <summary>
        /// Xác thực mật khẩu nhập vào với chuỗi băm lưu trữ trong CSDL
        /// </summary>
        bool VerifyPassword(string hashedPassword, string providedPassword);
    }
}