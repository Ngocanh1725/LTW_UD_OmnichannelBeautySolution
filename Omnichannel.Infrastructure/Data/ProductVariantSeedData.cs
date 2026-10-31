using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Domain.Entities;

namespace Omnichannel.Infrastructure.Data
{
    public static class ProductVariantSeedData
    {
        public static async Task SeedVariantsAndInventoryAsync(ApplicationDbContext context)
        {
            var now = DateTime.UtcNow;

            // =========================================================================
            // 1. SEED PRODUCT ATTRIBUTES & ATTRIBUTE VALUES
            // =========================================================================
            var attrColor = await context.ProductAttributes.FirstOrDefaultAsync(a => a.Code == "COLOR");
            if (attrColor == null)
            {
                attrColor = new ProductAttribute
                {
                    Name = "Màu Sắc",
                    Code = "COLOR",
                    DisplayType = "SWATCH",
                    Description = "Bảng màu son và tông trang điểm thực tế với mã màu Hex và ảnh Swatch",
                    SortOrder = 1,
                    IsActive = true
                };
                await context.ProductAttributes.AddAsync(attrColor);
                await context.SaveChangesAsync();
            }

            var attrVolume = await context.ProductAttributes.FirstOrDefaultAsync(a => a.Code == "VOLUME");
            if (attrVolume == null)
            {
                attrVolume = new ProductAttribute
                {
                    Name = "Dung Tích",
                    Code = "VOLUME",
                    DisplayType = "BUTTON",
                    Description = "Quy cách đóng gói dung tích thực tế (30ml, 50ml, 75ml, 100ml...)",
                    SortOrder = 2,
                    IsActive = true
                };
                await context.ProductAttributes.AddAsync(attrVolume);
                await context.SaveChangesAsync();
            }

            // Seed Attribute Values for COLOR
            var colorValues = new List<(string Value, string Hex, string Img)>
            {
                ("#Chili (Đỏ gạch)", "#A8332A", "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=600&h=600&fit=crop"),
                ("#Ruby Woo (Đỏ cổ điển)", "#9B111E", "https://images.unsplash.com/photo-1599733589046-10c005739ef9?w=600&h=600&fit=crop"),
                ("#Velvet Teddy (Nude hồng đất)", "#B87B72", "https://images.unsplash.com/photo-1625093742435-6fa192b6fb10?w=600&h=600&fit=crop"),
                ("#06 Figfig (Hồng sung)", "#B85A65", "https://images.unsplash.com/photo-1571781926291-c477ebfd024b?w=600&h=600&fit=crop"),
                ("#07 Jujube (Đỏ táo)", "#BA4538", "https://images.unsplash.com/photo-1617897903246-719242758050?w=600&h=600&fit=crop"),
                ("#20 Dark Coconut (Nâu dừa)", "#8B4538", "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=600&h=600&fit=crop")
            };

            foreach (var cv in colorValues)
            {
                if (!await context.ProductAttributeValues.AnyAsync(v => v.ProductAttributeId == attrColor.Id && v.Value == cv.Value))
                {
                    await context.ProductAttributeValues.AddAsync(new ProductAttributeValue
                    {
                        ProductAttributeId = attrColor.Id,
                        Value = cv.Value,
                        ColorHex = cv.Hex,
                        ImageUrl = cv.Img,
                        IsActive = true
                    });
                }
            }

            // Seed Attribute Values for VOLUME
            var volumeValues = new List<string> { "30ml", "35ml", "50ml", "75ml", "100ml" };
            foreach (var vol in volumeValues)
            {
                if (!await context.ProductAttributeValues.AnyAsync(v => v.ProductAttributeId == attrVolume.Id && v.Value == vol))
                {
                    await context.ProductAttributeValues.AddAsync(new ProductAttributeValue
                    {
                        ProductAttributeId = attrVolume.Id,
                        Value = vol,
                        IsActive = true
                    });
                }
            }
            await context.SaveChangesAsync();

            // =========================================================================
            // 2. ENSURE BRAND CLINIQUE & CLINIQUE PRODUCT
            // =========================================================================
            var brandClinique = await context.Brands.FirstOrDefaultAsync(b => b.Name.Contains("Clinique"));
            if (brandClinique == null)
            {
                brandClinique = new Brand
                {
                    Name = "Clinique",
                    Slug = "clinique",
                    CountryOfOrigin = "Mỹ",
                    LogoUrl = "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=200&h=200&fit=crop",
                    Description = "Thương hiệu dược mỹ phẩm cao cấp hàng đầu nước Mỹ, 100% không hương liệu và được kiểm nghiệm dị ứng nghiêm ngặt.",
                    IsFeatured = true,
                    SortOrder = 18
                };
                await context.Brands.AddAsync(brandClinique);
                await context.SaveChangesAsync();
            }

            var catMoisturizer = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "kem-duong-am")
                                 ?? await context.Categories.FirstOrDefaultAsync(c => c.Name.Contains("Dưỡng"));

