using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Notifications;
using Omnichannel.Application.Interfaces.Notifications;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services.Notifications
{
    public class InventoryNotificationService : IInventoryNotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<InventoryNotificationService> _logger;

        public InventoryNotificationService(
            ApplicationDbContext context,
            INotificationService notificationService,
            ILogger<InventoryNotificationService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<SubscriptionResultDto> SubscribeBackInStockAsync(
            SubscribeBackInStockRequest request,
            int? userId,
            CancellationToken cancellationToken = default)
        {
            if (request == null || request.ProductId <= 0)
            {
                return new SubscriptionResultDto { Success = false, Message = "Thông tin sản phẩm không hợp lệ." };
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);
            if (product == null)
            {
                return new SubscriptionResultDto { Success = false, Message = "Sản phẩm không tồn tại trong hệ thống." };
            }

            string? email = !string.IsNullOrWhiteSpace(request.Email) ? request.Email.Trim() : null;
            string? phone = !string.IsNullOrWhiteSpace(request.PhoneNumber) ? request.PhoneNumber.Trim() : null;
            string? name = !string.IsNullOrWhiteSpace(request.CustomerName) ? request.CustomerName.Trim() : null;

            if (!userId.HasValue && string.IsNullOrEmpty(email) && string.IsNullOrEmpty(phone))
            {
                return new SubscriptionResultDto
                {
                    Success = false,
                    Message = "Vui lòng đăng nhập hoặc nhập Email / Số điện thoại để nhận thông báo khi có hàng."
                };
            }

            if (userId.HasValue)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);
                if (user != null)
                {
                    if (string.IsNullOrEmpty(email)) email = user.Email;
                    if (string.IsNullOrEmpty(phone)) phone = user.PhoneNumber;
                    if (string.IsNullOrEmpty(name)) name = user.FullName;
                }
            }

            var existingSub = await _context.ProductSubscriptions
                .FirstOrDefaultAsync(s => s.ProductId == request.ProductId &&
                                         s.Type == "BACK_IN_STOCK" &&
                                         s.Status == "ACTIVE" &&
                                         ((userId.HasValue && s.UserId == userId.Value) ||
                                          (!string.IsNullOrEmpty(email) && s.Email == email) ||
                                          (!string.IsNullOrEmpty(phone) && s.PhoneNumber == phone)), cancellationToken);

            if (existingSub != null)
            {
                return new SubscriptionResultDto
                {
                    Success = true,
                    AlreadySubscribed = true,
                    SubscriptionId = existingSub.Id,
                    Message = "Bạn đã đăng ký nhận thông báo cho sản phẩm này rồi! Canmake Beauty sẽ báo ngay khi hàng về kho."
                };
            }

            var newSub = new ProductSubscription
            {
                ProductId = request.ProductId,
                UserId = userId,
                Type = "BACK_IN_STOCK",
                CustomerName = name ?? "Khách Hàng",
                Email = email,
                PhoneNumber = phone,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
                Notes = $"Đăng ký nhận tin khi có hàng lại cho sản phẩm '{product.Name}'"
            };

            await _context.ProductSubscriptions.AddAsync(newSub, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("[BackInStock Subscribed] Khách #{UserId} ({Email}) theo dõi sản phẩm #{ProductId} ({ProductName})",
                userId, email, product.Id, product.Name);

            return new SubscriptionResultDto
            {
                Success = true,
                SubscriptionId = newSub.Id,
                Message = $"🎉 Đăng ký thành công! Bạn sẽ nhận được thông báo ngay khi \"{product.Name}\" có hàng trở lại."
            };
        }

        public async Task<SubscriptionResultDto> SubscribePriceDropAsync(
            SubscribePriceDropRequest request,
            int? userId,
            CancellationToken cancellationToken = default)
        {
            if (request == null || request.ProductId <= 0 || request.TargetPrice <= 0)
            {
                return new SubscriptionResultDto { Success = false, Message = "Thông tin đăng ký không hợp lệ." };
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);
            if (product == null)
            {
                return new SubscriptionResultDto { Success = false, Message = "Sản phẩm không tồn tại trong hệ thống." };
            }

            decimal currentPrice = product.DiscountPrice.HasValue && product.DiscountPrice.Value > 0
                ? product.DiscountPrice.Value
                : product.SellingPrice;

            if (request.TargetPrice >= currentPrice)
            {
                return new SubscriptionResultDto
                {
                    Success = false,
                    Message = $"Mức giá kỳ vọng ({request.TargetPrice:N0} đ) phải thấp hơn giá hiện tại ({currentPrice:N0} đ)."
                };
            }

            string? email = !string.IsNullOrWhiteSpace(request.Email) ? request.Email.Trim() : null;
            string? phone = !string.IsNullOrWhiteSpace(request.PhoneNumber) ? request.PhoneNumber.Trim() : null;
            string? name = !string.IsNullOrWhiteSpace(request.CustomerName) ? request.CustomerName.Trim() : null;

            if (!userId.HasValue && string.IsNullOrEmpty(email) && string.IsNullOrEmpty(phone))
            {
                return new SubscriptionResultDto
                {
                    Success = false,
                    Message = "Vui lòng đăng nhập hoặc nhập Email / Số điện thoại để nhận thông báo giảm giá."
                };
            }

            if (userId.HasValue)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);
                if (user != null)
                {
                    if (string.IsNullOrEmpty(email)) email = user.Email;
                    if (string.IsNullOrEmpty(phone)) phone = user.PhoneNumber;
                    if (string.IsNullOrEmpty(name)) name = user.FullName;
                }
            }

            var existingSub = await _context.ProductSubscriptions
                .FirstOrDefaultAsync(s => s.ProductId == request.ProductId &&
                                         s.Type == "PRICE_DROP" &&
                                         s.Status == "ACTIVE" &&
                                         ((userId.HasValue && s.UserId == userId.Value) ||
                                          (!string.IsNullOrEmpty(email) && s.Email == email) ||
                                          (!string.IsNullOrEmpty(phone) && s.PhoneNumber == phone)), cancellationToken);

            if (existingSub != null)
            {
                existingSub.TargetPrice = request.TargetPrice;
                existingSub.Notes = $"Cập nhật mức giá kỳ vọng: {request.TargetPrice:N0} đ";
                await _context.SaveChangesAsync(cancellationToken);

                return new SubscriptionResultDto
                {
                    Success = true,
                    AlreadySubscribed = true,
                    SubscriptionId = existingSub.Id,
                    Message = $"Đã cập nhật mức giá theo dõi thành công: {request.TargetPrice:N0} đ cho sản phẩm \"{product.Name}\"."
                };
            }

            var newSub = new ProductSubscription
            {
                ProductId = request.ProductId,
                UserId = userId,
                Type = "PRICE_DROP",
                TargetPrice = request.TargetPrice,
                CustomerName = name ?? "Khách Hàng",
                Email = email,
                PhoneNumber = phone,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
                Notes = $"Theo dõi giảm giá về mức {request.TargetPrice:N0} đ cho sản phẩm '{product.Name}'"
            };

            await _context.ProductSubscriptions.AddAsync(newSub, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("[PriceDrop Subscribed] Khách #{UserId} theo dõi sản phẩm #{ProductId} ({ProductName}) - TargetPrice: {TargetPrice:N0}đ",
                userId, product.Id, product.Name, request.TargetPrice);

            return new SubscriptionResultDto
            {
                Success = true,
                SubscriptionId = newSub.Id,
                Message = $"📉 Đã kích hoạt theo dõi giá! Canmake Beauty sẽ báo ngay khi \"{product.Name}\" giảm về {request.TargetPrice:N0} đ."
            };
        }

        public async Task<List<CustomerSubscriptionDto>> GetCustomerSubscriptionsAsync(int userId, CancellationToken cancellationToken = default)
        {
            if (userId <= 0) return new List<CustomerSubscriptionDto>();

            var subs = await _context.ProductSubscriptions
                .AsNoTracking()
                .Include(s => s.Product)
                    .ThenInclude(p => p!.Batches)
                .Where(s => s.UserId == userId && s.Status != "CANCELLED")
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync(cancellationToken);

            var result = new List<CustomerSubscriptionDto>();
            foreach (var s in subs)
            {
                if (s.Product == null) continue;

                int activeStock = s.Product.Batches?
                    .Where(b => b.Status == "ACTIVE" && b.ExpDate > DateTime.UtcNow)
                    .Sum(b => b.CurrentQuantity) ?? 0;

                decimal currentPrice = s.Product.DiscountPrice.HasValue && s.Product.DiscountPrice.Value > 0
                    ? s.Product.DiscountPrice.Value
                    : s.Product.SellingPrice;

                result.Add(new CustomerSubscriptionDto
                {
                    Id = s.Id,
                    ProductId = s.ProductId,
                    ProductName = s.Product.Name,
                    ProductSlug = s.Product.Slug,
                    ProductThumbnailUrl = s.Product.ThumbnailUrl,
                    Type = s.Type,
                    TargetPrice = s.TargetPrice,
                    CurrentPrice = currentPrice,
                    Status = s.Status,
                    CreatedAt = s.CreatedAt,
                    NotifiedAt = s.NotifiedAt,
                    IsInStock = activeStock > 0,
                    AvailableStock = activeStock
                });
            }

            return result;
        }

        public async Task<bool> UnsubscribeAsync(int subscriptionId, int userId, CancellationToken cancellationToken = default)
        {
            if (subscriptionId <= 0 || userId <= 0) return false;

            var sub = await _context.ProductSubscriptions
                .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.UserId == userId, cancellationToken);

            if (sub == null) return false;

            sub.Status = "CANCELLED";
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("[Subscription Cancelled] Hủy đăng ký #{SubId} của User #{UserId}", subscriptionId, userId);
            return true;
        }

        public async Task<int> TriggerBackInStockAlertsAsync(int productId, int newQuantity, CancellationToken cancellationToken = default)
        {
            if (productId <= 0 || newQuantity <= 0) return 0;

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

            if (product == null) return 0;

            var activeSubs = await _context.ProductSubscriptions
                .Where(s => s.ProductId == productId && s.Type == "BACK_IN_STOCK" && s.Status == "ACTIVE")
                .ToListAsync(cancellationToken);

            if (!activeSubs.Any()) return 0;

            int count = 0;
            var now = DateTime.UtcNow;

            foreach (var sub in activeSubs)
            {
                sub.Status = "NOTIFIED";
                sub.NotifiedAt = now;
                count++;

                if (sub.UserId.HasValue)
                {
                    try
                    {
                        var notifDto = new CreateNotificationDto
                        {
                            UserId = sub.UserId.Value,
                            Type = "INVENTORY",
                            Severity = "SUCCESS",
                            Title = "Sản phẩm yêu thích đã có hàng trở lại! 🎉",
                            Message = $"Mỹ phẩm '{product.Name}' vừa về thêm {newQuantity} sản phẩm tại Canmake Beauty. Hãy nhanh tay đặt ngay kẻo hết!",
                            Url = $"/san-pham/{product.Slug}"
                        };

                        await _notificationService.CreateAndSendNotificationAsync(notifDto, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi gửi thông báo SignalR/DB cho User #{UserId}", sub.UserId.Value);
                    }
                }

                _logger.LogInformation("[MOCK EMAIL DISPATCH] Gửi thông báo có hàng tới {Contact} cho sản phẩm '{ProductName}' (SL mới: {Qty})",
                    sub.Email ?? sub.PhoneNumber ?? "User #" + sub.UserId, product.Name, newQuantity);
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("[BackInStock Triggered] Đã gửi thông báo có hàng cho {Count} khách hàng theo dõi sản phẩm #{ProductId} ({ProductName})",
                count, product.Id, product.Name);

            return count;
        }

        public async Task<int> TriggerPriceDropAlertsAsync(int productId, decimal newPrice, decimal? oldPrice = null, CancellationToken cancellationToken = default)
        {
            if (productId <= 0 || newPrice <= 0) return 0;

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

            if (product == null) return 0;

            var eligibleSubs = await _context.ProductSubscriptions
                .Where(s => s.ProductId == productId &&
                            s.Type == "PRICE_DROP" &&
                            s.Status == "ACTIVE" &&
                            s.TargetPrice.HasValue &&
                            s.TargetPrice.Value >= newPrice)
                .ToListAsync(cancellationToken);

            if (!eligibleSubs.Any()) return 0;

            int count = 0;
            var now = DateTime.UtcNow;

            foreach (var sub in eligibleSubs)
            {
                sub.Status = "NOTIFIED";
                sub.NotifiedAt = now;
                count++;

                if (sub.UserId.HasValue)
                {
                    try
                    {
                        var notifDto = new CreateNotificationDto
                        {
                            UserId = sub.UserId.Value,
                            Type = "PROMOTION",
                            Severity = "INFO",
                            Title = "Mỹ phẩm bạn theo dõi vừa giảm giá sốc! 📉",
                            Message = $"Sản phẩm '{product.Name}' hiện giảm còn {newPrice:N0} đ (mục tiêu bạn đặt: {(sub.TargetPrice ?? 0):N0} đ). Đặt mua ngay!",
                            Url = $"/san-pham/{product.Slug}"
                        };

                        await _notificationService.CreateAndSendNotificationAsync(notifDto, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi gửi thông báo giảm giá cho User #{UserId}", sub.UserId.Value);
                    }
                }

                _logger.LogInformation("[MOCK EMAIL DISPATCH] Gửi email giảm giá tới {Contact} cho sản phẩm '{ProductName}' (Giá mới: {NewPrice:N0}đ, Kỳ vọng: {Target:N0}đ)",
                    sub.Email ?? sub.PhoneNumber ?? "User #" + sub.UserId, product.Name, newPrice, sub.TargetPrice);
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("[PriceDrop Triggered] Đã gửi thông báo giảm giá cho {Count} khách hàng theo dõi sản phẩm #{ProductId} ({ProductName})",
                count, product.Id, product.Name);

            return count;
        }
    }
}

