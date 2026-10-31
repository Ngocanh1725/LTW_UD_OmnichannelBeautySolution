using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Cms;

namespace Omnichannel.Application.Interfaces.Cms
{
    public interface IThemeService
    {
        Task<ThemeConfigDto> GetCurrentThemeAsync();
        Task<bool> UpdateThemeAsync(ThemeConfigDto dto);
        void InvalidateCache();
    }
}
