-- =========================================================================================
-- PHIÊN 2 (BƯỚC 3): SCRIPT SQL IDEMPOTENT ĐỒNG BỘ ĐƯỜNG DẪN ẢNH SẢN PHẨM CHUẨN
-- Nhánh: feature/canmake-rebrand-images
-- Mục tiêu:
--   1. Cập nhật toàn bộ Products.ThumbnailUrl, Products.ImagesJson và Products.ThumbnailMediaId
--      về đường dẫn chuẩn duy nhất: /images/products/{skincare|makeup|perfume|hair-body}/{fileName}
--   2. Cập nhật OrderReturnRequests.EvidenceUrlsJson từ đường dẫn gãy /images/products/serum-b5.jpg
--      sang /images/products/skincare/serum-phuc-hoi-la-roche-posay-hyalu-b5-pure-30ml.jpg
--   3. Kiểm tra (SELECT COUNT) trước và sau khi UPDATE trong TRANSACTION an toàn.
-- =========================================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

-- 1. Bảng ánh xạ chuẩn 1-1 cho toàn bộ 98 sản phẩm (SKU -> CanonicalUrl & FileName)
DECLARE @CanonicalProductMap TABLE (
    Sku NVARCHAR(100) PRIMARY KEY,
    CanonicalUrl NVARCHAR(500) NOT NULL,
    FileName NVARCHAR(260) NOT NULL
);

INSERT INTO @CanonicalProductMap (Sku, CanonicalUrl, FileName) VALUES
-- Nhóm 16 sản phẩm khởi tạo ban đầu (Id = 1..16)
(N'LOREAL-REV-001', N'/images/products/skincare/nuoc-tay-trang-loreal-micellar-water-3-in-1-deep-cleansing-400ml.jpg', N'nuoc-tay-trang-loreal-micellar-water-3-in-1-deep-cleansing-400ml.jpg'),
(N'LOREAL-INF-102', N'/images/products/makeup/kem-nen-maybelline-fit-me-matte-poreless-foundation-30ml.jpg', N'kem-nen-maybelline-fit-me-matte-poreless-foundation-30ml.jpg'),
(N'MBL-FITME-120', N'/images/products/makeup/kem-nen-maybelline-fit-me-matte-poreless-foundation-30ml.jpg', N'kem-nen-maybelline-fit-me-matte-poreless-foundation-30ml.jpg'),
(N'MBL-SKYHIGH-01', N'/images/products/makeup/mascara-maybelline-lash-sensational-sky-high-waterproof-6ml.jpg', N'mascara-maybelline-lash-sensational-sky-high-waterproof-6ml.jpg'),
(N'ROH-LIP-ICE', N'/images/products/makeup/son-duong-chuyen-mau-lipice-sheer-color-24g.jpg', N'son-duong-chuyen-mau-lipice-sheer-color-24g.jpg'),
(N'SUNPLAY-SKIN-50', N'/images/products/skincare/sua-chong-nang-sunplay-skin-aqua-clear-white-spf50-55g.jpg', N'sua-chong-nang-sunplay-skin-aqua-clear-white-spf50-55g.jpg'),
(N'ROH-ACNES-C10', N'/images/products/skincare/tinh-chat-duong-sang-rohto-melano-cc-vitamin-c-essence-20ml.jpg', N'tinh-chat-duong-sang-rohto-melano-cc-vitamin-c-essence-20ml.jpg'),
(N'ROH-HDL-100', N'/images/products/skincare/dung-dich-duong-am-hada-labo-advanced-nourish-170ml.jpg', N'dung-dich-duong-am-hada-labo-advanced-nourish-170ml.jpg'),
(N'CNP-PROPOLIS-15', N'/images/products/skincare/tinh-chat-keo-ong-cnp-laboratory-propolis-energy-ampule-15ml.jpg', N'tinh-chat-keo-ong-cnp-laboratory-propolis-energy-ampule-15ml.jpg'),
(N'OHUI-CUSHION-01', N'/images/products/makeup/phan-nuoc-laneige-neo-cushion-matte-spf42-15g.jpg', N'phan-nuoc-laneige-neo-cushion-matte-spf42-15g.jpg'),
(N'SHISEIDO-ULT-50', N'/images/products/skincare/tinh-chat-shiseido-ultimune-power-infusing-concentrate-50ml.jpg', N'tinh-chat-shiseido-ultimune-power-infusing-concentrate-50ml.jpg'),
(N'ANESSA-MILK-60', N'/images/products/skincare/sua-chong-nang-anessa-perfect-uv-sunscreen-skincare-milk-60ml.jpg', N'sua-chong-nang-anessa-perfect-uv-sunscreen-skincare-milk-60ml.jpg'),
(N'LRP-B5-40', N'/images/products/skincare/kem-duong-la-roche-posay-cicaplast-baume-b5-plus-40ml.jpg', N'kem-duong-la-roche-posay-cicaplast-baume-b5-plus-40ml.jpg'),
(N'LRP-EFF-DUO', N'/images/products/skincare/kem-giam-mun-la-roche-posay-effaclar-duo-plus-m-40ml.jpg', N'kem-giam-mun-la-roche-posay-effaclar-duo-plus-m-40ml.jpg'),
(N'VICHY-M89-50', N'/images/products/skincare/khoang-co-dac-vichy-mineral-89-booster-50ml.jpg', N'khoang-co-dac-vichy-mineral-89-booster-50ml.jpg'),
(N'Paula-BHA-118', N'/images/products/skincare/dung-dich-tay-te-bao-chet-paulas-choice-skin-perfecting-2-bha-118ml.png', N'dung-dich-tay-te-bao-chet-paulas-choice-skin-perfecting-2-bha-118ml.png'),

