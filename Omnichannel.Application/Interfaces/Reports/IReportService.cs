using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Finance;
using Omnichannel.Application.DTOs.Reports;

namespace Omnichannel.Application.Interfaces.Reports
{
    /// <summary>
    /// Hợp đồng dịch vụ tính toán báo cáo doanh thu đa kênh, kiểm soát hạn sử dụng FEFO và P&L tài chính
    /// </summary>
    public interface IReportService
    {
        /// <summary>
        /// Lấy toàn bộ số liệu tổng hợp cho trang Dashboard điều hành và Báo cáo Doanh thu đa kênh
        /// </summary>
        Task<RevenueDashboardViewModel> GetRevenueDashboardAsync(
            string? storeId = null,
            string period = "7days",
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy danh sách Top sản phẩm bán chạy nhất
        /// </summary>
        Task<List<TopProductDto>> GetTopSellingProductsAsync(int top = 5, string? storeId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy báo cáo các lô hàng cận hạn sử dụng theo thuật toán FEFO
        /// </summary>
        Task<List<ExpiringBatchDto>> GetFefoExpiryReportAsync(int daysThreshold = 60, string? storeId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy thống kê doanh thu theo khoảng thời gian tùy chọn
        /// </summary>
        Task<List<DailyRevenueDto>> GetRevenueByDateRangeAsync(DateTime fromDate, DateTime toDate, string? storeId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Báo cáo lãi lỗ P&L (Doanh thu - Giá vốn COGS - Chi phí vận hành)
        /// </summary>
        Task<ProfitLossReportDto> GetProfitLossAsync(int? storeId, DateTime from, DateTime to, CancellationToken cancellationToken = default);

        /// <summary>
        /// Báo cáo vòng quay tồn kho (Inventory Turnover Ratio)
        /// </summary>
        Task<InventoryTurnoverDto> GetInventoryTurnoverAsync(int? storeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Hiệu quả khai thác mặt bằng: Doanh thu trên mỗi mét vuông (Sales per sqft/sqm)
        /// </summary>
        Task<SalesPerSqftDto> GetSalesPerSqftAsync(int storeId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    }
}