using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Omnichannel.Domain.Entities
{
    public partial class ThemeConfig
    {
        [MaxLength(100)]
        public string ThemeName { get; set; } = "Rose Gold Beauty";

        [MaxLength(100)]
        public string FontHeading { get; set; } = "'Playfair Display', serif";

        [MaxLength(100)]
        public string FontBody { get; set; } = "'Inter', sans-serif";

        public int? LogoMediaAssetId { get; set; }
        public int? FaviconMediaAssetId { get; set; }

        public virtual MediaAsset? LogoMediaAsset { get; set; }
        public virtual MediaAsset? FaviconMediaAsset { get; set; }
    }
}

