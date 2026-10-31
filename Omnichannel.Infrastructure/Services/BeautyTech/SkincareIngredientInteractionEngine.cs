using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.BeautyTech;
using Omnichannel.Application.Interfaces.BeautyTech;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services.BeautyTech
{
    public class SkincareIngredientInteractionEngine : ISkincareIngredientInteractionEngine
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SkincareIngredientInteractionEngine> _logger;

        private static readonly (string KeyA, string KeyB, string Type, string Severity, string Explanation, string Resolution)[] Rules =
        [
            ("retinol", "bha", "HIGH_CONFLICT", "WARNING",
                "Dùng chung Retinol và BHA (Salicylic Acid) trong cùng một buổi tối dễ gây quá tải bong sừng, làm mỏng màng ẩm tự nhiên và gây kích ứng đỏ rát.",
                "Tách lịch Cách Ngày (Skin Cycling): Đêm chẵn dùng BHA, Đêm lẻ dùng Retinol; hoặc chuyển BHA sang buổi sáng kèm kem chống nắng SPF50+."),

            ("retinol", "aha", "HIGH_CONFLICT", "WARNING",
                "AHA (Glycolic/Lactic Acid) có độ pH thấp (3.2 - 3.8) làm giảm pH bề mặt da, khiến Retinol phóng thích quá nhanh gây châm chích mạnh.",
                "Không thoa chồng trong cùng một buổi. Sử dụng AHA tối Thứ 2/Thứ 5 và Retinol tối Thứ 3/Thứ 6."),

            ("retinol", "benzoyl-peroxide", "CHEMICAL_DEACTIVATION", "DANGER",
                "Benzoyl Peroxide (BPO) là chất oxy hóa mạnh sẽ làm oxy hóa và vô hiệu hóa hoàn toàn hoạt tính chống lão hóa của phân tử Retinol.",
                "Bắt buộc tách buổi: Chấm mụn Benzoyl Peroxide vào Buổi Sáng (AM) và chỉ thoa Retinol vào Buổi Tối (PM)."),

            ("vitamin-c", "copper-peptides", "REDOX_CONFLICT", "DANGER",
                "Ion Đồng (Cu2+) trong Copper Peptides xúc tác phản ứng oxy hóa Vitamin C (L-Ascorbic Acid) làm mất tác dụng làm sáng da của cả hai sản phẩm.",
                "Tách buổi hoàn toàn: Thoa Serum Vitamin C vào Buổi Sáng (AM) và thoa Copper Peptides vào Buổi Tối (PM)."),

            ("vitamin-c", "retinol", "HIGH_CONFLICT", "WARNING",
                "Vitamin C (L-AA) cần môi trường pH acid thấp (< 3.5) để thấu sâu, trong khi Retinol hoạt động tối ưu ở pH trung tính (5.5 - 6.5). Thoa chung cùng lúc gây kích ứng quá tải.",
                "Tách buổi tiêu chuẩn: Dùng Vitamin C vào Buổi Sáng (kết hợp kem chống nắng tăng bảo vệ UV) và dùng Retinol vào Buổi Tối trước khi ngủ."),

            ("vitamin-c", "niacinamide", "REDOX_CONFLICT", "WARNING",
                "Ở môi trường pH thấp và nồng độ đậm đặc, Vitamin C tinh khiết (L-AA) kết hợp Niacinamide có thể tạo phức tạm thời gây đỏ bừng mặt (flushing) với da nhạy cảm.",
                "Giải pháp an toàn: Thoa cách nhau 15 - 20 phút, hoặc thoa Vitamin C vào Buổi Sáng và Niacinamide vào Buổi Tối."),

            ("aha", "bha", "HIGH_CONFLICT", "WARNING",
                "Dùng đồng thời cả AHA và BHA nồng độ cao trong cùng một chu trình thoa dễ dẫn đến hiện tượng lột tẩy quá đà (over-exfoliation), suy yếu màng bảo vệ biểu bì.",
                "Sử dụng sản phẩm có công thức phối hợp sẵn tỷ lệ thấp an toàn, hoặc dùng xen kẽ luân phiên các ngày trong tuần."),

            ("vitamin-c", "ferulic-acid", "GOLD_SYNERGY", "SUCCESS_SYNERGY",
                "Hiệp đồng chống oxy hóa chuẩn vàng: Ferulic Acid + Vitamin E giúp ổn định phân tử L-Ascorbic Acid và nhân gấp 4 - 8 lần khả năng chống gốc tự do tia UV.",
                "Thoa kết hợp vào Buổi Sáng (AM) ngay trước bước Kem chống nắng phổ rộng để đạt hiệu quả bảo vệ tối đa."),

            ("retinol", "panthenol", "BARRIER_SYNERGY", "SUCCESS_SYNERGY",
                "Vitamin B5 (Panthenol) và Ceramides giúp củng cố hàng rào lipid gian bào, giảm tới 75% hiện tượng khô bong tróc khi mới bắt đầu dùng Retinol.",
                "Áp dụng kỹ thuật Kẹp bánh mì (Sandwich): Thoa B5/HA -> Đợi 5 phút -> Thoa Retinol -> Khóa ẩm bằng Kem Ceramide."),

            ("retinol", "ceramide", "BARRIER_SYNERGY", "SUCCESS_SYNERGY",
                "Ceramide bổ sung lipid sinh học thiết yếu, tái thiết cấu trúc màng bảo vệ biểu bì khi Retinol đẩy nhanh chu kỳ thay mới tế bào sừng.",
                "Thoa kem Ceramide sau bước Serum Retinol từ 5 - 10 phút để khóa ẩm hoàn hảo."),

            ("niacinamide", "salicylic", "BARRIER_SYNERGY", "SUCCESS_SYNERGY",
                "Niacinamide (Vitamin B3) làm dịu và kháng viêm, trong khi Salicylic Acid (BHA) len lỏi làm sạch cổ nang lông, tạo bộ đôi kiềm dầu thu nhỏ lỗ chân lông lý tưởng.",
                "Có thể kết hợp thoa BHA làm sạch trước, sau đó thoa serum Niacinamide để làm dịu da và củng cố biểu bì."),

            ("hyaluronic-acid", "ceramide", "BARRIER_SYNERGY", "SUCCESS_SYNERGY",
                "Cặp đôi vàng cấp - giữ ẩm: Hyaluronic Acid ngậm nước đa tầng biểu bì và Ceramide khóa chặt độ ẩm không cho bốc hơi qua biểu bì (TEWL).",
                "Thoa HA trên nền da còn ẩm, sau đó khóa ẩm ngay lập tức bằng kem dưỡng ẩm Ceramide.")
        ];

        public SkincareIngredientInteractionEngine(
            ApplicationDbContext context,
            ILogger<SkincareIngredientInteractionEngine> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SkincareRoutineAnalysisDto> AnalyzeCartOrRoutineAsync(
            List<int> productIds,
            int? userId = null,
            CancellationToken ct = default)
        {
            if (productIds == null || !productIds.Any())
            {
                return new SkincareRoutineAnalysisDto(
                    HasCriticalConflicts: false,
                    HasAllergyAlerts: false,
                    AllergyWarnings: new List<string>(),
                    Interactions: new List<IngredientInteractionFindingDto>(),
                    MorningRoutineSteps: new List<string>(),
                    EveningRoutineSteps: new List<string>(),
                    AlternateNightNotes: new List<string>(),
                    SynergyCrossSellSuggestions: new List<IngredientSynergyCrossSellDto>());
            }

            var products = await _context.Products
                .AsNoTracking()
                .Include(p => p.ProductIngredients)
                    .ThenInclude(pi => pi.ActiveIngredient)
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync(ct);

            var findings = new List<IngredientInteractionFindingDto>();
            var allergyWarnings = new List<string>();

            // 1. Kiểm tra tiền sử dị ứng cá nhân hóa nếu khách đã có CustomerProfile
            if (userId.HasValue)
            {
                try
                {
                    var profile = await _context.CustomerProfiles
                        .AsNoTracking()
                        .FirstOrDefaultAsync(cp => cp.UserId == userId.Value, ct);

                    if (!string.IsNullOrWhiteSpace(profile?.KnownAllergies))
                    {
                        var allergies = JsonSerializer.Deserialize<List<string>>(profile.KnownAllergies) ?? new List<string>();
                        foreach (var prod in products)
                        {
                            var fullText = (prod.Name + " " + (prod.Description ?? "") + " " + (prod.ShortDescription ?? "")).ToLowerInvariant();
                            foreach (var allergy in allergies)
                            {
                                if (!string.IsNullOrWhiteSpace(allergy) && fullText.Contains(allergy.Trim().ToLowerInvariant()))
                                {
                                    allergyWarnings.Add(
                                        $"CẢNH BÁO DỊ ỨNG CÁ NHÂN: Sản phẩm '{prod.Name}' có chứa thành phần '{allergy}' nằm trong hồ sơ tiền sử kích ứng của bạn.");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi kiểm tra dị ứng người dùng với userId={UserId}", userId);
                }
            }

            // 2. Trích xuất Active Keys cho từng sản phẩm
            var detectedMap = products.Select(p =>
            {
                var combinedText = p.Name + " " + (p.ShortDescription ?? "") + " " + (p.Description ?? "");
                var keys = ExtractActiveKeys(combinedText);

                // Thêm từ ProductIngredients quan hệ thực thể
                if (p.ProductIngredients != null)
                {
                    foreach (var pi in p.ProductIngredients)
                    {
                        if (pi.ActiveIngredient != null)
                        {
                            var ingKeyText = pi.ActiveIngredient.Name + " " + pi.ActiveIngredient.Slug;
                            foreach (var k in ExtractActiveKeys(ingKeyText))
                            {
                                keys.Add(k);
                            }
                        }
                    }
                }

                return (Product: p, Keys: keys);
            }).ToList();

            // 3. Quét ma trận tương kỵ & hiệp đồng giữa các cặp sản phẩm
            var evaluatedPairs = new HashSet<string>();

            for (int i = 0; i < detectedMap.Count; i++)
            {
                for (int j = i + 1; j < detectedMap.Count; j++)
                {
                    var itemA = detectedMap[i];
                    var itemB = detectedMap[j];

                    foreach (var rule in Rules)
                    {
                        bool matchForward = itemA.Keys.Contains(rule.KeyA) && itemB.Keys.Contains(rule.KeyB);
                        bool matchReverse = itemA.Keys.Contains(rule.KeyB) && itemB.Keys.Contains(rule.KeyA);

                        if (matchForward || matchReverse)
                        {
                            var pairKey = $"{Math.Min(itemA.Product.Id, itemB.Product.Id)}_{Math.Max(itemA.Product.Id, itemB.Product.Id)}_{rule.KeyA}_{rule.KeyB}";
                            if (!evaluatedPairs.Add(pairKey)) continue;

                            findings.Add(new IngredientInteractionFindingDto(
                                InteractionType: rule.Type,
                                SeverityBadge: rule.Severity,
                                IngredientA: FormatIngredientDisplayName(rule.KeyA),
                                ProductAName: matchForward ? itemA.Product.Name : itemB.Product.Name,
                                IngredientB: FormatIngredientDisplayName(rule.KeyB),
                                ProductBName: matchForward ? itemB.Product.Name : itemA.Product.Name,
                                ScientificExplanation: rule.Explanation,
                                RecommendedResolution: rule.Resolution));
                        }
                    }
                }
            }

            // 4. Tự động phân bổ lịch dùng Buổi Sáng (AM), Buổi Tối (PM) và Skin Cycling
            var amSteps = new List<string>();
            var pmSteps = new List<string>();
            var cyclingNotes = new List<string>();

            foreach (var (prod, keys) in detectedMap)
            {
                if (keys.Contains("retinol"))
                {
                    pmSteps.Add($"🌙 [Tối - Đặc trị chống lão hóa]: {prod.Name} (Chỉ dùng ban đêm, tránh ánh nắng mặt trời)");
                }
                else if (keys.Contains("vitamin-c") || prod.Name.Contains("Chống Nắng", StringComparison.OrdinalIgnoreCase))
                {
                    amSteps.Add($"☀️ [Sáng - Bảo vệ & Chống oxy hóa]: {prod.Name}");
                }
                else if (keys.Contains("bha") || keys.Contains("aha"))
                {
                    cyclingNotes.Add($"🔄 [Tối Cách Ngày - Tẩy TBC Hóa học]: {prod.Name} (Không dùng chung đêm với Retinol)");
                }
                else if (keys.Contains("panthenol") || keys.Contains("ceramide") || keys.Contains("hyaluronic-acid"))
                {
                    amSteps.Add($"☀️ [Sáng - Phục hồi/Cấp ẩm]: {prod.Name}");
                    pmSteps.Add($"🌙 [Tối - Phục hồi/Khóa ẩm]: {prod.Name}");
                }
                else
                {
                    amSteps.Add($"☀️ [Sáng]: {prod.Name}");
                    pmSteps.Add($"🌙 [Tối]: {prod.Name}");
                }
            }

            // 5. Gợi ý Cross-Sell sản phẩm Hiệp đồng (Synergy Cross-Sell)
            var crossSell = new List<IngredientSynergyCrossSellDto>();
            bool hasRetinol = detectedMap.Any(d => d.Keys.Contains("retinol"));
            bool hasVitaminC = detectedMap.Any(d => d.Keys.Contains("vitamin-c"));
            bool hasBha = detectedMap.Any(d => d.Keys.Contains("bha"));

            var allActiveIds = detectedMap.Select(d => d.Product.Id).ToList();

            if (hasRetinol && !detectedMap.Any(d => d.Keys.Contains("panthenol") || d.Keys.Contains("ceramide")))
            {
                var b5Product = await _context.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive && !allActiveIds.Contains(p.Id) && (p.Name.Contains("B5") || p.Name.Contains("Panthenol") || p.Name.Contains("Ceramide")))
                    .OrderByDescending(p => p.IsFeatured)
                    .FirstOrDefaultAsync(ct);

                if (b5Product != null)
                {
                    crossSell.Add(new IngredientSynergyCrossSellDto(
                        b5Product.Id,
                        b5Product.Name,
                        b5Product.Slug,
                        b5Product.DiscountPrice ?? b5Product.SellingPrice,
                        b5Product.ThumbnailUrl,
                        "Panthenol (Vitamin B5) & Ceramide",
                        "Hiệp đồng làm dịu màng biểu bì, giảm tới 75% hiện tượng bong tróc rát đỏ khi dùng Retinol."));
                }
            }

            if (hasVitaminC && !detectedMap.Any(d => d.Product.Name.Contains("Chống Nắng", StringComparison.OrdinalIgnoreCase)))
            {
                var sunProduct = await _context.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive && !allActiveIds.Contains(p.Id) && (p.Name.Contains("Chống Nắng") || p.Name.Contains("Sunscreen") || p.Name.Contains("SPF")))
                    .OrderByDescending(p => p.IsFeatured)
                    .FirstOrDefaultAsync(ct);

                if (sunProduct != null)
                {
                    crossSell.Add(new IngredientSynergyCrossSellDto(
                        sunProduct.Id,
                        sunProduct.Name,
                        sunProduct.Slug,
                        sunProduct.DiscountPrice ?? sunProduct.SellingPrice,
                        sunProduct.ThumbnailUrl,
                        "Kem Chống Nắng Phổ Rộng SPF50+",
                        "Nhân đôi hiệu quả bảo vệ quang phổ tia UV khi dùng cùng Serum Vitamin C buổi sáng."));
                }
            }

            return new SkincareRoutineAnalysisDto(
                HasCriticalConflicts: findings.Any(f => f.SeverityBadge == "DANGER" || f.SeverityBadge == "WARNING"),
                HasAllergyAlerts: allergyWarnings.Count > 0,
                AllergyWarnings: allergyWarnings,
                Interactions: findings,
                MorningRoutineSteps: amSteps.Distinct().ToList(),
                EveningRoutineSteps: pmSteps.Distinct().ToList(),
                AlternateNightNotes: cyclingNotes.Distinct().ToList(),
                SynergyCrossSellSuggestions: crossSell);
        }

        public Task<SkincareRoutineAnalysisDto> CheckIngredientConflictsAsync(
            List<string> ingredientNames,
            CancellationToken ct = default)
        {
            if (ingredientNames == null || !ingredientNames.Any())
            {
                return Task.FromResult(new SkincareRoutineAnalysisDto(
                    HasCriticalConflicts: false,
                    HasAllergyAlerts: false,
                    AllergyWarnings: new List<string>(),
                    Interactions: new List<IngredientInteractionFindingDto>(),
                    MorningRoutineSteps: new List<string>(),
                    EveningRoutineSteps: new List<string>(),
                    AlternateNightNotes: new List<string>(),
                    SynergyCrossSellSuggestions: new List<IngredientSynergyCrossSellDto>()));
            }

            var detectedKeys = new HashSet<string>();
            var keyToOriginalName = new Dictionary<string, string>();

            foreach (var ing in ingredientNames)
            {
                var keys = ExtractActiveKeys(ing);
                foreach (var k in keys)
                {
                    detectedKeys.Add(k);
                    if (!keyToOriginalName.ContainsKey(k))
                    {
                        keyToOriginalName[k] = ing;
                    }
                }
            }

            var keyList = detectedKeys.ToList();
            var findings = new List<IngredientInteractionFindingDto>();

            for (int i = 0; i < keyList.Count; i++)
            {
                for (int j = i + 1; j < keyList.Count; j++)
                {
                    var kA = keyList[i];
                    var kB = keyList[j];

                    foreach (var rule in Rules)
                    {
                        bool matchForward = kA == rule.KeyA && kB == rule.KeyB;
                        bool matchReverse = kA == rule.KeyB && kB == rule.KeyA;

                        if (matchForward || matchReverse)
                        {
                            findings.Add(new IngredientInteractionFindingDto(
                                InteractionType: rule.Type,
                                SeverityBadge: rule.Severity,
                                IngredientA: FormatIngredientDisplayName(rule.KeyA),
                                ProductAName: keyToOriginalName.GetValueOrDefault(rule.KeyA, FormatIngredientDisplayName(rule.KeyA)),
                                IngredientB: FormatIngredientDisplayName(rule.KeyB),
                                ProductBName: keyToOriginalName.GetValueOrDefault(rule.KeyB, FormatIngredientDisplayName(rule.KeyB)),
                                ScientificExplanation: rule.Explanation,
                                RecommendedResolution: rule.Resolution));
                        }
                    }
                }
            }

            return Task.FromResult(new SkincareRoutineAnalysisDto(
                HasCriticalConflicts: findings.Any(f => f.SeverityBadge == "DANGER" || f.SeverityBadge == "WARNING"),
                HasAllergyAlerts: false,
                AllergyWarnings: new List<string>(),
                Interactions: findings,
                MorningRoutineSteps: new List<string>(),
                EveningRoutineSteps: new List<string>(),
                AlternateNightNotes: new List<string>(),
                SynergyCrossSellSuggestions: new List<IngredientSynergyCrossSellDto>()));
        }

        private static HashSet<string> ExtractActiveKeys(string text)
        {
            var lower = text.ToLowerInvariant();
            var set = new HashSet<string>();

            if (lower.Contains("retinol") || lower.Contains("tretinoin") || lower.Contains("a313") || lower.Contains("retinoid"))
                set.Add("retinol");

            if (lower.Contains("bha") || lower.Contains("salicylic") || lower.Contains("salicylate"))
                set.Add("bha");

            if (lower.Contains("aha") || lower.Contains("glycolic") || lower.Contains("lactic") || lower.Contains("mandelic"))
                set.Add("aha");

            if (lower.Contains("benzoyl") || lower.Contains("bpo"))
                set.Add("benzoyl-peroxide");

            if (lower.Contains("vitamin c") || lower.Contains("ascorbic") || lower.Contains("c15") || lower.Contains("l-aa") || lower.Contains("l-ascorbic"))
                set.Add("vitamin-c");

            if (lower.Contains("copper peptide") || lower.Contains("copper tripeptide") || (lower.Contains("copper") && lower.Contains("peptide")) || lower.Contains("ghk-cu") || lower.Contains("peptide đồng"))
                set.Add("copper-peptides");

            if (lower.Contains("niacinamide") || lower.Contains("vitamin b3") || lower.Contains("b3"))
                set.Add("niacinamide");

            if (lower.Contains("ferulic") || lower.Contains("ferulic acid"))
                set.Add("ferulic-acid");

            if (lower.Contains("panthenol") || lower.Contains("vitamin b5") || lower.Contains("b5"))
                set.Add("panthenol");

            if (lower.Contains("ceramide") || lower.Contains("ceramides"))
                set.Add("ceramide");

            if (lower.Contains("hyaluronic") || lower.Contains("ha đa phân tử") || lower.Contains("sodium hyaluronate"))
                set.Add("hyaluronic-acid");

            if (lower.Contains("zinc") || lower.Contains("kẽm"))
                set.Add("zinc");

            return set;
        }

        private static string FormatIngredientDisplayName(string key)
        {
            return key switch
            {
                "retinol" => "Retinol / Retinoids",
                "bha" => "BHA (Salicylic Acid)",
                "aha" => "AHA (Glycolic / Lactic Acid)",
                "benzoyl-peroxide" => "Benzoyl Peroxide (BPO)",
                "vitamin-c" => "Vitamin C (L-Ascorbic Acid)",
                "copper-peptides" => "Copper Peptides (GHK-Cu)",
                "niacinamide" => "Niacinamide (Vitamin B3)",
                "ferulic-acid" => "Ferulic Acid",
                "panthenol" => "Panthenol (Vitamin B5)",
                "ceramide" => "Ceramide",
                "hyaluronic-acid" => "Hyaluronic Acid (HA)",
                "zinc" => "Zinc PCA",
                _ => key.ToUpperInvariant()
            };
        }
    }
}
