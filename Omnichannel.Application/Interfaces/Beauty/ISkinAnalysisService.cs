using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.BeautyTech;

namespace Omnichannel.Application.Interfaces.Beauty
{
    public interface ISkinAnalysisService
    {
        Task<SkinAnalysisResultDto> AnalyzeSkinImageAsync(
            byte[] imageBytes,
            string? fileName = null,
            string? contentType = null,
            CancellationToken ct = default);
    }
}
