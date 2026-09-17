using System;

namespace Omnichannel.Domain.Entities
{
    public class Banner
    {
        public int BannerId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Position { get; set; } // 1: HeroSlider, 2: PopupPromo, 3: CategoryTop, 4: FooterStrip
        public string DesktopImageUrl { get; set; } = string.Empty;
        public string? MobileImageUrl { get; set; }
        public string? LinkUrl { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public int TrackingClickCount { get; set; }
        public bool IsActive { get; set; }
    }
}