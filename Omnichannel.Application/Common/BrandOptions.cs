namespace Omnichannel.Application.Common
{
    /// <summary>
    /// Nguồn cấu hình thương hiệu tập trung (Single Source of Truth) cho toàn bộ hệ thống.
    /// Được ánh xạ từ phần "Brand" trong appsettings.json và tiêm (inject) trực tiếp vào Layout, Views, ViewComponents và Services.
    /// </summary>
    public class BrandOptions
    {
        public const string SectionName = "Brand";

        /// <summary>
        /// Tên thương hiệu in hoa chuẩn (dùng cho Logo, Header, Tiêu đề lớn, Hóa đơn).
        /// </summary>
        public string Name { get; set; } = "CANMAKE BEAUTY";

        /// <summary>
        /// Tên thương hiệu dạng viết hoa chữ cái đầu (dùng trong câu văn, thông báo, đoạn mô tả).
        /// </summary>
        public string ShortName { get; set; } = "Canmake Beauty";

        /// <summary>
        /// Tên hiển thị thân thiện (alias của ShortName).
        /// </summary>
        public string DisplayName => ShortName;

        /// <summary>
        /// Hậu tố tiêu đề trang (Title Suffix).
        /// </summary>
        public string TitleSuffix => Name;

        /// <summary>
        /// Khẩu hiệu (Slogan) chính thức của hệ thống.
        /// </summary>
        public string Slogan { get; set; } = "Canmake Beauty – Hệ Thống Mỹ Phẩm Đa Kênh Chính Hãng";

        /// <summary>
        /// Tên pháp nhân đầy đủ có dấu (dùng trên chứng từ, hóa đơn, hợp đồng).
        /// </summary>
        public string CompanyLegalName { get; set; } = "CÔNG TY TNHH CANMAKE BEAUTY";

        /// <summary>
        /// Tên chủ tài khoản ngân hàng không dấu chuẩn VietQR / NAPAS247.
        /// </summary>
        public string BankAccountName { get; set; } = "CONG TY TNHH CANMAKE BEAUTY";

        /// <summary>
        /// Tên chương trình khách hàng thân thiết (Loyalty Club).
        /// </summary>
        public string LoyaltyProgramName { get; set; } = "Canmake Club";

        /// <summary>
        /// Tên khu vực đổi điểm thưởng (Rewards Bazaar).
        /// </summary>
        public string RewardsBazaarName { get; set; } = "Canmake Rewards";

        /// <summary>
        /// Tên đơn vị điểm thưởng tích lũy.
        /// </summary>
        public string PointsCurrencyName { get; set; } = "Canmake Xu";

        /// <summary>
        /// Tên trợ lý trí tuệ nhân tạo.
        /// </summary>
        public string AiAssistantName { get; set; } = "CANMAKE BEAUTY AI";

        /// <summary>
        /// Đường dẫn Logo ngang chuẩn (dùng cho Header Storefront, Admin).
        /// </summary>
        public string LogoUrl { get; set; } = "/images/branding/canmake-logo-horizontal.svg";

        /// <summary>
        /// Đường dẫn Logo bản trắng & Blush (dùng cho nền tối, Dark Mode, Footer).
        /// </summary>
        public string LogoWhiteUrl { get; set; } = "/images/branding/canmake-logo-white.svg";

        /// <summary>
        /// Đường dẫn Logo xếp chồng (Icon trên, chữ dưới – dùng cho trang Đăng nhập, Đăng ký, Quên mật khẩu).
        /// </summary>
        public string LogoStackedUrl { get; set; } = "/images/branding/canmake-logo-stacked.svg";

        /// <summary>
        /// Đường dẫn Logo đơn sắc đen thuần túy (dùng cho máy in hóa đơn nhiệt POS 80mm và nhãn vận đơn).
        /// </summary>
        public string LogoMonoUrl { get; set; } = "/images/branding/canmake-logo-mono.svg";

        /// <summary>
        /// Đường dẫn biểu tượng (Monogram C × M) độc lập.
        /// </summary>
        public string IconUrl { get; set; } = "/images/branding/canmake-logo-icon.svg";

        /// <summary>
        /// Đường dẫn Favicon định dạng vector SVG.
        /// </summary>
        public string FaviconSvgUrl { get; set; } = "/images/branding/favicon.svg";

        /// <summary>
        /// Đường dẫn Web Manifest PWA.
        /// </summary>
        public string ManifestUrl { get; set; } = "/site.webmanifest";

        /// <summary>
        /// Đường dẫn Favicon đa kích thước (.ico).
        /// </summary>
        public string FaviconUrl { get; set; } = "/images/branding/favicon.ico";

        /// <summary>
        /// Đường dẫn Favicon PNG 32x32.
        /// </summary>
        public string Favicon32Url { get; set; } = "/images/branding/favicon-32x32.png";

        /// <summary>
        /// Đường dẫn Apple Touch Icon 180x180.
        /// </summary>
        public string AppleTouchIconUrl { get; set; } = "/images/branding/apple-touch-icon.png";

        /// <summary>
        /// Đường dẫn ảnh Open Graph 1200x630 khi chia sẻ mạng xã hội.
        /// </summary>
        public string OgImageUrl { get; set; } = "/images/branding/og-image-1200x630.png";

        /// <summary>
        /// Tên miền chính thức của hệ thống.
        /// </summary>
        public string Domain { get; set; } = "canmakebeauty.vn";

        /// <summary>
        /// Số điện thoại tổng đài CSKH.
        /// </summary>
        public string Hotline { get; set; } = "1900 636 900";

        /// <summary>
        /// Địa chỉ trụ sở chính hoặc showroom trung tâm.
        /// </summary>
        public string Address { get; set; } = "68 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh";

        /// <summary>
        /// Địa chỉ Email CSKH chính hiển thị trên Header/Footer/Schema.org.
        /// </summary>
        public string Email { get; set; } = "hotro@canmakebeauty.vn";

        /// <summary>
        /// Địa chỉ Email bộ phận hỗ trợ kỹ thuật & đơn hàng.
        /// </summary>
        public string SupportEmail { get; set; } = "support@canmakebeauty.vn";

        /// <summary>
        /// Địa chỉ Email gửi thông báo tự động (SMTP No-Reply).
        /// </summary>
        public string NoReplyEmail { get; set; } = "no-reply@canmakebeauty.vn";

        /// <summary>
        /// Màu chủ đạo Primary Rose (#E8618C).
        /// </summary>
        public string ThemeColor { get; set; } = "#E8618C";

        /// <summary>
        /// Màu nhấn đậm Deep Berry (#B8336A) đạt chuẩn tương phản WCAG AA với chữ trắng.
        /// </summary>
        public string AccentColor { get; set; } = "#B8336A";

        /// <summary>
        /// Dòng bản quyền chân trang mặc định.
        /// </summary>
        public string CopyrightText { get; set; } = "© 2026 CANMAKE BEAUTY. Tất cả quyền được bảo lưu.";

        /// <summary>
        /// Tuyên bố miễn trừ trách nhiệm học thuật hiển thị tại Footer và tài liệu.
        /// </summary>
        public string AcademicDisclaimer { get; set; } = "Lưu ý học thuật: CANMAKE BEAUTY trong hệ thống này là thương hiệu giả định phục vụ mục đích nghiên cứu & trình diễn Khóa luận Tốt nghiệp, độc lập và không liên kết thương mại với nhãn hiệu CANMAKE TOKYO (IDA Laboratories Co., Ltd., Nhật Bản).";
    }
}

