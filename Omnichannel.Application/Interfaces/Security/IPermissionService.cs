using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Omnichannel.Application.Interfaces.Security
{
    public interface IPermissionService
    {
        /// <summary>
        /// Lấy tập hợp các quyền hiệu dụng (Effective Permissions) của người dùng từ Cache hoặc CSDL
        /// </summary>
        Task<HashSet<string>> GetEffectivePermissionsAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Xóa cache quyền của người dùng khi SuperAdmin cập nhật ma trận phân quyền
        /// </summary>
        Task InvalidateUserPermissionCacheAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Kiểm tra nhanh người dùng có quyền cụ thể hay không
        /// </summary>
        Task<bool> HasPermissionAsync(string userId, string module, string action, CancellationToken cancellationToken = default);
    }
}
