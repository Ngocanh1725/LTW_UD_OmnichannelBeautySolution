using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.DTOs.Sales;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Infrastructure.Services
{
    public class PosService : IPosService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PosService> _logger;

        public PosService(ApplicationDbContext context, ILogger<PosService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<PosProductSearchDto>> SearchProductsAsync(string storeId, string keyword, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return new List<PosProductSearchDto>();

            var cleanKeyword = keyword.Trim().ToLowerInvariant();
            var today = DateTime.Today;

            var inventories = await _context.StoreInventories
                .AsNoTracking()
                .Include(si => si.Batch)
                    .ThenInclude(b => b.Product)
                .Where(si => si.StoreId == storeId
                          && si.Batch.ExpDate > today
                          && (si.PhysicalQuantity - si.ReservedQuantity) > 0
                          && (si.Batch.Product.ProductName.ToLower().Contains(cleanKeyword)
                              || si.Batch.Product.ProductId.ToLower().Contains(cleanKeyword)
                              || si.Batch.Product.Barcode.Contains(cleanKeyword)))
                .OrderBy(si => si.Batch.ExpDate) // Luôn ưu tiên theo tiêu chuẩn FEFO
                .Take(20)
                .ToListAsync(cancellationToken);

            return inventories.Select(si => new PosProductSearchDto
            {
                ProductId = si.Batch.ProductId,
                Barcode = si.Batch.Product.Barcode,
                ProductName = si.Batch.Product.ProductName,
                SellingPrice = si.Batch.Product.SellingPrice,
                Unit = si.Batch.Product.Unit,
                AvailableQuantity = si.PhysicalQuantity - si.ReservedQuantity,
                BatchId = si.BatchId,
                BatchNumber = si.Batch.BatchNumber,
                ExpDateStr = si.Batch.ExpDate.ToString("dd/MM/yyyy")
            }).ToList();
        }

        public async Task<PosProductSearchDto?> GetProductByBarcodeAsync(string storeId, string barcode, CancellationToken cancellationToken = default)
        {
            var cleanBarcode = barcode.Trim();
            var today = DateTime.Today;

            // Tìm lô hàng còn hạn và date gần nhất của sản phẩm theo Barcode
            var inventory = await _context.StoreInventories
                .AsNoTracking()
                .Include(si => si.Batch)
                    .ThenInclude(b => b.Product)
                .Where(si => si.StoreId == storeId
                          && si.Batch.Product.Barcode == cleanBarcode
                          && si.Batch.ExpDate > today
                          && (si.PhysicalQuantity - si.ReservedQuantity) > 0)
                .OrderBy(si => si.Batch.ExpDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (inventory == null) return null;

            return new PosProductSearchDto
            {
                ProductId = inventory.Batch.ProductId,
                Barcode = inventory.Batch.Product.Barcode,
                ProductName = inventory.Batch.Product.ProductName,
                SellingPrice = inventory.Batch.Product.SellingPrice,
                Unit = inventory.Batch.Product.Unit,
                AvailableQuantity = inventory.PhysicalQuantity - inventory.ReservedQuantity,
                BatchId = inventory.BatchId,
                BatchNumber = inventory.Batch.BatchNumber,
                ExpDateStr = inventory.Batch.ExpDate.ToString("dd/MM/yyyy")
            };
        }

        public async Task<PosCheckoutResultDto> ProcessPosCheckoutAsync(PosCheckoutRequest request, string cashierUserId, CancellationToken cancellationToken = default)
        {
            if (!request.Items.Any())
            {
                return new PosCheckoutResultDto { Success = false, Message = "Hóa đơn chưa có sản phẩm nào." };
            }

            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // 1. Kiểm tra tồn kho khả dụng tức thời cho từng dòng
                foreach (var item in request.Items)
                {
                    var inventory = await _context.StoreInventories
                        .FirstOrDefaultAsync(si => si.StoreId == request.StoreId && si.BatchId == item.BatchId, cancellationToken);

                    if (inventory == null || (inventory.PhysicalQuantity - inventory.ReservedQuantity) < item.Quantity)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return new PosCheckoutResultDto
                        {
                            Success = false,
                            Message = $"Sản phẩm '{item.ProductName}' (Lô {item.BatchNumber}) không đủ số lượng tồn tại quầy."
                        };
                    }
                }

                // 2. Tính toán tài chính
                decimal subTotal = request.Items.Sum(i => i.LineTotal);
                decimal finalTotal = Math.Max(0, subTotal - request.OrderDiscountAmount);

                if (request.PaymentMethod == 1 && request.CashGiven < finalTotal)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new PosCheckoutResultDto { Success = false, Message = "Số tiền khách đưa không đủ để thanh toán." };
                }

                // 3. Tạo Đơn Hàng Bán Lẻ POS (Kênh 1)
                string orderId = $"POS-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

                var order = new Order
                {
                    OrderId = orderId,
                    OrderChannel = 1, // 1: POS Tại quầy
                    StoreId = request.StoreId,
                    CustomerId = null,
                    CreatedByStaffId = cashierUserId,
                    RecipientName = !string.IsNullOrWhiteSpace(request.CustomerName) ? request.CustomerName.Trim() : "Khách Lẻ Mua Tại Quầy",
                    RecipientPhone = !string.IsNullOrWhiteSpace(request.CustomerPhone) ? request.CustomerPhone.Trim() : "0900000000",
                    ShippingAddress = "Mua trực tiếp tại quầy thu ngân",
                    SubTotal = subTotal,
                    DiscountAmount = request.OrderDiscountAmount,
                    ShippingFee = 0,
                    FinalTotal = finalTotal,
                    PaymentMethod = request.PaymentMethod,
                    PaymentStatus = 2, // 2: Đã thanh toán ngay
                    OrderStatus = 5,   // 5: Hoàn tất ngay
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Orders.AddAsync(order, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                // 4. Trừ tồn kho vật lý và ghi sổ cái bất biến InventoryLogs
                foreach (var item in request.Items)
                {
                    var inventory = await _context.StoreInventories
                        .FirstAsync(si => si.StoreId == request.StoreId && si.BatchId == item.BatchId, cancellationToken);

                    inventory.PhysicalQuantity -= item.Quantity;
                    inventory.UpdatedAt = DateTime.UtcNow;

                    var orderDetail = new OrderDetail
                    {
                        OrderId = orderId,
                        ProductId = item.ProductId,
                        BatchId = item.BatchId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        DiscountRate = item.DiscountRate,
                        LineTotal = item.LineTotal
                    };
                    await _context.OrderDetails.AddAsync(orderDetail, cancellationToken);

                    var log = new InventoryLog
                    {
                        StoreId = request.StoreId,
                        BatchId = item.BatchId,
                        TransactionType = 2, // 2: Bán POS tại quầy
                        ReferenceDocNo = orderId,
                        QuantityChange = -item.Quantity,
                        RemainingQuantity = inventory.PhysicalQuantity,
                        CreatedBy = cashierUserId,
                        CreatedAt = DateTime.UtcNow,
                        Notes = $"Xuất bán tại quầy POS hóa đơn {orderId}"
                    };
                    await _context.InventoryLogs.AddAsync(log, cancellationToken);
                }

                // 5. Phát hành Hóa đơn VAT điện tử (Invoices)
                decimal totalBeforeTax = Math.Round(finalTotal / 1.1m, 2);
                decimal vatAmount = finalTotal - totalBeforeTax;
                string invoiceId = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
                long invoiceNumber = DateTime.UtcNow.Ticks % 10000000;
                string taxLookupCode = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();

                var invoice = new Invoice
                {
                    InvoiceId = invoiceId,
                    OrderId = orderId,
                    InvoiceSeries = "1C26TAA",
                    InvoiceNumber = invoiceNumber,
                    TaxLookupCode = taxLookupCode,
                    TotalBeforeTax = totalBeforeTax,
                    VatRate = 10.00m,
                    VatAmount = vatAmount,
                    TotalAfterTax = finalTotal,
                    CustomerTaxCode = null,
                    CompanyBillingName = null,
                    IssuedAt = DateTime.UtcNow,
                    IssuedByStaffId = cashierUserId,
                    IsCancelled = false
                };

                await _context.Invoices.AddAsync(invoice, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Thu ngân {UserId} thanh toán thành công hóa đơn POS {OrderId}.", cashierUserId, orderId);

                return new PosCheckoutResultDto
                {
                    Success = true,
                    Message = "Thanh toán thành công!",
                    OrderId = orderId,
                    InvoiceId = invoiceId,
                    InvoiceSeries = invoice.InvoiceSeries,
                    InvoiceNumber = invoiceNumber,
                    TaxLookupCode = taxLookupCode,
                    FinalTotal = finalTotal,
                    CashGiven = request.PaymentMethod == 1 ? request.CashGiven : finalTotal,
                    CreatedAt = order.CreatedAt
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình thanh toán quầy POS.");
                return new PosCheckoutResultDto { Success = false, Message = "Lỗi hệ thống khi chốt đơn thu ngân." };
            }
        }

        public async Task<PosReceiptPrintViewModel?> GetReceiptForPrintAsync(string orderId, CancellationToken cancellationToken = default)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Store)
                .Include(o => o.Staff)
                .Include(o => o.Invoice)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Batch)
                .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);

            if (order == null) return null;

            var items = order.OrderDetails.Select(od => new PosCartItemDto
            {
                ProductId = od.ProductId,
                ProductName = od.Product.ProductName,
                Barcode = od.Product.Barcode,
                BatchId = od.BatchId,
                BatchNumber = od.Batch.BatchNumber,
                ExpDateStr = od.Batch.ExpDate.ToString("MM/yyyy"),
                UnitPrice = od.UnitPrice,
                Quantity = od.Quantity,
                DiscountRate = od.DiscountRate
            }).ToList();

            return new PosReceiptPrintViewModel
            {
                StoreName = order.Store.StoreName,
                StoreAddress = order.Store.Address,
                StorePhone = order.Store.Phone,
                OrderId = order.OrderId,
                InvoiceId = order.Invoice?.InvoiceId ?? "N/A",
                InvoiceSeries = order.Invoice?.InvoiceSeries ?? "1C26TAA",
                InvoiceNumber = order.Invoice?.InvoiceNumber ?? 0,
                TaxLookupCode = order.Invoice?.TaxLookupCode ?? "N/A",
                CashierName = order.Staff?.FullName ?? "Thu Ngân Quầy",
                CustomerName = order.RecipientName,
                CustomerPhone = order.RecipientPhone,
                CreatedAt = order.CreatedAt,
                Items = items,
                SubTotal = order.SubTotal,
                DiscountAmount = order.DiscountAmount,
                VatRate = 10.00m,
                VatAmount = order.Invoice?.VatAmount ?? Math.Round(order.FinalTotal - (order.FinalTotal / 1.1m), 2),
                FinalTotal = order.FinalTotal,
                PaymentMethod = order.PaymentMethod,
                CashGiven = order.FinalTotal,
                ChangeAmount = 0
            };
        }
    }
}