using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services.Cms
{
    public class MediaStorageService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<MediaStorageService> _logger;

        public MediaStorageService(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            ILogger<MediaStorageService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<MediaAsset> SaveUploadedFileAsync(IFormFile file, int? staffId = null, string? category = null)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File rỗng hoặc không tồn tại.", nameof(file));
            }

            // Tạo thư mục wwwroot/uploads/cms hoặc uploads/banners nếu là category BANNER
            string subFolder = (!string.IsNullOrWhiteSpace(category) && category.Equals("BANNER", StringComparison.OrdinalIgnoreCase)) ? "banners" : "cms";
            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", subFolder);
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Đọc bytes & tính SHA256 checksum
            byte[] fileBytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                fileBytes = ms.ToArray();
            }

            var hash = Convert.ToHexString(SHA256.HashData(fileBytes)).ToLowerInvariant();

            // Kiểm tra trùng lặp
            var existing = await _context.MediaAssets.FirstOrDefaultAsync(m => m.ChecksumHash == hash);
            if (existing != null)
            {
                return existing;
            }

            // Lưu file vật lý
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
            var physicalPath = Path.Combine(uploadsFolder, uniqueFileName);
            await File.WriteAllBytesAsync(physicalPath, fileBytes);

            var publicUrl = $"/uploads/{subFolder}/{uniqueFileName}";
            var storagePath = publicUrl;

            var mediaAsset = new MediaAsset
            {
                AssetUid = Guid.NewGuid(),
                FileName = Path.GetFileName(file.FileName),
                ContentType = !string.IsNullOrWhiteSpace(file.ContentType) ? file.ContentType : "image/jpeg",
                MimeType = file.ContentType,
                Data = fileBytes,
                FileSize = file.Length,
                FileSizeBytes = file.Length,
                ChecksumHash = hash,
                Category = !string.IsNullOrWhiteSpace(category) ? category.ToUpperInvariant() : "CMS",
                StoragePath = storagePath,
                PublicUrl = publicUrl,
                UploadedByStaffId = staffId,
                CreatedAt = DateTime.UtcNow
            };

            _context.MediaAssets.Add(mediaAsset);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Đã lưu MediaAsset mới {Id}: {FileName} ({PublicUrl})", mediaAsset.Id, mediaAsset.FileName, publicUrl);
            return mediaAsset;
        }

        public async Task<(List<MediaAsset> Items, int TotalCount)> GetPagedMediaAsync(string? search = null, string? category = null, int page = 1, int pageSize = 24)
        {
            var query = _context.MediaAssets.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(m => m.FileName.Contains(search) || (m.AltText != null && m.AltText.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(m => m.Category == category);
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MediaAsset
                {
                    Id = m.Id,
                    FileName = m.FileName,
                    Category = m.Category,
                    FileSize = m.FileSize > 0 ? m.FileSize : (m.FileSizeBytes ?? 0L),
                    PublicUrl = !string.IsNullOrEmpty(m.PublicUrl) ? m.PublicUrl : $"/media/{m.Id}",
                    AltText = m.AltText,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync();

            return (items, total);
        }
    }
}