-- Nhóm 30 sản phẩm Skincare (Id = 17..46)
(N'SKU-SKIN-ANE-001', N'/images/products/skincare/sua-chong-nang-anessa-perfect-uv-sunscreen-skincare-milk-60ml.jpg', N'sua-chong-nang-anessa-perfect-uv-sunscreen-skincare-milk-60ml.jpg'),
(N'SKU-SKIN-LRP-002', N'/images/products/skincare/serum-phuc-hoi-la-roche-posay-hyalu-b5-pure-30ml.jpg', N'serum-phuc-hoi-la-roche-posay-hyalu-b5-pure-30ml.jpg'),
(N'SKU-SKIN-PC-003', N'/images/products/skincare/dung-dich-tay-te-bao-chet-paulas-choice-skin-perfecting-2-bha-118ml.png', N'dung-dich-tay-te-bao-chet-paulas-choice-skin-perfecting-2-bha-118ml.png'),
(N'SKU-SKIN-CRV-004', N'/images/products/skincare/sua-rua-mat-cerave-foaming-facial-cleanser-473ml.jpg', N'sua-rua-mat-cerave-foaming-facial-cleanser-473ml.jpg'),
(N'SKU-SKIN-BIO-005', N'/images/products/skincare/nuoc-tay-trang-bioderma-sensibio-h2o-500ml.jpg', N'nuoc-tay-trang-bioderma-sensibio-h2o-500ml.jpg'),
(N'SKU-SKIN-EL-006', N'/images/products/skincare/tinh-chat-estee-lauder-advanced-night-repair-50ml.jpg', N'tinh-chat-estee-lauder-advanced-night-repair-50ml.jpg'),
(N'SKU-SKIN-SK1-007', N'/images/products/skincare/tinh-chat-rau-ma-skin1004-madagascar-centella-ampoule-100ml.jpg', N'tinh-chat-rau-ma-skin1004-madagascar-centella-ampoule-100ml.jpg'),
(N'SKU-SKIN-ORD-008', N'/images/products/skincare/serum-the-ordinary-niacinamide-10-zinc-1-30ml.jpg', N'serum-the-ordinary-niacinamide-10-zinc-1-30ml.jpg'),
(N'SKU-SKIN-HDL-009', N'/images/products/skincare/dung-dich-duong-am-hada-labo-gokujyun-premium-lotion-170ml.jpg', N'dung-dich-duong-am-hada-labo-gokujyun-premium-lotion-170ml.jpg'),
(N'SKU-SKIN-KLS-010', N'/images/products/skincare/nuoc-hoa-hong-klairs-supple-preparation-unscented-toner-180ml.jpg', N'nuoc-hoa-hong-klairs-supple-preparation-unscented-toner-180ml.jpg'),
(N'SKU-SKIN-COS-011', N'/images/products/skincare/tinh-chat-oc-sen-cosrx-advanced-snail-96-mucin-power-essence-100ml.jpg', N'tinh-chat-oc-sen-cosrx-advanced-snail-96-mucin-power-essence-100ml.jpg'),
(N'SKU-SKIN-LRP-012', N'/images/products/skincare/kem-duong-la-roche-posay-cicaplast-baume-b5-plus-40ml.jpg', N'kem-duong-la-roche-posay-cicaplast-baume-b5-plus-40ml.jpg'),
(N'SKU-SKIN-KIE-013', N'/images/products/skincare/kem-duong-am-kiehls-ultra-facial-cream-50ml.jpg', N'kem-duong-am-kiehls-ultra-facial-cream-50ml.jpg'),
(N'SKU-SKIN-VCH-014', N'/images/products/skincare/khoang-co-dac-vichy-mineral-89-booster-50ml.jpg', N'khoang-co-dac-vichy-mineral-89-booster-50ml.jpg'),
(N'SKU-SKIN-OBA-015', N'/images/products/skincare/kem-duong-obagi-retinol-1-0-cream-28g.jpg', N'kem-duong-obagi-retinol-1-0-cream-28g.jpg'),
(N'SKU-SKIN-SVR-016', N'/images/products/skincare/gel-rua-mat-svr-sebiaclear-gel-moussant-400ml.jpg', N'gel-rua-mat-svr-sebiaclear-gel-moussant-400ml.jpg'),
(N'SKU-SKIN-EUC-017', N'/images/products/skincare/gel-chong-nang-eucerin-sun-dry-touch-oil-control-spf50-50ml.jpg', N'gel-chong-nang-eucerin-sun-dry-touch-oil-control-spf50-50ml.jpg'),
(N'SKU-SKIN-INN-018', N'/images/products/skincare/mat-na-dat-set-innisfree-super-volcanic-pore-clay-mask-100ml.jpg', N'mat-na-dat-set-innisfree-super-volcanic-pore-clay-mask-100ml.jpg'),
(N'SKU-SKIN-LNG-019', N'/images/products/skincare/mat-na-ngu-moi-laneige-lip-sleeping-mask-berry-20g.jpg', N'mat-na-ngu-moi-laneige-lip-sleeping-mask-berry-20g.jpg'),
(N'SKU-SKIN-SK2-020', N'/images/products/skincare/nuoc-than-sk-ii-facial-treatment-essence-230ml.jpg', N'nuoc-than-sk-ii-facial-treatment-essence-230ml.jpg'),
(N'SKU-SKIN-SUL-021', N'/images/products/skincare/tinh-chat-sulwhasoo-first-care-activating-serum-vi-60ml.jpg', N'tinh-chat-sulwhasoo-first-care-activating-serum-vi-60ml.jpg'),
(N'SKU-SKIN-CLN-022', N'/images/products/skincare/gel-duong-am-clinique-moisture-surge-100h-50ml.jpg', N'gel-duong-am-clinique-moisture-surge-100h-50ml.jpg'),
(N'SKU-SKIN-AVN-023', N'/images/products/skincare/xit-khoang-avene-thermal-spring-water-300ml.jpg', N'xit-khoang-avene-thermal-spring-water-300ml.jpg'),
(N'SKU-SKIN-SOM-024', N'/images/products/skincare/nuoc-hoa-hong-some-by-mi-aha-bha-pha-30-days-miracle-toner-150ml.jpg', N'nuoc-hoa-hong-some-by-mi-aha-bha-pha-30-days-miracle-toner-150ml.jpg'),
(N'SKU-SKIN-TOR-025', N'/images/products/skincare/serum-cap-am-torriden-dive-in-low-molecular-hyaluronic-acid-50ml.jpg', N'serum-cap-am-torriden-dive-in-low-molecular-hyaluronic-acid-50ml.jpg'),
(N'SKU-SKIN-BOJ-026', N'/images/products/skincare/kem-chong-nang-beauty-of-joseon-relief-sun-rice-probiotics-50ml.jpg', N'kem-chong-nang-beauty-of-joseon-relief-sun-rice-probiotics-50ml.jpg'),
(N'SKU-SKIN-SHU-027', N'/images/products/skincare/dau-tay-trang-shu-uemura-ultime8-sublime-beauty-cleansing-oil-150ml.jpg', N'dau-tay-trang-shu-uemura-ultime8-sublime-beauty-cleansing-oil-150ml.jpg'),
(N'SKU-SKIN-LNC-028', N'/images/products/skincare/tinh-chat-lancome-advanced-genifique-youth-activating-serum-50ml.jpg', N'tinh-chat-lancome-advanced-genifique-youth-activating-serum-50ml.jpg'),
(N'SKU-SKIN-MRD-029', N'/images/products/skincare/gel-rua-mat-murad-clarifying-cleanser-200ml.jpg', N'gel-rua-mat-murad-clarifying-cleanser-200ml.jpg'),
(N'SKU-SKIN-DRG-030', N'/images/products/skincare/kem-duong-am-dr-g-red-blemish-clear-soothing-cream-70ml.jpg', N'kem-duong-am-dr-g-red-blemish-clear-soothing-cream-70ml.jpg'),

