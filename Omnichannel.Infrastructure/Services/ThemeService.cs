using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Cms;
using Omnichannel.Application.Interfaces.Cms;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services
{
    public class ThemeService : IThemeService
    {
        private const string ThemeCacheKey = "current_active_theme";
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ThemeService> _logger;

        public ThemeService(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<ThemeService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<ThemeConfigDto> GetCurrentThemeAsync()
        {
            if (_cache.TryGetValue(ThemeCacheKey, out ThemeConfigDto? cachedTheme) && cachedTheme != null)
            {
                return cachedTheme;
            }

            var theme = await _context.ThemeConfigs
                .AsNoTracking()
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.UpdatedAt)
                .FirstOrDefaultAsync();

            if (theme == null)
            {
                _logger.LogInformation("Chưa có cấu hình Theme trong CSDL, sử dụng cấu hình mặc định OMNI BEAUTY.");
                theme = new ThemeConfig
                {
                    BrandName = "OMNI BEAUTY",
                    Slogan = "Hệ Thống Bán Lẻ Mỹ Phẩm Cao Cấp Tiên Phong Công Nghệ FEFO",
                    PrimaryColor = "#E11D48",
                    SecondaryColor = "#BE185D",
                    AccentColor = "#F59E0B",
                    FontFamily = "'Inter', sans-serif",
                    AnnouncementBarText = "✨ Chào mừng bạn đến với OMNI BEAUTY - Miễn phí vận chuyển cho đơn hàng từ 300K ✨",
                    AnnouncementBarActive = true,
                    AnnouncementBarBgColor = "#881337",
                    Hotline = "1900 8888",
                    ContactEmail = "hotro@omnibeauty.vn",
                    Address = "68 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
                    FooterCopyright = "© 2026 OMNI BEAUTY. Tất cả quyền được bảo lưu.",
                    IsActive = true,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            var dto = new ThemeConfigDto
            {
                Id = theme.Id,
                BrandName = theme.BrandName,
                Slogan = theme.Slogan,
                PrimaryColor = theme.PrimaryColor,
                SecondaryColor = theme.SecondaryColor,
                AccentColor = theme.AccentColor,
                FontFamily = theme.FontFamily,
                LogoMediaId = theme.LogoMediaId,
                FaviconMediaId = theme.FaviconMediaId,
                AnnouncementBarText = theme.AnnouncementBarText,
                AnnouncementBarActive = theme.AnnouncementBarActive,
                AnnouncementBarBgColor = theme.AnnouncementBarBgColor,
                Hotline = theme.Hotline,
                ContactEmail = theme.ContactEmail,
                Address = theme.Address,
                FooterCopyright = theme.FooterCopyright,
                CustomCss = theme.CustomCss
            };

            _cache.Set(ThemeCacheKey, dto, TimeSpan.FromMinutes(60));
            return dto;
        }

        public async Task<bool> UpdateThemeAsync(ThemeConfigDto dto)
        {
            try
            {
                var theme = await _context.ThemeConfigs
                    .FirstOrDefaultAsync(t => t.Id == dto.Id || (dto.Id == 0 && t.IsActive));

                if (theme == null)
                {
                    theme = new ThemeConfig
                    {
                        IsActive = true
                    };
                    _context.ThemeConfigs.Add(theme);
                }

                theme.BrandName = dto.BrandName;
                theme.Slogan = dto.Slogan;
                theme.PrimaryColor = dto.PrimaryColor;
                theme.SecondaryColor = dto.SecondaryColor;
                theme.AccentColor = dto.AccentColor;
                theme.FontFamily = dto.FontFamily;
                theme.LogoMediaId = dto.LogoMediaId;
                theme.FaviconMediaId = dto.FaviconMediaId;
                theme.AnnouncementBarText = dto.AnnouncementBarText;
                theme.AnnouncementBarActive = dto.AnnouncementBarActive;
                theme.AnnouncementBarBgColor = dto.AnnouncementBarBgColor;
                theme.Hotline = dto.Hotline;
                theme.ContactEmail = dto.ContactEmail;
                theme.Address = dto.Address;
                theme.FooterCopyright = dto.FooterCopyright;
                theme.CustomCss = dto.CustomCss;
                theme.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                dto.Id = theme.Id;

                InvalidateCache();
                _logger.LogInformation("Cập nhật ThemeConfig thành công và đã xóa cache key '{CacheKey}'.", ThemeCacheKey);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình cập nhật cấu hình Theme.");
                return false;
            }
        }

        public void InvalidateCache()
        {
            _cache.Remove(ThemeCacheKey);
        }
    }
}
