using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Inventory;

namespace Omnichannel.Application.Interfaces.Inventory
{
    public interface IInventoryAllocationEngine
    {
        /// <summary>
        /// Thuật toán FEFO: Tự động phân bổ số lượng cần lấy theo danh sách Lô có ExpDate gần nhất
        /// </summary>
        Task<List<AllocatedBatchDto>> AllocateBatchesFEFOAsync(string storeId, string sku, int quantity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Kiểm tra nhanh tồn kho khả dụng (Physical - Reserved) của sản phẩm tại một kho
        /// </summary>
        Task<bool> CheckAvailabilityAsync(string storeId, string sku, int quantity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Nhập kho theo Lô NSX/HSD và ghi nhận sổ cái bất biến InventoryLogs
        /// </summary>
        Task<bool> ImportBatchAsync(ImportBatchViewModel model, string staffUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cân bằng kiểm kê hoặc xuất hủy hàng hỏng/hết date
        /// </summary>
        Task<bool> AdjustStockAsync(StockAdjustmentViewModel model, string staffUserId, CancellationToken cancellationToken = default);
    }
}