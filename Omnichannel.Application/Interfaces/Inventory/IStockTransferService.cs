using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Inventory;

namespace Omnichannel.Application.Interfaces.Inventory
{
    public interface IStockTransferService
    {
        /// <summary>
        /// Tạo phiếu yêu cầu điều chuyển kho liên chi nhánh (Trạng thái PENDING)
        /// </summary>
        Task<StockTransferDto> CreateTransferRequestAsync(
            CreateStockTransferDto dto, 
            int createdByUserId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Phê duyệt và xuất kho theo thuật toán FEFO (Lô cận date xuất trước).
        /// Trừ tồn vật lý tại kho nguồn, ghi InventoryLog (TRANSFER_OUT), chuyển trạng thái sang IN_TRANSIT.
        /// </summary>
        Task<StockTransferDto> ApproveAndExportAsync(
            int transferId, 
            int approvedByUserId, 
            string? note = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Xác nhận thực nhận tại kho đích sau khi scan barcode/kiểm đếm.
        /// Tăng tồn vật lý tại kho đích, ghi InventoryLog (TRANSFER_IN), ghi nhận chênh lệch/thiếu hụt nếu có, chuyển trạng thái sang RECEIVED.
        /// </summary>
        Task<StockTransferDto> ReceiveAndImportAsync(
            ReceiveTransferDto dto, 
            int receivedByUserId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Hủy phiếu điều chuyển khi còn ở trạng thái PENDING
        /// </summary>
        Task<bool> CancelTransferAsync(
            int transferId, 
            int userId, 
            string reason, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy chi tiết phiếu điều chuyển theo ID
        /// </summary>
        Task<StockTransferDto?> GetTransferByIdAsync(
            int transferId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy chi tiết phiếu điều chuyển theo Mã phiếu
        /// </summary>
        Task<StockTransferDto?> GetTransferByCodeAsync(
            string transferCode, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy danh sách phiếu điều chuyển có bộ lọc
        /// </summary>
        Task<List<StockTransferDto>> GetTransfersAsync(
            StockTransferFilterDto? filter = null, 
            CancellationToken cancellationToken = default);
    }
}
