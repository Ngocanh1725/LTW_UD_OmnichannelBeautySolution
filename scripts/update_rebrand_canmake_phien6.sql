/*
 * ======================================================================================
 * SCRIPT CẬP NHẬT DỮ LIỆU ĐỒNG BỘ THƯƠNG HIỆU "CANMAKE BEAUTY" (PHIÊN 6 - IDEMPOTENT)
 * Database: OmnichannelBeautyDb (GIỮ NGUYÊN TÊN DATABASE THEO NGUYÊN TẮC 2)
 * Phạm vi: Cập nhật 25 cặp Bảng.Cột thuộc Nhóm 1 & Nhóm 3 (GIỮ NGUYÊN AuditLogs Nhóm 2)
 * Đặc tính: Idempotent (chạy lại nhiều lần cho kết quả như nhau), bọc trong TRANSACTION
 * ======================================================================================
 */
USE [OmnichannelBeautyDb];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

PRINT N'=== BẮT ĐẦU ĐỒNG BỘ DỮ LIỆU THƯƠNG HIỆU CANMAKE BEAUTY (PHIÊN 6) ===';

-- 1. Bảng ThemeConfigs (BrandName, Slogan, Bảng màu tông hồng Phiên 5, Email, Copyright, Logo)
UPDATE dbo.ThemeConfigs
SET BrandName = N'CANMAKE BEAUTY',
    Slogan = N'Canmake Beauty – Hệ Thống Mỹ Phẩm Đa Kênh Chính Hãng',
    PrimaryColor = '#E8618C',
    SecondaryColor = '#B8336A',
    AccentColor = '#F7C6D4',
    AnnouncementBarText = N'✨ Chào mừng bạn đến với CANMAKE BEAUTY - Miễn phí vận chuyển cho đơn hàng từ 300K ✨',
    AnnouncementBarBgColor = '#B8336A',
    ContactEmail = 'hotro@canmakebeauty.vn',
    FooterCopyright = N'© 2026 CANMAKE BEAUTY. Tất cả quyền được bảo lưu.',
    LogoMediaId = NULL,
    LogoMediaAssetId = NULL,
    FaviconMediaId = NULL,
    FaviconMediaAssetId = NULL,
    UpdatedAt = SYSUTCDATETIME()
WHERE BrandName LIKE N'%OMNI%'
   OR ContactEmail LIKE '%omnibeauty.vn%'
   OR PrimaryColor = '#E11D48';
PRINT N'1. ThemeConfigs cập nhật: ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + N' dòng.';

-- 2. Bảng PageLayouts
UPDATE dbo.PageLayouts
SET Title = N'Trang Chủ - CANMAKE BEAUTY',
    MetaDescription = N'Canmake Beauty – Hệ Thống Mỹ Phẩm Đa Kênh Chính Hãng'
WHERE PageKey = 'home_page' AND Title LIKE N'%OMNI%';
PRINT N'2. PageLayouts cập nhật: ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + N' dòng.';

-- 3. Bảng StaticPages
UPDATE dbo.StaticPages
SET Title = REPLACE(REPLACE(Title, N'Omnichannel Beauty Solution', N'Canmake Beauty'), N'Omnichannel Beauty', N'Canmake Beauty'),
    MetaTitle = REPLACE(REPLACE(MetaTitle, N'Omnichannel Beauty Solution', N'Canmake Beauty'), N'Omnichannel Beauty', N'Canmake Beauty'),
    Content = REPLACE(REPLACE(Content, N'Omnichannel Beauty Solution', N'Canmake Beauty'), N'Omnichannel Beauty', N'Canmake Beauty')
WHERE Title LIKE N'%Omnichannel%' OR MetaTitle LIKE N'%Omnichannel%' OR Content LIKE N'%Omnichannel%';
PRINT N'3. StaticPages cập nhật: ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + N' dòng.';

-- 4. Bảng Stores (Name, Email)
UPDATE dbo.Stores
SET Name = REPLACE(Name, N'OMNI BEAUTY', N'CANMAKE BEAUTY'),
    Email = REPLACE(REPLACE(Email, '@omnichannelbeauty.vn', '@canmakebeauty.vn'), '@omnibeauty.vn', '@canmakebeauty.vn')