            int catId = catMoisturizer?.Id ?? 17;

            var productClinique = await context.Products.FirstOrDefaultAsync(p => p.Sku == "SKU-SKIN-CLI-081");
            if (productClinique == null)
            {
                productClinique = new Product
                {
                    CategoryId = catId,
                    BrandId = brandClinique.Id,
                    Sku = "SKU-SKIN-CLI-081",
                    Barcode = "0020714902148",
                    Name = "Kem Dưỡng Ẩm Chuyên Sâu Clinique Moisture Surge 100H Auto-Replenishing Hydrator",
                    Slug = "kem-duong-am-chuyen-sau-clinique-moisture-surge-100h",
                    ShortDescription = "Công nghệ tự động phục hồi độ ẩm 100H, chứa men vi sinh lô hội độc quyền và Hyaluronic Acid đa tầng.",
                    Description = "Kem Dưỡng Ẩm Clinique Moisture Surge 100H Auto-Replenishing Hydrator là kiệt tác khóa ẩm dịu nhẹ cho mọi làn da. Kết cấu dạng gel-cream mát mẻ thâm nhập sâu qua hơn 10 tầng biểu bì da, cấp nước liên tục ngay cả sau khi đã rửa mặt. Công thức không chứa cồn, không paraben, an toàn tuyệt đối cho làn da nhạy cảm nhất.",
                    CostPrice = 450000m,
                    SellingPrice = 750000m,
                    DiscountPrice = 699000m,
                    ThumbnailUrl = "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=800&h=800&fit=crop",
                    ImagesJson = System.Text.Json.JsonSerializer.Serialize(new List<string>
                    {
                        "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=800&h=800&fit=crop",
                        "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?w=800&h=800&fit=crop",
                        "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&h=800&fit=crop"
                    }),
                    Unit = "Hũ",
                    ManufacturerName = "Clinique",
                    CountryOfOrigin = "Mỹ",
                    ImporterName = "Công ty TNHH Estée Lauder Việt Nam",
                    CosmeticNotificationNumber = "159821/21/CBMP-QLD",
                    ProductType = "STANDARD",
                    IsActive = true,
                    IsFeatured = true,
                    HasVariants = true
                };
                await context.Products.AddAsync(productClinique);
                await context.SaveChangesAsync();
            }

            // =========================================================================
            // 3. SEED 5 FOCUS PRODUCTS WITH DETAILED VARIANTS & INVENTORY STOCK
            // =========================================================================

            // Helper to get or link stores
            var storeShowroom = await context.Stores.FirstOrDefaultAsync(s => s.Code == "SH-BD" || s.Code == "SH-CG") ?? await context.Stores.FirstAsync();
            var storeHq = await context.Stores.FirstOrDefaultAsync(s => s.Code == "SH-Q1" || s.Code == "HQ-001") ?? await context.Stores.FirstAsync();
            var storeWarehouse = await context.Stores.FirstOrDefaultAsync(s => s.Code == "WH-MN" || s.Code == "SH-TD") ?? storeHq;

