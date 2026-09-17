using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Cms;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Security;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class PostController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PostController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [HasPermission("CMS_CONTENT", "VIEW")]
        public async Task<IActionResult> Index()
        {
            var posts = await _context.Posts
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Author)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PostItemViewModel
                {
                    PostId = p.PostId,
                    CategoryName = p.Category.CategoryName,
                    Title = p.Title,
                    Slug = p.Slug,
                    ThumbnailUrl = p.ThumbnailUrl,
                    AuthorName = p.Author.FullName,
                    Status = p.Status,
                    ViewCount = p.ViewCount,
                    PublishedAt = p.PublishedAt,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return View(posts);
        }

        [HttpGet]
        [HasPermission("CMS_CONTENT", "CREATE")]
        public async Task<IActionResult> Create()
        {
            var categories = await _context.PostCategories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToListAsync();
            ViewBag.Categories = new SelectList(categories, "CategoryId", "CategoryName");
            return View(new CreatePostViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("CMS_CONTENT", "CREATE")]
        public async Task<IActionResult> Create(CreatePostViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var categories = await _context.PostCategories.Where(c => c.IsActive).ToListAsync();
                ViewBag.Categories = new SelectList(categories, "CategoryId", "CategoryName");
                return View(model);
            }

            string cleanSlug = model.Slug.Trim().ToLowerInvariant();
            if (await _context.Posts.AnyAsync(p => p.Slug == cleanSlug))
            {
                ModelState.AddModelError("Slug", "Đường dẫn Slug này đã được bài viết khác sử dụng.");
                var categories = await _context.PostCategories.Where(c => c.IsActive).ToListAsync();
                ViewBag.Categories = new SelectList(categories, "CategoryId", "CategoryName");
                return View(model);
            }

            var authorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "USR-SUPERADMIN-0001";

            var post = new Post
            {
                CategoryId = model.CategoryId,
                Title = model.Title.Trim(),
                Slug = cleanSlug,
                Summary = model.Summary?.Trim(),
                ContentHtml = model.ContentHtml,
                ThumbnailUrl = model.ThumbnailUrl.Trim(),
                AuthorId = authorId,
                Status = model.Status,
                MetaTitle = model.MetaTitle?.Trim(),
                MetaKeywords = model.MetaKeywords?.Trim(),
                MetaDescription = model.MetaDescription?.Trim(),
                ViewCount = 0,
                PublishedAt = model.Status == 3 ? DateTime.UtcNow : null,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Posts.AddAsync(post);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã đăng tải thành công bài viết '{post.Title}'!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("CMS_CONTENT", "DELETE")]
        public async Task<IActionResult> Delete(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã gỡ bài viết khỏi hệ thống!";
            return RedirectToAction(nameof(Index));
        }
    }
}