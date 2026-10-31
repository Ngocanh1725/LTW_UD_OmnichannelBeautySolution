using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Finance;

namespace Omnichannel.Application.Interfaces.Finance
{
    public interface IFinanceService
    {
        /// <summary>
        /// Ghi nhận giao dịch vào Sổ cái tài chính (Transaction an toàn)
        /// </summary>
        Task<FinancialLedgerDto> RecordLedgerEntryAsync(CreateLedgerEntryDto dto, int? userId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy danh sách bản ghi Sổ cái theo bộ lọc
        /// </summary>
        Task<List<FinancialLedgerDto>> GetLedgerEntriesAsync(int? storeId = null, string? entryType = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tự động ghi nhận Bút toán Doanh thu (Credit) và Giá vốn hàng bán COGS (Debit) theo giá nhập lô FEFO của đơn hàng
        /// </summary>
        Task<(FinancialLedgerDto RevenueEntry, FinancialLedgerDto? CogsEntry)> RecordOrderPaymentAndCogsAsync(int orderId, int? userId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tính toán chính xác Giá vốn hàng bán (COGS) theo giá nhập ImportCost của từng lô ProductBatch xuất kho
        /// </summary>
        Task<decimal> CalculateOrderCogsAsync(int orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Ghi nhận bút toán Chi nhập kho đơn mua hàng (PO) và tạo công nợ Nhà cung cấp
        /// </summary>
        Task<SupplierDebtDto> RecordPurchaseOrderReceiptAsync(string purchaseOrderCode, int supplierId, int storeId, decimal totalAmount, DateTime? dueDate = null, int? userId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tạo hồ sơ công nợ Nhà cung cấp khi nhập kho
        /// </summary>
        Task<SupplierDebtDto> CreateSupplierDebtAsync(CreateSupplierDebtDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Ghi nhận thanh toán công nợ Nhà cung cấp (giảm nợ + tự động ghi Sổ cái)
        /// </summary>
        Task<bool> RecordSupplierPaymentAsync(RecordSupplierPaymentDto dto, int? userId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy danh sách công nợ Nhà cung cấp
        /// </summary>
        Task<List<SupplierDebtDto>> GetSupplierDebtsAsync(int? supplierId = null, string? status = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy danh sách công nợ quá hạn thanh toán kèm cảnh báo nợ xấu
        /// </summary>
        Task<List<SupplierDebtDto>> GetOverdueSupplierDebtsAsync(DateTime? asOfDate = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tạo phiếu đối soát tiền thu hộ COD từ đơn vị vận chuyển
        /// </summary>
        Task<CodReconciliationDto> CreateCodReconciliationAsync(CreateCodReconciliationDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Đối soát danh sách đơn hàng COD: so sánh tiền thực nhận từ ĐVVC với tổng tiền đơn hàng
        /// </summary>
        Task<CodReconciliationDto> ReconcileCodOrdersAsync(string shippingPartner, List<string> orderCodes, decimal actualReceivedAmount, string? note = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Xác nhận trạng thái đối soát COD (MATCHED / DISPUTED) và ghi sổ cái nếu có
        /// </summary>
        Task<bool> ConfirmCodReconciliationAsync(int reconciliationId, string status, string? note = null, int? userId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy danh sách các đợt đối soát COD
        /// </summary>
        Task<List<CodReconciliationDto>> GetCodReconciliationsAsync(string? partner = null, string? status = null, CancellationToken cancellationToken = default);
    }
}
