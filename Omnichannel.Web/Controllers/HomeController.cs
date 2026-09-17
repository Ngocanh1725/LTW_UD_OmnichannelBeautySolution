using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Cms;
using Omnichannel.Application.DTOs.Sales;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Web.Models;

namespace Omnichannel.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTime.UtcNow;
            var today = DateTime.Today;

            // 1. Tải Banners Hero Slider
            var banners = await _context.Banners
                .AsNoTracking()
                .Where(b => b.IsActive && b.Position == 1 && b.ValidFrom <= now && b.ValidTo >= now)
                .OrderBy(b => b.DisplayOrder)
                .Select(b => new BannerItemViewModel
                {
                    BannerId = b.BannerId,
                    Title = b.Title,
                    DesktopImageUrl = b.DesktopImageUrl,
                    LinkUrl = b.LinkUrl
                })
                .ToListAsync();

            // 2. Danh mục nổi bật
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive && c.ParentId == null)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new CategoryCardDto
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    Slug = c.Slug,
                    IconUrl = c.IconUrl ?? "fa-solid fa-wand-magic-sparkles",
                    ProductCount = c.Products.Count(p => p.Status == 1)
                })
                .ToListAsync();

            // 3. Sản phẩm kèm thông tin lô date gần nhất (Earliest Exp Date)
            var productsQuery = _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.ProductBatches)
                    .ThenInclude(pb => pb.StoreInventories)
                .Where(p => p.Status == 1);

            var productList = await productsQuery
                .Take(8)
                .ToListAsync();

            var mappedProducts = productList.Select(p =>
            {
                var validBatches = p.ProductBatches
                    .Where(b => b.ExpDate > today)
                    .OrderBy(b => b.ExpDate)
                    .ToList();

                var earliestBatch = validBatches.FirstOrDefault();
                int totalAvailable = validBatches
                    .SelectMany(b => b.StoreInventories)
                    .Sum(si => si.PhysicalQuantity - si.ReservedQuantity);

                return new StorefrontProductDto
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Slug = p.Slug,
                    CategoryName = p.Category.CategoryName,
                    SellingPrice = p.SellingPrice,
                    Unit = p.Unit,
                    EarliestExpDateStr = earliestBatch != null ? earliestBatch.ExpDate.ToString("MM/yyyy") : "12/2027",
                    TotalAvailableQuantity = Math.Max(0, totalAvailable)
                };
            }).ToList();

            var vm = new HomeIndexViewModel
            {
                HeroBanners = banners,
                FeaturedCategories = categories,
                FlashSaleProducts = mappedProducts.Take(4).ToList(),
                LatestProducts = mappedProducts
            };

            return View(vm);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}