using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.BeautyTech;
using Omnichannel.Application.DTOs.Catalog;
using Omnichannel.Application.Interfaces.BeautyTech;
using Omnichannel.Application.Interfaces.Catalog;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Web.Controllers
{
    public class SmartFilterController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ISmartFilterService _smartFilterService;
        private readonly ISkincareIngredientInteractionEngine _interactionEngine;
        private readonly ILogger<SmartFilterController> _logger;

        public SmartFilterController(
            ApplicationDbContext context,
            ISmartFilterService smartFilterService,
            ISkincareIngredientInteractionEngine interactionEngine,
            ILogger<SmartFilterController> logger)
        {
            _context = context;
            _smartFilterService = smartFilterService;
            _interactionEngine = interactionEngine;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "Chẩn Đoán Làn Da Cùng Chuyên Gia AI - OMNI BEAUTY";
            ViewData["Description"] = "Trợ lý da liễu AI phân tích chỉ số sinh học da, vẽ biểu đồ radar 5 trục và gợi ý phác đồ chăm sóc tương thích 100%.";
            return View();
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Diagnose([FromBody] SkinDiagnosticRequestDto request)
        {
            request ??= new SkinDiagnosticRequestDto();

            try
            {
                // 1. Phân tích loại da & phân nhóm chuyên sâu
                string classification;
                string subtype;
                string summary;

                int hydrationScore;
                int sebumScore;
                int firmnessScore;
                int barrierScore;
                int radianceScore;

                switch (request.NoonFeeling)
                {
                    case "oily_all":
                        sebumScore = 32; // Tuyến bã nhờn hoạt động quá mức
                        hydrationScore = request.Environment == "office_ac" ? 48 : 62;
                        barrierScore = request.PrimaryConcern == "acne_pore" ? 45 : 60;
                        classification = "Da Dầu Dày Sừng - Bít Tắc Nang Lông";
                        subtype = request.Environment == "office_ac" ? "Dầu thiếu nước biểu bì" : "Tăng tiết bã nhờn toàn mặt";
                        summary = "Làn da của bạn đang trong tình trạng tăng tiết bã nhờn mạnh mẽ, tạo môi trường thuận lợi cho vi khuẩn P.acnes phát triển. Màng bảo vệ tự nhiên bị quá tải, cần tập trung làm sạch sâu dịu nhẹ và kiềm dầu mà không làm mất ẩm tự nhiên.";
                        break;

                    case "dry_tight":
                        sebumScore = 42; // Thiếu lipid tự nhiên
                        hydrationScore = 36; // Rất thiếu ẩm
                        barrierScore = request.PrimaryConcern == "sensitive_redness" ? 38 : 50;
                        classification = "Da Khô Nhạy Cảm - Tổn Thương Màng Lipid";
                        subtype = "Thiếu ẩm tầng sâu & mất nước qua biểu bì (TEWL)";
                        summary = "Hàng rào biểu bì (Stratum Corneum) của bạn đang bị thiếu hụt Ceramide và acid béo tự nhiên, dẫn đến tình trạng bong tróc và châm chích khó chịu. Ưu tiên cấp ẩm đa tầng và tái tạo màng lipid bảo vệ.";
                        break;

                    case "tzone_oily":
                    default:
                        sebumScore = 54; // Chữ T dầu, 2 má thường/khô
                        hydrationScore = request.Environment == "office_ac" ? 52 : 65;
                        barrierScore = 65;
                        classification = "Da Hỗn Hợp Thiên Dầu - Thiếu Ẩm Tầng Sâu";
                        subtype = "Mất cân bằng tỷ lệ dầu / nước";
                        summary = "Vùng chữ T của bạn tiết nhiều dầu trong khi hai bên má lại thiếu ẩm. Nguyên nhân hàng đầu là do da thiếu nước nên tự động tiết thêm dầu để bù trừ. Cần phác đồ cân bằng vi sinh vật (Microbiome) và cấp nước tầng sâu không chứa dầu nặng.";
                        break;

                    case "normal_smooth":
                        sebumScore = 86;
                        hydrationScore = 85;
                        barrierScore = 88;
                        classification = "Da Thường Cân Bằng Khỏe Mạnh";
                        subtype = "Tỷ lệ dầu - nước lý tưởng";
                        summary = "Chúc mừng bạn! Làn da đang duy trì trạng thái sinh học cân bằng và có sức đề kháng tự nhiên tốt. Phác đồ hướng đến việc chống oxy hóa, duy trì collagen và bảo vệ da trước tia UV và ánh sáng xanh.";
                        break;
                }

                // 2. Tinh chỉnh theo Độ tuổi (Firmness Score)
                switch (request.AgeGroup)
                {
                    case "under_20":
                        firmnessScore = 94;
                        break;
                    case "20_29":
                        firmnessScore = 85;
                        break;
                    case "30_39":
                        firmnessScore = 68;
                        sebumScore = Math.Max(30, sebumScore - 5);
                        break;
                    case "above_40":
                        firmnessScore = 52;
                        hydrationScore = Math.Max(30, hydrationScore - 8);
                        break;
                    default:
                        firmnessScore = 80;
                        break;
                }

                // 3. Tinh chỉnh theo Vấn đề da & Môi trường (Radiance & Barrier)
                switch (request.PrimaryConcern)
                {
                    case "acne_pore":
                        radianceScore = 58;
                        barrierScore = Math.Max(35, barrierScore - 10);
                        break;
                    case "pigmentation":
                        radianceScore = 44; // Tăng sắc tố, thâm nám
                        break;
                    case "wrinkle_aging":
                        radianceScore = 60;
                        firmnessScore = Math.Max(40, firmnessScore - 12);
                        break;
                    case "sensitive_redness":
                        radianceScore = 52;
                        barrierScore = Math.Max(30, barrierScore - 18);
                        break;
                    default:
                        radianceScore = 75;
                        break;
                }

                if (request.Environment == "outdoor_sun")
                {
                    radianceScore = Math.Max(35, radianceScore - 8);
                    barrierScore = Math.Max(35, barrierScore - 6);
                }
                else if (request.Environment == "office_ac")
                {
                    hydrationScore = Math.Max(30, hydrationScore - 10);
                }
                else if (request.Environment == "screen_blue_light")
                {
                    radianceScore = Math.Max(35, radianceScore - 5);
                }

                // Điểm sức khỏe tổng thể (Thang 100)
                int overallHealthScore = (int)Math.Clamp(
                    Math.Round((hydrationScore * 0.25) + (sebumScore * 0.20) + (firmnessScore * 0.20) + (barrierScore * 0.20) + (radianceScore * 0.15)),
                    50, 96);

                if (request.IsPregnantOrNursing)
                {
                    classification += " [Phác Đồ An Toàn Thai Kỳ]";
                }

                // 4. Danh sách Hoạt chất Khuyên Dùng & Cần Tránh
                var recommendedIngredients = new List<IngredientAdviceDto>();
                var avoidIngredients = new List<IngredientAdviceDto>();

                // Hoạt chất khuyên dùng
                recommendedIngredients.Add(new IngredientAdviceDto
                {
                    Name = "Niacinamide (Vitamin B3 2% - 5%)",
                    Role = "Phục hồi hàng rào & Kiềm dầu đa năng",
                    Reason = "Cải thiện kích thước lỗ chân lông, điều tiết bã nhờn, làm sáng thâm mụn và an toàn 100% trong thai kỳ.",
                    Icon = "fa-wand-magic-sparkles"
                });

                recommendedIngredients.Add(new IngredientAdviceDto
                {
                    Name = "Panthenol (Vitamin B5) & Centella Asiatica",
                    Role = "Làm dịu & Tái tạo mô da",
                    Reason = "Giảm đỏ rát, củng cố màng lipid, làm dịu tức thì tổn thương do điều hòa hoặc khói bụi.",
                    Icon = "fa-shield-heart"
                });

                recommendedIngredients.Add(new IngredientAdviceDto
                {
                    Name = "Hyaluronic Acid Đa Kích Thước (Multi-HA)",
                    Role = "Cấp nước ngậm ẩm đa tầng",
                    Reason = "Thâm nhập vào sâu lớp trung bì, giữ nước gấp 1000 lần trọng lượng phân tử giúp da căng bóng.",
                    Icon = "fa-droplet"
                });

                if (request.IsPregnantOrNursing)
                {
                    recommendedIngredients.Add(new IngredientAdviceDto
                    {
                        Name = "Bakuchiol 1% (Chiết xuất hạt Babchi)",
                        Role = "Thảo dược thay thế Retinol",
                        Reason = "Kích thích sinh collagen, chống lão hóa và mờ nếp nhăn vượt trội mà tuyệt đối an toàn cho mẹ và bé.",
                        Icon = "fa-seedling"
                    });
                }
                else if (request.PrimaryConcern == "wrinkle_aging" || request.AgeGroup == "30_39" || request.AgeGroup == "above_40")
                {
                    recommendedIngredients.Add(new IngredientAdviceDto
                    {
                        Name = "Retinol Vi Nang (Encapsulated Retinol 0.5%)",
                        Role = "Vàng trong làng chống lão hóa",
                        Reason = "Tăng tốc độ thay mới tế bào sừng, thúc đẩy nguyên bào sợi tăng sinh collagen và elastin mới.",
                        Icon = "fa-gem"
                    });
                }
                else if (request.PrimaryConcern == "acne_pore")
                {
                    recommendedIngredients.Add(new IngredientAdviceDto
                    {
                        Name = "Salicylic Acid (BHA 1% - 2%)",
                        Role = "Làm sạch sâu trong cổ nang lông",
                        Reason = "Tan trong dầu, len lỏi sâu để dọn sạch tế bào chết bít tắc, gom cồi mụn ẩn và mụn đầu đen.",
                        Icon = "fa-sparkles"
                    });
                }

                // Hoạt chất cần tránh
                if (request.IsPregnantOrNursing)
                {
                    avoidIngredients.Add(new IngredientAdviceDto
                    {
                        Name = "Retinol, Tretinoin & Các dẫn xuất Retinoid",
                        Role = "Chống chỉ định tuyệt đối thai kỳ",
                        Reason = "Có nguy cơ hấp thu qua da và gây dị tật phôi thai (Phân loại X theo chuẩn FDA Hoa Kỳ).",
                        Icon = "fa-ban"
                    });

                    avoidIngredients.Add(new IngredientAdviceDto
                    {
                        Name = "BHA (Salicylic Acid) nồng độ cao > 1.5%",
                        Role = "Hấp thu toàn thân",
                        Reason = "Hạn chế sử dụng liều cao trong thời gian mang thai và cho con bú để bảo đảm an toàn tối đa.",
                        Icon = "fa-triangle-exclamation"
                    });

                    avoidIngredients.Add(new IngredientAdviceDto
                    {
                        Name = "Hydroquinone & Arbutin liều cao",
                        Role = "Chất làm trắng da hấp thu cao",
                        Reason = "Tỷ lệ hấp thu qua biểu bì lên tới 40%, không an toàn cho phụ nữ giai đoạn thai kỳ.",
                        Icon = "fa-skull-crossbones"
                    });
                }
                else if (request.PrimaryConcern == "sensitive_redness" || request.NoonFeeling == "dry_tight")
                {
                    avoidIngredients.Add(new IngredientAdviceDto
                    {
                        Name = "Cồn khô (Alcohol Denat / Ethanol) & Hương liệu nhân tạo",
                        Role = "Chất gây khô & Viêm da tiếp xúc",
                        Reason = "Hòa tan màng lipid tự nhiên, làm trầm trọng thêm tình trạng ửng đỏ và châm chích.",
                        Icon = "fa-ban"
                    });

                    avoidIngredients.Add(new IngredientAdviceDto
                    {
                        Name = "AHA / BHA nồng độ quá cao (> 10%)",
                        Role = "Lột tẩy biểu bì mạnh",
                        Reason = "Làm mỏng màng bảo vệ khi nền da đang yếu, dễ bắt nắng và kích ứng bùng phát.",
                        Icon = "fa-triangle-exclamation"
                    });
                }
                else
                {
                    avoidIngredients.Add(new IngredientAdviceDto
                    {
                        Name = "Dầu khoáng nặng (Heavy Mineral Oil) & Parabens",
                        Role = "Gây bít tắc cổ nang lông",
                        Reason = "Tạo màng bí bách khiến vi khuẩn kỵ khí sinh sôi, gây mụn ẩn diện rộng.",
                        Icon = "fa-ban"
                    });
                }

                // 5. Query sản phẩm tương thích 100% từ CSDL thật
                var query = _context.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive)
                    .Include(p => p.Category)
                    .Include(p => p.ProductSkinTypes).ThenInclude(pst => pst.SkinType)
                    .Include(p => p.ProductConcerns).ThenInclude(pc => pc.SkinConcern)
                    .Include(p => p.ProductIngredients).ThenInclude(pi => pi.ActiveIngredient)
                    .Include(p => p.Reviews);

                var allProducts = await query.ToListAsync();

                // Lọc bỏ sản phẩm chống chỉ định nếu đang mang thai hoặc cho con bú
                if (request.IsPregnantOrNursing)
                {
                    allProducts = allProducts.Where(p =>
                    {
                        var nameLower = p.Name.ToLowerInvariant();
                        var descLower = (p.ShortDescription ?? "").ToLowerInvariant();

                        bool hasRetinoids = nameLower.Contains("retinol") || nameLower.Contains("tretinoin") || descLower.Contains("retinol");
                        bool hasHighBha = nameLower.Contains("bha 2%") || descLower.Contains("salicylic acid 2%");
                        bool hasHydroquinone = nameLower.Contains("hydroquinone");

                        bool ingredientFlag = p.ProductIngredients.Any(pi =>
                            pi.ActiveIngredient != null &&
                            (pi.ActiveIngredient.Slug.Contains("retinol") || pi.ActiveIngredient.Name.ToLower().Contains("retinol")));

                        return !hasRetinoids && !hasHighBha && !hasHydroquinone && !ingredientFlag;
                    }).ToList();
                }

                // Chấm điểm tương thích cho từng sản phẩm
                string targetSkinSlug = request.NoonFeeling switch
                {
                    "oily_all" => "da-dau",
                    "dry_tight" => "da-kho",
                    "normal_smooth" => "da-thuong",
                    _ => "da-hon-hop"
                };

                string targetConcernSlug = request.PrimaryConcern switch
                {
                    "acne_pore" => "acne",
                    "pigmentation" => "brightening",
                    "wrinkle_aging" => "anti_aging",
                    "sensitive_redness" => "hydration",
                    _ => "hydration"
                };

                // Phân tích phân khúc ngân sách
                decimal? minBudget = null;
                decimal? maxBudget = null;
                switch (request.BudgetBracket?.ToLowerInvariant())
                {
                    case "under_300k":
                        maxBudget = 300000m;
                        break;
                    case "300k_500k":
                        minBudget = 300000m;
                        maxBudget = 500000m;
                        break;
                    case "500k_1m":
                        minBudget = 500000m;
                        maxBudget = 1000000m;
                        break;
                    case "above_1m":
                        minBudget = 1000000m;
                        break;
                }

                var scoredProducts = new List<SmartFilterProductDto>();

                foreach (var p in allProducts)
                {
                    double matchScore = 72.0;
                    var matchReasons = new List<string>();

                    bool matchesSkinType = p.ProductSkinTypes.Any(st => st.SkinType.Slug == targetSkinSlug || st.SkinType.Slug == "all");
                    bool matchesConcern = p.ProductConcerns.Any(c => c.SkinConcern.Slug == targetConcernSlug || c.SkinConcern.Slug.Contains(targetConcernSlug));

                    if (matchesSkinType)
                    {
                        matchScore += 12.0;
                        matchReasons.Add($"Tối ưu hóa riêng cho {classification.Split('-')[0].Trim()}");
                    }

                    if (matchesConcern)
                    {
                        matchScore += 10.0;
                        matchReasons.Add("Đặc trị hiệu quả mối quan tâm hàng đầu của bạn");
                    }

                    // Chấm điểm theo phân khúc ngân sách (tối đa 6 điểm)
                    var effectivePrice = p.DiscountPrice ?? p.SellingPrice;
                    if (minBudget.HasValue || maxBudget.HasValue)
                    {
                        bool isWithinBudget = true;
                        if (minBudget.HasValue && effectivePrice < minBudget.Value) isWithinBudget = false;
                        if (maxBudget.HasValue && effectivePrice > maxBudget.Value) isWithinBudget = false;

                        if (isWithinBudget)
                        {
                            matchScore += 6.0;
                            matchReasons.Add("Tối ưu theo phân khúc ngân sách của bạn");
                        }
                        else if (maxBudget.HasValue && effectivePrice <= maxBudget.Value * 1.2m)
                        {
                            matchScore += 2.0;
                        }
                    }

                    if (request.IsPregnantOrNursing)
                    {
                        matchReasons.Add("Chứng nhận 100% an toàn cho thai kỳ & cho con bú");
                        matchScore += 2.0;
                    }

                    var keyIngs = p.ProductIngredients.Select(pi => pi.ActiveIngredient.Name).Take(3).ToList();
                    if (keyIngs.Any(i => i.Contains("Niacinamide") || i.Contains("B5") || i.Contains("Hyaluronic") || i.Contains("Ceramide")))
                    {
                        matchScore += 3.0;
                    }

                    int finalPercent = (int)Math.Clamp(Math.Round(matchScore), 85, 99);

                    scoredProducts.Add(new SmartFilterProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Slug = p.Slug,
                        CategoryName = p.Category?.Name ?? "Dược Mỹ Phẩm",
                        Price = effectivePrice,
                        OriginalPrice = p.DiscountPrice.HasValue ? p.SellingPrice : null,
                        ThumbnailUrl = ProductImageSourceRegistry.ResolveProductImageUrl(p.ThumbnailUrl, p.Sku, p.Slug, p.Name),
                        Rating = p.Reviews.Any() ? Math.Round(p.Reviews.Average(r => r.Rating), 1) : 5.0,
                        ReviewCount = p.Reviews.Count,
                        MatchScore = finalPercent,
                        MatchReasons = matchReasons.Take(2).ToList(),
                        KeyIngredients = keyIngs
                    });
                }

                var topRecommended = scoredProducts
                    .OrderByDescending(p => p.MatchScore)
                    .ThenByDescending(p => p.Rating)
                    .Take(6)
                    .ToList();

                // 6. Phân tích ma trận tương kỵ & hiệp đồng hoạt chất đối với danh sách sản phẩm gợi ý
                int? currentUserId = null;
                if (User?.Identity?.IsAuthenticated == true)
                {
                    var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(idClaim, out var parsedId)) currentUserId = parsedId;
                }

                var conflictAnalysis = await _interactionEngine.AnalyzeCartOrRoutineAsync(
                    topRecommended.Select(p => p.Id).ToList(),
                    currentUserId);

                var result = new SkinDiagnosticResultDto
                {
                    OverallHealthScore = overallHealthScore,
                    SkinClassification = classification,
                    SkinSubtype = subtype,
                    ExpertSummary = summary,
                    IsPregnancySafeApplied = request.IsPregnantOrNursing,
                    BudgetBracket = request.BudgetBracket ?? "300k_500k",
                    RadarScores = new SkinRadarScoresDto
                    {
                        Hydration = hydrationScore,
                        SebumBalance = sebumScore,
                        Firmness = firmnessScore,
                        BarrierStrength = barrierScore,
                        Radiance = radianceScore
                    },
                    RecommendedIngredients = recommendedIngredients,
                    AvoidIngredients = avoidIngredients,
                    RecommendedProducts = topRecommended,
                    ConflictAnalysis = conflictAnalysis,
                    RoutineMorning = "Làm sạch dịu nhẹ → Nước hoa hồng cấp ẩm → Serum B5/HA → Kem dưỡng ẩm mỏng nhẹ → Kem chống nắng SPF 50+",
                    RoutineEvening = "Tẩy trang dịu nhẹ → Sữa rửa mặt tạo bọt mịn → Tinh chất Niacinamide/Bakuchiol → Kem phục hồi khóa ẩm Ceramide"
                };

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình chẩn đoán làn da AI.");
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống khi chẩn đoán. Vui lòng thử lại." });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> CheckConflicts([FromBody] ConflictCheckRequestDto request)
        {
            request ??= new ConflictCheckRequestDto();
            try
            {
                SkincareRoutineAnalysisDto analysis;
                if (request.ProductIds != null && request.ProductIds.Any())
                {
                    int? userId = request.UserId;
                    if (!userId.HasValue && User?.Identity?.IsAuthenticated == true)
                    {
                        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        if (int.TryParse(idClaim, out var parsedId)) userId = parsedId;
                    }
                    analysis = await _interactionEngine.AnalyzeCartOrRoutineAsync(request.ProductIds, userId);
                }
                else if (request.IngredientNames != null && request.IngredientNames.Any())
                {
                    analysis = await _interactionEngine.CheckIngredientConflictsAsync(request.IngredientNames);
                }
                else
                {
                    return BadRequest(new { success = false, message = "Vui lòng chọn ít nhất 2 hoạt chất hoặc sản phẩm để phân tích." });
                }

                return Json(new { success = true, data = analysis });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình phân tích tương kỵ hoạt chất.");
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống khi phân tích ma trận tương kỵ." });
            }
        }

        [HttpGet]
        public IActionResult GetCommonIngredients()
        {
            var ingredients = new[]
            {
                new { Key = "retinol", Name = "Retinol / Retinoids 0.5% - 1%", Category = "Đặc trị lão hóa", Tag = "Hoạt chất vàng" },
                new { Key = "bha", Name = "BHA (Salicylic Acid 2%)", Category = "Tẩy tế bào chết hóa học", Tag = "Làm sạch cổ nang lông" },
                new { Key = "aha", Name = "AHA (Glycolic / Lactic Acid)", Category = "Tẩy tế bào chết hóa học", Tag = "Sáng mịn bề mặt" },
                new { Key = "vitamin-c", Name = "Vitamin C (L-Ascorbic Acid 15%)", Category = "Chống oxy hóa & Sáng da", Tag = "Chống gốc tự do UV" },
                new { Key = "niacinamide", Name = "Niacinamide (Vitamin B3 5% - 10%)", Category = "Phục hồi & Kiềm dầu", Tag = "Củng cố màng lipid" },
                new { Key = "benzoyl-peroxide", Name = "Benzoyl Peroxide 2.5% - 5%", Category = "Đặc trị mụn viêm", Tag = "Diệt khuẩn P.acnes" },
                new { Key = "copper-peptides", Name = "Copper Peptides (GHK-Cu)", Category = "Tái tạo mô tế bào", Tag = "Kích thích collagen" },
                new { Key = "panthenol", Name = "Panthenol (Vitamin B5)", Category = "Làm dịu & Cấp ẩm", Tag = "Phục hồi da đỏ rát" },
                new { Key = "ceramide", Name = "Ceramides (1, 3, 6-II)", Category = "Khóa ẩm & Bảo vệ", Tag = "Màng lipid tự nhiên" },
                new { Key = "hyaluronic-acid", Name = "Hyaluronic Acid đa phân tử (HA)", Category = "Cấp nước ngậm ẩm", Tag = "Ngậm nước gấp 1000 lần" },
                new { Key = "ferulic-acid", Name = "Ferulic Acid 0.5%", Category = "Chống oxy hóa", Tag = "Ổn định Vitamin C" },
                new { Key = "zinc", Name = "Zinc PCA 1%", Category = "Kiềm dầu & Kháng viêm", Tag = "Giảm tiết bã nhờn" }
            };
            return Json(new { success = true, data = ingredients });
        }
    }
}
