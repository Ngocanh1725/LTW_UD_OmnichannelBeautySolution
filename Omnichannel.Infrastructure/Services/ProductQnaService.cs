using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Cms;
using Omnichannel.Application.Interfaces.Cms;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services
{
    public class ProductQnaService : IProductQnaService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductQnaService> _logger;

        public ProductQnaService(ApplicationDbContext context, ILogger<ProductQnaService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<ProductQuestionDto>> GetApprovedQuestionsAsync(int productId, int page = 1, int pageSize = 10, CancellationToken ct = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var questions = await _context.ProductQuestions
                .AsNoTracking()
                .Include(q => q.Product)
                .Include(q => q.User)
                .Include(q => q.Answers)
                .Where(q => q.ProductId == productId && q.IsApproved && !q.IsHidden)
                .OrderByDescending(q => q.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return questions.Select(q => new ProductQuestionDto
            {
                Id = q.Id,
                ProductId = q.ProductId,
                ProductName = q.Product?.Name ?? "Sản phẩm",
                UserId = q.UserId,
                AuthorName = !string.IsNullOrWhiteSpace(q.AuthorName) ? q.AuthorName : (q.User?.FullName ?? "Khách hàng"),
                Content = q.Content,
                IsApproved = q.IsApproved,
                IsHidden = q.IsHidden,
                CreatedAt = q.CreatedAt,
                Answers = q.Answers
                    .Where(a => a.IsApproved && !a.IsHidden)
                    .OrderBy(a => a.IsOfficial ? 0 : 1) // Câu trả lời chính thức của dược sĩ/CSKH lên đầu
                    .ThenBy(a => a.CreatedAt)
                    .Select(a => new ProductAnswerDto
                    {
                        Id = a.Id,
                        QuestionId = a.QuestionId,
                        UserId = a.UserId,
                        AuthorName = !string.IsNullOrWhiteSpace(a.AuthorName) ? a.AuthorName : (a.IsOfficial ? "Chuyên viên Dược Mỹ Phẩm Canmake Beauty" : "Khách hàng"),
                        Content = a.Content,
                        IsOfficial = a.IsOfficial,
                        IsApproved = a.IsApproved,
                        CreatedAt = a.CreatedAt
                    }).ToList()
            }).ToList();
        }

        public async Task<ProductQuestionDto> SubmitQuestionAsync(int productId, string content, int? userId, string? authorName = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Nội dung câu hỏi không được để trống.", nameof(content));
            }

            var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId, ct);
            if (product == null)
            {
                throw new InvalidOperationException($"Không tìm thấy sản phẩm với ID {productId}.");
            }

            string safeContent = HtmlEncoder.Default.Encode(content.Trim());

            string safeAuthor = authorName?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(safeAuthor) && userId.HasValue)
            {
                var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
                safeAuthor = user?.FullName ?? user?.Username ?? "Khách hàng";
            }
            if (string.IsNullOrEmpty(safeAuthor))
            {
                safeAuthor = "Khách hàng";
            }
            safeAuthor = HtmlEncoder.Default.Encode(safeAuthor);

            var question = new ProductQuestion
            {
                ProductId = productId,
                UserId = userId,
                AuthorName = safeAuthor,
                Content = safeContent,
                IsApproved = true, // Tự động duyệt hiển thị tức thì trên Storefront demo
                IsHidden = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProductQuestions.Add(question);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("[Q&A] Câu hỏi mới #{QuestionId} cho sản phẩm {ProductId} từ User {UserId}", question.Id, productId, userId);

            return new ProductQuestionDto
            {
                Id = question.Id,
                ProductId = question.ProductId,
                ProductName = product.Name,
                UserId = question.UserId,
                AuthorName = question.AuthorName,
                Content = question.Content,
                IsApproved = question.IsApproved,
                IsHidden = question.IsHidden,
                CreatedAt = question.CreatedAt,
                Answers = new List<ProductAnswerDto>()
            };
        }

        public async Task<ProductAnswerDto> SubmitAnswerAsync(int questionId, string content, int? userId, string? authorName = null, bool isOfficial = false, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Nội dung câu trả lời không được để trống.", nameof(content));
            }

            var question = await _context.ProductQuestions.FindAsync(new object[] { questionId }, ct);
            if (question == null)
            {
                throw new InvalidOperationException($"Không tìm thấy câu hỏi với ID {questionId}.");
            }

            string safeContent = HtmlEncoder.Default.Encode(content.Trim());

            string safeAuthor = authorName?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(safeAuthor))
            {
                if (isOfficial)
                {
                    safeAuthor = "Chuyên viên Dược Mỹ Phẩm Canmake Beauty";
                }
                else if (userId.HasValue)
                {
                    var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
                    safeAuthor = user?.FullName ?? user?.Username ?? "Cộng đồng";
                }
                else
                {
                    safeAuthor = "Cộng đồng";
                }
            }

            var answer = new ProductAnswer
            {
                QuestionId = questionId,
                UserId = userId,
                AuthorName = safeAuthor,
                Content = safeContent,
                IsOfficial = isOfficial,
                IsApproved = true,
                IsHidden = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProductAnswers.Add(answer);
            question.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("[Q&A] Câu trả lời mới #{AnswerId} cho câu hỏi #{QuestionId} (Official: {IsOfficial})", answer.Id, questionId, isOfficial);

            return new ProductAnswerDto
            {
                Id = answer.Id,
                QuestionId = answer.QuestionId,
                UserId = answer.UserId,
                AuthorName = answer.AuthorName,
                Content = answer.Content,
                IsOfficial = answer.IsOfficial,
                IsApproved = answer.IsApproved,
                CreatedAt = answer.CreatedAt
            };
        }

        public async Task<bool> ApproveQuestionAsync(int questionId, bool isApproved, CancellationToken ct = default)
        {
            var question = await _context.ProductQuestions.FindAsync(new object[] { questionId }, ct);
            if (question == null) return false;

            question.IsApproved = isApproved;
            question.UpdatedAt = DateTime.UtcNow;
            return await _context.SaveChangesAsync(ct) > 0;
        }

        public async Task<bool> ToggleHideQuestionAsync(int questionId, bool isHidden, CancellationToken ct = default)
        {
            var question = await _context.ProductQuestions.FindAsync(new object[] { questionId }, ct);
            if (question == null) return false;

            question.IsHidden = isHidden;
            question.UpdatedAt = DateTime.UtcNow;
            return await _context.SaveChangesAsync(ct) > 0;
        }

        public async Task<bool> DeleteQuestionAsync(int questionId, CancellationToken ct = default)
        {
            var question = await _context.ProductQuestions
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.Id == questionId, ct);

            if (question == null) return false;

            _context.ProductQuestions.Remove(question);
            return await _context.SaveChangesAsync(ct) > 0;
        }

        public async Task<List<ProductQuestionDto>> GetAllQuestionsForAdminAsync(int? productId = null, bool? isApproved = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _context.ProductQuestions
                .AsNoTracking()
                .Include(q => q.Product)
                .Include(q => q.User)
                .Include(q => q.Answers)
                .AsQueryable();

            if (productId.HasValue)
            {
                query = query.Where(q => q.ProductId == productId.Value);
            }

            if (isApproved.HasValue)
            {
                query = query.Where(q => q.IsApproved == isApproved.Value);
            }

            var items = await query
                .OrderByDescending(q => q.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return items.Select(q => new ProductQuestionDto
            {
                Id = q.Id,
                ProductId = q.ProductId,
                ProductName = q.Product?.Name ?? "Sản phẩm",
                UserId = q.UserId,
                AuthorName = !string.IsNullOrWhiteSpace(q.AuthorName) ? q.AuthorName : (q.User?.FullName ?? "Khách hàng"),
                Content = q.Content,
                IsApproved = q.IsApproved,
                IsHidden = q.IsHidden,
                CreatedAt = q.CreatedAt,
                Answers = q.Answers.Select(a => new ProductAnswerDto
                {
                    Id = a.Id,
                    QuestionId = a.QuestionId,
                    UserId = a.UserId,
                    AuthorName = a.AuthorName ?? "Khách hàng",
                    Content = a.Content,
                    IsOfficial = a.IsOfficial,
                    IsApproved = a.IsApproved,
                    CreatedAt = a.CreatedAt
                }).ToList()
            }).ToList();
        }
    }
}
