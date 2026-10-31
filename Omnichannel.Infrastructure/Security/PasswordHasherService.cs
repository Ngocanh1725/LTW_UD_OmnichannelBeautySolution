using Microsoft.AspNetCore.Identity;
using Omnichannel.Application.Interfaces.Security;

namespace Omnichannel.Infrastructure.Security
{
    public class PasswordHasherService : IPasswordHasherService
    {
        private readonly PasswordHasher<string> _hasher;
        private const string DummyUser = "OmnichannelSystemUser";

        public PasswordHasherService()
        {
            _hasher = new PasswordHasher<string>();
        }

        public string HashPassword(string plainPassword)
        {
            if (string.IsNullOrWhiteSpace(plainPassword))
                throw new ArgumentException("Mật khẩu không được để trống.", nameof(plainPassword));

            return _hasher.HashPassword(DummyUser, plainPassword);
        }

        public bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword) || string.IsNullOrWhiteSpace(providedPassword))
                return false;

            var result = _hasher.VerifyHashedPassword(DummyUser, hashedPassword, providedPassword);
            return result == PasswordVerificationResult.Success ||
                   result == PasswordVerificationResult.SuccessRehashNeeded;
        }

        public (bool IsValid, string? ErrorMessage) ValidatePasswordPolicy(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return (false, "Mật khẩu không được để trống.");
            }

            if (password.Length < 8)
            {
                return (false, "Mật khẩu phải có độ dài tối thiểu 8 ký tự.");
            }

            if (!password.Any(char.IsUpper))
            {
                return (false, "Mật khẩu phải chứa ít nhất 1 chữ cái in hoa (A-Z).");
            }

            if (!password.Any(char.IsLower))
            {
                return (false, "Mật khẩu phải chứa ít nhất 1 chữ cái in thường (a-z).");
            }

            if (!password.Any(char.IsDigit))
            {
                return (false, "Mật khẩu phải chứa ít nhất 1 chữ số (0-9).");
            }

            if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            {
                return (false, "Mật khẩu phải chứa ít nhất 1 ký tự đặc biệt (!@#$%^&*...).");
            }

            return (true, null);
        }
    }
}