            // -------------------------------------------------------------------------
            // FOCUS PRODUCT 1: Son MAC Matte Lipstick (Color Swatches)
            // -------------------------------------------------------------------------
            var pMac = await context.Products.FirstOrDefaultAsync(p => p.Sku == "SKU-LIP-MAC-032" || p.Name.Contains("MAC Matte Lipstick"));
            if (pMac != null)
            {
                pMac.HasVariants = true;
                pMac.Name = "Son Thỏi Lì MAC Matte Lipstick 3g (Bảng Màu Đa Sắc Điêu Khắc)";
                pMac.ThumbnailUrl = "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=800&h=800&fit=crop";

                var macVariants = new List<(string Sku, string Barcode, string Name, string ColorName, string Hex, decimal Cost, decimal Price, decimal Adj, decimal W, int Stock, string SwatchImg, string OnSkinImg)>
                {
                    ("SKU-LIP-MAC-CHILI", "0773602567285", "Son MAC Matte - #Chili (Đỏ gạch thời thượng) 3g", "#Chili (Đỏ gạch)", "#A8332A", 420000m, 650000m, 0m, 35m, 120,
                     "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=600&h=600&fit=crop",
                     "https://images.unsplash.com/photo-1599733589046-10c005739ef9?w=600&h=600&fit=crop"),

                    ("SKU-LIP-MAC-RUBY", "0773602567292", "Son MAC Matte - #Ruby Woo (Đỏ cổ điển quý phái) 3g", "#Ruby Woo (Đỏ cổ điển)", "#9B111E", 420000m, 650000m, 0m, 35m, 140,
                     "https://images.unsplash.com/photo-1599733589046-10c005739ef9?w=600&h=600&fit=crop",
                     "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=600&h=600&fit=crop"),

                    ("SKU-LIP-MAC-VTEDDY", "0773602567308", "Son MAC Matte - #Velvet Teddy (Nude hồng đất thanh lịch) 3g", "#Velvet Teddy (Nude hồng đất)", "#B87B72", 420000m, 650000m, 0m, 35m, 95,
                     "https://images.unsplash.com/photo-1625093742435-6fa192b6fb10?w=600&h=600&fit=crop",
                     "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=600&h=600&fit=crop")
                };

                await SeedSingleProductVariantsAsync(context, pMac, macVariants, "COLOR", storeShowroom.Id, storeHq.Id);
            }

            // -------------------------------------------------------------------------
            // FOCUS PRODUCT 2: Son Rom&nd Juicy Lasting Tint (Color Swatches)
            // -------------------------------------------------------------------------
            var pRomand = await context.Products.FirstOrDefaultAsync(p => p.Sku == "SKU-LIP-ROM-035" || p.Sku == "AMO-ROM-JLT" || p.Name.Contains("Rom&nd Juicy"));
            if (pRomand != null)
            {
                pRomand.HasVariants = true;
                pRomand.Name = "Son Tint Bóng Mọng Nước Rom&nd Juicy Lasting Tint 5.5g (Bảng Màu Trái Cây)";
                pRomand.ThumbnailUrl = "https://images.unsplash.com/photo-1571781926291-c477ebfd024b?w=800&h=800&fit=crop";

                var romandVariants = new List<(string Sku, string Barcode, string Name, string ColorName, string Hex, decimal Cost, decimal Price, decimal Adj, decimal W, int Stock, string SwatchImg, string OnSkinImg)>
                {
                    ("SKU-LIP-ROM-JLT06", "8809647391852", "Son Tint Rom&nd Juicy Lasting - #06 Figfig (Hồng sung) 5.5g", "#06 Figfig (Hồng sung)", "#B85A65", 110000m, 199000m, 0m, 30m, 180,
                     "https://images.unsplash.com/photo-1571781926291-c477ebfd024b?w=600&h=600&fit=crop",
                     "https://images.unsplash.com/photo-1617897903246-719242758050?w=600&h=600&fit=crop"),

                    ("SKU-LIP-ROM-JLT07", "8809647391869", "Son Tint Rom&nd Juicy Lasting - #07 Jujube (Đỏ táo mọng) 5.5g", "#07 Jujube (Đỏ táo)", "#BA4538", 110000m, 199000m, 0m, 30m, 150,
                     "https://images.unsplash.com/photo-1617897903246-719242758050?w=600&h=600&fit=crop",
                     "https://images.unsplash.com/photo-1571781926291-c477ebfd024b?w=600&h=600&fit=crop"),

                    ("SKU-LIP-ROM-JLT20", "8809647391876", "Son Tint Rom&nd Juicy Lasting - #20 Dark Coconut (Nâu dừa ngọt ngào) 5.5g", "#20 Dark Coconut (Nâu dừa)", "#8B4538", 110000m, 199000m, 0m, 30m, 160,
                     "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=600&h=600&fit=crop",
                     "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=600&h=600&fit=crop")
                };

                await SeedSingleProductVariantsAsync(context, pRomand, romandVariants, "COLOR", storeShowroom.Id, storeHq.Id);
            }

