using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Cms;
using Omnichannel.Domain.Entities;

namespace Omnichannel.Application.Interfaces.Cms
{
    public interface IMenuEngineService
    {
        /// <summary>
        /// Lấy cây phân cấp Menu theo mã (vd: HEADER_MAIN, FOOTER_COL1) kèm cơ chế giải quyết URL động và Cache
        /// </summary>
        Task<List<MenuItemDto>> GetHierarchyTreeAsync(string menuCode, bool onlyActive = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Giải quyết URL đích động theo TargetType (Category, Product, Post, StaticPage, CustomUrl)
        /// </summary>
        string ResolveUrl(MenuItem item);

        /// <summary>
        /// Xóa bộ đệm Cache của Menu khi có cập nhật từ Admin
        /// </summary>
        Task InvalidateMenuCacheAsync(string menuCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Thêm mới một nút Menu
        /// </summary>
        Task<bool> AddMenuItemAsync(CreateMenuItemViewModel model, CancellationToken cancellationToken = default);

        /// <summary>
        /// Xóa nút Menu và toàn bộ nhánh con
        /// </summary>
        Task<bool> DeleteMenuItemAsync(int menuItemId, CancellationToken cancellationToken = default);
    }
}