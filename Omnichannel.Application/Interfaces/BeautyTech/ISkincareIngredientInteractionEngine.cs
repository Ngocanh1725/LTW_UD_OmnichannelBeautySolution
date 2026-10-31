using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.BeautyTech;

namespace Omnichannel.Application.Interfaces.BeautyTech
{
    public interface ISkincareIngredientInteractionEngine
    {
        Task<SkincareRoutineAnalysisDto> AnalyzeCartOrRoutineAsync(
            List<int> productIds,
            int? userId = null,
            CancellationToken ct = default);

        Task<SkincareRoutineAnalysisDto> CheckIngredientConflictsAsync(
            List<string> ingredientNames,
            CancellationToken ct = default);
    }
}
