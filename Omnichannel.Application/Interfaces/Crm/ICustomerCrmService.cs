using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Crm;

namespace Omnichannel.Application.Interfaces.Crm
{
    public interface ICustomerCrmService
    {
        Task<CustomerPagedListViewModel> GetCustomersPagedAsync(CustomerFilterRequest filter, CancellationToken ct = default);
        Task<Customer360ProfileViewModel?> GetCustomer360ProfileAsync(int userId, CancellationToken ct = default);
        Task<CustomerEditDto?> GetCustomerForEditAsync(int userId, CancellationToken ct = default);
        Task<bool> UpdateCustomerAsync(CustomerEditDto dto, CancellationToken ct = default);
        Task<List<MemberTierOptionDto>> GetMemberTiersAsync(CancellationToken ct = default);
    }
}
