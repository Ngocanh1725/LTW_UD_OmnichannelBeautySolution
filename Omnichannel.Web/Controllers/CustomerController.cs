using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Customer;
using Omnichannel.Application.DTOs.Loyalty;
using Omnichannel.Application.Interfaces.Loyalty;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Application.Interfaces.Notifications;
using Omnichannel.Application.DTOs.Notifications;

namespace Omnichannel.Web.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILoyaltyService _loyaltyService;
        private readonly ICartService? _cartService;
        private readonly ILogger<CustomerController> _logger;
        private readonly IInventoryNotificationService? _subscriptionService;
        private readonly IOrderManagementService? _orderManagementService;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment? _env;

        private const string CartCookieKey = "OmnichannelBeauty_CartId";

        // Bộ lưu trữ địa chỉ khách hàng theo UserId
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, List<CustomerAddressViewModel>> _userAddresses = new();

        public CustomerController(
            ApplicationDbContext dbContext,
            ILoyaltyService loyaltyService,
            ILogger<CustomerController> logger,
            ICartService? cartService = null,
            IInventoryNotificationService? subscriptionService = null,
            IOrderManagementService? orderManagementService = null,
            Microsoft.AspNetCore.Hosting.IWebHostEnvironment? env = null)
        {
            _dbContext = dbContext;
            _loyaltyService = loyaltyService;
            _logger = logger;
            _cartService = cartService;
            _subscriptionService = subscriptionService;
            _orderManagementService = orderManagementService;
            _env = env;
        }

        private string GetOrCreateCartId()
        {
            if (Request.Cookies.TryGetValue(CartCookieKey, out string? cartId) && !string.IsNullOrEmpty(cartId))
            {
                return cartId;
            }
            string newCartId = "GUEST-" + Guid.NewGuid().ToString("N");
            Response.Cookies.Append(CartCookieKey, newCartId, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(14),
                HttpOnly = true,
                SameSite = SameSiteMode.Lax
            });
            return newCartId;
        }

        // GET: /Customer/Dashboard
        [HttpGet]
        public async Task<IActionResult> Dashboard(string? previewTier = null)
        {
            // 1. Xác định UserId (Nếu đã đăng nhập lấy từ Claim, nếu chưa lấy tài khoản demo customer01)
            int userId = 0;
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userIdClaim))
            {
                int.TryParse(userIdClaim, out userId);
            }

            var user = await _dbContext.Users
                .Include(u => u.LoyaltyWallet)
                    .ThenInclude(w => w!.CurrentTier)
                .Include(u => u.LoyaltyWallet)
                    .ThenInclude(w => w!.Transactions)
                .FirstOrDefaultAsync(u => u.Id == userId);

            // Nếu chưa đăng nhập hoặc không tìm thấy, lấy tài khoản mẫu Khách hàng hoặc tài khoản đầu tiên
            if (user == null)
            {
                user = await _dbContext.Users
                    .Include(u => u.LoyaltyWallet)
                        .ThenInclude(w => w!.CurrentTier)
                    .Include(u => u.LoyaltyWallet)
                        .ThenInclude(w => w!.Transactions)
                    .FirstOrDefaultAsync(u => u.Username == "customer01" || u.UserType == 1);
            }

            // Nếu DB chưa có user nào, tạo mô phỏng dữ liệu chuẩn VIP
            string fullName = user?.FullName ?? "Phương Anh Nguyễn";
            string phone = user?.PhoneNumber ?? "0908889999";
            string email = user?.Email ?? "phuonganh@omnibeauty.vn";
            string avatar = user?.AvatarUrl ?? "/images/branding/logo-icon.svg";
            int activeUserId = user?.Id ?? 101;

            // 2. Lấy thông tin Ví thành viên
            LoyaltyWalletDto? wallet = null;
            if (user != null)
            {
                try
                {
                    wallet = await _loyaltyService.GetWalletAsync(activeUserId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không thể tải ví điểm trực tiếp cho User {UserId}", activeUserId);
                }
            }

            // 3. Xử lý Hạng thành viên (Hỗ trợ Preview Tier: BRONZE | SILVER | GOLD | DIAMOND)
            string currentTierName = !string.IsNullOrWhiteSpace(previewTier)
                ? previewTier.Trim().ToUpperInvariant()
                : (wallet?.CurrentTier?.TierName ?? "GOLD").ToUpperInvariant();

            // Đồng bộ dữ liệu giả lập cho trải nghiệm VIP theo từng hạng
            int currentPoints = wallet?.CurrentBalance ?? 850;
            int totalPointsEarned = wallet?.TotalPointsEarned ?? 1250;
            decimal totalSpend = wallet?.TotalSpend12Months ?? 8550000m;
            decimal multiplier = 1.5m;
            string nextTier = "DIAMOND";
            decimal spendNeeded = 1450000m;
            decimal currentMinSpend = 2000000m;
            decimal nextMinSpend = 10000000m;
            int progress = 85;

            bool isPreviewMode = !string.IsNullOrWhiteSpace(previewTier);

            switch (currentTierName)
            {
                case "BRONZE":
                    multiplier = 1.0m;
                    if (isPreviewMode || wallet == null)
                    {
                        currentPoints = 50;
                        totalPointsEarned = 50;
                        totalSpend = 650000m;
                    }
                    nextTier = "SILVER";
                    currentMinSpend = 0m;
                    nextMinSpend = 2000000m;
                    spendNeeded = Math.Max(0, nextMinSpend - totalSpend);
                    progress = (int)Math.Clamp(Math.Round((totalSpend / nextMinSpend) * 100), 5, 100);
                    break;

                case "SILVER":
                    multiplier = 1.0m;
                    if (isPreviewMode || wallet == null)
                    {
                        currentPoints = 220;
                        totalPointsEarned = 350;
                        totalSpend = 1850000m;
                    }
                    nextTier = "GOLD";
                    currentMinSpend = 0m;
                    nextMinSpend = 5000000m;
                    spendNeeded = Math.Max(0, nextMinSpend - totalSpend);
                    progress = (int)Math.Clamp(Math.Round((totalSpend / nextMinSpend) * 100), 10, 100);
                    break;

                case "GOLD":
                    multiplier = 1.5m;
                    if (isPreviewMode || wallet == null)
                    {
                        currentPoints = 850;
                        totalPointsEarned = 1450;
                        totalSpend = 8550000m;
                    }
                    else if (totalSpend < 5000000m)
                    {
                        totalSpend = 8550000m;
                    }
                    nextTier = "DIAMOND";
                    currentMinSpend = 5000000m;
                    nextMinSpend = 10000000m;
                    spendNeeded = Math.Max(0, nextMinSpend - totalSpend);
                    progress = (int)Math.Clamp(Math.Round((totalSpend / nextMinSpend) * 100), 50, 99);
                    break;

                case "DIAMOND":
                    multiplier = 2.0m;
                    if (isPreviewMode || wallet == null)
                    {
                        currentPoints = 2850;
                        totalPointsEarned = 4600;
                        totalSpend = 18500000m;
                    }
                    nextTier = "VIP ELITE PRIVILEGE";
                    currentMinSpend = 10000000m;
                    nextMinSpend = 10000000m;
                    spendNeeded = 0m;
                    progress = 100;
                    break;
            }

            // Sinh mã vạch thành viên (Barcode 13 số & QR Code)
            string cleanPhoneDigits = new string(phone.Where(char.IsDigit).ToArray());
            if (cleanPhoneDigits.Length < 10) cleanPhoneDigits = cleanPhoneDigits.PadLeft(10, '0');
            string barcode = $"8938{cleanPhoneDigits.Substring(cleanPhoneDigits.Length - 9)}";
            string qrData = $"OMNI-VIP-{currentTierName}-{cleanPhoneDigits}";

            // 4. Lấy danh sách Voucher khả dụng
            List<VoucherDto> vouchers = new();
            if (user != null)
            {
                try
                {
                    vouchers = await _loyaltyService.GetUserVouchersAsync(activeUserId, true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi lấy voucher cho User {UserId}", activeUserId);
                }
            }

            if (!vouchers.Any())
            {
                vouchers = new List<VoucherDto>
                {
                    new()
                    {
                        Id = 1,
                        VoucherCode = "CHAOMOI10-VIP",
                        Description = "Giảm 10% đơn hàng đầu tiên chào mừng thành viên mới",
                        DiscountType = "PERCENTAGE",
                        DiscountValue = 10,
                        MinOrderValue = 300000,
                        MaxDiscountAmount = 100000,
                        ValidFrom = DateTime.UtcNow.AddDays(-5),
                        ValidTo = DateTime.UtcNow.AddDays(25),
                        IsUsed = false
                    },
                    new()
                    {
                        Id = 2,
                        VoucherCode = "FREESHIP-GOLD",
                        Description = "Miễn phí vận chuyển toàn quốc cho thành viên Gold & Diamond",
                        DiscountType = "FIXED_AMOUNT",
                        DiscountValue = 35000,
                        MinOrderValue = 0,
                        ValidFrom = DateTime.UtcNow.AddDays(-2),
                        ValidTo = DateTime.UtcNow.AddDays(60),
                        IsUsed = false
                    },
                    new()
                    {
                        Id = 3,
                        VoucherCode = "BIRTHDAY-VIP150K",
                        Description = "Quà tặng sinh nhật tháng này - Giảm ngay 150.000₫ cho hóa đơn nước hoa cao cấp",
                        DiscountType = "FIXED_AMOUNT",
                        DiscountValue = 150000,
                        MinOrderValue = 1000000,
                        ValidFrom = DateTime.UtcNow.AddDays(-1),
                        ValidTo = DateTime.UtcNow.AddDays(30),
                        IsUsed = false
                    }
                };
            }

            // 5. Lịch sử giao dịch điểm gần nhất
            var recentTxs = wallet?.RecentTransactions ?? new List<PointTransactionDto>
            {
                new()
                {
                    Id = 101,
                    TransactionType = "EARN",
                    Points = 120,
                    BalanceBefore = 730,
                    BalanceAfter = 850,
                    Description = "Tích điểm đơn hàng nước hoa Maison Margiela Replica #ORD-202610",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new()
                {
                    Id = 102,
                    TransactionType = "BONUS",
                    Points = 50,
                    BalanceBefore = 680,
                    BalanceAfter = 730,
                    Description = "Thưởng chào mừng thành viên mới Omni Beauty Club",
                    CreatedAt = DateTime.UtcNow.AddDays(-15)
                },
                new()
                {
                    Id = 103,
                    TransactionType = "EARN",
                    Points = 85,
                    BalanceBefore = 595,
                    BalanceAfter = 680,
                    Description = "Tích điểm đơn hàng kem dưỡng ẩm Clinique Moisture Surge",
                    CreatedAt = DateTime.UtcNow.AddDays(-25)
                }
            };

            // 6. Bảng danh sách Đặc quyền thành viên 4 Hạng
            var privileges = new List<MemberTierPrivilegeDto>
            {
                new()
                {
                    TierCode = "BRONZE",
                    TierName = "Bronze (Đồng)",
                    MinSpend = 0m,
                    CashBackRate = "1.0% (Mỗi 10.000₫ = 1 điểm)",
                    BirthdayGift = "Thưởng 50 Điểm Sinh Nhật vào ví",
                    ShippingPrivilege = "Miễn phí vận chuyển cho đơn từ 500.000₫",
                    FragranceTesting = "Nhận 1 vial mẫu thử nước hoa khi mua hàng",
                    ConsultantPrivilege = "Tư vấn da cơ bản qua Trợ lý AI Beauty Advisor",
                    IsCurrentTier = currentTierName == "BRONZE"
                },
                new()
                {
                    TierCode = "SILVER",
                    TierName = "Silver (Bạc)",
                    MinSpend = 2000000m,
                    CashBackRate = "1.2% (Tích lũy x1.2 điểm thưởng)",
                    BirthdayGift = "Voucher sinh nhật 50.000₫ + 100 điểm thưởng",
                    ShippingPrivilege = "Miễn phí vận chuyển cho đơn từ 300.000₫",
                    FragranceTesting = "Bộ đôi 2 vial nước hoa mini Haute Parfumerie",
                    ConsultantPrivilege = "Tư vấn chọn mùi hương và phác đồ da online",
                    IsCurrentTier = currentTierName == "SILVER"
                },
                new()
                {
                    TierCode = "GOLD",
                    TierName = "Gold (Vàng Kim)",
                    MinSpend = 5000000m,
                    CashBackRate = "1.5% (Tích lũy x1.5 điểm thưởng VIP)",
                    BirthdayGift = "Voucher sinh nhật 150.000₫ + Quà tặng Fullsize độc quyền",
                    ShippingPrivilege = "Miễn phí giao hàng Hỏa tốc 2H toàn quốc",
                    FragranceTesting = "Ưu tiên nhận Discovery Fragrance Kit trước ngày ra mắt 7 ngày",
                    ConsultantPrivilege = "Chuyên gia Da liễu & Chuyên gia Mùi hương chăm sóc 1-1",
                    IsCurrentTier = currentTierName == "GOLD"
                },
                new()
                {
                    TierCode = "DIAMOND",
                    TierName = "Diamond (Kim Cương)",
                    MinSpend = 10000000m,
                    CashBackRate = "2.0% (Tích lũy x2.0 điểm thưởng tối đa)",
                    BirthdayGift = "Set quà tặng Haute Luxury trị giá 1.500.000₫ & Hoa tươi tận nhà",
                    ShippingPrivilege = "Freeship trọn đời mọi đơn hàng không giới hạn giá trị",
                    FragranceTesting = "Bộ sưu tập Nước hoa Niche Limited Edition & Thư mời Private Event",
                    ConsultantPrivilege = "Trợ lý Sắc Đẹp Riêng (Dedicated Personal Advisor 24/7) & Quầy ưu tiên POS VIP",
                    IsCurrentTier = currentTierName == "DIAMOND"
                }
            };

            // 7. Tải dữ liệu Gamification Loyalty (GAP-08 - Phiên C7)
            DailyCheckInStatusDto checkInStatus = new();
            ReferralDashboardDto referralInfo = new();
            List<BeautyMissionDto> beautyMissions = new();
            List<RewardCatalogItemDto> rewardsCatalog = new();

            if (user != null)
            {
                try
                {
                    checkInStatus = await _loyaltyService.GetDailyCheckInStatusAsync(activeUserId);
                    referralInfo = await _loyaltyService.GetReferralDashboardAsync(activeUserId);
                    beautyMissions = await _loyaltyService.GetBeautyMissionsAsync(activeUserId);
                    rewardsCatalog = await _loyaltyService.GetRewardsCatalogAsync(activeUserId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi tải dữ liệu Gamification cho User {UserId}", activeUserId);
                }
            }

            var viewModel = new CustomerDashboardViewModel
            {
                UserId = activeUserId,
                FullName = fullName,
                PhoneNumber = phone,
                Email = email,
                AvatarUrl = avatar,
                WalletId = wallet?.Id ?? 1,
                TierName = currentTierName,
                PointMultiplier = multiplier,
                CurrentPoints = currentPoints,
                TotalPointsEarned = totalPointsEarned,
                TotalSpend12Months = totalSpend,
                MemberSince = wallet?.MemberSince ?? DateTime.UtcNow.AddMonths(-10),
                TierExpirationDate = new DateTime(DateTime.UtcNow.Year, 12, 31),
                QrCodeData = qrData,
                BarcodeNumber = barcode,
                NextTierName = nextTier,
                SpendNeededForNextTier = spendNeeded,
                CurrentTierMinSpend = currentMinSpend,
                NextTierMinSpend = nextMinSpend,
                ProgressPercentage = progress,
                UpgradeDeadlineText = "31/12",
                AvailableVouchers = vouchers,
                RecentTransactions = recentTxs,
                TierPrivileges = privileges,
                CheckInStatus = checkInStatus,
                ReferralInfo = referralInfo,
                BeautyMissions = beautyMissions,
                RewardsCatalog = rewardsCatalog
            };

            return View(viewModel);
        }

        // =========================================================================
        // GAMIFICATION LOYALTY ENDPOINTS (GAP-08 - PHIÊN C7)
        // =========================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckInDaily()
        {
            var (userId, _) = await GetCurrentCustomerUserAsync();
            if (userId <= 0)
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new { success = false, message = "Vui lòng đăng nhập để điểm danh." });
            }

            try
            {
                var result = await _loyaltyService.CheckInDailyAsync(userId);
                var status = await _loyaltyService.GetDailyCheckInStatusAsync(userId);
                var wallet = await _loyaltyService.GetWalletAsync(userId);

                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    pointsAwarded = result.PointsAwarded,
                    currentStreak = result.CurrentStreak,
                    newBalance = result.NewBalance,
                    availableDiscountVnd = result.NewBalance * 1000m,
                    checkInStatus = status,
                    recentTransactions = wallet?.RecentTransactions?.Take(6)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi điểm danh hằng ngày cho User {UserId}", userId);
                return Json(new { success = false, message = "Không thể thực hiện điểm danh lúc này: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RedeemReward([FromForm] int points = 50, [FromForm] string? rewardTitle = null)
        {
            var (userId, _) = await GetCurrentCustomerUserAsync();
            if (userId <= 0)
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new { success = false, message = "Vui lòng đăng nhập để đổi điểm." });
            }

            if (points <= 0)
            {
                return Json(new { success = false, message = "Số điểm quy đổi không hợp lệ." });
            }

            try
            {
                decimal vndAmount = points * 1000m;
                string desc = !string.IsNullOrWhiteSpace(rewardTitle)
                    ? $"Omni Rewards Bazaar: Đổi {points} điểm nhận {rewardTitle}"
                    : $"Omni Rewards Bazaar: Đổi {points} điểm nhận Voucher giảm {vndAmount:N0}₫";

                var redeemResult = await _loyaltyService.RedeemPointsAsync(userId, points, desc);

                // Lưu sẵn mã voucher vào cookie để khách hàng bấm "Dùng ngay tại Checkout" được tự động áp mã
                if (!string.IsNullOrWhiteSpace(redeemResult.VoucherCode))
                {
                    Response.Cookies.Append("OmnichannelBeauty_AppliedCoupon", redeemResult.VoucherCode, new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddDays(1),
                        HttpOnly = true,
                        SameSite = SameSiteMode.Lax
                    });
                }

                var vouchers = await _loyaltyService.GetUserVouchersAsync(userId, true);
                var wallet = await _loyaltyService.GetWalletAsync(userId);

                return Json(new
                {
                    success = true,
                    message = redeemResult.Message,
                    voucherCode = redeemResult.VoucherCode,
                    pointsRedeemed = redeemResult.PointsToRedeem,
                    discountAmountVnd = redeemResult.DiscountAmountVnd,
                    newBalance = redeemResult.NewBalance,
                    availableDiscountVnd = redeemResult.NewBalance * 1000m,
                    vouchersCount = vouchers.Count,
                    recentTransactions = wallet?.RecentTransactions?.Take(6)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyReferralCode([FromForm] string referralCode)
        {
            var (userId, _) = await GetCurrentCustomerUserAsync();
            if (userId <= 0)
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new { success = false, message = "Vui lòng đăng nhập để nhập mã giới thiệu." });
            }

            try
            {
                var result = await _loyaltyService.ApplyReferralCodeAsync(userId, referralCode);
                var wallet = await _loyaltyService.GetWalletAsync(userId);
                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    referralCode = result.ReferralCode,
                    pointsAwarded = result.PointsAwarded,
                    voucherCode = result.VoucherCode,
                    newBalance = result.NewBalance,
                    availableDiscountVnd = result.NewBalance * 1000m,
                    recentTransactions = wallet?.RecentTransactions?.Take(6)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClaimMission([FromForm] string missionCode)
        {
            var (userId, _) = await GetCurrentCustomerUserAsync();
            if (userId <= 0)
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new { success = false, message = "Vui lòng đăng nhập để nhận thưởng nhiệm vụ." });
            }

            try
            {
                var result = await _loyaltyService.ClaimMissionRewardAsync(userId, missionCode);
                var wallet = await _loyaltyService.GetWalletAsync(userId);
                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    missionCode = result.MissionCode,
                    pointsAwarded = result.PointsAwarded,
                    newBalance = result.NewBalance,
                    availableDiscountVnd = result.NewBalance * 1000m,
                    recentTransactions = wallet?.RecentTransactions?.Take(6)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // 1. SỔ ĐỊA CHỈ KHÁCH HÀNG (ADDRESSES)
        // ==========================================

        private List<CustomerAddressViewModel> GetOrCreateUserAddresses(int userId, string defaultName, string defaultPhone)
        {
            return _userAddresses.GetOrAdd(userId, _ => new List<CustomerAddressViewModel>
            {
                new()
                {
                    Id = 1,
                    UserId = userId,
                    FullName = string.IsNullOrWhiteSpace(defaultName) ? "Nguyễn Phương Anh" : defaultName,
                    PhoneNumber = string.IsNullOrWhiteSpace(defaultPhone) ? "0908889999" : defaultPhone,
                    Province = "Thành phố Hồ Chí Minh",
                    District = "Quận Bình Thạnh",
                    Ward = "Phường 22",
                    DetailedAddress = "Căn hộ 12.08, Tháp Sapphire 1, Saigon Pearl, 92 Nguyễn Hữu Cảnh",
                    FullAddress = "Căn hộ 12.08, Tháp Sapphire 1, Saigon Pearl, 92 Nguyễn Hữu Cảnh, Phường 22, Quận Bình Thạnh, Thành phố Hồ Chí Minh",
                    Label = "NHÀ RIÊNG",
                    IsDefault = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-3)
                },
                new()
                {
                    Id = 2,
                    UserId = userId,
                    FullName = $"{defaultName} (Phòng Marketing)",
                    PhoneNumber = string.IsNullOrWhiteSpace(defaultPhone) ? "0908889999" : defaultPhone,
                    Province = "Thành phố Hồ Chí Minh",
                    District = "Quận 1",
                    Ward = "Phường Bến Nghé",
                    DetailedAddress = "Tầng 18, Tòa nhà Bitexco Financial Tower, Số 2 Hải Triều",
                    FullAddress = "Tầng 18, Tòa nhà Bitexco Financial Tower, Số 2 Hải Triều, Phường Bến Nghé, Quận 1, Thành phố Hồ Chí Minh",
                    Label = "CÔNG TY",
                    IsDefault = false,
                    CreatedAt = DateTime.UtcNow.AddMonths(-2)
                },
                new()
                {
                    Id = 3,
                    UserId = userId,
                    FullName = "Bác Nguyễn Văn Hùng (Bố)",
                    PhoneNumber = "0912345678",
                    Province = "Thành phố Hà Nội",
                    District = "Quận Hoàn Kiếm",
                    Ward = "Phường Tràng Tiền",
                    DetailedAddress = "Số 45 Phố Tràng Tiền",
                    FullAddress = "Số 45 Phố Tràng Tiền, Phường Tràng Tiền, Quận Hoàn Kiếm, Thành phố Hà Nội",
                    Label = "NHÀ RIÊNG",
                    IsDefault = false,
                    CreatedAt = DateTime.UtcNow.AddMonths(-1)
                }
            });
        }

        // GET: /Customer/Addresses
        [HttpGet]
        public async Task<IActionResult> Addresses()
        {
            var (userId, user) = await GetCurrentCustomerUserAsync();
            string name = user?.FullName ?? "Nguyễn Phương Anh";
            string phone = user?.PhoneNumber ?? "0908889999";

            var addresses = GetOrCreateUserAddresses(userId, name, phone);

            var vm = new AddressListViewModel
            {
                UserId = userId,
                CustomerName = name,
                Addresses = addresses.OrderByDescending(a => a.IsDefault).ThenByDescending(a => a.CreatedAt).ToList()
            };

            return View(vm);
        }

        // POST: /Customer/SaveAddress
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAddress(SaveAddressRequest request)
        {
            var (userId, user) = await GetCurrentCustomerUserAsync();
            string name = user?.FullName ?? "Nguyễn Phương Anh";
            string phone = user?.PhoneNumber ?? "0908889999";

            var addresses = GetOrCreateUserAddresses(userId, name, phone);

            string fullAddress = $"{request.DetailedAddress?.Trim()}, {request.Ward?.Trim()}, {request.District?.Trim()}, {request.Province?.Trim()}";

            lock (addresses)
            {
                if (request.IsDefault)
                {
                    foreach (var a in addresses)
                    {
                        a.IsDefault = false;
                    }
                }

                if (request.Id > 0)
                {
                    var existing = addresses.FirstOrDefault(a => a.Id == request.Id);
                    if (existing != null)
                    {
                        existing.FullName = request.FullName?.Trim() ?? string.Empty;
                        existing.PhoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;
                        existing.Province = request.Province?.Trim() ?? string.Empty;
                        existing.District = request.District?.Trim() ?? string.Empty;
                        existing.Ward = request.Ward?.Trim() ?? string.Empty;
                        existing.DetailedAddress = request.DetailedAddress?.Trim() ?? string.Empty;
                        existing.FullAddress = fullAddress;
                        existing.Label = string.IsNullOrWhiteSpace(request.Label) ? "NHÀ RIÊNG" : request.Label.Trim().ToUpperInvariant();
                        existing.IsDefault = request.IsDefault;
                    }
                }
                else
                {
                    int nextId = addresses.Any() ? addresses.Max(a => a.Id) + 1 : 1;
                    addresses.Add(new CustomerAddressViewModel
                    {
                        Id = nextId,
                        UserId = userId,
                        FullName = request.FullName?.Trim() ?? string.Empty,
                        PhoneNumber = request.PhoneNumber?.Trim() ?? string.Empty,
                        Province = request.Province?.Trim() ?? string.Empty,
                        District = request.District?.Trim() ?? string.Empty,
                        Ward = request.Ward?.Trim() ?? string.Empty,
                        DetailedAddress = request.DetailedAddress?.Trim() ?? string.Empty,
                        FullAddress = fullAddress,
                        Label = string.IsNullOrWhiteSpace(request.Label) ? "NHÀ RIÊNG" : request.Label.Trim().ToUpperInvariant(),
                        IsDefault = request.IsDefault || !addresses.Any(),
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            TempData["SuccessMessage"] = "Đã lưu địa chỉ nhận hàng thành công!";
            return RedirectToAction("Addresses");
        }

        // POST: /Customer/DeleteAddress
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var (userId, user) = await GetCurrentCustomerUserAsync();
            string name = user?.FullName ?? "Nguyễn Phương Anh";
            string phone = user?.PhoneNumber ?? "0908889999";

            var addresses = GetOrCreateUserAddresses(userId, name, phone);
            lock (addresses)
            {
                var addr = addresses.FirstOrDefault(a => a.Id == id);
                if (addr != null)
                {
                    bool wasDefault = addr.IsDefault;
                    addresses.Remove(addr);
                    if (wasDefault && addresses.Any())
                    {
                        addresses.First().IsDefault = true;
                    }
                }
            }

            TempData["SuccessMessage"] = "Đã xóa địa chỉ thành công!";
            return RedirectToAction("Addresses");
        }

        // POST: /Customer/SetDefaultAddress
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefaultAddress(int id)
        {
            var (userId, user) = await GetCurrentCustomerUserAsync();
            string name = user?.FullName ?? "Nguyễn Phương Anh";
            string phone = user?.PhoneNumber ?? "0908889999";

            var addresses = GetOrCreateUserAddresses(userId, name, phone);
            lock (addresses)
            {
                foreach (var a in addresses)
                {
                    a.IsDefault = (a.Id == id);
                }
            }

            TempData["SuccessMessage"] = "Đã đặt địa chỉ mặc định thành công!";
            return RedirectToAction("Addresses");
        }

        // ==========================================
        // 2. LỊCH SỬ ĐƠN HÀNG (ORDERS LIST)
        // ==========================================

        // GET: /Customer/Orders
        [HttpGet]
        public async Task<IActionResult> Orders(string? status = "all")
        {
            var (userId, user) = await GetCurrentCustomerUserAsync();
            string name = user?.FullName ?? "Nguyễn Phương Anh";
            string phone = user?.PhoneNumber ?? "0908889999";
            string email = user?.Email ?? "phuonganh@omnibeauty.vn";

            var allOrders = await GetCustomerOrdersListAsync(userId, phone, email);

            string currentTab = string.IsNullOrWhiteSpace(status) ? "all" : status.Trim().ToLowerInvariant();

            // Lọc theo tab trạng thái: [Tất cả] | [Chờ xác nhận] | [Đang đóng gói] | [Đang giao hàng] | [Hoàn thành] | [Đã hủy]
            var filteredOrders = currentTab switch
            {
                "pending" => allOrders.Where(o => o.OrderStatus == "PENDING").ToList(),
                "confirmed" => allOrders.Where(o => o.OrderStatus == "CONFIRMED" || o.OrderStatus == "ALLOCATED" || o.OrderStatus == "PROCESSING").ToList(),
                "shipping" => allOrders.Where(o => o.OrderStatus == "SHIPPING").ToList(),
                "completed" => allOrders.Where(o => o.OrderStatus == "COMPLETED").ToList(),
                "cancelled" => allOrders.Where(o => o.OrderStatus == "CANCELLED").ToList(),
                _ => allOrders
            };

            var counts = new Dictionary<string, int>
            {
                ["all"] = allOrders.Count,
                ["pending"] = allOrders.Count(o => o.OrderStatus == "PENDING"),
                ["confirmed"] = allOrders.Count(o => o.OrderStatus == "CONFIRMED" || o.OrderStatus == "ALLOCATED" || o.OrderStatus == "PROCESSING"),
                ["shipping"] = allOrders.Count(o => o.OrderStatus == "SHIPPING"),
                ["completed"] = allOrders.Count(o => o.OrderStatus == "COMPLETED"),
                ["cancelled"] = allOrders.Count(o => o.OrderStatus == "CANCELLED")
            };

            var vm = new CustomerOrdersViewModel
            {
                UserId = userId,
                CustomerName = name,
                CurrentTab = currentTab,
                StatusCounts = counts,
                Orders = filteredOrders
            };

            return View(vm);
        }

        // ==========================================
        // 3. CHI TIẾT ĐƠN HÀNG (ORDER DETAIL)
        // ==========================================

        // GET: /Customer/OrderDetail/{id}
        [HttpGet]
        public async Task<IActionResult> OrderDetail(int id, string? orderCode = null)
        {
            int? authUserId = GetAuthenticatedUserId();
            bool isAdmin = User?.Identity?.IsAuthenticated == true &&
                           (User.IsInRole("SuperAdmin") || User.IsInRole("SUPER_ADMIN") || User.IsInRole("Admin") || User.IsInRole("StoreManager"));

            if (_orderManagementService != null && (id > 0 || !string.IsNullOrWhiteSpace(orderCode)))
            {
                var (allowed, errorMsg, detailVm) = await _orderManagementService.GetCustomerOrderDetailAsync(
                    id, orderCode, authUserId, isAdmin);

                if (detailVm != null)
                {
                    return View(detailVm);
                }

                if (!allowed && !string.IsNullOrWhiteSpace(errorMsg))
                {
                    // Nếu đơn hàng tồn tại nhưng thuộc về khách hàng khác -> Chặn 403 Forbidden (Anti-IDOR)
                    if (errorMsg.Contains("không có quyền", StringComparison.OrdinalIgnoreCase) ||
                        errorMsg.Contains("Vui lòng đăng nhập", StringComparison.OrdinalIgnoreCase))
                    {
                        TempData["ErrorMessage"] = errorMsg;
                        return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = errorMsg });
                    }
                }
            }

            var (userId, user) = await GetCurrentCustomerUserAsync();
            string name = user?.FullName ?? "Nguyễn Phương Anh";
            string phone = user?.PhoneNumber ?? "0908889999";

            Order? order = null;
            if (id > 0)
            {
                order = await _dbContext.Orders
                    .Include(o => o.StatusHistories)
                    .Include(o => o.ReturnRequests)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(d => d.Product)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(d => d.ProductBatch)
                    .Include(o => o.Invoice)
                    .FirstOrDefaultAsync(o => o.Id == id);
            }
            else if (!string.IsNullOrEmpty(orderCode))
            {
                order = await _dbContext.Orders
                    .Include(o => o.StatusHistories)
                    .Include(o => o.ReturnRequests)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(d => d.Product)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(d => d.ProductBatch)
                    .Include(o => o.Invoice)
                    .FirstOrDefaultAsync(o => o.OrderCode == orderCode);
            }

            if (order != null)
            {
                // Kiểm tra quyền sở hữu trực tiếp nếu không đi qua _orderManagementService
                if (!isAdmin)
                {
                    if (!authUserId.HasValue || (order.UserId.HasValue && order.UserId.Value != authUserId.Value))
                    {
                        return StatusCode(StatusCodes.Status403Forbidden, new
                        {
                            success = false,
                            message = "Bạn không có quyền truy cập đơn hàng của khách hàng khác."
                        });
                    }
                }

                return View(MapToOrderDetailViewModel(order));
            }

            // Fallback demo order detail for instant demonstration when orderId does not exist in DB
            var demoVm = GetDemoOrderDetail(id > 0 ? id : 101, orderCode ?? "ORD-202610-001", name, phone);
            return View(demoVm);
        }

        // POST: /Customer/Reorder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reorder(int orderId)
        {
            var order = await _dbContext.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            string cartId = GetOrCreateCartId();
            int itemsAdded = 0;

            if (order != null && _cartService != null)
            {
                foreach (var detail in order.OrderDetails)
                {
                    try
                    {
                        await _cartService.AddToCartAsync(cartId, null, detail.ProductId, null, detail.Quantity);
                        itemsAdded++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Lỗi thêm lại sản phẩm {ProductId} vào giỏ hàng", detail.ProductId);
                    }
                }
            }

            TempData["SuccessMessage"] = $"🎉 Đã thêm {itemsAdded} sản phẩm từ đơn hàng #{order?.OrderCode ?? orderId.ToString()} vào giỏ hàng! Bạn có thể tiến hành thanh toán ngay.";
            return RedirectToAction("Index", "Cart");
        }

        // POST: /Customer/RequestVatInvoice
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestVatInvoice([FromBody] VatInvoiceRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TaxCode) || string.IsNullOrWhiteSpace(request.CompanyName))
            {
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ Mã số thuế và Tên doanh nghiệp/công ty." });
            }

            var order = await _dbContext.Orders
                .Include(o => o.Invoice)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId);

            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng cần xuất hóa đơn VAT." });
            }

            if (order.Invoice == null)
            {
                var invoice = new Invoice
                {
                    InvoiceCode = $"INV-{DateTime.UtcNow:yyyyMMdd}-{order.Id:D4}",
                    OrderId = order.Id,
                    InvoiceDate = DateTime.UtcNow,
                    PaymentMethod = order.PaymentMethod,
                    TaxRate = 8.00m,
                    TaxAmount = Math.Round(order.TotalAmount * 0.08m / 1.08m, 2),
                    TotalAmount = order.TotalAmount,
                    CustomerTaxCode = request.TaxCode.Trim(),
                    CompanyName = request.CompanyName.Trim(),
                    Status = "PENDING"
                };
                _dbContext.Invoices.Add(invoice);
            }
            else
            {
                order.Invoice.CustomerTaxCode = request.TaxCode.Trim();
                order.Invoice.CompanyName = request.CompanyName.Trim();
                order.Invoice.Status = "PENDING";
            }

            await _dbContext.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Yêu cầu xuất hóa đơn điện tử VAT (8%) đã được ghi nhận. Kế toán Omni Beauty sẽ gửi hóa đơn điện tử đến email của bạn trong 24h làm việc!"
            });
        }

        // ==========================================
        // HELPER METHODS & DEMO DATA GENERATION
        // ==========================================

        private int? GetAuthenticatedUserId()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrWhiteSpace(userIdClaim) && int.TryParse(userIdClaim, out int parsedId) && parsedId > 0)
                {
                    return parsedId;
                }
            }
            return null;
        }

        private async Task<(int UserId, User? User)> GetCurrentCustomerUserAsync()
        {
            int userId = 0;
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userIdClaim))
            {
                int.TryParse(userIdClaim, out userId);
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == "customer01" || u.UserType == 1);
            }

            return (user?.Id ?? 101, user);
        }

        private async Task<List<CustomerOrderSummaryDto>> GetCustomerOrdersListAsync(int userId, string phone, string email)
        {
            var dbOrders = await _dbContext.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .Where(o => o.UserId == userId || o.CustomerPhone == phone || o.CustomerEmail == email)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            if (dbOrders.Count >= 3)
            {
                return dbOrders.Select(MapToOrderSummaryDto).ToList();
            }

            // Kết hợp thêm các đơn hàng mẫu đa dạng trạng thái để kiểm thử hoàn hảo
            var demoList = new List<CustomerOrderSummaryDto>
            {
                new()
                {
                    Id = 101,
                    OrderCode = "ORD-202610-091",
                    OrderDate = DateTime.UtcNow.AddHours(-2),
                    OrderStatus = "PENDING",
                    OrderStatusDisplayName = "Chờ xác nhận",
                    OrderStatusBadgeClass = "bg-amber-100 text-amber-800 border-amber-200",
                    TotalAmount = 2450000m,
                    TotalItemsCount = 2,
                    PrimaryProductName = "Nước hoa Maison Margiela Replica Jazz Club EDT 100ml",
                    RemainingProductsCount = 1,
                    ProductThumbnails = new List<string>
                    {
                        "/images/products/perfume/nuoc-hoa-unisex-maison-margiela-replica-jazz-club-100ml.jpg",
                        "/images/products/skincare/serum-phuc-hoi-la-roche-posay-hyalu-b5-pure-30ml.jpg"
                    },
                    PaymentMethod = "VNPAY",
                    PaymentStatus = "PAID"
                },
                new()
                {
                    Id = 102,
                    OrderCode = "ORD-202610-085",
                    OrderDate = DateTime.UtcNow.AddDays(-1),
                    OrderStatus = "CONFIRMED",
                    OrderStatusDisplayName = "Đang đóng gói (Chuẩn FEFO)",
                    OrderStatusBadgeClass = "bg-blue-100 text-blue-800 border-blue-200",
                    TotalAmount = 1850000m,
                    TotalItemsCount = 3,
                    PrimaryProductName = "Kem Dưỡng Ẩm Chuyên Sâu Clinique Moisture Surge 100H",
                    RemainingProductsCount = 2,
                    ProductThumbnails = new List<string>
                    {
                        "/images/products/skincare/gel-duong-am-clinique-moisture-surge-100h-50ml.jpg",
                        "/images/products/skincare/dung-dich-tay-te-bao-chet-paulas-choice-skin-perfecting-2-bha-118ml.png",
                        "/images/products/skincare/sua-rua-mat-cerave-hydrating-facial-cleanser-473ml.jpg"
                    },
                    PaymentMethod = "COD",
                    PaymentStatus = "UNPAID"
                },
                new()
                {
                    Id = 103,
                    OrderCode = "ORD-202610-072",
                    OrderDate = DateTime.UtcNow.AddDays(-2),
                    OrderStatus = "SHIPPING",
                    OrderStatusDisplayName = "Đang giao hàng",
                    OrderStatusBadgeClass = "bg-purple-100 text-purple-800 border-purple-200",
                    TotalAmount = 3200000m,
                    TotalItemsCount = 1,
                    PrimaryProductName = "Serum Phục Hồi Da SkinCeuticals Hydrating B5 Gel 30ml",
                    RemainingProductsCount = 0,
                    ProductThumbnails = new List<string>
                    {
                        "/images/products/skincare/serum-phuc-hoi-la-roche-posay-hyalu-b5-pure-30ml.jpg"
                    },
                    PaymentMethod = "VNPAY",
                    PaymentStatus = "PAID"
                },
                new()
                {
                    Id = 104,
                    OrderCode = "ORD-202610-044",
                    OrderDate = DateTime.UtcNow.AddDays(-6),
                    OrderStatus = "COMPLETED",
                    OrderStatusDisplayName = "Giao thành công",
                    OrderStatusBadgeClass = "bg-emerald-100 text-emerald-800 border-emerald-200",
                    TotalAmount = 1250000m,
                    TotalItemsCount = 2,
                    PrimaryProductName = "Son Thỏi Lì Cao Cấp MAC Matte Lipstick - Tone Chili",
                    RemainingProductsCount = 1,
                    ProductThumbnails = new List<string>
                    {
                        "/images/products/makeup/son-thoi-li-mac-macximal-silky-matte-lipstick-ruby-woo-35g.jpg",
                        "/images/products/skincare/sua-chong-nang-anessa-perfect-uv-sunscreen-skincare-milk-60ml.jpg"
                    },
                    PaymentMethod = "COD",
                    PaymentStatus = "PAID"
                },
                new()
                {
                    Id = 105,
                    OrderCode = "ORD-202609-012",
                    OrderDate = DateTime.UtcNow.AddDays(-18),
                    OrderStatus = "CANCELLED",
                    OrderStatusDisplayName = "Đã hủy",
                    OrderStatusBadgeClass = "bg-slate-100 text-slate-600 border-slate-200",
                    TotalAmount = 980000m,
                    TotalItemsCount = 1,
                    PrimaryProductName = "Toner Tẩy Da Chết Hóa Học Paula's Choice 2% BHA Liquid 118ml",
                    RemainingProductsCount = 0,
                    ProductThumbnails = new List<string>
                    {
                        "/images/products/skincare/dung-dich-tay-te-bao-chet-paulas-choice-skin-perfecting-2-bha-118ml.png"
                    },
                    PaymentMethod = "COD",
                    PaymentStatus = "CANCELLED"
                }
            };

            // Ưu tiên đưa các đơn thật từ DB lên đầu
            var combined = dbOrders.Select(MapToOrderSummaryDto).ToList();
            foreach (var demo in demoList)
            {
                if (!combined.Any(c => c.OrderCode == demo.OrderCode))
                {
                    combined.Add(demo);
                }
            }

            return combined;
        }

        private CustomerOrderSummaryDto MapToOrderSummaryDto(Order o)
        {
            var details = o.OrderDetails?.ToList() ?? new List<OrderDetail>();
            var firstItem = details.FirstOrDefault();

            string statusText = o.OrderStatus switch
            {
                "PENDING" => "Chờ xác nhận",
                "CONFIRMED" or "ALLOCATED" or "PROCESSING" => "Đang đóng gói (Chuẩn FEFO)",
                "SHIPPING" => "Đang giao hàng",
                "COMPLETED" => "Hoàn thành",
                "CANCELLED" => "Đã hủy",
                _ => o.OrderStatus
            };

            string badgeClass = o.OrderStatus switch
            {
                "PENDING" => "bg-amber-100 text-amber-800 border-amber-200",
                "CONFIRMED" or "ALLOCATED" or "PROCESSING" => "bg-blue-100 text-blue-800 border-blue-200",
                "SHIPPING" => "bg-purple-100 text-purple-800 border-purple-200",
                "COMPLETED" => "bg-emerald-100 text-emerald-800 border-emerald-200",
                "CANCELLED" => "bg-slate-100 text-slate-600 border-slate-200",
                _ => "bg-slate-100 text-slate-600 border-slate-200"
            };

            return new CustomerOrderSummaryDto
            {
                Id = o.Id,
                OrderCode = o.OrderCode,
                OrderDate = o.CreatedAt,
                OrderStatus = o.OrderStatus,
                OrderStatusDisplayName = statusText,
                OrderStatusBadgeClass = badgeClass,
                TotalAmount = o.TotalAmount,
                TotalItemsCount = details.Sum(d => d.Quantity),
                PrimaryProductName = firstItem?.Product?.Name ?? "Sản phẩm mỹ phẩm cao cấp",
                RemainingProductsCount = Math.Max(0, details.Count - 1),
                ProductThumbnails = details.Take(4).Select(d => ProductImageSourceRegistry.ResolveProductImageUrl(d.Product?.ThumbnailUrl, d.Product?.Sku, d.Product?.Slug, d.Product?.Name)).ToList(),
                PaymentMethod = o.PaymentMethod,
                PaymentStatus = o.PaymentStatus
            };
        }

        private CustomerOrderDetailViewModel MapToOrderDetailViewModel(Order o)
        {
            int step = o.OrderStatus switch
            {
                "PENDING" => 1,
                "CONFIRMED" or "ALLOCATED" => 2,
                "PROCESSING" => 3,
                "SHIPPING" => 4,
                "COMPLETED" => 5,
                _ => 1
            };

            var events = BuildTimelineEvents(step, o.CreatedAt, o.OrderStatus);

            var items = o.OrderDetails.Select(d => new CustomerOrderItemDto
            {
                ProductId = d.ProductId,
                ProductName = d.Product?.Name ?? "Mỹ phẩm cao cấp",
                Sku = d.Product?.Sku ?? "OMNI-SKU",
                ThumbnailUrl = ProductImageSourceRegistry.ResolveProductImageUrl(d.Product?.ThumbnailUrl, d.Product?.Sku, d.Product?.Slug, d.Product?.Name),
                VariantName = d.Product?.Unit ?? "Tiêu chuẩn",
                FefoBatchNumber = d.ProductBatch?.BatchNumber ?? "FEFO-2026-HQ01",
                FefoExpiryDate = d.ProductBatch?.ExpDate ?? DateTime.UtcNow.AddMonths(28),
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                DiscountRate = d.DiscountRate,
                LineTotal = d.LineTotal
            }).ToList();

            int pointsEarned = (int)Math.Round(o.TotalAmount / 10000m * 1.5m);

            return new CustomerOrderDetailViewModel
            {
                Id = o.Id,
                OrderCode = o.OrderCode,
                OrderDate = o.CreatedAt,
                OrderStatus = o.OrderStatus,
                OrderStatusDisplayName = o.OrderStatus switch
                {
                    "PENDING" => "Chờ xác nhận",
                    "CONFIRMED" or "ALLOCATED" or "PROCESSING" => "Đang đóng gói (Chuẩn FEFO)",
                    "SHIPPING" => "Đang giao hàng",
                    "COMPLETED" => "Giao thành công",
                    "CANCELLED" => "Đã hủy",
                    _ => o.OrderStatus
                },
                OrderStatusBadgeClass = o.OrderStatus switch
                {
                    "PENDING" => "bg-amber-100 text-amber-800 border-amber-200",
                    "CONFIRMED" or "ALLOCATED" or "PROCESSING" => "bg-blue-100 text-blue-800 border-blue-200",
                    "SHIPPING" => "bg-purple-100 text-purple-800 border-purple-200",
                    "COMPLETED" => "bg-emerald-100 text-emerald-800 border-emerald-200",
                    _ => "bg-slate-100 text-slate-600 border-slate-200"
                },
                PaymentMethod = o.PaymentMethod,
                PaymentStatus = o.PaymentStatus,
                OrderNote = o.Note,
                TimelineStep = step,
                TimelineEvents = events,
                RecipientName = o.CustomerName,
                RecipientPhone = o.CustomerPhone,
                ShippingAddress = o.ShippingAddress ?? "Căn hộ 12.08, Tháp Sapphire 1, Saigon Pearl, 92 Nguyễn Hữu Cảnh, P. 22, Q. Bình Thạnh, TP. HCM",
                CarrierName = "Giao Hàng Nhanh (GHN Express - Tracking: GHN-89382026VN)",
                TrackingCode = "GHN-89382026VN",
                Items = items,
                SubTotal = o.SubTotal > 0 ? o.SubTotal : items.Sum(i => i.LineTotal),
                ShippingFee = o.ShippingFee,
                DiscountAmount = o.DiscountAmount,
                AppliedVoucherCode = o.DiscountAmount > 0 ? "CHAOMOI10-VIP" : null,
                TotalAmount = o.TotalAmount,
                LoyaltyPointsEarned = pointsEarned,
                HasVatRequested = o.Invoice != null,
                VatInfo = o.Invoice != null ? new VatInvoiceRequestDto
                {
                    OrderId = o.Id,
                    TaxCode = o.Invoice.CustomerTaxCode ?? "",
                    CompanyName = o.Invoice.CompanyName ?? "",
                    RequestedAt = o.Invoice.InvoiceDate
                } : null
            };
        }

        private CustomerOrderDetailViewModel GetDemoOrderDetail(int id, string orderCode, string name, string phone)
        {
            var events = BuildTimelineEvents(3, DateTime.UtcNow.AddDays(-1), "PROCESSING");

            return new CustomerOrderDetailViewModel
            {
                Id = id,
                OrderCode = orderCode,
                OrderDate = DateTime.UtcNow.AddDays(-1),
                OrderStatus = "PROCESSING",
                OrderStatusDisplayName = "Đang đóng gói (Chuẩn FEFO)",
                OrderStatusBadgeClass = "bg-blue-100 text-blue-800 border-blue-200",
                PaymentMethod = "VNPAY",
                PaymentStatus = "PAID",
                OrderNote = "Vui lòng giao trong giờ hành chính, gọi trước 15 phút.",
                TimelineStep = 3,
                TimelineEvents = events,
                RecipientName = name,
                RecipientPhone = phone,
                ShippingAddress = "Căn hộ 12.08, Tháp Sapphire 1, Saigon Pearl, 92 Nguyễn Hữu Cảnh, Phường 22, Quận Bình Thạnh, TP. Hồ Chí Minh",
                CarrierName = "Giao Hàng Hỏa Tốc 2H (Omni VIP Express)",
                TrackingCode = "GHN-VIP-89382026VN",
                Items = new List<CustomerOrderItemDto>
                {
                    new()
                    {
                        ProductId = 1,
                        ProductName = "Nước hoa Maison Margiela Replica Jazz Club EDT",
                        Sku = "MM-REPLICA-JAZZ-100",
                        ThumbnailUrl = "/images/products/perfume/nuoc-hoa-unisex-maison-margiela-replica-jazz-club-100ml.jpg",
                        VariantName = "Dung tích 100ml",
                        FefoBatchNumber = "LOT-2026-FRAG08",
                        FefoExpiryDate = DateTime.UtcNow.AddMonths(36),
                        Quantity = 1,
                        UnitPrice = 3450000m,
                        DiscountRate = 0,
                        LineTotal = 3450000m
                    },
                    new()
                    {
                        ProductId = 2,
                        ProductName = "Kem Dưỡng Ẩm Chuyên Sâu Clinique Moisture Surge 100H Auto-Replenishing Hydrator",
                        Sku = "CLINIQUE-MS-50ML",
                        ThumbnailUrl = "/images/products/skincare/gel-duong-am-clinique-moisture-surge-100h-50ml.jpg",
                        VariantName = "Hũ 50ml",
                        FefoBatchNumber = "FEFO-CLN-202611",
                        FefoExpiryDate = DateTime.UtcNow.AddMonths(26),
                        Quantity = 1,
                        UnitPrice = 1150000m,
                        DiscountRate = 10,
                        LineTotal = 1035000m
                    }
                },
                SubTotal = 4600000m,
                ShippingFee = 0,
                DiscountAmount = 250000m,
                AppliedVoucherCode = "VIPGOLD-250K",
                TotalAmount = 4350000m,
                LoyaltyPointsEarned = 650,
                HasVatRequested = false
            };
        }

        private List<OrderTimelineEventDto> BuildTimelineEvents(int currentStep, DateTime orderDate, string status)
        {
            if (status == "CANCELLED")
            {
                return new List<OrderTimelineEventDto>
                {
                    new() { StepIndex = 1, Title = "Đặt hàng thành công", Description = "Đơn hàng đã được tạo trên hệ thống", FormattedTime = orderDate.ToString("dd/MM/yyyy HH:mm"), IsCompleted = true, IsCurrent = false },
                    new() { StepIndex = 2, Title = "Đã hủy đơn hàng", Description = "Đơn hàng đã bị hủy theo yêu cầu của khách hàng hoặc hết thời gian thanh toán", FormattedTime = orderDate.AddHours(2).ToString("dd/MM/yyyy HH:mm"), IsCompleted = true, IsCurrent = true }
                };
            }

            return new List<OrderTimelineEventDto>
            {
                new()
                {
                    StepIndex = 1,
                    Title = "Đặt hàng thành công",
                    Description = "Hệ thống Omni Beauty đã tiếp nhận đơn hàng của bạn",
                    FormattedTime = orderDate.ToString("dd/MM/yyyy HH:mm"),
                    IsCompleted = currentStep >= 1,
                    IsCurrent = currentStep == 1
                },
                new()
                {
                    StepIndex = 2,
                    Title = "Đã xác nhận",
                    Description = "Bộ phận CSKH & AI đã xác minh thông tin đơn và phương thức thanh toán",
                    FormattedTime = currentStep >= 2 ? orderDate.AddMinutes(25).ToString("dd/MM/yyyy HH:mm") : null,
                    IsCompleted = currentStep >= 2,
                    IsCurrent = currentStep == 2
                },
                new()
                {
                    StepIndex = 3,
                    Title = "Đang chuẩn bị hàng FEFO",
                    Description = "Kho thông minh phân bổ sản phẩm theo hạn dùng xa nhất (Thuật toán FEFO) & đóng hộp chống va đập",
                    FormattedTime = currentStep >= 3 ? orderDate.AddHours(2).ToString("dd/MM/yyyy HH:mm") : null,
                    IsCompleted = currentStep >= 3,
                    IsCurrent = currentStep == 3
                },
                new()
                {
                    StepIndex = 4,
                    Title = "Bàn giao Shipper",
                    Description = "Đơn hàng đã được bàn giao cho đối tác vận chuyển GHN Express / Hỏa tốc 2H",
                    FormattedTime = currentStep >= 4 ? orderDate.AddHours(5).ToString("dd/MM/yyyy HH:mm") : null,
                    IsCompleted = currentStep >= 4,
                    IsCurrent = currentStep == 4
                },
                new()
                {
                    StepIndex = 5,
                    Title = "Giao thành công",
                    Description = "Đơn hàng đã được giao tận tay quý khách. Điểm thưởng Omni Club đã được cộng vào ví!",
                    FormattedTime = currentStep >= 5 ? orderDate.AddDays(1).ToString("dd/MM/yyyy HH:mm") : null,
                    IsCompleted = currentStep >= 5,
                    IsCurrent = currentStep == 5
                }
            };
        }

        // =========================================================================
        // 7. SẢN PHẨM ĐANG THEO DÕI (BACK-IN-STOCK & PRICE DROP ALERTS) - GAP-05
        // =========================================================================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Subscriptions()
        {
            var (userId, user) = await GetCurrentCustomerUserAsync();
            if (userId <= 0)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = "/Customer/Subscriptions" });
            }

            var subscriptions = _subscriptionService != null
                ? await _subscriptionService.GetCustomerSubscriptionsAsync(userId)
                : new List<CustomerSubscriptionDto>();

            ViewBag.CustomerName = user?.FullName ?? "Quý Khách";
            return View(subscriptions);
        }

        // =========================================================================
        // 8. MINH BẠCH HẬU MUA HÀNG: TRA CỨU ĐƠN VÃNG LAI, HỦY ĐƠN & ĐỔI TRẢ (GAP-07)
        // =========================================================================

        [AllowAnonymous]
        [HttpGet("/tra-cuu-don-hang")]
        [HttpGet("/Customer/TrackOrder")]
        public async Task<IActionResult> TrackOrder([FromQuery] string? orderCode = null, [FromQuery] string? phone = null)
        {
            if (!string.IsNullOrWhiteSpace(orderCode) && !string.IsNullOrWhiteSpace(phone) && _orderManagementService != null)
            {
                var lookupResult = await _orderManagementService.LookupGuestOrderAsync(orderCode, phone);
                return View("TrackOrder", lookupResult);
            }

            return View("TrackOrder", new GuestOrderLookupViewModel
            {
                OrderCode = orderCode?.Trim() ?? string.Empty,
                PhoneNumber = phone?.Trim() ?? string.Empty,
                HasSearched = false
            });
        }

        [AllowAnonymous]
        [HttpPost("/tra-cuu-don-hang")]
        [HttpPost("/Customer/TrackOrder")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TrackOrderPost([FromForm] string orderCode, [FromForm] string phoneNumber)
        {
            if (_orderManagementService == null)
            {
                return View("TrackOrder", new GuestOrderLookupViewModel
                {
                    OrderCode = orderCode ?? string.Empty,
                    PhoneNumber = phoneNumber ?? string.Empty,
                    HasSearched = true,
                    ErrorMessage = "Dịch vụ tra cứu đơn hàng đang tạm bảo trì."
                });
            }

            var lookupResult = await _orderManagementService.LookupGuestOrderAsync(orderCode, phoneNumber);
            return View("TrackOrder", lookupResult);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder([FromBody] CancelOrderByCustomerRequest request)
        {
            if (request == null || request.OrderId <= 0)
            {
                return BadRequest(new { success = false, message = "Thông tin yêu cầu hủy đơn không hợp lệ." });
            }

            int? currentUserId = GetAuthenticatedUserId();
            if (!currentUserId.HasValue)
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new { success = false, message = "Vui lòng đăng nhập để hủy đơn hàng." });
            }

            if (_orderManagementService == null)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { success = false, message = "Dịch vụ đơn hàng không khả dụng." });
            }

            var (success, message) = await _orderManagementService.CancelOrderByCustomerAsync(
                request.OrderId, currentUserId.Value, request.Reason, request.Note);

            if (!success && message.Contains("không có quyền", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message });
            }

            return Json(new { success, message });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReturnRequest(
            [FromForm] int orderId,
            [FromForm] string reason,
            [FromForm] string? description,
            [FromForm] List<IFormFile>? photos)
        {
            int? currentUserId = GetAuthenticatedUserId();
            if (!currentUserId.HasValue)
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new { success = false, message = "Vui lòng đăng nhập để gửi yêu cầu đổi/trả." });
            }

            if (_orderManagementService == null)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { success = false, message = "Dịch vụ đổi/trả không khả dụng." });
            }

            var savedUrls = new List<string>();
            if (photos != null && photos.Any())
            {
                if (photos.Count > 3)
                {
                    return Json(new { success = false, message = "Chỉ được tải lên tối đa 3 ảnh minh chứng." });
                }

                string webRoot = _env?.WebRootPath ?? System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot");
                string uploadDir = System.IO.Path.Combine(webRoot, "uploads", "returns");
                System.IO.Directory.CreateDirectory(uploadDir);

                var allowedExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

                foreach (var file in photos)
                {
                    if (file.Length <= 0) continue;
                    if (file.Length > 2 * 1024 * 1024)
                    {
                        return Json(new { success = false, message = $"Ảnh '{file.FileName}' vượt quá dung lượng tối đa 2MB." });
                    }

                    string ext = System.IO.Path.GetExtension(file.FileName);
                    if (string.IsNullOrWhiteSpace(ext) || !allowedExts.Contains(ext))
                    {
                        return Json(new { success = false, message = "Chỉ chấp nhận định dạng ảnh .jpg, .png hoặc .webp." });
                    }

                    using var stream = file.OpenReadStream();
                    byte[] header = new byte[12];
                    int readBytes = await stream.ReadAsync(header.AsMemory(0, Math.Min(12, (int)file.Length)));
                    if (!IsValidImageMagicBytes(header, readBytes))
                    {
                        return Json(new { success = false, message = $"Tệp '{file.FileName}' không phải là ảnh hợp lệ." });
                    }

                    stream.Position = 0;
                    string safeFileName = $"ret_{orderId}_{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
                    string fullPath = System.IO.Path.Combine(uploadDir, safeFileName);
                    using (var fs = new System.IO.FileStream(fullPath, System.IO.FileMode.Create))
                    {
                        await stream.CopyToAsync(fs);
                    }

                    savedUrls.Add($"/uploads/returns/{safeFileName}");
                }
            }

            var (success, message, returnReq) = await _orderManagementService.SubmitReturnRequestAsync(
                orderId, currentUserId.Value, reason, description, savedUrls);

            if (!success && message.Contains("không có quyền", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message });
            }

            return Json(new { success, message, returnRequest = returnReq });
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderStatusJson(int id)
        {
            if (_orderManagementService == null || id <= 0)
            {
                return NotFound(new { success = false });
            }

            int? currentUserId = GetAuthenticatedUserId();
            bool isAdmin = User?.Identity?.IsAuthenticated == true &&
                           (User.IsInRole("SuperAdmin") || User.IsInRole("SUPER_ADMIN") || User.IsInRole("Admin") || User.IsInRole("StoreManager"));

            var (allowed, errorMsg, detail) = await _orderManagementService.GetCustomerOrderDetailAsync(
                id, null, currentUserId, isAdmin);

            if (!allowed || detail == null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = errorMsg });
            }

            return Json(new
            {
                success = true,
                orderId = detail.Id,
                orderCode = detail.OrderCode,
                orderStatus = detail.OrderStatus,
                orderStatusDisplayName = detail.OrderStatusDisplayName,
                orderStatusBadgeClass = detail.OrderStatusBadgeClass,
                paymentStatus = detail.PaymentStatus,
                timelineStep = detail.TimelineStep,
                trackingCode = detail.TrackingCode,
                carrierName = detail.CarrierName,
                canCancel = detail.CanCancel,
                canRequestReturn = detail.CanRequestReturn,
                returnDaysRemaining = detail.ReturnDaysRemaining,
                timelineEvents = detail.TimelineEvents,
                statusHistories = detail.StatusHistories,
                activeReturnRequest = detail.ActiveReturnRequest
            });
        }

        private static bool IsValidImageMagicBytes(byte[] header, int length)
        {
            if (length < 4) return false;
            // JPEG: FF D8 FF
            if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return true;
            // PNG: 89 50 4E 47
            if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47) return true;
            // WEBP: RIFF....WEBP
            if (length >= 12 &&
                header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
                header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
            {
                return true;
            }
            return false;
        }
    }
}
