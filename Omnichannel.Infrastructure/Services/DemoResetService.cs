using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Settings;
using Omnichannel.Application.Interfaces.Settings;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services
{
    public class DemoResetService : IDemoResetService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DemoResetService> _logger;

        public DemoResetService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<DemoResetService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<DemoResetResultDto> ResetDemoDataAsync(int? performedByUserId, string? userName = null, CancellationToken cancellationToken = default)
        {
            bool isDemoMode = _configuration.GetValue<bool>("DemoMode:Enabled", true);
            if (!isDemoMode)
            {
                _logger.LogWarning("Yêu cầu Reset Demo Data bị từ chối: DemoMode:Enabled = false.");
                return new DemoResetResultDto
                {
                    Success = false,
                    Message = "Chế độ DemoMode đang tắt trong cấu hình hệ thống. Không thể khôi phục dữ liệu."
                };
            }

            _logger.LogInformation("Bắt đầu quy trình Reset Demo Data bởi UserId={UserId}, User={UserName}...", performedByUserId, userName);

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    // 1. Tìm tất cả đơn hàng phát sinh trong quá trình diễn tập (không mang mã ORD-SEED-)
                    var nonSeedOrders = await _context.Orders
                        .Include(o => o.OrderDetails)
                        .Include(o => o.Invoice)
                        .Include(o => o.PointTransactions)
                        .Include(o => o.CouponUsages)
                        .Where(o => !o.OrderCode.StartsWith("ORD-SEED-"))
                        .ToListAsync(cancellationToken);

                    int deletedOrdersCount = nonSeedOrders.Count;
                    int restoredStockUnits = 0;

                    foreach (var order in nonSeedOrders)
                    {
                        // Hoàn nguyên tồn kho cho từng sản phẩm/lô hàng trong đơn diễn tập
                        foreach (var detail in order.OrderDetails)
                        {
                            if (detail.ProductBatchId.HasValue && detail.Quantity > 0)
                            {
                                var storeInv = await _context.StoreInventories
                                    .FirstOrDefaultAsync(si => si.StoreId == order.StoreId && si.ProductBatchId == detail.ProductBatchId.Value, cancellationToken);
                                if (storeInv != null)
                                {
                                    storeInv.PhysicalQuantity += detail.Quantity;
                                    if (storeInv.ReservedQuantity >= detail.Quantity)
                                    {
                                        storeInv.ReservedQuantity -= detail.Quantity;
                                    }
                                    restoredStockUnits += detail.Quantity;
                                }

                                var batch = await _context.ProductBatches
                                    .FirstOrDefaultAsync(pb => pb.Id == detail.ProductBatchId.Value, cancellationToken);
                                if (batch != null)
                                {
                                    batch.CurrentQuantity += detail.Quantity;
                                }
                            }
                        }

                        // Xóa các bản ghi ràng buộc
                        if (order.Invoice != null)
                        {
                            _context.Invoices.Remove(order.Invoice);
                        }
                        if (order.PointTransactions.Any())
                        {
                            _context.PointTransactions.RemoveRange(order.PointTransactions);
                        }
                        if (order.CouponUsages.Any())
                        {
                            _context.CouponUsages.RemoveRange(order.CouponUsages);
                        }
                        if (order.OrderDetails.Any())
                        {
                            _context.OrderDetails.RemoveRange(order.OrderDetails);
                        }

                        _context.Orders.Remove(order);
                    }

                    // 2. Làm sạch toàn bộ giỏ hàng tạm vãng lai
                    var cartItems = await _context.CartItems.ToListAsync(cancellationToken);
                    int purgedCartsCount = cartItems.Count;
                    _context.CartItems.RemoveRange(cartItems);

                    var carts = await _context.Carts.ToListAsync(cancellationToken);
                    _context.Carts.RemoveRange(carts);

                    // 3. Khôi phục tồn kho các lô demo về trạng thái dồi dào chuẩn
                    var allStoreInventories = await _context.StoreInventories
                        .Include(si => si.ProductBatch)
                        .ToListAsync(cancellationToken);

                    int restoredBatchesCount = 0;
                    foreach (var si in allStoreInventories)
                    {
                        if (si.PhysicalQuantity < 20 && si.ProductBatch != null && si.ProductBatch.InitialQuantity > 0)
                        {
                            si.PhysicalQuantity = Math.Max(si.ProductBatch.InitialQuantity, 50);
                            si.ReservedQuantity = 0;
                            restoredBatchesCount++;
                        }
                    }

                    // 4. Ghi Audit Log hành động Reset Demo Data
                    var auditLog = new AuditLog
                    {
                        UserId = performedByUserId,
                        UserName = userName ?? "superadmin",
                        Action = "RESET_DEMO_DATA",
                        EntityType = "System",
                        EntityId = "DEMO_STATE",
                        OldValues = JsonSerializer.Serialize(new
                        {
                            DeletedOrders = deletedOrdersCount,
                            PurgedCartItems = purgedCartsCount
                        }),
                        NewValues = JsonSerializer.Serialize(new
                        {
                            Status = "SUCCESS",
                            RestoredStockUnits = restoredStockUnits,
                            RestoredBatchesCount = restoredBatchesCount,
                            ResetTime = DateTime.UtcNow
                        }),
                        CreatedAt = DateTime.UtcNow
                    };
                    await _context.AuditLogs.AddAsync(auditLog, cancellationToken);

                    // 5. Lưu và cam kết transaction
                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation("Reset Demo Data thành công: Đã xóa {Orders} đơn diễn tập, {Carts} giỏ hàng, hoàn nguyên {Units} sản phẩm.",
                        deletedOrdersCount, purgedCartsCount, restoredStockUnits);

                    return new DemoResetResultDto
                    {
                        Success = true,
                        Message = $"Khôi phục trạng thái demo chuẩn thành công! Đã xóa {deletedOrdersCount} đơn hàng diễn tập, dọn sạch giỏ hàng tạm và hoàn nguyên tồn kho các lô hàng.",
                        DeletedOrdersCount = deletedOrdersCount,
                        PurgedCartsCount = purgedCartsCount,
                        RestoredBatchesCount = restoredBatchesCount,
                        ResetAt = DateTime.UtcNow
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Lỗi khi thực thi Reset Demo Data: {Message}", ex.Message);
                    return new DemoResetResultDto
                    {
                        Success = false,
                        Message = "Có lỗi xảy ra trong quá trình khôi phục dữ liệu: " + ex.Message
                    };
                }
            });
        }
    }
}

