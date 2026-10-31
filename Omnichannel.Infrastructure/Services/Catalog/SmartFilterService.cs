using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Catalog;
using Omnichannel.Application.Interfaces.Catalog;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services.Catalog
{
    public class SmartFilterService : ISmartFilterService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SmartFilterService> _logger;

        public SmartFilterService(ApplicationDbContext context, ILogger<SmartFilterService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SmartFilterResultDto> GetSmartRecommendationsAsync(SmartFilterCriteriaDto criteria)
        {
            criteria ??= new SmartFilterCriteriaDto();

            // 1. Chuẩn hóa SkinType slug
            var normSkinType = NormalizeSkinType(criteria.SkinType);

            // 2. Chuẩn hóa Concerns slugs
            var normConcerns = (criteria.Concerns ?? new List<string>())
                .Select(NormalizeConcern)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .ToList();

            // 3. Chuẩn hóa Ngân sách (Budget ceiling / range)
            decimal? minBudget = null;
            decimal? maxBudget = criteria.Budget;

            if (!string.IsNullOrWhiteSpace(criteria.BudgetBracket))
            {
                switch (criteria.BudgetBracket.ToLowerInvariant())
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
                        maxBudget = null;
                        break;
                }
            }

            // 4. Lấy danh sách sản phẩm cùng các quan hệ thuộc tính
            var query = _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive)
                .Include(p => p.Category)
                .Include(p => p.ProductSkinTypes)
                    .ThenInclude(pst => pst.SkinType)
                .Include(p => p.ProductConcerns)
                    .ThenInclude(pc => pc.SkinConcern)
                .Include(p => p.ProductIngredients)
                    .ThenInclude(pi => pi.ActiveIngredient)
                .Include(p => p.Reviews);

            var allProducts = await query.ToListAsync();

            // 5. Áp dụng Thuật toán Chấm điểm Phù Hợp (Scoring Algorithm)
            var scoredProducts = new List<SmartFilterProductDto>();

            foreach (var p in allProducts)
            {
                var effectivePrice = p.DiscountPrice ?? p.SellingPrice;
                var matchReasons = new List<string>();
                var matchedSkinTypes = new List<string>();
                var matchedConcerns = new List<string>();

                double rawScore = 0;

                // A. Chấm điểm Skin Type (Tối đa 40 điểm)
                if (!string.IsNullOrWhiteSpace(normSkinType))
                {
                    var hasExplicitSkinType = p.ProductSkinTypes.Any(st =>
                        st.SkinType.Slug == normSkinType ||
                        st.SkinType.Name.ToLower().Contains(normSkinType));

                    var pNameLower = p.Name.ToLowerInvariant();
                    var pDescLower = (p.ShortDescription ?? "").ToLowerInvariant();

                    if (hasExplicitSkinType)
                    {
                        rawScore += 40;
                        var stName = p.ProductSkinTypes.First(st => st.SkinType.Slug == normSkinType || st.SkinType.Name.ToLower().Contains(normSkinType)).SkinType.Name;
                        matchedSkinTypes.Add(stName);
                        matchReasons.Add($"Hoàn toàn phù hợp với {stName}");
                    }
                    else if (IsTextMatchSkinType(normSkinType, pNameLower, pDescLower))
                    {
                        rawScore += 28;
                        matchedSkinTypes.Add(GetSkinTypeNameFromSlug(normSkinType));
                        matchReasons.Add($"Công thức lành tính tương thích với {GetSkinTypeNameFromSlug(normSkinType)}");
                    }
                    else
                    {
                        // Nếu sản phẩm cho da thường/mọi loại da
                        rawScore += 15;
                    }
                }
                else
                {
                    rawScore += 35; // Không chọn da cụ thể
                }

                // B. Chấm điểm Concerns (Tối đa 40 điểm)
                if (normConcerns.Any())
                {
                    double pointsPerConcern = 40.0 / normConcerns.Count;
                    int matchedConcernCount = 0;

                    foreach (var c in normConcerns)
                    {
                        var hasExplicitConcern = p.ProductConcerns.Any(pc =>
                            pc.SkinConcern.Slug == c ||
                            pc.SkinConcern.Name.ToLower().Contains(c));

                        var pNameLower = p.Name.ToLowerInvariant();
                        var pDescLower = (p.ShortDescription ?? "").ToLowerInvariant();

                        if (hasExplicitConcern)
                        {
                            rawScore += pointsPerConcern;
                            matchedConcernCount++;
                            var cName = p.ProductConcerns.First(pc => pc.SkinConcern.Slug == c || pc.SkinConcern.Name.ToLower().Contains(c)).SkinConcern.Name;
                            matchedConcerns.Add(cName);
                        }
                        else if (IsTextMatchConcern(c, pNameLower, pDescLower))
                        {
                            rawScore += pointsPerConcern * 0.75;
                            matchedConcernCount++;
                            matchedConcerns.Add(GetConcernNameFromSlug(c));
                        }
                    }

                    if (matchedConcerns.Any())
                    {
                        matchReasons.Add($"Chuyên sâu giải quyết: {string.Join(", ", matchedConcerns.Distinct())}");
                    }
                }
                else
                {
                    rawScore += 35; // Không chọn concern
                }

                // C. Chấm điểm Ngân Sách Budget (Tối đa 20 điểm)
                if (maxBudget.HasValue || minBudget.HasValue)
                {
                    bool isWithinBudget = true;
                    if (minBudget.HasValue && effectivePrice < minBudget.Value) isWithinBudget = false;
                    if (maxBudget.HasValue && effectivePrice > maxBudget.Value) isWithinBudget = false;

                    if (isWithinBudget)
                    {
                        rawScore += 20;
                        matchReasons.Add("Nằm trọn vẹn trong khoảng ngân sách mong muốn");
                    }
                    else if (maxBudget.HasValue && effectivePrice <= maxBudget.Value * 1.25m)
                    {
                        rawScore += 10; // Hơi cao hơn một chút nhưng có thể chấp nhận
                    }
                    else
                    {
                        rawScore += 0;
                    }
                }
                else
                {
                    rawScore += 18;
                }

                // D. Bonus điểm cho Đánh giá & Thành phần vàng
                var avgRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 5.0;
                if (avgRating >= 4.5) rawScore += 4;
                if (p.IsFeatured) rawScore += 3;

                // E. Chuẩn hóa Match Score (từ 72% đến 98% cho chân thực)
                int finalMatchPercent = (int)Math.Clamp(Math.Round(rawScore * 0.94), 65, 98);

                // Lấy danh sách thành phần chính
                var keyIngredients = p.ProductIngredients
                    .Select(pi => pi.ActiveIngredient.Name)
                    .Take(3)
                    .ToList();

                scoredProducts.Add(new SmartFilterProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Slug = p.Slug,
                    CategoryName = p.Category?.Name ?? "Mỹ phẩm",
                    Price = effectivePrice,
                    OriginalPrice = p.DiscountPrice.HasValue ? p.SellingPrice : null,
                    ThumbnailUrl = p.ThumbnailUrl ?? "/images/default-product.png",
                    Rating = avgRating,
                    ReviewCount = p.Reviews.Count,
                    MatchScore = finalMatchPercent,
                    MatchReasons = matchReasons,
                    MatchedSkinTypes = matchedSkinTypes.Distinct().ToList(),
                    MatchedConcerns = matchedConcerns.Distinct().ToList(),
                    KeyIngredients = keyIngredients
                });
            }

            // 6. Sắp xếp giảm dần theo điểm MatchScore, ưu tiên sản phẩm match cao nhất
            var sortedProducts = scoredProducts
                .OrderByDescending(p => p.MatchScore)
                .ThenByDescending(p => p.Rating)
                .Take(criteria.Limit)
                .ToList();

            // 7. Tạo quy trình đề xuất cá nhân hóa
            var routine = GenerateRoutineRecommendation(normSkinType, normConcerns);

            return new SmartFilterResultDto
            {
                Success = true,
                TotalMatched = sortedProducts.Count,
                Criteria = criteria,
                Products = sortedProducts,
                RecommendedRoutine = routine
            };
        }

        private string NormalizeSkinType(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var lower = input.Trim().ToLowerInvariant();

            return lower switch
            {
                "oily" or "da-dau" or "dầu" or "da dầu" => "da-dau",
                "dry" or "da-kho" or "khô" or "da khô" => "da-kho",
                "combination" or "da-hon-hop" or "hỗn hợp" or "da hỗn hợp" => "da-hon-hop",
                "sensitive" or "da-nhay-cam" or "nhạy cảm" or "da nhạy cảm" => "da-nhay-cam",
                "normal" or "da-thuong" or "thường" or "da thường" => "da-thuong",
                _ => lower
            };
        }

        private string NormalizeConcern(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var lower = input.Trim().ToLowerInvariant();

            return lower switch
            {
                "hydration" or "duong-am" or "dưỡng ẩm" or "am" => "hydration",
                "sun_protection" or "sun" or "chong-nang" or "chống nắng" => "sun_protection",
                "acne" or "tri-mun" or "trị mụn" or "mun" => "acne",
                "anti_aging" or "chong-lao-hoa" or "chống lão hóa" or "lao-hoa" => "anti_aging",
                "brightening" or "lam-sang" or "làm sáng" or "sang-da" => "brightening",
                "pore_minimizing" or "se-khit-lo-chan-long" or "lỗ chân lông" or "se-khit" => "pore_minimizing",
                _ => lower
            };
        }

        private bool IsTextMatchSkinType(string normSkinType, string name, string desc)
        {
            return normSkinType switch
            {
                "da-dau" => name.Contains("dầu") || desc.Contains("dầu") || name.Contains("kiềm dầu") || name.Contains("bha") || desc.Contains("kiềm dầu"),
                "da-kho" => name.Contains("ẩm") || desc.Contains("ẩm") || name.Contains("khô") || desc.Contains("khô") || name.Contains("ha") || name.Contains("b5"),
                "da-nhay-cam" => name.Contains("nhạy cảm") || desc.Contains("nhạy cảm") || name.Contains("dịu nhẹ") || name.Contains("centella") || name.Contains("rau má"),
                "da-hon-hop" => name.Contains("hỗn hợp") || desc.Contains("hỗn hợp") || name.Contains("cân bằng"),
                _ => true
            };
        }

        private bool IsTextMatchConcern(string normConcern, string name, string desc)
        {
            return normConcern switch
            {
                "hydration" => name.Contains("ẩm") || desc.Contains("ẩm") || name.Contains("ha") || name.Contains("hyaluronic") || name.Contains("ceramide") || name.Contains("b5"),
                "sun_protection" => name.Contains("chống nắng") || desc.Contains("chống nắng") || name.Contains("sun") || name.Contains("spf") || name.Contains("uv"),
                "acne" => name.Contains("mụn") || desc.Contains("mụn") || name.Contains("bha") || name.Contains("tràm trà") || name.Contains("salicylic"),
                "anti_aging" => name.Contains("lão hóa") || desc.Contains("lão hóa") || name.Contains("retinol") || name.Contains("collagen") || name.Contains("nếp nhăn"),
                "brightening" => name.Contains("sáng") || desc.Contains("sáng") || name.Contains("trắng") || name.Contains("vitamin c") || name.Contains("niacinamide") || name.Contains("thâm"),
                "pore_minimizing" => name.Contains("lỗ chân lông") || desc.Contains("lỗ chân lông") || name.Contains("se khít") || name.Contains("bã nhờn") || name.Contains("kiềm dầu"),
                _ => false
            };
        }

        private string GetSkinTypeNameFromSlug(string slug)
        {
            return slug switch
            {
                "da-dau" => "Da Dầu",
                "da-kho" => "Da Khô",
                "da-hon-hop" => "Da Hỗn Hợp",
                "da-nhay-cam" => "Da Nhạy Cảm",
                "da-thuong" => "Da Thường",
                _ => "Da Thường"
            };
        }

        private string GetConcernNameFromSlug(string slug)
        {
            return slug switch
            {
                "hydration" => "Dưỡng Ẩm",
                "sun_protection" => "Chống Nắng",
                "acne" => "Trị Mụn",
                "anti_aging" => "Chống Lão Hóa",
                "brightening" => "Làm Sáng Da",
                "pore_minimizing" => "Se Khít Lỗ Chân Lông",
                _ => slug
            };
        }

        private List<string> GenerateRoutineRecommendation(string skinType, List<string> concerns)
        {
            var routine = new List<string>();

            // Bước 1: Làm sạch
            if (skinType == "da-dau" || concerns.Contains("acne"))
            {
                routine.Add("1️⃣ Làm sạch sâu: Sữa rửa mặt dạng Gel kiềm dầu, làm sạch bã nhờn dịu nhẹ");
            }
            else if (skinType == "da-kho" || concerns.Contains("hydration"))
            {
                routine.Add("1️⃣ Làm sạch ẩm mượt: Sữa rửa mặt tạo bọt dịu nhẹ bổ sung Amino Acid cấp ẩm");
            }
            else
            {
                routine.Add("1️⃣ Làm sạch chuẩn y khoa: Sữa rửa mặt cân bằng pH 5.5");
            }

            // Bước 2: Cân bằng & Mở đường dẫn
            routine.Add("2️⃣ Nước hoa hồng (Toner): Cân bằng độ ẩm và mở đường dẫn hấp thụ tinh chất");

            // Bước 3: Serum đặc trị theo concern
            if (concerns.Contains("acne") || concerns.Contains("pore_minimizing"))
            {
                routine.Add("3️⃣ Tinh chất Serum: Niacinamide 10% + Zinc hoặc BHA 2% kháng viêm thu nhỏ lỗ chân lông");
            }
            else if (concerns.Contains("brightening"))
            {
                routine.Add("3️⃣ Tinh chất Serum: Vitamin C tinh khiết hoặc Arbutin ức chế melanin mờ thâm");
            }
            else if (concerns.Contains("anti_aging"))
            {
                routine.Add("3️⃣ Tinh chất Serum: Retinol bọc vi nang kích thích tăng sinh collagen ban đêm");
            }
            else
            {
                routine.Add("3️⃣ Tinh chất Serum: Hyaluronic Acid đa phân tử cấp nước sâu");
            }

            // Bước 4: Khóa ẩm
            routine.Add("4️⃣ Khóa ẩm phục hồi: Kem dưỡng củng cố hàng rào lipid chống mất nước");

            // Bước 5: Chống nắng
            routine.Add("5️⃣ Bảo vệ ban ngày: Thoa kem chống nắng phổ rộng SPF 50+ PA++++ trước khi ra ngoài 20 phút");

            return routine;
        }

        public async Task<List<SkinTypeDto>> GetAvailableSkinTypesAsync()
        {
            return await _context.SkinTypes
                .AsNoTracking()
                .OrderBy(st => st.Id)
                .Select(st => new SkinTypeDto
                {
                    Id = st.Id,
                    Name = st.Name,
                    Slug = st.Slug
                })
                .ToListAsync();
        }

        public async Task<List<SkinConcernDto>> GetAvailableSkinConcernsAsync()
        {
            return await _context.SkinConcerns
                .AsNoTracking()
                .Where(sc => sc.IsActive)
                .OrderBy(sc => sc.SortOrder)
                .Select(sc => new SkinConcernDto
                {
                    Id = sc.Id,
                    Name = sc.Name,
                    Slug = sc.Slug,
                    Icon = sc.Icon,
                    Description = sc.Description
                })
                .ToListAsync();
        }

        public async Task<ProductAttributesViewModel> GetProductAttributesOverviewAsync(string? search = null, int? categoryId = null, int? skinTypeId = null, int? concernId = null)
        {
            var query = _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive)
                .Include(p => p.Category)
                .Include(p => p.ProductSkinTypes)
                    .ThenInclude(pst => pst.SkinType)
                .Include(p => p.ProductConcerns)
                    .ThenInclude(pc => pc.SkinConcern)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(s) || p.Sku.ToLower().Contains(s));
            }

            if (categoryId.HasValue && categoryId > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (skinTypeId.HasValue && skinTypeId > 0)
            {
                query = query.Where(p => p.ProductSkinTypes.Any(pst => pst.SkinTypeId == skinTypeId.Value));
            }

            if (concernId.HasValue && concernId > 0)
            {
                query = query.Where(p => p.ProductConcerns.Any(pc => pc.SkinConcernId == concernId.Value));
            }

            var total = await query.CountAsync();
            var products = await query
                .OrderByDescending(p => p.Id)
                .Take(50)
                .Select(p => new ProductAttributeManageDto
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    Sku = p.Sku,
                    CategoryName = p.Category.Name,
                    Price = p.DiscountPrice ?? p.SellingPrice,
                    ThumbnailUrl = p.ThumbnailUrl,
                    AssignedSkinTypeIds = p.ProductSkinTypes.Select(pst => pst.SkinTypeId).ToList(),
                    AssignedSkinTypeNames = p.ProductSkinTypes.Select(pst => pst.SkinType.Name).ToList(),
                    AssignedConcernIds = p.ProductConcerns.Select(pc => pc.SkinConcernId).ToList(),
                    AssignedConcernNames = p.ProductConcerns.Select(pc => pc.SkinConcern.Name).ToList()
                })
                .ToListAsync();

            var skinTypes = await GetAvailableSkinTypesAsync();
            var concerns = await GetAvailableSkinConcernsAsync();

            return new ProductAttributesViewModel
            {
                Products = products,
                AvailableSkinTypes = skinTypes,
                AvailableConcerns = concerns,
                TotalProducts = total
            };
        }

        public async Task<bool> UpdateProductAttributesAsync(int productId, List<int> skinTypeIds, List<int> concernIds)
        {
            var product = await _context.Products
                .Include(p => p.ProductSkinTypes)
                .Include(p => p.ProductConcerns)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return false;

            // Xóa cũ
            _context.ProductSkinTypes.RemoveRange(product.ProductSkinTypes);
            _context.ProductConcerns.RemoveRange(product.ProductConcerns);

            // Thêm mới
            if (skinTypeIds != null && skinTypeIds.Any())
            {
                foreach (var id in skinTypeIds.Distinct())
                {
                    _context.ProductSkinTypes.Add(new ProductSkinType
                    {
                        ProductId = productId,
                        SkinTypeId = id
                    });
                }
            }

            if (concernIds != null && concernIds.Any())
            {
                foreach (var id in concernIds.Distinct())
                {
                    _context.ProductConcerns.Add(new ProductConcern
                    {
                        ProductId = productId,
                        SkinConcernId = id
                    });
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> BulkUpdateAttributesAsync(BulkUpdateAttributesDto dto)
        {
            if (dto == null || dto.ProductIds == null || !dto.ProductIds.Any()) return 0;

            var targetProducts = await _context.Products
                .Include(p => p.ProductSkinTypes)
                .Include(p => p.ProductConcerns)
                .Where(p => dto.ProductIds.Contains(p.Id))
                .ToListAsync();

            if (!targetProducts.Any()) return 0;

            int updatedCount = 0;

            foreach (var p in targetProducts)
            {
                if (dto.Mode == "REPLACE")
                {
                    // Ghi đè toàn bộ
                    _context.ProductSkinTypes.RemoveRange(p.ProductSkinTypes);
                    _context.ProductConcerns.RemoveRange(p.ProductConcerns);

                    if (dto.SkinTypeIds != null)
                    {
                        foreach (var stId in dto.SkinTypeIds.Distinct())
                        {
                            _context.ProductSkinTypes.Add(new ProductSkinType { ProductId = p.Id, SkinTypeId = stId });
                        }
                    }

                    if (dto.ConcernIds != null)
                    {
                        foreach (var cId in dto.ConcernIds.Distinct())
                        {
                            _context.ProductConcerns.Add(new ProductConcern { ProductId = p.Id, SkinConcernId = cId });
                        }
                    }
                }
                else
                {
                    // Chế độ ADD: Chỉ thêm mới nếu chưa có
                    if (dto.SkinTypeIds != null)
                    {
                        foreach (var stId in dto.SkinTypeIds.Distinct())
                        {
                            if (!p.ProductSkinTypes.Any(x => x.SkinTypeId == stId))
                            {
                                _context.ProductSkinTypes.Add(new ProductSkinType { ProductId = p.Id, SkinTypeId = stId });
                            }
                        }
                    }

                    if (dto.ConcernIds != null)
                    {
                        foreach (var cId in dto.ConcernIds.Distinct())
                        {
                            if (!p.ProductConcerns.Any(x => x.SkinConcernId == cId))
                            {
                                _context.ProductConcerns.Add(new ProductConcern { ProductId = p.Id, SkinConcernId = cId });
                            }
                        }
                    }
                }

                updatedCount++;
            }

            await _context.SaveChangesAsync();
            return updatedCount;
        }
    }
}
