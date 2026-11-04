using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Cms;

namespace Omnichannel.Application.Interfaces.Cms
{
    public interface ILayoutEngineService
    {
        Task<PageLayoutDto> GetLayoutAsync(string pageKey, bool isDraft = false);
        Task<bool> SaveDraftLayoutAsync(string pageKey, SaveDraftLayoutRequest request);
        Task<bool> PublishLayoutAsync(string pageKey);
        Task<bool> UpdateThemeTokensAsync(ThemeTokensDto tokens);
        void InvalidateCache(string pageKey);
    }
}

