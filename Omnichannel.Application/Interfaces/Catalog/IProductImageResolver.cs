using System.Collections.Generic;

namespace Omnichannel.Application.Interfaces.Catalog
{
    /// <summary>
    /// Nguồn sự thật duy nhất (Single Source of Truth) để phân giải đường dẫn ảnh sản phẩm,
    /// ảnh WebP song hành, bộ sưu tập ảnh (Gallery), ảnh đại diện (Thumbnail), Alt Text chuẩn SEO
    /// và ảnh dự phòng (Fallback Placeholder) trên toàn hệ thống.
    /// </summary>
    public interface IProductImageResolver
    {
        /// <summary>
        /// Đường dẫn ảnh dự phòng mặc định tông hồng nhạt có logo mờ: /images/products/_placeholder.webp
        /// </summary>
        string FallbackImageUrl { get; }

        /// <summary>
        /// Đường dẫn ảnh dự phòng mặc định tông hồng nhạt có logo mờ: /images/products/_placeholder.webp
        /// </summary>
        string PlaceholderUrl { get; }

        /// <summary>
        /// Phân giải URL ảnh chính chuẩn (/images/products/{sub}/{file}) hoặc trả về PlaceholderUrl nếu sản phẩm chưa có ảnh thật riêng.
        /// </summary>
        string GetPrimaryImageUrl(
            string? candidateUrl = null,
            string? sku = null,
            string? slug = null,
            string? productName = null);

        /// <summary>
        /// Phân giải URL ảnh định dạng WebP (/images/products/{sub}/{file}.webp) phục vụ thẻ &lt;picture&gt;.
        /// </summary>
        string GetWebpImageUrl(
            string? candidateUrl = null,
            string? sku = null,
            string? slug = null,
            string? productName = null);

        /// <summary>
        /// Phân giải URL ảnh thu nhỏ (Thumbnail) cho Product Card, MiniCart, Cart, Checkout, POS, Search Suggest, Email/Print.
        /// </summary>
        string GetThumbnailUrl(
            string? candidateUrl = null,
            string? sku = null,
            string? slug = null,
            string? productName = null);

        /// <summary>
        /// Phân giải URL ảnh thu nhỏ (Thumbnail) cho Product Card, MiniCart, Cart, Checkout, POS, Search Suggest, Email/Print.
        /// </summary>
        string GetThumbnailImageUrl(
            string? candidateUrl = null,
            string? sku = null,
            string? slug = null,
            string? productName = null);

        /// <summary>
        /// Phân giải danh sách ảnh bộ sưu tập (Gallery) cho trang Chi tiết sản phẩm (PDP) và QuickView.
        /// Luôn trả về ít nhất 1 phần tử (ảnh chính hoặc PlaceholderUrl).
        /// </summary>
        IReadOnlyList<string> GetGalleryImageUrls(
            string? primaryUrl,
            string? imagesJson = null,
            string? sku = null,
            string? slug = null,
            string? productName = null);

        /// <summary>
        /// Sinh văn bản thay thế (alt text) tiếng Việt chuẩn SEO chứa Tên sản phẩm + Thương hiệu.
        /// </summary>
        string GetAltText(
            string? productName,
            string? brandName = null,
            string? categoryName = null,
            string? sku = null,
            string? slug = null);

        /// <summary>
        /// Chuyển đổi URL ảnh tương đối sang URL tuyệt đối (https://domain/images/products/...) cho Schema.org Product image và Open Graph.
        /// </summary>
        string GetAbsoluteImageUrl(
            string? relativeOrAbsoluteUrl,
            string? baseUrl,
            string? sku = null,
            string? slug = null,
            string? productName = null);

        /// <summary>
        /// Phân giải URL ảnh danh mục (/images/categories/{slug}.jpg) hoặc ảnh dự phòng, tránh link ngoài bị gãy.
        /// </summary>
        string GetCategoryImageUrl(
            string? categorySlug,
            string? candidateUrl = null);
    }
}