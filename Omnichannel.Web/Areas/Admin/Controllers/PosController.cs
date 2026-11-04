using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Application.DTOs.Sales;
using Omnichannel.Application.DTOs.Security;
using Omnichannel.Application.Interfaces.Loyalty;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Application.Interfaces.Security;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Security;
using Omnichannel.Web.Extensions;

namespace Omnichannel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class PosController : Controller
    {
        private readonly IPosService _posService;
        private readonly IPosShiftService _posShiftService;
        private readonly IPromotionEngine _promotionEngine;
        private readonly ILoyaltyService _loyaltyService;
        private readonly IStepUpElevationService _stepUpService;
        private readonly IOrderWorkflowService _orderWorkflow;
        private readonly ApplicationDbContext _context;

        public PosController(
            IPosService posService,
            IPosShiftService posShiftService,
            IPromotionEngine promotionEngine,
            ILoyaltyService loyaltyService,
            IStepUpElevationService stepUpService,
            IOrderWorkflowService orderWorkflow,
            ApplicationDbContext context)
        {
            _posService = posService;
            _posShiftService = posShiftService;
            _promotionEngine = promotionEngine;
            _loyaltyService = loyaltyService;
            _stepUpService = stepUpService;
            _orderWorkflow = orderWorkflow;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? storeId = null)
        {
            var effectiveStoreId = await GetEffectiveStoreIdAsync();
            var allStores = await _context.Stores
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            var currentStore = effectiveStoreId.HasValue
                ? allStores.Find(s => s.Id == effectiveStoreId.Value)
                : (!string.IsNullOrEmpty(storeId)
                    ? allStores.Find(s => s.Id.ToString() == storeId)
                    : allStores.FirstOrDefault());

            var stores = effectiveStoreId.HasValue
                ? allStores.Where(s => s.Id == effectiveStoreId.Value).ToList()
                : allStores;

            var cashierUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(cashierUserIdStr, out var cashierId);
            if (cashierId <= 0) cashierId = 4;
            var cashierName = User.FindFirstValue(ClaimTypes.GivenName) ?? User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(cashierName) || cashierName == "Thu Ngân Quầy")
            {
                cashierName = "Lê Thị Thu Ngân (POS-01)";
            }

            PosShiftSummaryDto? currentShift = null;
            if (currentStore != null)
            {
                var shift = await _context.PosShifts
                    .AsNoTracking()
                    .Include(s => s.Cashier)
                    .FirstOrDefaultAsync(s => s.StoreId == currentStore.Id && s.Status == "OPEN");

                if (shift != null)
                {
                    currentShift = new PosShiftSummaryDto
                    {
                        Id = shift.Id,
                        ShiftCode = shift.ShiftCode,
                        StoreId = shift.StoreId,
                        StoreName = currentStore.Name,
                        CashierId = shift.CashierId,
                        CashierName = shift.Cashier?.FullName ?? cashierName,
                        OpenedAt = shift.OpenedAt,
                        OpeningBalance = shift.OpeningBalance,
                        ExpectedCash = shift.ExpectedCash,
                        ActualCash = shift.ActualCash,
                        Difference = shift.Difference,
                        CashSalesTotal = shift.CashSalesTotal,
                        CardSalesTotal = shift.CardSalesTotal,
                        TransactionCount = shift.TransactionCount,
                        Status = shift.Status
                    };
                }
            }

            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();

            int pendingPickupCount = 0;
            if (currentStore != null)
            {
                pendingPickupCount = await _context.Orders
                    .AsNoTracking()
                    .CountAsync(o => o.DeliveryMethod == "STORE_PICKUP"
                                     && (o.PickupStoreId == currentStore.Id || (o.PickupStoreId == null && o.StoreId == currentStore.Id))
                                     && o.OrderStatus != "COMPLETED"
                                     && o.OrderStatus != "CANCELLED");
            }

            ViewBag.Stores = stores;
            ViewBag.CurrentStore = currentStore;
            ViewBag.CashierId = cashierId;
            ViewBag.CashierName = cashierName;
            ViewBag.CurrentShift = currentShift;
            ViewBag.Categories = categories;
            ViewBag.PendingPickupCount = pendingPickupCount;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetVisualCatalog(string storeId, int? categoryId = null)
        {
            var categories = await _context.Categories.AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .Select(c => new { c.Id, c.Name, c.Slug })
                .ToListAsync();

            var query = _context.Products.AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.IsActive);

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            var products = await query.Take(40).ToListAsync();
            int.TryParse(storeId, out var parsedStoreId);

            var productIds = products.Select(p => p.Id).ToList();
            var stockDict = new Dictionary<int, int>();

            if (parsedStoreId > 0 && productIds.Any())
            {
                var inventoryList = await _context.StoreInventories.AsNoTracking()
                    .Where(si => si.StoreId == parsedStoreId && productIds.Contains(si.ProductBatch.ProductId))
                    .Select(si => new { ProductId = si.ProductBatch.ProductId, Available = si.PhysicalQuantity - si.ReservedQuantity })
                    .ToListAsync();

                stockDict = inventoryList
                    .GroupBy(si => si.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(si => si.Available > 0 ? si.Available : 0));
            }

            var result = products.Select(p => new PosProductDto
            {
                ProductId = p.Id,
                Barcode = p.Barcode,
                Sku = p.Sku,
                Name = p.Name,
                SellingPrice = p.SellingPrice,
                DiscountPrice = p.DiscountPrice ?? 0m,
                FinalPrice = (p.DiscountPrice.HasValue && p.DiscountPrice.Value > 0) ? p.DiscountPrice.Value : p.SellingPrice,
                ImageUrl = p.ThumbnailUrl,
                CategoryName = p.Category?.Name ?? "Mỹ phẩm",
                AvailableQuantity = stockDict.TryGetValue(p.Id, out var qty) ? qty : 0
            }).ToList();

            return Json(new { success = true, categories, products = result });
        }

        [HttpGet]
        public async Task<IActionResult> SearchProducts(string storeId, string keyword)
        {
            var results = await _posService.SearchProductsAsync(keyword, storeId);
            return Json(results);
        }

        [HttpGet]
        public async Task<IActionResult> ScanBarcode(string storeId, string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                return Json(new { success = false, message = "Mã barcode/SKU không được để trống." });
            }

            var results = await _posService.SearchProductsAsync(barcode.Trim(), storeId);
            var exactMatch = results.FirstOrDefault(p => 
                string.Equals(p.Barcode, barcode.Trim(), StringComparison.OrdinalIgnoreCase) || 
                string.Equals(p.Sku, barcode.Trim(), StringComparison.OrdinalIgnoreCase)) 
                ?? results.FirstOrDefault();

            if (exactMatch == null)
            {
                return Json(new { success = false, message = $"Không tìm thấy sản phẩm có mã '{barcode}'." });
            }

            return Json(new { success = true, product = exactMatch });
        }

        [HttpGet]
        public async Task<IActionResult> LookupCustomer(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return Json(new { success = false, message = "Vui lòng nhập số điện thoại hoặc mã khách hàng." });
            }

            var customer = await _posService.LookupCustomerAsync(keyword.Trim());
            if (customer == null)
            {
                return Json(new { success = false, message = "Không tìm thấy hồ sơ khách hàng với thông tin đã nhập." });
            }

            var wallet = await _context.LoyaltyWallets
                .AsNoTracking()
                .Include(w => w.CurrentTier)
                .FirstOrDefaultAsync(w => w.UserId == customer.UserId);

            var tierName = wallet?.CurrentTier?.TierName ?? "Silver";
            var points = wallet?.CurrentBalance ?? 0;
            var pointMultiplier = wallet?.CurrentTier?.PointMultiplier ?? 1.0m;

            decimal vipDiscountRate = tierName.ToLower() switch
            {
                var t when t.Contains("diamond") || t.Contains("kim cương") => 15m,
                var t when t.Contains("platinum") || t.Contains("bạch kim") => 12m,
                var t when t.Contains("gold") || t.Contains("vàng") => 10m,
                var t when t.Contains("silver") || t.Contains("bạc") => 5m,
                _ => 0m
            };

            return Json(new
            {
                success = true,
                data = new
                {
                    userId = customer.UserId,
                    name = customer.CustomerName,
                    phone = customer.CustomerPhone,
                    email = customer.CustomerEmail,
                    skinType = customer.CurrentSkinType,
                    tierName = tierName,
                    points = points,
                    pointRate = 1000, // 1 điểm = 1.000 VNĐ
                    maxPointDiscount = points * 1000,
                    vipDiscountRate = vipDiscountRate,
                    pointMultiplier = pointMultiplier,
                    allergies = customer.KnownAllergies
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("POS_ACCESS")]
        public async Task<IActionResult> ApplyCoupon([FromBody] ApplyCouponRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.CouponCode))
            {
                return Json(new { success = false, message = "Vui lòng nhập mã voucher / coupon." });
            }

            var code = request.CouponCode.Trim();
            var coupon = await _promotionEngine.GetCouponByCodeAsync(code);

            if (coupon == null || !coupon.IsActive)
            {
                return Json(new { success = false, message = $"Mã voucher '{code}' không tồn tại hoặc đã hết hạn sử dụng." });
            }

            if (request.SubTotal < coupon.MinOrderValue)
            {
                return Json(new { success = false, message = $"Đơn hàng tối thiểu {coupon.MinOrderValue:N0} đ để áp dụng voucher này." });
            }

            decimal discountAmount = 0;
            if (coupon.DiscountType == "PERCENTAGE")
            {
                discountAmount = request.SubTotal * (coupon.DiscountValue / 100m);
                if (coupon.MaxDiscountAmount.HasValue && discountAmount > coupon.MaxDiscountAmount.Value)
                {
                    discountAmount = coupon.MaxDiscountAmount.Value;
                }
            }
            else
            {
                discountAmount = Math.Min(coupon.DiscountValue, request.SubTotal);
            }

            return Json(new
            {
                success = true,
                couponCode = coupon.CouponCode,
                discountAmount = discountAmount,
                message = $"Áp dụng voucher {coupon.CouponCode} thành công: giảm {discountAmount:N0} đ"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("POS_CHECKOUT")]
        public async Task<IActionResult> Checkout([FromBody] PosCheckoutDto request)
        {
            if (request == null || request.Items == null || !request.Items.Any())
            {
                return Json(new { success = false, message = "Giỏ hàng không có sản phẩm nào để thanh toán." });
            }

            var effectiveStoreId = await GetEffectiveStoreIdAsync();
            if (effectiveStoreId.HasValue)
            {
                if (int.TryParse(request.StoreId, out var reqStoreId) && reqStoreId != effectiveStoreId.Value)
                {
                    return Json(new { success = false, message = "Bạn không có quyền thanh toán cho chi nhánh khác." });
                }
                request.StoreId = effectiveStoreId.Value.ToString();
            }

            var cashierId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1";
            try
            {
                var result = await _posService.CheckoutAsync(request, cashierId);

                // Cấn trừ điểm tích lũy thành viên nếu có
                if (request.PointsToRedeem > 0 && request.CustomerUserId.HasValue)
                {
                    try
                    {
                        await _loyaltyService.RedeemPointsAsync(
                            request.CustomerUserId.Value,
                            request.PointsToRedeem,
                            $"Cấn trừ điểm thanh toán đơn hàng POS {result.OrderCode}");
                    }
                    catch (Exception)
                    {
                        // Không làm gián đoạn đơn nếu lỗi trừ điểm
                    }
                }

                return Json(new
                {
                    success = true,
                    orderId = result.OrderCode,
                    orderNumber = result.OrderId,
                    totalAmount = result.TotalAmount,
                    taxLookupCode = $"VAT-{result.OrderId:D8}",
                    message = $"Thanh toán thành công đơn hàng {result.OrderCode}!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [HasPermission("POS_ACCESS")]
        public async Task<IActionResult> PrintReceipt(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId)) return NotFound();

            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Store)
                .Include(o => o.User)
                .Include(o => o.Invoice)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails).ThenInclude(od => od.ProductBatch)
                .FirstOrDefaultAsync(o => o.OrderCode == orderId || o.Id.ToString() == orderId);

            if (order == null) return NotFound();

            string vipTier = "Thành viên Thân Thiết";
            int currentPoints = 0;
            int earnedPoints = (int)(order.TotalAmount / 10000); // 10.000 VNĐ = 1 điểm cơ sở

            if (order.UserId.HasValue || !string.IsNullOrEmpty(order.CustomerPhone))
            {
                var custUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u =>
                    (order.UserId.HasValue && u.Id == order.UserId.Value) ||
                    (!string.IsNullOrEmpty(order.CustomerPhone) && u.PhoneNumber == order.CustomerPhone));

                if (custUser != null)
                {
                    var wallet = await _context.LoyaltyWallets.AsNoTracking()
                        .Include(w => w.CurrentTier)
                        .FirstOrDefaultAsync(w => w.UserId == custUser.Id);

                    if (wallet != null)
                    {
                        vipTier = wallet.CurrentTier?.TierName ?? "Silver";
                        currentPoints = wallet.CurrentBalance;
                    }
                }
            }

            var printModel = new PosReceiptPrintViewModel
            {
                OrderId = order.OrderCode,
                StoreName = order.Store?.Name ?? "Omnichannel Beauty Solution",
                StoreAddress = order.Store?.Address ?? "Hồ Chí Minh, Việt Nam",
                StorePhone = order.Store?.PhoneNumber ?? "1900 1234",
                InvoiceSeries = "1C26TBB",
                InvoiceNumber = order.Invoice?.InvoiceCode ?? $"HD-{order.Id:D6}",
                CreatedAt = order.CreatedAt,
                CashierName = order.User?.FullName ?? "Thu Ngân Quầy",
                CustomerName = string.IsNullOrEmpty(order.CustomerName) ? "Khách lẻ tại quầy" : order.CustomerName,
                CustomerPhone = order.CustomerPhone ?? "",
                CustomerVipTier = vipTier,
                LoyaltyPointsEarned = earnedPoints,
                CurrentLoyaltyPoints = currentPoints,
                SubTotal = order.SubTotal,
                DiscountAmount = order.DiscountAmount,
                VatAmount = Math.Round(order.TotalAmount - (order.TotalAmount / 1.1m)),
                FinalTotal = order.TotalAmount,
                PaymentMethodName = order.PaymentMethod switch
                {
                    "CASH" => "Tiền mặt",
                    "VNPAY" or "VIETQR" => "Chuyển khoản VietQR",
                    "CREDIT_CARD" or "CARD" => "Thẻ ngân hàng (POS)",
                    _ => order.PaymentMethod
                },
                CashGiven = order.TotalAmount,
                ChangeAmount = 0,
                TaxLookupCode = order.Invoice?.InvoiceCode ?? $"VAT-{order.Id:D8}",
                Items = order.OrderDetails.Select(od => new PosReceiptPrintItemViewModel
                {
                    ProductName = od.Product?.Name ?? "Sản phẩm",
                    ProductSku = od.Product?.Sku ?? string.Empty,
                    BatchNumber = od.ProductBatch?.BatchNumber ?? "L01",
                    ExpDateStr = od.ProductBatch?.ExpDate.ToString("dd/MM/yyyy") ?? "",
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                    LineTotal = od.LineTotal
                }).ToList()
            };

            return View(printModel);
        }

        #region Shift Management Actions

        [HttpGet]
        public async Task<IActionResult> GetShiftStatus(int storeId)
        {
            var shift = await _context.PosShifts
                .AsNoTracking()
                .Include(s => s.Store)
                .Include(s => s.Cashier)
                .FirstOrDefaultAsync(s => s.StoreId == storeId && s.Status == "OPEN");

            if (shift == null)
            {
                return Json(new { hasOpenShift = false });
            }

            // Realtime metrics for current shift
            var shiftOrders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.PosShiftId == shift.Id && o.OrderStatus == "COMPLETED")
                .ToListAsync();

            var cashSales = shiftOrders.Where(o => o.PaymentMethod == "CASH").Sum(o => o.TotalAmount);
            var cardSales = shiftOrders.Where(o => o.PaymentMethod != "CASH").Sum(o => o.TotalAmount);

            // Giao dịch thu/chi trong két (Add Float / Drop Cash)
            var drawerTxs = await _context.PosDrawerTransactions
                .AsNoTracking()
                .Where(t => t.PosShiftId == shift.Id)
                .ToListAsync();

            var cashIn = drawerTxs.Where(t => t.Type == "CASH_IN").Sum(t => t.Amount);
            var cashOut = drawerTxs.Where(t => t.Type == "CASH_OUT" || t.Type == "PETTY_CASH").Sum(t => t.Amount);
            var expectedCash = shift.OpeningBalance + cashSales + cashIn - cashOut;

            return Json(new
            {
                hasOpenShift = true,
                shift = new
                {
                    id = shift.Id,
                    shiftCode = shift.ShiftCode,
                    storeName = shift.Store?.Name,
                    cashierName = shift.Cashier?.FullName ?? "Thu ngân",
                    openedAt = shift.OpenedAt.ToString("dd/MM/yyyy HH:mm"),
                    openingBalance = shift.OpeningBalance,
                    cashSales = cashSales,
                    cardSales = cardSales,
                    cashIn = cashIn,
                    cashOut = cashOut,
                    totalSales = cashSales + cardSales,
                    expectedCash = expectedCash,
                    orderCount = shiftOrders.Count
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("POS_SHIFT_OPEN")]
        public async Task<IActionResult> OpenShift([FromBody] OpenShiftDto dto)
        {
            if (dto.StoreId <= 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn chi nhánh hợp lệ." });
            }

            var effectiveStoreId = await GetEffectiveStoreIdAsync();
            if (effectiveStoreId.HasValue && dto.StoreId != effectiveStoreId.Value)
            {
                return Json(new { success = false, message = "Bạn không có quyền mở ca cho chi nhánh khác." });
            }

            var cashierUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(cashierUserIdStr, out var cashierId);
            if (dto.CashierId <= 0) dto.CashierId = cashierId > 0 ? cashierId : 1;

            try
            {
                var result = await _posShiftService.OpenShiftAsync(dto);
                return Json(new
                {
                    success = true,
                    message = $"Đã mở ca làm việc {result.ShiftCode} thành công!",
                    shift = result
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("POS_SHIFT_CLOSE")]
        public async Task<IActionResult> CloseShift([FromBody] CloseShiftDto dto)
        {
            if (dto.ShiftId <= 0)
            {
                return Json(new { success = false, message = "Mã ca làm việc không hợp lệ." });
            }

            try
            {
                var result = await _posShiftService.CloseShiftAsync(dto);
                return Json(new
                {
                    success = true,
                    message = $"Chốt ca làm việc {result.ShiftCode} thành công!",
                    data = new
                    {
                        shiftCode = result.ShiftCode,
                        openingBalance = result.OpeningBalance,
                        cashSalesTotal = result.CashSalesTotal,
                        cardSalesTotal = result.CardSalesTotal,
                        expectedCash = result.ExpectedCash,
                        actualCash = result.ActualCash,
                        difference = result.Difference,
                        transactionCount = result.TransactionCount
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("POS_REFUND")]
        public async Task<IActionResult> RefundOrder(int orderId, string reason)
        {
            var effectiveStoreId = await GetEffectiveStoreIdAsync();
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng cần hoàn trả." });
            }

            if (effectiveStoreId.HasValue && order.StoreId != effectiveStoreId.Value)
            {
                return Json(new { success = false, message = "Bạn không có quyền hoàn tiền đơn hàng của chi nhánh khác." });
            }

            order.OrderStatus = "REFUNDED";
            order.PaymentStatus = "REFUNDED";
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"Đã xác nhận hoàn tiền cho đơn hàng {order.OrderCode}." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("POS_ACCESS")]
        public async Task<IActionResult> AddDrawerTransaction([FromBody] DrawerTransactionDto dto, int shiftId = 0)
        {
            if (dto == null || dto.Amount <= 0)
            {
                return Json(new { success = false, message = "Số tiền giao dịch két phải lớn hơn 0." });
            }

            if (string.IsNullOrWhiteSpace(dto.Reason))
            {
                return Json(new { success = false, message = "Vui lòng nhập lý do giao dịch két." });
            }

            var cashierUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(cashierUserIdStr, out var cashierId);
            dto.PerformedBy = cashierId > 0 ? cashierId : 1;

            try
            {
                if (shiftId <= 0)
                {
                    var effectiveStoreId = await GetEffectiveStoreIdAsync();
                    var query = _context.PosShifts.Where(s => s.Status == "OPEN");
                    if (effectiveStoreId.HasValue) query = query.Where(s => s.StoreId == effectiveStoreId.Value);
                    var currentShift = await query.FirstOrDefaultAsync();
                    if (currentShift == null)
                    {
                        return Json(new { success = false, message = "Không có ca làm việc nào đang mở để thực hiện giao dịch két." });
                    }
                    shiftId = currentShift.Id;
                }

                await _posShiftService.AddDrawerTransactionAsync(shiftId, dto);
                string txName = dto.Type?.ToUpperInvariant() == "CASH_IN" ? "Bổ sung tiền lẻ (Add Float)" : "Rút bớt tiền nộp két lớn (Drop Cash)";
                return Json(new { success = true, message = $"Ghi nhận giao dịch {txName} thành công: {dto.Amount:N0} đ!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region 5. XÁC THỰC BƯỚC NHẢY (STEP-UP PIN AUTHENTICATION CHO QUẦY POS)

        [HttpPost]
        [Route("Admin/Pos/RequestStepUpElevation")]
        public async Task<IActionResult> RequestStepUpElevation([FromBody] Omnichannel.Application.DTOs.Security.StepUpRequestDto dto)
        {
            if (dto == null)
            {
                return Json(new { success = false, message = "Dữ liệu yêu cầu phê duyệt không hợp lệ." });
            }

            int cashierUserId = User.GetUserId() ?? 0;
            var effectiveStoreId = await GetEffectiveStoreIdAsync();
            int cashierStoreId = effectiveStoreId ?? User.GetStoreId() ?? 0;

            var result = await _stepUpService.IssueElevationTokenAsync(cashierUserId, cashierStoreId, dto);
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/Pos/CancelOrderWithStepUp")]
        public async Task<IActionResult> CancelOrderWithStepUp(int orderId, string reason, string elevationToken)
        {
            int cashierUserId = User.GetUserId() ?? 0;

            try
            {
                await _stepUpService.ValidateAndConsumeTokenAsync(elevationToken, cashierUserId, "POS_ORDER_CANCEL", orderId.ToString());
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Xác thực Quản lý thất bại: {ex.Message}" });
            }

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng cần hủy." });
            }

            var effectiveStoreId = await GetEffectiveStoreIdAsync();
            if (effectiveStoreId.HasValue && order.StoreId != effectiveStoreId.Value)
            {
                return Json(new { success = false, message = "Bạn không có quyền hủy đơn hàng của chi nhánh khác." });
            }

            order.OrderStatus = "CANCELLED";
            order.PaymentStatus = "CANCELLED";
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Đã hủy hóa đơn POS {order.OrderCode} thành công với sự phê duyệt của Quản lý." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/Pos/OpenCashDrawerNoSaleWithStepUp")]
        public async Task<IActionResult> OpenCashDrawerNoSaleWithStepUp(string reason, string elevationToken)
        {
            int cashierUserId = User.GetUserId() ?? 0;

            try
            {
                await _stepUpService.ValidateAndConsumeTokenAsync(elevationToken, cashierUserId, "POS_DRAWER_NO_SALE", "DRAWER_01");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Xác thực Quản lý thất bại: {ex.Message}" });
            }

            return Json(new { success = true, message = "Lệnh mở két tiền không qua bán hàng (No-Sale Drawer Kick) đã được Quản lý phê duyệt thành công!" });
        }

        #endregion

        #region 6. CỔNG ĐƠN NHẬN TẠI QUẦY O2O (CLICK & COLLECT PICKUP AT POS - PHIÊN D1)

        [HttpGet]
        [Route("Admin/Pos/PendingPickupOrders")]
        public async Task<IActionResult> PendingPickupOrders(int? storeId = null, string? keyword = null)
        {
            var effectiveStoreId = await GetEffectiveStoreIdAsync();

            // Store Isolation: Nếu nhân viên gắn với 1 cửa hàng cụ thể thì không được phép truy vấn cửa hàng khác
            if (effectiveStoreId.HasValue && storeId.HasValue && storeId.Value != effectiveStoreId.Value)
            {
                return Json(new
                {
                    success = false,
                    message = "Bạn không có quyền xem đơn Click & Collect của chi nhánh khác (Store Isolation).",
                    orders = Array.Empty<object>(),
                    count = 0
                });
            }

            int? targetStoreId = effectiveStoreId ?? storeId;

            var query = _context.Orders
                .AsNoTracking()
                .Include(o => o.Store)
                .Include(o => o.PickupStore)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductBatch)
                .Where(o => o.DeliveryMethod == "STORE_PICKUP"
                            && o.OrderStatus != "COMPLETED"
                            && o.OrderStatus != "CANCELLED");

            if (targetStoreId.HasValue && targetStoreId.Value > 0)
            {
                query = query.Where(o => (o.PickupStoreId ?? o.StoreId) == targetStoreId.Value);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                if (kw.StartsWith("OMNI-PICKUP|", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = kw.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2) kw = parts[1].Trim();
                }
                else if (kw.StartsWith("OB-", StringComparison.OrdinalIgnoreCase))
                {
                    kw = kw[3..].Trim();
                }

                query = query.Where(o =>
                    o.OrderCode.Contains(kw) ||
                    o.CustomerPhone.Contains(kw) ||
                    o.CustomerName.Contains(kw) ||
                    (o.PickupCode != null && o.PickupCode == kw));
            }

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Take(30)
                .ToListAsync();

            var dtos = orders.Select(o => new PosPickupOrderDto
            {
                OrderId = o.Id,
                OrderCode = o.OrderCode,
                PickupStoreId = o.PickupStoreId ?? o.StoreId,
                PickupStoreName = o.PickupStore?.Name ?? o.Store?.Name ?? $"Showroom #{o.StoreId}",
                CustomerName = o.CustomerName,
                CustomerPhone = o.CustomerPhone,
                OrderStatus = o.OrderStatus,
                PaymentStatus = o.PaymentStatus,
                PaymentMethod = o.PaymentMethod,
                TotalAmount = o.TotalAmount,
                CreatedAt = o.CreatedAt,
                PickupExpiresAt = o.PickupExpiresAt,
                MaskedPickupCode = !string.IsNullOrEmpty(o.PickupCode) && o.PickupCode.Length >= 2
                    ? $"****{o.PickupCode[^2..]}"
                    : "******",
                Items = o.OrderDetails.Select(od => new OrderDetailItemDto
                {
                    ProductName = od.Product?.Name ?? $"Sản phẩm #{od.ProductId}",
                    BatchNumber = od.ProductBatch?.BatchNumber ?? "FEFO",
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                    LineTotal = od.LineTotal
                }).ToList()
            }).ToList();

            return Json(new
            {
                success = true,
                count = dtos.Count,
                orders = dtos
            });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("Admin/Pos/HandoverPickupOrder")]
        [Route("Admin/Pos/CompletePickupOrder")]
        public async Task<IActionResult> HandoverPickupOrder([FromBody] System.Text.Json.JsonElement? body = null)
        {
            int orderId = Request.ResolveInt(body, "orderId", 0);
            string orderCode = Request.ResolveString(body, "orderCode", "code");
            string pickupCode = Request.ResolveString(body, "pickupCode", "pin");
            if (string.IsNullOrWhiteSpace(pickupCode))
            {
                pickupCode = Request.ResolveString(body, "pickupPin");
            }
            int requestedStoreId = Request.ResolveInt(body, "storeId", 0);
            int parsedShiftId = Request.ResolveInt(body, "shiftId", 0);
            int? shiftId = parsedShiftId > 0 ? parsedShiftId : null;

            // Hỗ trợ quét QR dạng OMNI-PICKUP|ORD-...|849201
            if (!string.IsNullOrWhiteSpace(pickupCode) && pickupCode.StartsWith("OMNI-PICKUP|", StringComparison.OrdinalIgnoreCase))
            {
                var parts = pickupCode.Split('|', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    if (string.IsNullOrWhiteSpace(orderCode)) orderCode = parts[1].Trim();
                    pickupCode = parts[2].Trim();
                }
            }

            var effectiveStoreId = await GetEffectiveStoreIdAsync();
            int cashierStoreId = effectiveStoreId ?? requestedStoreId;

            int? cashierUserId = User.GetUserId();
            string cashierName = User.FindFirstValue(ClaimTypes.GivenName) ?? User.Identity?.Name ?? "Thu ngân POS";

            var result = await _orderWorkflow.HandoverPickupOrderAtPosAsync(new HandoverPickupOrderRequestDto
            {
                OrderId = orderId,
                OrderCode = orderCode,
                PickupCode = pickupCode,
                CashierStoreId = cashierStoreId,
                CashierUserId = cashierUserId,
                CashierName = cashierName,
                PosShiftId = shiftId
            }, HttpContext.RequestAborted);

            if (!result.Success || result.HandedOverOrder == null)
            {
                return Json(new
                {
                    success = false,
                    message = result.Message
                });
            }

            return Json(new
            {
                success = true,
                message = result.Message,
                order = result.HandedOverOrder,
                receiptUrl = $"/Admin/Pos/PrintReceipt?orderId={result.HandedOverOrder.OrderId}"
            });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("Admin/Pos/RunExpiredPickupJob")]
        public async Task<IActionResult> RunExpiredPickupJob()
        {
            int released = await _orderWorkflow.ReleaseExpiredPickupOrdersAsync(HttpContext.RequestAborted);
            return Json(new
            {
                success = true,
                releasedCount = released,
                message = released > 0
                    ? $"Đã chạy Job hết hạn 48h: Tự động hủy giữ hàng và hoàn tồn kho FEFO cho {released} đơn Click & Collect quá hạn."
                    : "Đã chạy Job kiểm tra: Hiện không có đơn Click & Collect nào quá hạn 48h."
            });
        }

        #endregion

        private async Task<int?> GetEffectiveStoreIdAsync()
        {
            if (User.IsSuperAdmin())
            {
                return null;
            }

            var sid = User.GetStoreId();
            if (sid.HasValue) return sid;

            var uid = User.GetUserId();
            if (uid.HasValue)
            {
                var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == uid.Value);
                if (user?.StoreId != null) return user.StoreId;
            }
            return null;
        }
    }
}
