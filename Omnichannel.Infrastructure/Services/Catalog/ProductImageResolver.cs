using System.Collections.Generic;
using Omnichannel.Application.Interfaces.Catalog;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services.Catalog
{
    /// <summary>
    /// Triển khai IProductImageResolver dựa trên nguồn chuẩn duy nhất ProductImageSourceRegistry.
    /// </summary>
    public class ProductImageResolver : IProductImageResolver
    {
        public string FallbackImageUrl => ProductImageSourceRegistry.PlaceholderUrl;

        public string PlaceholderUrl => ProductImageSourceRegistry.PlaceholderUrl;

        public string GetPrimaryImageUrl(
            string? candidateUrl = null,
            string? sku = null,
            string? slug = null,
            string? productName = null)
        {
            return ProductImageSourceRegistry.ResolveProductImageUrl(candidateUrl, sku, slug, productName);
        }

        public string GetWebpImageUrl(
            string? candidateUrl = null,
            string? sku = null,
            string? slug = null,
            string? productName = null)
        {
            return ProductImageSourceRegistry.ResolveWebpImageUrl(candidateUrl, sku, slug, productName);
        }

        public string GetThumbnailUrl(
            string? candidateUrl = null,
            string? sku = null,
            string? slug = null,
            string? productName = null)
        {
            return ProductImageSourceRegistry.ResolveProductImageUrl(candidateUrl, sku, slug, productName);
        }

        public string GetThumbnailImageUrl(
            string? candidateUrl = null,
            string? sku = null,
            string? slug = null,
            string? productName = null)
        {
            return ProductImageSourceRegistry.ResolveProductImageUrl(candidateUrl, sku, slug, productName);
        }

        public IReadOnlyList<string> GetGalleryImageUrls(
            string? primaryUrl,
            string? imagesJson = null,
            string? sku = null,
            string? slug = null,
            string? productName = null)
        {
            return ProductImageSourceRegistry.ResolveGalleryImageUrls(primaryUrl, imagesJson, sku, slug, productName);
        }

        public string GetAltText(
            string? productName,
            string? brandName = null,
            string? categoryName = null,
            string? sku = null,
            string? slug = null)
        {
            return ProductImageSourceRegistry.ResolveAltText(productName, brandName, categoryName, sku, slug);
        }

        public string GetAbsoluteImageUrl(
            string? relativeOrAbsoluteUrl,
            string? baseUrl,
            string? sku = null,
            string? slug = null,
            string? productName = null)
        {
            return ProductImageSourceRegistry.ResolveAbsoluteImageUrl(relativeOrAbsoluteUrl, baseUrl, sku, slug, productName);
        }

        public string GetCategoryImageUrl(
            string? categorySlug,
            string? candidateUrl = null)
        {
            return ProductImageSourceRegistry.ResolveCategoryImageUrl(categorySlug, candidateUrl);
        }
    }
}
