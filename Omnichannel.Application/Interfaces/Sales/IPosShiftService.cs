using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Sales;

namespace Omnichannel.Application.Interfaces.Sales
{
    public interface IPosShiftService
    {
        /// <summary>
        /// Mở ca làm việc POS mới cho thu ngân tại chi nhánh
        /// </summary>
        Task<PosShiftDetailDto> OpenShiftAsync(OpenShiftDto dto, CancellationToken ct = default);

        /// <summary>
        /// Chốt ca làm việc POS, đối soát doanh thu tiền mặt, thẻ và tính chênh lệch két
        /// </summary>
        Task<PosShiftDetailDto> CloseShiftAsync(CloseShiftDto dto, CancellationToken ct = default);

        /// <summary>
        /// Lấy thông tin ca làm việc đang mở tại chi nhánh (nếu có)
        /// </summary>
        Task<PosShiftDetailDto?> GetCurrentOpenShiftAsync(int storeId, int cashierId, CancellationToken ct = default);

        /// <summary>
        /// Lấy lịch sử các ca làm việc của chi nhánh theo khoảng thời gian
        /// </summary>
        Task<List<PosShiftSummaryDto>> GetShiftHistoryAsync(int storeId, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);

        /// <summary>
        /// Thêm giao dịch thu/chi vãng lai từ két tiền mặt trong ca
        /// </summary>
        Task AddDrawerTransactionAsync(int shiftId, DrawerTransactionDto dto, CancellationToken ct = default);
    }
}
