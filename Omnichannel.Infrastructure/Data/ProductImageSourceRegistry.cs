using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Omnichannel.Infrastructure.Data
{
    public class ProductImageSourceItem
    {
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string CategorySubfolder { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string DirectImageUrl { get; set; } = string.Empty;
        public string AltText { get; set; } = string.Empty;

        public string CanonicalRelativeUrl =>
            string.IsNullOrWhiteSpace(CategorySubfolder) || string.IsNullOrWhiteSpace(FileName)
                ? ProductImageSourceRegistry.PlaceholderUrl
                : $"/images/{CategorySubfolder.Trim('/')}/{FileName.Trim('/')}";
    }

    /// <summary>
    /// Nguồn chuẩn duy nhất (Single Source of Truth) quản lý và phân giải đường dẫn ảnh sản phẩm
    /// về thư mục chuẩn /images/products/{skincare|makeup|perfume|hair-body}/{fileName},
    /// có cơ chế tự động ánh xạ đường dẫn cũ và fallback an toàn về /images/product-placeholder.svg.
    /// </summary>
    public static class ProductImageSourceRegistry
    {
        public const string PlaceholderUrl = "/images/products/_placeholder.webp";
        public const string LegacySvgPlaceholderUrl = "/images/product-placeholder.svg";
        public const string DefaultAvatarUrl = "/images/branding/logo-circle.png";

        private static readonly Lazy<RegistryLookupCache> _cache = new(BuildLookupCache, isThreadSafe: true);

        private sealed class RegistryLookupCache
        {
            public List<ProductImageSourceItem> Items { get; init; } = new();
            public Dictionary<string, string> BySku { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, string> BySlug { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, string> ByFileName { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, ProductImageSourceItem> ItemBySku { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, ProductImageSourceItem> ItemBySlug { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> ValidCanonicalUrls { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private static readonly Dictionary<string, string> CategorySlugImageMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["trang-diem"] = "/images/categories/kem-nen.jpg",
            ["cham-soc-da"] = "/images/categories/serum-tinh-chat.jpg",
            ["nuoc-hoa"] = "/images/categories/nuoc-hoa-nu.jpg",
            ["toc-co-the"] = "/images/categories/cham-soc-co-the.jpg",
            ["son-moi"] = "/images/categories/son-moi.jpg",
            ["kem-nen-cushion"] = "/images/categories/kem-nen.jpg",
            ["phan-mat"] = "/images/categories/kem-nen.jpg",
            ["sua-rua-mat"] = "/images/categories/sua-rua-mat.jpg",
            ["serum-tinh-chat"] = "/images/categories/serum-tinh-chat.jpg",
            ["kem-chong-nang"] = "/images/categories/kem-chong-nang.jpg",
            ["nuoc-tay-trang"] = "/images/categories/toner-nuoc-hoa-hong.jpg",
            ["cham-soc-toc"] = "/images/categories/cham-soc-toc.jpg",
            ["cham-soc-co-the"] = "/images/categories/cham-soc-co-the.jpg",
            ["dung-cu-lam-dep"] = "/images/categories/kem-nen.jpg",
            ["set-qua-tang"] = "/images/categories/nuoc-hoa-nu.jpg",
            ["toner-nuoc-hoa-hong"] = "/images/categories/toner-nuoc-hoa-hong.jpg",
            ["kem-duong-am"] = "/images/categories/serum-tinh-chat.jpg",
            ["mat-na-duong-da"] = "/images/categories/sua-rua-mat.jpg",
            ["cham-soc-vung-mat"] = "/images/categories/serum-tinh-chat.jpg",
            ["kem-lot-kem-nen"] = "/images/categories/kem-nen.jpg",
            ["ma-hong-bat-sang"] = "/images/categories/son-moi.jpg",
            ["trang-diem-mat-mascara"] = "/images/categories/kem-nen.jpg",
            ["xit-khoa-nen"] = "/images/categories/toner-nuoc-hoa-hong.jpg",
            ["nuoc-hoa-nu"] = "/images/categories/nuoc-hoa-nu.jpg",
            ["nuoc-hoa-nam"] = "/images/categories/nuoc-hoa-nam.jpg",
            ["nuoc-hoa-unisex"] = "/images/categories/nuoc-hoa-nam.jpg",
            ["giftset-nuoc-hoa"] = "/images/categories/nuoc-hoa-nu.jpg",
            ["nuoc-hoa-mini"] = "/images/categories/nuoc-hoa-nu.jpg",
            ["dau-goi-dau-xa"] = "/images/categories/cham-soc-toc.jpg",
            ["dau-duong-serum-toc"] = "/images/categories/cham-soc-toc.jpg",
            ["mat-na-u-toc"] = "/images/categories/cham-soc-toc.jpg",
            ["san-pham-tao-kieu"] = "/images/categories/cham-soc-toc.jpg",
            ["sua-tam-duong-am"] = "/images/categories/cham-soc-co-the.jpg",
            ["duong-the-body"] = "/images/categories/cham-soc-co-the.jpg",
            ["tay-te-bao-chet-body"] = "/images/categories/cham-soc-co-the.jpg",
            ["body-mist"] = "/images/categories/nuoc-hoa-nu.jpg",
            ["thuc-pham-lam-dep"] = "/images/categories/serum-tinh-chat.jpg",
            ["collagen-nuoc"] = "/images/categories/serum-tinh-chat.jpg",
            ["vitamin-vien-uong"] = "/images/categories/serum-tinh-chat.jpg",
            ["nuoc-hong-sam"] = "/images/categories/serum-tinh-chat.jpg"
        };

        private static readonly Dictionary<string, string> LegacyShortAliasMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["serum-b5.jpg"] = "/images/products/skincare/serum-phuc-hoi-la-roche-posay-hyalu-b5-pure-30ml.jpg",
            ["tinh-chat-la-roche-posay-hyalu-b5-serum-30ml.jpg"] = "/images/products/skincare/serum-phuc-hoi-la-roche-posay-hyalu-b5-pure-30ml.jpg",
            ["nuoc-tay-trang-loreal-paris-micellar-water-3-in-1-deep-cleansing-400ml.jpg"] = "/images/products/skincare/nuoc-tay-trang-bioderma-sensibio-h2o-500ml.png",
            ["kem-duong-am-laneige-water-bank-blue-hyaluronic-cream-50ml.jpg"] = "/images/products/skincare/mat-na-ngu-laneige-water-sleeping-mask-ex-70ml.jpg",
            ["kem-chong-nang-anessa-perfect-uv-sunscreen-skincare-milk-60ml.jpg"] = "/images/products/skincare/sua-chong-nang-anessa-perfect-uv-sunscreen-skincare-milk-spf50-60ml.jpg",
            ["kem-chong-nang-la-roche-posay-anthelios-oil-correct-spf50-50ml.jpg"] = "/images/products/skincare/kem-chong-nang-la-roche-posay-anthelios-uvmune-400-oil-control-50ml.jpg",
            ["tinh-chat-skin1004-madagascar-centella-ampoule-100ml.jpg"] = "/images/products/skincare/tinh-chat-rau-ma-skin1004-madagascar-centella-ampoule-100ml.jpg",
            ["kem-duong-la-roche-posay-cicaplast-baume-b5-plus-40ml.webp"] = "/images/products/skincare/kem-duong-phuc-hoi-la-roche-posay-cicaplast-baume-b5-plus-40ml.jpg",
            ["son-thoi-mac-matte-lipstick-ruby-woo.jpg"] = "/images/products/makeup/son-thoi-mac-retro-matte-lipstick-707-ruby-woo-3g.jpg",
            ["kem-nen-maybelline-fit-me-matte-poreless-foundation-30ml.jpg"] = "/images/products/makeup/kem-nen-loreal-infallible-32h-fresh-wear-foundation-30ml.jpg",
            ["nuoc-hoa-nam-dior-sauvage-eau-de-toilette-100ml.jpg"] = "/images/products/perfume/nuoc-hoa-nam-dior-sauvage-eau-de-parfum-100ml.jpg",
            ["dau-duong-toc-moroccanoil-treatment-original-100ml.jpg"] = "/images/products/hair-body/tinh-dau-duong-toc-moroccanoil-treatment-original-100ml.jpg",
            ["perfume-jazz-club.jpg"] = "/images/products/perfume/nuoc-hoa-unisex-replica-jazz-club-edt-100ml.jpg",
            ["clinique-moisture.jpg"] = "/images/products/skincare/kem-duong-am-chuyen-sau-clinique-moisture-surge-100h-50ml.jpg",
            ["toner-bha.jpg"] = "/images/products/skincare/dung-dich-tay-te-bao-chet-paulas-choice-skin-perfecting-2-bha-118ml.png",
            ["paulas-choice-bha.jpg"] = "/images/products/skincare/dung-dich-tay-te-bao-chet-paulas-choice-skin-perfecting-2-bha-118ml.png",
            ["cleanser-cerave.jpg"] = "/images/products/skincare/sua-rua-mat-cerave-foaming-facial-cleanser-236ml.jpg",
            ["skinceuticals-b5.jpg"] = "/images/products/skincare/serum-phuc-hoi-la-roche-posay-hyalu-b5-pure-30ml.jpg",
            ["mac-chili.jpg"] = "/images/products/makeup/son-thoi-mac-matte-lipstick-602-chili-3g.jpg",
            ["sunscreen-anessa.jpg"] = "/images/products/skincare/sua-chong-nang-anessa-perfect-uv-sunscreen-skincare-milk-spf50-60ml.jpg"
        };

        public static List<ProductImageSourceItem> GetAllItems() => _cache.Value.Items;

        /// <summary>
        /// Phân giải mọi đường dẫn ảnh sản phẩm về chuẩn /images/products/{category}/{fileName}.
        /// Tự động chuyển đổi đường dẫn cũ (đường dẫn phẳng, đường dẫn gãy)
        /// hoặc tìm theo SKU / Slug / Tên sản phẩm; nếu không tìm thấy sẽ trả về /images/product-placeholder.svg.
        /// </summary>
        public static string ResolveProductImageUrl(
            string? candidateUrl,
            string? sku = null,
            string? slug = null,
            string? productName = null)
        {
            var cache = _cache.Value;

            // 1. Ưu tiên tra cứu trực tiếp theo SKU hoặc Slug trong Registry chuẩn
            if (!string.IsNullOrWhiteSpace(sku) && cache.BySku.TryGetValue(sku.Trim(), out var bySkuUrl))
            {
                return bySkuUrl;
            }

            if (!string.IsNullOrWhiteSpace(slug) && cache.BySlug.TryGetValue(slug.Trim(), out var bySlugUrl))
            {
                return bySlugUrl;
            }

            // 2. Xử lý candidateUrl nếu được truyền vào
            if (!string.IsNullOrWhiteSpace(candidateUrl))
            {
                var trimmed = candidateUrl.Trim().Replace("\\", "/");

                // Bỏ qua các đường dẫn placeholder
                if (trimmed.Contains("default-product", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.Contains("placeholder-product", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.Equals(LegacySvgPlaceholderUrl, StringComparison.OrdinalIgnoreCase) ||
                    trimmed.Equals(PlaceholderUrl, StringComparison.OrdinalIgnoreCase))
                {
                    trimmed = string.Empty;
                }

                if (!string.IsNullOrEmpty(trimmed))
                {
                    // Nếu đã là đường dẫn chuẩn hợp lệ trong /images/products/{sub}/{file}
                    if (cache.ValidCanonicalUrls.Contains(trimmed))
                    {
                        return trimmed;
                    }

                    // Nếu là đường dẫn /media/{id}, /uploads/..., hoặc ảnh danh mục/banner/brand hợp lệ
                    if (trimmed.StartsWith("/media/", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.StartsWith("/images/banners/", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.StartsWith("/images/categories/", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.StartsWith("/images/branding/", StringComparison.OrdinalIgnoreCase))
                    {
                        return trimmed;
                    }

                    // Bóc tách fileName từ đường dẫn bất kỳ để ánh xạ về thư mục chuẩn /images/products/{sub}/{file}
                    var fileName = Path.GetFileName(trimmed);
                    if (!string.IsNullOrWhiteSpace(fileName))
                    {
                        if (LegacyShortAliasMap.TryGetValue(fileName, out var aliasResolved))
                        {
                            return aliasResolved;
                        }

                        if (cache.ByFileName.TryGetValue(fileName, out var byFileUrl))
                        {
                            return byFileUrl;
                        }

                        var baseName = Path.GetFileNameWithoutExtension(fileName);
                        if (!string.IsNullOrWhiteSpace(baseName) && cache.BySlug.TryGetValue(baseName, out var byBaseSlugUrl))
                        {
                            return byBaseSlugUrl;
                        }
                    }
                }
            }

            // 3. Fallback tìm kiếm thông minh theo tên sản phẩm nếu có
            if (!string.IsNullOrWhiteSpace(productName))
            {
                if (productName.Contains("BHA", StringComparison.OrdinalIgnoreCase) ||
                    productName.Contains("Paula", StringComparison.OrdinalIgnoreCase))
                {
                    return "/images/products/skincare/dung-dich-tay-te-bao-chet-paulas-choice-skin-perfecting-2-bha-118ml.png";
                }
                if (productName.Contains("Retinol", StringComparison.OrdinalIgnoreCase) ||
                    productName.Contains("Night Repair", StringComparison.OrdinalIgnoreCase))
                {
                    return "/images/products/skincare/tinh-chat-estee-lauder-advanced-night-repair-50ml.jpg";
                }
                if (productName.Contains("Silcot", StringComparison.OrdinalIgnoreCase))
                {
                    return "/images/products/skincare/bong-tay-trang-cao-cap-silcot-huu-co-100-mieng.jpg";
                }
            }

            return PlaceholderUrl;
        }

        /// <summary>
        /// Phân giải URL ảnh định dạng WebP (/images/products/{sub}/{file}.webp) phục vụ thẻ &lt;picture&gt;.
        /// </summary>
        public static string ResolveWebpImageUrl(
            string? candidateUrl,
            string? sku = null,
            string? slug = null,
            string? productName = null)
        {
            var primary = ResolveProductImageUrl(candidateUrl, sku, slug, productName);
            if (string.IsNullOrWhiteSpace(primary) ||
                primary.Equals(PlaceholderUrl, StringComparison.OrdinalIgnoreCase) ||
                primary.Equals(LegacySvgPlaceholderUrl, StringComparison.OrdinalIgnoreCase))
            {
                return PlaceholderUrl;
            }

            if (primary.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ||
                primary.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ||
                primary.StartsWith("/media/", StringComparison.OrdinalIgnoreCase) ||
                primary.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                return primary;
            }

            if (primary.StartsWith("/images/products/", StringComparison.OrdinalIgnoreCase) ||
                primary.StartsWith("/images/banners/", StringComparison.OrdinalIgnoreCase) ||
                primary.StartsWith("/images/categories/", StringComparison.OrdinalIgnoreCase))
            {
                var ext = Path.GetExtension(primary);
                if (!string.IsNullOrEmpty(ext))
                {
                    return primary.Substring(0, primary.Length - ext.Length) + ".webp";
                }
            }

            return primary;
        }

        /// <summary>
        /// Phân giải danh sách ảnh bộ sưu tập (Gallery) cho trang Chi tiết sản phẩm (PDP) và QuickView.
        /// </summary>
        public static IReadOnlyList<string> ResolveGalleryImageUrls(
            string? primaryUrl,
            string? imagesJson = null,
            string? sku = null,
            string? slug = null,
            string? productName = null)
        {
            var primary = ResolveProductImageUrl(primaryUrl, sku, slug, productName);
            var list = new List<string> { primary };

            if (!string.IsNullOrWhiteSpace(imagesJson))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<List<string>>(imagesJson);
                    if (parsed != null)
                    {
                        foreach (var raw in parsed)
                        {
                            if (string.IsNullOrWhiteSpace(raw)) continue;
                            var resolved = ResolveProductImageUrl(raw, sku, slug, productName);
                            if (!string.IsNullOrWhiteSpace(resolved) &&
                                !resolved.Equals(PlaceholderUrl, StringComparison.OrdinalIgnoreCase) &&
                                !list.Contains(resolved, StringComparer.OrdinalIgnoreCase))
                            {
                                list.Add(resolved);
                            }
                        }
                    }
                }
                catch
                {
                    // Bỏ qua nếu JSON không hợp lệ, giữ nguyên ảnh chính
                }
            }

            return list;
        }

        /// <summary>
        /// Sinh văn bản thay thế (alt text) tiếng Việt chuẩn SEO chứa Tên sản phẩm + Thương hiệu.
        /// </summary>
        public static string ResolveAltText(
            string? productName,
            string? brandName = null,
            string? categoryName = null,
            string? sku = null,
            string? slug = null)
        {
            var cache = _cache.Value;
            if (!string.IsNullOrWhiteSpace(sku) && cache.ItemBySku.TryGetValue(sku.Trim(), out var itemBySku))
            {
                if (!string.IsNullOrWhiteSpace(itemBySku.AltText))
                {
                    return itemBySku.AltText;
                }
                if (string.IsNullOrWhiteSpace(brandName) && !string.IsNullOrWhiteSpace(itemBySku.BrandName))
                {
                    brandName = itemBySku.BrandName;
                }
            }
            else if (!string.IsNullOrWhiteSpace(slug) && cache.ItemBySlug.TryGetValue(slug.Trim(), out var itemBySlug))
            {
                if (!string.IsNullOrWhiteSpace(itemBySlug.AltText))
                {
                    return itemBySlug.AltText;
                }
                if (string.IsNullOrWhiteSpace(brandName) && !string.IsNullOrWhiteSpace(itemBySlug.BrandName))
                {
                    brandName = itemBySlug.BrandName;
                }
            }

            var cleanName = string.IsNullOrWhiteSpace(productName)
                ? "Mỹ phẩm chính hãng"
                : productName.Trim();

            var cleanBrand = brandName?.Trim();
            if (!string.IsNullOrWhiteSpace(cleanBrand))
            {
                if (cleanName.Contains(cleanBrand, StringComparison.OrdinalIgnoreCase))
                {
                    return $"{cleanName} - Chính hãng {cleanBrand}";
                }
                return $"{cleanName} - Thương hiệu {cleanBrand} chính hãng";
            }

            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                return $"{cleanName} - {categoryName.Trim()} chính hãng";
            }

            return $"{cleanName} - Mỹ phẩm chính hãng";
        }

        /// <summary>
        /// Chuyển đổi URL ảnh tương đối sang URL tuyệt đối (https://domain/images/products/...) cho Schema.org Product image và Open Graph.
        /// </summary>
        public static string ResolveAbsoluteImageUrl(
            string? relativeOrAbsoluteUrl,
            string? baseUrl,
            string? sku = null,
            string? slug = null,
            string? productName = null)
        {
            var resolved = ResolveProductImageUrl(relativeOrAbsoluteUrl, sku, slug, productName);
            if (resolved.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                resolved.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return resolved;
            }

            var cleanBase = string.IsNullOrWhiteSpace(baseUrl)
                ? "https://localhost:5001"
                : baseUrl.Trim().TrimEnd('/');

            return $"{cleanBase}/{resolved.TrimStart('/')}";
        }

        /// <summary>
        /// Phân giải URL ảnh danh mục (/images/categories/{slug}.jpg) hoặc ảnh dự phòng, tránh link ngoài bị gãy.
        /// </summary>
        public static string ResolveCategoryImageUrl(string? categorySlug, string? candidateUrl = null)
        {
            if (!string.IsNullOrWhiteSpace(categorySlug) &&
                CategorySlugImageMap.TryGetValue(categorySlug.Trim(), out var mapped))
            {
                return mapped;
            }

            if (!string.IsNullOrWhiteSpace(candidateUrl))
            {
                var trimmed = candidateUrl.Trim().Replace("\\", "/");
                if (trimmed.StartsWith("/images/categories/", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("/media/image/", StringComparison.OrdinalIgnoreCase))
                {
                    return trimmed;
                }
            }

            return "/images/categories/serum-tinh-chat.jpg";
        }

        private static RegistryLookupCache BuildLookupCache()
        {
            var items = LoadRegistryItemsFromDiskOrFallback();
            var bySku = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var bySlug = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var byFileName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var itemBySku = new Dictionary<string, ProductImageSourceItem>(StringComparer.OrdinalIgnoreCase);
            var itemBySlug = new Dictionary<string, ProductImageSourceItem>(StringComparer.OrdinalIgnoreCase);
            var validUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { PlaceholderUrl };

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.CategorySubfolder) ||
                    !item.CategorySubfolder.StartsWith("products", StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrWhiteSpace(item.FileName))
                {
                    continue;
                }

                var canonicalUrl = item.FileName.Equals("_placeholder.webp", StringComparison.OrdinalIgnoreCase)
                    ? PlaceholderUrl
                    : $"/images/{item.CategorySubfolder.Trim('/')}/{item.FileName.Trim('/')}";
                validUrls.Add(canonicalUrl);

                if (!string.IsNullOrWhiteSpace(item.Sku))
                {
                    bySku[item.Sku.Trim()] = canonicalUrl;
                    itemBySku[item.Sku.Trim()] = item;
                }
                if (!string.IsNullOrWhiteSpace(item.Slug))
                {
                    bySlug[item.Slug.Trim()] = canonicalUrl;
                    itemBySlug[item.Slug.Trim()] = item;
                }
                if (!canonicalUrl.Equals(PlaceholderUrl, StringComparison.OrdinalIgnoreCase))
                {
                    byFileName[item.FileName.Trim()] = canonicalUrl;
                }
            }

            // Quét bổ sung thư mục vật lý wwwroot/images/products/** nếu có trên đĩa
            var physicalProductsRoot = ResolvePhysicalProductsDirectory();
            if (!string.IsNullOrEmpty(physicalProductsRoot) && Directory.Exists(physicalProductsRoot))
            {
                foreach (var file in Directory.GetFiles(physicalProductsRoot, "*.*", SearchOption.AllDirectories))
                {
                    var fn = Path.GetFileName(file);
                    var subDir = Path.GetFileName(Path.GetDirectoryName(file) ?? string.Empty);
                    if (string.IsNullOrWhiteSpace(fn) || string.IsNullOrWhiteSpace(subDir)) continue;

                    var canonicalUrl = $"/images/products/{subDir}/{fn}";
                    validUrls.Add(canonicalUrl);
                    if (!byFileName.ContainsKey(fn))
                    {
                        byFileName[fn] = canonicalUrl;
                    }
                    var baseSlug = Path.GetFileNameWithoutExtension(fn);
                    if (!string.IsNullOrWhiteSpace(baseSlug) && !bySlug.ContainsKey(baseSlug))
                    {
                        bySlug[baseSlug] = canonicalUrl;
                    }
                }
            }

            foreach (var kv in LegacyShortAliasMap)
            {
                validUrls.Add(kv.Value);
            }

            return new RegistryLookupCache
            {
                Items = items,
                BySku = bySku,
                BySlug = bySlug,
                ByFileName = byFileName,
                ItemBySku = itemBySku,
                ItemBySlug = itemBySlug,
                ValidCanonicalUrls = validUrls
            };
        }

        private static string? ResolvePhysicalProductsDirectory()
        {
            var candidates = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products"),
                Path.Combine(Directory.GetCurrentDirectory(), "Omnichannel.Web", "wwwroot", "images", "products"),
                Path.Combine(AppContext.BaseDirectory, "wwwroot", "images", "products")
            };

            return candidates.FirstOrDefault(Directory.Exists);
        }

        private static List<ProductImageSourceItem> LoadRegistryItemsFromDiskOrFallback()
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "product_image_registry.json"),
                Path.Combine(AppContext.BaseDirectory, "Data", "product_image_registry.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Omnichannel.Infrastructure", "Data", "product_image_registry.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "Omnichannel.Infrastructure", "Data", "product_image_registry.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "product_image_registry.json")
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        var json = File.ReadAllText(path);
                        var items = JsonSerializer.Deserialize<List<ProductImageSourceItem>>(json, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        if (items != null && items.Count > 0)
                        {
                            return items;
                        }
                    }
                    catch
                    {
                        // Fallback to curated in-code registry
                    }
                }
            }

            return GetCuratedRegistry();
        }

        public static List<ProductImageSourceItem> GetCuratedRegistry()
        {
            return new List<ProductImageSourceItem>
            {
                new()
                {
                    Sku = "SKU-SKIN-ANE-001",
                    Name = "Sữa Chống Nắng Kiềm Dầu Anessa Perfect UV Sunscreen Skincare Milk SPF50+ PA++++ 60ml",
                    Slug = "sua-chong-nang-anessa-perfect-uv-sunscreen-skincare-milk-spf50-60ml",
                    BrandName = "Anessa",
                    CategorySubfolder = "products/skincare",
                    FileName = "sua-chong-nang-anessa-perfect-uv-sunscreen-skincare-milk-spf50-60ml.jpg",
                    DirectImageUrl = "/images/products/skincare/sua-chong-nang-anessa-perfect-uv-sunscreen-skincare-milk-spf50-60ml.jpg",
                    AltText = "Sữa Chống Nắng Kiềm Dầu Anessa Perfect UV Sunscreen Skincare Milk SPF50+ PA++++ 60ml chính hãng"
                },
                new()
                {
                    Sku = "SKU-SKIN-LRP-002",
                    Name = "Serum Phục Hồi Tái Tạo Hàng Rào Ẩm La Roche-Posay Hyalu B5 Pure 30ml",
                    Slug = "serum-phuc-hoi-la-roche-posay-hyalu-b5-pure-30ml",
                    BrandName = "La Roche-Posay",
                    CategorySubfolder = "products/skincare",
                    FileName = "serum-phuc-hoi-la-roche-posay-hyalu-b5-pure-30ml.jpg",
                    DirectImageUrl = "/images/products/skincare/serum-phuc-hoi-la-roche-posay-hyalu-b5-pure-30ml.jpg",
                    AltText = "Serum Phục Hồi La Roche-Posay Hyalu B5 Pure 30ml chính hãng Pháp"
                },
                new()
                {
                    Sku = "SKU-SKIN-KLA-003",
                    Name = "Nước Hoa Hồng Cấp Ẩm Không Mùi Klairs Supple Preparation Unscented Toner 180ml",
                    Slug = "nuoc-hoa-hong-klairs-supple-preparation-unscented-toner-180ml",
                    BrandName = "Klairs",
                    CategorySubfolder = "products/skincare",
                    FileName = "nuoc-hoa-hong-klairs-supple-preparation-unscented-toner-180ml.webp",
                    DirectImageUrl = "/images/products/skincare/nuoc-hoa-hong-klairs-supple-preparation-unscented-toner-180ml.webp",
                    AltText = "Nước Hoa Hồng Cấp Ẩm Không Mùi Klairs Supple Preparation Unscented Toner 180ml"
                },
                new()
                {
                    Sku = "SKU-SKIN-CER-004",
                    Name = "Sữa Rửa Mặt Cho Da Thường Đến Dầu Mụn CeraVe Foaming Facial Cleanser 236ml",
                    Slug = "sua-rua-mat-cerave-foaming-facial-cleanser-236ml",
                    BrandName = "CeraVe",
                    CategorySubfolder = "products/skincare",
                    FileName = "sua-rua-mat-cerave-foaming-facial-cleanser-236ml.jpg",
                    DirectImageUrl = "/images/products/skincare/sua-rua-mat-cerave-foaming-facial-cleanser-236ml.jpg",
                    AltText = "Sữa Rửa Mặt CeraVe Foaming Facial Cleanser 236ml da dầu mụn"
                },
                new()
                {
                    Sku = "SKU-SKIN-EST-005",
                    Name = "Tinh Chất Chống Lão Hóa Phục Hồi Ban Đêm Estée Lauder Advanced Night Repair Synchronized Multi-Recovery 50ml",
                    Slug = "tinh-chat-estee-lauder-advanced-night-repair-50ml",
                    BrandName = "Estée Lauder",
                    CategorySubfolder = "products/skincare",
                    FileName = "tinh-chat-estee-lauder-advanced-night-repair-50ml.jpg",
                    DirectImageUrl = "/images/products/skincare/tinh-chat-estee-lauder-advanced-night-repair-50ml.jpg",
                    AltText = "Serum Estée Lauder Advanced Night Repair phục hồi ban đêm 50ml"
                },
                new()
                {
                    Sku = "SKU-SKIN-BIO-006",
                    Name = "Nước Tẩy Trang Dành Cho Da Nhạy Cảm Bioderma Sensibio H2O Micellar Water 500ml",
                    Slug = "nuoc-tay-trang-bioderma-sensibio-h2o-500ml",
                    BrandName = "Bioderma",
                    CategorySubfolder = "products/skincare",
                    FileName = "nuoc-tay-trang-bioderma-sensibio-h2o-500ml.png",
                    DirectImageUrl = "/images/products/skincare/nuoc-tay-trang-bioderma-sensibio-h2o-500ml.png",
                    AltText = "Nước Tẩy Trang Bioderma Sensibio H2O Micellar Water nắp hồng 500ml"
                },
                new()
                {
                    Sku = "SKU-SKIN-ORD-007",
                    Name = "Tinh Chất Thu Nhỏ Lỗ Chân Lông Kiềm Dầu The Ordinary Niacinamide 10% + Zinc 1% 30ml",
                    Slug = "tinh-chat-the-ordinary-niacinamide-10-zinc-1-30ml",
                    BrandName = "The Ordinary",
                    CategorySubfolder = "products/skincare",
                    FileName = "tinh-chat-the-ordinary-niacinamide-10-zinc-1-30ml.png",
                    DirectImageUrl = "/images/products/skincare/tinh-chat-the-ordinary-niacinamide-10-zinc-1-30ml.png",
                    AltText = "Serum The Ordinary Niacinamide 10% + Zinc 1% 30ml kiềm dầu thu nhỏ chân lông"
                },
                new()
                {
                    Sku = "SKU-SKIN-PAU-008",
                    Name = "Dung Dịch Tẩy Tế Bào Chết Hóa Học Paula's Choice Skin Perfecting 2% BHA Liquid Exfoliant 118ml",
                    Slug = "dung-dich-tay-te-bao-chet-paulas-choice-skin-perfecting-2-bha-118ml",
                    BrandName = "Paula's Choice",
                    CategorySubfolder = "products/skincare",
                    FileName = "dung-dich-tay-te-bao-chet-paulas-choice-skin-perfecting-2-bha-118ml.png",
                    DirectImageUrl = "/images/products/skincare/dung-dich-tay-te-bao-chet-paulas-choice-skin-perfecting-2-bha-118ml.png",
                    AltText = "Dung Dịch Tẩy Tế Bào Chết Paula's Choice Skin Perfecting 2% BHA Liquid 118ml"
                },
                new()
                {
                    Sku = "SKU-SKIN-HDL-009",
                    Name = "Dung Dịch Dưỡng Ẩm Chuyên Sâu Hada Labo Gokujyun Premium Lotion 170ml",
                    Slug = "dung-dich-duong-am-hada-labo-gokujyun-premium-lotion-170ml",
                    BrandName = "Hada Labo",
                    CategorySubfolder = "products/skincare",
                    FileName = "dung-dich-duong-am-hada-labo-gokujyun-premium-lotion-170ml.jpg",
                    DirectImageUrl = "/images/products/skincare/dung-dich-duong-am-hada-labo-gokujyun-premium-lotion-170ml.jpg",
                    AltText = "Dung Dịch Dưỡng Ẩm Hada Labo Gokujyun Premium Lotion 170ml"
                },
                new()
                {
                    Sku = "SKU-SKIN-LRP-010",
                    Name = "Kem Dưỡng Phục Hồi Làm Dịu Da Đa Năng La Roche-Posay Cicaplast Baume B5+ 40ml",
                    Slug = "kem-duong-phuc-hoi-la-roche-posay-cicaplast-baume-b5-plus-40ml",
                    BrandName = "La Roche-Posay",
                    CategorySubfolder = "products/skincare",
                    FileName = "kem-duong-phuc-hoi-la-roche-posay-cicaplast-baume-b5-plus-40ml.jpg",
                    DirectImageUrl = "/images/products/skincare/kem-duong-phuc-hoi-la-roche-posay-cicaplast-baume-b5-plus-40ml.jpg",
                    AltText = "Kem Dưỡng La Roche-Posay Cicaplast Baume B5+ phục hồi làm dịu da đa năng 40ml"
                },
                new()
                {
                    Sku = "SKU-LIP-MAC-031",
                    Name = "Son Thỏi Siêu Lì MAC Retro Matte Lipstick - #707 Ruby Woo 3g",
                    Slug = "son-thoi-mac-retro-matte-lipstick-707-ruby-woo-3g",
                    BrandName = "MAC",
                    CategorySubfolder = "products/makeup",
                    FileName = "son-thoi-mac-retro-matte-lipstick-707-ruby-woo-3g.jpg",
                    DirectImageUrl = "/images/products/makeup/son-thoi-mac-retro-matte-lipstick-707-ruby-woo-3g.jpg",
                    AltText = "Son Thỏi Lì MAC Retro Matte Lipstick Ruby Woo chính hãng"
                },
                new()
                {
                    Sku = "SKU-MAK-MAY-047",
                    Name = "Kem Lót Kiềm Dầu Che Mờ Lỗ Chân Lông Maybelline Baby Skin Pore Eraser 22ml",
                    Slug = "kem-lot-maybelline-baby-skin-pore-eraser-22ml",
                    BrandName = "Maybelline",
                    CategorySubfolder = "products/makeup",
                    FileName = "kem-lot-maybelline-baby-skin-pore-eraser-22ml.jpg",
                    DirectImageUrl = "/images/products/makeup/kem-lot-maybelline-baby-skin-pore-eraser-22ml.jpg",
                    AltText = "Kem Lót Kiềm Dầu Maybelline Baby Skin Pore Eraser 22ml"
                },
                new()
                {
                    Sku = "SKU-PERF-CHA-057",
                    Name = "Nước Hoa Nữ Quý Phái Chanel Coco Mademoiselle Eau De Parfum 100ml",
                    Slug = "nuoc-hoa-nu-chanel-coco-mademoiselle-edp-100ml",
                    BrandName = "Chanel",
                    CategorySubfolder = "products/perfume",
                    FileName = "nuoc-hoa-nu-chanel-coco-mademoiselle-edp-100ml.jpg",
                    DirectImageUrl = "/images/products/perfume/nuoc-hoa-nu-chanel-coco-mademoiselle-edp-100ml.jpg",
                    AltText = "Nước Hoa Nữ Chanel Coco Mademoiselle EDP 100ml"
                },
                new()
                {
                    Sku = "SKU-PERF-DIO-056",
                    Name = "Nước Hoa Nam Quyến Rũ Mạnh Mẽ Dior Sauvage Eau De Parfum 100ml",
                    Slug = "nuoc-hoa-nam-dior-sauvage-eau-de-parfum-100ml",
                    BrandName = "Dior",
                    CategorySubfolder = "products/perfume",
                    FileName = "nuoc-hoa-nam-dior-sauvage-eau-de-parfum-100ml.jpg",
                    DirectImageUrl = "/images/products/perfume/nuoc-hoa-nam-dior-sauvage-eau-de-parfum-100ml.jpg",
                    AltText = "Nước Hoa Nam Dior Sauvage EDP 100ml"
                },
                new()
                {
                    Sku = "SKU-HAIR-MOR-071",
                    Name = "Tinh Dầu Dưỡng Tóc Phục Hồi Hư Tổn Moroccanoil Treatment Original 100ml",
                    Slug = "tinh-dau-duong-toc-moroccanoil-treatment-original-100ml",
                    BrandName = "Moroccanoil",
                    CategorySubfolder = "products/hair-body",
                    FileName = "tinh-dau-duong-toc-moroccanoil-treatment-original-100ml.jpg",
                    DirectImageUrl = "/images/products/hair-body/tinh-dau-duong-toc-moroccanoil-treatment-original-100ml.jpg",
                    AltText = "Tinh Dầu Dưỡng Tóc Moroccanoil Treatment Original 100ml"
                }
            };
        }
    }
}
