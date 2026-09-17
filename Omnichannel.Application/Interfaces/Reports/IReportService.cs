using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Reports;

namespace Omnichannel.Application.Interfaces.Reports
{
    public interface IReportService
    {
        Task<RevenueDashboardViewModel> GetRevenueDashboardAsync(string? storeId = null, CancellationToken cancellationToken = default);
    }
}
