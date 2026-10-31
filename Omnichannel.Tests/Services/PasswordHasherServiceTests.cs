using FluentAssertions;
using Omnichannel.Infrastructure.Security;
using Xunit;

namespace Omnichannel.Tests.Services
{
    public class PasswordHasherServiceTests
    {
        [Fact]
        public void VerifyPassword_Admin123_WithSeededHash_ReturnsTrue()
        {
            var service = new PasswordHasherService();
            const string seedHash = "AQAAAAIAAYagAAAAEH8l8xGgjNDnkhL2Bw3b+scuEfAheJlogt27NtNomSM8/JI6LSwL9JWUUOgyZ50kqQ==";

            bool isValid = service.VerifyPassword(seedHash, "Admin@123");

            isValid.Should().BeTrue();
        }

        [Fact]
        public void VerifyPassword_Admin123456_WithLegacyAdminHash_ReturnsTrue()
        {
            var service = new PasswordHasherService();
            const string legacyAdminHash = "AQAAAAIAAYagAAAAEGlCLw5GtMCnc3V0yYzrEOTXVWDGJx96YiVytx68Hq0BBgzpW1hMYP6rtwkDUKTkmQ==";

            bool isValid = service.VerifyPassword(legacyAdminHash, "Admin@123456");

            isValid.Should().BeTrue();
        }

        [Fact]
        public void HashPassword_NewHash_VerifiesSuccessfully()
        {
            var service = new PasswordHasherService();
            string hash = service.HashPassword("Admin@123456");

            bool isValid = service.VerifyPassword(hash, "Admin@123456");

            isValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("Admin@123", true)]
        [InlineData("Beauty#2026Secure", true)]
        [InlineData("P@ssw0rd", true)]
        [InlineData("short", false)] // < 8 chars
        [InlineData("alllowercase123!", false)] // No uppercase
        [InlineData("ALLUPPERCASE123!", false)] // No lowercase
        [InlineData("NoDigitsHere!@@", false)] // No digits
        [InlineData("NoSpecialChar123", false)] // No special char
        [InlineData("", false)] // Empty
        public void ValidatePasswordPolicy_ValidatesCorrectly(string password, bool expectedValid)
        {
            var service = new PasswordHasherService();

            var (isValid, errorMessage) = service.ValidatePasswordPolicy(password);

            isValid.Should().Be(expectedValid);
            if (!expectedValid)
            {
                errorMessage.Should().NotBeNullOrWhiteSpace();
            }
            else
            {
                errorMessage.Should().BeNull();
            }
        }
    }
}
