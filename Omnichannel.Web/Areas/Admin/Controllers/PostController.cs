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
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        [HttpGet]
        [HasPermission("CMS_POST_MANAGE")]
        public async Task<IActionResult> Index()
        {
            var posts = await _context.Posts
                .AsNoTracking()
                .Include(p => p.PostCategory)
                .Include(p => p.Author)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PostItemViewModel
                {
                    PostId = p.Id,
                    CategoryName = p.PostCategory != null ? p.PostCategory.Name : "Chung",
                    Title = p.Title,
                    Slug = p.Slug,
                    ThumbnailUrl = p.ThumbnailUrl ?? "",
                    AuthorName = p.Author != null ? (p.Author.FullName ?? p.Author.Username) : "Biên tập viên",
                    Status = p.IsPublished ? 3 : 1,
                    ViewCount = p.ViewCount,
                    PublishedAt = p.PublishedAt,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return View(posts);
        }

        [HttpGet]
        [HasPermission("CMS_POST_MANAGE")]
        public async Task<IActionResult> Create()
        {
            var categories = await _context.PostCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(new CreatePostViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("CMS_POST_MANAGE")]
        public async Task<IActionResult> Create(CreatePostViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var categories = await _context.PostCategories.Where(c => c.IsActive).ToListAsync();
                ViewBag.Categories = new SelectList(categories, "Id", "Name", model.CategoryId);
                return View(model);
            }

            string cleanSlug = string.IsNullOrWhiteSpace(model.Slug)
                ? Slugify(model.Title)
                : model.Slug.Trim().ToLowerInvariant();

            if (await _context.Posts.AnyAsync(p => p.Slug == cleanSlug))
            {
                cleanSlug = $"{cleanSlug}-{Guid.NewGuid():N}"[..Math.Min(cleanSlug.Length + 9, 150)];
            }

            int? authorId = null;
            var uidClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(uidClaim, out var parsedUid))
            {
                authorId = parsedUid;
            }

            bool isPublished = model.Status == 3;
            var post = new Post
            {
                Title = model.Title.Trim(),
                Slug = cleanSlug,
                Summary = model.Summary?.Trim(),
                Content = model.ContentHtml,
                ThumbnailUrl = model.ThumbnailUrl?.Trim(),
                PostCategoryId = model.CategoryId,
                AuthorId = authorId,
                IsPublished = isPublished,
                PublishedAt = isPublished ? DateTime.UtcNow : null,
                CreatedAt = DateTime.UtcNow,
                ViewCount = 0
            };

            await _context.Posts.AddAsync(post);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xuất bản bài viết '{post.Title}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [HasPermission("CMS_POST_MANAGE")]
        public async Task<IActionResult> Edit(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

            var categories = await _context.PostCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            ViewBag.Categories = new SelectList(categories, "Id", "Name", post.PostCategoryId);

            var vm = new EditPostViewModel
            {
                PostId = post.Id,
                CategoryId = post.PostCategoryId,
                Title = post.Title,
                Slug = post.Slug,
                Summary = post.Summary,
                ContentHtml = post.Content,
                ThumbnailUrl = post.ThumbnailUrl ?? "",
                Status = post.IsPublished ? 3 : 1
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("CMS_POST_MANAGE")]
        public async Task<IActionResult> Edit(EditPostViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var categories = await _context.PostCategories.Where(c => c.IsActive).ToListAsync();
                ViewBag.Categories = new SelectList(categories, "Id", "Name", model.CategoryId);
                return View(model);
            }

            var post = await _context.Posts.FindAsync(model.PostId);
            if (post == null) return NotFound();

            string cleanSlug = string.IsNullOrWhiteSpace(model.Slug)
                ? Slugify(model.Title)
                : model.Slug.Trim().ToLowerInvariant();

            if (await _context.Posts.AnyAsync(p => p.Slug == cleanSlug && p.Id != model.PostId))
            {
                cleanSlug = $"{cleanSlug}-{Guid.NewGuid():N}"[..Math.Min(cleanSlug.Length + 9, 150)];
            }

            bool wasPublished = post.IsPublished;
            bool nowPublished = model.Status == 3;

            post.Title = model.Title.Trim();
            post.Slug = cleanSlug;
            post.Summary = model.Summary?.Trim();
            post.Content = model.ContentHtml;
            post.ThumbnailUrl = model.ThumbnailUrl?.Trim();
            post.PostCategoryId = model.CategoryId;
            post.IsPublished = nowPublished;
            if (!wasPublished && nowPublished && post.PublishedAt == null)
            {
                post.PublishedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã cập nhật bài viết '{post.Title}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("CMS_POST_MANAGE")]
        public async Task<IActionResult> TogglePublish(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

            post.IsPublished = !post.IsPublished;
            if (post.IsPublished && post.PublishedAt == null)
            {
                post.PublishedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = post.IsPublished
                ? $"Đã xuất bản bài viết '{post.Title}'."
                : $"Đã chuyển bài viết '{post.Title}' về bản nháp.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("CMS_POST_MANAGE")]
        public async Task<IActionResult> Delete(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return NotFound();

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã gỡ bài viết khỏi hệ thống!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [HasPermission("CMS_POST_MANAGE")]
        public IActionResult GenerateSlug([FromQuery] string title)
        {
            var slug = Slugify(title);
            return Json(new { success = true, slug });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("CMS_POST_MANAGE")]
        public async Task<IActionResult> UploadImage(Microsoft.AspNetCore.Http.IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "Vui lòng chọn tệp ảnh hợp lệ." });
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var ext = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                return BadRequest(new { success = false, message = "Định dạng không được hỗ trợ (chỉ nhận JPG, PNG, WEBP, GIF)." });
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { success = false, message = "Dung lượng ảnh tối đa 5MB." });
            }

            var uploadsFolder = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "uploads", "posts");
            if (!System.IO.Directory.Exists(uploadsFolder))
            {
                System.IO.Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid():N}_{System.IO.Path.GetFileName(file.FileName)}";
            var filePath = System.IO.Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var url = $"/uploads/posts/{uniqueFileName}";
            return Json(new { success = true, url });
        }

        private static string Slugify(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var normalized = text.Trim().ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var c in normalized)
            {
                var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    if (c == 'đ') sb.Append('d');
                    else if (char.IsLetterOrDigit(c)) sb.Append(c);
                    else if (char.IsWhiteSpace(c) || c == '-' || c == '_') sb.Append('-');
                }
            }
            var slug = System.Text.RegularExpressions.Regex.Replace(sb.ToString(), "-+", "-").Trim('-');
            return string.IsNullOrEmpty(slug) ? "post-" + Guid.NewGuid().ToString("N")[..8] : slug;
        }
    }
}