using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Web.Models;

namespace Omnichannel.Web.Controllers
{
    /// <summary>
    /// Controller điều hướng trang chủ và trải nghiệm storefront khách hàng
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IActionResult> Index()
        {
            // Truy vấn Banner quảng cáo trang chủ với chuỗi định danh chuẩn
            var heroBanners = await _context.Banners
                .AsNoTracking()
                .Where(b => b.IsActive && (b.Position == "HERO_HOME" || b.Position == "1"))
                .OrderBy(b => b.SortOrder)
                .ToListAsync();

            ViewBag.HeroBanners = heroBanners;

            // Truy vấn Danh mục gốc cho thanh điều hướng và avatar tròn
            var rootCategories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive && c.ParentId == null)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();

            ViewBag.Categories = rootCategories;

            // Truy vấn Top 8 sản phẩm nổi bật
            var featuredProducts = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Batches)
                .Where(p => p.IsActive && p.IsFeatured)
                .Take(8)
                .ToListAsync();

            ViewBag.FeaturedProducts = featuredProducts;

            // Truy vấn danh sách các lô hàng cận date FEFO phục vụ Flash Sale
            var now = DateTime.UtcNow;
            var fefoThresholdDate = now.AddDays(60);

            var fefoBatches = await _context.ProductBatches
                .AsNoTracking()
                .Include(pb => pb.Product)
                .Where(pb => pb.CurrentQuantity > 0 && (pb.Status == "NEAR_EXPIRATION" || pb.ExpDate <= fefoThresholdDate))
                .OrderBy(pb => pb.ExpDate)
                .Take(4)
                .ToListAsync();

            ViewBag.FefoBatches = fefoBatches;

            return View();
        }

        [HttpGet("clearance-fefo")]
        public async Task<IActionResult> ClearanceFefo()
        {
            var now = DateTime.UtcNow;
            var fefoThresholdDate = now.AddDays(60);

            var nearExpiryBatches = await _context.ProductBatches
                .AsNoTracking()
                .Include(pb => pb.Product)
                    .ThenInclude(p => p.Category)
                .Where(pb => pb.CurrentQuantity > 0 && (pb.Status == "NEAR_EXPIRATION" || pb.ExpDate <= fefoThresholdDate))
                .OrderBy(pb => pb.ExpDate)
                .ToListAsync();

            return View(nearExpiryBatches);
        }

        [HttpGet]
        public async Task<IActionResult> GetProductTab(string category = "all")
        {
            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.IsActive);

            if (category != "all")
            {
                if(category == "skincare") query = query.Where(p => p.Category.Name.Contains("Da") || p.Category.Name.Contains("Rửa Mặt") || p.Category.Name.Contains("Tẩy") || p.Category.Name.Contains("Chống Nắng") || p.Category.Name.Contains("Serum"));
                else if(category == "makeup") query = query.Where(p => p.Category.Name.Contains("Trang Điểm") || p.Category.Name.Contains("Son") || p.Category.Name.Contains("Cushion"));
                else if(category == "haircare") query = query.Where(p => p.Category.Name.Contains("Tóc"));
            }

            var products = await query.Take(8).ToListAsync();
            return PartialView("_ProductTabGrid", products);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}