namespace Omnichannel.Application.DTOs.Cms
{
    public class ThemeConfigDto
    {
        public int Id { get; set; }
        public string BrandName { get; set; } = "OMNI BEAUTY";
        public string? Slogan { get; set; }
        public string PrimaryColor { get; set; } = "#E11D48";
        public string SecondaryColor { get; set; } = "#BE185D";
        public string AccentColor { get; set; } = "#F59E0B";
        public string FontFamily { get; set; } = "'Inter', sans-serif";
        public int? LogoMediaId { get; set; }
        public int? FaviconMediaId { get; set; }
        public string LogoUrl => LogoMediaId.HasValue ? $"/media/{LogoMediaId}" : "/images/branding/logo-full.jpg";
        public string FaviconUrl => FaviconMediaId.HasValue ? $"/media/{FaviconMediaId}" : "/images/branding/favicon-32x32.png";
        public string? AnnouncementBarText { get; set; }
        public bool AnnouncementBarActive { get; set; }
        public string AnnouncementBarBgColor { get; set; } = "#881337";
        public string? Hotline { get; set; }
        public string? ContactEmail { get; set; }
        public string? Address { get; set; }
        public string? FooterCopyright { get; set; }
        public string? CustomCss { get; set; }
    }
}
