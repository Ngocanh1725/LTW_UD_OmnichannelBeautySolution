using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    public class MenuEngineService : IMenuEngineService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<MenuEngineService> _logger;

        private const string CachePrefix = "MenuTree:";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

        public MenuEngineService(
            ApplicationDbContext context,
            IMemoryCache memoryCache,
            ILogger<MenuEngineService> logger)
        {
            _context = context;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<List<MenuItemDto>> GetHierarchyTreeAsync(string menuCode, bool onlyActive = true, CancellationToken cancellationToken = default)
        {
            string cacheKey = $"{CachePrefix}{menuCode}:active_{onlyActive}";

            if (_memoryCache.TryGetValue(cacheKey, out List<MenuItemDto>? cachedTree) && cachedTree != null)
            {
                return cachedTree;
            }

            var menu = await _context.Menus
                .AsNoTracking()
                .Include(m => m.MenuItems)
                .FirstOrDefaultAsync(m => m.Code == menuCode, cancellationToken);

            if (menu == null)
            {
                return new List<MenuItemDto>();
            }

            var query = menu.MenuItems.AsQueryable();
            if (onlyActive)
            {
                query = query.Where(item => item.IsActive);
            }

            var flatItems = query.OrderBy(item => item.SortOrder).ToList();

            // Biên dịch cấu trúc phẳng thành cây đệ quy và giải quyết liên kết URL động
            var rootNodes = flatItems
                .Where(i => i.ParentId == null)
                .Select(i => MapToDto(i, flatItems))
                .ToList();

            _memoryCache.Set(cacheKey, rootNodes, CacheTtl);
            return rootNodes;
        }

        public string ResolveUrl(MenuItem item)
        {
            var linkType = !string.IsNullOrEmpty(item.LinkType) ? item.LinkType.Trim().ToUpperInvariant() : "CUSTOM";

            if (linkType == "CUSTOM")
            {
                return string.IsNullOrEmpty(item.Url) ? "#" : item.Url;
            }

            var slug = !string.IsNullOrEmpty(item.TargetSlug) ? item.TargetSlug.Trim() : string.Empty;
            if (string.IsNullOrEmpty(slug))
            {
                return string.IsNullOrEmpty(item.Url) ? "#" : item.Url;
            }

            return linkType switch
            {
                "CATEGORY" => $"/category/{slug}", // Chọn Danh mục -> Tự thành link Danh mục
                "PRODUCT" => $"/product/{slug}",// Chọn Sản phẩm -> Tự thành link Sản phẩm
                "POST" => $"/blog/{slug}",// Chọn Bài viết  -> Tự thành link Blog
                "STATIC_PAGE" => $"/page/{slug}",// Chọn Trang     -> Tự thành link Trang tĩnh
                _ => string.IsNullOrEmpty(item.Url) ? "#" : item.Url // Link tự do
            };
        }

        public Task InvalidateMenuCacheAsync(string menuCode, CancellationToken cancellationToken = default)
        {
            _memoryCache.Remove($"{CachePrefix}{menuCode}:active_True");
            _memoryCache.Remove($"{CachePrefix}{menuCode}:active_False");
            _logger.LogInformation("Đã xóa bộ đệm Cache cho cây Menu {MenuCode}.", menuCode);
            return Task.CompletedTask;
        }

        public async Task<bool> AddMenuItemAsync(CreateMenuItemViewModel model, CancellationToken cancellationToken = default)
        {
            var menu = await _context.Menus.FindAsync(new object[] { model.MenuId }, cancellationToken);
            if (menu == null) return false;

            // Xác định LinkType đa hình
            string linkType = !string.IsNullOrEmpty(model.LinkType) ? model.LinkType.Trim().ToUpperInvariant() : "CUSTOM";
            if (linkType == "CUSTOM" && model.TargetType > 0 && model.TargetType != 5)
            {
                linkType = model.TargetType switch
                {
                    1 => "CATEGORY",
                    2 => "PRODUCT",
                    3 => "POST",
                    4 => "STATIC_PAGE",
                    _ => "CUSTOM"
                };
            }

            int? targetEntityId = model.TargetEntityId;
            if (!targetEntityId.HasValue && int.TryParse(model.TargetId, out int tid))
            {
                targetEntityId = tid;
            }

            // Tự động tìm nạp Slug chính xác theo loại thực thể nếu chưa có
            string? slug = model.TargetSlug?.Trim();
            if (string.IsNullOrEmpty(slug) && targetEntityId.HasValue && linkType != "CUSTOM")
            {
                slug = await ResolveEntitySlugAsync(linkType, targetEntityId.Value, cancellationToken);
            }

            var newItem = new MenuItem
            {
                MenuId = model.MenuId,
                ParentId = model.ParentId,
                Title = model.Title.Trim(),
                LinkType = linkType,
                TargetEntityId = targetEntityId,
                TargetSlug = slug,
                CssClass = model.CssClass?.Trim(),
                SortOrder = model.DisplayOrder,
                Target = model.OpenInNewTab ? "_blank" : "_self",
                Icon = model.IconClass?.Trim(),
                IsActive = true
            };

            if (linkType == "CUSTOM")
            {
                newItem.Url = !string.IsNullOrEmpty(model.CustomUrl) ? model.CustomUrl.Trim() : "#";
            }
            else
            {
                newItem.Url = ResolveUrl(newItem);
            }

            await _context.MenuItems.AddAsync(newItem, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            await InvalidateMenuCacheAsync(menu.Code, cancellationToken);
            return true;
        }

        public async Task<bool> DeleteMenuItemAsync(int menuItemId, CancellationToken cancellationToken = default)
        {
            var item = await _context.MenuItems
                .Include(i => i.Menu)
                .FirstOrDefaultAsync(i => i.Id == menuItemId, cancellationToken);

            if (item == null) return false;

            string menuCode = item.Menu.Code;

            // Xóa đệ quy các nút con phụ thuộc
            var childIds = await _context.MenuItems
                .Where(i => i.ParentId == menuItemId)
                .Select(i => i.Id)
                .ToListAsync(cancellationToken);

            foreach (var cid in childIds)
            {
                await DeleteMenuItemAsync(cid, cancellationToken);
            }

            _context.MenuItems.Remove(item);
            await _context.SaveChangesAsync(cancellationToken);

            await InvalidateMenuCacheAsync(menuCode, cancellationToken);
            return true;
        }

        public async Task<bool> UpdateMenuItemAsync(EditMenuItemViewModel model, CancellationToken cancellationToken = default)
        {
            var item = await _context.MenuItems
                .Include(i => i.Menu)
                .FirstOrDefaultAsync(i => i.Id == model.MenuItemId, cancellationToken);

            if (item == null) return false;

            string menuCode = item.Menu.Code;
            item.Title = model.Title.Trim();
            item.ParentId = model.ParentId;
            item.SortOrder = model.DisplayOrder;
            item.Target = model.OpenInNewTab ? "_blank" : "_self";
            item.Icon = model.IconClass?.Trim();
            item.CssClass = model.CssClass?.Trim();
            item.IsActive = model.IsActive;

            var linkType = model.TargetType switch
            {
                1 => "CATEGORY",
                2 => "PRODUCT",
                3 => "POST",
                4 => "STATIC_PAGE",
                _ => "CUSTOM"
            };

            item.LinkType = linkType;
            if (int.TryParse(model.TargetId, out var targetEntityId))
            {
                item.TargetEntityId = targetEntityId;
            }

            string? slug = model.TargetSlug?.Trim();
            if (string.IsNullOrEmpty(slug) && item.TargetEntityId.HasValue && linkType != "CUSTOM")
            {
                slug = await ResolveEntitySlugAsync(linkType, item.TargetEntityId.Value, cancellationToken);
            }
            item.TargetSlug = slug;

            if (linkType == "CUSTOM")
            {
                item.Url = !string.IsNullOrEmpty(model.CustomUrl) ? model.CustomUrl.Trim() : "#";
            }
            else
            {
                item.Url = ResolveUrl(item);
            }

            await _context.SaveChangesAsync(cancellationToken);
            await InvalidateMenuCacheAsync(menuCode, cancellationToken);
            return true;
        }

        public async Task<bool> ReorderMenuItemsAsync(ReorderMenuRequest request, CancellationToken cancellationToken = default)
        {
            var menu = await _context.Menus
                .Include(m => m.MenuItems)
                .FirstOrDefaultAsync(m => m.Id == request.MenuId, cancellationToken);

            if (menu == null) return false;

            foreach (var rItem in request.Items)
            {
                var item = menu.MenuItems.FirstOrDefault(i => i.Id == rItem.Id);
                if (item != null)
                {
                    item.SortOrder = rItem.SortOrder;
                    item.ParentId = rItem.ParentId;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await InvalidateMenuCacheAsync(menu.Code, cancellationToken);
            return true;
        }

        public async Task<int> SyncEntitySlugChangeAsync(string entityType, int entityId, string oldSlug, string newSlug, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(oldSlug) || string.IsNullOrWhiteSpace(newSlug) || oldSlug.Trim() == newSlug.Trim())
            {
                return 0;
            }

            var cleanOld = oldSlug.Trim();
            var cleanNew = newSlug.Trim();
            var normType = entityType.Trim().ToUpperInvariant();

            // 1. Cập nhật MenuItem có TargetEntityId tương ứng
            var matchingItems = await _context.MenuItems
                .Include(m => m.Menu)
                .Where(m => m.LinkType == normType && m.TargetEntityId == entityId)
                .ToListAsync(cancellationToken);

            var affectedMenuCodes = new HashSet<string>();

            foreach (var item in matchingItems)
            {
                item.TargetSlug = cleanNew;
                item.Url = ResolveUrl(item);
                if (item.Menu != null)
                {
                    affectedMenuCodes.Add(item.Menu.Code);
                }
            }

            // 2. Tạo bản ghi SlugRedirect (HTTP 301)
            var existingRedirect = await _context.SlugRedirects
                .FirstOrDefaultAsync(r => r.OldSlug == cleanOld && r.EntityType == normType, cancellationToken);

            if (existingRedirect == null)
            {
                var redirect = new SlugRedirect
                {
                    OldSlug = cleanOld,
                    NewSlug = cleanNew,
                    EntityType = normType,
                    EntityId = entityId,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.SlugRedirects.AddAsync(redirect, cancellationToken);
            }
            else
            {
                existingRedirect.NewSlug = cleanNew;
                existingRedirect.CreatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            // 3. Xóa cache của các menu bị ảnh hưởng
            foreach (var code in affectedMenuCodes)
            {
                await InvalidateMenuCacheAsync(code, cancellationToken);
            }

            // Xóa cache SlugRedirect
            _memoryCache.Remove($"SlugRedirect:{cleanOld}");

            _logger.LogInformation("Đã đồng bộ đổi slug cho {EntityType} #{EntityId}: '{OldSlug}' -> '{NewSlug}'. Cập nhật {Count} MenuItems.",
                normType, entityId, cleanOld, cleanNew, matchingItems.Count);

            return matchingItems.Count;
        }

        public async Task<SlugRedirect?> GetRedirectAsync(string oldSlug, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(oldSlug)) return null;

            string cleanSlug = oldSlug.Trim();
            string cacheKey = $"SlugRedirect:{cleanSlug}";

            if (_memoryCache.TryGetValue(cacheKey, out SlugRedirect? cachedRedirect))
            {
                return cachedRedirect;
            }

            var redirect = await _context.SlugRedirects
                .AsNoTracking()
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync(r => r.OldSlug == cleanSlug, cancellationToken);

            if (redirect != null)
            {
                _memoryCache.Set(cacheKey, redirect, TimeSpan.FromHours(6));
            }

            return redirect;
        }

        private MenuItemDto MapToDto(MenuItem entity, List<MenuItem> allItems)
        {
            var dto = new MenuItemDto
            {
                MenuItemId = entity.Id,
                MenuId = entity.MenuId,
                ParentId = entity.ParentId,
                Title = entity.Title,
                LinkType = entity.LinkType ?? "CUSTOM",
                TargetEntityId = entity.TargetEntityId,
                TargetSlug = entity.TargetSlug,
                CustomUrl = entity.Url,
                ResolvedUrl = ResolveUrl(entity),
                DisplayOrder = entity.SortOrder,
                OpenInNewTab = entity.Target == "_blank",
                IconClass = entity.Icon,
                CssClass = entity.CssClass,
                IsActive = entity.IsActive,
                Children = allItems
                    .Where(child => child.ParentId == entity.Id)
                    .OrderBy(child => child.SortOrder)
                    .Select(child => MapToDto(child, allItems))
                    .ToList()
            };

            return dto;
        }

        private async Task<string?> ResolveEntitySlugAsync(string entityType, int entityId, CancellationToken cancellationToken)
        {
            return entityType.ToUpperInvariant() switch
            {
                "CATEGORY" => (await _context.Categories.FindAsync(new object[] { entityId }, cancellationToken))?.Slug,
                "PRODUCT" => (await _context.Products.FindAsync(new object[] { entityId }, cancellationToken))?.Slug,
                "POST" => (await _context.Posts.FindAsync(new object[] { entityId }, cancellationToken))?.Slug,
                "STATIC_PAGE" => (await _context.StaticPages.FindAsync(new object[] { entityId }, cancellationToken))?.Slug,
                _ => null
            };
        }
    }
}
