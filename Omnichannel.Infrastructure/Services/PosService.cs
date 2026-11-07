using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Crm;
using Omnichannel.Application.DTOs.Inventory;
using Omnichannel.Application.DTOs.Sales;
using Omnichannel.Application.Interfaces.Crm;
using Omnichannel.Application.Interfaces.Loyalty;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services
{
    public class PosService : IPosService
    {
        private readonly ApplicationDbContext _context;
        private readonly IInventoryAllocationEngine _allocationEngine;
        private readonly ICustomerProfileService? _customerProfileService;
        private readonly ILoyaltyService? _loyaltyService;

        public PosService(
            ApplicationDbContext context,
            IInventoryAllocationEngine allocationEngine,
            ICustomerProfileService? customerProfileService = null,
            ILoyaltyService? loyaltyService = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _allocationEngine = allocationEngine ?? throw new ArgumentNullException(nameof(allocationEngine));
            _customerProfileService = customerProfileService;
            _loyaltyService = loyaltyService;
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
                    FinalPrice = (p.DiscountPrice.HasValue && p.DiscountPrice.Value > 0) ? p.DiscountPrice.Value : p.SellingPrice,
                    ImageUrl = Data.ProductImageSourceRegistry.ResolveProductImageUrl(p.ThumbnailUrl, p.Sku, p.Slug, p.Name),
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
            var orderCode = $"POS{DateTime.Now:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";

            // Tìm ca làm việc đang mở tại chi nhánh để liên kết đơn hàng
            var activeShift = await _context.PosShifts
                .FirstOrDefaultAsync(s => s.StoreId == parsedStoreId && s.Status == "OPEN", cancellationToken);

            var order = new Order
            {
                OrderCode = orderCode,
                StoreId = parsedStoreId,
                PosShiftId = activeShift?.Id,
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

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync(cancellationToken);

                    await _allocationEngine.DeductStockAsync(dto.StoreId, allAllocations, $"Bán hàng POS: {orderCode}", cancellationToken);

                    // Tự động tạo hóa đơn VAT
                    var invoice = new Invoice
                    {
                        InvoiceCode = $"INV-{DateTime.UtcNow:yyyyMMdd}-{order.Id:D4}",
                        OrderId = order.Id,
                        InvoiceDate = DateTime.UtcNow,
                        PaymentMethod = order.PaymentMethod,
                        TaxRate = 10.0m,
                        TaxAmount = Math.Round(order.TotalAmount - (order.TotalAmount / 1.1m)),
                        TotalAmount = order.TotalAmount,
                        CashierId = parsedCashierId > 0 ? parsedCashierId : null,
                        Status = "ISSUED",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Invoices.Add(invoice);

                    // Ghi nhận Sổ cái: Doanh thu POS (Credit)
                    var revenueLedger = new FinancialLedger
                    {
                        LedgerCode = $"LED-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                        EntryDate = DateTime.UtcNow,
                        EntryType = "REVENUE",
                        StoreId = parsedStoreId,
                        OrderId = order.Id,
                        CreditAmount = order.TotalAmount,
                        DebitAmount = 0m,
                        Description = $"Doanh thu bán lẻ tại quầy POS {order.OrderCode} qua {order.PaymentMethod}",
                        CreatedBy = parsedCashierId > 0 ? parsedCashierId : null,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.FinancialLedgers.Add(revenueLedger);

                    // Ghi nhận Sổ cái: Giá vốn hàng bán COGS (Debit) theo giá nhập lô FEFO
                    decimal posCogs = 0m;
                    foreach (var alloc in allAllocations)
                    {
                        var batch = await _context.ProductBatches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == alloc.BatchId, cancellationToken);
                        if (batch != null && batch.ImportCost > 0)
                        {
                            posCogs += (batch.ImportCost * alloc.AllocatedQuantity);
                        }
                    }

                    if (posCogs > 0)
                    {
                        var cogsLedger = new FinancialLedger
                        {
                            LedgerCode = $"LED-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                            EntryDate = DateTime.UtcNow,
                            EntryType = "COGS",
                            StoreId = parsedStoreId,
                            OrderId = order.Id,
                            DebitAmount = posCogs,
                            CreditAmount = 0m,
                            Description = $"Giá vốn hàng bán (COGS) xuất kho theo lô FEFO đơn POS {order.OrderCode}",
                            CreatedBy = parsedCashierId > 0 ? parsedCashierId : null,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.FinancialLedgers.Add(cogsLedger);
                    }

                    await _context.SaveChangesAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });

                // Tự động tích điểm thành viên nếu có số điện thoại khách hàng khớp User
                if (_loyaltyService != null && !string.IsNullOrWhiteSpace(order.CustomerPhone))
                {
                    try
                    {
                        var customerUser = await _context.Users.AsNoTracking()
                            .FirstOrDefaultAsync(u => u.PhoneNumber == order.CustomerPhone, cancellationToken);
                        if (customerUser != null)
                        {
                            await _loyaltyService.EarnPointsAsync(customerUser.Id, order.Id, order.TotalAmount, cancellationToken);
                        }
                    }
                    catch (Exception)
                    {
                        // Không làm gián đoạn luồng in hóa đơn POS nếu có lỗi tích điểm
                    }
                }

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

        public async Task<CustomerProfileDto?> LookupCustomerAsync(string phoneOrQr, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(phoneOrQr))
                return null;

            var term = phoneOrQr.Trim();

            // 1. Phân tích nếu là mã QR dạng "USER-123", "CUST-123", "QR-123" hoặc số Id
            int? resolvedUserId = null;
            if (term.StartsWith("USER-", StringComparison.OrdinalIgnoreCase) ||
                term.StartsWith("CUST-", StringComparison.OrdinalIgnoreCase) ||
                term.StartsWith("QR-", StringComparison.OrdinalIgnoreCase))
            {
                var idPart = term.Substring(term.IndexOf('-') + 1);
                if (int.TryParse(idPart, out var parsedId))
                {
                    resolvedUserId = parsedId;
                }
            }
            else if (int.TryParse(term, out var directId) && term.Length <= 8)
            {
                if (await _context.Users.AnyAsync(u => u.Id == directId, cancellationToken))
                {
                    resolvedUserId = directId;
                }
            }

            User? user = null;
            if (resolvedUserId.HasValue)
            {
                user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == resolvedUserId.Value, cancellationToken);
            }

            if (user == null)
            {
                // Tìm theo Số điện thoại, Username, hoặc Email
                user = await _context.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.PhoneNumber == term || u.Username == term || u.Email == term, cancellationToken);
            }

            if (user == null)
            {
                // Có thể mã QR là CustomerProfileId ("CP-123")
                if (term.StartsWith("CP-", StringComparison.OrdinalIgnoreCase))
                {
                    var idPart = term.Substring(3);
                    if (int.TryParse(idPart, out var cpId) && _customerProfileService != null)
                    {
                        return await _customerProfileService.GetProfileByIdAsync(cpId, cancellationToken);
                    }
                }
                return null;
            }

            if (_customerProfileService != null)
            {
                var profile = await _customerProfileService.GetProfileByUserIdAsync(user.Id, cancellationToken);
                if (profile != null)
                {
                    return profile;
                }
            }

            // Nếu chưa có CustomerProfile trong bảng CustomerProfiles, tạo DTO cơ bản từ thông tin User
            return new CustomerProfileDto
            {
                UserId = user.Id,
                CustomerName = user.FullName ?? user.Username,
                CustomerPhone = user.PhoneNumber,
                CustomerEmail = user.Email,
                CurrentSkinType = "Chưa cập nhật",
                SkinConcerns = new List<string>(),
                KnownAllergies = new List<string>(),
                PreferredBrands = new List<string>(),
                CreatedAt = DateTime.UtcNow
            };
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