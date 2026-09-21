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
            return item.Url ?? "#";
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

            // Tự động tìm nạp Slug chính xác theo loại thực thể nếu chưa có
            string? slug = model.TargetSlug?.Trim();
            if (string.IsNullOrEmpty(slug) && !string.IsNullOrEmpty(model.TargetId))
            {
                slug = await ResolveEntitySlugAsync(model.TargetType, model.TargetId, cancellationToken);
            }

            string url = "#";
            if (!string.IsNullOrEmpty(model.CustomUrl))
            {
                url = model.CustomUrl;
            }
            else if (!string.IsNullOrEmpty(model.TargetSlug))
            {
                url = model.TargetType switch
                {
                    1 => $"/danh-muc/{model.TargetSlug}",
                    2 => $"/san-pham/{model.TargetSlug}",
                    3 => $"/tin-tuc/{model.TargetSlug}",
                    4 => $"/trang/{model.TargetSlug}",
                    _ => "#"
                };
            }

            var newItem = new MenuItem
            {
                MenuId = model.MenuId,
                ParentId = model.ParentId,
                Title = model.Title.Trim(),
                Url = url,
                SortOrder = model.DisplayOrder,
                Target = model.OpenInNewTab ? "_blank" : "_self",
                Icon = model.IconClass?.Trim(),
                IsActive = true
            };

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

        private MenuItemDto MapToDto(MenuItem entity, List<MenuItem> allItems)
        {
            var dto = new MenuItemDto
            {
                MenuItemId = entity.Id,
                MenuId = entity.MenuId,
                ParentId = entity.ParentId,
                Title = entity.Title,
                CustomUrl = entity.Url,
                ResolvedUrl = ResolveUrl(entity),
                DisplayOrder = entity.SortOrder,
                OpenInNewTab = entity.Target == "_blank",
                IconClass = entity.Icon,
                IsActive = entity.IsActive,
                Children = allItems
                    .Where(child => child.ParentId == entity.Id)
                    .OrderBy(child => child.SortOrder)
                    .Select(child => MapToDto(child, allItems))
                    .ToList()
            };

            return dto;
        }

        private async Task<string?> ResolveEntitySlugAsync(int targetType, string targetId, CancellationToken cancellationToken)
        {
            return targetType switch
            {
                1 when int.TryParse(targetId, out int catId) =>
                    (await _context.Categories.FindAsync(new object[] { catId }, cancellationToken))?.Slug,
                2 => (await _context.Products.FindAsync(new object[] { targetId }, cancellationToken))?.Slug,
                3 when int.TryParse(targetId, out int postId) =>
                    (await _context.Posts.FindAsync(new object[] { postId }, cancellationToken))?.Slug,
                4 when int.TryParse(targetId, out int pageId) =>
                    (await _context.StaticPages.FindAsync(new object[] { pageId }, cancellationToken))?.Slug,
                _ => null
            };
        }
    }
}
