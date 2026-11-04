-- =========================================================================================
-- SCRIPT CẬP NHẬT DỮ LIỆU ĐỒNG BỘ ẢNH SẢN PHẨM & DANH MỤC - PHIÊN 3 (IDEMPOTENT)
-- Mục tiêu:
--   1. Gán ảnh dự phòng chuẩn (/images/products/_placeholder.webp) cho 14 sản phẩm
--      chưa có ảnh thực tế riêng (tránh dùng chung ảnh hoặc sai nội dung thương hiệu).
--   2. Chuẩn hóa tên tiếng Việt có dấu cho SKU-PROD-3293 và gán BrandId cho nhóm seed đầu.
--   3. Đồng bộ dbo.Categories.ImageUrl từ URL Unsplash ngoài về /images/categories/*.jpg.
-- =========================================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

-- 1. Báo cáo số dòng trước khi cập nhật (Preview)
SELECT COUNT(*) AS ProductsToSetPlaceholder
FROM dbo.Products
WHERE Id IN (1, 2, 4, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16)
  AND (ThumbnailUrl <> N'/images/products/_placeholder.webp'
       OR ImagesJson <> N'["/images/products/_placeholder.webp"]');

-- 2. Cập nhật 14 sản phẩm dùng chung ảnh / thiếu ảnh thật về _placeholder.webp
UPDATE dbo.Products
SET ThumbnailUrl = N'/images/products/_placeholder.webp',
    ImagesJson   = N'["/images/products/_placeholder.webp"]',
    UpdatedAt    = GETUTCDATE()
WHERE Id IN (1, 2, 4, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16)
  AND (ThumbnailUrl <> N'/images/products/_placeholder.webp'
       OR ImagesJson <> N'["/images/products/_placeholder.webp"]');

-- 3. Chuẩn hóa tên tiếng Việt có dấu cho sản phẩm Id = 16 (SKU-PROD-3293)
UPDATE dbo.Products
SET Name = N'Serum Rau Má Phục Hồi Da Nhạy Cảm Centella 50ml',
    UpdatedAt = GETUTCDATE()
WHERE SKU = N'SKU-PROD-3293'
  AND Name <> N'Serum Rau Má Phục Hồi Da Nhạy Cảm Centella 50ml';

-- 4. Đồng bộ dbo.Categories.ImageUrl về ảnh danh mục nội bộ /images/categories/*.jpg
UPDATE c
SET c.ImageUrl = m.LocalImageUrl
FROM dbo.Categories c
INNER JOIN (
    VALUES
        (N'trang-diem',            N'/images/categories/kem-nen.jpg'),
        (N'cham-soc-da',           N'/images/categories/serum-tinh-chat.jpg'),
        (N'nuoc-hoa',              N'/images/categories/nuoc-hoa-nu.jpg'),
        (N'toc-co-the',            N'/images/categories/cham-soc-co-the.jpg'),
        (N'son-moi',               N'/images/categories/son-moi.jpg'),
        (N'kem-nen-cushion',       N'/images/categories/kem-nen.jpg'),
        (N'phan-mat',              N'/images/categories/kem-nen.jpg'),
        (N'sua-rua-mat',           N'/images/categories/sua-rua-mat.jpg'),
        (N'serum-tinh-chat',       N'/images/categories/serum-tinh-chat.jpg'),
        (N'kem-chong-nang',        N'/images/categories/kem-chong-nang.jpg'),
        (N'nuoc-tay-trang',        N'/images/categories/toner-nuoc-hoa-hong.jpg'),
        (N'cham-soc-toc',          N'/images/categories/cham-soc-toc.jpg'),
        (N'cham-soc-co-the',       N'/images/categories/cham-soc-co-the.jpg'),
        (N'dung-cu-lam-dep',       N'/images/categories/kem-nen.jpg'),
        (N'set-qua-tang',          N'/images/categories/nuoc-hoa-nu.jpg'),
        (N'toner-nuoc-hoa-hong',   N'/images/categories/toner-nuoc-hoa-hong.jpg'),
        (N'kem-duong-am',          N'/images/categories/serum-tinh-chat.jpg'),
        (N'mat-na-duong-da',       N'/images/categories/sua-rua-mat.jpg'),
        (N'cham-soc-vung-mat',     N'/images/categories/serum-tinh-chat.jpg'),
        (N'kem-lot-kem-nen',       N'/images/categories/kem-nen.jpg'),
        (N'ma-hong-bat-sang',      N'/images/categories/son-moi.jpg'),
        (N'trang-diem-mat-mascara',N'/images/categories/kem-nen.jpg'),
        (N'xit-khoa-nen',          N'/images/categories/toner-nuoc-hoa-hong.jpg'),
        (N'nuoc-hoa-nu',           N'/images/categories/nuoc-hoa-nu.jpg'),
        (N'nuoc-hoa-nam',          N'/images/categories/nuoc-hoa-nam.jpg'),
        (N'nuoc-hoa-unisex',       N'/images/categories/nuoc-hoa-nam.jpg'),
        (N'giftset-nuoc-hoa',      N'/images/categories/nuoc-hoa-nu.jpg'),
        (N'nuoc-hoa-mini',         N'/images/categories/nuoc-hoa-nu.jpg'),
        (N'dau-goi-dau-xa',        N'/images/categories/cham-soc-toc.jpg'),
        (N'dau-duong-serum-toc',   N'/images/categories/cham-soc-toc.jpg'),
        (N'mat-na-u-toc',          N'/images/categories/cham-soc-toc.jpg'),
        (N'san-pham-tao-kieu',     N'/images/categories/cham-soc-toc.jpg'),
        (N'sua-tam-duong-am',      N'/images/categories/cham-soc-co-the.jpg'),
        (N'duong-the-body',        N'/images/categories/cham-soc-co-the.jpg'),
        (N'tay-te-bao-chet-body',  N'/images/categories/cham-soc-co-the.jpg'),
        (N'body-mist',             N'/images/categories/nuoc-hoa-nu.jpg'),
        (N'thuc-pham-lam-dep',     N'/images/categories/serum-tinh-chat.jpg'),
        (N'collagen-nuoc',         N'/images/categories/serum-tinh-chat.jpg'),
        (N'vitamin-vien-uong',     N'/images/categories/serum-tinh-chat.jpg'),
        (N'nuoc-hong-sam',         N'/images/categories/serum-tinh-chat.jpg')
) AS m(Slug, LocalImageUrl) ON c.Slug = m.Slug
WHERE ISNULL(c.ImageUrl, N'') <> m.LocalImageUrl;

COMMIT TRANSACTION;

