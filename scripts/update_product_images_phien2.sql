-- =========================================================================================
-- PHIÊN 2 (BƯỚC 3): SCRIPT SQL IDEMPOTENT ĐỒNG BỘ ĐƯỜNG DẪN ẢNH SẢN PHẨM CHUẨN
-- Nhánh: feature/canmake-rebrand-images
-- =========================================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @CanonicalProductMap TABLE (
    Sku NVARCHAR(100) PRIMARY KEY,
    CanonicalUrl NVARCHAR(500) NOT NULL,
    FileName NVARCHAR(260) NOT NULL
);

INSERT INTO @CanonicalProductMap (Sku, CanonicalUrl, FileName) VALUES
(N'AMO-ROM-VT02', N'/images/products/makeup/son-kem-li-romand-zero-velvet-tint-02-joyful-55g.jpg', N'son-kem-li-romand-zero-velvet-tint-02-joyful-55g.jpg'),
(N'AMO-ROM-JLT', N'/images/products/makeup/son-tint-bong-romand-juicy-lasting-tint-06-figfig-55g.jpg', N'son-tint-bong-romand-juicy-lasting-tint-06-figfig-55g.jpg'),
(N'LOR-COL-666', N'/images/products/makeup/son-thoi-loreal-paris-color-riche-matte-37g.jpg', N'son-thoi-loreal-paris-color-riche-matte-37g.jpg'),
(N'AMO-INN-AIR', N'/images/products/makeup/son-kem-li-romand-zero-velvet-tint-02-joyful-55g.jpg', N'son-kem-li-romand-zero-velvet-tint-02-joyful-55g.jpg'),
(N'ROH-LIP-ICE', N'/images/products/makeup/son-duong-chuyen-mau-lipice-sheer-color-24g.jpg', N'son-duong-chuyen-mau-lipice-sheer-color-24g.jpg'),
(N'LOR-HYA-B5', N'/images/products/skincare/serum-phuc-hoi-la-roche-posay-hyalu-b5-pure-30ml.jpg', N'serum-phuc-hoi-la-roche-posay-hyalu-b5-pure-30ml.jpg'),
(N'LOR-MCL-400', N'/images/products/skincare/nuoc-tay-trang-bioderma-sensibio-h2o-500ml.png', N'nuoc-tay-trang-bioderma-sensibio-h2o-500ml.png'),
(N'ROH-HDL-100', N'/images/products/skincare/dung-dich-duong-am-hada-labo-advanced-nourish-170ml.jpg', N'dung-dich-duong-am-hada-labo-advanced-nourish-170ml.jpg'),
(N'AMO-LAN-WSLEEP', N'/images/products/skincare/mat-na-ngu-laneige-water-sleeping-mask-ex-70ml.jpg', N'mat-na-ngu-laneige-water-sleeping-mask-ex-70ml.jpg'),
(N'ROH-ACNES-WASH', N'/images/products/skincare/gel-rua-mat-cosrx-low-ph-good-morning-gel-cleanser-150ml.jpg', N'gel-rua-mat-cosrx-low-ph-good-morning-gel-cleanser-150ml.jpg'),
(N'AMO-LAN-CUSH21', N'/images/products/makeup/phan-nuoc-laneige-neo-cushion-matte-spf42-15g-x-2.jpg', N'phan-nuoc-laneige-neo-cushion-matte-spf42-15g-x-2.jpg'),
(N'LOR-INF-FND', N'/images/products/makeup/kem-nen-loreal-infallible-32h-fresh-wear-foundation-30ml.jpg', N'kem-nen-loreal-infallible-32h-fresh-wear-foundation-30ml.jpg'),
(N'AMO-INN-POWDER', N'/images/products/makeup/phan-phu-bot-khoang-innisfree-no-sebum-mineral-powder-5g.jpg', N'phan-phu-bot-khoang-innisfree-no-sebum-mineral-powder-5g.jpg'),
(N'ROH-SS-SKINA', N'/images/products/skincare/sua-chong-nang-anessa-perfect-uv-sunscreen-skincare-milk-spf50-60ml.jpg', N'sua-chong-nang-anessa-perfect-uv-sunscreen-skincare-milk-spf50-60ml.jpg'),
(N'LOR-LRP-ANTHE', N'/images/products/skincare/kem-chong-nang-la-roche-posay-anthelios-uvmune-400-oil-control-50ml.jpg', N'kem-chong-nang-la-roche-posay-anthelios-uvmune-400-oil-control-50ml.jpg'),
(N'SKU-PROD-3293', N'/images/products/skincare/tinh-chat-rau-ma-skin1004-madagascar-centella-ampoule-100ml.jpg', N'tinh-chat-rau-ma-skin1004-madagascar-centella-ampoule-100ml.jpg'),
(N'SKU-SKIN-ANE-001', N'/images/products/skincare/sua-chong-nang-anessa-perfect-uv-sunscreen-skincare-milk-spf50-60ml.jpg', N'sua-chong-nang-anessa-perfect-uv-sunscreen-skincare-milk-spf50-60ml.jpg'),
(N'SKU-SKIN-LRP-002', N'/images/products/skincare/serum-phuc-hoi-la-roche-posay-hyalu-b5-pure-30ml.jpg', N'serum-phuc-hoi-la-roche-posay-hyalu-b5-pure-30ml.jpg'),
(N'SKU-SKIN-KLA-003', N'/images/products/skincare/nuoc-hoa-hong-klairs-supple-preparation-unscented-toner-180ml.webp', N'nuoc-hoa-hong-klairs-supple-preparation-unscented-toner-180ml.webp'),
(N'SKU-SKIN-CER-004', N'/images/products/skincare/sua-rua-mat-cerave-foaming-facial-cleanser-236ml.jpg', N'sua-rua-mat-cerave-foaming-facial-cleanser-236ml.jpg'),
(N'SKU-SKIN-EST-005', N'/images/products/skincare/tinh-chat-estee-lauder-advanced-night-repair-50ml.jpg', N'tinh-chat-estee-lauder-advanced-night-repair-50ml.jpg'),
(N'SKU-SKIN-BIO-006', N'/images/products/skincare/nuoc-tay-trang-bioderma-sensibio-h2o-500ml.png', N'nuoc-tay-trang-bioderma-sensibio-h2o-500ml.png'),
(N'SKU-SKIN-ORD-007', N'/images/products/skincare/tinh-chat-the-ordinary-niacinamide-10-zinc-1-30ml.png', N'tinh-chat-the-ordinary-niacinamide-10-zinc-1-30ml.png'),
(N'SKU-SKIN-PAU-008', N'/images/products/skincare/dung-dich-tay-te-bao-chet-paulas-choice-skin-perfecting-2-bha-118ml.png', N'dung-dich-tay-te-bao-chet-paulas-choice-skin-perfecting-2-bha-118ml.png'),
(N'SKU-SKIN-HDL-009', N'/images/products/skincare/dung-dich-duong-am-hada-labo-gokujyun-premium-lotion-170ml.jpg', N'dung-dich-duong-am-hada-labo-gokujyun-premium-lotion-170ml.jpg'),
(N'SKU-SKIN-LRP-010', N'/images/products/skincare/kem-duong-phuc-hoi-la-roche-posay-cicaplast-baume-b5-plus-40ml.jpg', N'kem-duong-phuc-hoi-la-roche-posay-cicaplast-baume-b5-plus-40ml.jpg'),
(N'SKU-SKIN-SKI-011', N'/images/products/skincare/nuoc-than-sk-ii-facial-treatment-essence-230ml.jpg', N'nuoc-than-sk-ii-facial-treatment-essence-230ml.jpg'),
(N'SKU-SKIN-SUL-012', N'/images/products/skincare/tinh-chat-sulwhasoo-first-care-activating-serum-vi-60ml.jpg', N'tinh-chat-sulwhasoo-first-care-activating-serum-vi-60ml.jpg'),
(N'SKU-SKIN-CER-013', N'/images/products/skincare/kem-duong-am-cerave-moisturizing-cream-454g.jpg', N'kem-duong-am-cerave-moisturizing-cream-454g.jpg'),
(N'SKU-SKIN-TOR-014', N'/images/products/skincare/serum-cap-nuoc-torriden-dive-in-low-molecular-ha-50ml.jpg', N'serum-cap-nuoc-torriden-dive-in-low-molecular-ha-50ml.jpg'),
(N'SKU-SKIN-COS-015', N'/images/products/skincare/tinh-chat-oc-sen-cosrx-advanced-snail-96-mucin-100ml.jpg', N'tinh-chat-oc-sen-cosrx-advanced-snail-96-mucin-100ml.jpg'),
(N'SKU-SKIN-SBM-016', N'/images/products/skincare/serum-some-by-mi-aha-bha-pha-30-days-miracle-50ml.jpg', N'serum-some-by-mi-aha-bha-pha-30-days-miracle-50ml.jpg'),
(N'SKU-SKIN-COS-017', N'/images/products/skincare/gel-rua-mat-cosrx-low-ph-good-morning-gel-cleanser-150ml.jpg', N'gel-rua-mat-cosrx-low-ph-good-morning-gel-cleanser-150ml.jpg'),
(N'SKU-SKIN-SHI-018', N'/images/products/skincare/tinh-chat-shiseido-ultimune-power-infusing-concentrate-50ml.jpg', N'tinh-chat-shiseido-ultimune-power-infusing-concentrate-50ml.jpg'),
(N'SKU-SKIN-DHC-019', N'/images/products/skincare/dau-tay-trang-dhc-deep-cleansing-oil-200ml.jpg', N'dau-tay-trang-dhc-deep-cleansing-oil-200ml.jpg'),
(N'SKU-SKIN-LRP-020', N'/images/products/skincare/kem-chong-nang-la-roche-posay-anthelios-uvmune-400-oil-control-50ml.jpg', N'kem-chong-nang-la-roche-posay-anthelios-uvmune-400-oil-control-50ml.jpg'),
(N'SKU-SKIN-LAN-021', N'/images/products/skincare/mat-na-ngu-laneige-water-sleeping-mask-ex-70ml.jpg', N'mat-na-ngu-laneige-water-sleeping-mask-ex-70ml.jpg'),
(N'SKU-SKIN-EST-022', N'/images/products/skincare/kem-duong-mat-estee-lauder-advanced-night-repair-eye-15ml.jpg', N'kem-duong-mat-estee-lauder-advanced-night-repair-eye-15ml.jpg'),
(N'SKU-SKIN-INN-023', N'/images/products/skincare/kem-duong-innisfree-cherry-blossom-glow-jelly-50ml.jpg', N'kem-duong-innisfree-cherry-blossom-glow-jelly-50ml.jpg'),
(N'SKU-SKIN-SBM-024', N'/images/products/skincare/nuoc-hoa-hong-some-by-mi-aha-bha-pha-miracle-toner-150ml.jpg', N'nuoc-hoa-hong-some-by-mi-aha-bha-pha-miracle-toner-150ml.jpg'),
(N'SKU-SKIN-INN-025', N'/images/products/skincare/kem-khoa-am-innisfree-green-tea-seed-hyaluronic-cream-50ml.jpg', N'kem-khoa-am-innisfree-green-tea-seed-hyaluronic-cream-50ml.jpg'),
(N'SKU-SKIN-INN-026', N'/images/products/skincare/mat-na-dat-set-innisfree-super-volcanic-pore-clay-mask-100ml.jpg', N'mat-na-dat-set-innisfree-super-volcanic-pore-clay-mask-100ml.jpg'),
(N'SKU-SKIN-S10-027', N'/images/products/skincare/tinh-chat-rau-ma-skin1004-madagascar-centella-ampoule-100ml.jpg', N'tinh-chat-rau-ma-skin1004-madagascar-centella-ampoule-100ml.jpg'),
(N'SKU-SKIN-S10-028', N'/images/products/skincare/kem-chong-nang-skin1004-centella-air-fit-suncream-plus-spf50-50ml.jpg', N'kem-chong-nang-skin1004-centella-air-fit-suncream-plus-spf50-50ml.jpg'),
(N'SKU-SKIN-LAN-029', N'/images/products/skincare/tinh-chat-lancome-advanced-genifique-concentrate-50ml.jpg', N'tinh-chat-lancome-advanced-genifique-concentrate-50ml.jpg'),
(N'SKU-SKIN-LAN-030', N'/images/products/skincare/kem-mat-lancome-advanced-genifique-eye-cream-15ml.jpg', N'kem-mat-lancome-advanced-genifique-eye-cream-15ml.jpg'),
(N'SKU-LIP-MAC-031', N'/images/products/makeup/son-thoi-mac-retro-matte-lipstick-707-ruby-woo-3g.jpg', N'son-thoi-mac-retro-matte-lipstick-707-ruby-woo-3g.jpg'),
(N'SKU-LIP-MAC-032', N'/images/products/makeup/son-thoi-mac-matte-lipstick-602-chili-3g.jpg', N'son-thoi-mac-matte-lipstick-602-chili-3g.jpg'),
(N'SKU-LIP-MAC-033', N'/images/products/makeup/son-thoi-mac-powder-kiss-lipstick-316-devoted-to-chili-3g.jpg', N'son-thoi-mac-powder-kiss-lipstick-316-devoted-to-chili-3g.jpg'),
(N'SKU-LIP-ROM-034', N'/images/products/makeup/son-kem-li-romand-zero-velvet-tint-02-joyful-55g.jpg', N'son-kem-li-romand-zero-velvet-tint-02-joyful-55g.jpg'),
(N'SKU-LIP-ROM-035', N'/images/products/makeup/son-tint-bong-romand-juicy-lasting-tint-06-figfig-55g.jpg', N'son-tint-bong-romand-juicy-lasting-tint-06-figfig-55g.jpg'),
(N'SKU-LIP-ROM-036', N'/images/products/makeup/son-bong-romand-glasting-water-tint-04-vintage-ocean-4g.jpg', N'son-bong-romand-glasting-water-tint-04-vintage-ocean-4g.jpg'),
(N'SKU-LIP-DIO-037', N'/images/products/makeup/son-thoi-rouge-dior-satin-lipstick-999-metallic-velvet-35g.jpg', N'son-thoi-rouge-dior-satin-lipstick-999-metallic-velvet-35g.jpg'),
(N'SKU-LIP-YSL-038', N'/images/products/makeup/son-thoi-ysl-rouge-pur-couture-the-slim-21-rouge-paradoxe-22g.jpg', N'son-thoi-ysl-rouge-pur-couture-the-slim-21-rouge-paradoxe-22g.jpg'),
(N'SKU-LIP-CHA-039', N'/images/products/makeup/son-thoi-chanel-rouge-allure-velvet-58-rouge-vie-35g.jpg', N'son-thoi-chanel-rouge-allure-velvet-58-rouge-vie-35g.jpg'),
(N'SKU-LIP-LAN-040', N'/images/products/makeup/mat-na-ngu-moi-laneige-lip-sleeping-mask-berry-20g.jpg', N'mat-na-ngu-moi-laneige-lip-sleeping-mask-berry-20g.jpg'),
(N'SKU-MAK-LAN-041', N'/images/products/makeup/phan-nuoc-laneige-neo-cushion-matte-spf42-15g-x-2.jpg', N'phan-nuoc-laneige-neo-cushion-matte-spf42-15g-x-2.jpg'),
(N'SKU-MAK-SUL-042', N'/images/products/makeup/phan-nuoc-sulwhasoo-perfecting-cushion-spf50-15g-x-2.jpg', N'phan-nuoc-sulwhasoo-perfecting-cushion-spf50-15g-x-2.jpg'),
(N'SKU-MAK-EST-043', N'/images/products/makeup/kem-nen-estee-lauder-double-wear-stay-in-place-30ml.jpg', N'kem-nen-estee-lauder-double-wear-stay-in-place-30ml.jpg'),
(N'SKU-MAK-LOR-044', N'/images/products/makeup/kem-nen-loreal-infallible-32h-fresh-wear-foundation-30ml.jpg', N'kem-nen-loreal-infallible-32h-fresh-wear-foundation-30ml.jpg'),
(N'SKU-MAK-INN-045', N'/images/products/makeup/phan-phu-bot-khoang-innisfree-no-sebum-mineral-powder-5g.jpg', N'phan-phu-bot-khoang-innisfree-no-sebum-mineral-powder-5g.jpg'),
(N'SKU-MAK-CAN-046', N'/images/products/makeup/phan-phu-nen-canmake-marshmallow-finish-powder-spf50-10g.jpg', N'phan-phu-nen-canmake-marshmallow-finish-powder-spf50-10g.jpg'),
(N'SKU-MAK-MAY-047', N'/images/products/makeup/kem-lot-maybelline-baby-skin-pore-eraser-22ml.jpg', N'kem-lot-maybelline-baby-skin-pore-eraser-22ml.jpg'),
(N'SKU-MAK-MAY-048', N'/images/products/makeup/mascara-maybelline-lash-sensational-sky-high-waterproof-6ml.jpg', N'mascara-maybelline-lash-sensational-sky-high-waterproof-6ml.jpg'),
(N'SKU-MAK-MAY-049', N'/images/products/makeup/but-ke-mat-nuoc-maybelline-hyper-sharp-liner-05g.jpg', N'but-ke-mat-nuoc-maybelline-hyper-sharp-liner-05g.jpg'),
(N'SKU-MAK-CAN-050', N'/images/products/makeup/ma-hong-kem-canmake-cream-cheek-cl01-clear-red-heart-22g.jpg', N'ma-hong-kem-canmake-cream-cheek-cl01-clear-red-heart-22g.jpg'),
(N'SKU-MAK-ROM-051', N'/images/products/makeup/bang-phan-mat-romand-better-than-eyes-01-dry-mango-tulip-65g.jpg', N'bang-phan-mat-romand-better-than-eyes-01-dry-mango-tulip-65g.jpg'),
(N'SKU-MAK-URB-052', N'/images/products/makeup/xit-khoa-nen-urban-decay-all-nighter-setting-spray-118ml.jpg', N'xit-khoa-nen-urban-decay-all-nighter-setting-spray-118ml.jpg'),
(N'SKU-MAK-MAC-053', N'/images/products/makeup/xit-co-dinh-nen-mac-prep-prime-fix-plus-100ml.jpg', N'xit-co-dinh-nen-mac-prep-prime-fix-plus-100ml.jpg'),
(N'SKU-MAK-DIO-054', N'/images/products/makeup/kem-lot-dior-backstage-face-and-body-primer-50ml.jpg', N'kem-lot-dior-backstage-face-and-body-primer-50ml.jpg'),
(N'SKU-MAK-DIO-055', N'/images/products/makeup/bang-phan-bat-sang-dior-backstage-glow-face-palette-001-universal-10g.jpg', N'bang-phan-bat-sang-dior-backstage-glow-face-palette-001-universal-10g.jpg'),
(N'SKU-PERF-DIO-056', N'/images/products/perfume/nuoc-hoa-nam-dior-sauvage-eau-de-parfum-100ml.jpg', N'nuoc-hoa-nam-dior-sauvage-eau-de-parfum-100ml.jpg'),
(N'SKU-PERF-CHA-057', N'/images/products/perfume/nuoc-hoa-nu-chanel-coco-mademoiselle-edp-100ml.jpg', N'nuoc-hoa-nu-chanel-coco-mademoiselle-edp-100ml.jpg'),
(N'SKU-PERF-CHA-058', N'/images/products/perfume/nuoc-hoa-nu-chanel-n5-eau-de-parfum-100ml.jpg', N'nuoc-hoa-nu-chanel-n5-eau-de-parfum-100ml.jpg'),
(N'SKU-PERF-YSL-059', N'/images/products/perfume/nuoc-hoa-nu-ysl-libre-eau-de-parfum-90ml.jpg', N'nuoc-hoa-nu-ysl-libre-eau-de-parfum-90ml.jpg'),
(N'SKU-PERF-YSL-060', N'/images/products/perfume/nuoc-hoa-nu-ysl-black-opium-edp-90ml.jpg', N'nuoc-hoa-nu-ysl-black-opium-edp-90ml.jpg'),
(N'SKU-PERF-DIO-061', N'/images/products/perfume/nuoc-hoa-nu-dior-miss-dior-eau-de-parfum-100ml.jpg', N'nuoc-hoa-nu-dior-miss-dior-eau-de-parfum-100ml.jpg'),
(N'SKU-PERF-CHA-062', N'/images/products/perfume/nuoc-hoa-nam-chanel-bleu-de-chanel-edp-100ml.jpg', N'nuoc-hoa-nam-chanel-bleu-de-chanel-edp-100ml.jpg'),
(N'SKU-PERF-YSL-063', N'/images/products/perfume/nuoc-hoa-nam-ysl-y-eau-de-parfum-100ml.jpg', N'nuoc-hoa-nam-ysl-y-eau-de-parfum-100ml.jpg'),
(N'SKU-PERF-LAN-064', N'/images/products/perfume/nuoc-hoa-nu-lancome-la-vie-est-belle-edp-100ml.jpg', N'nuoc-hoa-nu-lancome-la-vie-est-belle-edp-100ml.jpg'),
(N'SKU-PERF-JOM-065', N'/images/products/perfume/nuoc-hoa-unisex-jo-malone-english-pear-and-freesia-100ml.jpg', N'nuoc-hoa-unisex-jo-malone-english-pear-and-freesia-100ml.jpg'),
(N'SKU-PERF-JOM-066', N'/images/products/perfume/nuoc-hoa-unisex-jo-malone-wood-sage-and-sea-salt-100ml.jpg', N'nuoc-hoa-unisex-jo-malone-wood-sage-and-sea-salt-100ml.jpg'),
(N'SKU-PERF-TOM-067', N'/images/products/perfume/nuoc-hoa-unisex-tom-ford-black-orchid-edp-50ml.jpg', N'nuoc-hoa-unisex-tom-ford-black-orchid-edp-50ml.jpg'),
(N'SKU-PERF-DIO-068', N'/images/products/perfume/bo-qua-tang-nuoc-hoa-dior-miss-dior-miniature-giftset-4x5ml.jpg', N'bo-qua-tang-nuoc-hoa-dior-miss-dior-miniature-giftset-4x5ml.jpg'),
(N'SKU-PERF-CHA-069', N'/images/products/perfume/nuoc-hoa-bo-tui-chanel-coco-mademoiselle-purse-spray-3x20ml.jpg', N'nuoc-hoa-bo-tui-chanel-coco-mademoiselle-purse-spray-3x20ml.jpg'),
(N'SKU-PERF-MAI-070', N'/images/products/perfume/nuoc-hoa-unisex-replica-jazz-club-edt-100ml.jpg', N'nuoc-hoa-unisex-replica-jazz-club-edt-100ml.jpg'),
(N'SKU-HAIR-MOR-071', N'/images/products/hair-body/tinh-dau-duong-toc-moroccanoil-treatment-original-100ml.jpg', N'tinh-dau-duong-toc-moroccanoil-treatment-original-100ml.jpg'),
(N'SKU-HAIR-MOR-072', N'/images/products/hair-body/dau-goi-duong-am-moroccanoil-hydrating-shampoo-250ml.jpg', N'dau-goi-duong-am-moroccanoil-hydrating-shampoo-250ml.jpg'),
(N'SKU-HAIR-MOR-073', N'/images/products/hair-body/dau-xa-duong-am-moroccanoil-hydrating-conditioner-250ml.jpg', N'dau-xa-duong-am-moroccanoil-hydrating-conditioner-250ml.jpg'),
(N'SKU-HAIR-FIN-074', N'/images/products/hair-body/kem-u-toc-fino-premium-touch-hair-mask-230g.jpg', N'kem-u-toc-fino-premium-touch-hair-mask-230g.jpg'),
(N'SKU-BOD-LOC-075', N'/images/products/hair-body/dau-tam-hanh-nhan-loccitane-almond-shower-oil-250ml.jpg', N'dau-tam-hanh-nhan-loccitane-almond-shower-oil-250ml.jpg'),
(N'SKU-BOD-SOL-076', N'/images/products/hair-body/kem-duong-the-sol-de-janeiro-brazilian-bum-bum-cream-240ml.jpg', N'kem-duong-the-sol-de-janeiro-brazilian-bum-bum-cream-240ml.jpg'),
(N'SKU-BOD-COC-077', N'/images/products/hair-body/tay-te-bao-chet-body-ca-phe-dak-lak-cocoon-200ml.jpg', N'tay-te-bao-chet-body-ca-phe-dak-lak-cocoon-200ml.jpg'),
(N'SKU-BOD-BBW-078', N'/images/products/hair-body/xit-thom-toan-than-bath-and-body-works-into-the-night-236ml.jpg', N'xit-thom-toan-than-bath-and-body-works-into-the-night-236ml.jpg'),
(N'SKU-NUT-SHI-079', N'/images/products/hair-body/nuoc-uong-collagen-shiseido-the-collagen-enriched-10x50ml.jpg', N'nuoc-uong-collagen-shiseido-the-collagen-enriched-10x50ml.jpg'),
(N'SKU-NUT-CKJ-080', N'/images/products/hair-body/nuoc-hong-sam-cheong-kwan-jang-everytime-balance-30x10ml.jpg', N'nuoc-hong-sam-cheong-kwan-jang-everytime-balance-30x10ml.jpg'),
(N'SKU-SKIN-CLI-081', N'/images/products/skincare/kem-duong-am-chuyen-sau-clinique-moisture-surge-100h-50ml.jpg', N'kem-duong-am-chuyen-sau-clinique-moisture-surge-100h-50ml.jpg'),
(N'SKU-CROSS-9001', N'/images/products/skincare/bong-tay-trang-cao-cap-silcot-huu-co-100-mieng.jpg', N'bong-tay-trang-cao-cap-silcot-huu-co-100-mieng.jpg');

-- 1. Cập nhật dbo.Products (ThumbnailUrl, ImagesJson, ThumbnailMediaId)
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

-- 2. Cập nhật dbo.OrderReturnRequests (EvidenceUrlsJson gãy -> đường dẫn chuẩn)
UPDATE dbo.OrderReturnRequests
SET EvidenceUrlsJson = REPLACE(
    EvidenceUrlsJson,
    N'/images/products/serum-b5.jpg',
    N'/images/products/skincare/serum-phuc-hoi-la-roche-posay-hyalu-b5-pure-30ml.jpg'
)
WHERE EvidenceUrlsJson LIKE N'%/images/products/serum-b5.jpg%';

COMMIT TRANSACTION;