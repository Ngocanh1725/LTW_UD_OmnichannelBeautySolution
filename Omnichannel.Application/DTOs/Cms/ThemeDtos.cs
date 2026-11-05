namespace Omnichannel.Application.DTOs.Cms
{
    public class ThemeConfigDto
    {
        public int Id { get; set; }
        public string BrandName { get; set; } = "CANMAKE BEAUTY";
        public string? Slogan { get; set; }
        public string PrimaryColor { get; set; } = "#E8618C";
        public string SecondaryColor { get; set; } = "#B8336A";
        public string AccentColor { get; set; } = "#F7C6D4";
        public string FontFamily { get; set; } = "'Inter', sans-serif";
        public int? LogoMediaId { get; set; }
        public int? FaviconMediaId { get; set; }
        public string LogoUrl => LogoMediaId.HasValue ? $"/media/{LogoMediaId}" : "/images/branding/canmake-logo-horizontal.svg";
        public string FaviconUrl => FaviconMediaId.HasValue ? $"/media/{FaviconMediaId}" : "/images/branding/favicon-32x32.png";
        public string? AnnouncementBarText { get; set; }
        public bool AnnouncementBarActive { get; set; }
        public string AnnouncementBarBgColor { get; set; } = "#B8336A";
        public string? Hotline { get; set; }
        public string? ContactEmail { get; set; }
        public string? Address { get; set; }
        public string? FooterCopyright { get; set; }
        public string? CustomCss { get; set; }
    }
}