            // -------------------------------------------------------------------------
            // FOCUS PRODUCT 3: Serum Estée Lauder ANR (Volume Sizes: 30ml, 50ml, 100ml)
            // -------------------------------------------------------------------------
            var pEstee = await context.Products.FirstOrDefaultAsync(p => p.Sku == "SKU-SKIN-EST-005" || p.Name.Contains("Estée Lauder Advanced Night Repair"));
            if (pEstee != null)
            {
                pEstee.HasVariants = true;
                pEstee.Name = "Tinh Chất Phục Hồi Ban Đêm Estée Lauder Advanced Night Repair Synchronized Multi-Recovery Complex";
                pEstee.CostPrice = 1300000m;
                pEstee.SellingPrice = 2100000m; // Base price for 30ml
                pEstee.ThumbnailUrl = "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=800&h=800&fit=crop";

                var esteeVariants = new List<(string Sku, string Barcode, string Name, string Volume, string Hex, decimal Cost, decimal Price, decimal Adj, decimal W, int Stock, string SwatchImg, string OnSkinImg)>
                {
                    ("SKU-SKIN-EST-ANR-30ML", "0887167485485", "Tinh Chất Estée Lauder ANR Phục Hồi Ban Đêm - 30ml", "30ml", "", 1300000m, 2100000m, 0m, 110m, 80,
                     "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=600&h=600&fit=crop",
                     "https://images.unsplash.com/photo-1608248597359-01c5185d992e?w=600&h=600&fit=crop"),

                    ("SKU-SKIN-EST-ANR-50ML", "0887167485492", "Tinh Chất Estée Lauder ANR Phục Hồi Ban Đêm - 50ml", "50ml", "", 2000000m, 3200000m, 1100000m, 160m, 120,
                     "https://images.unsplash.com/photo-1608248597359-01c5185d992e?w=600&h=600&fit=crop",
                     "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=600&h=600&fit=crop"),

                    ("SKU-SKIN-EST-ANR-100ML", "0887167485508", "Tinh Chất Estée Lauder ANR Phục Hồi Ban Đêm - 100ml (Bản Tiết Kiệm)", "100ml", "", 3100000m, 4900000m, 2800000m, 280m, 50,
                     "https://images.unsplash.com/photo-1601049541289-9b1b7bbbfe19?w=600&h=600&fit=crop",
                     "https://images.unsplash.com/photo-1608248597359-01c5185d992e?w=600&h=600&fit=crop")
                };

                await SeedSingleProductVariantsAsync(context, pEstee, esteeVariants, "VOLUME", storeShowroom.Id, storeHq.Id);
            }

