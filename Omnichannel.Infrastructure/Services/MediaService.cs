using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Media;
using Omnichannel.Application.Interfaces.Media;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services
{
    public class MediaService : IMediaService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<MediaService> _logger;

        private const long MaxFileSize = 10 * 1024 * 1024; // 10MB
        private const long MaxCacheableFileSize = 1 * 1024 * 1024; // 1MB

        public MediaService(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<MediaService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<MediaAssetDto> UploadAsync(MediaUploadRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                throw new ArgumentException("File rỗng hoặc không hợp lệ.", nameof(request.File));

            if (request.File.Length > MaxFileSize)
                throw new InvalidOperationException($"Dung lượng file vượt quá giới hạn cho phép ({MaxFileSize / (1024 * 1024)}MB).");

            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                await request.File.CopyToAsync(memoryStream);
                fileBytes = memoryStream.ToArray();
            }

            var detectedMime = ValidateAndDetectMimeType(fileBytes, request.File.FileName);
            var hash = ComputeSha256(fileBytes);

            // Kiểm tra chống trùng lặp dữ liệu nhị phân trong CSDL
            var existing = await _context.MediaAssets
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ChecksumHash == hash);

            if (existing != null)
            {
                _logger.LogInformation("Phát hiện ảnh trùng lặp SHA256 (ID: {Id}, File: {File}). Tái sử dụng bản ghi.", existing.Id, existing.FileName);
                return MapToDto(existing);
            }

            var entity = new MediaAsset
            {
                FileName = Path.GetFileName(request.File.FileName),
                ContentType = detectedMime,
                Data = fileBytes,
                FileSize = fileBytes.Length,
                AltText = string.IsNullOrWhiteSpace(request.AltText) ? Path.GetFileNameWithoutExtension(request.File.FileName) : request.AltText.Trim(),
                ChecksumHash = hash,
                Category = string.IsNullOrWhiteSpace(request.Category) ? "PRODUCT" : request.Category.ToUpperInvariant(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = request.UserId
            };

            _context.MediaAssets.Add(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Đã lưu ảnh mới vào CSDL nhị phân: {FileName} (ID: {Id}, Size: {Size} bytes)", entity.FileName, entity.Id, entity.FileSize);

            return MapToDto(entity);
        }

        public async Task<List<MediaAssetDto>> UploadBatchAsync(IEnumerable<IFormFile> files, string category, int? userId)
        {
            var results = new List<MediaAssetDto>();
            foreach (var file in files)
            {
                if (file.Length == 0) continue;
                var uploaded = await UploadAsync(new MediaUploadRequest
                {
                    File = file,
                    Category = category,
                    UserId = userId
                });
                results.Add(uploaded);
            }
            return results;
        }

        public async Task<MediaStreamResultDto?> GetMediaStreamAsync(int id)
        {
            var cacheKey = $"media_stream_{id}";
            if (_cache.TryGetValue(cacheKey, out MediaStreamResultDto? cachedDto) && cachedDto != null)
            {
                return cachedDto;
            }

            var media = await _context.MediaAssets
                .AsNoTracking()
                .Where(m => m.Id == id)
                .Select(m => new MediaStreamResultDto
                {
                    Data = m.Data,
                    ContentType = m.ContentType,
                    FileName = m.FileName,
                    ChecksumHash = m.ChecksumHash,
                    LastModified = m.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (media != null && media.Data.Length <= MaxCacheableFileSize)
            {
                _cache.Set(cacheKey, media, new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(30),
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
                });
            }

            return media;
        }

        public async Task<MediaStreamResultDto?> GetMediaStreamByHashAsync(string hash)
        {
            var cacheKey = $"media_stream_hash_{hash}";
            if (_cache.TryGetValue(cacheKey, out MediaStreamResultDto? cachedDto) && cachedDto != null)
            {
                return cachedDto;
            }

            var media = await _context.MediaAssets
                .AsNoTracking()
                .Where(m => m.ChecksumHash == hash)
                .Select(m => new MediaStreamResultDto
                {
                    Data = m.Data,
                    ContentType = m.ContentType,
                    FileName = m.FileName,
                    ChecksumHash = m.ChecksumHash,
                    LastModified = m.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (media != null && media.Data.Length <= MaxCacheableFileSize)
            {
                _cache.Set(cacheKey, media, TimeSpan.FromMinutes(30));
            }

            return media;
        }

        public async Task<(List<MediaAssetDto> Items, int TotalCount)> GetPagedListAsync(MediaFilterRequest filter)
        {
            var query = _context.MediaAssets.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Category) && !filter.Category.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                var cat = filter.Category.Trim().ToUpperInvariant();
                query = query.Where(m => m.Category == cat);
            }

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim();
                query = query.Where(m => m.FileName.Contains(kw) || (m.AltText != null && m.AltText.Contains(kw)));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((filter.PageIndex - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(m => new MediaAssetDto
                {
                    Id = m.Id,
                    FileName = m.FileName,
                    ContentType = m.ContentType,
                    FileSize = m.FileSize,
                    Width = m.Width,
                    Height = m.Height,
                    AltText = m.AltText,
                    Category = m.Category,
                    ChecksumHash = m.ChecksumHash,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.MediaAssets.FindAsync(id);
            if (entity == null) return false;

            _context.MediaAssets.Remove(entity);
            await _context.SaveChangesAsync();

            _cache.Remove($"media_stream_{id}");
            _cache.Remove($"media_stream_hash_{entity.ChecksumHash}");

            return true;
        }

        public async Task<bool> UpdateAltTextAsync(int id, string altText)
        {
            var entity = await _context.MediaAssets.FindAsync(id);
            if (entity == null) return false;

            entity.AltText = altText?.Trim();
            await _context.SaveChangesAsync();
            return true;
        }

        #region Helper Methods (Magic Bytes & SHA256)

        private static string ComputeSha256(byte[] data)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(data);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static string ValidateAndDetectMimeType(byte[] bytes, string originalFileName)
        {
            if (bytes == null || bytes.Length < 4)
                throw new InvalidOperationException("File dữ liệu không đủ kích thước để nhận diện định dạng ảnh.");

            // 1. JPEG (FF D8 FF)
            if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
                return "image/jpeg";

            // 2. PNG (89 50 4E 47)
            if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                return "image/png";

            // 3. GIF (47 49 46 38)
            if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x38)
                return "image/gif";

            // 4. WEBP (RIFF .... WEBP)
            if (bytes.Length >= 12 &&
                bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 && // "RIFF"
                bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)  // "WEBP"
                return "image/webp";

            // 5. SVG (Text containing <svg)
            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            if (extension == ".svg")
            {
                var previewLength = Math.Min(bytes.Length, 1024);
                var text = Encoding.UTF8.GetString(bytes, 0, previewLength);
                if (text.Contains("<svg", StringComparison.OrdinalIgnoreCase))
                    return "image/svg+xml";
            }

            // 6. ICO
            if (bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0x01 && bytes[3] == 0x00)
                return "image/x-icon";

            throw new InvalidOperationException($"Định dạng file không được hỗ trợ hoặc không phải là hình ảnh an toàn ({extension}).");
        }

        private static MediaAssetDto MapToDto(MediaAsset entity)
        {
            return new MediaAssetDto
            {
                Id = entity.Id,
                FileName = entity.FileName,
                ContentType = entity.ContentType,
                FileSize = entity.FileSize,
                Width = entity.Width,
                Height = entity.Height,
                AltText = entity.AltText,
                Category = entity.Category,
                ChecksumHash = entity.ChecksumHash,
                CreatedAt = entity.CreatedAt
            };
        }

        #endregion
    }
}
