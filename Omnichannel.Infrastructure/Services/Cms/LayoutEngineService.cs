using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Cms;
using Omnichannel.Application.Interfaces.Cms;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services.Cms
{
    public class LayoutEngineService : ILayoutEngineService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<LayoutEngineService> _logger;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public const string HomeCompositeCacheKey = "HomeSections_Composite_Model_v1";
        public const string HomeSectionsCacheKey = "home_ui_sections";
        public const string ThemeConfigCacheKey = "theme_config_current";

        public LayoutEngineService(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<LayoutEngineService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PageLayoutDto> GetLayoutAsync(string pageKey, bool isDraft = false)
        {
            var cacheKey = $"published_layout_{pageKey}";
            if (!isDraft && _cache.TryGetValue(cacheKey, out PageLayoutDto? cached) && cached != null)
            {
                return cached;
            }

            var layout = await _context.PageLayouts
                .Include(p => p.LayoutSections.OrderBy(s => s.OrderIndex))
                    .ThenInclude(s => s.SectionItems.OrderBy(i => i.OrderIndex))
                        .ThenInclude(i => i.MediaAsset)
                .FirstOrDefaultAsync(p => p.PageKey == pageKey);

            var theme = await _context.ThemeConfigs.OrderBy(t => t.Id).FirstOrDefaultAsync();
            var themeTokens = new ThemeTokensDto
            {
                PrimaryColor = theme?.PrimaryColor ?? "#E11D48",
                SecondaryColor = theme?.SecondaryColor ?? "#BE185D",
                AccentColor = theme?.AccentColor ?? "#F59E0B",
                FontHeading = theme?.FontHeading ?? "'Playfair Display', serif",
                FontBody = theme?.FontBody ?? "'Inter', sans-serif"
            };

            if (layout == null)
            {
                return new PageLayoutDto
                {
                    PageKey = pageKey,
                    Title = "Trang Chủ - CANMAKE BEAUTY",
                    IsPublished = true,
                    Theme = themeTokens
                };
            }

            if (isDraft && !string.IsNullOrWhiteSpace(layout.DraftConfigJson))
            {
                try
                {
                    var draftDto = JsonSerializer.Deserialize<PageLayoutDto>(layout.DraftConfigJson, JsonOptions);
                    if (draftDto != null)
                    {
                        draftDto.Id = layout.Id;
                        draftDto.PageKey = layout.PageKey;
                        if (draftDto.Theme == null) draftDto.Theme = themeTokens;
                        return draftDto;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi deserialize DraftConfigJson cho pageKey {PageKey}. Fallback sang CSDL.", pageKey);
                }
            }
            else if (!isDraft && !string.IsNullOrWhiteSpace(layout.PublishedConfigJson))
            {
                try
                {
                    var publishedDto = JsonSerializer.Deserialize<PageLayoutDto>(layout.PublishedConfigJson, JsonOptions);
                    if (publishedDto != null)
                    {
                        publishedDto.Id = layout.Id;
                        publishedDto.PageKey = layout.PageKey;
                        if (publishedDto.Theme == null) publishedDto.Theme = themeTokens;
                        _cache.Set(cacheKey, publishedDto, TimeSpan.FromMinutes(30));
                        return publishedDto;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi deserialize PublishedConfigJson cho pageKey {PageKey}. Fallback sang CSDL.", pageKey);
                }
            }

            // Fallback sang CSDL quan hệ
            var result = new PageLayoutDto
            {
                Id = layout.Id,
                PageKey = layout.PageKey,
                Title = layout.Title,
                MetaDescription = layout.MetaDescription,
                IsPublished = layout.IsPublished,
                Theme = themeTokens,
                Sections = layout.LayoutSections.Select(s => new LayoutSectionDto
                {
                    Id = s.Id,
                    PageLayoutId = s.PageLayoutId,
                    SectionKey = s.SectionKey,
                    DisplayName = s.DisplayName,
                    OrderIndex = s.OrderIndex,
                    IsVisible = s.IsVisible,
                    CustomStylesJson = s.CustomStylesJson,
                    Items = s.SectionItems.Select(i => new SectionItemDto
                    {
                        Id = i.Id,
                        LayoutSectionId = i.LayoutSectionId,
                        Title = i.Title,
                        Subtitle = i.Subtitle,
                        TargetUrl = i.TargetUrl,
                        MediaAssetId = i.MediaAssetId,
                        ImageUrl = i.MediaAsset != null
                            ? (!string.IsNullOrEmpty(i.MediaAsset.PublicUrl) ? i.MediaAsset.PublicUrl : $"/media/{i.MediaAsset.Id}")
                            : null,
                        OrderIndex = i.OrderIndex,
                        PayloadJson = i.PayloadJson
                    }).ToList()
                }).ToList()
            };

            if (!isDraft)
            {
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(30));
            }

            return result;
        }

        public async Task<bool> SaveDraftLayoutAsync(string pageKey, SaveDraftLayoutRequest request)
        {
            var layout = await _context.PageLayouts.FirstOrDefaultAsync(p => p.PageKey == pageKey);
            if (layout == null)
            {
                layout = new PageLayout
                {
                    PageKey = pageKey,
                    Title = "Trang Chủ - CANMAKE BEAUTY",
                    IsPublished = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.PageLayouts.Add(layout);
            }

            var draftDto = new PageLayoutDto
            {
                Id = layout.Id,
                PageKey = pageKey,
                Title = layout.Title,
                MetaDescription = layout.MetaDescription,
                IsPublished = false,
                Sections = request.Sections ?? new List<LayoutSectionDto>(),
                Theme = request.Theme ?? new ThemeTokensDto()
            };

            layout.DraftConfigJson = JsonSerializer.Serialize(draftDto, JsonOptions);
            layout.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> PublishLayoutAsync(string pageKey)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var layout = await _context.PageLayouts
                        .Include(p => p.LayoutSections)
                            .ThenInclude(s => s.SectionItems)
                        .FirstOrDefaultAsync(p => p.PageKey == pageKey);

                    if (layout == null) return false;

                    // Nếu chưa có DraftConfigJson thì lấy cấu hình hiện tại đóng gói lại
                    PageLayoutDto? draftDto = null;
                    if (!string.IsNullOrWhiteSpace(layout.DraftConfigJson))
                    {
                        draftDto = JsonSerializer.Deserialize<PageLayoutDto>(layout.DraftConfigJson, JsonOptions);
                    }

                    if (draftDto == null)
                    {
                        draftDto = await GetLayoutAsync(pageKey, isDraft: false);
                    }

                    // 1. Sao chép Draft -> Published
                    draftDto.IsPublished = true;
                    layout.PublishedConfigJson = JsonSerializer.Serialize(draftDto, JsonOptions);
                    layout.IsPublished = true;
                    layout.UpdatedAt = DateTime.UtcNow;

                    // 2. Đồng bộ các Sections & Items trong CSDL quan hệ
                    if (draftDto.Sections != null && draftDto.Sections.Any())
                    {
                        foreach (var secDto in draftDto.Sections)
                        {
                            var existingSection = layout.LayoutSections.FirstOrDefault(s => s.Id == secDto.Id || s.SectionKey == secDto.SectionKey);
                            if (existingSection != null)
                            {
                                existingSection.DisplayName = secDto.DisplayName;
                                existingSection.OrderIndex = secDto.OrderIndex;
                                existingSection.IsVisible = secDto.IsVisible;
                                existingSection.CustomStylesJson = secDto.CustomStylesJson;

                                if (secDto.Items != null && secDto.Items.Any())
                                {
                                    foreach (var itemDto in secDto.Items)
                                    {
                                        var existingItem = existingSection.SectionItems.FirstOrDefault(i => i.Id == itemDto.Id);
                                        if (existingItem != null)
                                        {
                                            existingItem.Title = itemDto.Title;
                                            existingItem.Subtitle = itemDto.Subtitle;
                                            existingItem.TargetUrl = itemDto.TargetUrl;
                                            existingItem.MediaAssetId = itemDto.MediaAssetId;
                                            existingItem.OrderIndex = itemDto.OrderIndex;
                                            existingItem.PayloadJson = itemDto.PayloadJson;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // 3. Đồng bộ Tokens sang ThemeConfigs
                    if (draftDto.Theme != null)
                    {
                        var theme = await _context.ThemeConfigs.OrderBy(t => t.Id).FirstOrDefaultAsync();
                        if (theme != null)
                        {
                            if (!string.IsNullOrEmpty(draftDto.Theme.PrimaryColor)) theme.PrimaryColor = draftDto.Theme.PrimaryColor;
                            if (!string.IsNullOrEmpty(draftDto.Theme.SecondaryColor)) theme.SecondaryColor = draftDto.Theme.SecondaryColor;
                            if (!string.IsNullOrEmpty(draftDto.Theme.AccentColor)) theme.AccentColor = draftDto.Theme.AccentColor;
                            if (!string.IsNullOrEmpty(draftDto.Theme.FontHeading)) theme.FontHeading = draftDto.Theme.FontHeading;
                            if (!string.IsNullOrEmpty(draftDto.Theme.FontBody)) theme.FontBody = draftDto.Theme.FontBody;
                            theme.UpdatedAt = DateTime.UtcNow;
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // 4. Xóa In-Memory Cache
                    InvalidateCache(pageKey);

                    _logger.LogInformation("Xuất bản thành công Layout {PageKey} và đồng bộ CSDL/Cache.", pageKey);
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Lỗi khi Xuất bản Layout {PageKey}", pageKey);
                    throw;
                }
            });
        }

        public async Task<bool> UpdateThemeTokensAsync(ThemeTokensDto tokens)
        {
            var theme = await _context.ThemeConfigs.OrderBy(t => t.Id).FirstOrDefaultAsync();
            if (theme == null) return false;

            if (!string.IsNullOrEmpty(tokens.PrimaryColor)) theme.PrimaryColor = tokens.PrimaryColor;
            if (!string.IsNullOrEmpty(tokens.SecondaryColor)) theme.SecondaryColor = tokens.SecondaryColor;
            if (!string.IsNullOrEmpty(tokens.AccentColor)) theme.AccentColor = tokens.AccentColor;
            if (!string.IsNullOrEmpty(tokens.FontHeading)) theme.FontHeading = tokens.FontHeading;
            if (!string.IsNullOrEmpty(tokens.FontBody)) theme.FontBody = tokens.FontBody;
            theme.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _cache.Remove(ThemeConfigCacheKey);
            return true;
        }

        public void InvalidateCache(string pageKey)
        {
            _cache.Remove($"published_layout_{pageKey}");
            _cache.Remove(HomeCompositeCacheKey);
            _cache.Remove(HomeSectionsCacheKey);
            _cache.Remove(ThemeConfigCacheKey);
        }
    }
}