-- Nhóm 25 sản phẩm Makeup (Id = 47..71)
(N'SKU-MAKE-MAC-031', N'/images/products/makeup/son-thoi-li-mac-macximal-silky-matte-lipstick-ruby-woo-35g.jpg', N'son-thoi-li-mac-macximal-silky-matte-lipstick-ruby-woo-35g.jpg'),
(N'SKU-MAKE-3CE-032', N'/images/products/makeup/son-kem-li-3ce-velvet-lip-tint-daffodil-4g.jpg', N'son-kem-li-3ce-velvet-lip-tint-daffodil-4g.jpg'),
(N'SKU-MAKE-ROM-033', N'/images/products/makeup/son-tint-bong-romand-juicy-lasting-tint-jujube-55g.jpg', N'son-tint-bong-romand-juicy-lasting-tint-jujube-55g.jpg'),
(N'SKU-MAKE-DIR-034', N'/images/products/makeup/son-duong-dior-addict-lip-glow-001-pink-32g.jpg', N'son-duong-dior-addict-lip-glow-001-pink-32g.jpg'),
(N'SKU-MAKE-YSL-035', N'/images/products/makeup/son-thoi-li-ysl-rouge-pur-couture-the-slim-21-rouge-paradoxe-22g.jpg', N'son-thoi-li-ysl-rouge-pur-couture-the-slim-21-rouge-paradoxe-22g.jpg'),
(N'SKU-MAKE-MAY-036', N'/images/products/makeup/mascara-maybelline-lash-sensational-sky-high-waterproof-6ml.jpg', N'mascara-maybelline-lash-sensational-sky-high-waterproof-6ml.jpg'),
(N'SKU-MAKE-MAY-037', N'/images/products/makeup/kem-nen-maybelline-fit-me-matte-poreless-foundation-30ml.jpg', N'kem-nen-maybelline-fit-me-matte-poreless-foundation-30ml.jpg'),
(N'SKU-MAKE-NRS-038', N'/images/products/makeup/kem-che-khuyet-diem-nars-radiant-creamy-concealer-custard-6ml.jpg', N'kem-che-khuyet-diem-nars-radiant-creamy-concealer-custard-6ml.jpg'),
(N'SKU-MAKE-CLIO-039', N'/images/products/makeup/phan-nuoc-clio-kill-cover-the-new-founwear-cushion-15g.jpg', N'phan-nuoc-clio-kill-cover-the-new-founwear-cushion-15g.jpg'),
(N'SKU-MAKE-LNG-040', N'/images/products/makeup/phan-nuoc-laneige-neo-cushion-matte-spf42-15g.jpg', N'phan-nuoc-laneige-neo-cushion-matte-spf42-15g.jpg'),
(N'SKU-MAKE-LM-041', N'/images/products/makeup/phan-phu-bot-laura-mercier-translucent-loose-setting-powder-29g.jpg', N'phan-phu-bot-laura-mercier-translucent-loose-setting-powder-29g.jpg'),
(N'SKU-MAKE-3CE-042', N'/images/products/makeup/bang-phan-mat-3ce-multi-eye-color-palette-overtake-85g.jpg', N'bang-phan-mat-3ce-multi-eye-color-palette-overtake-85g.jpg'),
(N'SKU-MAKE-CLIO-043', N'/images/products/makeup/but-ke-mat-nuoc-clio-waterproof-pen-liner-kill-black-055ml.jpg', N'but-ke-mat-nuoc-clio-waterproof-pen-liner-kill-black-055ml.jpg'),
(N'SKU-MAKE-SHU-044', N'/images/products/makeup/chi-ke-may-shu-uemura-hard-formula-eyebrow-pencil-seal-brown-4g.jpg', N'chi-ke-may-shu-uemura-hard-formula-eyebrow-pencil-seal-brown-4g.jpg'),
(N'SKU-MAKE-NRS-045', N'/images/products/makeup/phan-ma-hong-nars-blush-orgasm-48g.jpg', N'phan-ma-hong-nars-blush-orgasm-48g.jpg'),
(N'SKU-MAKE-MAC-046', N'/images/products/makeup/xit-co-dinh-nen-mac-prep-prime-fix-plus-100ml.jpg', N'xit-co-dinh-nen-mac-prep-prime-fix-plus-100ml.jpg'),
(N'SKU-MAKE-BOB-047', N'/images/products/makeup/kem-lot-duong-am-bobbi-brown-vitamin-enriched-face-base-50ml.jpg', N'kem-lot-duong-am-bobbi-brown-vitamin-enriched-face-base-50ml.jpg'),
(N'SKU-MAKE-EL-048', N'/images/products/makeup/kem-nen-estee-lauder-double-wear-stay-in-place-makeup-30ml.jpg', N'kem-nen-estee-lauder-double-wear-stay-in-place-makeup-30ml.jpg'),
(N'SKU-MAKE-BBI-049', N'/images/products/makeup/son-kem-li-bbia-last-velvet-lip-tint-v25-final-note-5g.jpg', N'son-kem-li-bbia-last-velvet-lip-tint-v25-final-note-5g.jpg'),
(N'SKU-MAKE-PER-050', N'/images/products/makeup/son-kem-li-peripera-ink-velvet-17-rosy-nude-4g.jpg', N'son-kem-li-peripera-ink-velvet-17-rosy-nude-4g.jpg'),
(N'SKU-MAKE-KSM-051', N'/images/products/makeup/mascara-kiss-me-heroine-make-long-and-curl-waterproof-6g.jpg', N'mascara-kiss-me-heroine-make-long-and-curl-waterproof-6g.jpg'),
(N'SKU-MAKE-INN-052', N'/images/products/makeup/phan-phu-kiem-dau-innisfree-no-sebum-mineral-powder-5g.jpg', N'phan-phu-kiem-dau-innisfree-no-sebum-mineral-powder-5g.jpg'),
(N'SKU-MAKE-ROM-053', N'/images/products/makeup/bang-phan-mat-romand-better-than-palette-01-pampas-garden-75g.jpg', N'bang-phan-mat-romand-better-than-palette-01-pampas-garden-75g.jpg'),
(N'SKU-MAKE-TF-054', N'/images/products/makeup/son-thoi-tom-ford-lip-color-16-scarlet-rouge-3g.jpg', N'son-thoi-tom-ford-lip-color-16-scarlet-rouge-3g.jpg'),
(N'SKU-MAKE-CHN-055', N'/images/products/makeup/son-thoi-chanel-rouge-allure-velvet-58-rouge-vie-35g.jpg', N'son-thoi-chanel-rouge-allure-velvet-58-rouge-vie-35g.jpg'),

