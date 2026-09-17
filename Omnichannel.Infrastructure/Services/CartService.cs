using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Sales;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services
{
    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _context;

        public CartService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CartViewModel> GetCartAsync(string cartId, CancellationToken cancellationToken = default)
        {
            var cart = await _context.Carts
                .AsNoTracking()
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p.ProductBatches)
                .FirstOrDefaultAsync(c => c.CartId == cartId, cancellationToken);

            if (cart == null)
            {
                return new CartViewModel { CartId = cartId };
            }

            var today = DateTime.Today;

            var items = cart.CartItems.Select(ci =>
            {
                var nearestBatch = ci.Product.ProductBatches
                    .Where(b => b.ExpDate > today)
                    .OrderBy(b => b.ExpDate)
                    .FirstOrDefault();

                return new CartItemViewModel
                {
                    CartItemId = ci.CartItemId,
                    ProductId = ci.ProductId,
                    ProductName = ci.Product.ProductName,
                    Barcode = ci.Product.Barcode,
                    Slug = ci.Product.Slug,
                    UnitPrice = ci.Product.SellingPrice,
                    Quantity = ci.Quantity,
                    NearestExpDateStr = nearestBatch != null ? nearestBatch.ExpDate.ToString("MM/yyyy") : "Đang cập nhật",
                    ThumbnailUrl = "https://placehold.co/100x100/FDF2F8/7C3AED?text=Beauty"
                };
            }).ToList();

            return new CartViewModel
            {
                CartId = cartId,
                Items = items
            };
        }

        public async Task<bool> AddToCartAsync(string cartId, string productId, int quantity, CancellationToken cancellationToken = default)
        {
            if (quantity <= 0) return false;

            var product = await _context.Products.FindAsync(new object[] { productId }, cancellationToken);
            if (product == null || product.Status != 1) return false;

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.CartId == cartId, cancellationToken);

            if (cart == null)
            {
                cart = new Cart
                {
                    CartId = cartId,
                    LastActive = DateTime.UtcNow
                };
                await _context.Carts.AddAsync(cart, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.CartItems.Add(new CartItem
                {
                    CartId = cartId,
                    ProductId = productId,
                    Quantity = quantity,
                    AddedAt = DateTime.UtcNow
                });
            }

            cart.LastActive = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> UpdateQuantityAsync(string cartId, string productId, int delta, CancellationToken cancellationToken = default)
        {
            var item = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cartId && ci.ProductId == productId, cancellationToken);

            if (item == null) return false;

            item.Quantity += delta;
            if (item.Quantity <= 0)
            {
                _context.CartItems.Remove(item);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> RemoveItemAsync(string cartId, string productId, CancellationToken cancellationToken = default)
        {
            var item = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cartId && ci.ProductId == productId, cancellationToken);

            if (item == null) return false;

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task ClearCartAsync(string cartId, CancellationToken cancellationToken = default)
        {
            var items = await _context.CartItems.Where(ci => ci.CartId == cartId).ToListAsync(cancellationToken);
            if (items.Any())
            {
                _context.CartItems.RemoveRange(items);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
