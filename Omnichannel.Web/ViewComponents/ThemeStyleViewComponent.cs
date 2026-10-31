using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Omnichannel.Application.Interfaces.Cms;

namespace Omnichannel.Web.ViewComponents
{
    public class ThemeStyleViewComponent : ViewComponent
    {
        private readonly IThemeService _themeService;

        public ThemeStyleViewComponent(IThemeService themeService)
        {
            _themeService = themeService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var theme = await _themeService.GetCurrentThemeAsync();
            return View(theme);
        }
    }
}
