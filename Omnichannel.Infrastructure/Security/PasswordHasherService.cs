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
    }
}
