using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.BeautyTech;
using Omnichannel.Application.Interfaces.Beauty;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services.Beauty
{
    public class SkincareConsultantService : ISkincareConsultantService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SkincareConsultantService> _logger;

        public SkincareConsultantService(ApplicationDbContext context, ILogger<SkincareConsultantService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<RoutineRecommendationResultDto> GenerateRoutineAsync(SkinQuizRequestDto request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            // 1. Chuẩn hóa mã SkinType và SkinConcern
            var (normalizedSkinType, skinTypeName) = ResolveSkinType(request.SkinType);
            var (normalizedConcern, concernName) = ResolveSkinConcern(request.SkinConcern);
            var budgetTier = (request.BudgetTier ?? "standard").ToLowerInvariant();

            _logger.LogInformation("Phân tích routine da: SkinType={SkinType}, Concern={Concern}, Budget={Budget}", 
                normalizedSkinType, normalizedConcern, budgetTier);

            // 2. Nạp danh mục sản phẩm đang active
            var products = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductSkinTypes)
                    .ThenInclude(pst => pst.SkinType)
                .Include(p => p.ProductConcerns)
                    .ThenInclude(pc => pc.SkinConcern)
                .Include(p => p.ProductIngredients)
                    .ThenInclude(pi => pi.ActiveIngredient)
                .Include(p => p.Batches)
                .Where(p => p.IsActive)
                .ToListAsync(ct);

            // 3. Định nghĩa 4 bước chuẩn da liễu
            var stepDefinitions = new (int Order, string Code, string Name, string Subtitle, string Guide)[]
            {
                (1, "1_Cleanser", "Bước 1: Làm sạch sâu (Cleanser)", 
                    "Làm sạch sâu bã nhờn, thông thoáng lỗ chân lông chuẩn pH 5.5", 
                    "Sử dụng 2 lần/ngày (sáng và tối). Lấy lượng bọt vừa đủ mát-xa nhẹ nhàng theo chuyển động tròn trong 60 giây."),
                (2, "2_Toner", "Bước 2: Cân bằng & Mở đường dẫn (Toner)", 
                    "Cân bằng độ ẩm pH, làm dịu ửng đỏ và mở đường dẫn dưỡng chất", 
                    "Thấm dung dịch ra bông tẩy trang hoặc vỗ trực tiếp lên da sau khi rửa mặt để cấp ẩm tức thì."),
                (3, "3_Serum", "Bước 3: Tinh chất điều trị chuyên sâu (Serum)", 
                    "Tinh chất cô đặc nồng độ cao tác động tầng sâu vào nguyên nhân gây mụn/nám/lão hóa", 
                    "Thoa 3-4 giọt tinh chất lên trán và hai má, vỗ nhẹ nhàng cho dưỡng chất thẩm thấu sâu."),
                (4, "4_Moisturizer", "Bước 4: Khóa ẩm & Phục hồi lipid (Moisturizer)", 
                    "Khóa ẩm đa tầng, phục hồi và củng cố hàng rào bảo vệ da tự nhiên", 
                    "Bước khóa ẩm cuối cùng vào buổi tối, ngăn ngừa dưỡng chất bay hơi và nuôi dưỡng da suốt đêm.")
            };

            var selectedProductIds = new HashSet<int>();
            var routineSteps = new List<RoutineStepDto>();

            foreach (var stepDef in stepDefinitions)
            {
                var candidate = SelectBestProductForStep(
                    products, 
                    stepDef.Code, 
                    normalizedSkinType, 
                    normalizedConcern, 
                    budgetTier, 
                    selectedProductIds);

                if (candidate != null)
                {
                    selectedProductIds.Add(candidate.Id);

                    var keyIngs = candidate.ProductIngredients
                        .Select(pi => pi.ActiveIngredient?.Name ?? string.Empty)
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .Take(3)
                        .ToList();

                    if (!keyIngs.Any())
                    {
                        keyIngs = GetDefaultIngredientsForStep(stepDef.Code, normalizedConcern);
                    }

                    var safetyBadges = new List<string> { "Chuẩn Dược Mỹ Phẩm" };
                    if (candidate.IsActive) safetyBadges.Add("Chính Hãng 100%");
                    if (candidate.PAOMonths.HasValue) safetyBadges.Add($"HSD sau mở nắp: {candidate.PAOMonths.Value}M");

                    routineSteps.Add(new RoutineStepDto
                    {
                        StepOrder = stepDef.Order,
                        StepCode = stepDef.Code,
                        StepName = stepDef.Name,
                        Subtitle = stepDef.Subtitle,
                        UsageGuide = stepDef.Guide,
                        SelectedProduct = new RoutineProductDto
                        {
                            ProductId = candidate.Id,
                            ProductName = candidate.Name,
                            Sku = candidate.Sku,
                            Slug = candidate.Slug,
                            ThumbnailUrl = candidate.ThumbnailUrl,
                            Price = candidate.SellingPrice,
                            DiscountPrice = candidate.DiscountPrice,
                            BrandName = candidate.Brand?.Name ?? candidate.ManufacturerName ?? "Omni Beauty",
                            KeyIngredients = keyIngs,
                            SafetyBadges = safetyBadges,
                            StepCode = stepDef.Code
                        }
                    });
                }
            }

            // 4. Tính toán chi phí combo ưu đãi 15%
            decimal originalTotal = routineSteps.Sum(s => s.SelectedProduct?.EffectivePrice ?? 0);
            const decimal comboPercent = 15m;
            decimal discountAmount = Math.Round(originalTotal * (comboPercent / 100m), 0);
            decimal finalPrice = Math.Max(0, originalTotal - discountAmount);

            string advice = GenerateDermatologistAdvice(normalizedSkinType, normalizedConcern);

            return new RoutineRecommendationResultDto
            {
                SkinType = normalizedSkinType,
                SkinTypeName = skinTypeName,
                SkinConcern = normalizedConcern,
                SkinConcernName = concernName,
                RoutineTitle = $"Phác Đồ Skincare Chuyên Sâu 4 Bước Dành Cho {skinTypeName}",
                DermatologistAdvice = advice,
                Steps = routineSteps,
                OriginalTotalPrice = originalTotal,
                ComboDiscountPercent = comboPercent,
                ComboDiscountAmount = discountAmount,
                FinalComboPrice = finalPrice,
                ComboCouponCode = "COMBO15"
            };
        }

        private static Product? SelectBestProductForStep(
            List<Product> allProducts,
            string stepCode,
            string skinTypeSlug,
            string concernSlug,
            string budgetTier,
            HashSet<int> alreadySelectedIds)
        {
            var candidates = allProducts
                .Where(p => !alreadySelectedIds.Contains(p.Id))
                .Where(p => MatchesStep(p, stepCode))
                .ToList();

            if (!candidates.Any())
            {
                // Fallback nếu không có sản phẩm phân loại chặt chẽ
                candidates = allProducts
                    .Where(p => !alreadySelectedIds.Contains(p.Id))
                    .ToList();
            }

            if (!candidates.Any()) return null;

            // Chấm điểm độ tương thích
            var scored = candidates.Select(p =>
            {
                int score = 0;

                // 1. Phù hợp loại da (+10 điểm)
                if (p.ProductSkinTypes.Any(pst => pst.SkinType != null && pst.SkinType.Slug == skinTypeSlug))
                {
                    score += 10;
                }
                else if (p.ProductSkinTypes.Any(pst => pst.SkinType != null && pst.SkinType.Slug == "da-thuong"))
                {
                    score += 3;
                }

                // 2. Phù hợp mối quan tâm (+10 điểm)
                if (p.ProductConcerns.Any(pc => pc.SkinConcern != null && pc.SkinConcern.Slug == concernSlug))
                {
                    score += 10;
                }

                // 3. Còn tồn kho FEFO (+5 điểm)
                if (p.Batches.Any(b => b.CurrentQuantity > 0))
                {
                    score += 5;
                }

                // 4. Ngân sách mong muốn (+5 điểm)
                decimal price = p.DiscountPrice.HasValue && p.DiscountPrice.Value > 0 ? p.DiscountPrice.Value : p.SellingPrice;
                if (budgetTier == "economy" && price < 400000) score += 5;
                else if (budgetTier == "premium" && price > 800000) score += 5;
                else if (budgetTier == "standard" && price >= 300000 && price <= 900000) score += 5;

                return new { Product = p, Score = score };
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Product.Id)
            .ToList();

            return scored.FirstOrDefault()?.Product;
        }

        private static bool MatchesStep(Product p, string stepCode)
        {
            if (!string.IsNullOrWhiteSpace(p.RoutineStep) && p.RoutineStep.Equals(stepCode, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var catSlug = p.Category?.Slug?.ToLowerInvariant() ?? string.Empty;
            var name = p.Name.ToLowerInvariant();

            return stepCode switch
            {
                "1_Cleanser" => catSlug.Contains("sua-rua-mat") || catSlug.Contains("nuoc-tay-trang") || name.Contains("rửa mặt") || name.Contains("cleanser") || name.Contains("tẩy trang"),
                "2_Toner" => catSlug.Contains("toner") || name.Contains("toner") || name.Contains("hoa hồng") || name.Contains("cân bằng"),
                "3_Serum" => catSlug.Contains("serum") || name.Contains("serum") || name.Contains("tinh chất") || name.Contains("ampoule") || name.Contains("b5"),
                "4_Moisturizer" => catSlug.Contains("kem-duong-am") || name.Contains("kem dưỡng") || name.Contains("moisturizer") || name.Contains("khóa ẩm") || name.Contains("cream"),
                _ => false
            };
        }

        private static (string Slug, string Name) ResolveSkinType(string raw)
        {
            var r = (raw ?? string.Empty).Trim().ToLowerInvariant();
            return r switch
            {
                "da-dau" or "oily" => ("da-dau", "Da Dầu (Mụn & Bã Nhờn)"),
                "da-kho" or "dry" => ("da-kho", "Da Khô (Thiếu Ẩm & Bong Tróc)"),
                "da-nhay-cam" or "sensitive" => ("da-nhay-cam", "Da Nhạy Cảm (Dễ Kích Ứng)"),
                "da-hon-hop" or "combination" => ("da-hon-hop", "Da Hỗn Hợp (Dầu Vùng Chữ T)"),
                _ => ("da-thuong", "Da Thường / Hỗn Hợp Nhẹ")
            };
        }

        private static (string Slug, string Name) ResolveSkinConcern(string raw)
        {
            var r = (raw ?? string.Empty).Trim().ToLowerInvariant();
            return r switch
            {
                "acne" or "tri-mun" or "mun" => ("acne", "Trị Mụn & Giảm Dầu Thừa"),
                "anti_aging" or "lao-hoa" or "aging" => ("anti_aging", "Chống Lão Hóa & Nếp Nhăn"),
                "brightening" or "nam-sam" or "pigmentation" => ("brightening", "Làm Sáng Da & Mờ Thâm"),
                "pore_minimizing" or "lo-chan-long" => ("pore_minimizing", "Se Khít Lỗ Chân Lông"),
                _ => ("hydration", "Cấp Ẩm Chuyên Sâu & Phục Hồi Màng Ẩm")
            };
        }

        private static List<string> GetDefaultIngredientsForStep(string stepCode, string concernSlug)
        {
            return stepCode switch
            {
                "1_Cleanser" => new List<string> { "pH 5.5 Chuẩn Sinh Lý", "Amino Acid Dịu Nhẹ", "Không Gây Khô Căng" },
                "2_Toner" => new List<string> { "Centella Asiatica (Rau Má)", "Hyaluronic Acid", "Chiết Xuất Tràm Trà" },
                "3_Serum" => concernSlug == "acne" 
                    ? new List<string> { "Salicylic Acid (BHA 2%)", "Niacinamide 4%", "Tràm Trà Kháng Viêm" }
                    : new List<string> { "Panthenol (Vitamin B5)", "Hyaluronic Acid Đa Tầng", "Peptides Tái Tạo" },
                "4_Moisturizer" => new List<string> { "Ceramide Phức Hợp", "Squalane Khóa Ẩm", "Không Gây Bít Tắc Lỗ Chân Lông" },
                _ => new List<string> { "Dược Mỹ Phẩm Chuẩn Châu Âu" }
            };
        }

        private static string GenerateDermatologistAdvice(string skinType, string concern)
        {
            if (skinType == "da-dau" && concern == "acne")
            {
                return "Đối với làn da dầu mụn, nguyên tắc quan trọng nhất là làm sạch sâu dịu nhẹ không phá hủy màng ẩm tự nhiên. Phác đồ 4 bước được thiết kế tập trung vào BHA và Tràm trà giúp thông thoáng bã nhờn, kết hợp Serum phục hồi B5 ngăn ngừa thâm mụn và kem dưỡng dạng gel không bít tắc.";
            }

            if (skinType == "da-kho")
            {
                return "Làn da khô cần được ngậm nước đa tầng từ các phân tử Hyaluronic Acid và khóa chặt màng lipid bằng Ceramide/Squalane. Phác đồ này sẽ giúp tái tạo hàng rào bảo vệ, loại bỏ cảm giác căng rát và giúp da mềm mượt suốt 24 giờ.";
            }

            if (skinType == "da-nhay-cam")
            {
                return "Với da nhạy cảm dễ ửng đỏ, phác đồ ưu tiên các hoạt chất làm dịu tự nhiên như Rau Má (Madecassoside) và Vitamin B5 chuẩn y khoa. Công thức 100% không cồn khô, không hương liệu tổng hợp, an toàn tuyệt đối cho làn da dễ kích ứng.";
            }

            return "Phác đồ 4 bước được các chuyên gia da liễu cân chỉnh tối ưu giữa khả năng làm sạch sâu, điều trị tập trung và củng cố hàng rào bảo vệ da, giúp làn da duy trì vẻ rạng rỡ và khỏe khoắn dài lâu.";
        }
    }
}

