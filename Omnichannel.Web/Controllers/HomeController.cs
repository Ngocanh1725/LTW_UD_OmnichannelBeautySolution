using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        private readonly IWebHostEnvironment? _env;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext context,
            IWebHostEnvironment? env = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _env = env;
        }

        /// <summary>
        /// Trang Living Styleguide trình bày toàn bộ Design System "Rosé Luxe" (Chỉ mở trên môi trường Development)
        /// </summary>
        [HttpGet("styleguide")]
        public async Task<IActionResult> Styleguide()
        {
            var env = _env ?? HttpContext?.RequestServices?.GetService<IWebHostEnvironment>();
            if (env == null || !env.IsDevelopment())
            {
                return NotFound();
            }

            List<Product> sampleProducts = new();
            List<Category> sampleCategories = new();
            try
            {
                sampleProducts = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Brand)
                    .Include(p => p.Category)
                    .Include(p => p.Batches)
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.IsFeatured)
                    .ThenBy(p => p.Id)
                    .Take(4)
                    .ToListAsync();

                sampleCategories = await _context.Categories
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Id)
                    .Take(4)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không tải được dữ liệu mẫu cho trang /styleguide, sử dụng dữ liệu tĩnh.");
            }

            ViewBag.SampleProducts = sampleProducts;
            ViewBag.SampleCategories = sampleCategories;
            return View();
        }

        public IActionResult Index([FromQuery] string? mode = null)
        {
            if (string.Equals(mode, "draft", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.IsPreviewMode = true;
                ViewBag.PreviewMode = "draft";
            }
            // Giao diện trang chủ được điều phối và tối ưu hóa 100% qua HomeSectionsViewComponent (đã tích hợp L1 MemoryCache)
            return View();
        }

        [HttpGet("preview")]
        public IActionResult Preview([FromQuery] string mode = "draft")
        {
            ViewBag.IsPreviewMode = true;
            ViewBag.PreviewMode = mode;
            return View("Index");
        }

        [HttpGet("clearance-fefo")]
        public async Task<IActionResult> ClearanceFefo()
        {
            var now = DateTime.UtcNow;
            var fefoThresholdDate = now.AddDays(90);

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
                .AsSplitQuery()
                .Include(p => p.Category)
                .Include(p => p.Batches)
                .Where(p => p.IsActive);

            var cat = (category ?? "all").ToLowerInvariant();
            if (cat == "newest" || cat == "moi-nhat")
            {
                query = query.OrderByDescending(p => p.CreatedAt);
            }
            else if (cat == "bestseller" || cat == "ban-chay")
            {
                query = query.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.Id);
            }
            else if (cat == "sensitive" || cat == "da-nhay-cam")
            {
                query = query.Where(p => (p.Description != null && p.Description.Contains("nhạy cảm")) 
                                      || p.Name.Contains("Dịu") 
                                      || p.Name.Contains("Phục Hồi") 
                                      || p.Name.Contains("B5") 
                                      || p.Name.Contains("Centella") 
                                      || p.Name.Contains("Ceramide") 
                                      || p.Name.Contains("Sensitive"))
                             .OrderByDescending(p => p.Id);
            }
            else if (cat == "skincare")
            {
                query = query.Where(p => (p.Category != null && (p.Category.Name.Contains("Da") || p.Category.Name.Contains("Rửa Mặt") || p.Category.Name.Contains("Tẩy") || p.Category.Name.Contains("Chống Nắng") || p.Category.Name.Contains("Serum") || p.Category.Slug.Contains("cham-soc-da"))) || p.Name.Contains("Serum") || p.Name.Contains("Kem") || p.Name.Contains("Toner"))
                             .OrderByDescending(p => p.Id);
            }
            else if (cat == "makeup")
            {
                query = query.Where(p => (p.Category != null && (p.Category.Name.Contains("Trang Điểm") || p.Category.Name.Contains("Son") || p.Category.Name.Contains("Cushion") || p.Category.Slug.Contains("trang-diem"))) || p.Name.Contains("Son") || p.Name.Contains("Phấn") || p.Name.Contains("Cushion"))
                             .OrderByDescending(p => p.Id);
            }
            else if (cat == "fragrance")
            {
                query = query.Where(p => (p.Category != null && (p.Category.Name.Contains("Nước Hoa") || p.Category.Slug.Contains("nuoc-hoa"))) || p.Name.Contains("Nước Hoa") || p.Name.Contains("Parfum"))
                             .OrderByDescending(p => p.Id);
            }
            else if (cat == "haircare")
            {
                query = query.Where(p => (p.Category != null && (p.Category.Name.Contains("Tóc") || p.Category.Slug.Contains("toc"))) || p.Name.Contains("Dầu Gội") || p.Name.Contains("Dưỡng Tóc"))
                             .OrderByDescending(p => p.Id);
            }
            else
            {
                query = query.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.Id);
            }

            var products = await query.Take(8).ToListAsync();

            if (!products.Any())
            {
                products = await _context.Products
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(p => p.Category)
                    .Include(p => p.Batches)
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.Id)
                    .Take(8)
                    .ToListAsync();
            }

            return PartialView("_ProductTabGrid", products);
        }

        [HttpGet]
        public async Task<IActionResult> QuickView(int id)
        {
            var product = await _context.Products
                .AsNoTracking()
                .AsSplitQuery()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Batches)
                .Include(p => p.Variants.Where(v => v.IsActive))
                    .ThenInclude(v => v.Swatches)
                .FirstOrDefaultAsync(p => p.Id == id);
                
            if (product == null) return NotFound();
                
            return PartialView("_QuickView", product);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // Custom Error Handling: 404, 403, 500
        [Route("Home/StatusCode")]
        [Route("Home/ErrorStatusCode")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult ErrorStatusCode(int? statusCode = null, int? code = null)
        {
            var actualCode = statusCode ?? code ?? HttpContext.Response.StatusCode;

            if (actualCode == 404)
            {
                Response.StatusCode = 404;
                return View("NotFound404");
            }
            if (actualCode == 403)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            Response.StatusCode = actualCode;
            return View("Error500", new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }

        [Route("Error/404")]
        public IActionResult NotFound404()
        {
            Response.StatusCode = 404;
            return View("NotFound404");
        }

        [Route("Error/500")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error500()
        {
            Response.StatusCode = 500;
            return View("Error500", new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error500", new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }

        #region Technical SEO: Sitemap.xml & Robots.txt

        /// <summary>
        /// SEO: Tự động sinh sitemap.xml động chứa toàn bộ URL danh mục sản phẩm, chi tiết 80+ sản phẩm và bài viết blog
        /// Tuân thủ chuẩn Sitemaps.org XML Schema
        /// </summary>
        [Route("sitemap.xml")]
        [HttpGet]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> Sitemap()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            var urlset = new XElement(ns + "urlset");

            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

            // 1. Core Landing Pages (Priority 0.8 - 1.0)
            var staticPages = new List<(string Path, string Priority, string Freq)>
            {
                ("/", "1.0", "daily"),
                ("/category", "0.9", "daily"),
                ("/san-pham", "0.9", "daily"),
                ("/clearance-fefo", "0.9", "daily"),
                ("/tu-van-da", "0.9", "daily"),
                ("/phan-tich-da", "0.8", "weekly"),
                ("/so-sanh", "0.8", "weekly"),
                ("/tra-cuu-don-hang", "0.8", "weekly"),
                ("/Blog", "0.8", "daily"),
                ("/Product/SmartFilter", "0.8", "weekly")
            };

            foreach (var sp in staticPages)
            {
                urlset.Add(new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}{sp.Path}"),
                    new XElement(ns + "lastmod", today),
                    new XElement(ns + "changefreq", sp.Freq),
                    new XElement(ns + "priority", sp.Priority)
                ));
            }

            // 2. Dynamic Categories (Priority 0.9, daily)
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive && !string.IsNullOrEmpty(c.Slug))
                .OrderBy(c => c.SortOrder)
                .Select(c => c.Slug)
                .ToListAsync();

            foreach (var slug in categories)
            {
                urlset.Add(new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}/category/{slug}"),
                    new XElement(ns + "lastmod", today),
                    new XElement(ns + "changefreq", "daily"),
                    new XElement(ns + "priority", "0.9")
                ));
            }

            // 3. Dynamic Products (80+ active items, Priority 0.8, daily)
            var products = await _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive && !string.IsNullOrEmpty(p.Slug))
                .Select(p => new
                {
                    p.Slug,
                    LastMod = (p.UpdatedAt ?? p.CreatedAt).ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            foreach (var prod in products)
            {
                urlset.Add(new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}/Product/Detail/{prod.Slug}"),
                    new XElement(ns + "lastmod", prod.LastMod),
                    new XElement(ns + "changefreq", "daily"),
                    new XElement(ns + "priority", "0.8")
                ));
            }

            // 4. Dynamic Blog Posts (Priority 0.7, weekly)
            var posts = await _context.Posts
                .AsNoTracking()
                .Where(p => p.IsPublished && !string.IsNullOrEmpty(p.Slug))
                .Select(p => new
                {
                    p.Slug,
                    LastMod = (p.PublishedAt ?? p.CreatedAt).ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            foreach (var post in posts)
            {
                urlset.Add(new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}/Blog/{post.Slug}"),
                    new XElement(ns + "lastmod", post.LastMod),
                    new XElement(ns + "changefreq", "weekly"),
                    new XElement(ns + "priority", "0.7")
                ));
            }

            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                urlset
            );

            return Content(doc.ToString(SaveOptions.DisableFormatting), "application/xml", Encoding.UTF8);
        }

        /// <summary>
        /// SEO: Cấu hình robots.txt chuẩn bot Google / Bing index, chặn khu vực quản trị nhạy cảm
        /// </summary>
        [Route("robots.txt")]
        [HttpGet]
        [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
        public IActionResult Robots()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var sb = new StringBuilder();

            sb.AppendLine("# ==============================================================");
            sb.AppendLine("# CANMAKE BEAUTY - Senior Technical SEO Robots.txt Directives");
            sb.AppendLine("# ==============================================================");
            sb.AppendLine("User-agent: *");
            sb.AppendLine("Allow: /");
            sb.AppendLine("Allow: /category/");
            sb.AppendLine("Allow: /Product/");
            sb.AppendLine("Allow: /san-pham/");
            sb.AppendLine("Allow: /Blog/");
            sb.AppendLine("Allow: /tin-tuc/");
            sb.AppendLine("Allow: /css/");
            sb.AppendLine("Allow: /js/");
            sb.AppendLine("Allow: /images/");
            sb.AppendLine();
            sb.AppendLine("# Disallow administrative & sensitive transactional paths");
            sb.AppendLine("Disallow: /admin/");
            sb.AppendLine("Disallow: /Admin/");
            sb.AppendLine("Disallow: /api/");
            sb.AppendLine("Disallow: /Api/");
            sb.AppendLine("Disallow: /checkout/");
            sb.AppendLine("Disallow: /Checkout/");
            sb.AppendLine("Disallow: /cart/");
            sb.AppendLine("Disallow: /Cart/");
            sb.AppendLine("Disallow: /account/");
            sb.AppendLine("Disallow: /Account/");
            sb.AppendLine("Disallow: /order/");
            sb.AppendLine("Disallow: /Order/");
            sb.AppendLine("Disallow: /systemHub");
            sb.AppendLine("Disallow: /notificationHub");
            sb.AppendLine("Disallow: /*?*sort=");
            sb.AppendLine("Disallow: /*?*filter=");
            sb.AppendLine("Disallow: /*?*search=");
            sb.AppendLine("Disallow: /Home/Error*");
            sb.AppendLine("Disallow: /Home/StatusCode*");
            sb.AppendLine();
            sb.AppendLine("# Googlebot specific crawling rules");
            sb.AppendLine("User-agent: Googlebot");
            sb.AppendLine("Allow: /");
            sb.AppendLine("Disallow: /Admin/");
            sb.AppendLine("Disallow: /Account/");
            sb.AppendLine("Disallow: /Checkout/");
            sb.AppendLine();
            sb.AppendLine("# Bingbot specific crawling rules");
            sb.AppendLine("User-agent: Bingbot");
            sb.AppendLine("Allow: /");
            sb.AppendLine("Disallow: /Admin/");
            sb.AppendLine("Disallow: /Account/");
            sb.AppendLine("Disallow: /Checkout/");
            sb.AppendLine();
            sb.AppendLine($"Sitemap: {baseUrl}/sitemap.xml");

            return Content(sb.ToString(), "text/plain", Encoding.UTF8);
        }

        #endregion
    }
}