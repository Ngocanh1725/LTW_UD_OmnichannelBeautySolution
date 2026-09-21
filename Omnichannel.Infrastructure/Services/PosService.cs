using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Inventory;
using Omnichannel.Application.DTOs.Sales;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services
{
    public class PosService : IPosService
    {
        private readonly ApplicationDbContext _context;
        private readonly IInventoryAllocationEngine _allocationEngine;

        public PosService(ApplicationDbContext context, IInventoryAllocationEngine allocationEngine)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _allocationEngine = allocationEngine ?? throw new ArgumentNullException(nameof(allocationEngine));
        }

        public async Task<List<PosProductDto>> SearchProductsAsync(string keyword, string? storeId = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return new List<PosProductDto>();
            }

            int.TryParse(keyword.Trim(), out var parsedProductId);
            int.TryParse(storeId, out var parsedStoreId);

            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Batches)
                    .ThenInclude(b => b.StoreInventories)
                .Where(p => p.IsActive);

            if (parsedProductId > 0)
            {
                query = query.Where(p => p.Id == parsedProductId || p.Barcode == keyword || p.Sku == keyword || p.Name.Contains(keyword));
            }
            else
            {
                query = query.Where(p => p.Barcode == keyword || p.Sku == keyword || p.Name.Contains(keyword));
            }

            var products = await query.Take(25).ToListAsync(cancellationToken);
            var result = new List<PosProductDto>();

            foreach (var p in products)
            {
                int availableStock = 0;
                if (parsedStoreId > 0)
                {
                    availableStock = p.Batches
                        .Where(b => b.ExpDate > DateTime.Today)
                        .SelectMany(b => b.StoreInventories.Where(si => si.StoreId == parsedStoreId))
                        .Sum(si => Math.Max(0, si.PhysicalQuantity - si.ReservedQuantity));
                }
                else
                {
                    availableStock = p.Batches
                        .Where(b => b.ExpDate > DateTime.Today)
                        .SelectMany(b => b.StoreInventories)
                        .Sum(si => Math.Max(0, si.PhysicalQuantity - si.ReservedQuantity));
                }

                result.Add(new PosProductDto
                {
                    ProductId = p.Id,
                    Barcode = p.Barcode,
                    Sku = p.Sku,
                    Name = p.Name,
                    SellingPrice = p.SellingPrice,
                    DiscountPrice = p.DiscountPrice ?? 0m,
                    FinalPrice = (p.DiscountPrice ?? 0m) > 0 ? p.DiscountPrice.Value : p.SellingPrice,
                    ImageUrl = p.ThumbnailUrl,
                    CategoryName = p.Category?.Name ?? "Mỹ phẩm",
                    AvailableQuantity = availableStock
                });
            }

            return result;
        }

        public async Task<PosReceiptDto> CheckoutAsync(PosCheckoutDto dto, string cashierUserId, CancellationToken cancellationToken = default)
        {
            if (dto.Items == null || !dto.Items.Any())
            {
                throw new InvalidOperationException("Hóa đơn thanh toán không có sản phẩm.");
            }

            int.TryParse(dto.StoreId, out var parsedStoreId);
            int.TryParse(cashierUserId, out var parsedCashierId);

            var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == parsedStoreId, cancellationToken);
            if (store == null)
            {
                throw new InvalidOperationException($"Không tìm thấy chi nhánh với ID: {dto.StoreId}");
            }

            var cashier = await _context.Users.FirstOrDefaultAsync(u => u.Id == parsedCashierId, cancellationToken);
            var cashierName = cashier?.FullName ?? "Thu ngân";

            decimal subTotal = 0;
            var orderDetails = new List<OrderDetail>();
            var allAllocations = new List<AllocatedBatchDto>();

            foreach (var item in dto.Items)
            {
                var product = await _context.Products.FindAsync(new object[] { item.ProductId }, cancellationToken);
                if (product == null) continue;

                var allocations = await _allocationEngine.AllocateBatchesFEFOAsync(dto.StoreId, product.Sku, item.Quantity, cancellationToken);
                if (allocations.Sum(a => a.AllocatedQuantity) < item.Quantity)
                {
                    throw new InvalidOperationException($"Sản phẩm '{product.Name}' không đủ tồn kho khả dụng tại chi nhánh.");
                }

                allAllocations.AddRange(allocations);

                foreach (var alloc in allocations)
                {
                    var unitPrice = product.SellingPrice;
                    var discount = item.DiscountAmount > 0 ? (item.DiscountAmount / item.Quantity) : 0;
                    var lineTotal = (unitPrice - discount) * alloc.AllocatedQuantity;
                    subTotal += lineTotal;

                    orderDetails.Add(new OrderDetail
                    {
                        ProductId = product.Id,
                        ProductBatchId = alloc.BatchId,
                        Quantity = alloc.AllocatedQuantity,
                        UnitPrice = unitPrice,
                        DiscountRate = unitPrice > 0 ? (discount / unitPrice) * 100m : 0,
                        LineTotal = lineTotal
                    });
                }
            }

            var totalAmount = Math.Max(0, subTotal - dto.DiscountAmount);
            var orderCode = $"POS{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(100, 999)}";

            var order = new Order
            {
                OrderCode = orderCode,
                StoreId = parsedStoreId,
                UserId = parsedCashierId > 0 ? parsedCashierId : null,
                CustomerName = dto.CustomerName ?? "Khách lẻ tại quầy",
                CustomerPhone = dto.CustomerPhone ?? string.Empty,
                SubTotal = subTotal,
                DiscountAmount = dto.DiscountAmount,
                TotalAmount = totalAmount,
                OrderStatus = "COMPLETED",
                PaymentStatus = "PAID",
                PaymentMethod = dto.PaymentMethod ?? "CASH",
                OrderSource = "POS",
                CreatedAt = DateTime.UtcNow,
                OrderDetails = orderDetails
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(cancellationToken);

            await _allocationEngine.DeductStockAsync(dto.StoreId, allAllocations, $"Bán hàng POS: {orderCode}", cancellationToken);

            var receiptItems = orderDetails.Select(od =>
            {
                var alloc = allAllocations.FirstOrDefault(a => a.BatchId == od.ProductBatchId);
                return new PosReceiptItemDto
                {
                    ProductName = alloc?.ProductName ?? "Sản phẩm",
                    BatchCode = alloc?.BatchNumber,
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                    DiscountAmount = 0,
                    LineTotal = od.LineTotal
                };
            }).ToList();

            return new PosReceiptDto
            {
                OrderId = order.Id,
                OrderCode = order.OrderCode,
                StoreName = store.Name,
                CashierName = cashierName,
                CustomerName = order.CustomerName,
                CustomerPhone = order.CustomerPhone,
                CreatedAt = order.CreatedAt,
                SubTotal = order.SubTotal,
                DiscountAmount = order.DiscountAmount,
                TotalAmount = order.TotalAmount,
                AmountReceived = dto.AmountReceived,
                ChangeAmount = Math.Max(0, dto.AmountReceived - totalAmount),
                PaymentMethod = order.PaymentMethod,
                Items = receiptItems
            };
        }

        public async Task<PosReceiptDto?> GetReceiptAsync(int orderId, CancellationToken cancellationToken = default)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Store)
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductBatch)
                .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

            return order != null ? MapToReceiptDto(order) : null;
        }

        public async Task<PosReceiptDto?> GetReceiptByCodeAsync(string orderCode, CancellationToken cancellationToken = default)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Store)
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductBatch)
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode, cancellationToken);

            return order != null ? MapToReceiptDto(order) : null;
        }

        private static PosReceiptDto MapToReceiptDto(Order order)
        {
            return new PosReceiptDto
            {
                OrderId = order.Id,
                OrderCode = order.OrderCode,
                StoreName = order.Store?.Name ?? "Chi nhánh",
                CashierName = order.User?.FullName ?? "Thu ngân",
                CustomerName = order.CustomerName,
                CustomerPhone = order.CustomerPhone,
                CreatedAt = order.CreatedAt,
                SubTotal = order.SubTotal,
                DiscountAmount = order.DiscountAmount,
                TotalAmount = order.TotalAmount,
                AmountReceived = order.TotalAmount,
                ChangeAmount = 0,
                PaymentMethod = order.PaymentMethod,
                // Sửa lỗi dòng 259: Lấy BatchNumber thay vì BatchCode
                Items = order.OrderDetails.Select(od => new PosReceiptItemDto
                {
                    ProductName = od.Product?.Name ?? "Sản phẩm",
                    BatchCode = od.ProductBatch?.BatchNumber,
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                    DiscountAmount = 0,
                    LineTotal = od.LineTotal
                }).ToList()
            };
        }
    }
}