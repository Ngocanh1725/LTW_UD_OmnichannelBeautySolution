using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Omnichannel.Application.DTOs.Cms;
using Omnichannel.Application.Interfaces.Inventory;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Web.Models;

namespace Omnichannel.Web.ViewComponents
{
    public class HomeSectionsViewComponent : ViewComponent
    {
        public const string CacheKey = "home_ui_sections";
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly IStoreInventoryService _storeInventoryService;

        public HomeSectionsViewComponent(
            ApplicationDbContext context, 
            IMemoryCache cache,
            IStoreInventoryService storeInventoryService)
        {
            _context = context;
            _cache = cache;
            _storeInventoryService = storeInventoryService;
        }

        public const string CompositeModelCacheKey = "HomeSections_Composite_Model_v1";

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (_cache.TryGetValue(CompositeModelCacheKey, out HomeSectionsViewModel? cachedModel) && cachedModel != null)
            {
                return View(cachedModel);
            }

            if (!_cache.TryGetValue(CacheKey, out List<UiSection>? sections) || sections == null)
            {
                sections = await _context.UiSections
                    .AsNoTracking()
                    .Where(s => s.IsVisible)
                    .OrderBy(s => s.DisplayOrder)
                    .ToListAsync();

                _cache.Set(CacheKey, sections, TimeSpan.FromMinutes(30));
            }

            if (!sections.Any())
            {
                sections = new List<UiSection>
                {
                    new() { Id = 1, SectionCode = "HERO_SLIDER", Title = "Hero Banner Slider", Subtitle = "Bộ sưu tập mỹ phẩm dẫn đầu xu hướng làm đẹp 2026", DisplayOrder = 1, IsVisible = true, LayoutType = "CAROUSEL" },
                    new() { Id = 2, SectionCode = "FEATURED_CATEGORIES", Title = "Danh Mục Nổi Bật", Subtitle = "Khám phá vẻ đẹp thuần khiết theo từng phân nhóm da liễu", DisplayOrder = 2, IsVisible = true, LayoutType = "GRID_6" },
                    new() { Id = 3, SectionCode = "FLASH_SALE", Title = "Flash Sale Deal Sốc", Subtitle = "Cơ hội săn mỹ phẩm cao cấp chuẩn hạn dùng FEFO với giá ưu đãi cực lớn", DisplayOrder = 3, IsVisible = true, LayoutType = "GRID_4" },
                    new() { Id = 4, SectionCode = "BEST_SELLERS", Title = "Sản Phẩm Bán Chạy Nhất", Subtitle = "Top mỹ phẩm được các tín đồ làm đẹp tin chọn nhiều nhất", DisplayOrder = 4, IsVisible = true, LayoutType = "GRID_4" }
                };
            }

            var model = new HomeSectionsViewModel();

            // Lazy pre-fetch needed entities for efficiency
            List<Banner>? banners = null;
            Dictionary<int, BannerStyleConfigDto>? bannerStyles = null;
            List<Category>? categories = null;
            List<Product>? flashSaleProducts = null;
            List<Product>? bestSellers = null;