WHERE Name LIKE N'%OMNI BEAUTY%' OR Email LIKE '%omnichannelbeauty.vn%' OR Email LIKE '%omnibeauty.vn%';
PRINT N'4. Stores cập nhật: ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + N' dòng.';

-- 5. Bảng Settings (SITE_NAME, SUPPORT_EMAIL, SMTP_USER, PAYMENT_VNPAY_TMN_CODE)
UPDATE dbo.Settings
SET SettingValue = N'Canmake Beauty', UpdatedAt = SYSUTCDATETIME()
WHERE SettingKey = 'SITE_NAME' AND SettingValue LIKE N'%Omni%';

UPDATE dbo.Settings
SET SettingValue = 'support@canmakebeauty.vn', UpdatedAt = SYSUTCDATETIME()
WHERE SettingKey = 'SUPPORT_EMAIL' AND SettingValue LIKE '%omni%';

UPDATE dbo.Settings
SET SettingValue = 'no-reply@canmakebeauty.vn', UpdatedAt = SYSUTCDATETIME()
WHERE SettingKey = 'SMTP_USER' AND SettingValue LIKE '%omni%';

UPDATE dbo.Settings
SET SettingValue = 'CANMAKECODE', UpdatedAt = SYSUTCDATETIME()
WHERE SettingKey = 'PAYMENT_VNPAY_TMN_CODE' AND SettingValue = 'OMNICODE';
PRINT N'5. Settings đã đồng bộ SITE_NAME, SUPPORT_EMAIL, SMTP_USER, PAYMENT_VNPAY_TMN_CODE.';

-- 6. Bảng ChatMessages
UPDATE dbo.ChatMessages
SET Content = REPLACE(
                REPLACE(
                  REPLACE(Content, N'Omnichannel Beauty Solution', N'Canmake Beauty'),
                  N'Omnichannel Beauty', N'Canmake Beauty'),
                N'Omni Beauty', N'Canmake Beauty')
WHERE Content LIKE N'%Omni%';
PRINT N'6. ChatMessages cập nhật: ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + N' dòng.';

-- 7. Bảng ProductAnswers (Q&A)
UPDATE dbo.ProductAnswers
SET AuthorName = REPLACE(AuthorName, N'Omni Beauty', N'Canmake Beauty')
WHERE AuthorName LIKE N'%Omni Beauty%';
PRINT N'7. ProductAnswers cập nhật: ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + N' dòng.';

-- 8. Bảng ProductReviews (AdminReply, AdminReplyBy, Content)
UPDATE dbo.ProductReviews
SET AdminReply = REPLACE(AdminReply, N'Omni Beauty', N'Canmake Beauty')
WHERE AdminReply LIKE N'%Omni Beauty%';
PRINT N'8a. ProductReviews.AdminReply cập nhật: ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + N' dòng.';

UPDATE dbo.ProductReviews
SET AdminReplyBy = REPLACE(AdminReplyBy, N'Omni Beauty', N'Canmake Beauty')
WHERE AdminReplyBy LIKE N'%Omni Beauty%';
PRINT N'8b. ProductReviews.AdminReplyBy cập nhật: ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + N' dòng.';

UPDATE dbo.ProductReviews
SET Content = REPLACE(Content, N'Omni Beauty', N'Canmake Beauty')
WHERE Content LIKE N'%Omni Beauty%';
PRINT N'8c. ProductReviews.Content cập nhật: ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + N' dòng.';

-- 9. Bảng OrderStatusHistories
UPDATE dbo.OrderStatusHistories
SET ChangedByName = REPLACE(REPLACE(ChangedByName, N'Omni Beauty', N'Canmake Beauty'), N'Omni VIP Express', N'Canmake VIP Express'),
    CarrierName = REPLACE(CarrierName, N'Omni VIP Express', N'Canmake VIP Express'),
    Description = REPLACE(REPLACE(Description, N'Omni Beauty', N'Canmake Beauty'), N'Omni VIP Express', N'Canmake VIP Express')