-- Nhóm 15 sản phẩm Perfume (Id = 72..86)
(N'SKU-PERF-CHN-056', N'/images/products/perfume/nuoc-hoa-nu-chanel-coco-mademoiselle-edp-100ml.jpg', N'nuoc-hoa-nu-chanel-coco-mademoiselle-edp-100ml.jpg'),
(N'SKU-PERF-DIR-057', N'/images/products/perfume/nuoc-hoa-nam-dior-sauvage-eau-de-parfum-100ml.jpg', N'nuoc-hoa-nam-dior-sauvage-eau-de-parfum-100ml.jpg'),
(N'SKU-PERF-YSL-058', N'/images/products/perfume/nuoc-hoa-nu-ysl-libre-edp-90ml.jpg', N'nuoc-hoa-nu-ysl-libre-edp-90ml.jpg'),
(N'SKU-PERF-NR-059', N'/images/products/perfume/nuoc-hoa-nu-narciso-rodriguez-for-her-pure-musc-edp-100ml.jpg', N'nuoc-hoa-nu-narciso-rodriguez-for-her-pure-musc-edp-100ml.jpg'),
(N'SKU-PERF-JML-060', N'/images/products/perfume/nuoc-hoa-unisex-jo-malone-english-pear-and-freesia-100ml.jpg', N'nuoc-hoa-unisex-jo-malone-english-pear-and-freesia-100ml.jpg'),
(N'SKU-PERF-GUC-061', N'/images/products/perfume/nuoc-hoa-nu-gucci-flora-gorgeous-gardenia-edp-100ml.jpg', N'nuoc-hoa-nu-gucci-flora-gorgeous-gardenia-edp-100ml.jpg'),
(N'SKU-PERF-LLB-062', N'/images/products/perfume/nuoc-hoa-unisex-le-labo-santal-33-edp-100ml.jpg', N'nuoc-hoa-unisex-le-labo-santal-33-edp-100ml.jpg'),
(N'SKU-PERF-VRS-063', N'/images/products/perfume/nuoc-hoa-nam-versace-eros-eau-de-toilette-100ml.jpg', N'nuoc-hoa-nam-versace-eros-eau-de-toilette-100ml.jpg'),
(N'SKU-PERF-LNC-064', N'/images/products/perfume/nuoc-hoa-nu-lancome-la-vie-est-belle-edp-100ml.jpg', N'nuoc-hoa-nu-lancome-la-vie-est-belle-edp-100ml.jpg'),
(N'SKU-PERF-CHN-065', N'/images/products/perfume/nuoc-hoa-nam-bleu-de-chanel-eau-de-parfum-100ml.jpg', N'nuoc-hoa-nam-bleu-de-chanel-eau-de-parfum-100ml.jpg'),
(N'SKU-PERF-DIR-066', N'/images/products/perfume/nuoc-hoa-nu-miss-dior-blooming-bouquet-edt-100ml.jpg', N'nuoc-hoa-nu-miss-dior-blooming-bouquet-edt-100ml.jpg'),
(N'SKU-PERF-CH-067', N'/images/products/perfume/nuoc-hoa-nu-carolina-herrera-good-girl-edp-80ml.jpg', N'nuoc-hoa-nu-carolina-herrera-good-girl-edp-80ml.jpg'),
(N'SKU-PERF-GA-068', N'/images/products/perfume/nuoc-hoa-nu-giorgio-armani-si-passione-edp-100ml.jpg', N'nuoc-hoa-nu-giorgio-armani-si-passione-edp-100ml.jpg'),
(N'SKU-PERF-MMR-069', N'/images/products/perfume/nuoc-hoa-unisex-maison-margiela-replica-jazz-club-100ml.jpg', N'nuoc-hoa-unisex-maison-margiela-replica-jazz-club-100ml.jpg'),
(N'SKU-PERF-TF-070', N'/images/products/perfume/nuoc-hoa-unisex-tom-ford-oud-wood-edp-50ml.jpg', N'nuoc-hoa-unisex-tom-ford-oud-wood-edp-50ml.jpg'),

