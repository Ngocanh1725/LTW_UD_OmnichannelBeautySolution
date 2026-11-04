using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Catalog;

namespace Omnichannel.Application.Interfaces.Catalog
{
    /// <summary>
    /// Service cung cấp tính năng tìm kiếm gợi ý tức thì (Instant Search Suggestion)
    /// </summary>
    public interface ISearchSuggestService
    {
        /// <summary>
        /// Lấy danh sách gợi ý sản phẩm, thương hiệu, danh mục và từ khóa hot
        /// </summary>
        Task<SearchSuggestionDto> GetSuggestionsAsync(
            string? query,
            int maxProducts = 6,
            int maxBrands = 4,
            int maxCategories = 4,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Chuẩn hóa chuỗi tiếng Việt loại bỏ dấu và ký tự phụ trợ để so khớp không dấu
        /// </summary>
        string RemoveDiacritics(string text);
    }
}

