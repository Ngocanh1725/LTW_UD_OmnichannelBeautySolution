using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Reports;

namespace Omnichannel.Application.Interfaces.Reports
{
    /// <summary>
    /// Hợp đồng dịch vụ tính toán báo cáo doanh thu đa kênh và kiểm soát hạn sử dụng FEFO
    /// </summary>
    public interface IReportService
    {
        /// <summary>
        /// Lấy toàn bộ số liệu tổng hợp cho trang Dashboard điều hành
        /// </summary>
        Task<RevenueDashboardViewModel> GetRevenueDashboardAsync(string? storeId = null, CancellationToken cancellationToken = default);

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
    }
}