            foreach (var section in sections)
            {
                var itemModel = new HomeSectionItemViewModel
                {
                    Section = section
                };

                switch (section.SectionCode.ToUpperInvariant())
                {
                    case "HERO_SLIDER":
                        if (banners == null)
                        {
                            banners = await _context.Banners
                                .AsNoTracking()
                                .Where(b => b.IsActive && (b.Position == "HERO_HOME" || b.Position == "1" || string.IsNullOrEmpty(b.Position)))
                                .OrderBy(b => b.DisplayOrder)
                                .ThenBy(b => b.SortOrder)
                                .ToListAsync();

                            if (!banners.Any())
                            {
                                banners = await _context.Banners
                                    .AsNoTracking()
                                    .Where(b => b.IsActive)
                                    .OrderBy(b => b.DisplayOrder)
                                    .ThenBy(b => b.SortOrder)
                                    .ToListAsync();
                            }

                            if (!banners.Any())
                            {
                                banners = new List<Banner>
                                {
                                    new()
                                    {
                                        Title = "Bừng Sáng Nhan Sắc Mùa Hè 2026",
                                        Subtitle = "Ưu đãi độc quyền đến 40% cho bộ sưu tập làm đẹp cao cấp",
                                        DesktopImageUrl = "/images/banners/hero-banner-summer-beauty.jpg",
                                        ImageUrl = "/images/banners/hero-banner-summer-beauty.jpg",
                                        LinkUrl = "/category/cham-soc-da",
                                        ButtonText = "Khám Phá Ngay"
                                    },
                                    new()
                                    {
                                        Title = "Chu Trình K-Beauty Chuẩn Spa",
                                        Subtitle = "Nuôi dưỡng làn da căng mọng ngậm nước với tinh chất quý giá",
                                        DesktopImageUrl = "/images/banners/banner-routine-kbeauty.jpg",
                                        ImageUrl = "/images/banners/banner-routine-kbeauty.jpg",
                                        LinkUrl = "/category/trang-diem",
                                        ButtonText = "Mua Ngay"
                                    }
                                };
                            }
                        }

                        if (bannerStyles == null)
                        {
                            bannerStyles = new Dictionary<int, BannerStyleConfigDto>();
                            var styleSettings = await _context.Settings
                                .AsNoTracking()
                                .Where(s => s.SettingKey.StartsWith("BANNER_STYLE_CONFIG_"))
                                .ToListAsync();
                            foreach (var s in styleSettings)
                            {
                                var idPart = s.SettingKey.Replace("BANNER_STYLE_CONFIG_", "");
                                if (int.TryParse(idPart, out int bId) && !string.IsNullOrWhiteSpace(s.SettingValue))
                                {
                                    try
                                    {
                                        var parsed = JsonSerializer.Deserialize<BannerStyleConfigDto>(s.SettingValue);
                                        if (parsed != null)
                                        {
                                            bannerStyles[bId] = parsed;
                                        }
                                    }
                                    catch
                                    {
                                        // Ignore malformed json
                                    }
                                }
                            }
                        }

                        itemModel.Banners = banners;
                        itemModel.BannerStyles = bannerStyles;
                        break;

                    case "FEATURED_CATEGORIES":
                        if (categories == null)
                        {
                            categories = await _context.Categories
                                .AsNoTracking()
                                .Where(c => c.IsActive && c.ParentId == null)
                                .OrderBy(c => c.SortOrder)
                                .Take(12)
                                .ToListAsync();

                            if (categories.Count < 6)
                            {
                                var subCategories = await _context.Categories
                                    .AsNoTracking()
                                    .Where(c => c.IsActive && c.ParentId != null && !categories.Select(r => r.Id).Contains(c.Id))
                                    .OrderBy(c => c.SortOrder)
                                    .Take(12 - categories.Count)
                                    .ToListAsync();

                                categories.AddRange(subCategories);
                            }
                        }
                        itemModel.Categories = categories;
                        break;

                    case "FLASH_SALE":
                        var flashCampaign = await _storeInventoryService.GetActiveFlashSaleAsync();
                        itemModel.FlashSaleCampaign = flashCampaign;

                        if (flashSaleProducts == null)
                        {
                            flashSaleProducts = await _context.Products
                                .AsNoTracking()
                                .Include(p => p.Category)
                                .Include(p => p.Supplier)
                                .Where(p => p.IsActive && p.DiscountPrice != null && p.DiscountPrice > 0)
                                .OrderBy(p => p.Id)
                                .Take(8)
                                .ToListAsync();

                            if (flashSaleProducts.Count < 4)
                            {
                                var extra = await _context.Products
                                    .AsNoTracking()
                                    .Include(p => p.Category)
                                    .Include(p => p.Supplier)
                                    .Where(p => p.IsActive && !flashSaleProducts.Select(f => f.Id).Contains(p.Id))
                                    .OrderByDescending(p => p.Id)
                                    .Take(8 - flashSaleProducts.Count)
                                    .ToListAsync();

                                flashSaleProducts.AddRange(extra);
                            }
                        }
                        itemModel.FlashSaleProducts = flashSaleProducts;
                        break;

                    case "BEST_SELLERS":
                        if (bestSellers == null)
                        {
                            bestSellers = await _context.Products
                                .AsNoTracking()
                                .Include(p => p.Category)
                                .Include(p => p.Supplier)
                                .Where(p => p.IsActive)
                                .OrderByDescending(p => p.IsFeatured)
                                .ThenByDescending(p => p.Id)
                                .Take(8)
                                .ToListAsync();
                        }
                        itemModel.BestSellers = bestSellers;
                        break;

                    case "MID_BANNER":
                        var midBanners = await _context.Banners
                            .AsNoTracking()
                            .Where(b => b.IsActive)
                            .OrderByDescending(b => b.SortOrder)
                            .Take(1)
                            .ToListAsync();
                        itemModel.MidBanners = midBanners;
                        break;

                    case "BEAUTY_BLOG":
                        var blogPosts = await _context.Posts
                            .AsNoTracking()
                            .Include(p => p.PostCategory)
                            .Where(p => p.IsPublished)
                            .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                            .Take(3)
                            .ToListAsync();
                        itemModel.BlogPosts = blogPosts;
                        break;
                }

                model.Sections.Add(itemModel);
            }

            _cache.Set(CompositeModelCacheKey, model, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(15),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });

            return View(model);
        }
    }
}
