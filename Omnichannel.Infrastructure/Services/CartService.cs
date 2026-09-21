using System;
using System.Collections.Generic;
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
    /// <summary>
    /// Dịch vụ xử lý giỏ hàng đa kênh (Khách vãng lai theo Session và Thành viên theo UserId)
    /// </summary>
    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _context;

        public CartService(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<CartDto> GetOrCreateCartAsync(string? sessionId, string? userId, CancellationToken cancellationToken = default)
        {
            var cart = await FindActiveCartEntityAsync(sessionId, userId, cancellationToken);

            if (cart == null)
            {
                int? parsedUserId = null;
                if (!string.IsNullOrWhiteSpace(userId) && int.TryParse(userId, out var uid))
                {
                    parsedUserId = uid;
                }

                cart = new Cart
                {
                    CartSessionId = string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString() : sessionId,
                    UserId = parsedUserId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Carts.Add(cart);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return MapToDto(cart);
        }

        public async Task<CartDto?> GetCartAsync(string? sessionId, string? userId, CancellationToken cancellationToken = default)
        {
            var cart = await FindActiveCartEntityAsync(sessionId, userId, cancellationToken);
            return cart != null ? MapToDto(cart) : null;
        }

        public async Task<CartDto> AddToCartAsync(string? sessionId, string? userId, int productId, int? batchId, int quantity, CancellationToken cancellationToken = default)
        {
            if (quantity <= 0) quantity = 1;

            var cart = await FindActiveCartEntityAsync(sessionId, userId, cancellationToken);
            if (cart == null)
            {
                var newDto = await GetOrCreateCartAsync(sessionId, userId, cancellationToken);
                cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.Id == newDto.Id, cancellationToken);
            }

            if (cart == null)
            {
                throw new InvalidOperationException("Không thể khởi tạo giỏ hàng.");
            }

            var product = await _context.Products.FindAsync(new object[] { productId }, cancellationToken);
            if (product == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy sản phẩm có ID: {productId}");
            }

            // Kiểm tra xem sản phẩm cùng Lô (ProductBatch) đã có trong giỏ chưa
            var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                existingItem.UnitPrice = product.SellingPrice;
            }
            else
            {
                var newItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPrice = product.SellingPrice
                };
                cart.CartItems.Add(newItem);
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return MapToDto(cart);
        }

        public async Task<CartDto> UpdateQuantityAsync(string? sessionId, string? userId, int cartItemId, int quantity, CancellationToken cancellationToken = default)
        {
            var cart = await FindActiveCartEntityAsync(sessionId, userId, cancellationToken);
            if (cart == null)
            {
                throw new InvalidOperationException("Không tìm thấy giỏ hàng.");
            }

            var item = cart.CartItems.FirstOrDefault(ci => ci.Id == cartItemId);
            if (item != null)
            {
                if (quantity <= 0)
                {
                    cart.CartItems.Remove(item);
                    _context.CartItems.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }

                cart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return MapToDto(cart);
        }

        public async Task<CartDto> RemoveFromCartAsync(string? sessionId, string? userId, int cartItemId, CancellationToken cancellationToken = default)
        {
            var cart = await FindActiveCartEntityAsync(sessionId, userId, cancellationToken);
            if (cart == null)
            {
                throw new InvalidOperationException("Không tìm thấy giỏ hàng.");
            }

            var item = cart.CartItems.FirstOrDefault(ci => ci.Id == cartItemId);
            if (item != null)
            {
                cart.CartItems.Remove(item);
                _context.CartItems.Remove(item);
                cart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return MapToDto(cart);
        }

        public async Task ClearCartAsync(string? sessionId, string? userId, CancellationToken cancellationToken = default)
        {
            var cart = await FindActiveCartEntityAsync(sessionId, userId, cancellationToken);
            if (cart != null && cart.CartItems.Any())
            {
                _context.CartItems.RemoveRange(cart.CartItems);
                cart.CartItems.Clear();
                cart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task MergeCartsAsync(string sessionId, string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            int? parsedUserId = int.TryParse(userId, out var uid) ? uid : null;
            if (!parsedUserId.HasValue) return;

            var guestCart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.CartSessionId == sessionId && c.UserId == null, cancellationToken);

            if (guestCart == null || !guestCart.CartItems.Any())
            {
                return;
            }

            var userCart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == parsedUserId.Value, cancellationToken);

            if (userCart == null)
            {
                // Sửa lỗi dòng 321: Ép kiểu an toàn parsedUserId (int?) thay vì chuỗi userId (string)
                guestCart.UserId = parsedUserId.Value;
                guestCart.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                foreach (var guestItem in guestCart.CartItems)
                {
                    var existingItem = userCart.CartItems.FirstOrDefault(ci =>
                        ci.ProductId == guestItem.ProductId);

                    if (existingItem != null)
                    {
                        existingItem.Quantity += guestItem.Quantity;
                    }
                    else
                    {
                        userCart.CartItems.Add(new CartItem
                        {
                            CartId = userCart.Id,
                            ProductId = guestItem.ProductId,
                            Quantity = guestItem.Quantity,
                            UnitPrice = guestItem.UnitPrice
                        });
                    }
                }

                _context.CartItems.RemoveRange(guestCart.CartItems);
                _context.Carts.Remove(guestCart);
                userCart.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        // ================= HELPER METHODS =================
        private async Task<Cart?> FindActiveCartEntityAsync(string? sessionId, string? userId, CancellationToken cancellationToken)
        {
            int? parsedUserId = null;
            if (!string.IsNullOrWhiteSpace(userId) && int.TryParse(userId, out var uid))
            {
                parsedUserId = uid;
            }

            IQueryable<Cart> query = _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product);

            if (parsedUserId.HasValue)
            {
                return await query.FirstOrDefaultAsync(c => c.UserId == parsedUserId.Value, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                return await query.FirstOrDefaultAsync(c => c.CartSessionId == sessionId, cancellationToken);
            }

            return null;
        }

        private CartDto MapToDto(Cart cart)
        {
            var items = cart.CartItems.Select(ci => new CartItemDto
            {
                Id = ci.Id,
                CartId = ci.CartId,
                ProductId = ci.ProductId,
                ProductName = ci.Product?.Name ?? "Sản phẩm",
                ProductSku = ci.Product?.Sku,
                ProductImageUrl = ci.Product?.ThumbnailUrl,
                ProductBatchId = null,
                BatchNumber = null,
                ExpDate = null,
                Quantity = ci.Quantity,
                UnitPrice = ci.UnitPrice,
                DiscountAmount = 0,
                LineTotal = ci.UnitPrice * ci.Quantity
            }).ToList();

            var subTotal = items.Sum(i => i.UnitPrice * i.Quantity);
            var discountAmount = 0m;

            return new CartDto
            {
                Id = cart.Id,
                SessionId = cart.CartSessionId,
                UserId = cart.UserId,
                SubTotal = subTotal,
                DiscountAmount = discountAmount,
                TotalAmount = subTotal - discountAmount,
                TotalItems = items.Sum(i => i.Quantity),
                Items = items,
                CartItems = items
            };
        }
    }
}