using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Domain.Entities;

namespace Omnichannel.Infrastructure.Data
{
    public static partial class DbInitializer
    {
        /// <summary>
        /// Đảm bảo dữ liệu demo chân thực và đầy đủ trước giờ thuyết trình (Phiên B2):
        /// - ≥ 60 sản phẩm active (hệ thống hiện có 98 sản phẩm)
        /// - ≥ 15 sản phẩm có lô cận date (đã có EnsureFefoBatchesSynchronizedAsync)
        /// - ≥ 30 khách hàng đa dạng hạng Loyalty, có ≥ 8 khách hàng sinh nhật trong tháng hiện tại
        /// - Voucher demo unexpired: DEMO10, FREESHIP, COMBO15
        /// - ≥ 200 đánh giá sản phẩm tự nhiên tiếng Việt, đa dạng số sao và tiêu chí phụ
        /// - ≥ 150 đơn hàng trải đều 90 ngày, đa kênh (ONLINE 60% / POS 40%), đỉnh bán cuối tuần
        /// </summary>
        public static async Task EnsureRealisticDemoDataSeededAsync(ApplicationDbContext context, ILogger? logger = null)
        {
            var strategy = context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    // 1. SEED / ENSURE DEMO COUPONS
                    await EnsureDemoCouponsAsync(context, logger);

                    // 2. SEED / ENSURE DEMO CUSTOMERS & LOYALTY (≥ 30 customers, birthdays in current month)
                    await EnsureDemoCustomersAndLoyaltyAsync(context, logger);

                    // 3. SEED / ENSURE DEMO ORDERS (≥ 150 orders across past 90 days with ORD-SEED- prefix)
                    await EnsureDemoOrdersAcross90DaysAsync(context, logger);

                    // 4. SEED / ENSURE DEMO PRODUCT REVIEWS (≥ 200 natural Vietnamese reviews)
                    await EnsureDemoProductReviewsAsync(context, logger);

                    await transaction.CommitAsync();
                    LogInfo(logger, "[DbInitializer] Đã hoàn tất đồng bộ chuẩn hóa dữ liệu Demo (Coupons, 30+ Khách hàng, 150+ Đơn 90 ngày, 200+ Review).");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    LogError(logger, ex, "[DbInitializer] Lỗi khi chuẩn hóa dữ liệu Demo (EnsureRealisticDemoDataSeededAsync).");
                    throw;
                }
            });
        }

        // =========================================================================
        // 1. COUPONS: DEMO10, FREESHIP, COMBO15
        // =========================================================================
        private static async Task EnsureDemoCouponsAsync(ApplicationDbContext context, ILogger? logger)
        {
            var now = DateTime.UtcNow;

            // Coupon DEMO10: Giảm 10% tối đa 100k
            if (!await context.Coupons.AnyAsync(c => c.CouponCode == "DEMO10"))
            {
                context.Coupons.Add(new Coupon
                {
                    CouponCode = "DEMO10",
                    Description = "Mã trải nghiệm Demo: Giảm 10% tối đa 100.000đ cho đơn hàng",
                    DiscountType = "PERCENTAGE",
                    DiscountValue = 10m,
                    MinOrderValue = 0m,
                    MaxDiscountAmount = 100000m,
                    ValidFrom = now.AddMonths(-3),
                    ValidTo = now.AddYears(5),
                    UsageLimit = 10000,
                    UsageLimitPerUser = 100,
                    UsageCount = 0,
                    IsActive = true,
                    CreatedAt = now.AddMonths(-3)
                });
            }

            // Coupon FREESHIP: Miễn phí vận chuyển 30k
            if (!await context.Coupons.AnyAsync(c => c.CouponCode == "FREESHIP"))
            {
                context.Coupons.Add(new Coupon
                {
                    CouponCode = "FREESHIP",
                    Description = "Mã trải nghiệm Demo: Miễn phí vận chuyển 30.000đ toàn quốc",
                    DiscountType = "FIXED_AMOUNT",
                    DiscountValue = 30000m,
                    MinOrderValue = 0m,
                    MaxDiscountAmount = 30000m,
                    ValidFrom = now.AddMonths(-3),
                    ValidTo = now.AddYears(5),
                    UsageLimit = 10000,
                    UsageLimitPerUser = 100,
                    UsageCount = 0,
                    IsActive = true,
                    CreatedAt = now.AddMonths(-3)
                });
            }

            // Coupon COMBO15: Giảm 15% trọn bộ Routine
            if (!await context.Coupons.AnyAsync(c => c.CouponCode == "COMBO15"))
            {
                context.Coupons.Add(new Coupon
                {
                    CouponCode = "COMBO15",
                    Description = "Ưu đãi Combo Routine 4 bước chăm sóc da - Giảm 15% trọn bộ",
                    DiscountType = "PERCENTAGE",
                    DiscountValue = 15m,
                    MinOrderValue = 0m,
                    MaxDiscountAmount = 250000m,
                    ValidFrom = now.AddMonths(-3),
                    ValidTo = now.AddYears(5),
                    UsageLimit = 10000,
                    UsageLimitPerUser = 100,
                    UsageCount = 0,
                    IsActive = true,
                    CreatedAt = now.AddMonths(-3)
                });
            }

            await context.SaveChangesAsync();
        }

        // =========================================================================
        // 2. CUSTOMERS & LOYALTY (≥ 30 customers, birthdays in current month)
        // =========================================================================
        private static async Task EnsureDemoCustomersAndLoyaltyAsync(ApplicationDbContext context, ILogger? logger)
        {
            var customerCount = await context.Users.CountAsync(u => u.UserType == 1);
            if (customerCount >= 30) return;

            var customerRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Customer")
                              ?? await context.Roles.FirstOrDefaultAsync();

            var tiers = await context.MemberTiers.OrderBy(t => t.MinSpend).ToListAsync();
            var bronzeTier = tiers.FirstOrDefault(t => t.TierName == "Bronze") ?? tiers[0];
            var silverTier = tiers.FirstOrDefault(t => t.TierName == "Silver") ?? bronzeTier;
            var goldTier = tiers.FirstOrDefault(t => t.TierName == "Gold") ?? silverTier;
            var platinumTier = tiers.FirstOrDefault(t => t.TierName == "Platinum") ?? goldTier;
            var diamondTier = tiers.FirstOrDefault(t => t.TierName == "Diamond") ?? platinumTier;

            var currentMonth = DateTime.UtcNow.Month;
            var now = DateTime.UtcNow;

            // Danh sách 24 khách hàng mẫu bổ sung (từ customer03 đến customer26)
            var demoCustomersData = new[]
            {
                new { Username = "customer03", Name = "Lê Thị Mai Anh", Phone = "0901234003", Email = "maianh.le@gmail.com", BirthMonth = currentMonth, BirthDay = 5, Skin = "Da dầu mụn", Tier = goldTier, Spend = 6800000m, Points = 680, Concerns = "[\"Mụn ẩn\", \"Dầu thừa\", \"Lỗ chân lông to\"]", Brands = "[\"La Roche-Posay\", \"Paula's Choice\"]" },
                new { Username = "customer04", Name = "Nguyễn Thu Hà", Phone = "0901234004", Email = "thuha.nguyen@gmail.com", BirthMonth = currentMonth, BirthDay = 12, Skin = "Da nhạy cảm", Tier = silverTier, Spend = 3400000m, Points = 340, Concerns = "[\"Đỏ rát\", \"Kích ứng\", \"Mỏng yếu\"]", Brands = "[\"Bioderma\", \"Klairs\"]" },
                new { Username = "customer05", Name = "Phạm Hoàng Yến", Phone = "0901234005", Email = "hoangyen.pham@gmail.com", BirthMonth = currentMonth, BirthDay = 18, Skin = "Da khô thiếu nước", Tier = platinumTier, Spend = 12500000m, Points = 1250, Concerns = "[\"Bong tróc\", \"Nếp nhăn\", \"Khô căng\"]", Brands = "[\"CeraVe\", \"The Ordinary\"]" },
                new { Username = "customer06", Name = "Vũ Mỹ Linh", Phone = "0901234006", Email = "mylinh.vu@gmail.com", BirthMonth = currentMonth, BirthDay = 22, Skin = "Da hỗn hợp thiên dầu", Tier = diamondTier, Spend = 24000000m, Points = 2400, Concerns = "[\"Thâm mụn\", \"Dầu vùng T\", \"Mụn đầu đen\"]", Brands = "[\"Paula's Choice\", \"Innisfree\"]" },
                new { Username = "customer07", Name = "Đỗ Hương Giang", Phone = "0901234007", Email = "huonggiang.do@gmail.com", BirthMonth = currentMonth, BirthDay = 8, Skin = "Da dầu mụn", Tier = goldTier, Spend = 5900000m, Points = 590, Concerns = "[\"Mụn bọc\", \"Thâm đỏ\", \"Bít tắc\"]", Brands = "[\"La Roche-Posay\", \"SVR\"]" },
                new { Username = "customer08", Name = "Hoàng Kim Ngân", Phone = "0901234008", Email = "kimngan.hoang@gmail.com", BirthMonth = currentMonth, BirthDay = 15, Skin = "Da thường", Tier = bronzeTier, Spend = 1200000m, Points = 120, Concerns = "[\"Làm sáng da\", \"Chống nắng\"]", Brands = "[\"Anessa\", \"L'Oreal\"]" },
                new { Username = "customer09", Name = "Bùi Bích Phương", Phone = "0901234009", Email = "bichphuong.bui@gmail.com", BirthMonth = currentMonth, BirthDay = 27, Skin = "Da khô", Tier = silverTier, Spend = 2800000m, Points = 280, Concerns = "[\"Thiếu ẩm\", \"Lão hóa sớm\"]", Brands = "[\"Hada Labo\", \"CeraVe\"]" },
                new { Username = "customer10", Name = "Đặng Thùy Dung", Phone = "0901234010", Email = "thuydung.dang@gmail.com", BirthMonth = currentMonth, BirthDay = 3, Skin = "Da nhạy cảm", Tier = bronzeTier, Spend = 950000m, Points = 95, Concerns = "[\"Dễ nổi mụn\", \"Dị ứng hương liệu\"]", Brands = "[\"Simple\", \"Bioderma\"]" },
                new { Username = "customer11", Name = "Lê Thanh Thảo", Phone = "0901234011", Email = "thanhthao.le@gmail.com", BirthMonth = (currentMonth % 12) + 1, BirthDay = 10, Skin = "Da dầu mụn", Tier = silverTier, Spend = 3100000m, Points = 310, Concerns = "[\"Mụn đầu đen\", \"Lỗ chân lông to\"]", Brands = "[\"Cosrx\", \"Some By Mi\"]" },
                new { Username = "customer12", Name = "Ngô Quỳnh Anh", Phone = "0901234012", Email = "quynhanh.ngo@gmail.com", BirthMonth = (currentMonth % 12) + 1, BirthDay = 14, Skin = "Da hỗn hợp", Tier = goldTier, Spend = 7400000m, Points = 740, Concerns = "[\"Tàn nhang\", \"Không đều màu\"]", Brands = "[\"The Ordinary\", \"Obagi\"]" },
                new { Username = "customer13", Name = "Dương Cẩm Tú", Phone = "0901234013", Email = "camtu.duong@gmail.com", BirthMonth = (currentMonth % 12) + 1, BirthDay = 20, Skin = "Da dầu thiếu nước", Tier = bronzeTier, Spend = 1600000m, Points = 160, Concerns = "[\"Đổ dầu nhiều\", \"Da xỉn màu\"]", Brands = "[\"Klairs\", \"Laneige\"]" },
                new { Username = "customer14", Name = "Võ Diệu Hoa", Phone = "0901234014", Email = "dieuhoa.vo@gmail.com", BirthMonth = (currentMonth % 12) + 1, BirthDay = 25, Skin = "Da lão hóa", Tier = platinumTier, Spend = 14200000m, Points = 1420, Concerns = "[\"Nếp nhăn\", \"Chảy xệ\", \"Đốm nâu\"]", Brands = "[\"Estee Lauder\", \"Kiehl's\"]" },
                new { Username = "customer15", Name = "Phan Thảo Trang", Phone = "0901234015", Email = "thaotrang.phan@gmail.com", BirthMonth = 1, BirthDay = 8, Skin = "Da khô", Tier = bronzeTier, Spend = 800000m, Points = 80, Concerns = "[\"Khô ráp quanh miệng\"]", Brands = "[\"CeraVe\"]" },
                new { Username = "customer16", Name = "Trịnh Thùy Tiên", Phone = "0901234016", Email = "thuytien.trinh@gmail.com", BirthMonth = 2, BirthDay = 16, Skin = "Da dầu mụn", Tier = silverTier, Spend = 2600000m, Points = 260, Concerns = "[\"Mụn viêm\", \"Thâm đỏ\"]", Brands = "[\"La Roche-Posay\"]" },
                new { Username = "customer17", Name = "Lâm Mỹ Tâm", Phone = "0901234017", Email = "mytam.lam@gmail.com", BirthMonth = 3, BirthDay = 19, Skin = "Da nhạy cảm", Tier = goldTier, Spend = 6200000m, Points = 620, Concerns = "[\"Hàng rào da tổn thương\"]", Brands = "[\"Avene\", \"Bioderma\"]" },
                new { Username = "customer18", Name = "Hồ Bích Trâm", Phone = "0901234018", Email = "bichtram.ho@gmail.com", BirthMonth = 4, BirthDay = 21, Skin = "Da hỗn hợp thiên dầu", Tier = bronzeTier, Spend = 1450000m, Points = 145, Concerns = "[\"Bít tắc lỗ chân lông\"]", Brands = "[\"Paula's Choice\"]" },
                new { Username = "customer19", Name = "Đoàn Thiên Ân", Phone = "0901234019", Email = "thienan.doan@gmail.com", BirthMonth = 5, BirthDay = 11, Skin = "Da dầu", Tier = silverTier, Spend = 3900000m, Points = 390, Concerns = "[\"Dầu nhờn bóng loáng\"]", Brands = "[\"SVR\", \"Vichy\"]" },
                new { Username = "customer20", Name = "Vũ Hoàng My", Phone = "0901234020", Email = "hoangmy.vu@gmail.com", BirthMonth = 6, BirthDay = 17, Skin = "Da thường", Tier = bronzeTier, Spend = 500000m, Points = 50, Concerns = "[\"Chống lão hóa phòng ngừa\"]", Brands = "[\"L'Oreal\"]" },
                new { Username = "customer21", Name = "Đinh Ngọc Diệp", Phone = "0901234021", Email = "ngocdiep.dinh@gmail.com", BirthMonth = 7, BirthDay = 24, Skin = "Da khô nhạy cảm", Tier = goldTier, Spend = 8100000m, Points = 810, Concerns = "[\"Bong tróc mẩn ngứa\"]", Brands = "[\"CeraVe\", \"Klairs\"]" },
                new { Username = "customer22", Name = "Trần Khánh Vy", Phone = "0901234022", Email = "khanhvy.tran@gmail.com", BirthMonth = 8, BirthDay = 29, Skin = "Da dầu mụn", Tier = silverTier, Spend = 4200000m, Points = 420, Concerns = "[\"Mụn tuổi dậy thì\", \"Thâm mụn\"]", Brands = "[\"The Ordinary\", \"Cosrx\"]" },
                new { Username = "customer23", Name = "Nguyễn Minh Châu", Phone = "0901234023", Email = "minhchau.nguyen@gmail.com", BirthMonth = 9, BirthDay = 4, Skin = "Da hỗn hợp", Tier = bronzeTier, Spend = 1100000m, Points = 110, Concerns = "[\"Lỗ chân lông to\"]", Brands = "[\"Innisfree\"]" },
                new { Username = "customer24", Name = "Tạ Yến Nhi", Phone = "0901234024", Email = "yennhi.ta@gmail.com", BirthMonth = 10, BirthDay = 13, Skin = "Da nám tàn nhang", Tier = platinumTier, Spend = 15800000m, Points = 1580, Concerns = "[\"Nám chân sâu\", \"Sạm màu\"]", Brands = "[\"Obagi\", \"Skinceuticals\"]" },
                new { Username = "customer25", Name = "Trịnh Kim Chi", Phone = "0901234025", Email = "kimchi.trinh@gmail.com", BirthMonth = currentMonth, BirthDay = 1, Skin = "Da dầu mụn", Tier = diamondTier, Spend = 26500000m, Points = 2650, Concerns = "[\"Mụn dai dẳng\", \"Sẹo rỗ\"]", Brands = "[\"Paula's Choice\", \"La Roche-Posay\"]" },
                new { Username = "customer26", Name = "Chu Thúy Nga", Phone = "0901234026", Email = "thuynga.chu@gmail.com", BirthMonth = currentMonth, BirthDay = 30, Skin = "Da khô", Tier = silverTier, Spend = 3800000m, Points = 380, Concerns = "[\"Khô nứt nẻ\"]", Brands = "[\"Bioderma\", \"Hada Labo\"]" }
            };

            var pwdHash = HashPassword("Admin@123");

            foreach (var item in demoCustomersData)
            {
                if (await context.Users.AnyAsync(u => u.Username == item.Username)) continue;

                var user = new User
                {
                    Username = item.Username,
                    NormalizedUsername = item.Username.ToUpperInvariant(),
                    FullName = item.Name,
                    Email = item.Email,
                    PhoneNumber = item.Phone,
                    PasswordHash = pwdHash,
                    UserType = 1, // Khách hàng
                    IsActive = true,
                    CreatedAt = now.AddMonths(-6)
                };

                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();

                if (customerRole != null)
                {
                    await context.UserRoles.AddAsync(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = customerRole.Id
                    });
                }

                // Profile
                var profile = new CustomerProfile
                {
                    UserId = user.Id,
                    DateOfBirth = new DateTime(1996 + (user.Id % 8), item.BirthMonth, Math.Min(item.BirthDay, 28)),
                    Gender = "Female",
                    CurrentSkinType = item.Skin,
                    SkinConcerns = item.Concerns,
                    PreferredBrands = item.Brands,
                    CreatedAt = now.AddMonths(-6)
                };
                await context.CustomerProfiles.AddAsync(profile);

                // Loyalty Wallet
                var wallet = new LoyaltyWallet
                {
                    UserId = user.Id,
                    CurrentTierId = item.Tier.Id,
                    TotalPointsEarned = item.Points,
                    CurrentBalance = item.Points,
                    TotalSpend12Months = item.Spend,
                    MemberSince = now.AddMonths(-6),
                    LastTierEvaluationDate = now.AddDays(-15)
                };
                await context.LoyaltyWallets.AddAsync(wallet);
            }

            await context.SaveChangesAsync();
            LogInfo(logger, "[DbInitializer] Đã khởi tạo danh sách khách hàng và ví Loyalty chuẩn hóa (>= 30 khách hàng, có sinh nhật tháng hiện tại).");
        }

        // =========================================================================
        // 3. ORDERS: ≥ 150 ORDERS ACROSS 90 DAYS (ORD-SEED-)
        // =========================================================================
        private static async Task EnsureDemoOrdersAcross90DaysAsync(ApplicationDbContext context, ILogger? logger)
        {
            var existingCodes = new HashSet<string>(await context.Orders.Select(o => o.OrderCode).ToListAsync());
            var existingSeedOrdersCount = existingCodes.Count(c => c.StartsWith("ORD-SEED-"));
            if (existingSeedOrdersCount >= 155) return;

            var stores = await context.Stores.Where(s => s.IsActive).ToListAsync();
            if (!stores.Any()) return;

            var hqStore = stores.FirstOrDefault(s => s.Code == "SH-Q1" || s.Code == "HQ-001") ?? stores[0];
            var bdStore = stores.FirstOrDefault(s => s.Code == "SH-BD" || s.Code == "SH-CG") ?? (stores.Count > 1 ? stores[1] : hqStore);

            var productsWithBatches = await context.Products
                .Include(p => p.Batches)
                .Where(p => p.IsActive && p.Batches.Any())
                .ToListAsync();

            if (productsWithBatches.Count < 5) return;

            var customers = await context.Users.Where(u => u.UserType == 1).ToListAsync();
            var now = DateTime.UtcNow;

            int neededOrders = 155 - existingSeedOrdersCount;
            int seededCount = 0;
            var rand = new Random(42); // Cố định seed cho tính xác định (determistic)

            // Trải đều 90 ngày (từ day -89 đến day 0)
            for (int dayOffset = 89; dayOffset >= 0 && seededCount < neededOrders; dayOffset--)
            {
                var orderDate = now.AddDays(-dayOffset);
                var isWeekend = orderDate.DayOfWeek == DayOfWeek.Saturday || orderDate.DayOfWeek == DayOfWeek.Sunday;
                var isPayday = orderDate.Day == 15 || orderDate.Day == 25 || orderDate.Day == 28;

                // Tăng tần suất đơn vào cuối tuần và ngày lương (Weekend + Payday Surge)
                int ordersForDay = isWeekend ? rand.Next(2, 4) : (isPayday ? rand.Next(2, 4) : rand.Next(1, 3));

                for (int i = 0; i < ordersForDay && seededCount < neededOrders; i++)
                {
                    seededCount++;
                    var hour = rand.Next(9, 21);
                    var minute = rand.Next(0, 59);
                    var exactDate = orderDate.Date.AddHours(hour).AddMinutes(minute);

                    // Phân bổ kênh: 60% Website, 40% POS
                    bool isOnline = (seededCount % 10) < 6;
                    var store = isOnline ? hqStore : (rand.Next(0, 2) == 0 ? hqStore : bdStore);
                    var source = isOnline ? "WEBSITE" : "POS";

                    // Trạng thái đơn hàng: 87% Hoàn tất, 7% Đang xử lý, 4% Chờ duyệt, 2% Đã hủy
                    string orderStatus;
                    string paymentStatus;
                    int statusRoll = rand.Next(1, 101);
                    if (statusRoll <= 87)
                    {
                        orderStatus = "COMPLETED";
                        paymentStatus = "PAID";
                    }
                    else if (statusRoll <= 94)
                    {
                        orderStatus = "PROCESSING";
                        paymentStatus = "PAID";
                    }
                    else if (statusRoll <= 98)
                    {
                        orderStatus = "PENDING";
                        paymentStatus = "UNPAID";
                    }
                    else
                    {
                        orderStatus = "CANCELLED";
                        paymentStatus = "UNPAID";
                    }

                    // Phương thức thanh toán
                    string paymentMethod;
                    if (isOnline)
                    {
                        int pmRoll = rand.Next(1, 4);
                        paymentMethod = pmRoll == 1 ? "VNPAY" : (pmRoll == 2 ? "COD" : "MOMO");
                    }
                    else
                    {
                        int pmRoll = rand.Next(1, 4);
                        paymentMethod = pmRoll == 1 ? "CASH" : (pmRoll == 2 ? "VNPAY" : "MOMO");
                    }

                    // Khách hàng
                    var customer = customers.Count > 0 ? customers[rand.Next(0, customers.Count)] : null;
                    string custName = customer?.FullName ?? $"Khách Hàng {seededCount:D3}";
                    string custPhone = customer?.PhoneNumber ?? $"090{rand.Next(1000000, 9999999)}";
                    string? custEmail = customer?.Email;
                    string? shippingAddr = isOnline ? $"{rand.Next(12, 380)} Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh" : null;

                    int seq = seededCount + existingSeedOrdersCount;
                    string code;
                    do
                    {
                        seq++;
                        code = $"ORD-SEED-{exactDate:yyyyMMdd}-{seq:D4}";
                    } while (existingCodes.Contains(code));

                    existingCodes.Add(code);

                    var order = new Order
                    {
                        OrderCode = code,
                        StoreId = store.Id,
                        UserId = customer?.Id,
                        OrderSource = source,
                        OrderStatus = orderStatus,
                        PaymentStatus = paymentStatus,
                        PaymentMethod = paymentMethod,
                        CustomerName = custName,
                        CustomerPhone = custPhone,
                        CustomerEmail = custEmail,
                        ShippingAddress = shippingAddr,
                        CreatedAt = exactDate,
                        UpdatedAt = exactDate.AddMinutes(30)
                    };

                    // Chi tiết 1-3 sản phẩm ngẫu nhiên
                    int itemCount = rand.Next(1, 4);
                    var pickedProducts = productsWithBatches.OrderBy(_ => rand.Next()).Take(itemCount).ToList();
                    decimal subTotal = 0m;

                    var details = new List<OrderDetail>();
                    foreach (var prod in pickedProducts)
                    {
                        var batch = prod.Batches.OrderBy(b => b.ExpDate).FirstOrDefault();
                        int qty = rand.Next(1, 3);
                        decimal unitPrice = prod.SellingPrice > 0 ? prod.SellingPrice : 250000m;
                        decimal discountRate = (prod.DiscountPrice.HasValue && prod.DiscountPrice.Value < prod.SellingPrice) ? 10m : 0m;
                        decimal lineGross = qty * unitPrice;
                        decimal lineDisc = lineGross * (discountRate / 100m);
                        decimal lineTotal = Math.Round(lineGross - lineDisc, 2);

                        details.Add(new OrderDetail
                        {
                            Order = order,
                            ProductId = prod.Id,
                            ProductBatchId = batch?.Id,
                            Quantity = qty,
                            UnitPrice = unitPrice,
                            DiscountRate = discountRate,
                            LineTotal = lineTotal
                        });

                        subTotal += lineTotal;
                    }

                    decimal discountAmount = 0m;
                    // Áp voucher cho khoảng 25% đơn hàng
                    if (rand.Next(1, 5) == 1)
                    {
                        discountAmount = Math.Min(subTotal * 0.10m, 50000m);
                    }

                    decimal shippingFee = (isOnline && subTotal < 500000m) ? 30000m : 0m;
                    decimal totalAmount = Math.Max(0m, subTotal - discountAmount + shippingFee);

                    order.SubTotal = subTotal;
                    order.DiscountAmount = discountAmount;
                    order.ShippingFee = shippingFee;
                    order.TotalAmount = totalAmount;
                    order.OrderDetails = details;

                    await context.Orders.AddAsync(order);
                }
            }

            await context.SaveChangesAsync();
            LogInfo(logger, $"[DbInitializer] Đã khởi tạo {seededCount} đơn hàng đa kênh (ORD-SEED-) trải đều 90 ngày với xu hướng doanh thu rõ nét.");
        }

        // =========================================================================
        // 4. REVIEWS: ≥ 200 AUTHENTIC VIETNAMESE REVIEWS
        // =========================================================================
        private static async Task EnsureDemoProductReviewsAsync(ApplicationDbContext context, ILogger? logger)
        {
            var existingReviewsCount = await context.ProductReviews.CountAsync();
            if (existingReviewsCount >= 200) return;

            var activeProducts = await context.Products.Where(p => p.IsActive).Take(50).ToListAsync();
            if (!activeProducts.Any()) return;

            var customers = await context.Users.Where(u => u.UserType == 1).ToListAsync();
            var recentOrders = await context.Orders.Where(o => o.OrderStatus == "COMPLETED").Take(100).ToListAsync();

            var reviewTemplates5Star = new[]
            {
                new { Title = "Chân ái cho da dầu mụn!", Content = "Dùng em này 2 tuần trộm vía mụn viêm gom còi nhanh hẳn, không bị rát da như loại trước mình dùng. Thấm siêu nhanh không bết rít tí nào.", Scent = "Tràm trà tự nhiên", Longevity = "Kiềm dầu 6-8H", Feel = "Mỏng nhẹ thoáng da" },
                new { Title = "Cấp ẩm siêu mượt, phục hồi đỉnh", Content = "Chất serum mỏng nhẹ tênh, thấm nhanh không bết dính tí nào. Make up đè lên siêu tệp da không lo bị mốc phấn. Đã mua lại chai thứ 3!", Scent = "Dịu nhẹ", Longevity = "Cấp ẩm cả ngày", Feel = "Mềm mướt rạng rỡ" },
                new { Title = "Đóng gói kỹ, hàng chính hãng chuẩn", Content = "Giao hàng siêu nhanh, đóng gói bọc xốp 3 lớp kỹ càng. Hàng chính hãng quét mã QR chuẩn đét. Dùng êm ru da nhạy cảm cực hợp.", Scent = "Không mùi", Longevity = "Mềm mịn 8H", Feel = "Mát dịu tức thì" },
                new { Title = "Chống nắng chân ái mùa hè", Content = "Kem chống nắng nâng tone nhẹ nhàng tự nhiên, không bị vón cục hay vệt trắng. Đi ngoài trời cả ngày không bị đổ dầu bóng nhờn.", Scent = "Thanh mát", Longevity = "Lâu trôi 8H", Feel = "Khô thoáng mịn lì" },
                new { Title = "Lỗ chân lông thông thoáng rõ rệt", Content = "Sau 1 tháng dùng đều đặn kết hợp toner cùng dòng, mụn đầu đen vùng mũi giảm đáng kể, da mịn màng sờ rất thích.", Scent = "Thảo mộc thanh khiết", Longevity = "Kiềm dầu tốt", Feel = "Thấm nhanh" },
                new { Title = "Da căng bóng như gái Hàn", Content = "Dưỡng ẩm siêu đỉnh, tối thoa sáng dậy rửa mặt da mọng nước thích mê. Xứng đáng 10/10 trong tầm giá.", Scent = "Dịu ngọt nhẹ", Longevity = "Khóa ẩm 24H", Feel = "Căng bóng tự nhiên" }
            };

            var reviewTemplates4Star = new[]
            {
                new { Title = "Sản phẩm tốt, kiềm dầu ổn", Content = "Chất kem ổn áp, kiềm dầu được tầm 5-6 tiếng. Giá hơi chát tí nhưng tiền nào của nấy, xài rất an tâm.", Scent = "Dịu nhẹ", Longevity = "Kiềm dầu 5H", Feel = "Mỏng nhẹ" },
                new { Title = "Dưỡng ẩm tốt, vòi pump hơi cứng", Content = "Công dụng phục hồi làm dịu da rất tốt, mỗi tội cái vòi nhấn hơi cứng xíu nhưng bù lại chất lượng 9/10.", Scent = "Không mùi", Longevity = "Cấp ẩm 8H", Feel = "Thấm vừa phải" },
                new { Title = "Làm dịu da cháy nắng nhanh", Content = "Đi biển về da rát bôi em này vào mát rượi dịu ngay. Chai hơi nhỏ xài nhanh hết, mong hãng ra size to hơn.", Scent = "Thảo mộc", Longevity = "Mát dịu", Feel = "Mát lạnh sảng khoái" }
            };

            var reviewTemplates3Star = new[]
            {
                new { Title = "Dùng tạm ổn, cấp ẩm trung bình", Content = "Da mình hỗn hợp thiên khô thấy chưa đủ ẩm lắm vào mùa lạnh, phải layer thêm kem dưỡng nữa mới đủ đô.", Scent = "Hơi nồng nhẹ", Longevity = "Cấp ẩm 4H", Feel = "Hơi dính nhẹ lúc đầu" },
                new { Title = "Không kích ứng nhưng tác dụng chậm", Content = "Dùng an toàn lành tính, không bị nổi mụn nhưng chưa thấy mờ thâm rõ rệt. Chắc phải kiên trì thêm thời gian nữa.", Scent = "Không mùi", Longevity = "Trung bình", Feel = "Êm dịu" }
            };

            var skinTypes = new[] { "Da dầu mụn", "Da hỗn hợp thiên dầu", "Da khô thiếu nước", "Da nhạy cảm", "Da thường" };
            var ageRanges = new[] { "18-25", "26-35", "36-45" };

            var cskhReplies = new[]
            {
                "Omni Beauty cảm ơn bạn đã tin yêu và đánh giá sản phẩm! Chúc bạn luôn có làn da rạng rỡ và khỏe đẹp. Nếu cần tư vấn thêm quy trình skincare chuyên sâu, đừng ngại nhắn tin cho chuyên viên Omni Beauty nhé ạ! ❤️",
                "Cảm ơn phản hồi chi tiết của quý khách. Đội ngũ Omni Beauty rất vui vì sản phẩm đã mang lại hiệu quả tốt cho làn da của bạn. Chúc bạn luôn tự tin tỏa sáng!",
                "Omni Beauty xin ghi nhận góp ý quý báu của quý khách về bao bì/dung tích sản phẩm để chuyển tiếp tới hãng nhằm cải tiến trải nghiệm mua sắm ngày một hoàn hảo hơn."
            };

            int neededReviews = 210 - existingReviewsCount;
            var rand = new Random(101);
            var now = DateTime.UtcNow;

            var newReviews = new List<ProductReview>();

            for (int i = 0; i < neededReviews; i++)
            {
                var prod = activeProducts[i % activeProducts.Count];
                var user = customers.Count > 0 ? customers[rand.Next(0, customers.Count)] : null;
                var linkedOrder = recentOrders.Count > 0 ? recentOrders[rand.Next(0, recentOrders.Count)] : null;

                // Tỷ lệ đánh giá: 75% 5 sao, 18% 4 sao, 7% 3 sao
                int starRoll = rand.Next(1, 101);
                int rating;
                string title;
                string content;
                string scent;
                string longevity;
                string feel;

                if (starRoll <= 75)
                {
                    rating = 5;
                    var t = reviewTemplates5Star[rand.Next(0, reviewTemplates5Star.Length)];
                    title = t.Title;
                    content = t.Content;
                    scent = t.Scent;
                    longevity = t.Longevity;
                    feel = t.Feel;
                }
                else if (starRoll <= 93)
                {
                    rating = 4;
                    var t = reviewTemplates4Star[rand.Next(0, reviewTemplates4Star.Length)];
                    title = t.Title;
                    content = t.Content;
                    scent = t.Scent;
                    longevity = t.Longevity;
                    feel = t.Feel;
                }
                else
                {
                    rating = 3;
                    var t = reviewTemplates3Star[rand.Next(0, reviewTemplates3Star.Length)];
                    title = t.Title;
                    content = t.Content;
                    scent = t.Scent;
                    longevity = t.Longevity;
                    feel = t.Feel;
                }

                var reviewDate = now.AddDays(-rand.Next(1, 85)).AddHours(rand.Next(1, 23));
                bool hasReply = (i % 3) == 0; // ~33% review có phản hồi CSKH

                var review = new ProductReview
                {
                    ProductId = prod.Id,
                    UserId = user?.Id,
                    OrderId = linkedOrder?.Id,
                    Rating = rating,
                    Title = title,
                    Content = content,
                    SkinType = skinTypes[rand.Next(0, skinTypes.Length)],
                    AgeRange = ageRanges[rand.Next(0, ageRanges.Length)],
                    Scent = scent,
                    Longevity = longevity,
                    SkinFeel = feel,
                    HelpfulCount = rand.Next(2, 38),
                    IsVerifiedPurchase = true,
                    IsApproved = true,
                    IsHidden = false,
                    CreatedAt = reviewDate,
                    AdminReply = hasReply ? cskhReplies[rand.Next(0, cskhReplies.Length)] : null,
                    AdminRepliedAt = hasReply ? reviewDate.AddHours(rand.Next(2, 6)) : null
                };

                newReviews.Add(review);
            }

            await context.ProductReviews.AddRangeAsync(newReviews);
            await context.SaveChangesAsync();
            LogInfo(logger, $"[DbInitializer] Đã khởi tạo {newReviews.Count} đánh giá sản phẩm tự nhiên (≥ 200 review, đầy đủ rating, phản hồi CSKH).");
        }
    }
}
