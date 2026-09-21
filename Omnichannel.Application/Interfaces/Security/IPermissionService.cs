using System.Collections.Generic;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Security;

namespace Omnichannel.Application.Interfaces.Security
{
    public interface IPermissionService
    {
        Task<HashSet<string>> GetEffectivePermissionsAsync(int userId);
        Task<bool> HasPermissionAsync(int userId, string permissionCode);

        Task InvalidateUserPermissionCacheAsync(int userId);
        Task InvalidateRolePermissionCacheAsync(int roleId);

        Task<List<UserPermissionDetailDto>> GetUserPermissionBreakdownAsync(int userId);
    }
}