            // -------------------------------------------------------------------------
            // FOCUS PRODUCT 4: Kem Dưỡng Ẩm Clinique Moisture Surge 100H (Volume Sizes: 30ml, 50ml, 75ml)
            // -------------------------------------------------------------------------
            if (productClinique != null)
            {
                var cliniqueVariants = new List<(string Sku, string Barcode, string Name, string Volume, string Hex, decimal Cost, decimal Price, decimal Adj, decimal W, int Stock, string SwatchImg, string OnSkinImg)>
                {
                    ("SKU-SKIN-CLI-MS-30ML", "0020714902148", "Kem Dưỡng Ẩm Clinique Moisture Surge 100H - 30ml", "30ml", "", 450000m, 750000m, 0m, 95m, 90,
                     "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=600&h=600&fit=crop",
                     "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?w=600&h=600&fit=crop"),

                    ("SKU-SKIN-CLI-MS-50ML", "0020714902155", "Kem Dưỡng Ẩm Clinique Moisture Surge 100H - 50ml", "50ml", "", 750000m, 1250000m, 500000m, 145m, 110,
                     "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?w=600&h=600&fit=crop",
                     "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=600&h=600&fit=crop"),

                    ("SKU-SKIN-CLI-MS-75ML", "0020714902162", "Kem Dưỡng Ẩm Clinique Moisture Surge 100H - 75ml (Siêu Tiết Kiệm)", "75ml", "", 1050000m, 1750000m, 1000000m, 210m, 60,
                     "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=600&h=600&fit=crop",
                     "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?w=600&h=600&fit=crop")
                };

                await SeedSingleProductVariantsAsync(context, productClinique, cliniqueVariants, "VOLUME", storeShowroom.Id, storeHq.Id);
            }

            // -------------------------------------------------------------------------
            // FOCUS PRODUCT 5: Nước Hoa Chanel Coco Mademoiselle EDP (Volume Sizes: 35ml, 50ml, 100ml)
            // -------------------------------------------------------------------------
            var pChanel = await context.Products.FirstOrDefaultAsync(p => p.Sku == "SKU-PERF-CHA-057" || p.Name.Contains("Chanel Coco Mademoiselle"));
            if (pChanel != null)
            {
                pChanel.HasVariants = true;
                pChanel.Name = "Nước Hoa Nữ Quý Tộc Chanel Coco Mademoiselle Eau De Parfum";
                pChanel.CostPrice = 1650000m;
                pChanel.SellingPrice = 2650000m; // Base price for 35ml
                pChanel.ThumbnailUrl = "https://images.unsplash.com/photo-1541643600914-78b084683601?w=800&h=800&fit=crop";

                var chanelVariants = new List<(string Sku, string Barcode, string Name, string Volume, string Hex, decimal Cost, decimal Price, decimal Adj, decimal W, int Stock, string SwatchImg, string OnSkinImg)>
                {
                    ("SKU-PERF-CHA-COCO-35ML", "3145891165104", "Nước Hoa Chanel Coco Mademoiselle EDP - 35ml", "35ml", "", 1650000m, 2650000m, 0m, 180m, 45,
                     "https://images.unsplash.com/photo-1541643600914-78b084683601?w=600&h=600&fit=crop",
                     "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=600&h=600&fit=crop"),

                    ("SKU-PERF-CHA-COCO-50ML", "3145891165159", "Nước Hoa Chanel Coco Mademoiselle EDP - 50ml", "50ml", "", 2350000m, 3650000m, 1000000m, 250m, 70,
                     "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=600&h=600&fit=crop",
                     "https://images.unsplash.com/photo-1541643600914-78b084683601?w=600&h=600&fit=crop"),

                    ("SKU-PERF-CHA-COCO-100ML", "3145891165203", "Nước Hoa Chanel Coco Mademoiselle EDP - 100ml", "100ml", "", 3250000m, 4550000m, 1900000m, 420m, 55,
                     "https://images.unsplash.com/photo-1523293182086-7651a899d37f?w=600&h=600&fit=crop",
                     "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=600&h=600&fit=crop")
                };

                await SeedSingleProductVariantsAsync(context, pChanel, chanelVariants, "VOLUME", storeShowroom.Id, storeHq.Id);
            }

            await context.SaveChangesAsync();
        }

