using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Settings;
using Omnichannel.Application.Interfaces.Settings;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<SettingsService> _logger;

        private const string CacheKeyPrefix = "AppSetting:";
        private const string CacheGroupPrefix = "AppSettingGroup:";
        private static readonly TimeSpan DefaultCacheTtl = TimeSpan.FromHours(12);

        // Khóa mã hóa bảo vệ cấu hình nhạy cảm (32 bytes AES-256)
        private static readonly byte[] EncryptionKey = SHA256.HashData(Encoding.UTF8.GetBytes("Omnichannel@Beauty#Sec2026!MasterConfigKey"));
        private static readonly byte[] EncryptionIv = MD5.HashData(Encoding.UTF8.GetBytes("Omnichannel#IV16"));

        public SettingsService(
            ApplicationDbContext context,
            IMemoryCache memoryCache,
            ILogger<SettingsService> logger)
        {
            _context = context;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key, T? defaultValue = default, CancellationToken cancellationToken = default)
        {
            var rawValue = await GetValueAsync(key, null, cancellationToken);
            if (string.IsNullOrEmpty(rawValue))
            {
                return defaultValue;
            }

            try
            {
                var targetType = typeof(T);
                var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

                if (underlyingType == typeof(string))
                {
                    return (T)(object)rawValue;
                }

                if (underlyingType == typeof(bool))
                {
                    bool isTrue = rawValue.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                  rawValue.Equals("1") ||
                                  rawValue.Equals("yes", StringComparison.OrdinalIgnoreCase);
                    return (T)(object)isTrue;
                }

                if (underlyingType == typeof(int))
                {
                    if (int.TryParse(rawValue, out int intVal)) return (T)(object)intVal;
                    return defaultValue;
                }

                if (underlyingType == typeof(long))
                {
                    if (long.TryParse(rawValue, out long longVal)) return (T)(object)longVal;
                    return defaultValue;
                }

                if (underlyingType == typeof(decimal))
                {
                    if (decimal.TryParse(rawValue, out decimal decVal)) return (T)(object)decVal;
                    return defaultValue;
                }

                if (underlyingType == typeof(double))
                {
                    if (double.TryParse(rawValue, out double dblVal)) return (T)(object)dblVal;
                    return defaultValue;
                }

                // Hỗ trợ kiểu đối tượng JSON
                return JsonSerializer.Deserialize<T>(rawValue);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể chuyển đổi cấu hình '{Key}' có giá trị '{Value}' sang kiểu {Type}. Sử dụng giá trị mặc định.",
                    key, rawValue, typeof(T).Name);
                return defaultValue;
            }
        }

        public async Task<string?> GetValueAsync(string key, string? defaultValue = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key)) return defaultValue;

            string normalizedKey = key.Trim().ToUpperInvariant();
            string cacheKey = $"{CacheKeyPrefix}{normalizedKey}";

            if (_memoryCache.TryGetValue(cacheKey, out string? cachedVal) && cachedVal != null)
            {
                return cachedVal;
            }

            var setting = await _context.Settings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SettingKey == normalizedKey, cancellationToken);

            if (setting == null)
            {
                return defaultValue;
            }

            string resultValue = setting.SettingValue;
            if (setting.IsEncrypted && !string.IsNullOrEmpty(resultValue))
            {
                resultValue = Decrypt(resultValue);
            }

            _memoryCache.Set(cacheKey, resultValue, DefaultCacheTtl);
            return resultValue;
        }

        public async Task<bool> SetAsync(string key, string value, string? group = null, string? dataType = null, string? description = null, bool isEncrypted = false, int? userId = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;

            string normalizedKey = key.Trim().ToUpperInvariant();
            var setting = await _context.Settings
                .FirstOrDefaultAsync(s => s.SettingKey == normalizedKey, cancellationToken);

            string storeValue = value;
            if (isEncrypted && !string.IsNullOrEmpty(value))
            {
                storeValue = Encrypt(value);
            }

            if (setting == null)
            {
                setting = new Setting
                {
                    SettingKey = normalizedKey,
                    SettingValue = storeValue,
                    SettingGroup = !string.IsNullOrWhiteSpace(group) ? group.Trim().ToUpperInvariant() : "GENERAL",
                    DataType = !string.IsNullOrWhiteSpace(dataType) ? dataType.Trim().ToUpperInvariant() : "STRING",
                    Description = description?.Trim(),
                    IsEncrypted = isEncrypted,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = userId
                };
                await _context.Settings.AddAsync(setting, cancellationToken);
            }
            else
            {
                setting.SettingValue = storeValue;
                if (!string.IsNullOrWhiteSpace(group)) setting.SettingGroup = group.Trim().ToUpperInvariant();
                if (!string.IsNullOrWhiteSpace(dataType)) setting.DataType = dataType.Trim().ToUpperInvariant();
                if (description != null) setting.Description = description.Trim();
                setting.IsEncrypted = isEncrypted;
                setting.UpdatedAt = DateTime.UtcNow;
                setting.UpdatedBy = userId;
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Invalidate Cache
            await InvalidateCacheAsync(normalizedKey, cancellationToken);
            if (!string.IsNullOrEmpty(setting.SettingGroup))
            {
                _memoryCache.Remove($"{CacheGroupPrefix}{setting.SettingGroup}");
            }

            return true;
        }

        public async Task<Dictionary<string, string>> GetGroupAsync(string group, CancellationToken cancellationToken = default)
        {
            string normalizedGroup = group.Trim().ToUpperInvariant();
            string cacheKey = $"{CacheGroupPrefix}{normalizedGroup}";

            if (_memoryCache.TryGetValue(cacheKey, out Dictionary<string, string>? cachedDict) && cachedDict != null)
            {
                return cachedDict;
            }

            var settings = await _context.Settings
                .AsNoTracking()
                .Where(s => s.SettingGroup == normalizedGroup)
                .ToListAsync(cancellationToken);

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in settings)
            {
                string val = s.SettingValue;
                if (s.IsEncrypted && !string.IsNullOrEmpty(val))
                {
                    val = Decrypt(val);
                }
                dict[s.SettingKey] = val;
            }

            _memoryCache.Set(cacheKey, dict, DefaultCacheTtl);
            return dict;
        }

        public async Task<List<SettingDto>> GetGroupSettingsAsync(string group, CancellationToken cancellationToken = default)
        {
            string normalizedGroup = group.Trim().ToUpperInvariant();

            var settings = await _context.Settings
                .AsNoTracking()
                .Include(s => s.UpdatedByUser)
                .Where(s => s.SettingGroup == normalizedGroup)
                .OrderBy(s => s.SettingKey)
                .ToListAsync(cancellationToken);

            return settings.Select(s => new SettingDto
            {
                Id = s.Id,
                SettingKey = s.SettingKey,
                SettingValue = s.IsEncrypted ? Decrypt(s.SettingValue) : s.SettingValue,
                SettingGroup = s.SettingGroup,
                Description = s.Description,
                DataType = s.DataType,
                IsEncrypted = s.IsEncrypted,
                UpdatedAt = s.UpdatedAt,
                UpdatedBy = s.UpdatedBy,
                UpdatedByName = s.UpdatedByUser?.FullName ?? s.UpdatedByUser?.Username
            }).ToList();
        }

        public async Task<List<SettingDto>> GetAllSettingsAsync(CancellationToken cancellationToken = default)
        {
            var settings = await _context.Settings
                .AsNoTracking()
                .Include(s => s.UpdatedByUser)
                .OrderBy(s => s.SettingGroup)
                .ThenBy(s => s.SettingKey)
                .ToListAsync(cancellationToken);

            return settings.Select(s => new SettingDto
            {
                Id = s.Id,
                SettingKey = s.SettingKey,
                SettingValue = s.IsEncrypted ? Decrypt(s.SettingValue) : s.SettingValue,
                SettingGroup = s.SettingGroup,
                Description = s.Description,
                DataType = s.DataType,
                IsEncrypted = s.IsEncrypted,
                UpdatedAt = s.UpdatedAt,
                UpdatedBy = s.UpdatedBy,
                UpdatedByName = s.UpdatedByUser?.FullName ?? s.UpdatedByUser?.Username
            }).ToList();
        }

        public async Task<bool> UpdateBatchAsync(Dictionary<string, string> settings, int? userId = null, CancellationToken cancellationToken = default)
        {
            if (settings == null || settings.Count == 0) return true;

            var keys = settings.Keys.Select(k => k.Trim().ToUpperInvariant()).ToList();
            var existingSettings = await _context.Settings
                .Where(s => keys.Contains(s.SettingKey))
                .ToListAsync(cancellationToken);

            var affectedGroups = new HashSet<string>();

            foreach (var kvp in settings)
            {
                string normKey = kvp.Key.Trim().ToUpperInvariant();
                string newVal = kvp.Value ?? string.Empty;

                var entity = existingSettings.FirstOrDefault(s => s.SettingKey == normKey);
                if (entity != null)
                {
                    // Nếu là field mã hóa và người dùng để trống hoặc giữ nguyên giá trị mặt nạ thì không ghi đè
                    if (entity.IsEncrypted && string.IsNullOrWhiteSpace(newVal))
                    {
                        continue;
                    }

                    entity.SettingValue = entity.IsEncrypted ? Encrypt(newVal) : newVal;
                    entity.UpdatedAt = DateTime.UtcNow;
                    entity.UpdatedBy = userId;

                    affectedGroups.Add(entity.SettingGroup);
                    _memoryCache.Remove($"{CacheKeyPrefix}{normKey}");
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            foreach (var g in affectedGroups)
            {
                _memoryCache.Remove($"{CacheGroupPrefix}{g}");
            }

            return true;
        }

        public Task InvalidateCacheAsync(string? key = null, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrEmpty(key))
            {
                _memoryCache.Remove($"{CacheKeyPrefix}{key.Trim().ToUpperInvariant()}");
            }
            else
            {
                // Xóa các nhóm chính
                _memoryCache.Remove($"{CacheGroupPrefix}GENERAL");
                _memoryCache.Remove($"{CacheGroupPrefix}SMTP");
                _memoryCache.Remove($"{CacheGroupPrefix}PAYMENT");
                _memoryCache.Remove($"{CacheGroupPrefix}SHIPPING");
            }

            return Task.CompletedTask;
        }

        #region Encryption Helper
        private static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            try
            {
                using var aes = Aes.Create();
                aes.Key = EncryptionKey;
                aes.IV = EncryptionIv;

                using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream();
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                }

                return Convert.ToBase64String(ms.ToArray());
            }
            catch
            {
                return plainText;
            }
        }

        private static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;

            try
            {
                var buffer = Convert.FromBase64String(cipherText);
                using var aes = Aes.Create();
                aes.Key = EncryptionKey;
                aes.IV = EncryptionIv;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(buffer);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);

                return sr.ReadToEnd();
            }
            catch
            {
                // Fallback nếu chuỗi lưu trữ trước đó chưa mã hóa
                return cipherText;
            }
        }
        #endregion
    }
}
