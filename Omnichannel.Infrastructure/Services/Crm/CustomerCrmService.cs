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
using Omnichannel.Application.Interfaces.Loyalty;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services.Crm
{
    public class CustomerCrmService : ICustomerCrmService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILoyaltyService _loyaltyService;
        private readonly ILogger<CustomerCrmService> _logger;

        public CustomerCrmService(
            ApplicationDbContext context,
            ILoyaltyService loyaltyService,
            ILogger<CustomerCrmService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CustomerPagedListViewModel> GetCustomersPagedAsync(CustomerFilterRequest filter, CancellationToken ct = default)
        {
            filter ??= new CustomerFilterRequest();
            if (filter.PageNumber < 1) filter.PageNumber = 1;
            if (filter.PageSize <= 0) filter.PageSize = 10;

            // 1. Base query: Users who are customers (UserType == 1 or has Role 'Customer' or has LoyaltyWallet/CustomerProfile)
            var baseQuery = _context.Users
                .AsNoTracking()
                .Where(u => u.UserType == 1 
                         || u.UserRoles.Any(ur => ur.Role.Name == "Customer")
                         || u.LoyaltyWallet != null 
                         || u.CustomerProfile != null);

            // 2. Fetch available member tiers
            var tiers = await _context.MemberTiers
                .AsNoTracking()
                .Where(t => t.IsActive)
                .OrderBy(t => t.SortOrder)
                .ToListAsync(ct);

            // 3. Count by Tier for top tabs
            var allCount = await baseQuery.CountAsync(ct);
            var tierCounts = new Dictionary<string, int>
            {
                { "All", allCount }
            };

            foreach (var tier in tiers)
            {
                var count = await baseQuery.CountAsync(u => u.LoyaltyWallet != null && u.LoyaltyWallet.CurrentTierId == tier.Id, ct);
                tierCounts[tier.TierName] = count;
            }

            // 4. Apply Filters
            var query = baseQuery;

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim().ToLower();
                query = query.Where(u =>
                    u.FullName.ToLower().Contains(kw) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(kw)) ||
                    u.Email.ToLower().Contains(kw) ||
                    u.Username.ToLower().Contains(kw));
            }

            if (filter.TierId.HasValue && filter.TierId.Value > 0)
            {
                query = query.Where(u => u.LoyaltyWallet != null && u.LoyaltyWallet.CurrentTierId == filter.TierId.Value);
            }

            // 5. Apply Sorting
            switch (filter.SortBy?.ToLowerInvariant())
            {
                case "spend_asc":
                    query = query.OrderBy(u => u.LoyaltyWallet != null ? u.LoyaltyWallet.TotalSpend12Months : 0);
                    break;
                case "points_desc":
                    query = query.OrderByDescending(u => u.LoyaltyWallet != null ? u.LoyaltyWallet.CurrentBalance : 0);
                    break;
                case "points_asc":
                    query = query.OrderBy(u => u.LoyaltyWallet != null ? u.LoyaltyWallet.CurrentBalance : 0);
                    break;
                case "newest":
                    query = query.OrderByDescending(u => u.CreatedAt);
                    break;
                case "oldest":
                    query = query.OrderBy(u => u.CreatedAt);
                    break;
                case "name_asc":
                    query = query.OrderBy(u => u.FullName);
                    break;
                case "spend_desc":
                default:
                    query = query.OrderByDescending(u => u.LoyaltyWallet != null ? u.LoyaltyWallet.TotalSpend12Months : 0);
                    break;
            }

            // 6. Pagination
            var totalCount = await query.CountAsync(ct);

            var items = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(u => new CustomerListItemDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    FullName = u.FullName,
                    PhoneNumber = u.PhoneNumber,
                    Email = u.Email,
                    AvatarUrl = u.AvatarUrl,
                    TierName = u.LoyaltyWallet != null ? u.LoyaltyWallet.CurrentTier.TierName : "Silver",
                    TierBadgeIcon = u.LoyaltyWallet != null ? u.LoyaltyWallet.CurrentTier.BadgeIconUrl : null,
                    CurrentPoints = u.LoyaltyWallet != null ? u.LoyaltyWallet.CurrentBalance : 0,
                    TotalSpend12Months = u.LoyaltyWallet != null ? u.LoyaltyWallet.TotalSpend12Months : 0,
                    TotalSpendAllTime = u.Orders.Where(o => o.OrderStatus != "CANCELLED").Sum(o => (decimal?)o.TotalAmount) ?? 0,
                    TotalOrders = u.Orders.Count,
                    MemberSince = u.LoyaltyWallet != null ? u.LoyaltyWallet.MemberSince : u.CreatedAt,
                    IsActive = u.IsActive,
                    CurrentSkinType = u.CustomerProfile != null ? u.CustomerProfile.CurrentSkinType : null
                })
                .ToListAsync(ct);

            var tierOptions = tiers.Select(t => new MemberTierOptionDto
            {
                Id = t.Id,
                TierName = t.TierName,
                MinSpend = t.MinSpend,
                MaxSpend = t.MaxSpend,
                PointMultiplier = t.PointMultiplier,
                MemberCount = tierCounts.TryGetValue(t.TierName, out var c) ? c : 0
            }).ToList();

            return new CustomerPagedListViewModel
            {
                Items = items,
                Filter = filter,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TierCounts = tierCounts,
                AvailableTiers = tierOptions
            };
        }

        public async Task<Customer360ProfileViewModel?> GetCustomer360ProfileAsync(int userId, CancellationToken ct = default)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.LoyaltyWallet)
                    .ThenInclude(w => w!.CurrentTier)
                .Include(u => u.CustomerProfile)
                    .ThenInclude(cp => cp!.Consultant)
                .Include(u => u.CustomerProfile)
                    .ThenInclude(cp => cp!.SkinHistories)
                        .ThenInclude(sh => sh.RecordedByUser)
                .Include(u => u.Orders)
                    .ThenInclude(o => o.OrderDetails)
                .FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user == null)
            {
                return null;
            }

            // Ensure loyalty wallet exists if user is missing one
            if (user.LoyaltyWallet == null)
            {
                await _loyaltyService.GetOrCreateWalletAsync(userId, ct);
                // Reload wallet
                var wallet = await _context.LoyaltyWallets
                    .AsNoTracking()
                    .Include(w => w.CurrentTier)
                    .FirstOrDefaultAsync(w => w.UserId == userId, ct);
                user.LoyaltyWallet = wallet;
            }

            var walletObj = user.LoyaltyWallet;
            var currentTier = walletObj?.CurrentTier;

            // Fetch Point Transactions
            var pointTransactions = new List<CustomerPointTransactionDto>();
            if (walletObj != null)
            {
                pointTransactions = await _context.PointTransactions
                    .AsNoTracking()
                    .Where(pt => pt.WalletId == walletObj.Id)
                    .OrderByDescending(pt => pt.CreatedAt)
                    .Take(50)
                    .Select(pt => new CustomerPointTransactionDto
                    {
                        Id = pt.Id,
                        TransactionType = pt.TransactionType,
                        Points = pt.Points,
                        BalanceBefore = pt.BalanceBefore,
                        BalanceAfter = pt.BalanceAfter,
                        Description = pt.Description,
                        CreatedAt = pt.CreatedAt
                    })
                    .ToListAsync(ct);
            }

            // Calculate Order metrics
            var nonCancelledOrders = user.Orders.Where(o => o.OrderStatus != "CANCELLED").ToList();
            var totalOrdersCount = user.Orders.Count;
            var completedOrdersCount = user.Orders.Count(o => o.OrderStatus == "COMPLETED");
            var totalSpendAllTime = nonCancelledOrders.Sum(o => o.TotalAmount);
            var totalSpend12Months = walletObj?.TotalSpend12Months ?? 0;
            var avgOrderValue = nonCancelledOrders.Any() ? totalSpendAllTime / nonCancelledOrders.Count : 0;

            // Calculate Next Tier info
            var allTiers = await _context.MemberTiers
                .AsNoTracking()
                .Where(t => t.IsActive)
                .OrderBy(t => t.MinSpend)
                .ToListAsync(ct);

            var nextTier = allTiers.FirstOrDefault(t => t.MinSpend > (currentTier?.MinSpend ?? 0));
            decimal spendNeeded = 0;
            string? nextTierName = null;
            if (nextTier != null)
            {
                nextTierName = nextTier.TierName;
                spendNeeded = Math.Max(0, nextTier.MinSpend - totalSpend12Months);
            }

            // 12-Month Monthly Spending Trend (from 11 months ago to current month)
            var monthlyLabels = new List<string>();
            var monthlyValues = new List<decimal>();
            var now = DateTime.UtcNow;

            for (int i = 11; i >= 0; i--)
            {
                var targetMonth = now.AddMonths(-i);
                var label = $"T{targetMonth.Month:D2}/{targetMonth.Year.ToString().Substring(2)}";
                monthlyLabels.Add(label);

                var monthSpend = user.Orders
                    .Where(o => o.OrderStatus != "CANCELLED" 
                             && o.CreatedAt.Year == targetMonth.Year 
                             && o.CreatedAt.Month == targetMonth.Month)
                    .Sum(o => o.TotalAmount);

                monthlyValues.Add(monthSpend);
            }

            // Customer Profile & Beauty details
            var profile = user.CustomerProfile;
            var skinConcerns = new List<string>();
            var allergies = new List<string>();
            var brands = new List<string>();

            if (profile != null)
            {
                try { if (!string.IsNullOrWhiteSpace(profile.SkinConcerns)) skinConcerns = JsonSerializer.Deserialize<List<string>>(profile.SkinConcerns) ?? new(); } catch { }
                try { if (!string.IsNullOrWhiteSpace(profile.KnownAllergies)) allergies = JsonSerializer.Deserialize<List<string>>(profile.KnownAllergies) ?? new(); } catch { }
                try { if (!string.IsNullOrWhiteSpace(profile.PreferredBrands)) brands = JsonSerializer.Deserialize<List<string>>(profile.PreferredBrands) ?? new(); } catch { }
            }

            var skinHistories = profile?.SkinHistories?
                .OrderByDescending(sh => sh.RecordedAt)
                .Select(sh => {
                    List<string> imgs = new();
                    try { if (!string.IsNullOrWhiteSpace(sh.ImageUrls)) imgs = JsonSerializer.Deserialize<List<string>>(sh.ImageUrls) ?? new(); } catch { }
                    return new CustomerSkinHistoryDto
                    {
                        Id = sh.Id,
                        RecordedAt = sh.RecordedAt,
                        SkinType = sh.SkinType,
                        SkinCondition = sh.SkinCondition,
                        TreatmentNote = sh.TreatmentNote,
                        RecordedByName = sh.RecordedByUser?.FullName ?? "Chuyên viên",
                        ImageUrls = imgs
                    };
                }).ToList() ?? new List<CustomerSkinHistoryDto>();

            // Orders list mapped
            var ordersList = user.Orders
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new CustomerOrderSummaryDto
                {
                    Id = o.Id,
                    OrderCode = o.OrderCode,
                    OrderSource = o.OrderSource,
                    CreatedAt = o.CreatedAt,
                    TotalAmount = o.TotalAmount,
                    OrderStatus = o.OrderStatus,
                    PaymentStatus = o.PaymentStatus,
                    PaymentMethod = o.PaymentMethod,
                    ItemsCount = o.OrderDetails?.Sum(od => od.Quantity) ?? 0
                })
                .ToList();

            return new Customer360ProfileViewModel
            {
                UserId = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                Gender = profile?.Gender,
                DateOfBirth = profile?.DateOfBirth,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                MemberSince = walletObj?.MemberSince ?? user.CreatedAt,
                QrCodeData = walletObj?.QrCodeData ?? $"LOYALTY-{user.Id}-MEMBER",

                TierId = currentTier?.Id ?? 0,
                TierName = currentTier?.TierName ?? "Silver",
                PointMultiplier = currentTier?.PointMultiplier ?? 1.0m,
                MinSpend = currentTier?.MinSpend ?? 0,
                MaxSpend = currentTier?.MaxSpend,
                CurrentPoints = walletObj?.CurrentBalance ?? 0,
                TotalPointsEarned = walletObj?.TotalPointsEarned ?? 0,
                TotalSpend12Months = totalSpend12Months,
                TotalSpendAllTime = totalSpendAllTime,
                TotalOrdersCount = totalOrdersCount,
                CompletedOrdersCount = completedOrdersCount,
                AverageOrderValue = avgOrderValue,
                SpendNeededForNextTier = spendNeeded,
                NextTierName = nextTierName,

                MonthlySpendingLabels = monthlyLabels,
                MonthlySpendingValues = monthlyValues,

                HasBeautyProfile = profile != null,
                ProfileId = profile?.Id,
                CurrentSkinType = profile?.CurrentSkinType,
                SkinConcerns = skinConcerns,
                KnownAllergies = allergies,
                PreferredBrands = brands,
                ConsultantNote = profile?.Note,
                LastConsultationDate = profile?.LastConsultationDate,
                ConsultantUserId = profile?.ConsultantUserId,
                ConsultantName = profile?.Consultant?.FullName,
                SkinHistories = skinHistories,

                Orders = ordersList,
                PointTransactions = pointTransactions
            };
        }

        public async Task<CustomerEditDto?> GetCustomerForEditAsync(int userId, CancellationToken ct = default)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.CustomerProfile)
                .FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user == null)
            {
                return null;
            }

            var profile = user.CustomerProfile;
            string concernsText = string.Empty;
            string allergiesText = string.Empty;
            string brandsText = string.Empty;

            if (profile != null)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(profile.SkinConcerns))
                    {
                        var list = JsonSerializer.Deserialize<List<string>>(profile.SkinConcerns);
                        if (list != null) concernsText = string.Join(", ", list);
                    }
                }
                catch { concernsText = profile.SkinConcerns ?? string.Empty; }

                try
                {
                    if (!string.IsNullOrWhiteSpace(profile.KnownAllergies))
                    {
                        var list = JsonSerializer.Deserialize<List<string>>(profile.KnownAllergies);
                        if (list != null) allergiesText = string.Join(", ", list);
                    }
                }
                catch { allergiesText = profile.KnownAllergies ?? string.Empty; }

                try
                {
                    if (!string.IsNullOrWhiteSpace(profile.PreferredBrands))
                    {
                        var list = JsonSerializer.Deserialize<List<string>>(profile.PreferredBrands);
                        if (list != null) brandsText = string.Join(", ", list);
                    }
                }
                catch { brandsText = profile.PreferredBrands ?? string.Empty; }
            }

            return new CustomerEditDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                IsActive = user.IsActive,
                Gender = profile?.Gender,
                DateOfBirth = profile?.DateOfBirth,
                CurrentSkinType = profile?.CurrentSkinType,
                SkinConcernsInput = concernsText,
                KnownAllergiesInput = allergiesText,
                PreferredBrandsInput = brandsText,
                Note = profile?.Note,
                ConsultantUserId = profile?.ConsultantUserId
            };
        }

        public async Task<bool> UpdateCustomerAsync(CustomerEditDto dto, CancellationToken ct = default)
        {
            if (dto == null) return false;

            var user = await _context.Users
                .Include(u => u.CustomerProfile)
                .FirstOrDefaultAsync(u => u.Id == dto.UserId, ct);

            if (user == null)
            {
                return false;
            }

            // Update user properties
            user.FullName = dto.FullName?.Trim() ?? string.Empty;
            user.PhoneNumber = dto.PhoneNumber?.Trim();
            user.Email = dto.Email?.Trim() ?? string.Empty;
            user.IsActive = dto.IsActive;
            if (!string.IsNullOrWhiteSpace(dto.AvatarUrl))
            {
                user.AvatarUrl = dto.AvatarUrl.Trim();
            }
            user.UpdatedAt = DateTime.UtcNow;

            // Parse beauty profile inputs into lists and JSON
            var concernsList = ParseTags(dto.SkinConcernsInput);
            var allergiesList = ParseTags(dto.KnownAllergiesInput);
            var brandsList = ParseTags(dto.PreferredBrandsInput);

            string? concernsJson = concernsList.Any() ? JsonSerializer.Serialize(concernsList) : null;
            string? allergiesJson = allergiesList.Any() ? JsonSerializer.Serialize(allergiesList) : null;
            string? brandsJson = brandsList.Any() ? JsonSerializer.Serialize(brandsList) : null;

            if (user.CustomerProfile == null)
            {
                user.CustomerProfile = new CustomerProfile
                {
                    UserId = user.Id,
                    Gender = dto.Gender,
                    DateOfBirth = dto.DateOfBirth,
                    CurrentSkinType = dto.CurrentSkinType,
                    SkinConcerns = concernsJson,
                    KnownAllergies = allergiesJson,
                    PreferredBrands = brandsJson,
                    Note = dto.Note,
                    ConsultantUserId = dto.ConsultantUserId,
                    CreatedAt = DateTime.UtcNow
                };
            }
            else
            {
                user.CustomerProfile.Gender = dto.Gender;
                user.CustomerProfile.DateOfBirth = dto.DateOfBirth;
                user.CustomerProfile.CurrentSkinType = dto.CurrentSkinType;
                user.CustomerProfile.SkinConcerns = concernsJson;
                user.CustomerProfile.KnownAllergies = allergiesJson;
                user.CustomerProfile.PreferredBrands = brandsJson;
                user.CustomerProfile.Note = dto.Note;
                user.CustomerProfile.ConsultantUserId = dto.ConsultantUserId;
                user.CustomerProfile.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<List<MemberTierOptionDto>> GetMemberTiersAsync(CancellationToken ct = default)
        {
            var tiers = await _context.MemberTiers
                .AsNoTracking()
                .Where(t => t.IsActive)
                .OrderBy(t => t.SortOrder)
                .Select(t => new MemberTierOptionDto
                {
                    Id = t.Id,
                    TierName = t.TierName,
                    MinSpend = t.MinSpend,
                    MaxSpend = t.MaxSpend,
                    PointMultiplier = t.PointMultiplier,
                    MemberCount = t.Wallets.Count
                })
                .ToListAsync(ct);

            return tiers;
        }

        private static List<string> ParseTags(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return new List<string>();
            return input
                .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .ToList();
        }
    }
}