        private static async Task SeedSingleProductVariantsAsync(
            ApplicationDbContext context,
            Product product,
            List<(string Sku, string Barcode, string Name, string OptionValue, string Hex, decimal Cost, decimal Price, decimal Adj, decimal W, int Stock, string SwatchImg, string OnSkinImg)> variants,
            string attributeType,
            int storeShowroomId,
            int storeHqId)
        {
            var now = DateTime.UtcNow;
            var warehouseStore = await context.Stores.FirstOrDefaultAsync(s => s.Code == "WH-MN" || s.Id == 3);
            int storeWarehouseId = warehouseStore?.Id ?? storeHqId;
            int sort = 1;

            foreach (var item in variants)
            {
                var variant = await context.ProductVariants.FirstOrDefaultAsync(v => v.Sku == item.Sku);
                if (variant == null)
                {
                    variant = new ProductVariant
                    {
                        ProductId = product.Id,
                        Sku = item.Sku,
                        Barcode = item.Barcode,
                        VariantName = item.Name,
                        CostPrice = item.Cost,
                        SellingPrice = item.Price,
                        PriceAdjustment = item.Adj,
                        Weight = item.W,
                        WeightGrams = item.W,
                        StockQuantity = item.Stock,
                        SwatchImageUrl = item.SwatchImg,
                        SortOrder = sort++,
                        IsActive = true,
                        CreatedAt = now
                    };

                    if (attributeType == "COLOR")
                    {
                        variant.ColorName = item.OptionValue;
                        variant.ColorHex = item.Hex;
                        variant.SwatchColorHex = item.Hex;
                        variant.Volume = product.Unit;
                    }
                    else
                    {
                        variant.Volume = item.OptionValue;
                    }

                    await context.ProductVariants.AddAsync(variant);
                    await context.SaveChangesAsync(); // save to generate variant.Id
                }
                else
                {
                    variant.VariantName = item.Name;
                    variant.CostPrice = item.Cost;
                    variant.SellingPrice = item.Price;
                    variant.PriceAdjustment = item.Adj;
                    variant.Weight = item.W;
                    variant.WeightGrams = item.W;
                    variant.StockQuantity = item.Stock;
                    variant.SwatchImageUrl = item.SwatchImg;
                    variant.IsActive = true;

                    if (attributeType == "COLOR")
                    {
                        variant.ColorName = item.OptionValue;
                        variant.ColorHex = item.Hex;
                        variant.SwatchColorHex = item.Hex;
                    }
                    else
                    {
                        variant.Volume = item.OptionValue;
                    }
                    await context.SaveChangesAsync();
                }

                // 2. Add Swatches if color variant
                if (attributeType == "COLOR" && !string.IsNullOrEmpty(item.SwatchImg))
                {
                    var swatch = await context.ProductSwatches.FirstOrDefaultAsync(s => s.ProductVariantId == variant.Id);
                    if (swatch == null)
                    {
                        swatch = new ProductSwatch
                        {
                            ProductVariantId = variant.Id,
                            SwatchImageUrl = item.SwatchImg,
                            SwatchOnSkinImageUrl = item.OnSkinImg,
                            SortOrder = 1
                        };
                        await context.ProductSwatches.AddAsync(swatch);
                    }
                    else
                    {
                        swatch.SwatchImageUrl = item.SwatchImg;
                        swatch.SwatchOnSkinImageUrl = item.OnSkinImg;
                    }
                }

                // 3. Link Attribute Value mapping
                var attrVal = await context.ProductAttributeValues.FirstOrDefaultAsync(v => v.Value == item.OptionValue);
                if (attrVal != null)
                {
                    if (!await context.ProductVariantAttributeValues.AnyAsync(pvav => pvav.ProductVariantId == variant.Id && pvav.ProductAttributeValueId == attrVal.Id))
                    {
                        await context.ProductVariantAttributeValues.AddAsync(new ProductVariantAttributeValue
                        {
                            ProductVariantId = variant.Id,
                            ProductAttributeValueId = attrVal.Id
                        });
                    }
                }

                // 4. Create FEFO Batches & Store Inventories for each variant
                // Batch 1: FEFO near-date (còn 38 ngày)
                var batchNearCode = $"LOT-{variant.Sku}-24FEFO";
                var batchNear = await context.ProductBatches.FirstOrDefaultAsync(b => b.BatchNumber == batchNearCode);
                if (batchNear == null)
                {
                    batchNear = new ProductBatch
                    {
                        ProductId = product.Id,
                        ProductVariantId = variant.Id,
                        BatchNumber = batchNearCode,
                        ManufacturingDate = now.AddMonths(-18),
                        ExpDate = now.AddDays(115),
                        ImportCost = item.Cost * 0.9m,
                        InitialQuantity = 40,
                        CurrentQuantity = 30,
                        Status = "NEAR_EXPIRATION",
                        AlertDaysThreshold = 90,
                        ClearanceDiscountPercent = 50.0m,
                        CreatedAt = now.AddMonths(-18)
                    };
                    await context.ProductBatches.AddAsync(batchNear);
                    await context.SaveChangesAsync();

                    // Showroom Ba Đình inventory: 10
                    await context.StoreInventories.AddAsync(new StoreInventory
                    {
                        StoreId = storeShowroomId,
                        ProductBatchId = batchNear.Id,
                        PhysicalQuantity = 10,
                        ReservedQuantity = 1,
                        LastUpdated = now
                    });

                    // Flagship Q1 inventory: 10
                    await context.StoreInventories.AddAsync(new StoreInventory
                    {
                        StoreId = storeHqId,
                        ProductBatchId = batchNear.Id,
                        PhysicalQuantity = 10,
                        ReservedQuantity = 0,
                        LastUpdated = now
                    });

                    // Tổng Kho Miền Nam inventory: 10
                    await context.StoreInventories.AddAsync(new StoreInventory
                    {
                        StoreId = storeWarehouseId,
                        ProductBatchId = batchNear.Id,
                        PhysicalQuantity = 10,
                        ReservedQuantity = 0,
                        LastUpdated = now
                    });
                }

                // Batch 2: Fresh batch date xa (>2.5 năm)
                var batchFarCode = $"LOT-{variant.Sku}-26FRESH";
                var batchFar = await context.ProductBatches.FirstOrDefaultAsync(b => b.BatchNumber == batchFarCode);
                if (batchFar == null)
                {
                    int farQty = item.Stock - 30;
                    if (farQty < 20) farQty = 50;

                    batchFar = new ProductBatch
                    {
                        ProductId = product.Id,
                        ProductVariantId = variant.Id,
                        BatchNumber = batchFarCode,
                        ManufacturingDate = now.AddMonths(-2),
                        ExpDate = now.AddMonths(34),
                        ImportCost = item.Cost,
                        InitialQuantity = farQty + 20,
                        CurrentQuantity = farQty,
                        Status = "ACTIVE",
                        AlertDaysThreshold = 90,
                        ClearanceDiscountPercent = null,
                        CreatedAt = now.AddMonths(-2)
                    };
                    await context.ProductBatches.AddAsync(batchFar);
                    await context.SaveChangesAsync();

                    int showroomShare = (int)(farQty * 0.25);
                    int hqShare = (int)(farQty * 0.35);
                    int whShare = farQty - showroomShare - hqShare;

                    await context.StoreInventories.AddAsync(new StoreInventory
                    {
                        StoreId = storeShowroomId,
                        ProductBatchId = batchFar.Id,
                        PhysicalQuantity = showroomShare,
                        ReservedQuantity = 2,
                        LastUpdated = now
                    });

                    await context.StoreInventories.AddAsync(new StoreInventory
                    {
                        StoreId = storeHqId,
                        ProductBatchId = batchFar.Id,
                        PhysicalQuantity = hqShare,
                        ReservedQuantity = 0,
                        LastUpdated = now
                    });

                    await context.StoreInventories.AddAsync(new StoreInventory
                    {
                        StoreId = storeWarehouseId,
                        ProductBatchId = batchFar.Id,
                        PhysicalQuantity = whShare,
                        ReservedQuantity = 0,
                        LastUpdated = now
                    });
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
