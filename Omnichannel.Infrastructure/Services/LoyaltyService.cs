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

        /// <summary>
        /// Mốc điểm thưởng điểm danh 7 ngày liên tiếp: 10 - 10 - 20 - 20 - 30 - 30 - 100
        /// </summary>
        public static readonly int[] DailyCheckInPoints = { 10, 10, 20, 20, 30, 30, 100 };

        public static DateTime GetVietnamDate(DateTime? utcNow = null)
        {
            var utc = utcNow ?? DateTime.UtcNow;
            if (utc.Kind == DateTimeKind.Local)
            {
                utc = utc.ToUniversalTime();
            }
            return utc.AddHours(7).Date;
        }

        public static string BuildDefaultReferralCode(int userId) => $"OMNI-REF-{userId:D4}";

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
                    ReferralCode = BuildDefaultReferralCode(userId),
                    CurrentStreak = 0,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.LoyaltyWallets.AddAsync(wallet, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                wallet.CurrentTier = defaultTier;
                wallet.User = user;
            }
            else if (string.IsNullOrWhiteSpace(wallet.ReferralCode))
            {
                wallet.ReferralCode = BuildDefaultReferralCode(userId);
                await _context.SaveChangesAsync(cancellationToken);
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

            // Tự động kích hoạt thưởng Giới thiệu bạn bè nếu đây là đơn hàng hoàn tất đầu tiên của người được giới thiệu
            try
            {
                await ProcessReferralFirstOrderCompletedAsync(userId, orderId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi xử lý thưởng giới thiệu đơn đầu tiên cho User {UserId}", userId);
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
                ValidFrom = DateTime.UtcNow.AddMinutes(-5),
                ValidTo = DateTime.UtcNow.AddMonths(1),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            // Đồng bộ sang bảng Coupons để áp dụng ngay lập tức tại trang Checkout & POS
            var syncedCoupon = new Coupon
            {
                CouponCode = voucherCode,
                Description = desc,
                DiscountType = "FIXED_AMOUNT",
                DiscountValue = discountAmount,
                MinOrderValue = discountAmount,
                MaxDiscountAmount = discountAmount,
                UsageLimit = 1,
                UsageCount = 0,
                UsageLimitPerUser = 1,
                ValidFrom = DateTime.UtcNow.AddMinutes(-5),
                ValidTo = DateTime.UtcNow.AddMonths(1),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.PointTransactions.AddAsync(pointTx, cancellationToken);
            await _context.Vouchers.AddAsync(voucher, cancellationToken);
            await _context.Coupons.AddAsync(syncedCoupon, cancellationToken);
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
                ReferralCode = wallet.ReferralCode ?? BuildDefaultReferralCode(wallet.UserId),
                CurrentStreak = wallet.CurrentStreak,
                LastCheckInDate = wallet.LastCheckInDate,
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

        // =========================================================================
        // GAMIFICATION LOYALTY IMPLEMENTATION (GAP-08 - PHIÊN C7)
        // =========================================================================

        public async Task<DailyCheckInStatusDto> GetDailyCheckInStatusAsync(
            int userId,
            DateTime? utcNowOverride = null,
            CancellationToken cancellationToken = default)
        {
            await GetOrCreateWalletAsync(userId, cancellationToken);
            var wallet = await _context.LoyaltyWallets
                .AsNoTracking()
                .FirstAsync(w => w.UserId == userId, cancellationToken);

            DateTime vnToday = GetVietnamDate(utcNowOverride);

            bool checkedInToday = (wallet.LastCheckInDate.HasValue && wallet.LastCheckInDate.Value.Date == vnToday)
                || await _context.LoyaltyCheckIns
                    .AsNoTracking()
                    .AnyAsync(c => c.UserId == userId && c.CheckInDate == vnToday, cancellationToken);

            int effectiveStreak;
            if (checkedInToday)
            {
                effectiveStreak = Math.Clamp(wallet.CurrentStreak, 1, 7);
            }
            else if (wallet.LastCheckInDate.HasValue && wallet.LastCheckInDate.Value.Date == vnToday.AddDays(-1))
            {
                // Chuỗi 7 ngày đã hoàn thành hôm qua -> hôm nay bắt đầu lại chu kỳ mới từ ngày 1
                effectiveStreak = wallet.CurrentStreak >= 7 ? 0 : Math.Max(0, wallet.CurrentStreak);
            }
            else
            {
                // Bỏ lỡ ít nhất 1 ngày hoặc chưa từng điểm danh -> reset về 0
                effectiveStreak = 0;
            }

            int nextDayIndex = checkedInToday
                ? (effectiveStreak >= 7 ? 1 : effectiveStreak + 1)
                : (effectiveStreak + 1);
            int nextPoints = DailyCheckInPoints[Math.Clamp(nextDayIndex - 1, 0, 6)];

            var slots = new List<DailyCheckInSlotDto>();
            for (int day = 1; day <= 7; day++)
            {
                slots.Add(new DailyCheckInSlotDto
                {
                    DayNumber = day,
                    Points = DailyCheckInPoints[day - 1],
                    IsCheckedIn = day <= effectiveStreak,
                    IsTodaySlot = !checkedInToday && day == effectiveStreak + 1
                });
            }

            return new DailyCheckInStatusDto
            {
                CurrentStreak = checkedInToday ? effectiveStreak : (wallet.LastCheckInDate.HasValue && wallet.LastCheckInDate.Value.Date == vnToday.AddDays(-1) ? wallet.CurrentStreak : 0),
                CheckedInToday = checkedInToday,
                TodayVietnamDate = vnToday.ToString("dd/MM/yyyy"),
                NextPoints = nextPoints,
                Slots = slots
            };
        }

        public async Task<DailyCheckInResultDto> CheckInDailyAsync(
            int userId,
            DateTime? utcNowOverride = null,
            CancellationToken cancellationToken = default)
        {
            await GetOrCreateWalletAsync(userId, cancellationToken);
            var wallet = await _context.LoyaltyWallets
                .FirstAsync(w => w.UserId == userId, cancellationToken);

            DateTime nowUtc = utcNowOverride ?? DateTime.UtcNow;
            DateTime vnToday = GetVietnamDate(nowUtc);

            bool alreadyCheckedIn = (wallet.LastCheckInDate.HasValue && wallet.LastCheckInDate.Value.Date == vnToday)
                || await _context.LoyaltyCheckIns
                    .AnyAsync(c => c.UserId == userId && c.CheckInDate == vnToday, cancellationToken);

            if (alreadyCheckedIn)
            {
                return new DailyCheckInResultDto
                {
                    Success = false,
                    Message = $"Bạn đã điểm danh ngày {vnToday:dd/MM/yyyy} rồi! Hãy quay lại sau 00:00 (giờ VN) để nhận điểm tiếp theo.",
                    PointsAwarded = 0,
                    CurrentStreak = wallet.CurrentStreak,
                    NewBalance = wallet.CurrentBalance,
                    CheckedInToday = true
                };
            }

            int newStreak;
            if (wallet.LastCheckInDate.HasValue && wallet.LastCheckInDate.Value.Date == vnToday.AddDays(-1))
            {
                newStreak = (wallet.CurrentStreak % 7) + 1;
            }
            else
            {
                newStreak = 1;
            }

            int pointsAwarded = DailyCheckInPoints[newStreak - 1];
            int balanceBefore = wallet.CurrentBalance;
            int balanceAfter = balanceBefore + pointsAwarded;

            wallet.CurrentBalance = balanceAfter;
            wallet.TotalPointsEarned += pointsAwarded;
            wallet.CurrentStreak = newStreak;
            wallet.LastCheckInDate = vnToday;
            wallet.UpdatedAt = nowUtc;
            if (string.IsNullOrWhiteSpace(wallet.ReferralCode))
            {
                wallet.ReferralCode = BuildDefaultReferralCode(userId);
            }

            var pointTx = new PointTransaction
            {
                WalletId = wallet.Id,
                TransactionType = "CHECKIN",
                Points = pointsAwarded,
                BalanceBefore = balanceBefore,
                BalanceAfter = balanceAfter,
                Description = $"Điểm danh hằng ngày (Ngày {newStreak}/7 - {vnToday:dd/MM/yyyy}) nhận +{pointsAwarded} điểm",
                ExpiresAt = nowUtc.AddMonths(12),
                CreatedAt = nowUtc
            };

            await _context.PointTransactions.AddAsync(pointTx, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var checkIn = new LoyaltyCheckIn
            {
                UserId = userId,
                WalletId = wallet.Id,
                CheckInDate = vnToday,
                StreakDay = newStreak,
                PointsAwarded = pointsAwarded,
                PointTransactionId = pointTx.Id,
                CreatedAt = nowUtc
            };

            await _context.LoyaltyCheckIns.AddAsync(checkIn, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} điểm danh ngày {VnDate}: Chuỗi {Streak}/7, +{Points} điểm (Dư: {Balance})",
                userId, vnToday.ToString("yyyy-MM-dd"), newStreak, pointsAwarded, balanceAfter);

            return new DailyCheckInResultDto
            {
                Success = true,
                Message = newStreak == 7
                    ? $"🎉 Tuyệt vời! Bạn đã cán mốc Chuỗi 7 Ngày Điểm Danh và nhận Jackpot +{pointsAwarded} điểm!"
                    : $"🎉 Điểm danh Ngày {newStreak}/7 thành công! Bạn nhận được +{pointsAwarded} điểm Omni Xu.",
                PointsAwarded = pointsAwarded,
                CurrentStreak = newStreak,
                NewBalance = balanceAfter,
                CheckedInToday = true
            };
        }

        public async Task<ReferralDashboardDto> GetReferralDashboardAsync(int userId, CancellationToken cancellationToken = default)
        {
            var walletDto = await GetOrCreateWalletAsync(userId, cancellationToken);
            string myCode = walletDto.ReferralCode ?? BuildDefaultReferralCode(userId);

            var myReferrals = await _context.LoyaltyReferrals
                .AsNoTracking()
                .Include(r => r.ReferredUser)
                .Where(r => r.ReferrerUserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken);

            var referredByRecord = await _context.LoyaltyReferrals
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ReferredUserId == userId, cancellationToken);

            return new ReferralDashboardDto
            {
                MyReferralCode = myCode,
                TotalReferredCount = myReferrals.Count,
                RewardedReferredCount = myReferrals.Count(r => r.Status == "REWARDED"),
                TotalPointsEarnedFromReferrals = myReferrals.Where(r => r.Status == "REWARDED").Sum(r => r.ReferrerPointsAwarded),
                CanApplyReferralCode = referredByRecord == null,
                AppliedReferralCode = referredByRecord?.ReferralCode,
                ReferredByStatus = referredByRecord?.Status,
                Referrals = myReferrals.Select(r => new ReferralItemDto
                {
                    Id = r.Id,
                    ReferredUserId = r.ReferredUserId,
                    ReferredUserName = r.ReferredUser?.FullName ?? r.ReferredUser?.Username ?? $"Thành viên #{r.ReferredUserId}",
                    Status = r.Status,
                    PointsAwarded = r.ReferrerPointsAwarded,
                    RewardVoucherCode = r.RewardVoucherCode,
                    CreatedAt = r.CreatedAt,
                    RewardedAt = r.RewardedAt
                }).ToList()
            };
        }

        public async Task<ApplyReferralResultDto> ApplyReferralCodeAsync(
            int referredUserId,
            string referralCode,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(referralCode))
            {
                return new ApplyReferralResultDto
                {
                    Success = false,
                    Message = "Vui lòng nhập mã giới thiệu."
                };
            }

            string cleanCode = referralCode.Trim().ToUpperInvariant();
            await GetOrCreateWalletAsync(referredUserId, cancellationToken);

            // 1. Tìm người giới thiệu theo ReferralCode trên ví hoặc theo quy ước OMNI-REF-{id}
            var referrerWallet = await _context.LoyaltyWallets
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.ReferralCode != null && w.ReferralCode.ToUpper() == cleanCode, cancellationToken);

            int referrerUserId = referrerWallet?.UserId ?? 0;
            if (referrerUserId <= 0 && cleanCode.StartsWith("OMNI-REF-", StringComparison.OrdinalIgnoreCase))
            {
                var suffix = cleanCode.Substring("OMNI-REF-".Length);
                if (int.TryParse(suffix, out int parsedId) && parsedId > 0)
                {
                    var referrerUserExists = await _context.Users.AnyAsync(u => u.Id == parsedId, cancellationToken);
                    if (referrerUserExists)
                    {
                        await GetOrCreateWalletAsync(parsedId, cancellationToken);
                        referrerUserId = parsedId;
                    }
                }
            }

            if (referrerUserId <= 0)
            {
                return new ApplyReferralResultDto
                {
                    Success = false,
                    Message = $"Mã giới thiệu '{cleanCode}' không tồn tại trên hệ thống."
                };
            }

            // 2. Chặn tự giới thiệu chính mình
            if (referrerUserId == referredUserId)
            {
                return new ApplyReferralResultDto
                {
                    Success = false,
                    Message = "Bạn không thể tự sử dụng mã giới thiệu của chính mình."
                };
            }

            // 3. Chặn nhập mã nhiều lần
            bool alreadyReferred = await _context.LoyaltyReferrals
                .AnyAsync(r => r.ReferredUserId == referredUserId, cancellationToken);

            if (alreadyReferred)
            {
                return new ApplyReferralResultDto
                {
                    Success = false,
                    Message = "Tài khoản của bạn đã áp dụng mã giới thiệu trước đó."
                };
            }

            // 4. Chặn giới thiệu vòng lặp (A đã giới thiệu B -> B không được giới thiệu ngược lại A)
            bool isCircularReferral = await _context.LoyaltyReferrals
                .AnyAsync(r => r.ReferrerUserId == referredUserId && r.ReferredUserId == referrerUserId, cancellationToken);

            if (isCircularReferral)
            {
                return new ApplyReferralResultDto
                {
                    Success = false,
                    Message = "Không thể giới thiệu vòng lặp hai chiều giữa hai tài khoản đã giới thiệu nhau."
                };
            }

            var referral = new LoyaltyReferral
            {
                ReferrerUserId = referrerUserId,
                ReferredUserId = referredUserId,
                ReferralCode = cleanCode,
                Status = "PENDING",
                ReferrerPointsAwarded = 0,
                ReferredPointsAwarded = 0,
                CreatedAt = DateTime.UtcNow
            };

            await _context.LoyaltyReferrals.AddAsync(referral, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            // Kiểm tra nếu tài khoản này đã có đơn hàng hoàn tất/thanh toán thì trao thưởng luôn
            var completedOrder = await _context.Orders
                .AsNoTracking()
                .Where(o => o.UserId == referredUserId && (o.OrderStatus == "COMPLETED" || o.OrderStatus == "DELIVERED" || o.PaymentStatus == "PAID"))
                .OrderBy(o => o.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (completedOrder != null)
            {
                return await ProcessReferralFirstOrderCompletedAsync(referredUserId, completedOrder.Id, cancellationToken);
            }

            var currentWallet = await _context.LoyaltyWallets
                .AsNoTracking()
                .FirstAsync(w => w.UserId == referredUserId, cancellationToken);

            return new ApplyReferralResultDto
            {
                Success = true,
                ReferralCode = cleanCode,
                PointsAwarded = 0,
                NewBalance = currentWallet.CurrentBalance,
                Message = $"Đã liên kết mã giới thiệu {cleanCode} thành công! Ngay khi bạn hoàn tất đơn hàng đầu tiên, bạn sẽ nhận +50 điểm & Voucher 50.000₫, người giới thiệu nhận +100 điểm."
            };
        }

        public async Task<ApplyReferralResultDto> ProcessReferralFirstOrderCompletedAsync(
            int referredUserId,
            int? orderId = null,
            CancellationToken cancellationToken = default)
        {
            var referral = await _context.LoyaltyReferrals
                .FirstOrDefaultAsync(r => r.ReferredUserId == referredUserId, cancellationToken);

            if (referral == null)
            {
                return new ApplyReferralResultDto
                {
                    Success = false,
                    Message = "Tài khoản chưa liên kết mã giới thiệu."
                };
            }

            if (referral.Status == "REWARDED")
            {
                var w = await _context.LoyaltyWallets.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == referredUserId, cancellationToken);
                return new ApplyReferralResultDto
                {
                    Success = false,
                    ReferralCode = referral.ReferralCode,
                    Message = "Phần thưởng giới thiệu bạn bè đã được trao trước đó.",
                    NewBalance = w?.CurrentBalance ?? 0
                };
            }

            await GetOrCreateWalletAsync(referral.ReferrerUserId, cancellationToken);
            await GetOrCreateWalletAsync(referral.ReferredUserId, cancellationToken);

            var referrerWallet = await _context.LoyaltyWallets.FirstAsync(w => w.UserId == referral.ReferrerUserId, cancellationToken);
            var referredWallet = await _context.LoyaltyWallets.FirstAsync(w => w.UserId == referral.ReferredUserId, cancellationToken);

            const int referrerPoints = 100; // 100 điểm = 100.000đ
            const int referredPoints = 50;  // 50 điểm = 50.000đ
            var now = DateTime.UtcNow;

            // 1. Cộng điểm vào sổ cái Người giới thiệu
            int refBefore = referrerWallet.CurrentBalance;
            int refAfter = refBefore + referrerPoints;
            referrerWallet.CurrentBalance = refAfter;
            referrerWallet.TotalPointsEarned += referrerPoints;
            referrerWallet.UpdatedAt = now;

            await _context.PointTransactions.AddAsync(new PointTransaction
            {
                WalletId = referrerWallet.Id,
                OrderId = orderId,
                TransactionType = "REFERRAL_BONUS",
                Points = referrerPoints,
                BalanceBefore = refBefore,
                BalanceAfter = refAfter,
                Description = $"Thưởng giới thiệu bạn mới (User #{referral.ReferredUserId}) hoàn tất đơn đầu tiên (+{referrerPoints} điểm)",
                ExpiresAt = now.AddMonths(12),
                CreatedAt = now
            }, cancellationToken);

            // 2. Cộng điểm vào sổ cái Người được giới thiệu
            int newBefore = referredWallet.CurrentBalance;
            int newAfter = newBefore + referredPoints;
            referredWallet.CurrentBalance = newAfter;
            referredWallet.TotalPointsEarned += referredPoints;
            referredWallet.UpdatedAt = now;

            await _context.PointTransactions.AddAsync(new PointTransaction
            {
                WalletId = referredWallet.Id,
                OrderId = orderId,
                TransactionType = "REFERRAL_BONUS",
                Points = referredPoints,
                BalanceBefore = newBefore,
                BalanceAfter = newAfter,
                Description = $"Thưởng chào bạn mới từ mã giới thiệu {referral.ReferralCode} (+{referredPoints} điểm)",
                ExpiresAt = now.AddMonths(12),
                CreatedAt = now
            }, cancellationToken);

            // 3. Tặng Voucher 50.000đ cho cả hai bên
            string voucherCode = $"REF50K-{referral.ReferredUserId:D3}-{Random.Shared.Next(100, 999)}";
            string voucherDesc = $"Voucher 50.000₫ quà tặng Giới thiệu bạn bè ({referral.ReferralCode})";

            await _context.Vouchers.AddRangeAsync(new[]
            {
                new Voucher
                {
                    UserId = referral.ReferredUserId,
                    VoucherCode = voucherCode,
                    Description = voucherDesc,
                    DiscountType = "FIXED_AMOUNT",
                    DiscountValue = 50000m,
                    MinOrderValue = 150000m,
                    MaxDiscountAmount = 50000m,
                    ValidFrom = now.AddMinutes(-5),
                    ValidTo = now.AddMonths(2),
                    IsUsed = false,
                    CreatedAt = now
                },
                new Voucher
                {
                    UserId = referral.ReferrerUserId,
                    VoucherCode = $"REF50K-{referral.ReferrerUserId:D3}-{Random.Shared.Next(100, 999)}",
                    Description = voucherDesc,
                    DiscountType = "FIXED_AMOUNT",
                    DiscountValue = 50000m,
                    MinOrderValue = 150000m,
                    MaxDiscountAmount = 50000m,
                    ValidFrom = now.AddMinutes(-5),
                    ValidTo = now.AddMonths(2),
                    IsUsed = false,
                    CreatedAt = now
                }
            }, cancellationToken);

            await _context.Coupons.AddAsync(new Coupon
            {
                CouponCode = voucherCode,
                Description = voucherDesc,
                DiscountType = "FIXED_AMOUNT",
                DiscountValue = 50000m,
                MinOrderValue = 150000m,
                MaxDiscountAmount = 50000m,
                UsageLimit = 2,
                UsageLimitPerUser = 1,
                ValidFrom = now.AddMinutes(-5),
                ValidTo = now.AddMonths(2),
                IsActive = true,
                CreatedAt = now
            }, cancellationToken);

            referral.Status = "REWARDED";
            referral.ReferrerPointsAwarded = referrerPoints;
            referral.ReferredPointsAwarded = referredPoints;
            referral.RewardVoucherCode = voucherCode;
            referral.CompletedOrderId = orderId;
            referral.RewardedAt = now;

            await _context.SaveChangesAsync(cancellationToken);

            return new ApplyReferralResultDto
            {
                Success = true,
                ReferralCode = referral.ReferralCode,
                PointsAwarded = referredPoints,
                VoucherCode = voucherCode,
                NewBalance = referredWallet.CurrentBalance,
                Message = $"🎉 Chúc mừng! Bạn nhận được +{referredPoints} điểm & Voucher {voucherCode} (-50.000₫), người giới thiệu nhận +{referrerPoints} điểm!"
            };
        }

        public async Task<List<BeautyMissionDto>> GetBeautyMissionsAsync(int userId, CancellationToken cancellationToken = default)
        {
            await GetOrCreateWalletAsync(userId, cancellationToken);
            var wallet = await _context.LoyaltyWallets
                .AsNoTracking()
                .FirstAsync(w => w.UserId == userId, cancellationToken);

            var claimedTxs = await _context.PointTransactions
                .AsNoTracking()
                .Where(t => t.WalletId == wallet.Id && t.TransactionType == "MISSION")
                .Select(t => t.Description)
                .ToListAsync(cancellationToken);

            bool IsMissionClaimed(string code) => claimedTxs.Any(d => d != null && d.Contains($"[{code}]", StringComparison.OrdinalIgnoreCase));

            // Kiểm tra điều kiện thực tế trên hệ thống
            bool hasSkinProfile = await _context.CustomerProfiles
                .AsNoTracking()
                .AnyAsync(cp => cp.UserId == userId && !string.IsNullOrEmpty(cp.CurrentSkinType), cancellationToken);

            bool hasPhotoReview = await _context.ProductReviews
                .AsNoTracking()
                .AnyAsync(r => r.UserId == userId && r.ImagesJson != null && r.ImagesJson != "" && r.ImagesJson != "[]", cancellationToken);

            bool hasFirstOrder = await _context.Orders
                .AsNoTracking()
                .AnyAsync(o => o.UserId == userId && (o.OrderStatus == "COMPLETED" || o.OrderStatus == "DELIVERED" || o.PaymentStatus == "PAID"), cancellationToken);

            return new List<BeautyMissionDto>
            {
                new()
                {
                    MissionCode = "MISSION_SKIN_QUIZ",
                    Title = "Hoàn thành Trắc nghiệm Làn da AI (Skin Quiz)",
                    Description = "Khám phá loại da & phác đồ hoạt chất cá nhân hóa cùng Chuyên gia AI.",
                    IconClass = "fa-solid fa-wand-magic-sparkles",
                    PointsReward = 30,
                    IsConditionMet = hasSkinProfile,
                    IsClaimed = IsMissionClaimed("MISSION_SKIN_QUIZ"),
                    ActionUrl = "/SkinQuiz",
                    ActionText = "Làm Skin Quiz (+30đ)"
                },
                new()
                {
                    MissionCode = "MISSION_PHOTO_REVIEW",
                    Title = "Viết đánh giá sản phẩm có hình ảnh thật",
                    Description = "Chia sẻ cảm nhận & ảnh chụp sản phẩm thực tế giúp cộng đồng làm đẹp.",
                    IconClass = "fa-solid fa-camera-retro",
                    PointsReward = 50,
                    IsConditionMet = hasPhotoReview,
                    IsClaimed = IsMissionClaimed("MISSION_PHOTO_REVIEW"),
                    ActionUrl = "/Shop",
                    ActionText = "Đánh giá SP (+50đ)"
                },
                new()
                {
                    MissionCode = "MISSION_FIRST_ORDER",
                    Title = "Mua & hoàn tất đơn hàng đầu tiên",
                    Description = "Trải nghiệm mua sắm mỹ phẩm chính hãng lô date FEFO mới nhất tại Omni.",
                    IconClass = "fa-solid fa-bag-shopping",
                    PointsReward = 100,
                    IsConditionMet = hasFirstOrder,
                    IsClaimed = IsMissionClaimed("MISSION_FIRST_ORDER"),
                    ActionUrl = "/Shop",
                    ActionText = "Mua sắm ngay (+100đ)"
                }
            };
        }

        public async Task<ClaimMissionResultDto> ClaimMissionRewardAsync(
            int userId,
            string missionCode,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(missionCode))
            {
                return new ClaimMissionResultDto { Success = false, Message = "Mã nhiệm vụ không hợp lệ." };
            }

            string cleanCode = missionCode.Trim().ToUpperInvariant();
            var missions = await GetBeautyMissionsAsync(userId, cancellationToken);
            var mission = missions.FirstOrDefault(m => m.MissionCode.Equals(cleanCode, StringComparison.OrdinalIgnoreCase));

            if (mission == null)
            {
                return new ClaimMissionResultDto { Success = false, Message = "Không tìm thấy nhiệm vụ này." };
            }

            if (mission.IsClaimed)
            {
                var w = await _context.LoyaltyWallets.AsNoTracking().FirstAsync(x => x.UserId == userId, cancellationToken);
                return new ClaimMissionResultDto
                {
                    Success = false,
                    MissionCode = cleanCode,
                    PointsAwarded = 0,
                    NewBalance = w.CurrentBalance,
                    Message = "Bạn đã nhận thưởng nhiệm vụ này trước đó rồi."
                };
            }

            var wallet = await _context.LoyaltyWallets.FirstAsync(w => w.UserId == userId, cancellationToken);
            int balanceBefore = wallet.CurrentBalance;
            int balanceAfter = balanceBefore + mission.PointsReward;

            wallet.CurrentBalance = balanceAfter;
            wallet.TotalPointsEarned += mission.PointsReward;
            wallet.UpdatedAt = DateTime.UtcNow;

            var pointTx = new PointTransaction
            {
                WalletId = wallet.Id,
                TransactionType = "MISSION",
                Points = mission.PointsReward,
                BalanceBefore = balanceBefore,
                BalanceAfter = balanceAfter,
                Description = $"[{cleanCode}] Thưởng hoàn thành nhiệm vụ: {mission.Title} (+{mission.PointsReward} điểm)",
                ExpiresAt = DateTime.UtcNow.AddMonths(12),
                CreatedAt = DateTime.UtcNow
            };

            await _context.PointTransactions.AddAsync(pointTx, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return new ClaimMissionResultDto
            {
                Success = true,
                MissionCode = cleanCode,
                PointsAwarded = mission.PointsReward,
                NewBalance = balanceAfter,
                Message = $"🎉 Chúc mừng! Bạn đã nhận +{mission.PointsReward} điểm từ nhiệm vụ '{mission.Title}'!"
            };
        }

        public async Task<List<RewardCatalogItemDto>> GetRewardsCatalogAsync(int userId, CancellationToken cancellationToken = default)
        {
            var walletDto = await GetOrCreateWalletAsync(userId, cancellationToken);
            int balance = walletDto.CurrentBalance;

            return new List<RewardCatalogItemDto>
            {
                new()
                {
                    RewardCode = "BAZAAR_50K",
                    Title = "Voucher Giảm 50.000₫",
                    Description = "Áp dụng cho mọi đơn hàng từ 50.000₫ tại Website & Showroom POS.",
                    BadgeText = "Phổ biến nhất",
                    PointsCost = 50,
                    DiscountValueVnd = 50000m,
                    MinOrderValueVnd = 50000m,
                    CanAfford = balance >= 50
                },
                new()
                {
                    RewardCode = "BAZAAR_100K",
                    Title = "Voucher Giảm 100.000₫",
                    Description = "Ưu đãi đặc quyền quy đổi điểm thưởng cho đơn hàng từ 100.000₫.",
                    BadgeText = "Tiết kiệm",
                    PointsCost = 100,
                    DiscountValueVnd = 100000m,
                    MinOrderValueVnd = 100000m,
                    CanAfford = balance >= 100
                },
                new()
                {
                    RewardCode = "BAZAAR_200K",
                    Title = "Voucher VIP Giảm 200.000₫",
                    Description = "Siêu voucher Rewards Bazaar cho tín đồ Skincare & Nước hoa đơn từ 200.000₫.",
                    BadgeText = "Hạng VIP",
                    PointsCost = 200,
                    DiscountValueVnd = 200000m,
                    MinOrderValueVnd = 200000m,
                    CanAfford = balance >= 200
                }
            };
        }
    }
}