-- Nhóm 11 sản phẩm Hair & Body (Id = 87..97) + Cross-sell (Id = 98)
(N'SKU-BODY-MRC-071', N'/images/products/hair-body/dau-duong-toc-moroccanoil-treatment-original-100ml.jpg', N'dau-duong-toc-moroccanoil-treatment-original-100ml.jpg'),
(N'SKU-BODY-OLP-072', N'/images/products/hair-body/kem-u-phuc-hoi-toc-olaplex-no-3-hair-perfector-100ml.jpg', N'kem-u-phuc-hoi-toc-olaplex-no-3-hair-perfector-100ml.jpg'),
(N'SKU-BODY-TSU-073', N'/images/products/hair-body/bo-dau-goi-xa-tsubaki-premium-moist-and-repair-490ml.jpg', N'bo-dau-goi-xa-tsubaki-premium-moist-and-repair-490ml.jpg'),
(N'SKU-BODY-LOC-074', N'/images/products/hair-body/sua-tam-duong-am-loccitane-almond-shower-oil-250ml.jpg', N'sua-tam-duong-am-loccitane-almond-shower-oil-250ml.jpg'),
(N'SKU-BODY-PC-075', N'/images/products/hair-body/kem-duong-the-paulas-choice-2-bha-body-spot-exfoliant-210ml.jpg', N'kem-duong-the-paulas-choice-2-bha-body-spot-exfoliant-210ml.jpg'),
(N'SKU-BODY-CCN-076', N'/images/products/hair-body/tay-da-chet-body-ca-phe-dak-lak-cocoon-200ml.jpg', N'tay-da-chet-body-ca-phe-dak-lak-cocoon-200ml.jpg'),
(N'SKU-BODY-CCN-077', N'/images/products/hair-body/nuoc-duong-toc-tinh-dau-buoi-cocoon-140ml.jpg', N'nuoc-duong-toc-tinh-dau-buoi-cocoon-140ml.jpg'),
(N'SKU-BODY-VAS-078', N'/images/products/hair-body/sua-duong-the-vaseline-gluta-hya-dewy-radiance-330ml.jpg', N'sua-duong-the-vaseline-gluta-hya-dewy-radiance-330ml.jpg'),
(N'SKU-BODY-TBS-079', N'/images/products/hair-body/bo-duong-the-the-body-shop-shea-body-butter-200ml.jpg', N'bo-duong-the-the-body-shop-shea-body-butter-200ml.jpg'),
(N'SKU-BODY-BBW-080', N'/images/products/hair-body/xit-thom-toan-than-bath-and-body-works-into-the-night-236ml.jpg', N'xit-thom-toan-than-bath-and-body-works-into-the-night-236ml.jpg'),
(N'SKU-PERF-MFK-081', N'/images/products/perfume/nuoc-hoa-maison-francis-kurkdjian-baccarat-rouge-540-edp-70ml.jpg', N'nuoc-hoa-maison-francis-kurkdjian-baccarat-rouge-540-edp-70ml.jpg'),
(N'SKU-CROSS-9001', N'/images/products/skincare/bong-tay-trang-cao-cap-silcot-huu-co-100-mieng.jpg', N'bong-tay-trang-cao-cap-silcot-huu-co-100-mieng.jpg');

