using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Sales;

namespace Omnichannel.Application.Interfaces.Sales
{
    public interface ICartService
    {
        Task<CartDto> GetOrCreateCartAsync(string? sessionId, string? userId, CancellationToken cancellationToken = default);
        Task<CartDto?> GetCartAsync(string? sessionId, string? userId, CancellationToken cancellationToken = default);
        Task<CartDto> AddToCartAsync(string? sessionId, string? userId, int productId, int? batchId, int quantity, CancellationToken cancellationToken = default);
        Task<CartDto> UpdateQuantityAsync(string? sessionId, string? userId, int cartItemId, int quantity, CancellationToken cancellationToken = default);
        Task<CartDto> RemoveFromCartAsync(string? sessionId, string? userId, int cartItemId, CancellationToken cancellationToken = default);
        Task ClearCartAsync(string? sessionId, string? userId, CancellationToken cancellationToken = default);
        Task MergeCartsAsync(string sessionId, string userId, CancellationToken cancellationToken = default);
    }
}