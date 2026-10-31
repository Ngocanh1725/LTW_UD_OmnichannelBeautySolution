using System;
using Microsoft.AspNetCore.Http;

namespace Omnichannel.Application.DTOs.Media
{
    public class MediaAssetDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string? AltText { get; set; }
        public string Category { get; set; } = string.Empty;
        public string ChecksumHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Url => $"/media/{Id}";
    }

    public class MediaStreamResultDto
    {
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ChecksumHash { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
    }

    public class MediaUploadRequest
    {
        public IFormFile File { get; set; } = null!;
        public string Category { get; set; } = "PRODUCT";
        public string? AltText { get; set; }
        public int? UserId { get; set; }
    }

    public class MediaFilterRequest
    {
        public string? Keyword { get; set; }
        public string? Category { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 24;
    }
}