-- =========================================================================================
-- 2. KIỂM TRA SỐ DÒNG BỊ ẢNH HƯỞNG TRƯỚC KHI UPDATE (PRE-CHECK SELECT COUNT)
-- =========================================================================================
SELECT
    (SELECT COUNT(*) FROM dbo.Products p
     INNER JOIN @CanonicalProductMap m ON p.Sku = m.Sku
     LEFT JOIN dbo.MediaAssets ma ON ma.FileName = m.FileName AND ma.Category = N'PRODUCT'
     WHERE ISNULL(p.ThumbnailUrl, N'') <> m.CanonicalUrl
        OR ISNULL(p.ImagesJson, N'') <> CONCAT(N'["', m.CanonicalUrl, N'"]')
        OR (ma.Id IS NOT NULL AND ISNULL(p.ThumbnailMediaId, 0) <> ma.Id)
    ) AS AffectedProductsCount,
    (SELECT COUNT(*) FROM dbo.OrderReturnRequests
     WHERE EvidenceUrlsJson LIKE N'%/images/products/serum-b5.jpg%'
    ) AS AffectedOrderReturnRequestsCount,
    (SELECT COUNT(*) FROM dbo.ProductVariants
     WHERE SwatchImageUrl LIKE N'%/images/real_products/%'
    ) AS AffectedProductVariantsCount,
    (SELECT COUNT(*) FROM dbo.ProductReviews
     WHERE ImagesJson LIKE N'%/images/real_products/%'
    ) AS AffectedProductReviewsCount;

