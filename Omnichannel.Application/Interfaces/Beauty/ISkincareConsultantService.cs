using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.BeautyTech;

namespace Omnichannel.Application.Interfaces.Beauty
{
    public interface ISkincareConsultantService
    {
        Task<RoutineRecommendationResultDto> GenerateRoutineAsync(SkinQuizRequestDto request, CancellationToken ct = default);
    }
}

