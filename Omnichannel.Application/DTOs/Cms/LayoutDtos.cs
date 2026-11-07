using System;
using System.Collections.Generic;

namespace Omnichannel.Application.DTOs.Cms
{
    public class PageLayoutDto
    {
        public int Id { get; set; }
        public string PageKey { get; set; } = "home_page";
        public string Title { get; set; } = string.Empty;
        public string? MetaDescription { get; set; }
        public bool IsPublished { get; set; } = true;
        public List<LayoutSectionDto> Sections { get; set; } = new();
        public ThemeTokensDto Theme { get; set; } = new();
    }

    public class LayoutSectionDto
    {
        public int Id { get; set; }
        public int PageLayoutId { get; set; }
        public string SectionKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public bool IsVisible { get; set; } = true;
        public string? CustomStylesJson { get; set; }
        public List<SectionItemDto> Items { get; set; } = new();
    }

    public class SectionItemDto
    {
        public int Id { get; set; }
        public int LayoutSectionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string? TargetUrl { get; set; }
        public int? MediaAssetId { get; set; }
        public string? ImageUrl { get; set; }
        public int OrderIndex { get; set; }
        public string? PayloadJson { get; set; }
    }

    public class ThemeTokensDto
    {
        public string PrimaryColor { get; set; } = "#E11D48";
        public string SecondaryColor { get; set; } = "#BE185D";
        public string AccentColor { get; set; } = "#F59E0B";
        public string FontHeading { get; set; } = "'Playfair Display', serif";
        public string FontBody { get; set; } = "'Inter', sans-serif";
    }

    public class SaveDraftLayoutRequest
    {
        public string PageKey { get; set; } = "home_page";
        public List<LayoutSectionDto> Sections { get; set; } = new();
        public ThemeTokensDto? Theme { get; set; }
    }

    public class PublishLayoutResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    }
}

