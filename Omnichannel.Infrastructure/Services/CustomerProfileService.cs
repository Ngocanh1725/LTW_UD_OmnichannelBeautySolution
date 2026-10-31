using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Crm;
using Omnichannel.Application.Interfaces.Crm;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services
{
    public class CustomerProfileService : ICustomerProfileService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CustomerProfileService> _logger;

        public CustomerProfileService(ApplicationDbContext context, ILogger<CustomerProfileService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CustomerProfileDto?> GetProfileByUserIdAsync(int userId, CancellationToken ct = default)
        {
            var profile = await _context.CustomerProfiles
                .AsNoTracking()
                .Include(cp => cp.User)
                .Include(cp => cp.Consultant)
                .Include(cp => cp.SkinHistories.OrderByDescending(sh => sh.RecordedAt).Take(5))
                    .ThenInclude(sh => sh.RecordedByUser)
                .FirstOrDefaultAsync(cp => cp.UserId == userId, ct);

            return profile != null ? MapToProfileDto(profile) : null;
        }

        public async Task<CustomerProfileDto?> GetProfileByIdAsync(int profileId, CancellationToken ct = default)
        {
            var profile = await _context.CustomerProfiles
                .AsNoTracking()
                .Include(cp => cp.User)
                .Include(cp => cp.Consultant)
                .Include(cp => cp.SkinHistories.OrderByDescending(sh => sh.RecordedAt).Take(5))
                    .ThenInclude(sh => sh.RecordedByUser)
                .FirstOrDefaultAsync(cp => cp.Id == profileId, ct);

            return profile != null ? MapToProfileDto(profile) : null;
        }

        public async Task<CustomerProfileDto> UpsertProfileAsync(CreateCustomerProfileDto dto, CancellationToken ct = default)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == dto.UserId, ct);
            if (user == null)
            {
                throw new InvalidOperationException($"Không tìm thấy người dùng với ID {dto.UserId}.");
            }

            var profile = await _context.CustomerProfiles
                .Include(cp => cp.User)
                .Include(cp => cp.Consultant)
                .FirstOrDefaultAsync(cp => cp.UserId == dto.UserId, ct);

            string? concernsJson = dto.SkinConcerns != null ? JsonSerializer.Serialize(dto.SkinConcerns) : null;
            string? allergiesJson = dto.KnownAllergies != null ? JsonSerializer.Serialize(dto.KnownAllergies) : null;
            string? brandsJson = dto.PreferredBrands != null ? JsonSerializer.Serialize(dto.PreferredBrands) : null;

            if (profile == null)
            {
                profile = new CustomerProfile
                {
                    UserId = dto.UserId,
                    DateOfBirth = dto.DateOfBirth,
                    Gender = dto.Gender,
                    CurrentSkinType = dto.CurrentSkinType,
                    SkinConcerns = concernsJson,
                    KnownAllergies = allergiesJson,
                    PreferredBrands = brandsJson,
                    Note = dto.Note,
                    ConsultantUserId = dto.ConsultantUserId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.CustomerProfiles.Add(profile);
            }
            else
            {
                profile.DateOfBirth = dto.DateOfBirth;
                profile.Gender = dto.Gender;
                profile.CurrentSkinType = dto.CurrentSkinType;
                profile.SkinConcerns = concernsJson;
                profile.KnownAllergies = allergiesJson;
                profile.PreferredBrands = brandsJson;
                profile.Note = dto.Note;
                profile.ConsultantUserId = dto.ConsultantUserId;
                profile.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Đã cập nhật hồ sơ sắc đẹp cho khách hàng {UserId}", dto.UserId);

            return MapToProfileDto(profile);
        }

        public async Task<SkinHistoryDto> AddSkinHistoryAsync(int profileId, AddSkinHistoryDto dto, int? recordedBy = null, CancellationToken ct = default)
        {
            var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(cp => cp.Id == profileId, ct);
            if (profile == null)
            {
                throw new InvalidOperationException($"Không tìm thấy hồ sơ khách hàng với ID {profileId}.");
            }

            string? imagesJson = dto.ImageUrls != null && dto.ImageUrls.Any()
                ? JsonSerializer.Serialize(dto.ImageUrls)
                : null;

            var history = new SkinHistory
            {
                CustomerProfileId = profileId,
                RecordedAt = DateTime.UtcNow,
                SkinType = dto.SkinType,
                SkinCondition = dto.SkinCondition,
                TreatmentNote = dto.TreatmentNote,
                RecordedBy = recordedBy,
                ImageUrls = imagesJson
            };

            // Cập nhật loại da hiện tại và ngày tư vấn gần nhất trên hồ sơ
            profile.CurrentSkinType = dto.SkinType;
            profile.LastConsultationDate = DateTime.UtcNow;
            profile.UpdatedAt = DateTime.UtcNow;

            _context.SkinHistories.Add(history);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Ghi nhận nhật ký làn da mới cho Profile {ProfileId}. Loại da: {SkinType}", profileId, dto.SkinType);

            return new SkinHistoryDto
            {
                Id = history.Id,
                CustomerProfileId = history.CustomerProfileId,
                RecordedAt = history.RecordedAt,
                SkinType = history.SkinType,
                SkinCondition = history.SkinCondition,
                TreatmentNote = history.TreatmentNote,
                RecordedBy = history.RecordedBy,
                ImageUrls = dto.ImageUrls ?? new List<string>()
            };
        }

        public async Task<List<SkinHistoryDto>> GetSkinHistoriesAsync(int profileId, CancellationToken ct = default)
        {
            var histories = await _context.SkinHistories
                .AsNoTracking()
                .Include(sh => sh.RecordedByUser)
                .Where(sh => sh.CustomerProfileId == profileId)
                .OrderByDescending(sh => sh.RecordedAt)
                .ToListAsync(ct);

            return histories.Select(sh =>
            {
                List<string> imgs = new();
                if (!string.IsNullOrWhiteSpace(sh.ImageUrls))
                {
                    try { imgs = JsonSerializer.Deserialize<List<string>>(sh.ImageUrls) ?? new(); } catch { }
                }

                return new SkinHistoryDto
                {
                    Id = sh.Id,
                    CustomerProfileId = sh.CustomerProfileId,
                    RecordedAt = sh.RecordedAt,
                    SkinType = sh.SkinType,
                    SkinCondition = sh.SkinCondition,
                    TreatmentNote = sh.TreatmentNote,
                    RecordedBy = sh.RecordedBy,
                    RecordedByName = sh.RecordedByUser?.FullName ?? sh.RecordedByUser?.Username,
                    ImageUrls = imgs
                };
            }).ToList();
        }

        public async Task<RepurchaseAlertDto> CalculateRepurchaseScheduleAsync(int userId, int productId, decimal volume, decimal dailyUsage, CancellationToken ct = default)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId, ct);
            if (product == null)
            {
                throw new InvalidOperationException($"Không tìm thấy sản phẩm với ID {productId}.");
            }

            var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(cp => cp.UserId == userId, ct);
            if (profile == null)
            {
                profile = new CustomerProfile
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.CustomerProfiles.Add(profile);
                await _context.SaveChangesAsync(ct);
            }

            if (dailyUsage <= 0) dailyUsage = 1;
            int days = (int)Math.Ceiling(volume / dailyUsage);
            var now = DateTime.UtcNow;
            var depletionDate = now.AddDays(days);

            var schedule = await _context.RepurchaseSchedules
                .FirstOrDefaultAsync(rs => rs.CustomerProfileId == profile.Id && rs.ProductId == productId, ct);

            if (schedule == null)
            {
                schedule = new RepurchaseSchedule
                {
                    CustomerProfileId = profile.Id,
                    ProductId = productId,
                    LastPurchaseDate = now,
                    EstimatedVolumeMl = volume,
                    DailyUsageMl = dailyUsage,
                    EstimatedDepletionDate = depletionDate,
                    IsNotified = false,
                    CreatedAt = now
                };
                _context.RepurchaseSchedules.Add(schedule);
            }
            else
            {
                schedule.LastPurchaseDate = now;
                schedule.EstimatedVolumeMl = volume;
                schedule.DailyUsageMl = dailyUsage;
                schedule.EstimatedDepletionDate = depletionDate;
                schedule.IsNotified = false;
            }

            await _context.SaveChangesAsync(ct);

            var user = await _context.Users.FindAsync(new object[] { userId }, ct);

            return new RepurchaseAlertDto
            {
                ScheduleId = schedule.Id,
                CustomerProfileId = profile.Id,
                UserId = userId,
                CustomerName = user?.FullName ?? user?.Username ?? "Khách hàng",
                CustomerPhone = user?.PhoneNumber,
                ProductId = product.Id,
                ProductName = product.Name,
                ProductThumbnailUrl = product.ThumbnailUrl,
                LastPurchaseDate = schedule.LastPurchaseDate,
                EstimatedVolumeMl = schedule.EstimatedVolumeMl,
                DailyUsageMl = schedule.DailyUsageMl,
                EstimatedDepletionDate = schedule.EstimatedDepletionDate,
                DaysRemaining = Math.Max(0, (int)(schedule.EstimatedDepletionDate.Date - DateTime.UtcNow.Date).TotalDays),
                IsNotified = schedule.IsNotified
            };
        }

        public async Task<List<RepurchaseAlertDto>> GetRepurchaseAlertsAsync(int daysThreshold = 7, CancellationToken ct = default)
        {
            var thresholdDate = DateTime.UtcNow.Date.AddDays(daysThreshold);

            var schedules = await _context.RepurchaseSchedules
                .AsNoTracking()
                .Include(rs => rs.Product)
                .Include(rs => rs.CustomerProfile)
                    .ThenInclude(cp => cp.User)
                .Where(rs => rs.EstimatedDepletionDate.Date <= thresholdDate && !rs.IsNotified)
                .OrderBy(rs => rs.EstimatedDepletionDate)
                .ToListAsync(ct);

            return schedules.Select(s => new RepurchaseAlertDto
            {
                ScheduleId = s.Id,
                CustomerProfileId = s.CustomerProfileId,
                UserId = s.CustomerProfile.UserId,
                CustomerName = s.CustomerProfile.User.FullName ?? s.CustomerProfile.User.Username,
                CustomerPhone = s.CustomerProfile.User.PhoneNumber,
                ProductId = s.ProductId,
                ProductName = s.Product?.Name ?? "Sản phẩm",
                ProductThumbnailUrl = s.Product?.ThumbnailUrl,
                LastPurchaseDate = s.LastPurchaseDate,
                EstimatedVolumeMl = s.EstimatedVolumeMl,
                DailyUsageMl = s.DailyUsageMl,
                EstimatedDepletionDate = s.EstimatedDepletionDate,
                DaysRemaining = Math.Max(0, (int)(s.EstimatedDepletionDate.Date - DateTime.UtcNow.Date).TotalDays),
                IsNotified = s.IsNotified
            }).ToList();
        }

        private static CustomerProfileDto MapToProfileDto(CustomerProfile cp)
        {
            List<string> concerns = new();
            List<string> allergies = new();
            List<string> brands = new();

            try { if (!string.IsNullOrWhiteSpace(cp.SkinConcerns)) concerns = JsonSerializer.Deserialize<List<string>>(cp.SkinConcerns) ?? new(); } catch { }
            try { if (!string.IsNullOrWhiteSpace(cp.KnownAllergies)) allergies = JsonSerializer.Deserialize<List<string>>(cp.KnownAllergies) ?? new(); } catch { }
            try { if (!string.IsNullOrWhiteSpace(cp.PreferredBrands)) brands = JsonSerializer.Deserialize<List<string>>(cp.PreferredBrands) ?? new(); } catch { }

            var histories = cp.SkinHistories.Select(sh =>
            {
                List<string> imgs = new();
                try { if (!string.IsNullOrWhiteSpace(sh.ImageUrls)) imgs = JsonSerializer.Deserialize<List<string>>(sh.ImageUrls) ?? new(); } catch { }

                return new SkinHistoryDto
                {
                    Id = sh.Id,
                    CustomerProfileId = sh.CustomerProfileId,
                    RecordedAt = sh.RecordedAt,
                    SkinType = sh.SkinType,
                    SkinCondition = sh.SkinCondition,
                    TreatmentNote = sh.TreatmentNote,
                    RecordedBy = sh.RecordedBy,
                    RecordedByName = sh.RecordedByUser?.FullName ?? sh.RecordedByUser?.Username,
                    ImageUrls = imgs
                };
            }).ToList();

            return new CustomerProfileDto
            {
                Id = cp.Id,
                UserId = cp.UserId,
                CustomerName = cp.User?.FullName ?? cp.User?.Username ?? "Khách hàng",
                CustomerPhone = cp.User?.PhoneNumber,
                CustomerEmail = cp.User?.Email,
                DateOfBirth = cp.DateOfBirth,
                Gender = cp.Gender,
                CurrentSkinType = cp.CurrentSkinType,
                SkinConcerns = concerns,
                KnownAllergies = allergies,
                PreferredBrands = brands,
                Note = cp.Note,
                LastConsultationDate = cp.LastConsultationDate,
                ConsultantUserId = cp.ConsultantUserId,
                ConsultantName = cp.Consultant?.FullName ?? cp.Consultant?.Username,
                CreatedAt = cp.CreatedAt,
                RecentSkinHistories = histories
            };
        }
    }
}
