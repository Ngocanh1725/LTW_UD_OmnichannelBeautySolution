using System;
using System.Collections.Generic;
using System.IO;
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
    }

    public static class ProductImageSourceRegistry
    {
        public static List<ProductImageSourceItem> GetAllItems()
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "product_image_registry.json"),
                Path.Combine(AppContext.BaseDirectory, "Data", "product_image_registry.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Omnichannel.Infrastructure", "Data", "product_image_registry.json"),
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
                        // Fallback to in-code registry
                    }
                }
            }

            return GetCuratedRegistry();
        }

        public static List<ProductImageSourceItem> GetCuratedRegistry()
        {
            return new List<ProductImageSourceItem>
            {
                // =========================================================================
                // NHÓM 1: TOP 10 SẢN PHẨM SKINCARE THỰC TẾ CHUẨN XÁC 100%
                // =========================================================================
                new()
                {
                    Sku = "SKU-SKIN-ANE-001",
                    Name = "Sữa Chống Nắng Kiềm Dầu Anessa Perfect UV Sunscreen Skincare Milk SPF50+ PA++++ 60ml",
                    Slug = "sua-chong-nang-anessa-perfect-uv-sunscreen-skincare-milk-spf50-60ml",
                    BrandName = "Anessa",
                    CategorySubfolder = "products/skincare",
                    FileName = "sua-chong-nang-anessa-perfect-uv-sunscreen-skincare-milk-spf50-60ml.jpg",
                    DirectImageUrl = "https://bizweb.dktcdn.net/thumb/1024x1024/100/525/087/products/anessa-6a8c873e-d033-413d-9639-e85776d0c805.jpg",
                    AltText = "Sữa Chống Nắng Kiềm Dầu Anessa Perfect UV Sunscreen Skincare Milk SPF50+ PA++++ 60ml chính hãng"
                },
                new()
                {
                    Sku = "SKU-SKIN-LRP-002",
                    Name = "Serum Phục Hồi Tái Tạo Hàng Rào Ẩm La Roche-Posay Hyalu B5 Pure 30ml",
                    Slug = "serum-phuc-hoi-la-roche-posay-hyalu-b5-pure-30ml",
                    BrandName = "La Roche-Posay",
                    CategorySubfolder = "products/skincare",
                    FileName = "serum-phuc-hoi-la-roche-posay-hyalu-b5-pure-30ml.webp",
                    DirectImageUrl = "https://cdn.nhathuoclongchau.com.vn/unsafe/2560x0/filters:quality(90):format(webp)/00021995_la_roche_posay_hyalu_b5_serum_30ml_mb049200_duong_chat_ho_tro_tai_tao_lan_da_giup_da_san_chac_va_d_3350_60b0_large_85574eba54.jpg",
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
                    DirectImageUrl = "https://klairsvietnam.vn/wp-content/uploads/2020/07/nuoc-hoa-hong-Klairs-Supple-Preparation-Unscented-Toner-180ml.webp",
                    AltText = "Nước Hoa Hồng Cấp Ẩm Không Mùi Klairs Supple Preparation Unscented Toner 180ml"
                },
                new()
                {
                    Sku = "SKU-SKIN-CER-004",
                    Name = "Sữa Rửa Mặt Cho Da Thường Đến Dầu Mụn CeraVe Foaming Facial Cleanser 236ml",
                    Slug = "sua-rua-mat-cerave-foaming-facial-cleanser-236ml",
                    BrandName = "CeraVe",
                    CategorySubfolder = "products/skincare",
                    FileName = "sua-rua-mat-cerave-foaming-facial-cleanser-236ml.webp",
                    DirectImageUrl = "https://cdn.nhathuoclongchau.com.vn/unsafe/2560x0/filters:quality(90):format(webp)/cerave_6f1f3f3ea7.jpg",
                    AltText = "Sữa Rửa Mặt CeraVe Foaming Facial Cleanser 236ml da dầu mụn"
                },
                new()
                {
                    Sku = "SKU-SKIN-EST-005",
                    Name = "Tinh Chất Chống Lão Hóa Phục Hồi Ban Đêm Estee Lauder Advanced Night Repair Synchronized Multi-Recovery 50ml",
                    Slug = "tinh-chat-estee-lauder-advanced-night-repair-50ml",
                    BrandName = "Estee Lauder",
                    CategorySubfolder = "products/skincare",
                    FileName = "tinh-chat-estee-lauder-advanced-night-repair-50ml.jpg",
                    DirectImageUrl = "https://bizweb.dktcdn.net/100/514/727/products/advanced-night-repair-synchronized-multi-recovery-complex-100ml-fullbox.jpg?v=1727755585427",
                    AltText = "Serum Estee Lauder Advanced Night Repair phục hồi ban đêm 50ml"
                },
                new()
                {
                    Sku = "SKU-SKIN-BIO-006",
                    Name = "Nước Tẩy Trang Dành Cho Da Nhạy Cảm Bioderma Sensibio H2O Micellar Water 500ml",
                    Slug = "nuoc-tay-trang-bioderma-sensibio-h2o-500ml",
                    BrandName = "Bioderma",
                    CategorySubfolder = "products/skincare",
                    FileName = "nuoc-tay-trang-bioderma-sensibio-h2o-500ml.webp",
                    DirectImageUrl = "https://cdn.nhathuoclongchau.com.vn/unsafe/2560x0/filters:quality(90):format(webp)/nuoc_tay_trang_cho_da_nhay_cam_bioderma_sensibio_h2o_500ml_00032012_1_d3a18bb58f.png",
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
                    DirectImageUrl = "https://product.hstatic.net/1000025647/product/the-ordinary-niacinamide-10-zinc-1_cf0ea884638c4e83917ce443ee6b9829.png",
                    AltText = "Serum The Ordinary Niacinamide 10% + Zinc 1% 30ml kiềm dầu thu nhỏ chân lông"
                },
                new()
                {
                    Sku = "SKU-SKIN-PAU-008",
                    Name = "Dung Dịch Tẩy Tế Bào Chết Hóa Học Paula's Choice Skin Perfecting 2% BHA Liquid Exfoliant 118ml",
                    Slug = "tay-te-bao-chet-paulas-choice-2-bha-liquid-118ml",
                    BrandName = "Paula's Choice",
                    CategorySubfolder = "products/skincare",
                    FileName = "tay-te-bao-chet-paulas-choice-2-bha-liquid-118ml.png",
                    DirectImageUrl = "https://www.paulaschoice.com/dw/image/v2/BBNX_PRD/on/demandware.static/-/Sites-pc-catalog/default/dw954917ff/images/products/2-percent-bha-liquid-exfoliant-2010-L.png?sw=1000&sfrm=png",
                    AltText = "Dung Dịch Tẩy Tế Bào Chết Paula's Choice Skin Perfecting 2% BHA Liquid 118ml"
                },
                new()
                {
                    Sku = "SKU-SKIN-HDL-009",
                    Name = "Dung Dịch Dưỡng Ẩm Chuyên Sâu Da Thường Hada Labo Advanced Nourish Lotion 170ml",
                    Slug = "dung-dich-duong-am-hada-labo-advanced-nourish-170ml",
                    BrandName = "Hada Labo",
                    CategorySubfolder = "products/skincare",
                    FileName = "dung-dich-duong-am-hada-labo-advanced-nourish-170ml.jpg",
                    DirectImageUrl = "https://bizweb.dktcdn.net/thumb/large/100/353/847/products/ava-san-pham-tren-website-15-cb6c9ca7-39f1-44db-b9c1-c0423ebed982.jpg?v=1732425295387",
                    AltText = "Dung Dịch Dưỡng Ẩm Hada Labo Advanced Nourish Hyaluron Lotion 170ml"
                },
                new()
                {
                    Sku = "SKU-SKIN-LRP-010",
                    Name = "Kem Dưỡng Phục Hồi Làm Dịu Da Đa Năng La Roche-Posay Cicaplast Baume B5+ 40ml",
                    Slug = "kem-duong-la-roche-posay-cicaplast-baume-b5-plus-40ml",
                    BrandName = "La Roche-Posay",
                    CategorySubfolder = "products/skincare",
                    FileName = "kem-duong-la-roche-posay-cicaplast-baume-b5-plus-40ml.webp",
                    DirectImageUrl = "https://cdn.nhathuoclongchau.com.vn/unsafe/2560x0/filters:quality(90):format(webp)/IMG_9629_8628daed64.jpg",
                    AltText = "Kem Dưỡng La Roche-Posay Cicaplast Baume B5+ phục hồi làm dịu da đa năng 40ml"
                },

                // =========================================================================
                // NHÓM 2: CÁC SẢN PHẨM TRANG ĐIỂM TIÊU BIỂU (MAKEUP)
                // =========================================================================
                new()
                {
                    Sku = "SKU-LIP-MAC-031",
                    Name = "Son Thỏi Lì Cổ Điển MAC Matte Lipstick - Ruby Woo (Đỏ Cổ Điển)",
                    Slug = "son-thoi-mac-matte-lipstick-ruby-woo",
                    BrandName = "MAC",
                    CategorySubfolder = "products/makeup",
                    FileName = "son-thoi-mac-matte-lipstick-ruby-woo.jpg",
                    DirectImageUrl = "https://bizweb.dktcdn.net/thumb/1024x1024/100/353/847/products/mac-ruby-woo.jpg",
                    AltText = "Son Thỏi Lì MAC Matte Lipstick Ruby Woo chính hãng"
                },
                new()
                {
                    Sku = "SKU-MAK-MAY-047",
                    Name = "Kem Nền Kiềm Dầu Chống Nắng Maybelline Fit Me Matte + Poreless Foundation 30ml",
                    Slug = "kem-nen-maybelline-fit-me-matte-poreless-foundation-30ml",
                    BrandName = "Maybelline",
                    CategorySubfolder = "products/makeup",
                    FileName = "kem-nen-maybelline-fit-me-matte-poreless-foundation-30ml.jpg",
                    DirectImageUrl = "https://bizweb.dktcdn.net/thumb/1024x1024/100/353/847/products/maybelline-fit-me.jpg",
                    AltText = "Kem Nền Kiềm Dầu Maybelline Fit Me Matte Poreless Foundation 30ml"
                },

                // =========================================================================
                // NHÓM 3: NƯỚC HOA CHÍNH HÃNG TIÊU BIỂU (PERFUME)
                // =========================================================================
                new()
                {
                    Sku = "SKU-PERF-CHA-058",
                    Name = "Nước Hoa Nữ Quý Phái Chanel Coco Mademoiselle Eau De Parfum 100ml",
                    Slug = "nuoc-hoa-nu-chanel-coco-mademoiselle-edp-100ml",
                    BrandName = "Chanel",
                    CategorySubfolder = "products/perfume",
                    FileName = "nuoc-hoa-nu-chanel-coco-mademoiselle-edp-100ml.jpg",
                    DirectImageUrl = "https://bizweb.dktcdn.net/thumb/1024x1024/100/353/847/products/chanel-coco-mademoiselle.jpg",
                    AltText = "Nước Hoa Nữ Chanel Coco Mademoiselle EDP 100ml"
                },
                new()
                {
                    Sku = "SKU-PERF-DIO-061",
                    Name = "Nước Hoa Nam Lịch Lãm Phóng Khoáng Dior Sauvage Eau De Toilette 100ml",
                    Slug = "nuoc-hoa-nam-dior-sauvage-eau-de-toilette-100ml",
                    BrandName = "Dior",
                    CategorySubfolder = "products/perfume",
                    FileName = "nuoc-hoa-nam-dior-sauvage-eau-de-toilette-100ml.jpg",
                    DirectImageUrl = "https://bizweb.dktcdn.net/thumb/1024x1024/100/353/847/products/dior-sauvage.jpg",
                    AltText = "Nước Hoa Nam Dior Sauvage EDT 100ml"
                },

                // =========================================================================
                // NHÓM 4: CHĂM SÓC TÓC & CƠ THỂ (HAIR & BODY)
                // =========================================================================
                new()
                {
                    Sku = "SKU-HAIR-MOR-070",
                    Name = "Dầu Dưỡng Tóc Phục Hồi Chuyên Sâu Moroccanoil Treatment Original 100ml",
                    Slug = "dau-duong-toc-moroccanoil-treatment-original-100ml",
                    BrandName = "Moroccanoil",
                    CategorySubfolder = "products/hair-body",
                    FileName = "dau-duong-toc-moroccanoil-treatment-original-100ml.jpg",
                    DirectImageUrl = "https://bizweb.dktcdn.net/thumb/1024x1024/100/353/847/products/moroccanoil-treatment.jpg",
                    AltText = "Dầu Dưỡng Tóc Moroccanoil Treatment Original 100ml"
                }
            };
        }
    }
}
