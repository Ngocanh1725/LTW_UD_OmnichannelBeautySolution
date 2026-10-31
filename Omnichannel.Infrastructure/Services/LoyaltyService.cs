using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Loyalty;
using Omnichannel.Application.Interfaces.Loyalty;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services
{
    public class LoyaltyService : ILoyaltyService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LoyaltyService> _logger;

        public LoyaltyService(ApplicationDbContext context, ILogger<LoyaltyService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<LoyaltyWalletDto> GetOrCreateWalletAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException($"Không tìm thấy người dùng với ID {userId}.");
            }

            var wallet = await _context.LoyaltyWallets
                .Include(w => w.User)
                .Include(w => w.CurrentTier)
                .Include(w => w.Transactions.OrderByDescending(t => t.CreatedAt).Take(15))
                    .ThenInclude(t => t.Order)
                .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

            if (wallet == null)
            {
                var defaultTier = await _context.MemberTiers
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.SortOrder)
                    .FirstOrDefaultAsync(cancellationToken);

                if (defaultTier == null)
                {
                    // Tự động tạo Silver tier nếu DB chưa seed
                    defaultTier = new MemberTier
                    {
                        TierName = "Silver",
                        MinSpend = 0,
                        MaxSpend = 2000000m,
                        PointMultiplier = 1.0m,
                        BirthdayBonusPoints = 50,
                        Description = "Hạng Bạc - Khách hàng mới tham gia",
                        SortOrder = 1,
                        IsActive = true
                    };
                    await _context.MemberTiers.AddAsync(defaultTier, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                wallet = new LoyaltyWallet
                {
                    UserId = userId,
                    CurrentTierId = defaultTier.Id,
                    TotalPointsEarned = 0,
                    CurrentBalance = 0,
                    TotalSpend12Months = 0,
                    MemberSince = DateTime.UtcNow,
                    QrCodeData = $"BEAUTY-MEM-{userId:D6}",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.LoyaltyWallets.AddAsync(wallet, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                wallet.CurrentTier = defaultTier;
                wallet.User = user;
            }

            return await MapToWalletDtoAsync(wallet, cancellationToken);
        }

        public async Task<LoyaltyWalletDto?> GetWalletAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await GetOrCreateWalletAsync(userId, cancellationToken);
        }

        public async Task<EarnPointsDto> EarnPointsAsync(int userId, int orderId, decimal orderAmount, CancellationToken cancellationToken = default)
        {
            if (orderAmount <= 0)
            {
                return new EarnPointsDto
                {
                    UserId = userId,
                    OrderId = orderId,
                    OrderAmount = orderAmount,
                    BasePoints = 0,
                    PointsEarned = 0,
                    NewBalance = 0,
                    TierMultiplier = 1.0m,
                    TierName = "Silver"
                };
            }

            // Kiểm tra xem đơn hàng đã từng được tích điểm chưa để tránh duplicate
            var existingEarn = await _context.PointTransactions
                .AnyAsync(pt => pt.OrderId == orderId && pt.TransactionType == "EARN", cancellationToken);

            if (existingEarn)
            {
                _logger.LogWarning("Đơn hàng #{OrderId} đã được tích điểm trước đó cho UserId {UserId}.", orderId, userId);
                var currentWallet = await _context.LoyaltyWallets
                    .Include(w => w.CurrentTier)
                    .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

                return new EarnPointsDto
                {
                    UserId = userId,
                    OrderId = orderId,
                    OrderAmount = orderAmount,
                    BasePoints = 0,
                    PointsEarned = 0,
                    NewBalance = currentWallet?.CurrentBalance ?? 0,
                    TierMultiplier = currentWallet?.CurrentTier?.PointMultiplier ?? 1.0m,
                    TierName = currentWallet?.CurrentTier?.TierName ?? "Silver"
                };
            }

            var wallet = await _context.LoyaltyWallets
                .Include(w => w.CurrentTier)
                .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

            if (wallet == null)
            {
                await GetOrCreateWalletAsync(userId, cancellationToken);
                wallet = await _context.LoyaltyWallets
                    .Include(w => w.CurrentTier)
                    .FirstAsync(w => w.UserId == userId, cancellationToken);
            }

            // Quy tắc: mỗi 10.000 VNĐ = 1 điểm cơ sở
            int basePoints = (int)Math.Floor(orderAmount / 10000m);
            decimal multiplier = wallet.CurrentTier?.PointMultiplier ?? 1.0m;
            int pointsEarned = (int)Math.Round(basePoints * multiplier);

            if (pointsEarned <= 0)
            {
                pointsEarned = 0;
            }

            int balanceBefore = wallet.CurrentBalance;
            int balanceAfter = balanceBefore + pointsEarned;

            wallet.CurrentBalance = balanceAfter;
            wallet.TotalPointsEarned += pointsEarned;
            wallet.UpdatedAt = DateTime.UtcNow;

            var transaction = new PointTransaction
            {
                WalletId = wallet.Id,
                OrderId = orderId,
                TransactionType = "EARN",
                Points = pointsEarned,
                BalanceBefore = balanceBefore,
                BalanceAfter = balanceAfter,
                Description = $"Tích điểm đơn hàng #{orderId} (+{pointsEarned} điểm, hệ số x{multiplier:0.0})",
                ExpiresAt = DateTime.UtcNow.AddMonths(12),
                CreatedAt = DateTime.UtcNow
            };

            await _context.PointTransactions.AddAsync(transaction, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Tích thành công {Points} điểm cho User {UserId} từ đơn hàng #{OrderId}. Số dư mới: {NewBalance}",
                pointsEarned, userId, orderId, balanceAfter);

            // Tự động đánh giá lại hạng sau khi phát sinh đơn hàng
            try
            {
                await EvaluateTierAsync(userId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tự động đánh giá hạng thành viên cho User {UserId}", userId);
            }

            return new EarnPointsDto
            {
                UserId = userId,
                OrderId = orderId,
                OrderAmount = orderAmount,
                BasePoints = basePoints,
                PointsEarned = pointsEarned,
                NewBalance = wallet.CurrentBalance,
                TierMultiplier = multiplier,
                TierName = wallet.CurrentTier?.TierName ?? "Silver"
            };
        }

        public async Task<RedeemPointsDto> RedeemPointsAsync(int userId, int points, string description, CancellationToken cancellationToken = default)
        {
            if (points <= 0)
            {
                throw new ArgumentException("Số điểm đổi phải lớn hơn 0.", nameof(points));
            }

            var wallet = await _context.LoyaltyWallets
                .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

            if (wallet == null)
            {
                throw new InvalidOperationException($"Người dùng {userId} chưa có ví điểm.");
            }

            if (wallet.CurrentBalance < points)
            {
                throw new InvalidOperationException($"Số điểm khả dụng ({wallet.CurrentBalance}) không đủ để thực hiện đổi {points} điểm.");
            }

            // Quy tắc: 1 điểm = 1.000 VNĐ
            decimal discountAmount = points * 1000m;
            int balanceBefore = wallet.CurrentBalance;
            int balanceAfter = balanceBefore - points;

            wallet.CurrentBalance = balanceAfter;
            wallet.UpdatedAt = DateTime.UtcNow;

            string voucherCode = $"LOYALTY-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
            string desc = string.IsNullOrWhiteSpace(description)
                ? $"Đổi {points} điểm nhận Voucher {discountAmount:N0} VNĐ"
                : description;

            var pointTx = new PointTransaction
            {
                WalletId = wallet.Id,
                TransactionType = "REDEEM",
                Points = -points,
                BalanceBefore = balanceBefore,
                BalanceAfter = balanceAfter,
                Description = desc,
                CreatedAt = DateTime.UtcNow
            };

            var voucher = new Voucher
            {
                UserId = userId,
                VoucherCode = voucherCode,
                Description = desc,
                DiscountType = "FIXED_AMOUNT",
                DiscountValue = discountAmount,
                MinOrderValue = discountAmount,
                MaxDiscountAmount = discountAmount,
                ValidFrom = DateTime.UtcNow,
                ValidTo = DateTime.UtcNow.AddMonths(1),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            await _context.PointTransactions.AddAsync(pointTx, cancellationToken);
            await _context.Vouchers.AddAsync(voucher, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} đã đổi {Points} điểm thành Voucher {VoucherCode} trị giá {Amount} VNĐ",
                userId, points, voucherCode, discountAmount);

            return new RedeemPointsDto
            {
                UserId = userId,
                PointsToRedeem = points,
                DiscountAmountVnd = discountAmount,
                NewBalance = balanceAfter,
                VoucherCode = voucherCode,
                Message = $"Đổi thành công {points} điểm sang mã voucher {voucherCode} giảm {discountAmount:N0} VNĐ."
            };
        }

        public async Task<TierInfoDto> EvaluateTierAsync(int userId, CancellationToken cancellationToken = default)
        {
            var wallet = await _context.LoyaltyWallets
                .Include(w => w.CurrentTier)
                .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

            if (wallet == null)
            {
                await GetOrCreateWalletAsync(userId, cancellationToken);
                wallet = await _context.LoyaltyWallets
                    .Include(w => w.CurrentTier)
                    .FirstAsync(w => w.UserId == userId, cancellationToken);
            }

            // Tính tổng chi tiêu 12 tháng gần nhất từ các đơn hàng PAID/COMPLETED
            var twelveMonthsAgo = DateTime.UtcNow.AddMonths(-12);
            var totalSpend = await _context.Orders
                .AsNoTracking()
                .Where(o => o.UserId == userId && (o.PaymentStatus == "PAID" || o.OrderStatus == "COMPLETED") && o.CreatedAt >= twelveMonthsAgo)
                .SumAsync(o => (decimal?)o.TotalAmount, cancellationToken) ?? 0m;

            wallet.TotalSpend12Months = totalSpend;
            wallet.LastTierEvaluationDate = DateTime.UtcNow;

            var tiers = await _context.MemberTiers
                .Where(t => t.IsActive)
                .OrderBy(t => t.MinSpend)
                .ToListAsync(cancellationToken);

            var qualifyingTier = tiers
                .Where(t => totalSpend >= t.MinSpend && (!t.MaxSpend.HasValue || totalSpend <= t.MaxSpend.Value))
                .OrderByDescending(t => t.MinSpend)
                .FirstOrDefault() ?? tiers.FirstOrDefault();

            if (qualifyingTier != null && qualifyingTier.Id != wallet.CurrentTierId)
            {
                var oldTierName = wallet.CurrentTier?.TierName ?? "Silver";
                wallet.CurrentTierId = qualifyingTier.Id;
                wallet.UpdatedAt = DateTime.UtcNow;

                var upgradeLog = new PointTransaction
                {
                    WalletId = wallet.Id,
                    TransactionType = "ADJUST",
                    Points = 0,
                    BalanceBefore = wallet.CurrentBalance,
                    BalanceAfter = wallet.CurrentBalance,
                    Description = $"Thăng hạng thành viên: {oldTierName} -> {qualifyingTier.TierName} (Tổng chi tiêu 12T: {totalSpend:N0} VNĐ)",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.PointTransactions.AddAsync(upgradeLog, cancellationToken);
                _logger.LogInformation("User {UserId} được nâng hạng từ {OldTier} lên {NewTier}",
                    userId, oldTierName, qualifyingTier.TierName);
            }

            await _context.SaveChangesAsync(cancellationToken);

            var activeTier = qualifyingTier ?? wallet.CurrentTier ?? await _context.MemberTiers.OrderBy(t => t.SortOrder).FirstAsync(cancellationToken);
            return new TierInfoDto
            {
                Id = activeTier.Id,
                TierName = activeTier.TierName,
                MinSpend = activeTier.MinSpend,
                MaxSpend = activeTier.MaxSpend,
                PointMultiplier = activeTier.PointMultiplier,
                BirthdayBonusPoints = activeTier.BirthdayBonusPoints,
                Description = activeTier.Description,
                BadgeIconUrl = activeTier.BadgeIconUrl,
                SortOrder = activeTier.SortOrder,
                IsActive = activeTier.IsActive
            };
        }

        public async Task<List<VoucherDto>> GetUserVouchersAsync(int userId, bool onlyValid = true, CancellationToken cancellationToken = default)
        {
            var query = _context.Vouchers
                .AsNoTracking()
                .Include(v => v.UsedOrder)
                .Where(v => v.UserId == userId || v.UserId == null);

            if (onlyValid)
            {
                var now = DateTime.UtcNow;
                query = query.Where(v => !v.IsUsed && v.ValidFrom <= now && v.ValidTo >= now);
            }

            var vouchers = await query
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync(cancellationToken);

            return vouchers.Select(v => new VoucherDto
            {
                Id = v.Id,
                UserId = v.UserId,
                VoucherCode = v.VoucherCode,
                Description = v.Description,
                DiscountType = v.DiscountType,
                DiscountValue = v.DiscountValue,
                MinOrderValue = v.MinOrderValue,
                MaxDiscountAmount = v.MaxDiscountAmount,
                ValidFrom = v.ValidFrom,
                ValidTo = v.ValidTo,
                IsUsed = v.IsUsed,
                UsedAt = v.UsedAt,
                UsedOrderId = v.UsedOrderId,
                UsedOrderCode = v.UsedOrder?.OrderCode
            }).ToList();
        }

        public async Task<string> GenerateMemberQrAsync(int userId, CancellationToken cancellationToken = default)
        {
            var wallet = await _context.LoyaltyWallets
                .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

            string qrCode = $"BEAUTY-MEM-{userId:D6}";
            if (wallet != null)
            {
                wallet.QrCodeData = qrCode;
                wallet.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return qrCode;
        }

        private async Task<LoyaltyWalletDto> MapToWalletDtoAsync(LoyaltyWallet wallet, CancellationToken cancellationToken)
        {
            // Tìm hạng tiếp theo
            decimal currentMinSpend = wallet.CurrentTier?.MinSpend ?? 0m;
            var nextTier = await _context.MemberTiers
                .Where(t => t.IsActive && t.MinSpend > currentMinSpend)
                .OrderBy(t => t.MinSpend)
                .FirstOrDefaultAsync(cancellationToken);

            decimal? spendNeeded = null;
            if (nextTier != null)
            {
                spendNeeded = Math.Max(0, nextTier.MinSpend - wallet.TotalSpend12Months);
            }

            var recentTx = wallet.Transactions.Select(t => new PointTransactionDto
            {
                Id = t.Id,
                WalletId = t.WalletId,
                OrderId = t.OrderId,
                OrderCode = t.Order?.OrderCode,
                TransactionType = t.TransactionType,
                Points = t.Points,
                BalanceBefore = t.BalanceBefore,
                BalanceAfter = t.BalanceAfter,
                Description = t.Description,
                ExpiresAt = t.ExpiresAt,
                CreatedAt = t.CreatedAt
            }).ToList();

            return new LoyaltyWalletDto
            {
                Id = wallet.Id,
                UserId = wallet.UserId,
                CustomerName = wallet.User?.FullName ?? wallet.User?.Username ?? "Khách hàng",
                CustomerPhone = wallet.User?.PhoneNumber,
                CurrentTier = wallet.CurrentTier != null ? new TierInfoDto
                {
                    Id = wallet.CurrentTier.Id,
                    TierName = wallet.CurrentTier.TierName,
                    MinSpend = wallet.CurrentTier.MinSpend,
                    MaxSpend = wallet.CurrentTier.MaxSpend,
                    PointMultiplier = wallet.CurrentTier.PointMultiplier,
                    BirthdayBonusPoints = wallet.CurrentTier.BirthdayBonusPoints,
                    Description = wallet.CurrentTier.Description,
                    BadgeIconUrl = wallet.CurrentTier.BadgeIconUrl,
                    SortOrder = wallet.CurrentTier.SortOrder,
                    IsActive = wallet.CurrentTier.IsActive
                } : null,
                TotalPointsEarned = wallet.TotalPointsEarned,
                CurrentBalance = wallet.CurrentBalance,
                TotalSpend12Months = wallet.TotalSpend12Months,
                MemberSince = wallet.MemberSince,
                LastTierEvaluationDate = wallet.LastTierEvaluationDate,
                NextTierName = nextTier?.TierName,
                SpendNeededForNextTier = spendNeeded,
                QrCodeData = wallet.QrCodeData ?? $"BEAUTY-MEM-{wallet.UserId:D6}",
                RecentTransactions = recentTx
            };
        }

        public async Task<List<LoyaltyWalletDto>> GetAllWalletsAsync(string? search = null, int? tierId = null, CancellationToken cancellationToken = default)
        {
            // Tự động khởi tạo ví cho các khách hàng chưa có ví
            var existingUserIdsInWallets = await _context.LoyaltyWallets
                .Select(w => w.UserId)
                .ToListAsync(cancellationToken);

            var customerUsersWithoutWallet = await _context.Users
                .Where(u => !existingUserIdsInWallets.Contains(u.Id))
                .Take(50)
                .ToListAsync(cancellationToken);

            if (customerUsersWithoutWallet.Any())
            {
                var defaultTier = await _context.MemberTiers
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.SortOrder)
                    .FirstOrDefaultAsync(cancellationToken);

                if (defaultTier != null)
                {
                    foreach (var u in customerUsersWithoutWallet)
                    {
                        _context.LoyaltyWallets.Add(new LoyaltyWallet
                        {
                            UserId = u.Id,
                            CurrentTierId = defaultTier.Id,
                            TotalPointsEarned = 0,
                            CurrentBalance = 0,
                            TotalSpend12Months = 0,
                            MemberSince = u.CreatedAt,
                            QrCodeData = $"BEAUTY-MEM-{u.Id:D6}",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            var query = _context.LoyaltyWallets
                .AsNoTracking()
                .Include(w => w.User)
                .Include(w => w.CurrentTier)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(w =>
                    (w.User.FullName != null && w.User.FullName.Contains(search)) ||
                    w.User.Username.Contains(search) ||
                    (w.User.PhoneNumber != null && w.User.PhoneNumber.Contains(search)) ||
                    (w.User.Email != null && w.User.Email.Contains(search)));
            }

            if (tierId.HasValue && tierId.Value > 0)
            {
                query = query.Where(w => w.CurrentTierId == tierId.Value);
            }

            var wallets = await query
                .OrderByDescending(w => w.TotalSpend12Months)
                .ThenByDescending(w => w.CurrentBalance)
                .ToListAsync(cancellationToken);

            var dtos = new List<LoyaltyWalletDto>();
            foreach (var w in wallets)
            {
                dtos.Add(await MapToWalletDtoAsync(w, cancellationToken));
            }

            return dtos;
        }

        public async Task<bool> AdjustPointsAsync(AdjustPointsRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Points == 0) return true;

            var wallet = await _context.LoyaltyWallets
                .Include(w => w.CurrentTier)
                .FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken);

            if (wallet == null)
            {
                await GetOrCreateWalletAsync(request.UserId, cancellationToken);
                wallet = await _context.LoyaltyWallets
                    .Include(w => w.CurrentTier)
                    .FirstAsync(w => w.UserId == request.UserId, cancellationToken);
            }

            int balanceBefore = wallet.CurrentBalance;
            int balanceAfter = Math.Max(0, balanceBefore + request.Points);

            wallet.CurrentBalance = balanceAfter;
            if (request.Points > 0)
            {
                wallet.TotalPointsEarned += request.Points;
            }
            wallet.UpdatedAt = DateTime.UtcNow;

            string desc = $"Điều chỉnh thủ công bởi Quản trị viên: {request.Reason}";
            if (!string.IsNullOrWhiteSpace(request.Note))
            {
                desc += $" ({request.Note.Trim()})";
            }

            var transaction = new PointTransaction
            {
                WalletId = wallet.Id,
                TransactionType = "ADJUST",
                Points = request.Points,
                BalanceBefore = balanceBefore,
                BalanceAfter = balanceAfter,
                Description = desc,
                CreatedAt = DateTime.UtcNow
            };

            await _context.PointTransactions.AddAsync(transaction, cancellationToken);
            var result = await _context.SaveChangesAsync(cancellationToken) > 0;

            _logger.LogInformation("Quản trị viên đã điều chỉnh {Points} điểm cho User {UserId}. Lý do: {Reason}. Số dư: {Before} -> {After}",
                request.Points, request.UserId, request.Reason, balanceBefore, balanceAfter);

            return result;
        }

        public async Task<LoyaltyWalletDto?> GetWalletDetailsAsync(int walletId, CancellationToken cancellationToken = default)
        {
            var wallet = await _context.LoyaltyWallets
                .Include(w => w.User)
                .Include(w => w.CurrentTier)
                .Include(w => w.Transactions.OrderByDescending(t => t.CreatedAt).Take(100))
                    .ThenInclude(t => t.Order)
                .FirstOrDefaultAsync(w => w.Id == walletId || w.UserId == walletId, cancellationToken);

            if (wallet == null) return null;

            return await MapToWalletDtoAsync(wallet, cancellationToken);
        }

        public async Task<List<TierInfoDto>> GetAllTiersAsync(CancellationToken cancellationToken = default)
        {
            var tiers = await _context.MemberTiers
                .AsNoTracking()
                .OrderBy(t => t.SortOrder)
                .ThenBy(t => t.MinSpend)
                .ToListAsync(cancellationToken);

            return tiers.Select(t => new TierInfoDto
            {
                Id = t.Id,
                TierName = t.TierName,
                MinSpend = t.MinSpend,
                MaxSpend = t.MaxSpend,
                PointMultiplier = t.PointMultiplier,
                BirthdayBonusPoints = t.BirthdayBonusPoints,
                Description = t.Description,
                BadgeIconUrl = t.BadgeIconUrl,
                SortOrder = t.SortOrder,
                IsActive = t.IsActive
            }).ToList();
        }

        public async Task<bool> UpdateTierAsync(TierInfoDto tierDto, CancellationToken cancellationToken = default)
        {
            var tier = await _context.MemberTiers.FindAsync(new object[] { tierDto.Id }, cancellationToken);
            if (tier == null) return false;

            tier.TierName = tierDto.TierName.Trim();
            tier.MinSpend = tierDto.MinSpend;
            tier.MaxSpend = tierDto.MaxSpend;
            tier.PointMultiplier = tierDto.PointMultiplier;
            tier.BirthdayBonusPoints = tierDto.BirthdayBonusPoints;
            tier.Description = tierDto.Description?.Trim();
            tier.SortOrder = tierDto.SortOrder;
            tier.IsActive = tierDto.IsActive;

            return await _context.SaveChangesAsync(cancellationToken) > 0;
        }

        public async Task<LoyaltySummaryDto> GetLoyaltySummaryAsync(CancellationToken cancellationToken = default)
        {
            var wallets = await _context.LoyaltyWallets
                .AsNoTracking()
                .Include(w => w.CurrentTier)
                .ToListAsync(cancellationToken);

            return new LoyaltySummaryDto
            {
                TotalMembers = wallets.Count,
                TotalActivePoints = wallets.Sum(w => w.CurrentBalance),
                TotalLifetimePoints = wallets.Sum(w => w.TotalPointsEarned),
                SilverCount = wallets.Count(w => w.CurrentTier != null && w.CurrentTier.TierName.Equals("Silver", StringComparison.OrdinalIgnoreCase)),
                GoldCount = wallets.Count(w => w.CurrentTier != null && w.CurrentTier.TierName.Equals("Gold", StringComparison.OrdinalIgnoreCase)),
                DiamondCount = wallets.Count(w => w.CurrentTier != null && w.CurrentTier.TierName.Equals("Diamond", StringComparison.OrdinalIgnoreCase)),
                TotalSpendOverall = wallets.Sum(w => w.TotalSpend12Months)
            };
        }
    }
}

