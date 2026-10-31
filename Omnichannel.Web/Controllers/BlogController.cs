using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Web.Models;

namespace Omnichannel.Web.Controllers
{
    public class BlogController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BlogController(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        [HttpGet("Blog")]
        [HttpGet("tin-tuc")]
        public async Task<IActionResult> Index(string? category = null, string? q = null, int page = 1)
        {
            var query = _context.Posts
                .AsNoTracking()
                .Include(p => p.PostCategory)
                .Include(p => p.Author)
                .Where(p => p.IsPublished);

            if (!string.IsNullOrWhiteSpace(category))
            {
                var cleanCat = category.Trim().ToLowerInvariant();
                query = query.Where(p => p.PostCategory.Slug == cleanCat);
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var cleanQ = q.Trim().ToLowerInvariant();
                query = query.Where(p => p.Title.ToLower().Contains(cleanQ) || (p.Summary != null && p.Summary.ToLower().Contains(cleanQ)));
            }

            int pageSize = 9;
            int total = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)total / pageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            var posts = await query
                .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var categories = await _context.PostCategories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            var recentPosts = await _context.Posts
                .AsNoTracking()
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                .Take(5)
                .ToListAsync();

            var featuredPost = posts.FirstOrDefault();

            var vm = new BlogIndexViewModel
            {
                Posts = posts,
                Categories = categories,
                FeaturedPost = featuredPost,
                RecentPosts = recentPosts,
                SelectedCategorySlug = category,
                Query = q,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalPosts = total
            };

            return View(vm);
        }

        [HttpGet("Blog/{slug}")]
        [HttpGet("tin-tuc/{slug}")]
        public async Task<IActionResult> Detail(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return RedirectToAction(nameof(Index));
            }

            var cleanSlug = slug.Trim().ToLowerInvariant();
            var post = await _context.Posts
                .Include(p => p.PostCategory)
                .Include(p => p.Author)
                .FirstOrDefaultAsync(p => p.Slug == cleanSlug && p.IsPublished);

            if (post == null)
            {
                return NotFound();
            }

            // Tăng lượt xem (không blocking)
            try
            {
                post.ViewCount++;
                await _context.SaveChangesAsync();
            }
            catch { }

            var relatedPosts = await _context.Posts
                .AsNoTracking()
                .Where(p => p.PostCategoryId == post.PostCategoryId && p.Id != post.Id && p.IsPublished)
                .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                .Take(3)
                .ToListAsync();

            // Gợi ý sản phẩm làm đẹp liên quan
            var relatedProducts = await _context.Products
                .AsNoTracking()
                .Include(p => p.Batches)
                .Where(p => p.IsActive)
                .Take(4)
                .ToListAsync();

            var vm = new BlogDetailViewModel
            {
                Post = post,
                RelatedPosts = relatedPosts,
                RelatedProducts = relatedProducts
            };

            return View(vm);
        }
    }
}
