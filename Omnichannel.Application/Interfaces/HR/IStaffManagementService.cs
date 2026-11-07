using System.Threading.Tasks;
using Omnichannel.Application.Common;
using Omnichannel.Application.DTOs.HR;

namespace Omnichannel.Application.Interfaces.HR
{
    public interface IStaffManagementService
    {
        Task<PagedResult<StaffListItemDto>> GetPagedAsync(int actorId, StaffFilterQuery query);
        Task<PagedResult<StaffListItemDto>> GetTrashPagedAsync(int actorId, StaffFilterQuery query);
        Task<StaffStatsDto> GetStatsAsync(int actorId);
        Task<StaffDetailDto?> GetDetailAsync(int actorId, int id);
        Task<OperationResult<int>> CreateAsync(int actorId, CreateStaffRequest request);
        Task<OperationResult> UpdateAsync(int actorId, int id, UpdateStaffRequest request);
        Task<OperationResult> SoftDeleteAsync(int actorId, int id);
        Task<OperationResult> RestoreAsync(int actorId, int id);
        Task<OperationResult> ToggleStatusAsync(int actorId, int id);
        Task<OperationResult<string>> ResetPasswordAsync(int actorId, ResetPasswordRequest request);
        Task<BulkStaffActionResult> BulkAsync(int actorId, BulkStaffActionRequest request);
        Task<byte[]> ExportAsync(int actorId, StaffFilterQuery query, string format);
        Task<bool> CheckUsernameExistsAsync(string username, int? excludeUserId = null);
        Task<bool> CheckEmailExistsAsync(string email, int? excludeUserId = null);
        Task<bool> CheckEmployeeCodeExistsAsync(string employeeCode, int? excludeUserId = null);
        Task<PagedResult<CustomerUserListItemDto>> GetCustomersPagedAsync(CustomerUserFilterQuery query);
        Task<OperationResult> ToggleCustomerStatusAsync(int actorId, int customerId);
    }
}

