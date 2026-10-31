using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Chat;

namespace Omnichannel.Application.Interfaces.Chat
{
    public interface IChatbotService
    {
        Task<ChatBotResponseDto> ProcessMessageAsync(ChatRequestDto request, int? userId = null, string? userName = null);
        Task<ChatHistoryViewModel> GetChatHistoryAsync(ChatHistoryFilterDto filter);
        Task<ChatSessionDetailDto?> GetSessionDetailAsync(int sessionId);
        Task<ChatSummaryStatsDto> GetSummaryStatsAsync();
        Task<bool> DeleteSessionAsync(int sessionId);
    }
}