WHERE ChangedByName LIKE N'%Omni%' OR CarrierName LIKE N'%Omni%' OR Description LIKE N'%Omni%';
PRINT N'9. OrderStatusHistories cập nhật: ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + N' dòng.';

-- 10. Bảng PointTransactions, Coupons, Vouchers
UPDATE dbo.PointTransactions
SET Description = REPLACE(REPLACE(Description, N'Omni Rewards', N'Canmake Rewards'), N'Omni Beauty', N'Canmake Beauty')
WHERE Description LIKE N'%Omni%';
PRINT N'10a. PointTransactions cập nhật: ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + N' dòng.';

UPDATE dbo.Coupons
SET Description = REPLACE(REPLACE(Description, N'Omni Rewards', N'Canmake Rewards'), N'Omni Beauty', N'Canmake Beauty')
WHERE Description LIKE N'%Omni%';
PRINT N'10b. Coupons cập nhật: ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + N' dòng.';

UPDATE dbo.Vouchers
SET Description = REPLACE(REPLACE(Description, N'Omni Rewards', N'Canmake Rewards'), N'Omni Beauty', N'Canmake Beauty')
WHERE Description LIKE N'%Omni%';
PRINT N'10c. Vouchers cập nhật: ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + N' dòng.';

-- 11. Bảng LoyaltyWallets & LoyaltyReferrals (PA 3.2-A: OMNI-REF-* -> CANMAKE-REF-*)
UPDATE dbo.LoyaltyWallets
SET ReferralCode = REPLACE(ReferralCode, 'OMNI-REF-', 'CANMAKE-REF-')
WHERE ReferralCode LIKE 'OMNI-REF-%';
PRINT N'11a. LoyaltyWallets.ReferralCode cập nhật: ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + N' dòng.';

UPDATE dbo.LoyaltyReferrals
SET ReferralCode = REPLACE(ReferralCode, 'OMNI-REF-', 'CANMAKE-REF-')
WHERE ReferralCode LIKE 'OMNI-REF-%';
PRINT N'11b. LoyaltyReferrals.ReferralCode cập nhật: ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + N' dòng.';

-- 12. Bảng Users, Orders, ProductSubscriptions (PA 3.3-A: *@omnichannelbeauty.vn -> *@canmakebeauty.vn)
UPDATE dbo.Users
SET Email = REPLACE(REPLACE(REPLACE(Email, '@omnichannelbeauty.vn', '@canmakebeauty.vn'), '@omnibeauty.vn', '@canmakebeauty.vn'), '@omnichannel.com', '@canmakebeauty.vn')
WHERE Email LIKE '%@omnichannelbeauty.vn' OR Email LIKE '%@omnibeauty.vn' OR Email LIKE '%@omnichannel.com';
PRINT N'12a. Users.Email cập nhật: ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + N' dòng.';

UPDATE dbo.Orders
SET CustomerEmail = REPLACE(REPLACE(REPLACE(CustomerEmail, '@omnichannelbeauty.vn', '@canmakebeauty.vn'), '@omnibeauty.vn', '@canmakebeauty.vn'), '@omnichannel.com', '@canmakebeauty.vn')
WHERE CustomerEmail LIKE '%@omnichannelbeauty.vn' OR CustomerEmail LIKE '%@omnibeauty.vn' OR CustomerEmail LIKE '%@omnichannel.com';
PRINT N'12b. Orders.CustomerEmail cập nhật: ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + N' dòng.';

UPDATE dbo.ProductSubscriptions
SET Email = REPLACE(REPLACE(REPLACE(Email, '@omnichannelbeauty.vn', '@canmakebeauty.vn'), '@omnibeauty.vn', '@canmakebeauty.vn'), '@omnichannel.com', '@canmakebeauty.vn')
WHERE Email LIKE '%@omnichannelbeauty.vn' OR Email LIKE '%@omnibeauty.vn' OR Email LIKE '%@omnichannel.com';
PRINT N'12c. ProductSubscriptions.Email cập nhật: ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + N' dòng.';

COMMIT TRANSACTION;
PRINT N'=== HOÀN TẤT ĐỒNG BỘ DỮ LIỆU THƯƠNG HIỆU CANMAKE BEAUTY THÀNH CÔNG ===';
GO