-- =========================================================================================
-- 3. CẬP NHẬT DỮ LIỆU TRONG TRANSACTION (IDEMPOTENT)
-- =========================================================================================

-- 3.1. Cập nhật dbo.Products (ThumbnailUrl, ImagesJson, ThumbnailMediaId)
UPDATE p
SET
    p.ThumbnailUrl = m.CanonicalUrl,
    p.ImagesJson = CONCAT(N'["', m.CanonicalUrl, N'"]'),
    p.ThumbnailMediaId = COALESCE(ma.Id, p.ThumbnailMediaId)
FROM dbo.Products p
INNER JOIN @CanonicalProductMap m ON p.Sku = m.Sku
LEFT JOIN dbo.MediaAssets ma ON ma.FileName = m.FileName AND ma.Category = N'PRODUCT'
WHERE ISNULL(p.ThumbnailUrl, N'') <> m.CanonicalUrl
   OR ISNULL(p.ImagesJson, N'') <> CONCAT(N'["', m.CanonicalUrl, N'"]')
   OR (ma.Id IS NOT NULL AND ISNULL(p.ThumbnailMediaId, 0) <> ma.Id);

-- 3.2. Cập nhật dbo.OrderReturnRequests (EvidenceUrlsJson gãy -> đường dẫn chuẩn)
UPDATE dbo.OrderReturnRequests
SET EvidenceUrlsJson = REPLACE(
    EvidenceUrlsJson,
    N'/images/products/serum-b5.jpg',
    N'/images/products/skincare/serum-phuc-hoi-la-roche-posay-hyalu-b5-pure-30ml.jpg'
)
WHERE EvidenceUrlsJson LIKE N'%/images/products/serum-b5.jpg%';

COMMIT TRANSACTION;