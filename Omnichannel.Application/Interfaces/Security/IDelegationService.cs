using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Omnichannel.Application.Common;
using Omnichannel.Application.DTOs.Security;
using Omnichannel.Domain.Entities;

namespace Omnichannel.Application.Interfaces.Security
{
    public interface IDelegationService
    {
        Task<bool> IsSuperAdminAsync(int actorId);

        Task<DelegationContextDto> GetContextAsync(int actorId);

        Task<IReadOnlyList<int>> GetGrantablePermissionIdsAsync(int actorId);

        Task<IReadOnlyList<int>> GetAssignableRoleIdsAsync(int actorId);

        Task<bool> CanManageUserAsync(int actorId, int targetUserId);

        IQueryable<User> ApplyUserScope(IQueryable<User> query, DelegationContextDto ctx);

        Task<OperationResult> GrantUserPermissionAsync(
            int actorId,
            int targetUserId,
            int permissionId,
            bool isGranted,
            string? reason = null);

        Task<OperationResult> RemoveUserPermissionOverrideAsync(
            int actorId,
            int targetUserId,
            int permissionId);

        Task<OperationResult> AssignRolesAsync(
            int actorId,
            int targetUserId,
            IEnumerable<int> roleIds);

        Task<OperationResult> SaveDelegationScopeAsync(
            int actorId,
            SaveDelegationScopeRequest req);

        Task<OperationResult> CascadeRevokeAsync(int adminUserId);
    }
}

