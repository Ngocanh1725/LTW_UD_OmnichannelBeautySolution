using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Sales;

namespace Omnichannel.Application.Interfaces.Sales
{
    public interface ICartService
    {
        Task<CartViewModel> GetCartAsync(string cartId, CancellationToken cancellationToken = default);
        Task<bool> AddToCartAsync(string cartId, string productId, int quantity, CancellationToken cancellationToken = default);
        Task<bool> UpdateQuantityAsync(string cartId, string productId, int delta, CancellationToken cancellationToken = default);
        Task<bool> RemoveItemAsync(string cartId, string productId, CancellationToken cancellationToken = default);
        Task ClearCartAsync(string cartId, CancellationToken cancellationToken = default);
    }
}