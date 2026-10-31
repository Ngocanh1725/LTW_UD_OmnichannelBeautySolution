using System.Collections.Generic;

namespace Omnichannel.Infrastructure.Data
{
    public class CosmeticProductSeedDto
    {
        public string Sku { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string CatSlug { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string CountryOfOrigin { get; set; } = string.Empty;
        public string Unit { get; set; } = "Chai";
        public string Volume { get; set; } = string.Empty;
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string ShortDescription { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public List<string> Images { get; set; } = new();
        public List<string> SkinTypeSlugs { get; set; } = new();
        public List<(string IngredientSlug, string Concentration)> Ingredients { get; set; } = new();
        public bool HasVariants { get; set; } = false;
        public bool IsFeatured { get; set; } = false;
        public string CosmeticNotificationNumber { get; set; } = string.Empty;
        public string ImporterName { get; set; } = string.Empty;
    }

    public static class ProductCatalogData
    {
        public static List<CosmeticProductSeedDto> Get80CosmeticProducts()
        {
            return new List<CosmeticProductSeedDto>
            {
                // =========================================================================
                // I. NHÓM SKINCARE CHĂM SÓC DA (30 SẢN PHẨM THỰC TẾ)
                // =========================================================================
                new()
                {
                    Sku = "SKU-SKIN-ANE-001",
                    Barcode = "8935001202001",
                    Name = "Sữa Chống Nắng Kiềm Dầu Anessa Perfect UV Sunscreen Skincare Milk SPF50+ PA++++ 60ml",
                    Slug = "sua-chong-nang-anessa-perfect-uv-sunscreen-skincare-milk-spf50-60ml",
                    CatSlug = "kem-chong-nang",
                    BrandName = "Anessa",
                    CountryOfOrigin = "Nhật Bản",
                    Unit = "Chai",
                    Volume = "60ml",
                    CostPrice = 420000m,
                    SellingPrice = 715000m,
                    DiscountPrice = 629000m,
                    ShortDescription = "Công nghệ Auto Booster chống trôi nhiệt ẩm vượt trội, kiềm dầu khô thoáng suốt 8h.",
                    Description = "Sữa Chống Nắng Anessa Perfect UV Sunscreen Skincare Milk SPF50+ PA++++ 60ml là kem chống nắng số 1 Nhật Bản suốt 21 năm liên tiếp. Tích hợp công nghệ Auto Booster giúp màng lọc UV càng bền vững khi tiếp xúc với nhiệt độ, mồ hôi và nước. Bổ sung 50% thành phần dưỡng da gồm chiết xuất trà xanh, hoa hồng Tormentilla và Hyaluronic Acid bảo vệ da không bị khô ráp, lão hóa sớm.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1556228722-d0b5d92fe77c?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1617897903246-719242758050?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-dau", "da-hon-hop", "da-thuong" },
                    Ingredients = new() { ("hyaluronic-acid", "1%"), ("collagen-thuy-phan", "0.5%"), ("tea-tree-tram-tra", "0.2%") },
                    IsFeatured = true,
                    CosmeticNotificationNumber = "182940/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH Mỹ Phẩm Shiseido Việt Nam"
                },
                new()
                {
                    Sku = "SKU-SKIN-LRP-002",
                    Barcode = "8935001202002",
                    Name = "Serum Phục Hồi Tái Tạo Hàng Rào Ẩm La Roche-Posay Hyalu B5 Pure 30ml",
                    Slug = "serum-phuc-hoi-la-roche-posay-hyalu-b5-pure-30ml",
                    CatSlug = "serum-tinh-chat",
                    BrandName = "La Roche-Posay",
                    CountryOfOrigin = "Pháp",
                    Unit = "Chai",
                    Volume = "30ml",
                    CostPrice = 680000m,
                    SellingPrice = 1050000m,
                    DiscountPrice = 920000m,
                    ShortDescription = "Chứa Panthenol B5 5% và HA phân tử kép làm dịu và phục hồi da nhạy cảm kích ứng sau 1 giờ.",
                    Description = "Tinh chất La Roche-Posay Hyalu B5 là giải pháp hàng đầu cho làn da thiếu nước, mệt mỏi và lão hóa. Với phức hợp độc quyền 2 loại Hyaluronic Acid nguyên chất cùng Vitamin B5 và Madecassoside (Centella Asiatica), sản phẩm giúp kích thích tái tạo biểu bì, làm đầy các rãnh nhăn nông và củng cố hàng rào bảo vệ tự nhiên của da.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1608248597359-009848520330?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-nhay-cam", "da-kho", "da-thuong" },
                    Ingredients = new() { ("panthenol-vitamin-b5", "5%"), ("hyaluronic-acid", "1.5%"), ("centella-asiatica", "0.5%") },
                    IsFeatured = true,
                    CosmeticNotificationNumber = "154092/21/CBMP-QLD",
                    ImporterName = "Công ty TNHH L'Oréal Việt Nam"
                },
                new()
                {
                    Sku = "SKU-SKIN-KLA-003",
                    Barcode = "8935001202003",
                    Name = "Nước Hoa Hồng Cấp Ẩm Không Mùi Klairs Supple Preparation Unscented Toner 180ml",
                    Slug = "nuoc-hoa-hong-klairs-supple-preparation-unscented-toner-180ml",
                    CatSlug = "toner-nuoc-hoa-hong",
                    BrandName = "Torriden",
                    CountryOfOrigin = "Hàn Quốc",
                    Unit = "Chai",
                    Volume = "180ml",
                    CostPrice = 240000m,
                    SellingPrice = 395000m,
                    DiscountPrice = 345000m,
                    ShortDescription = "Cân bằng độ pH 5.0, làm dịu mẩn đỏ và cấp ẩm sâu với chiết xuất rau má và HA.",
                    Description = "Klairs Supple Preparation Unscented Toner là phiên bản không chứa tinh dầu thơm nhân tạo, thiết kế an toàn tuyệt đối cho làn da siêu nhạy cảm. Công thức chứa Phyto-Oligo, Sodium Hyaluronate và Centella Asiatica cấp ẩm tức thì, làm dịu tình trạng kích ứng và giúp các bước dưỡng tiếp theo thẩm thấu tối đa.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1608248597359-009848520330?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1608248597359-009848520330?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-nhay-cam", "da-kho", "da-thuong", "da-hon-hop" },
                    Ingredients = new() { ("hyaluronic-acid", "1%"), ("centella-asiatica", "1%"), ("panthenol-vitamin-b5", "0.5%") },
                    IsFeatured = false,
                    CosmeticNotificationNumber = "120394/20/CBMP-QLD",
                    ImporterName = "Công ty CP Mỹ Phẩm Quốc Tế Thùy Dung"
                },
                new()
                {
                    Sku = "SKU-SKIN-CER-004",
                    Barcode = "8935001202004",
                    Name = "Sữa Rửa Mặt Cho Da Thường Đến Dầu Mụn CeraVe Foaming Facial Cleanser 236ml",
                    Slug = "sua-rua-mat-cerave-foaming-facial-cleanser-236ml",
                    CatSlug = "sua-rua-mat",
                    BrandName = "CeraVe",
                    CountryOfOrigin = "Mỹ",
                    Unit = "Chai",
                    Volume = "236ml",
                    CostPrice = 220000m,
                    SellingPrice = 380000m,
                    DiscountPrice = 330000m,
                    ShortDescription = "Phức hợp 3 Ceramide thiết yếu và Niacinamide làm sạch sâu bã nhờn không gây khô rát.",
                    Description = "CeraVe Foaming Cleanser được phát triển bởi các chuyên gia da liễu hàng đầu Hoa Kỳ, tạo lớp bọt mịn dịu nhẹ cuốn trôi bụi bẩn, dầu thừa mà không làm tổn hại đến lớp màng hydrolipid bảo vệ da. Chứa 3 loại Ceramide thiết yếu (1, 3, 6-II), Niacinamide kiềm dầu và Hyaluronic Acid giữ ẩm.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1556228722-d0b5d92fe77c?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1556228722-d0b5d92fe77c?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-dau", "da-hon-hop" },
                    Ingredients = new() { ("ceramide", "1%"), ("niacinamide", "2%"), ("hyaluronic-acid", "0.5%") },
                    IsFeatured = true,
                    CosmeticNotificationNumber = "168920/21/CBMP-QLD",
                    ImporterName = "Công ty TNHH L'Oréal Việt Nam"
                },
                new()
                {
                    Sku = "SKU-SKIN-EST-005",
                    Barcode = "8935001202005",
                    Name = "Tinh Chất Phục Hồi Ban Đêm Estée Lauder Advanced Night Repair Synchronized 50ml",
                    Slug = "tinh-chat-estee-lauder-advanced-night-repair-50ml",
                    CatSlug = "serum-tinh-chat",
                    BrandName = "Estée Lauder",
                    CountryOfOrigin = "Mỹ",
                    Unit = "Chai",
                    Volume = "50ml",
                    CostPrice = 2100000m,
                    SellingPrice = 3650000m,
                    DiscountPrice = 3290000m,
                    ShortDescription = "Công nghệ Chronolux Power Signal phục hồi DNA tế bào, giảm nếp nhăn và săn chắc da thần tốc.",
                    Description = "Chai serum nâu huyền thoại thế giới với hơn 70 bằng sáng chế toàn cầu. Estée Lauder Advanced Night Repair đánh thức khả năng tự tái tạo tự nhiên của da trong giấc ngủ ban đêm, tăng sinh collagen, chống oxy hóa liên tục 8 tiếng và khóa ẩm sâu suốt 72 giờ cho làn da trẻ trung rạng ngời vào sáng hôm sau.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1512290900672-1f02e6a0d33e?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-kho", "da-nhay-cam", "da-thuong", "da-hon-hop" },
                    Ingredients = new() { ("peptides", "2%"), ("hyaluronic-acid", "2%"), ("panthenol-vitamin-b5", "1%") },
                    IsFeatured = true,
                    CosmeticNotificationNumber = "179420/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH Estée Lauder Việt Nam"
                },
                new()
                {
                    Sku = "SKU-SKIN-BIO-006",
                    Barcode = "8935001202006",
                    Name = "Nước Tẩy Trang Làm Sạch Dịu Nhẹ Bioderma Sensibio H2O Micellar Water 500ml",
                    Slug = "nuoc-tay-trang-bioderma-sensibio-h2o-500ml",
                    CatSlug = "nuoc-tay-trang",
                    BrandName = "Bioderma",
                    CountryOfOrigin = "Pháp",
                    Unit = "Chai",
                    Volume = "500ml",
                    CostPrice = 280000m,
                    SellingPrice = 495000m,
                    DiscountPrice = 425000m,
                    ShortDescription = "Hạt micelle mô phỏng sinh học hút sạch 99% bụi mịn PM 2.5 và lớp makeup cứng đầu.",
                    Description = "Bioderma Sensibio H2O là chai nước tẩy trang nắp hồng kinh điển cứ mỗi 2 giây lại có 1 chai được bán ra trên thế giới. Với độ pH 5.5 sinh lý cân bằng sinh thái làn da, chiết xuất dưa leo làm dịu và công thức không xà phòng, không cồn, không paraben, an toàn tuyệt đối ngay cả cho vùng da mắt môi mỏng manh.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1556228722-d0b5d92fe77c?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-nhay-cam", "da-thuong", "da-kho" },
                    Ingredients = new() { ("centella-asiatica", "0.5%"), ("panthenol-vitamin-b5", "0.5%") },
                    IsFeatured = true,
                    CosmeticNotificationNumber = "143091/21/CBMP-QLD",
                    ImporterName = "Công ty CP Thương Mại Việt Nữ"
                },
                new()
                {
                    Sku = "SKU-SKIN-ORD-007",
                    Barcode = "8935001202007",
                    Name = "Tinh Chất Giảm Dầu Mụn & Mờ Thâm The Ordinary Niacinamide 10% + Zinc 1% 30ml",
                    Slug = "tinh-chat-the-ordinary-niacinamide-10-zinc-1-30ml",
                    CatSlug = "serum-tinh-chat",
                    BrandName = "The Ordinary",
                    CountryOfOrigin = "Canada",
                    Unit = "Chai",
                    Volume = "30ml",
                    CostPrice = 140000m,
                    SellingPrice = 245000m,
                    DiscountPrice = 215000m,
                    ShortDescription = "Nồng độ Niacinamide 10% kết hợp Kẽm 1% thu nhỏ lỗ chân lông và kiểm soát bã nhờn.",
                    Description = "The Ordinary Niacinamide 10% + Zinc 1% là giải pháp lý tưởng giúp cải thiện tình trạng lỗ chân lông to, bít tắc tuyến bã nhờn và thâm sau mụn. Niacinamide tăng cường tổng hợp Ceramide củng cố màng lipid, trong khi muối kẽm của axit carboxylic pyrrolidone giúp kiểm soát sự bài tiết bã nhờn hiệu quả.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1608248597359-009848520330?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-dau", "da-hon-hop" },
                    Ingredients = new() { ("niacinamide", "10%") },
                    IsFeatured = false,
                    CosmeticNotificationNumber = "156094/21/CBMP-QLD",
                    ImporterName = "Công ty TNHH Deciem Châu Á"
                },
                new()
                {
                    Sku = "SKU-SKIN-PAU-008",
                    Barcode = "8935001202008",
                    Name = "Dung Dịch Tẩy Tế Bào Chết Paula's Choice Skin Perfecting 2% BHA Liquid 118ml",
                    Slug = "dung-dich-tay-te-bao-chet-paulas-choice-skin-perfecting-2-bha-118ml",
                    CatSlug = "serum-tinh-chat",
                    BrandName = "Paula's Choice",
                    CountryOfOrigin = "Mỹ",
                    Unit = "Chai",
                    Volume = "118ml",
                    CostPrice = 620000m,
                    SellingPrice = 949000m,
                    DiscountPrice = 829000m,
                    ShortDescription = "Axit Salicylic 2% tan trong dầu, đẩy mụn ẩn và làm sạch sâu bã nhờn cứng đầu.",
                    Description = "Sản phẩm biểu tượng bán chạy nhất toàn cầu của Paula's Choice. Với công thức 2% BHA (Salicylic Acid) tan trong dầu tối ưu, sản phẩm thẩm thấu sâu vào từng lỗ chân lông làm sạch hoàn toàn bã nhờn, tế bào chết tích tụ và cặn mỹ phẩm, giải quyết triệt để mụn đầu đen, mụn cám và làm đều màu da rõ rệt.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1608248597359-009848520330?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1608248597359-009848520330?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-dau", "da-hon-hop" },
                    Ingredients = new() { ("bha-salicylic-acid", "2%"), ("tea-tree-tram-tra", "0.5%") },
                    IsFeatured = true,
                    CosmeticNotificationNumber = "142095/20/CBMP-QLD",
                    ImporterName = "Công ty CP Bệnh Viện Hữu Nghị Quốc Tế Hà Nội"
                },
                new()
                {
                    Sku = "SKU-SKIN-HDL-009",
                    Barcode = "8935001202009",
                    Name = "Dung Dịch Dưỡng Ẩm Chuyên Sâu Hada Labo Gokujyun Premium Lotion 170ml",
                    Slug = "dung-dich-duong-am-hada-labo-gokujyun-premium-lotion-170ml",
                    CatSlug = "toner-nuoc-hoa-hong",
                    BrandName = "Hada Labo",
                    CountryOfOrigin = "Nhật Bản",
                    Unit = "Chai",
                    Volume = "170ml",
                    CostPrice = 180000m,
                    SellingPrice = 295000m,
                    DiscountPrice = 255000m,
                    ShortDescription = "Chứa 7 loại Hyaluronic Acid đa phân tử cấp nước sâu từ thượng bì tới hạ bì.",
                    Description = "Hada Labo Gokujyun Premium Lotion chai vàng sang trọng là tuyệt tác cấp ẩm của tập đoàn Rohto Mentholatum. 7 loại Hyaluronic Acid gồm HA thường, Super HA, Nano HA, 3D HA, Adsorption HA, Penetration HA và Fermented HA tạo thành mạng lưới giữ nước bền vững suốt 24 giờ cho làn da căng mướt ngậm nước.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1556228722-d0b5d92fe77c?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1556228722-d0b5d92fe77c?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1608248597359-009848520330?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-kho", "da-thuong", "da-nhay-cam" },
                    Ingredients = new() { ("hyaluronic-acid", "3%"), ("ceramide", "0.5%") },
                    IsFeatured = false,
                    CosmeticNotificationNumber = "139042/20/CBMP-QLD",
                    ImporterName = "Công ty TNHH Rohto-Mentholatum Việt Nam"
                },
                new()
                {
                    Sku = "SKU-SKIN-LRP-010",
                    Barcode = "8935001202010",
                    Name = "Kem Dưỡng Phục Hồi Làm Dịu Da La Roche-Posay Cicaplast Baume B5+ 40ml",
                    Slug = "kem-duong-phuc-hoi-la-roche-posay-cicaplast-baume-b5-plus-40ml",
                    CatSlug = "kem-duong-am",
                    BrandName = "La Roche-Posay",
                    CountryOfOrigin = "Pháp",
                    Unit = "Tuýp",
                    Volume = "40ml",
                    CostPrice = 240000m,
                    SellingPrice = 415000m,
                    DiscountPrice = 365000m,
                    ShortDescription = "Tribioma cân bằng hệ vi sinh da, kết hợp Panthenol 5% và Madecassoside giảm đỏ rát tức thì.",
                    Description = "La Roche-Posay Cicaplast Baume B5+ là phiên bản nâng cấp đột phá với phức hợp lợi khuẩn Tribioma độc quyền giúp tái cân bằng hệ vi sinh vật trên da, phục hồi màng ẩm tổn thương sau liệu trình laser, peel hoặc kích ứng mỹ phẩm. Chứa 5% Panthenol, Madecassoside và bơ hạt mỡ dưỡng ẩm tối ưu.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-nhay-cam", "da-kho" },
                    Ingredients = new() { ("panthenol-vitamin-b5", "5%"), ("centella-asiatica", "0.5%"), ("ceramide", "0.5%") },
                    IsFeatured = true,
                    CosmeticNotificationNumber = "192045/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH L'Oréal Việt Nam"
                },
                new()
                {
                    Sku = "SKU-SKIN-SKI-011",
                    Barcode = "8935001202011",
                    Name = "Nước Thần Trẻ Hóa Tái Tạo Làn Da Pha Lê SK-II Facial Treatment Essence 230ml",
                    Slug = "nuoc-than-sk-ii-facial-treatment-essence-230ml",
                    CatSlug = "toner-nuoc-hoa-hong",
                    BrandName = "SK-II",
                    CountryOfOrigin = "Nhật Bản",
                    Unit = "Chai",
                    Volume = "230ml",
                    CostPrice = 2800000m,
                    SellingPrice = 4850000m,
                    DiscountPrice = 4390000m,
                    ShortDescription = "Chứa hơn 90% tinh chất men Pitera kỳ diệu giúp làn da trong suốt, đều màu và mềm mượt.",
                    Description = "Được mệnh danh là 'Nước Thần' vạn người mê, SK-II Facial Treatment Essence chứa hơn 90% Pitera - chiết xuất lên men sinh học giàu vitamin, axit amin, khoáng chất và axit hữu cơ. Sản phẩm nuôi dưỡng chu kỳ tái tạo tự nhiên của da, thu nhỏ lỗ chân lông và làm mờ các vết đốm nâu mang lại làn da pha lê rạng ngời.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1567928815116-f75471158609?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1567928815116-f75471158609?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1608248597359-009848520330?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-kho", "da-hon-hop" },
                    Ingredients = new() { ("pitera", "90%"), ("peptides", "0.5%") },
                    IsFeatured = true,
                    CosmeticNotificationNumber = "138940/20/CBMP-QLD",
                    ImporterName = "Công ty TNHH Procter & Gamble Việt Nam"
                },
                new()
                {
                    Sku = "SKU-SKIN-SUL-012",
                    Barcode = "8935001202012",
                    Name = "Tinh Chất Khởi Nguồn Tái Sinh Da Sulwhasoo First Care Activating Serum VI 60ml",
                    Slug = "tinh-chat-sulwhasoo-first-care-activating-serum-vi-60ml",
                    CatSlug = "serum-tinh-chat",
                    BrandName = "Sulwhasoo",
                    CountryOfOrigin = "Hàn Quốc",
                    Unit = "Chai",
                    Volume = "60ml",
                    CostPrice = 1350000m,
                    SellingPrice = 2200000m,
                    DiscountPrice = 1980000m,
                    ShortDescription = "Hợp chất sâm hoàng gia Ginseng Technology đánh thức sinh khí và độ đàn hồi tự nhiên.",
                    Description = "Sulwhasoo First Care Activating Serum VI thế hệ thứ 6 là tuyệt tác chăm sóc da bước đầu tiên sau khi làm sạch. Công thức kết tinh từ sâm đỏ quý hiếm và thảo mộc cung đình Jaum Activator giúp thẩm thấu tức thì, tăng cường màng chắn tự vệ, củng cố độ ẩm và tăng cường hiệu năng của mọi bước dưỡng tiếp theo.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1512290900672-1f02e6a0d33e?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-kho", "da-nhay-cam", "da-hon-hop" },
                    Ingredients = new() { ("peptides", "1%"), ("panthenol-vitamin-b5", "1%"), ("centella-asiatica", "0.5%") },
                    IsFeatured = true,
                    CosmeticNotificationNumber = "192301/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH Amorepacific Việt Nam"
                },
                new()
                {
                    Sku = "SKU-SKIN-CER-013",
                    Barcode = "8935001202013",
                    Name = "Kem Dưỡng Ẩm Chuyên Sâu Cho Da Khô Đến Rất Khô CeraVe Moisturizing Cream 454g",
                    Slug = "kem-duong-am-cerave-moisturizing-cream-454g",
                    CatSlug = "kem-duong-am",
                    BrandName = "CeraVe",
                    CountryOfOrigin = "Mỹ",
                    Unit = "Hũ",
                    Volume = "454g",
                    CostPrice = 290000m,
                    SellingPrice = 490000m,
                    DiscountPrice = 420000m,
                    ShortDescription = "Công nghệ MVE giải phóng dưỡng chất suốt 24h, phục hồi da nứt nẻ khô căng.",
                    Description = "CeraVe Moisturizing Cream dạng hũ lớn là sản phẩm dưỡng ẩm gia đình số 1 tại Hoa Kỳ. Công nghệ nhũ tương đa tầng MVE (Multivesicular Emulsion) độc quyền giải phóng 3 loại Ceramide và Hyaluronic Acid thẩm thấu từng đợt suốt cả ngày, tái sinh màng lipid cho da khô rát trở nên mềm mịn, không bết dính.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1556228722-d0b5d92fe77c?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-kho", "da-nhay-cam" },
                    Ingredients = new() { ("ceramide", "2%"), ("hyaluronic-acid", "1%") },
                    IsFeatured = false,
                    CosmeticNotificationNumber = "156420/21/CBMP-QLD",
                    ImporterName = "Công ty TNHH L'Oréal Việt Nam"
                },
                new()
                {
                    Sku = "SKU-SKIN-TOR-014",
                    Barcode = "8935001202014",
                    Name = "Serum Cấp Nước Đa Tầng Torriden Dive-In Low Molecular Hyaluronic Acid 50ml",
                    Slug = "serum-cap-nuoc-torriden-dive-in-low-molecular-ha-50ml",
                    CatSlug = "serum-tinh-chat",
                    BrandName = "Torriden",
                    CountryOfOrigin = "Hàn Quốc",
                    Unit = "Chai",
                    Volume = "50ml",
                    CostPrice = 260000m,
                    SellingPrice = 440000m,
                    DiscountPrice = 385000m,
                    ShortDescription = "Serum cấp ẩm số 1 bảng xếp hạng Hwahae & Olive Young Hàn Quốc với 5 loại HA.",
                    Description = "Torriden Dive-In Serum với màu xanh pastel thanh khiết từ chiết xuất Malachite tự nhiên. Chứa phức hợp 5D Complex Hyaluronic Acid với phân tử siêu vi thẩm thấu tới tầng sâu nhất của biểu bì, kết hợp D-Panthenol và Allantoin cấp nước làm dịu da tức thì mà không hề để lại cảm giác dính dớp.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1608248597359-009848520330?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1608248597359-009848520330?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-dau", "da-hon-hop", "da-nhay-cam" },
                    Ingredients = new() { ("hyaluronic-acid", "3%"), ("panthenol-vitamin-b5", "1%"), ("ceramide", "0.5%") },
                    IsFeatured = true,
                    CosmeticNotificationNumber = "188430/22/CBMP-QLD",
                    ImporterName = "Công ty CP Thương Mại Dịch Vụ Kim Điền"
                },
                new()
                {
                    Sku = "SKU-SKIN-COS-015",
                    Barcode = "8935001202015",
                    Name = "Tinh Chất Dịch Nhầy Ốc Sên Tái Tạo Da COSRX Advanced Snail 96 Mucin Power 100ml",
                    Slug = "tinh-chat-oc-sen-cosrx-advanced-snail-96-mucin-100ml",
                    CatSlug = "serum-tinh-chat",
                    BrandName = "COSRX",
                    CountryOfOrigin = "Hàn Quốc",
                    Unit = "Chai",
                    Volume = "100ml",
                    CostPrice = 220000m,
                    SellingPrice = 389000m,
                    DiscountPrice = 329000m,
                    ShortDescription = "Chứa 96.3% dịch lọc ốc sên cô đặc phục hồi màng ẩm và làm mờ thâm sẹo sau mụn.",
                    Description = "COSRX Advanced Snail 96 Mucin Power Essence là giải pháp phục hồi da trứ danh từ Hàn Quốc. Chứa đến 96.3% dịch nhầy ốc sên tự nhiên tinh khiết giàu allantoin, collagen, elastin và axit glycolic giúp dưỡng ẩm sâu, làm mờ thâm mụn, kích thích lành thương và phục hồi độ đàn hồi cho làn da căng bóng mịn màng.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1608248597359-009848520330?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1556228722-d0b5d92fe77c?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-nhay-cam", "da-kho", "da-dau" },
                    Ingredients = new() { ("hyaluronic-acid", "1%"), ("panthenol-vitamin-b5", "0.5%") },
                    IsFeatured = false,
                    CosmeticNotificationNumber = "139420/20/CBMP-QLD",
                    ImporterName = "Công ty CP Quốc Tế Đại Cát Á"
                },
                new()
                {
                    Sku = "SKU-SKIN-SBM-016",
                    Barcode = "8935001202016",
                    Name = "Serum Phục Hồi Làm Dịu Da Mụn Some By Mi AHA-BHA-PHA 30 Days Miracle 50ml",
                    Slug = "serum-some-by-mi-aha-bha-pha-30-days-miracle-50ml",
                    CatSlug = "serum-tinh-chat",
                    BrandName = "Some By Mi",
                    CountryOfOrigin = "Hàn Quốc",
                    Unit = "Chai",
                    Volume = "50ml",
                    CostPrice = 195000m,
                    SellingPrice = 345000m,
                    DiscountPrice = 295000m,
                    ShortDescription = "Phức hợp 3 loại Acid kết hợp 10.000ppm tràm trà giảm sưng mụn và làm sạch bã nhờn.",
                    Description = "Tinh chất trị mụn 30 ngày Some By Mi kết hợp tỉ lệ hoàn hảo giữa AHA (tẩy tế bào chết bề mặt), BHA (làm sạch sâu bã nhờn trong lỗ chân lông), PHA (khóa ẩm kháng viêm) cùng 10.000ppm nước tràm trà và 14.5% chiết xuất rau má làm dịu tức thì các đốm mụn sưng đỏ.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1608248597359-009848520330?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-dau", "da-hon-hop" },
                    Ingredients = new() { ("tea-tree-tram-tra", "1%"), ("aha-glycolic-acid", "0.5%"), ("bha-salicylic-acid", "0.5%"), ("centella-asiatica", "1%") },
                    IsFeatured = false,
                    CosmeticNotificationNumber = "129840/20/CBMP-QLD",
                    ImporterName = "Công ty CP Mỹ Phẩm Fobic"
                },
                new()
                {
                    Sku = "SKU-SKIN-COS-017",
                    Barcode = "8935001202017",
                    Name = "Gel Rửa Mặt Dịu Nhẹ Độ pH Chuẩn COSRX Low pH Good Morning Gel Cleanser 150ml",
                    Slug = "gel-rua-mat-cosrx-low-ph-good-morning-gel-cleanser-150ml",
                    CatSlug = "sua-rua-mat",
                    BrandName = "COSRX",
                    CountryOfOrigin = "Hàn Quốc",
                    Unit = "Tuýp",
                    Volume = "150ml",
                    CostPrice = 130000m,
                    SellingPrice = 239000m,
                    DiscountPrice = 199000m,
                    ShortDescription = "Độ pH lý tưởng 5.0 - 6.0 với dầu tràm trà và BHA tự nhiên làm sạch sâu mà không khô căng.",
                    Description = "Sữa rửa mặt dạng gel trong suốt có bọt mịn tự nhiên của COSRX. Công thức chứa dầu tràm trà tự nhiên kháng khuẩn, kết hợp 0.5% Betaine Salicylate (BHA) thanh lọc da chết nhẹ nhàng mỗi sáng, bảo vệ màng axit sinh học tự nhiên cho da thông thoáng và tươi tắn suốt cả ngày.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1556228722-d0b5d92fe77c?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1556228722-d0b5d92fe77c?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-dau", "da-nhay-cam", "da-hon-hop" },
                    Ingredients = new() { ("tea-tree-tram-tra", "0.5%"), ("bha-salicylic-acid", "0.5%") },
                    IsFeatured = false,
                    CosmeticNotificationNumber = "118940/19/CBMP-QLD",
                    ImporterName = "Công ty CP Quốc Tế Đại Cát Á"
                },
                new()
                {
                    Sku = "SKU-SKIN-SHI-018",
                    Barcode = "8935001202018",
                    Name = "Tinh Chất Tăng Cường Đề Kháng Da Shiseido Ultimune Power Infusing Concentrate 50ml",
                    Slug = "tinh-chat-shiseido-ultimune-power-infusing-concentrate-50ml",
                    CatSlug = "serum-tinh-chat",
                    BrandName = "Shiseido",
                    CountryOfOrigin = "Nhật Bản",
                    Unit = "Chai",
                    Volume = "50ml",
                    CostPrice = 1850000m,
                    SellingPrice = 3100000m,
                    DiscountPrice = 2790000m,
                    ShortDescription = "Công nghệ ImuGenerationRED kích hoạt lưu thông máu và tế bào miễn dịch bảo vệ da.",
                    Description = "Tinh chất đỏ biểu tượng của Shiseido chứa tinh túy từ nấm Linh Chi đỏ và chiết xuất rễ hoa Diên Vĩ Iris. Sản phẩm giúp tăng cường khả năng tự vệ tự nhiên của tế bào da trước khói bụi ô nhiễm và căng thẳng oxy hóa, cải thiện độ đàn hồi, làm mờ nếp nhăn và mang lại làn da căng mịn đầy sức sống.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1540555700478-4be289fbecef?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1540555700478-4be289fbecef?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-kho", "da-nhay-cam" },
                    Ingredients = new() { ("peptides", "1%"), ("hyaluronic-acid", "1%"), ("panthenol-vitamin-b5", "0.5%") },
                    IsFeatured = true,
                    CosmeticNotificationNumber = "177430/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH Mỹ Phẩm Shiseido Việt Nam"
                },
                new()
                {
                    Sku = "SKU-SKIN-DHC-019",
                    Barcode = "8935001202019",
                    Name = "Dầu Tẩy Trang Làm Sạch Sâu & Chống Oxy Hóa DHC Deep Cleansing Oil 200ml",
                    Slug = "dau-tay-trang-dhc-deep-cleansing-oil-200ml",
                    CatSlug = "nuoc-tay-trang",
                    BrandName = "DHC",
                    CountryOfOrigin = "Nhật Bản",
                    Unit = "Chai",
                    Volume = "200ml",
                    CostPrice = 380000m,
                    SellingPrice = 649000m,
                    DiscountPrice = 559000m,
                    ShortDescription = "Tinh chất dầu Olive Tây Ban Nha hữu cơ hòa tan mọi lớp makeup chống nước cứng đầu.",
                    Description = "DHC Deep Cleansing Oil là sản phẩm tẩy trang gốc dầu huyền thoại số 1 tại Nhật Bản. Chứa dầu olive hữu cơ ép lạnh nguyên chất kết hợp Vitamin E và tinh dầu hương thảo, nhẹ nhàng hòa tan hoàn toàn lớp mascara chống nước, son lì và kem nền dày cộm mà không làm mất đi độ ẩm tự nhiên của da.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1608248597359-009848520330?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-kho", "da-thuong", "da-hon-hop" },
                    Ingredients = new() { ("squalane", "2%"), ("argan-oil", "1%") },
                    IsFeatured = false,
                    CosmeticNotificationNumber = "123091/20/CBMP-QLD",
                    ImporterName = "Công ty CP Belie Việt Nam"
                },
                new()
                {
                    Sku = "SKU-SKIN-LRP-020",
                    Barcode = "8935001202020",
                    Name = "Kem Chống Nắng Kiềm Dầu La Roche-Posay Anthelios UVmune 400 Oil Control 50ml",
                    Slug = "kem-chong-nang-la-roche-posay-anthelios-uvmune-400-oil-control-50ml",
                    CatSlug = "kem-chong-nang",
                    BrandName = "La Roche-Posay",
                    CountryOfOrigin = "Pháp",
                    Unit = "Tuýp",
                    Volume = "50ml",
                    CostPrice = 340000m,
                    SellingPrice = 535000m,
                    DiscountPrice = 469000m,
                    ShortDescription = "Màng lọc độc quyền Mexoryl 400 chống tia UVA dài nhất, kiểm soát dầu thừa 12H.",
                    Description = "Đột phá bảo vệ da quang phổ rộng sau 10 năm nghiên cứu của tập đoàn L'Oréal. Màng lọc Mexoryl 400 chống lại tia UVA siêu dài (380-400nm) – nguyên nhân chính gây lão hóa sâu và nám sạm. Hoạt chất Airlicium hấp thụ bã nhờn gấp 100 lần trọng lượng giúp da luôn ráo mịn, không bóng nhờn.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1556228722-d0b5d92fe77c?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-dau", "da-nhay-cam", "da-hon-hop" },
                    Ingredients = new() { ("panthenol-vitamin-b5", "0.5%"), ("niacinamide", "1%") },
                    IsFeatured = true,
                    CosmeticNotificationNumber = "194021/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH L'Oréal Việt Nam"
                },
                new()
                {
                    Sku = "SKU-SKIN-LAN-021",
                    Barcode = "8935001202021",
                    Name = "Mặt Nạ Ngủ Cấp Nước Thanh Lọc Da Laneige Water Sleeping Mask EX 70ml",
                    Slug = "mat-na-ngu-laneige-water-sleeping-mask-ex-70ml",
                    CatSlug = "mat-na-duong-da",
                    BrandName = "Laneige",
                    CountryOfOrigin = "Hàn Quốc",
                    Unit = "Hũ",
                    Volume = "70ml",
                    CostPrice = 480000m,
                    SellingPrice = 790000m,
                    DiscountPrice = 699000m,
                    ShortDescription = "Công nghệ Sleeping Micro Biome và phức hợp men Probiotics phục hồi làn da mệt mỏi.",
                    Description = "Mặt nạ ngủ Laneige Water Sleeping Mask EX phiên bản cải tiến mang lại trải nghiệm spa tại nhà. Hợp chất Probiotics Complex từ trà xanh lên men kết hợp Squalane củng cố hàng rào bảo vệ, cấp nước dồi dào suốt đêm giúp làn da sáng bừng, rạng rỡ và căng mọng như được ngủ đủ 8 tiếng.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1567928815116-f75471158609?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1567928815116-f75471158609?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-kho", "da-thuong", "da-hon-hop" },
                    Ingredients = new() { ("hyaluronic-acid", "2%"), ("squalane", "1%"), ("panthenol-vitamin-b5", "0.5%") },
                    IsFeatured = true,
                    CosmeticNotificationNumber = "178230/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH Amorepacific Việt Nam"
                },
                new()
                {
                    Sku = "SKU-SKIN-EST-022",
                    Barcode = "8935001202022",
                    Name = "Kem Dưỡng Vùng Da Mắt Chống Quầng Thâm Estée Lauder Advanced Night Repair Eye 15ml",
                    Slug = "kem-duong-mat-estee-lauder-advanced-night-repair-eye-15ml",
                    CatSlug = "cham-soc-vung-mat",
                    BrandName = "Estée Lauder",
                    CountryOfOrigin = "Mỹ",
                    Unit = "Hũ",
                    Volume = "15ml",
                    CostPrice = 1250000m,
                    SellingPrice = 2150000m,
                    DiscountPrice = 1950000m,
                    ShortDescription = "Giảm rõ rệt quầng thâm mắt chỉ sau 2 tuần, chống tác hại ánh sáng xanh màn hình.",
                    Description = "Kem dưỡng mắt dạng gel-creme siêu sang trọng của Estée Lauder. Công nghệ Chronolux Power Signal độc quyền cùng phức hợp chống oxy hóa đa tác động bảo vệ vùng da mắt mỏng manh trước ánh sáng xanh từ thiết bị điện tử, xóa mờ nếp nhăn đuôi mắt và quầng thâm mệt mỏi.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1512290900672-1f02e6a0d33e?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1512290900672-1f02e6a0d33e?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-nhay-cam", "da-kho", "da-thuong" },
                    Ingredients = new() { ("peptides", "2%"), ("hyaluronic-acid", "1%"), ("panthenol-vitamin-b5", "0.5%") },
                    IsFeatured = true,
                    CosmeticNotificationNumber = "182930/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH Estée Lauder Việt Nam"
                },
                new()
                {
                    Sku = "SKU-SKIN-INN-023",
                    Barcode = "8935001202023",
                    Name = "Kem Dưỡng Dạng Thạch Trắng Sáng Mờ Nám Innisfree Cherry Blossom Glow Jelly 50ml",
                    Slug = "kem-duong-innisfree-cherry-blossom-glow-jelly-50ml",
                    CatSlug = "kem-duong-am",
                    BrandName = "Innisfree",
                    CountryOfOrigin = "Hàn Quốc",
                    Unit = "Hũ",
                    Volume = "50ml",
                    CostPrice = 280000m,
                    SellingPrice = 480000m,
                    DiscountPrice = 410000m,
                    ShortDescription = "Chiết xuất lá hoa anh đào đảo Jeju và Betaine tự nhiên nuôi dưỡng da trong veo phát sáng.",
                    Description = "Innisfree Cherry Blossom Glow Jelly Cream với chất gel thạch trong suốt mỏng nhẹ như sương, tan ngay trên da khi thoa. Chiết xuất lá hoa anh đào hoàng gia đảo ngọc Jeju kết hợp Niacinamide dưỡng sáng da đều màu, cấp nước thanh mát mang lại hiệu ứng làn da thủy tinh Glass Skin chuẩn Hàn.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1527799820374-dcf8d9d4a388?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-dau", "da-hon-hop", "da-thuong" },
                    Ingredients = new() { ("niacinamide", "2%"), ("hyaluronic-acid", "1%") },
                    IsFeatured = false,
                    CosmeticNotificationNumber = "154320/21/CBMP-QLD",
                    ImporterName = "Công ty TNHH Amorepacific Việt Nam"
                },
                new()
                {
                    Sku = "SKU-SKIN-SBM-024",
                    Barcode = "8935001202024",
                    Name = "Nước Hoa Hồng Giảm Mụn Làm Sạch Sâu Some By Mi Miracle Toner 150ml",
                    Slug = "nuoc-hoa-hong-some-by-mi-aha-bha-pha-miracle-toner-150ml",
                    CatSlug = "toner-nuoc-hoa-hong",
                    BrandName = "Some By Mi",
                    CountryOfOrigin = "Hàn Quốc",
                    Unit = "Chai",
                    Volume = "150ml",
                    CostPrice = 180000m,
                    SellingPrice = 320000m,
                    DiscountPrice = 275000m,
                    ShortDescription = "Cân bằng độ ẩm, loại bỏ bã nhờn và tế bào chết với bộ 3 AHA-BHA-PHA và tràm trà.",
                    Description = "Some By Mi AHA-BHA-PHA 30 Days Miracle Toner chứa chiết xuất tràm trà thiên nhiên nồng độ cao cùng cây đu đủ và hạt đậu lăng. Giúp lấy sạch cặn bẩn còn sót lại sâu dưới lỗ chân lông, kháng khuẩn ngăn ngừa mụn tái phát và se khít nang lông rõ rệt sau 30 ngày kiên trì sử dụng.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1608248597359-009848520330?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1608248597359-009848520330?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-dau", "da-hon-hop" },
                    Ingredients = new() { ("tea-tree-tram-tra", "1%"), ("aha-glycolic-acid", "0.5%"), ("bha-salicylic-acid", "0.5%") },
                    IsFeatured = false,
                    CosmeticNotificationNumber = "128490/20/CBMP-QLD",
                    ImporterName = "Công ty CP Mỹ Phẩm Fobic"
                },
                new()
                {
                    Sku = "SKU-SKIN-INN-025",
                    Barcode = "8935001202025",
                    Name = "Kem Khóa Ẩm Trà Xanh Đảo Jeju Innisfree Green Tea Seed Hyaluronic Cream 50ml",
                    Slug = "kem-khoa-am-innisfree-green-tea-seed-hyaluronic-cream-50ml",
                    CatSlug = "kem-duong-am",
                    BrandName = "Innisfree",
                    CountryOfOrigin = "Hàn Quốc",
                    Unit = "Hũ",
                    Volume = "50ml",
                    CostPrice = 290000m,
                    SellingPrice = 510000m,
                    DiscountPrice = 435000m,
                    ShortDescription = "Trà xanh hữu cơ Beauty Green Tea giàu 16 loại amino acid khóa ẩm màng hydrolipid.",
                    Description = "Kem dưỡng ẩm trà xanh trứ danh Innisfree chứa chiết xuất lá trà xanh giống Beauty Green Tea hữu cơ tuyển chọn kỹ lưỡng từ đảo Jeju. Công thức bổ sung phức hợp 5 loại Hyaluronic Acid khóa chặt độ ẩm trong tế bào da suốt 100 giờ, phục hồi làn da khô sần, mất nước trở nên mịn màng và dẻo dai.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1527799820374-dcf8d9d4a388?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-kho", "da-thuong", "da-nhay-cam" },
                    Ingredients = new() { ("hyaluronic-acid", "2%"), ("panthenol-vitamin-b5", "1%"), ("ceramide", "0.5%") },
                    IsFeatured = false,
                    CosmeticNotificationNumber = "165430/21/CBMP-QLD",
                    ImporterName = "Công ty TNHH Amorepacific Việt Nam"
                },
                new()
                {
                    Sku = "SKU-SKIN-INN-026",
                    Barcode = "8935001202026",
                    Name = "Mặt Nạ Đất Sét Tro Núi Lửa Hút Bã Nhờn Innisfree Super Volcanic Pore Clay Mask 100ml",
                    Slug = "mat-na-dat-set-innisfree-super-volcanic-pore-clay-mask-100ml",
                    CatSlug = "mat-na-duong-da",
                    BrandName = "Innisfree",
                    CountryOfOrigin = "Hàn Quốc",
                    Unit = "Hũ",
                    Volume = "100ml",
                    CostPrice = 190000m,
                    SellingPrice = 340000m,
                    DiscountPrice = 289000m,
                    ShortDescription = "Đá tro núi lửa Jeju Sphere hút sạch 98% sợi bã nhờn và se nhỏ lỗ chân lông tức thì.",
                    Description = "Mặt nạ đất sét hút dầu huyền thoại giải quyết 10 vấn đề về lỗ chân lông của Innisfree. Công nghệ đá tro núi lửa Jeju Sphere độc quyền có sức hút bã nhờn mạnh gấp 2 lần, kết hợp bột vỏ hạt óc chó và AHA làm sạch sâu từng nang lông, giảm sợi bã nhờn và mang lại làn da mịn mượt không bóng dầu.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1567928815116-f75471158609?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1567928815116-f75471158609?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1527799820374-dcf8d9d4a388?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1556228722-d0b5d92fe77c?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-dau", "da-hon-hop" },
                    Ingredients = new() { ("aha-glycolic-acid", "0.5%"), ("tea-tree-tram-tra", "0.3%") },
                    IsFeatured = false,
                    CosmeticNotificationNumber = "143210/20/CBMP-QLD",
                    ImporterName = "Công ty TNHH Amorepacific Việt Nam"
                },
                new()
                {
                    Sku = "SKU-SKIN-S10-027",
                    Barcode = "8935001202027",
                    Name = "Tinh Chất Rau Má Phục Hồi Dịu Da Skin1004 Madagascar Centella Ampoule 100ml",
                    Slug = "tinh-chat-rau-ma-skin1004-madagascar-centella-ampoule-100ml",
                    CatSlug = "serum-tinh-chat",
                    BrandName = "COSRX",
                    CountryOfOrigin = "Hàn Quốc",
                    Unit = "Chai",
                    Volume = "100ml",
                    CostPrice = 240000m,
                    SellingPrice = 410000m,
                    DiscountPrice = 349000m,
                    ShortDescription = "100% chiết xuất rau má vùng Madagascar nguyên chất làm dịu ửng đỏ và củng cố hàng rào da.",
                    Description = "Skin1004 Madagascar Centella Ampoule là sản phẩm làm dịu da nhạy cảm mụn bán chạy bậc nhất châu Á. Chứa 100% tinh chất rau má cô đặc được thu hái thủ công tại vùng thảo nguyên Madagascar giàu hoạt chất sinh học Asiaticoside, Madecassic Acid giúp làm dịu vết mụn sưng viêm, phục hồi da tổn thương do ánh nắng.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1608248597359-009848520330?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-nhay-cam", "da-dau", "da-kho" },
                    Ingredients = new() { ("centella-asiatica", "5%") },
                    IsFeatured = true,
                    CosmeticNotificationNumber = "176540/22/CBMP-QLD",
                    ImporterName = "Công ty CP Tiên Phong Cosmetics"
                },
                new()
                {
                    Sku = "SKU-SKIN-S10-028",
                    Barcode = "8935001202028",
                    Name = "Kem Chống Nắng Vật Lý Chiết Xuất Rau Má Skin1004 Centella Air-Fit Plus SPF50+ 50ml",
                    Slug = "kem-chong-nang-skin1004-centella-air-fit-suncream-plus-spf50-50ml",
                    CatSlug = "kem-chong-nang",
                    BrandName = "COSRX",
                    CountryOfOrigin = "Hàn Quốc",
                    Unit = "Tuýp",
                    Volume = "50ml",
                    CostPrice = 210000m,
                    SellingPrice = 360000m,
                    DiscountPrice = 299000m,
                    ShortDescription = "Kem chống nắng vật lý mỏng nhẹ, nâng tông tự nhiên và không gây cay mắt.",
                    Description = "Skin1004 Madagascar Centella Air-Fit Suncream Plus SPF50+ PA++++ với màng lọc vật lý Zinc Oxide và Titanium Dioxide an toàn cho mẹ bầu và da nhạy cảm. Công thức tăng cường 20% chiết xuất rau má Madagascar kết hợp Niacinamide dưỡng sáng, hiệu chỉnh sắc da hồng hào nhẹ nhàng, kiềm dầu khô thoáng.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1556228722-d0b5d92fe77c?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-nhay-cam", "da-dau", "da-hon-hop" },
                    Ingredients = new() { ("centella-asiatica", "1%"), ("niacinamide", "2%") },
                    IsFeatured = false,
                    CosmeticNotificationNumber = "187650/22/CBMP-QLD",
                    ImporterName = "Công ty CP Tiên Phong Cosmetics"
                },
                new()
                {
                    Sku = "SKU-SKIN-LAN-029",
                    Barcode = "8935001202029",
                    Name = "Tinh Chất Trẻ Hóa Tái Tạo Tế Bào Lancôme Advanced Génifique Youth Activating 50ml",
                    Slug = "tinh-chat-lancome-advanced-genifique-concentrate-50ml",
                    CatSlug = "serum-tinh-chat",
                    BrandName = "Lancôme",
                    CountryOfOrigin = "Pháp",
                    Unit = "Chai",
                    Volume = "50ml",
                    CostPrice = 2100000m,
                    SellingPrice = 3700000m,
                    DiscountPrice = 3350000m,
                    ShortDescription = "Phức hợp 7 phân đoạn Pre- & Probiotics phục hồi hệ vi sinh màng da săn chắc trẻ trung.",
                    Description = "Đỉnh cao khoa học trẻ hóa da từ nhà mốt hoa hồng vàng Lancôme. Tinh chất Advanced Génifique tăng cường hệ vi sinh trên bề mặt da sau một giọt đầu tiên. Chứa phức hợp 7 phần đoạn Pre- và Probiotics, Hyaluronic Acid và Vitamin Cg giúp củng cố màng bảo vệ, mờ nếp nhăn và đem lại làn da sáng mịn căng bóng.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1512290900672-1f02e6a0d33e?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-kho", "da-nhay-cam", "da-hon-hop" },
                    Ingredients = new() { ("hyaluronic-acid", "2%"), ("peptides", "2%"), ("vitamin-c", "1%") },
                    IsFeatured = true,
                    CosmeticNotificationNumber = "198740/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH L'Oréal Việt Nam"
                },
                new()
                {
                    Sku = "SKU-SKIN-LAN-030",
                    Barcode = "8935001202030",
                    Name = "Kem Dưỡng Mắt Chống Lão Hóa & Sáng Quầng Thâm Lancôme Advanced Génifique Eye Cream 15ml",
                    Slug = "kem-mat-lancome-advanced-genifique-eye-cream-15ml",
                    CatSlug = "cham-soc-vung-mat",
                    BrandName = "Lancôme",
                    CountryOfOrigin = "Pháp",
                    Unit = "Hũ",
                    Volume = "15ml",
                    CostPrice = 1250000m,
                    SellingPrice = 2200000m,
                    DiscountPrice = 1990000m,
                    ShortDescription = "Củng cố màng ẩm vùng mắt mỏng manh, xóa mờ quầng thâm và bọng mắt rõ rệt.",
                    Description = "Lancôme Advanced Génifique Yeux là liệu pháp nuôi dưỡng vùng da quanh mắt đỉnh cao. Công nghệ vi sinh sinh học bổ sung hàng triệu phân đoạn men vi sinh prebiotic và probiotic, kích thích tăng sinh vi tuần hoàn máu giảm bọng mắt, làm mờ các rãnh nhăn chân chim và cải thiện quầng thâm sáng rực rỡ.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1512290900672-1f02e6a0d33e?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1512290900672-1f02e6a0d33e?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-nhay-cam", "da-kho", "da-thuong" },
                    Ingredients = new() { ("peptides", "2%"), ("hyaluronic-acid", "1%"), ("ceramide", "0.5%") },
                    IsFeatured = true,
                    CosmeticNotificationNumber = "198741/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH L'Oréal Việt Nam"
                },

                // =========================================================================
                // II. NHÓM SON MÔI & TRANG ĐIỂM MAKEUP (25 SẢN PHẨM THỰC TẾ)
                // =========================================================================
                new()
                {
                    Sku = "SKU-LIP-MAC-031",
                    Barcode = "8935001202031",
                    Name = "Son Thỏi Siêu Lì MAC Retro Matte Lipstick - #707 Ruby Woo (Đỏ Cổ Điển) 3g",
                    Slug = "son-thoi-mac-retro-matte-lipstick-707-ruby-woo-3g",
                    CatSlug = "son-moi",
                    BrandName = "MAC Cosmetics",
                    CountryOfOrigin = "Mỹ",
                    Unit = "Thỏi",
                    Volume = "3g",
                    CostPrice = 380000m,
                    SellingPrice = 650000m,
                    DiscountPrice = 579000m,
                    ShortDescription = "Màu son đỏ ruby huyền thoại tôn da số 1 thế giới, chất son siêu lì bám màu suốt 12h.",
                    Description = "MAC Retro Matte Lipstick #707 Ruby Woo là thỏi son hình viên đạn kinh điển mà mọi tín đồ làm đẹp đều sở hữu. Tông đỏ thuần cổ điển ánh xanh tôn da và răng trắng sáng tức thì. Chất son retro matte lì hoàn hảo, lên màu chuẩn sắc ngay từ lần quẹt đầu tiên và bền màu bền bỉ qua các bữa tiệc.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-dau", "da-kho" },
                    Ingredients = new() { ("vitamin-c", "0.2%") },
                    HasVariants = true,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "154980/21/CBMP-QLD",
                    ImporterName = "Công ty TNHH Estée Lauder Việt Nam"
                },
                new()
                {
                    Sku = "SKU-LIP-MAC-032",
                    Barcode = "8935001202032",
                    Name = "Son Thỏi Lì MAC Matte Lipstick - #602 Chili (Đỏ Tương Ớt Thời Thượng) 3g",
                    Slug = "son-thoi-mac-matte-lipstick-602-chili-3g",
                    CatSlug = "son-moi",
                    BrandName = "MAC Cosmetics",
                    CountryOfOrigin = "Mỹ",
                    Unit = "Thỏi",
                    Volume = "3g",
                    CostPrice = 380000m,
                    SellingPrice = 650000m,
                    DiscountPrice = 579000m,
                    ShortDescription = "Sắc đỏ pha cam đất ấm áp, phù hợp hoàn hảo với làn da châu Á.",
                    Description = "MAC Matte Lipstick #602 Chili là sự hòa quyện tuyệt mỹ giữa đỏ tươi, cam và chút sắc nâu đất trầm ấm tạo nên tông màu đỏ gạch tôn vinh khí chất quý cô. Chất son matte mềm mịn lướt êm ái trên môi, che phủ mọi khuyết điểm rãnh môi và giữ độ ẩm mịn không gây khô tróc khó chịu.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-dau", "da-kho" },
                    Ingredients = new() { ("vitamin-c", "0.2%") },
                    HasVariants = true,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "154981/21/CBMP-QLD",
                    ImporterName = "Công ty TNHH Estée Lauder Việt Nam"
                },
                new()
                {
                    Sku = "SKU-LIP-MAC-033",
                    Barcode = "8935001202033",
                    Name = "Son Thỏi Mờ Lì Mịn Mượt MAC Powder Kiss Lipstick - #316 Devoted To Chili 3g",
                    Slug = "son-thoi-mac-powder-kiss-lipstick-316-devoted-to-chili-3g",
                    CatSlug = "son-moi",
                    BrandName = "MAC Cosmetics",
                    CountryOfOrigin = "Mỹ",
                    Unit = "Thỏi",
                    Volume = "3g",
                    CostPrice = 420000m,
                    SellingPrice = 710000m,
                    DiscountPrice = 639000m,
                    ShortDescription = "Hiệu ứng mờ ảo Soft-Focus làm mờ viền môi, chất son nhẹ xốp như bột phấn.",
                    Description = "Đột phá chất son cải tiến Powder Kiss Lipstick của MAC mang lại cảm giác nhẹ tênh như không khí. Màu #316 Devoted To Chili là phiên bản mềm mại hơn của thỏi Chili huyền thoại, lớp finish mờ ảo như thoa qua một lớp filter nhung mịn lôi cuốn mọi ánh nhìn.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-dau", "da-kho" },
                    Ingredients = new() { ("vitamin-c", "0.2%") },
                    HasVariants = true,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "154982/21/CBMP-QLD",
                    ImporterName = "Công ty TNHH Estée Lauder Việt Nam"
                },
                new()
                {
                    Sku = "SKU-LIP-ROM-034",
                    Barcode = "8935001202034",
                    Name = "Son Kem Lì Xốp Mịn Rom&nd Zero Velvet Tint - #02 Joyful (Đỏ Cam Đất) 5.5g",
                    Slug = "son-kem-li-romand-zero-velvet-tint-02-joyful-55g",
                    CatSlug = "son-moi",
                    BrandName = "Rom&nd",
                    CountryOfOrigin = "Hàn Quốc",
                    Unit = "Thỏi",
                    Volume = "5.5g",
                    CostPrice = 115000m,
                    SellingPrice = 189000m,
                    DiscountPrice = 159000m,
                    ShortDescription = "Chất son velvet nhung xốp làm đầy rãnh môi, lên màu chuẩn nét ngay từ lần đầu thoa.",
                    Description = "Rom&nd Zero Velvet Tint #02 Joyful là thỏi son kem lì quốc dân của giới trẻ châu Á. Chất son xốp nhẹ tênh, dễ tán đều và có hiệu ứng che mờ nếp nhăn môi hoàn hảo. Màu đỏ cam đất rực rỡ mang lại diện mạo tràn đầy năng lượng tươi tắn cho ngày mới.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1583241800698-e8ab01830a07?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-dau", "da-kho" },
                    Ingredients = new() { ("vitamin-c", "0.1%") },
                    HasVariants = true,
                    IsFeatured = false,
                    CosmeticNotificationNumber = "143090/20/CBMP-QLD",
                    ImporterName = "Công ty TNHH B2Link Việt Nam"
                },
                new()
                {
                    Sku = "SKU-LIP-ROM-035",
                    Barcode = "8935001202035",
                    Name = "Son Tint Bóng Mọng Nước Rom&nd Juicy Lasting Tint - #06 Figfig (Hồng Quả Sung) 5.5g",
                    Slug = "son-tint-bong-romand-juicy-lasting-tint-06-figfig-55g",
                    CatSlug = "son-moi",
                    BrandName = "Rom&nd",
                    CountryOfOrigin = "Hàn Quốc",
                    Unit = "Thỏi",
                    Volume = "5.5g",
                    CostPrice = 120000m,
                    SellingPrice = 199000m,
                    DiscountPrice = 169000m,
                    ShortDescription = "Lớp bóng thủy tinh mọng nước, bền màu với lớp stain bám chặt không lem.",
                    Description = "Rom&nd Juicy Lasting Tint #06 Figfig là màu son best-seller mang sắc hồng đất quả sung thanh lịch MLBB (My Lips But Better). Kết cấu son bóng dạng gel nước mỏng nhẹ, giữ độ ẩm mịn màng và để lại lớp màu stain tự nhiên bám dính suốt 8 tiếng.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-dau", "da-kho" },
                    Ingredients = new() { ("hyaluronic-acid", "0.2%") },
                    HasVariants = true,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "143091/20/CBMP-QLD",
                    ImporterName = "Công ty TNHH B2Link Việt Nam"
                },
                new()
                {
                    Sku = "SKU-LIP-ROM-036",
                    Barcode = "8935001202036",
                    Name = "Son Bóng Trong Suốt Ánh Nước Rom&nd Glasting Water Tint - #04 Vintage Ocean 4g",
                    Slug = "son-bong-romand-glasting-water-tint-04-vintage-ocean-4g",
                    CatSlug = "son-moi",
                    BrandName = "Rom&nd",
                    CountryOfOrigin = "Hàn Quốc",
                    Unit = "Thỏi",
                    Volume = "4g",
                    CostPrice = 120000m,
                    SellingPrice = 199000m,
                    DiscountPrice = 169000m,
                    ShortDescription = "Lớp màng bóng tách biệt mỏng trong như pha lê, tông đỏ nâu trầm cổ điển mê hoặc.",
                    Description = "Rom&nd Glasting Water Tint áp dụng công nghệ tách lớp độc đáo: lớp màu bám chặt vào môi trong khi lớp bóng trong suốt nổi lên trên như một màng nước pha lê lấp lánh, không hề gây dính nhớp khi bặm môi. Tông #04 Vintage Ocean là sắc đỏ nâu trầm ấm quyến rũ.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-dau", "da-kho" },
                    Ingredients = new() { ("hyaluronic-acid", "0.2%") },
                    HasVariants = true,
                    IsFeatured = false,
                    CosmeticNotificationNumber = "143092/20/CBMP-QLD",
                    ImporterName = "Công ty TNHH B2Link Việt Nam"
                },
                new()
                {
                    Sku = "SKU-LIP-DIO-037",
                    Barcode = "8935001202037",
                    Name = "Son Thỏi Cao Cấp Rouge Dior Satin Lipstick - #999 Metallic Velvet Đỏ Cổ Điển 3.5g",
                    Slug = "son-thoi-rouge-dior-satin-lipstick-999-metallic-velvet-35g",
                    CatSlug = "son-moi",
                    BrandName = "Dior",
                    CountryOfOrigin = "Pháp",
                    Unit = "Thỏi",
                    Volume = "3.5g",
                    CostPrice = 750000m,
                    SellingPrice = 1250000m,
                    DiscountPrice = 1150000m,
                    ShortDescription = "Thỏi son đỏ hoàng gia Haute Couture biểu tượng nước Pháp, chiết xuất hoa mẫu đơn đỏ.",
                    Description = "Rouge Dior #999 là kiệt tác nghệ thuật trang điểm trường tồn của nhà mốt Christian Dior. Màu đỏ nguyên bản tôn vinh nét quyến rũ kiêu kỳ của mọi phụ nữ. Công thức chăm sóc môi từ hoa mẫu đơn đỏ và hoa lựu cung cấp độ ẩm liên tục 16 giờ, cảm giác nhung mịn sang trọng tuyệt đỉnh.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1512290900672-1f02e6a0d33e?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-dau", "da-kho" },
                    Ingredients = new() { ("squalane", "0.5%") },
                    HasVariants = true,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "194090/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH LVMH Parfums & Cosmetiques Việt Nam"
                },
                new()
                {
                    Sku = "SKU-LIP-YSL-038",
                    Barcode = "8935001202038",
                    Name = "Son Thỏi Vuông Cao Cấp YSL Rouge Pur Couture The Slim - #21 Rouge Paradoxe 2.2g",
                    Slug = "son-thoi-ysl-rouge-pur-couture-the-slim-21-rouge-paradoxe-22g",
                    CatSlug = "son-moi",
                    BrandName = "Yves Saint Laurent",
                    CountryOfOrigin = "Pháp",
                    Unit = "Thỏi",
                    Volume = "2.2g",
                    CostPrice = 760000m,
                    SellingPrice = 1290000m,
                    DiscountPrice = 1190000m,
                    ShortDescription = "Thiết kế đầu son vuông sắc cạnh định hình viền môi chuẩn xác, chất son lì mịn như da thuộc.",
                    Description = "YSL The Slim #21 Rouge Paradoxe sở hữu thiết kế thân son vuông thanh mảnh mạ vàng xa hoa. Tông đỏ ruby thuần tôn vinh cá tính tự do phá cách của người phụ nữ hiện đại. Chất son mờ lì như chất liệu da thuộc Haute Couture ôm ấp làn môi nhẹ nhàng, bám dính lâu trôi cả ngày.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1512290900672-1f02e6a0d33e?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-dau", "da-kho" },
                    Ingredients = new() { ("vitamin-c", "0.2%") },
                    HasVariants = true,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "187420/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH L'Oréal Việt Nam"
                },
                new()
                {
                    Sku = "SKU-LIP-CHA-039",
                    Barcode = "8935001202039",
                    Name = "Son Thỏi Lì Nhung Xa Xỉ Chanel Rouge Allure Velvet - #58 Rouge Vie 3.5g",
                    Slug = "son-thoi-chanel-rouge-allure-velvet-58-rouge-vie-35g",
                    CatSlug = "son-moi",
                    BrandName = "Chanel",
                    CountryOfOrigin = "Pháp",
                    Unit = "Thỏi",
                    Volume = "3.5g",
                    CostPrice = 820000m,
                    SellingPrice = 1350000m,
                    DiscountPrice = 1250000m,
                    ShortDescription = "Hạt sắc tố siêu mịn và dầu jojoba dưỡng ẩm, cảm giác bấm 'Click' đen tuyền kiêu sa.",
                    Description = "Chanel Rouge Allure Velvet #58 Rouge Vie là nốt son đỏ mận bí ẩn và đầy quyền lực của nhà mốt Chanel Paris. Vỏ son bấm mở độc quyền với tiếng 'Click' danh giá, chất son lì như lụa nhung mịn màng chứa dầu jojoba và chiết xuất bơ hạt mỡ nâng niu bờ môi không tì vết.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1547887537-6158d64c35b3?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-dau", "da-kho" },
                    Ingredients = new() { ("squalane", "0.5%") },
                    HasVariants = true,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "192840/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH Chanel Việt Nam"
                },
                new()
                {
                    Sku = "SKU-LIP-LAN-040",
                    Barcode = "8935001202040",
                    Name = "Mặt Nạ Ngủ Dưỡng Ẩm Môi Mềm Mọng Laneige Lip Sleeping Mask Berry 20g",
                    Slug = "mat-na-ngu-moi-laneige-lip-sleeping-mask-berry-20g",
                    CatSlug = "son-moi",
                    BrandName = "Laneige",
                    CountryOfOrigin = "Hàn Quốc",
                    Unit = "Hũ",
                    Volume = "20g",
                    CostPrice = 240000m,
                    SellingPrice = 420000m,
                    DiscountPrice = 365000m,
                    ShortDescription = "Phức hợp quả mọng Berry Fruit Complex làm mềm và tan chảy tế bào da chết trên môi.",
                    Description = "Mặt nạ ngủ môi số 1 toàn cầu Laneige Lip Sleeping Mask hương quả mọng ngọt ngào. Công thức giàu Vitamin C và chất chống oxy hóa từ dâu tây, mâm xôi, việt quất nhẹ nhàng hòa tan lớp da khô bong tróc suốt giấc ngủ đêm, trả lại bờ môi căng đầy, ửng hồng mọng nước vào sáng hôm sau.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-kho", "da-nhay-cam", "da-thuong" },
                    Ingredients = new() { ("vitamin-c", "1%"), ("hyaluronic-acid", "0.5%") },
                    HasVariants = false,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "176540/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH Amorepacific Việt Nam"
                },
                new()
                {
                    Sku = "SKU-MAK-LAN-041",
                    Barcode = "8935001202041",
                    Name = "Phấn Nước Kiềm Dầu Mỏng Nhẹ Laneige Neo Cushion Matte SPF42 PA++ (15g x 2)",
                    Slug = "phan-nuoc-laneige-neo-cushion-matte-spf42-15g-x-2",
                    CatSlug = "kem-nen-cushion",
                    BrandName = "Laneige",
                    CountryOfOrigin = "Hàn Quốc",
                    Unit = "Hộp",
                    Volume = "15g x 2",
                    CostPrice = 480000m,
                    SellingPrice = 790000m,
                    DiscountPrice = 690000m,
                    ShortDescription = "Kiềm dầu 24h, kháng mồ hôi không lem khẩu trang và chống ánh sáng xanh màn hình.",
                    Description = "Laneige Neo Cushion Matte thiết kế bo tròn công thái học như chiếc hộp tai nghe thời thượng. Hạt phấn siêu vi che phủ khuyết điểm hoàn hảo nhưng mỏng nhẹ gấp đôi cushion thông thường, kiểm soát dầu thừa liên tục 24h và ngăn chặn tình trạng vỡ nền dưới thời tiết nóng ẩm.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1512496015851-a90fb38ba796?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1512496015851-a90fb38ba796?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1583241800698-e8ab01830a07?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-dau", "da-hon-hop" },
                    Ingredients = new() { ("panthenol-vitamin-b5", "0.5%"), ("niacinamide", "1%") },
                    HasVariants = true,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "165490/21/CBMP-QLD",
                    ImporterName = "Công ty TNHH Amorepacific Việt Nam"
                },
                new()
                {
                    Sku = "SKU-MAK-SUL-042",
                    Barcode = "8935001202042",
                    Name = "Phấn Nước Hoàng Gia Tinh Chất Nhân Sâm Sulwhasoo Perfecting Cushion SPF50+ (15g x 2)",
                    Slug = "phan-nuoc-sulwhasoo-perfecting-cushion-spf50-15g-x-2",
                    CatSlug = "kem-nen-cushion",
                    BrandName = "Sulwhasoo",
                    CountryOfOrigin = "Hàn Quốc",
                    Unit = "Hộp",
                    Volume = "15g x 2",
                    CostPrice = 890000m,
                    SellingPrice = 1550000m,
                    DiscountPrice = 1390000m,
                    ShortDescription = "Hợp chất dưỡng da JAUM Activator giúp lớp nền ẩm mượt không tì vết suốt cả ngày.",
                    Description = "Sulwhasoo Perfecting Cushion đại diện cho đỉnh cao trang điểm dưỡng da thảo dược hoàng gia Hàn Quốc. Lớp nền mỏng mịn tạo độ bóng khỏe tự nhiên từ sâu bên trong tế bào, che phủ nám sạm khuyết điểm hoàn hảo mà vẫn giữ làn da luôn được thở và ngậm nước cả ngày.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1512496015851-a90fb38ba796?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1512496015851-a90fb38ba796?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-kho", "da-hon-hop" },
                    Ingredients = new() { ("peptides", "1%"), ("hyaluronic-acid", "1%") },
                    HasVariants = true,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "194320/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH Amorepacific Việt Nam"
                },
                new()
                {
                    Sku = "SKU-MAK-EST-043",
                    Barcode = "8935001202043",
                    Name = "Kem Nền Bền Màu Suốt 24H Estée Lauder Double Wear Stay-in-Place SPF10 30ml",
                    Slug = "kem-nen-estee-lauder-double-wear-stay-in-place-30ml",
                    CatSlug = "kem-lot-kem-nen",
                    BrandName = "Estée Lauder",
                    CountryOfOrigin = "Mỹ",
                    Unit = "Chai",
                    Volume = "30ml",
                    CostPrice = 950000m,
                    SellingPrice = 1650000m,
                    DiscountPrice = 1480000m,
                    ShortDescription = "Khả năng che phủ tuyệt đối không xê dịch suốt 24H dưới thời tiết nắng nóng đổ dầu.",
                    Description = "Kem nền vương giả của mọi chuyên gia trang điểm thế giới. Estée Lauder Double Wear sở hữu công thức kiềm dầu đỉnh cao, chống trôi nước và mồ hôi bền bỉ 24 giờ. Lớp finish lì mịn không làm tắc nghẽn lỗ chân lông, không xuống tone và che sạch vết thâm mụn chỉ với một lớp mỏng.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1512496015851-a90fb38ba796?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-dau", "da-hon-hop" },
                    Ingredients = new() { ("vitamin-c", "0.2%") },
                    HasVariants = true,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "187420/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH Estée Lauder Việt Nam"
                },
                new()
                {
                    Sku = "SKU-MAK-LOR-044",
                    Barcode = "8935001202044",
                    Name = "Kem Nền Bắt Sáng Tự Nhiên L'Oréal Infallible 32H Fresh Wear Foundation 30ml",
                    Slug = "kem-nen-loreal-infallible-32h-fresh-wear-foundation-30ml",
                    CatSlug = "kem-lot-kem-nen",
                    BrandName = "L'Oréal Paris",
                    CountryOfOrigin = "Pháp",
                    Unit = "Chai",
                    Volume = "30ml",
                    CostPrice = 220000m,
                    SellingPrice = 369000m,
                    DiscountPrice = 319000m,
                    ShortDescription = "Công nghệ thoáng khí Oxy-Technology giữ lớp nền tươi mới rạng ngời suốt 32H.",
                    Description = "L'Oréal Infallible 32H Fresh Wear là dòng kem nền bán chạy hàng đầu phân khúc với hạt phấn siêu nhẹ cho phép làn da tự do hô hấp. Công thức kháng nước, kháng mồ hôi và chống oxy hóa giúp màu nền không bị xỉn màu suốt cả ngày dài làm việc năng động.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1512496015851-a90fb38ba796?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-dau", "da-hon-hop", "da-thuong" },
                    Ingredients = new() { ("vitamin-c", "0.2%") },
                    HasVariants = false,
                    IsFeatured = false,
                    CosmeticNotificationNumber = "154390/21/CBMP-QLD",
                    ImporterName = "Công ty TNHH L'Oréal Việt Nam"
                },
                new()
                {
                    Sku = "SKU-MAK-INN-045",
                    Barcode = "8935001202045",
                    Name = "Phấn Phủ Bột Khoáng Hút Dầu Tức Thì Innisfree No-Sebum Mineral Powder 5g",
                    Slug = "phan-phu-bot-khoang-innisfree-no-sebum-mineral-powder-5g",
                    CatSlug = "kem-nen-cushion",
                    BrandName = "Innisfree",
                    CountryOfOrigin = "Hàn Quốc",
                    Unit = "Hộp",
                    Volume = "5g",
                    CostPrice = 85000m,
                    SellingPrice = 150000m,
                    DiscountPrice = 129000m,
                    ShortDescription = "Khoáng chất núi lửa và bạc hà đảo Jeju thấm hút bã nhờn, giữ da ráo mịn tức thì.",
                    Description = "Hộp phấn bột nhỏ gọn không màu bán chạy nhất châu Á. Innisfree No-Sebum Mineral Powder chứa khoáng chất tự nhiên đảo ngọc Jeju kết hợp chiết xuất bạc hà tươi mát. Giúp cố định lớp trang điểm, hút sạch dầu thừa trên trán gò má và thậm chí dùng chữa cháy cho mái tóc bết dầu.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1512496015851-a90fb38ba796?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1512496015851-a90fb38ba796?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1527799820374-dcf8d9d4a388?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-dau", "da-hon-hop" },
                    Ingredients = new() { ("tea-tree-tram-tra", "0.1%") },
                    HasVariants = false,
                    IsFeatured = false,
                    CosmeticNotificationNumber = "132450/20/CBMP-QLD",
                    ImporterName = "Công ty TNHH Amorepacific Việt Nam"
                },
                new()
                {
                    Sku = "SKU-MAK-CAN-046",
                    Barcode = "8935001202046",
                    Name = "Phấn Phủ Nén Kiềm Dầu Tự Nhiên Canmake Marshmallow Finish Powder SPF50+ 10g",
                    Slug = "phan-phu-nen-canmake-marshmallow-finish-powder-spf50-10g",
                    CatSlug = "kem-nen-cushion",
                    BrandName = "Canmake",
                    CountryOfOrigin = "Nhật Bản",
                    Unit = "Hộp",
                    Volume = "10g",
                    CostPrice = 180000m,
                    SellingPrice = 310000m,
                    DiscountPrice = 269000m,
                    ShortDescription = "Tạo lớp da mịn như kẹo dẻo Marshmallow với 10 thành phần tinh chất dưỡng da Nhật Bản.",
                    Description = "Canmake Marshmallow Finish Powder là biểu tượng trang điểm trong trẻo chuẩn phong cách nữ sinh Tokyo. Hạt phấn nén siêu mềm xốp kết hợp chỉ số chống nắng SPF50+ PA+++ che phủ lỗ chân lông và vùng da không đều màu, đem lại làn da mềm mịn như kẹo bông mà không bị nặng mặt.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1512496015851-a90fb38ba796?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1512496015851-a90fb38ba796?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1583241800698-e8ab01830a07?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-dau", "da-hon-hop", "da-thuong" },
                    Ingredients = new() { ("squalane", "0.5%"), ("hyaluronic-acid", "0.2%") },
                    HasVariants = true,
                    IsFeatured = false,
                    CosmeticNotificationNumber = "143920/20/CBMP-QLD",
                    ImporterName = "Công ty CP Tập Đoàn Phú Thái"
                },
                new()
                {
                    Sku = "SKU-MAK-MAY-047",
                    Barcode = "8935001202047",
                    Name = "Kem Lót Kiềm Dầu Che Mờ Lỗ Chân Lông Maybelline Baby Skin Pore Eraser 22ml",
                    Slug = "kem-lot-maybelline-baby-skin-pore-eraser-22ml",
                    CatSlug = "kem-lot-kem-nen",
                    BrandName = "L'Oréal Paris",
                    CountryOfOrigin = "Mỹ",
                    Unit = "Tuýp",
                    Volume = "22ml",
                    CostPrice = 90000m,
                    SellingPrice = 165000m,
                    DiscountPrice = 139000m,
                    ShortDescription = "Chất gel trong suốt làm mờ lỗ chân lông tức thì, tạo bề mặt da láng mịn như da em bé.",
                    Description = "Maybelline Baby Skin Pore Eraser là bí quyết tạo nên lớp nền hoàn hảo không tì vết. Chất gel trong veo không màu lấp đầy các rãnh nhăn nông và lỗ chân lông to, đồng thời ngăn tiết dầu thừa giúp lớp kem nền lướt êm ái và bám chặt suốt nhiều giờ.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1512496015851-a90fb38ba796?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1556228722-d0b5d92fe77c?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-dau", "da-hon-hop" },
                    Ingredients = new() { ("vitamin-c", "0.1%") },
                    HasVariants = false,
                    IsFeatured = false,
                    CosmeticNotificationNumber = "120930/19/CBMP-QLD",
                    ImporterName = "Công ty TNHH L'Oréal Việt Nam"
                },
                new()
                {
                    Sku = "SKU-MAK-MAY-048",
                    Barcode = "8935001202048",
                    Name = "Mascara Chuốt Dài Mi Không Trôi Maybelline Lash Sensational Sky High Waterproof 6ml",
                    Slug = "mascara-maybelline-lash-sensational-sky-high-waterproof-6ml",
                    CatSlug = "trang-diem-mat-mascara",
                    BrandName = "L'Oréal Paris",
                    CountryOfOrigin = "Mỹ",
                    Unit = "Thỏi",
                    Volume = "6ml",
                    CostPrice = 130000m,
                    SellingPrice = 245000m,
                    DiscountPrice = 209000m,
                    ShortDescription = "Đầu cọ Flex Tower uốn cong linh hoạt kéo dài từng sợi mi đến tận ngọn, chống nước đỉnh cao.",
                    Description = "Mascara Sky High gây bão toàn cầu của Maybelline New York. Thiết kế đầu cọ tháp linh hoạt Flex Tower chuốt đều từng sợi mi từ gốc đến ngọn mà không vón cục. Chứa chiết xuất tre và sợi tơ fiber kéo dài mi bất tận, công thức chống thấm nước đỉnh cao giữ độ cong vút suốt 24H.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1516975080664-ed2fc6a32937?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1516975080664-ed2fc6a32937?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1583241800698-e8ab01830a07?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-dau", "da-kho" },
                    Ingredients = new() { ("panthenol-vitamin-b5", "0.2%") },
                    HasVariants = false,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "178920/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH L'Oréal Việt Nam"
                },
                new()
                {
                    Sku = "SKU-MAK-MAY-049",
                    Barcode = "8935001202049",
                    Name = "Bút Kẻ Mắt Nước Sắc Nét Chống Nước Maybelline Hyper Sharp Liner 0.5g",
                    Slug = "but-ke-mat-nuoc-maybelline-hyper-sharp-liner-05g",
                    CatSlug = "trang-diem-mat-mascara",
                    BrandName = "L'Oréal Paris",
                    CountryOfOrigin = "Mỹ",
                    Unit = "Cây",
                    Volume = "0.5g",
                    CostPrice = 95000m,
                    SellingPrice = 185000m,
                    DiscountPrice = 155000m,
                    ShortDescription = "Đầu bút siêu mảnh 0.01mm kẻ đường eyeliner sắc nét, mực đen bóng không lem 36h.",
                    Description = "Maybelline Hyper Sharp Liner với đầu cọ cải tiến gồm hàng trăm sợi lông tơ mềm mại đan kết tạo nét vẽ chính xác chỉ 0.01mm. Mực ra đều và đậm màu ngay nét đầu tiên, công nghệ màng phim chống thấm nước và dầu thừa trên mí mắt bền màu suốt cả ngày.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1516975080664-ed2fc6a32937?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1516975080664-ed2fc6a32937?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1583241800698-e8ab01830a07?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-dau", "da-kho" },
                    Ingredients = new() { ("vitamin-c", "0.1%") },
                    HasVariants = false,
                    IsFeatured = false,
                    CosmeticNotificationNumber = "143920/20/CBMP-QLD",
                    ImporterName = "Công ty TNHH L'Oréal Việt Nam"
                },
                new()
                {
                    Sku = "SKU-MAK-CAN-050",
                    Barcode = "8935001202050",
                    Name = "Má Hồng Dạng Kem Trong Veo Canmake Cream Cheek - #CL01 Clear Red Heart 2.2g",
                    Slug = "ma-hong-kem-canmake-cream-cheek-cl01-clear-red-heart-22g",
                    CatSlug = "ma-hong-bat-sang",
                    BrandName = "Canmake",
                    CountryOfOrigin = "Nhật Bản",
                    Unit = "Hộp",
                    Volume = "2.2g",
                    CostPrice = 120000m,
                    SellingPrice = 215000m,
                    DiscountPrice = 185000m,
                    ShortDescription = "Chất gel kem trong veo tan chảy trên da, gò má ửng hồng căng mọng tự nhiên.",
                    Description = "Canmake Cream Cheek #CL01 mang lại đôi gò má ửng hồng ngọt ngào như trái dâu tây tươi dưới nắng. Chất kem mềm mượt tan ngay khi chạm vào da và nhanh chóng chuyển thành lớp finish ráo mịn, không gây bết dính và giữ sắc màu tươi tắn tự nhiên suốt 8 tiếng.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1583241800698-e8ab01830a07?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1583241800698-e8ab01830a07?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1512496015851-a90fb38ba796?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-kho", "da-hon-hop" },
                    Ingredients = new() { ("squalane", "0.5%") },
                    HasVariants = true,
                    IsFeatured = false,
                    CosmeticNotificationNumber = "128940/20/CBMP-QLD",
                    ImporterName = "Công ty CP Tập Đoàn Phú Thái"
                },
                new()
                {
                    Sku = "SKU-MAK-ROM-051",
                    Barcode = "8935001202051",
                    Name = "Bảng Phấn Mắt 4 Ô Thuần Khiết Rom&nd Better Than Eyes - #01 Dry Mango Tulip 6.5g",
                    Slug = "bang-phan-mat-romand-better-than-eyes-01-dry-mango-tulip-65g",
                    CatSlug = "trang-diem-mat-mascara",
                    BrandName = "Rom&nd",
                    CountryOfOrigin = "Hàn Quốc",
                    Unit = "Hộp",
                    Volume = "6.5g",
                    CostPrice = 175000m,
                    SellingPrice = 295000m,
                    DiscountPrice = 249000m,
                    ShortDescription = "Bảng mắt 4 ô tông cam đào san hô ngọt ngào, chất phấn nhũ ngọc trai lấp lánh như sương.",
                    Description = "Rom&nd Better Than Eyes #01 Dry Mango Tulip gồm 3 ô màu lì mịn như bột sữa và 1 ô nhũ ngọc trai siêu lấp lánh. Tông màu cam đào san hô ấm áp cực kỳ dễ phối đồ, bám màu chắc chắn không bị bay bụi phấn và tôn lên ánh nhìn trong trẻo ngây thơ chuẩn Hàn.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1583241800698-e8ab01830a07?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1583241800698-e8ab01830a07?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1516975080664-ed2fc6a32937?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-dau", "da-kho" },
                    Ingredients = new() { ("vitamin-c", "0.1%") },
                    HasVariants = true,
                    IsFeatured = false,
                    CosmeticNotificationNumber = "134210/20/CBMP-QLD",
                    ImporterName = "Công ty TNHH B2Link Việt Nam"
                },
                new()
                {
                    Sku = "SKU-MAK-URB-052",
                    Barcode = "8935001202052",
                    Name = "Xịt Khóa Lớp Trang Điểm Kiềm Dầu 16H Urban Decay All Nighter Setting Spray 118ml",
                    Slug = "xit-khoa-nen-urban-decay-all-nighter-setting-spray-118ml",
                    CatSlug = "xit-khoa-nen",
                    BrandName = "L'Oréal Paris",
                    CountryOfOrigin = "Mỹ",
                    Unit = "Chai",
                    Volume = "118ml",
                    CostPrice = 550000m,
                    SellingPrice = 920000m,
                    DiscountPrice = 820000m,
                    ShortDescription = "Công nghệ kiểm soát nhiệt độ Temperature Control giữ lớp make up không xê dịch 16H.",
                    Description = "Xịt khóa nền huyền thoại được ca ngợi bởi giới chuyên gia toàn cầu. Công nghệ Temperature Control độc quyền làm hạ nhiệt độ bề mặt trang điểm, ngăn chặn hiện tượng mồ hôi và dầu làm chảy nền. Phun sương siêu mịn không đọng giọt nước, giữ lớp nền tinh tươm suốt 16H.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1571781926291-c477ebfd024b?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1571781926291-c477ebfd024b?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1512496015851-a90fb38ba796?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-dau", "da-hon-hop" },
                    Ingredients = new() { ("panthenol-vitamin-b5", "0.5%") },
                    HasVariants = false,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "176540/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH L'Oréal Việt Nam"
                },
                new()
                {
                    Sku = "SKU-MAK-MAC-053",
                    Barcode = "8935001202053",
                    Name = "Xịt Cố Định Nền Dưỡng Ẩm Bắt Sáng MAC Prep + Prime Fix+ 100ml",
                    Slug = "xit-co-dinh-nen-mac-prep-prime-fix-plus-100ml",
                    CatSlug = "xit-khoa-nen",
                    BrandName = "MAC Cosmetics",
                    CountryOfOrigin = "Mỹ",
                    Unit = "Chai",
                    Volume = "100ml",
                    CostPrice = 480000m,
                    SellingPrice = 820000m,
                    DiscountPrice = 720000m,
                    ShortDescription = "Cung cấp độ ẩm tức thì với trà xanh, hoa cúc và dưa chuột giúp nền tiệp da hoàn hảo.",
                    Description = "MAC Fix+ là bảo bối hậu trường sàn diễn thời trang danh giá. Chứa trà xanh giàu chất chống oxy hóa, hoa cúc làm dịu và dưa leo mát lạnh giúp làm dịu da, làm tan hiện tượng cakey mốc nền, giúp các lớp phấn hòa quyện vào da tự nhiên như làn da thứ hai.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1571781926291-c477ebfd024b?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1571781926291-c477ebfd024b?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-kho", "da-thuong", "da-nhay-cam" },
                    Ingredients = new() { ("panthenol-vitamin-b5", "1%"), ("hyaluronic-acid", "0.5%") },
                    HasVariants = false,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "154983/21/CBMP-QLD",
                    ImporterName = "Công ty TNHH Estée Lauder Việt Nam"
                },
                new()
                {
                    Sku = "SKU-MAK-DIO-054",
                    Barcode = "8935001202054",
                    Name = "Kem Lót Bắt Sáng Căng Mọng Dior Backstage Face & Body Primer 50ml",
                    Slug = "kem-lot-dior-backstage-face-and-body-primer-50ml",
                    CatSlug = "kem-lot-kem-nen",
                    BrandName = "Dior",
                    CountryOfOrigin = "Pháp",
                    Unit = "Chai",
                    Volume = "50ml",
                    CostPrice = 720000m,
                    SellingPrice = 1250000m,
                    DiscountPrice = 1150000m,
                    ShortDescription = "Tạo hiệu ứng làm mờ khuyết điểm và bắt sáng căng mọng làn da chuẩn sàn runway.",
                    Description = "Dior Backstage Face & Body Primer là bí quyết tạo nên làn da trong suốt của các người mẫu sàn diễn thời trang Paris. Chứa phức hợp làm mờ quang học và tinh chất bơ hạt mỡ dưỡng ẩm 24h, sản phẩm giúp làm phẳng bề mặt da, giữ màu kem nền chuẩn xác và bắt sáng rạng ngời dưới ánh đèn.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1512496015851-a90fb38ba796?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1512290900672-1f02e6a0d33e?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-kho", "da-hon-hop" },
                    Ingredients = new() { ("squalane", "0.5%"), ("hyaluronic-acid", "0.2%") },
                    HasVariants = false,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "194091/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH LVMH Parfums & Cosmetiques Việt Nam"
                },
                new()
                {
                    Sku = "SKU-MAK-DIO-055",
                    Barcode = "8935001202055",
                    Name = "Bảng Phấn Bắt Sáng 4 Ô Ngọc Trai Dior Backstage Glow Face Palette - #001 Universal 10g",
                    Slug = "bang-phan-bat-sang-dior-backstage-glow-face-palette-001-universal-10g",
                    CatSlug = "ma-hong-bat-sang",
                    BrandName = "Dior",
                    CountryOfOrigin = "Pháp",
                    Unit = "Hộp",
                    Volume = "10g",
                    CostPrice = 890000m,
                    SellingPrice = 1550000m,
                    DiscountPrice = 1390000m,
                    ShortDescription = "4 ô màu ngọc trai đa chiều: Trắng tinh khôi, Vàng đồng, Hồng ánh nhũ và Đồng ấm áp.",
                    Description = "Dior Backstage Glow Face Palette #001 Universal là bảng phấn highlight huyền thoại của mọi tín đồ mê trang điểm phát sáng. Hạt phấn ngọc trai siêu mịn tan chảy trên da tạo hiệu ứng bắt sáng đa chiều từ nhẹ nhàng thanh lịch đến rạng rỡ lộng lẫy dưới ánh đèn đêm tiệc sang trọng.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1583241800698-e8ab01830a07?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1583241800698-e8ab01830a07?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1512496015851-a90fb38ba796?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-dau", "da-kho" },
                    Ingredients = new() { ("vitamin-c", "0.1%") },
                    HasVariants = true,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "194092/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH LVMH Parfums & Cosmetiques Việt Nam"
                },

                // =========================================================================
                // III. NHÓM NƯỚC HOA CAO CẤP HAUTE PARFUMERIE (15 SẢN PHẨM THỰC TẾ)
                // =========================================================================
                new()
                {
                    Sku = "SKU-PERF-DIO-056",
                    Barcode = "8935001202056",
                    Name = "Nước Hoa Nam Quyến Rũ Mạnh Mẽ Dior Sauvage Eau De Parfum 100ml",
                    Slug = "nuoc-hoa-nam-dior-sauvage-eau-de-parfum-100ml",
                    CatSlug = "nuoc-hoa-nam",
                    BrandName = "Dior",
                    CountryOfOrigin = "Pháp",
                    Unit = "Chai",
                    Volume = "100ml",
                    CostPrice = 2300000m,
                    SellingPrice = 3950000m,
                    DiscountPrice = 3590000m,
                    ShortDescription = "Nốt hương cam Bergamot Calabria cay nồng quyện cùng hương gỗ tuyết tùng và vani Papua.",
                    Description = "Dior Sauvage EDP là tuyệt phẩm hương thơm nam giới được săn đón bậc nhất hành tinh. Lấy cảm hứng từ sa mạc lúc hoàng hôn huyền bí, nốt hương mở đầu thanh mát của cam Bergamot bùng nổ, tiếp nối bằng hạt tiêu Tứ Xuyên cay nhẹ và khép lại với hương vani ngọt ngào quyện cùng hổ phách ấm áp.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1523293182086-7651a899d37f?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1523293182086-7651a899d37f?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1547887537-6158d64c35b3?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-dau", "da-kho" },
                    Ingredients = new() { },
                    HasVariants = false,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "194093/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH LVMH Parfums & Cosmetiques Việt Nam"
                },
                new()
                {
                    Sku = "SKU-PERF-CHA-057",
                    Barcode = "8935001202057",
                    Name = "Nước Hoa Nữ Quý Phái Thanh Lịch Chanel Coco Mademoiselle EDP 100ml",
                    Slug = "nuoc-hoa-nu-chanel-coco-mademoiselle-edp-100ml",
                    CatSlug = "nuoc-hoa-nu",
                    BrandName = "Chanel",
                    CountryOfOrigin = "Pháp",
                    Unit = "Chai",
                    Volume = "100ml",
                    CostPrice = 2600000m,
                    SellingPrice = 4450000m,
                    DiscountPrice = 4090000m,
                    ShortDescription = "Hương thơm hoa cỏ phương Đông sống động: Cam ngọt, Hoa hồng Grasse và Hoắc hương quý phái.",
                    Description = "Chanel Coco Mademoiselle là bản tuyên ngôn của người phụ nữ độc lập và thanh lịch kiểu Paris. Mở đầu bằng nốt cam quýt tươi mát, dẫn lối đến tầng hương giữa của hoa hồng Thổ Nhĩ Kỳ và hoa nhài Grasse, đọng lại lâu bền trên nền hoắc hương và đậu tonka nồng nàn quyến rũ.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1547887537-6158d64c35b3?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1547887537-6158d64c35b3?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1523293182086-7651a899d37f?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-dau", "da-kho" },
                    Ingredients = new() { },
                    HasVariants = false,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "192841/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH Chanel Việt Nam"
                },
                new()
                {
                    Sku = "SKU-PERF-CHA-058",
                    Barcode = "8935001202058",
                    Name = "Nước Hoa Nữ Biểu Tượng Kinh Điển Chanel N°5 Eau De Parfum 100ml",
                    Slug = "nuoc-hoa-nu-chanel-n5-eau-de-parfum-100ml",
                    CatSlug = "nuoc-hoa-nu",
                    BrandName = "Chanel",
                    CountryOfOrigin = "Pháp",
                    Unit = "Chai",
                    Volume = "100ml",
                    CostPrice = 2650000m,
                    SellingPrice = 4500000m,
                    DiscountPrice = 4150000m,
                    ShortDescription = "Tuyệt phẩm mùi hương vượt thời gian với nốt hương Aldehyde, Ngọc Lan Tây và Gỗ đàn hương.",
                    Description = "Chai nước hoa nổi tiếng nhất lịch sử nhân loại ra mắt năm 1921 bởi Coco Chanel. Chanel N°5 là sự hòa quyện phức hợp của hơn 80 hương liệu quý hiếm, mở ra bằng hợp chất aldehyde phát sáng, tôn vinh trái tim hoa hồng tháng Năm và hoa nhài, kết thúc bằng xạ hương và vani Bourbon quý phái.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1547887537-6158d64c35b3?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1547887537-6158d64c35b3?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1523293182086-7651a899d37f?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-dau", "da-kho" },
                    Ingredients = new() { },
                    HasVariants = false,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "192842/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH Chanel Việt Nam"
                },
                new()
                {
                    Sku = "SKU-PERF-YSL-059",
                    Barcode = "8935001202059",
                    Name = "Nước Hoa Nữ Tự Do Nồng Nàn Yves Saint Laurent Libre Eau De Parfum 90ml",
                    Slug = "nuoc-hoa-nu-ysl-libre-eau-de-parfum-90ml",
                    CatSlug = "nuoc-hoa-nu",
                    BrandName = "Yves Saint Laurent",
                    CountryOfOrigin = "Pháp",
                    Unit = "Chai",
                    Volume = "90ml",
                    CostPrice = 2100000m,
                    SellingPrice = 3650000m,
                    DiscountPrice = 3290000m,
                    ShortDescription = "Hương thơm hoa oải hương Pháp nồng nàn hòa quyện cùng hoa cam Morocco và Vani Madagascar.",
                    Description = "YSL Libre tôn vinh tinh thần 'Tự Do' mãnh liệt của phái đẹp. Sự đối lập quyến rũ giữa sự gợi cảm của hoa cam Morocco nồng ấm và tính nam tính bất khuất của hoa oải hương Pháp, bọc trong lớp vỏ chai khắc logo YSL mạ vàng vát chéo kinh điển.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1547887537-6158d64c35b3?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1523293182086-7651a899d37f?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-dau", "da-kho" },
                    Ingredients = new() { },
                    HasVariants = false,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "187421/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH L'Oréal Việt Nam"
                },
                new()
                {
                    Sku = "SKU-PERF-YSL-060",
                    Barcode = "8935001202060",
                    Name = "Nước Hoa Nữ Gây Nghiện Ngọt Ngào Yves Saint Laurent Black Opium EDP 90ml",
                    Slug = "nuoc-hoa-nu-ysl-black-opium-edp-90ml",
                    CatSlug = "nuoc-hoa-nu",
                    BrandName = "Yves Saint Laurent",
                    CountryOfOrigin = "Pháp",
                    Unit = "Chai",
                    Volume = "90ml",
                    CostPrice = 2050000m,
                    SellingPrice = 3550000m,
                    DiscountPrice = 3190000m,
                    ShortDescription = "Cú sốc năng lượng từ cà phê đen, hoa cam trắng và vani ngọt ngào gây nghiện.",
                    Description = "YSL Black Opium là liều thuốc gây nghiện của những đêm tiệc cuồng nhiệt. Hương cà phê đen nồng nàn bốc tỏa ngay từ giây đầu tiên, xen lẫn vị ngọt béo của vani và nét nữ tính của hoa trắng, đựng trong thân chai kim tuyến đen huyền ảo bí ẩn.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1547887537-6158d64c35b3?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1523293182086-7651a899d37f?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-dau", "da-kho" },
                    Ingredients = new() { },
                    HasVariants = false,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "187422/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH L'Oréal Việt Nam"
                },
                new()
                {
                    Sku = "SKU-PERF-DIO-061",
                    Barcode = "8935001202061",
                    Name = "Nước Hoa Nữ Tiểu Thư Ngọt Ngào Dior Miss Dior Eau De Parfum 100ml",
                    Slug = "nuoc-hoa-nu-dior-miss-dior-eau-de-parfum-100ml",
                    CatSlug = "nuoc-hoa-nu",
                    BrandName = "Dior",
                    CountryOfOrigin = "Pháp",
                    Unit = "Chai",
                    Volume = "100ml",
                    CostPrice = 2350000m,
                    SellingPrice = 3950000m,
                    DiscountPrice = 3590000m,
                    ShortDescription = "Bản giao hưởng hoa cỏ ngàn hoa 'Millefiori' với hoa hồng Grasse và chiếc nơ Couture dệt tay.",
                    Description = "Miss Dior EDP như một bó hoa ngàn sắc nở rộ rực rỡ. Nốt hoa hồng Centifolia với hương vị ngọt cay tinh tế, hoa chuông thung lũng thanh khiết và hoa mẫu đơn quý phái. Cổ chai điểm xuyết chiếc nơ vải dệt thủ công độc bản từ những xưởng dệt lụa lâu đời nhất nước Pháp.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1547887537-6158d64c35b3?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1523293182086-7651a899d37f?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-dau", "da-kho" },
                    Ingredients = new() { },
                    HasVariants = false,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "194094/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH LVMH Parfums & Cosmetiques Việt Nam"
                },
                new()
                {
                    Sku = "SKU-PERF-CHA-062",
                    Barcode = "8935001202062",
                    Name = "Nước Hoa Nam Lịch Lãm Đẳng Cấp Chanel Bleu De Chanel EDP 100ml",
                    Slug = "nuoc-hoa-nam-chanel-bleu-de-chanel-edp-100ml",
                    CatSlug = "nuoc-hoa-nam",
                    BrandName = "Chanel",
                    CountryOfOrigin = "Pháp",
                    Unit = "Chai",
                    Volume = "100ml",
                    CostPrice = 2500000m,
                    SellingPrice = 4250000m,
                    DiscountPrice = 3890000m,
                    ShortDescription = "Hương thơm gỗ thơm nồng nàn: Bưởi tây, Gỗ tuyết tùng và Hổ phách quý phái.",
                    Description = "Bleu De Chanel là kiệt tác hương thơm định nghĩa người đàn ông bản lĩnh và tự do. Mở đầu bằng sự bùng nổ tươi mát của bưởi tây và bạc hà, tiếp nối bằng nhịp đập ấm áp của gừng và nhục đậu khấu, đọng lại sâu lắng trên nền gỗ tuyết tùng New Caledonia và hổ phách sang trọng.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1523293182086-7651a899d37f?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1523293182086-7651a899d37f?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1547887537-6158d64c35b3?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-dau", "da-kho" },
                    Ingredients = new() { },
                    HasVariants = false,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "192843/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH Chanel Việt Nam"
                },
                new()
                {
                    Sku = "SKU-PERF-YSL-063",
                    Barcode = "8935001202063",
                    Name = "Nước Hoa Nam Cá Tính Hiện Đại Yves Saint Laurent Y Eau De Parfum 100ml",
                    Slug = "nuoc-hoa-nam-ysl-y-eau-de-parfum-100ml",
                    CatSlug = "nuoc-hoa-nam",
                    BrandName = "Yves Saint Laurent",
                    CountryOfOrigin = "Pháp",
                    Unit = "Chai",
                    Volume = "100ml",
                    CostPrice = 2100000m,
                    SellingPrice = 3600000m,
                    DiscountPrice = 3250000m,
                    ShortDescription = "Sự đối lập giữa Táo xanh giòn ngọt, Xô thơm thanh mát và Đậu Tonka trầm ấm.",
                    Description = "YSL Y Eau De Parfum đại diện cho thế hệ người đàn ông dám ước mơ và không ngừng kiến tạo. Mùi hương fougère hiện đại mở ra với nốt táo xanh giòn tan và gừng cay nồng, tầng hương giữa thanh tao của xô thơm và phong lữ, lưu lại nốt hương gỗ và đậu tonka bền bỉ suốt 12H.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1523293182086-7651a899d37f?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1523293182086-7651a899d37f?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1547887537-6158d64c35b3?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-dau", "da-kho" },
                    Ingredients = new() { },
                    HasVariants = false,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "187423/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH L'Oréal Việt Nam"
                },
                new()
                {
                    Sku = "SKU-PERF-LAN-064",
                    Barcode = "8935001202064",
                    Name = "Nước Hoa Nữ Nụ Cười Pha Lê Lancôme La Vie Est Belle EDP 100ml",
                    Slug = "nuoc-hoa-nu-lancome-la-vie-est-belle-edp-100ml",
                    CatSlug = "nuoc-hoa-nu",
                    BrandName = "Lancôme",
                    CountryOfOrigin = "Pháp",
                    Unit = "Chai",
                    Volume = "100ml",
                    CostPrice = 1950000m,
                    SellingPrice = 3350000m,
                    DiscountPrice = 2990000m,
                    ShortDescription = "Hương thơm ngọt ngào của Hoa Diên Vĩ Pháp, Kẹo Praline béo ngậy và Hoắc hương.",
                    Description = "La Vie Est Belle mang thông điệp 'Cuộc sống tươi đẹp' với nụ cười pha lê chạm khắc nơi đáy chai. Được tinh chế bởi ba bậc thầy nước hoa hàng đầu nước Pháp, nốt hoa diên vĩ Pallida quý giá hòa quyện cùng đường caramel ngọt lịm và hoa cam tỏa sáng đem lại nguồn năng lượng tích cực bất tận.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1547887537-6158d64c35b3?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1512290900672-1f02e6a0d33e?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-dau", "da-kho" },
                    Ingredients = new() { },
                    HasVariants = false,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "198742/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH L'Oréal Việt Nam"
                },
                new()
                {
                    Sku = "SKU-PERF-JOM-065",
                    Barcode = "8935001202065",
                    Name = "Nước Hoa Unisex Thanh Lịch Jo Malone London English Pear & Freesia Cologne 100ml",
                    Slug = "nuoc-hoa-unisex-jo-malone-english-pear-and-freesia-100ml",
                    CatSlug = "nuoc-hoa-unisex",
                    BrandName = "Jo Malone London",
                    CountryOfOrigin = "Anh",
                    Unit = "Chai",
                    Volume = "100ml",
                    CostPrice = 2150000m,
                    SellingPrice = 3750000m,
                    DiscountPrice = 3390000m,
                    ShortDescription = "Hương lê chín mọng mùa thu nước Anh kết hợp hoa lan Nam Phi trắng tinh khôi.",
                    Description = "Mùi hương kinh điển được yêu thích nhất của thương hiệu Jo Malone London. Gợi nhớ tiết trời mùa thu êm ả tại vùng đồng quê nước Anh với quả lê William mọng nước, bao bọc bởi hương thơm mong manh của hoa lan Nam Phi trắng và làm ấm lại bằng hoắc hương và hổ phách dịu êm.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1594035910387-fea47794261f?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1594035910387-fea47794261f?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1547887537-6158d64c35b3?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-dau", "da-kho" },
                    Ingredients = new() { },
                    HasVariants = false,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "165490/21/CBMP-QLD",
                    ImporterName = "Công ty TNHH Estée Lauder Việt Nam"
                },
                new()
                {
                    Sku = "SKU-PERF-JOM-066",
                    Barcode = "8935001202066",
                    Name = "Nước Hoa Unisex Gió Biển Jo Malone London Wood Sage & Sea Salt 100ml",
                    Slug = "nuoc-hoa-unisex-jo-malone-wood-sage-and-sea-salt-100ml",
                    CatSlug = "nuoc-hoa-unisex",
                    BrandName = "Jo Malone London",
                    CountryOfOrigin = "Anh",
                    Unit = "Chai",
                    Volume = "100ml",
                    CostPrice = 2150000m,
                    SellingPrice = 3750000m,
                    DiscountPrice = 3390000m,
                    ShortDescription = "Hương gió biển mặn mòi, khoáng đạt hòa cùng xô thơm và gỗ lũa hoang sơ.",
                    Description = "Trốn thoát khỏi ồn ào thành thị đến với bờ biển lộng gió nước Anh. Jo Malone Wood Sage & Sea Salt mang lại cảm giác sảng khoái tinh thần tuyệt đối với vị muối biển mằn mặn, hòa cùng hương đất mộc mạc của cây xô thơm và hạt ambrette ấm áp, tạo nên phong thái khoáng đạt tự tại.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1594035910387-fea47794261f?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1594035910387-fea47794261f?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1547887537-6158d64c35b3?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-dau", "da-kho" },
                    Ingredients = new() { },
                    HasVariants = false,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "165491/21/CBMP-QLD",
                    ImporterName = "Công ty TNHH Estée Lauder Việt Nam"
                },
                new()
                {
                    Sku = "SKU-PERF-TOM-067",
                    Barcode = "8935001202067",
                    Name = "Nước Hoa Unisex Quyền Lực Bí Ẩn Tom Ford Black Orchid EDP 50ml",
                    Slug = "nuoc-hoa-unisex-tom-ford-black-orchid-edp-50ml",
                    CatSlug = "nuoc-hoa-unisex",
                    BrandName = "Estée Lauder",
                    CountryOfOrigin = "Mỹ",
                    Unit = "Chai",
                    Volume = "50ml",
                    CostPrice = 220000m,
                    SellingPrice = 3850000m,
                    DiscountPrice = 3490000m,
                    ShortDescription = "Hương hoa phong lan đen hiếm có, nấm truffle đen, socola đen và hoắc hương ma mị.",
                    Description = "Tom Ford Black Orchid là biểu tượng của sự quyến rũ đen tối và xa hoa đỉnh điểm. Tác phẩm mùi hương mở đầu bằng nấm truffle cay nồng và nho đen Hy Lạp, bùng nổ hương hoa phong lan đen độc bản và kết thúc bằng socola đắng, hoắc hương đậm sâu không thể nào quên.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1523293182086-7651a899d37f?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1523293182086-7651a899d37f?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1547887537-6158d64c35b3?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-dau", "da-kho" },
                    Ingredients = new() { },
                    HasVariants = false,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "154984/21/CBMP-QLD",
                    ImporterName = "Công ty TNHH Estée Lauder Việt Nam"
                },
                new()
                {
                    Sku = "SKU-PERF-DIO-068",
                    Barcode = "8935001202068",
                    Name = "Bộ Quà Tặng Nước Hoa Nữ Cao Cấp Dior Miss Dior Miniature Giftset (4 x 5ml)",
                    Slug = "bo-qua-tang-nuoc-hoa-dior-miss-dior-miniature-giftset-4x5ml",
                    CatSlug = "giftset-nuoc-hoa",
                    BrandName = "Dior",
                    CountryOfOrigin = "Pháp",
                    Unit = "Hộp",
                    Volume = "4 x 5ml",
                    CostPrice = 1100000m,
                    SellingPrice = 1950000m,
                    DiscountPrice = 1750000m,
                    ShortDescription = "Hộp quà tặng xa xỉ gồm 4 tuyệt phẩm nước hoa Miss Dior biểu tượng trong phiên bản mini.",
                    Description = "Dior Miss Dior Miniature Giftset là món quà sang trọng tinh tế dành tặng phái đẹp. Hộp quà mang họa tiết hoàng gia Pháp chứa 4 chai nước hoa mini 5ml: Miss Dior EDP, Blooming Bouquet, Rose N'Roses và Miss Dior Parfum cho nàng thỏa sức thay đổi mùi hương theo từng cung bậc cảm xúc.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1512290900672-1f02e6a0d33e?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1512290900672-1f02e6a0d33e?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1547887537-6158d64c35b3?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-dau", "da-kho" },
                    Ingredients = new() { },
                    HasVariants = false,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "194095/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH LVMH Parfums & Cosmetiques Việt Nam"
                },
                new()
                {
                    Sku = "SKU-PERF-CHA-069",
                    Barcode = "8935001202069",
                    Name = "Nước Hoa Bỏ Túi Xa Xỉ Chanel Coco Mademoiselle Purse Spray (3 x 20ml)",
                    Slug = "nuoc-hoa-bo-tui-chanel-coco-mademoiselle-purse-spray-3x20ml",
                    CatSlug = "nuoc-hoa-mini",
                    BrandName = "Chanel",
                    CountryOfOrigin = "Pháp",
                    Unit = "Hộp",
                    Volume = "3 x 20ml",
                    CostPrice = 1950000m,
                    SellingPrice = 3350000m,
                    DiscountPrice = 3050000m,
                    ShortDescription = "Phiên bản vỏ xoay mạ vàng bỏ túi sang trọng tiện lợi, kèm 2 ống refill thay thế.",
                    Description = "Chanel Coco Mademoiselle Purse Spray được thiết kế dành riêng cho những chuyến du lịch và dạ tiệc xa hoa. Vỏ son xoay sơn mài trắng viền vàng quý phái nhỏ gọn trong túi xách. Kèm theo 3 ống tinh chất nước hoa 20ml giúp nàng luôn lưu giữ hương thơm quyến rũ mọi lúc mọi nơi.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1588405748880-12d1d2a59f75?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1588405748880-12d1d2a59f75?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1547887537-6158d64c35b3?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-dau", "da-kho" },
                    Ingredients = new() { },
                    HasVariants = false,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "192844/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH Chanel Việt Nam"
                },
                new()
                {
                    Sku = "SKU-PERF-MAI-070",
                    Barcode = "8935001202070",
                    Name = "Nước Hoa Unisex Hương Rượu Gỗ Maison Margiela Replica Jazz Club EDT 100ml",
                    Slug = "nuoc-hoa-unisex-replica-jazz-club-edt-100ml",
                    CatSlug = "nuoc-hoa-unisex",
                    BrandName = "L'Oréal Paris",
                    CountryOfOrigin = "Pháp",
                    Unit = "Chai",
                    Volume = "100ml",
                    CostPrice = 2050000m,
                    SellingPrice = 3500000m,
                    DiscountPrice = 3150000m,
                    ShortDescription = "Tái hiện không gian câu lạc bộ nhạc Jazz Brooklyn với rượu Rum, xì gà và hạt vani.",
                    Description = "Maison Margiela Replica Jazz Club tái hiện sống động ký ức của một quán bar nhạc Jazz ấm áp tại khu Brooklyn New York. Mở ra với nốt rượu Rum cay nồng nàn và hạt tiêu hồng, lan tỏa làn khói lá xì gà thượng hạng và kết thúc bằng nốt hương gỗ bồ đề và vani ngọt ngào sâu lắng.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1523293182086-7651a899d37f?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1547887537-6158d64c35b3?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-dau", "da-kho" },
                    Ingredients = new() { },
                    HasVariants = false,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "176541/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH L'Oréal Việt Nam"
                },

                // =========================================================================
                // IV. NHÓM CHĂM SÓC CƠ THỂ, TÓC & THỰC PHẨM LÀM ĐẸP (10 SẢN PHẨM THỰC TẾ)
                // =========================================================================
                new()
                {
                    Sku = "SKU-HAIR-MOR-071",
                    Barcode = "8935001202071",
                    Name = "Tinh Dầu Dưỡng Tóc Phục Hồi Hư Tổn Moroccanoil Treatment Original 100ml",
                    Slug = "tinh-dau-duong-toc-moroccanoil-treatment-original-100ml",
                    CatSlug = "dau-duong-serum-toc",
                    BrandName = "Shiseido",
                    CountryOfOrigin = "Israel",
                    Unit = "Chai",
                    Volume = "100ml",
                    CostPrice = 550000m,
                    SellingPrice = 950000m,
                    DiscountPrice = 850000m,
                    ShortDescription = "Tinh dầu Argan nguyên chất nuôi dưỡng tóc bóng mượt tức thì, rút ngắn thời gian sấy 50%.",
                    Description = "Moroccanoil Treatment là sản phẩm tiên phong trong lĩnh vực chăm sóc tóc bằng tinh dầu quý. Chứa dầu argan giàu chất chống oxy hóa và protein nuôi dưỡng, sản phẩm thẩm thấu tức thì vào từng sợi tóc hư tổn do uốn nhuộm, gỡ rối hoàn hảo và mang lại độ bóng khỏe rực rỡ suốt ngày dài.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1527799820374-dcf8d9d4a388?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1527799820374-dcf8d9d4a388?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1535585209827-a15fcdbc4c2d?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1608248597359-009848520330?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-kho" },
                    Ingredients = new() { ("argan-oil", "5%"), ("squalane", "1%") },
                    HasVariants = false,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "143095/20/CBMP-QLD",
                    ImporterName = "Công ty TNHH Sáng Tạo Phong Cách Sống"
                },
                new()
                {
                    Sku = "SKU-HAIR-MOR-072",
                    Barcode = "8935001202072",
                    Name = "Dầu Gội Dưỡng Ẩm Phục Hồi Tóc Khô Xơ Moroccanoil Hydrating Shampoo 250ml",
                    Slug = "dau-goi-duong-am-moroccanoil-hydrating-shampoo-250ml",
                    CatSlug = "dau-goi-dau-xa",
                    BrandName = "Shiseido",
                    CountryOfOrigin = "Israel",
                    Unit = "Chai",
                    Volume = "250ml",
                    CostPrice = 320000m,
                    SellingPrice = 560000m,
                    DiscountPrice = 495000m,
                    ShortDescription = "Cung cấp độ ẩm dồi dào với dầu Argan, Tảo đỏ và Vitamin A, E cho mái tóc suôn mềm.",
                    Description = "Moroccanoil Hydrating Shampoo là giải pháp làm sạch nhẹ nhàng và bù nước chuyên sâu cho mái tóc khô xơ thiếu sức sống. Công thức không chứa sunfat, phốt phát và paraben, an toàn cho cả tóc nhuộm màu, giúp cân bằng độ ẩm tự nhiên của sợi tóc và da đầu.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1535585209827-a15fcdbc4c2d?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1535585209827-a15fcdbc4c2d?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1527799820374-dcf8d9d4a388?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-kho" },
                    Ingredients = new() { ("argan-oil", "2%"), ("panthenol-vitamin-b5", "1%") },
                    HasVariants = false,
                    IsFeatured = false,
                    CosmeticNotificationNumber = "143096/20/CBMP-QLD",
                    ImporterName = "Công ty TNHH Sáng Tạo Phong Cách Sống"
                },
                new()
                {
                    Sku = "SKU-HAIR-MOR-073",
                    Barcode = "8935001202073",
                    Name = "Dầu Xả Dưỡng Ẩm Phục Hồi Tóc Khô Xơ Moroccanoil Hydrating Conditioner 250ml",
                    Slug = "dau-xa-duong-am-moroccanoil-hydrating-conditioner-250ml",
                    CatSlug = "dau-goi-dau-xa",
                    BrandName = "Shiseido",
                    CountryOfOrigin = "Israel",
                    Unit = "Chai",
                    Volume = "250ml",
                    CostPrice = 340000m,
                    SellingPrice = 580000m,
                    DiscountPrice = 515000m,
                    ShortDescription = "Khóa chặt dưỡng chất và độ ẩm trong thân tóc, chống gãy rụng và chẻ ngọn.",
                    Description = "Dầu xả Moroccanoil Hydrating Conditioner bổ sung lượng ẩm bị mất đi do môi trường và tạo kiểu nhiệt. Nhẹ nhàng gỡ rối, phục hồi các sợi tóc khô yếu trở nên bóng khỏe, dễ chải và tràn ngập hương thơm hổ phách phương Đông nồng nàn quyến rũ.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1535585209827-a15fcdbc4c2d?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1535585209827-a15fcdbc4c2d?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1527799820374-dcf8d9d4a388?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-kho" },
                    Ingredients = new() { ("argan-oil", "2%"), ("ceramide", "0.5%") },
                    HasVariants = false,
                    IsFeatured = false,
                    CosmeticNotificationNumber = "143097/20/CBMP-QLD",
                    ImporterName = "Công ty TNHH Sáng Tạo Phong Cách Sống"
                },
                new()
                {
                    Sku = "SKU-HAIR-FIN-074",
                    Barcode = "8935001202074",
                    Name = "Kem Ủ Tóc Phục Hồi Tinh Chất Sữa Ong Chúa Fino Premium Touch Hair Mask 230g",
                    Slug = "kem-u-toc-fino-premium-touch-hair-mask-230g",
                    CatSlug = "mat-na-u-toc",
                    BrandName = "Shiseido",
                    CountryOfOrigin = "Nhật Bản",
                    Unit = "Hũ",
                    Volume = "230g",
                    CostPrice = 140000m,
                    SellingPrice = 245000m,
                    DiscountPrice = 209000m,
                    ShortDescription = "Chứa 7 loại tinh chất vàng bao gồm sữa ong chúa và squalane phục hồi tóc hư tổn nặng.",
                    Description = "Kem ủ tóc Fino Premium Touch của Shiseido liên tục đứng đầu bảng xếp hạng @Cosme Nhật Bản. Chứa 7 tinh chất dưỡng sâu gồm sữa ong chúa EX, trehalose, PCA và squalane thấm sâu vào lõi tóc hư tổn do tẩy nhuộm, phục hồi độ đàn hồi và độ bóng mượt chuẩn salon.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1527799820374-dcf8d9d4a388?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1535585209827-a15fcdbc4c2d?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-kho" },
                    Ingredients = new() { ("squalane", "2%"), ("peptides", "1%") },
                    HasVariants = false,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "154985/21/CBMP-QLD",
                    ImporterName = "Công ty TNHH Mỹ Phẩm Shiseido Việt Nam"
                },
                new()
                {
                    Sku = "SKU-BOD-LOC-075",
                    Barcode = "8935001202075",
                    Name = "Dầu Tắm Hạnh Nhân Dưỡng Ẩm Mịn Màng L'Occitane Almond Shower Oil 250ml",
                    Slug = "dau-tam-hanh-nhan-loccitane-almond-shower-oil-250ml",
                    CatSlug = "sua-tam-duong-am",
                    BrandName = "Bioderma",
                    CountryOfOrigin = "Pháp",
                    Unit = "Chai",
                    Volume = "250ml",
                    CostPrice = 480000m,
                    SellingPrice = 790000m,
                    DiscountPrice = 699000m,
                    ShortDescription = "Kết cấu dầu biến hóa thành lớp bọt sữa mềm mịn, lưu giữ hương thơm hạnh nhân ngọt ngào.",
                    Description = "Dầu tắm hạnh nhân kinh điển của vùng Provence nước Pháp. Khi tiếp xúc với nước, kết cấu dầu vàng óng ả biến đổi kỳ diệu thành lớp bọt sữa mềm mịn bồng bềnh, nhẹ nhàng làm sạch cơ thể, nuôi dưỡng làn da ẩm mượt như lụa và lưu hương thơm ngọt ngào thư giãn suốt ngày dài.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1540555700478-4be289fbecef?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1556228722-d0b5d92fe77c?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-kho", "da-nhay-cam", "da-thuong" },
                    Ingredients = new() { ("squalane", "2%"), ("panthenol-vitamin-b5", "1%") },
                    HasVariants = false,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "176542/22/CBMP-QLD",
                    ImporterName = "Công ty TNHH L'Occitane Việt Nam"
                },
                new()
                {
                    Sku = "SKU-BOD-SOL-076",
                    Barcode = "8935001202076",
                    Name = "Kem Dưỡng Thể Săn Chắc Thơm Ngọt Sol de Janeiro Brazilian Bum Bum Cream 240ml",
                    Slug = "kem-duong-the-sol-de-janeiro-brazilian-bum-bum-cream-240ml",
                    CatSlug = "duong-the-body",
                    BrandName = "CeraVe",
                    CountryOfOrigin = "Mỹ",
                    Unit = "Hũ",
                    Volume = "240ml",
                    CostPrice = 650000m,
                    SellingPrice = 1150000m,
                    DiscountPrice = 1050000m,
                    ShortDescription = "Chiết xuất hạt Guaraná giàu caffeine làm săn chắc da cơ thể, nốt hương hồ trăn caramen bùng nổ.",
                    Description = "Cơn sốt dưỡng thể số 1 tại chuỗi bán lẻ Sephora toàn cầu. Chiết xuất Guaraná chứa hàm lượng caffeine gấp 5 lần cà phê thông thường kích thích tuần hoàn máu và làm săn chắc các vùng da đùi, mông, bụng. Mùi hương Cheirosa 62 với caramen muối và hạt hồ trăn gây nghiện ngọt ngào say đắm.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1540555700478-4be289fbecef?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1540555700478-4be289fbecef?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-kho" },
                    Ingredients = new() { ("hyaluronic-acid", "1%"), ("squalane", "1%") },
                    HasVariants = false,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "187424/22/CBMP-QLD",
                    ImporterName = "Công ty CP Thương Mại Dịch Vụ Kim Điền"
                },
                new()
                {
                    Sku = "SKU-BOD-COC-077",
                    Barcode = "8935001202077",
                    Name = "Tẩy Tế Bào Chết Body Cà Phê Đắk Lắk Thuần Chay Cocoon 200ml",
                    Slug = "tay-te-bao-chet-body-ca-phe-dak-lak-cocoon-200ml",
                    CatSlug = "tay-te-bao-chet-body",
                    BrandName = "Innisfree",
                    CountryOfOrigin = "Việt Nam",
                    Unit = "Hũ",
                    Volume = "200ml",
                    CostPrice = 75000m,
                    SellingPrice = 145000m,
                    DiscountPrice = 125000m,
                    ShortDescription = "Hạt cà phê Đắk Lắk xay nhuyễn kết hợp bơ cacao Tiền Giang làm mịn da và giảm thâm sần.",
                    Description = "Niềm tự hào mỹ phẩm thuần chay 100% Việt Nam. Hạt cà phê nguyên chất Đắk Lắk xay mịn giàu chất chống oxy hóa nhẹ nhàng cuốn trôi lớp tế bào sừng già cỗi, kết hợp bơ ca cao Tiền Giang dưỡng ẩm sâu, giải quyết triệt để tình trạng viêm nang lông và da sần sùi không đều màu.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1540555700478-4be289fbecef?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1540555700478-4be289fbecef?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1556228722-d0b5d92fe77c?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-dau", "da-kho" },
                    Ingredients = new() { ("aha-glycolic-acid", "1%"), ("squalane", "1%") },
                    HasVariants = false,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "128491/20/CBMP-HCM",
                    ImporterName = "Công ty TNHH Mỹ Phẩm Nature Story"
                },
                new()
                {
                    Sku = "SKU-BOD-BBW-078",
                    Barcode = "8935001202078",
                    Name = "Xịt Thơm Toàn Thân Quyến Rũ Bath & Body Works Into The Night Body Mist 236ml",
                    Slug = "xit-thom-toan-than-bath-and-body-works-into-the-night-236ml",
                    CatSlug = "body-mist",
                    BrandName = "L'Oréal Paris",
                    CountryOfOrigin = "Mỹ",
                    Unit = "Chai",
                    Volume = "236ml",
                    CostPrice = 180000m,
                    SellingPrice = 320000m,
                    DiscountPrice = 279000m,
                    ShortDescription = "Hương thơm quyến rũ sang trọng từ quả mâm xôi đen, hoa nhài đêm và hổ phách ấm áp.",
                    Description = "Bath & Body Works Into The Night là chai xịt thơm toàn thân sang chảnh bậc nhất của nhãn hiệu Hoa Kỳ. Chiết xuất nha đam và hoa cúc dưỡng ẩm nhẹ nhàng cho da cơ thể, nốt hương hòa quyện giữa mâm xôi mọng nước, cánh hoa nhài nửa đêm và hổ phách xạ hương lưu luyến bền lâu suốt 6 tiếng.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1592945403244-b3fbafd7f539?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1540555700478-4be289fbecef?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1571781926291-c477ebfd024b?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-kho" },
                    Ingredients = new() { ("panthenol-vitamin-b5", "0.5%") },
                    HasVariants = false,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "154986/21/CBMP-QLD",
                    ImporterName = "Công ty TNHH Thương Mại Mesa"
                },
                new()
                {
                    Sku = "SKU-NUT-SHI-079",
                    Barcode = "8935001202079",
                    Name = "Nước Uống Trắng Da Chống Lão Hóa Shiseido The Collagen Enriched (10 chai x 50ml)",
                    Slug = "nuoc-uong-collagen-shiseido-the-collagen-enriched-10x50ml",
                    CatSlug = "collagen-nuoc",
                    BrandName = "Shiseido",
                    CountryOfOrigin = "Nhật Bản",
                    Unit = "Hộp",
                    Volume = "10 chai x 50ml",
                    CostPrice = 480000m,
                    SellingPrice = 820000m,
                    DiscountPrice = 730000m,
                    ShortDescription = "Hàm lượng 1.000mg Collagen thủy phân kết hợp Hyaluronic Acid và Ceramide tự nhiên.",
                    Description = "Thực phẩm làm đẹp cao cấp số 1 Nhật Bản từ Shiseido. Chứa 1.000mg Collagen peptide phân tử siêu nhỏ chiết xuất từ cá biển sâu hấp thụ gấp 7 lần thông thường, kết hợp chiết xuất trái dâu Lingonberry Bắc Âu, quả Amla Ấn Độ, Hyaluronic Acid và Ceramide giúp da săn chắc, làm mờ nếp nhăn và trẻ hóa từ sâu bên trong.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1584017911766-d451b3d0e843?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1512069772995-ec65ed45afd6?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-kho", "da-nhay-cam" },
                    Ingredients = new() { ("collagen-thuy-phan", "5%"), ("hyaluronic-acid", "1%"), ("ceramide", "0.5%"), ("vitamin-c", "1%") },
                    HasVariants = false,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "194320/22/ATTP-XNCB",
                    ImporterName = "Công ty TNHH Mỹ Phẩm Shiseido Việt Nam"
                },
                new()
                {
                    Sku = "SKU-NUT-CKJ-080",
                    Barcode = "8935001202080",
                    Name = "Nước Hồng Sâm Hoàng Gia 6 Năm Tuổi Cheong Kwan Jang Everytime Balance (30 gói x 10ml)",
                    Slug = "nuoc-hong-sam-cheong-kwan-jang-everytime-balance-30x10ml",
                    CatSlug = "nuoc-hong-sam",
                    BrandName = "Sulwhasoo",
                    CountryOfOrigin = "Hàn Quốc",
                    Unit = "Hộp",
                    Volume = "30 gói x 10ml",
                    CostPrice = 1100000m,
                    SellingPrice = 1850000m,
                    DiscountPrice = 1690000m,
                    ShortDescription = "Hồng sâm chính phủ Hàn Quốc 6 năm tuổi bổ khí huyết, tăng sinh collagen cho sắc diện hồng hào.",
                    Description = "Cheong Kwan Jang KGC là thương hiệu hồng sâm số 1 thế giới được bảo trợ bởi chính phủ Hàn Quốc. Chứa 100% tinh chất hồng sâm 6 năm tuổi cô đặc cùng mật ong và chiết xuất cam thảo. Giúp bồi bổ khí huyết, tăng cường lưu thông máu, xóa tan căng thẳng mệt mỏi và duy trì sắc diện tươi trẻ hồng hào rạng rỡ.",
                    ThumbnailUrl = "https://images.unsplash.com/photo-1512069772995-ec65ed45afd6?w=800&h=800&fit=crop&q=80",
                    Images = new List<string>
                    {
                        "https://images.unsplash.com/photo-1512069772995-ec65ed45afd6?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=800&h=800&fit=crop&q=80",
                        "https://images.unsplash.com/photo-1584017911766-d451b3d0e843?w=800&h=800&fit=crop&q=80"
                    },
                    SkinTypeSlugs = new() { "da-thuong", "da-kho", "da-nhay-cam" },
                    Ingredients = new() { ("peptides", "1%"), ("panthenol-vitamin-b5", "1%") },
                    HasVariants = false,
                    IsFeatured = true,
                    CosmeticNotificationNumber = "192302/22/ATTP-XNCB",
                    ImporterName = "Công ty TNHH KGC Việt Nam"
                }
            };
        }
    }
}
