using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Security;

namespace Omnichannel.Application.Interfaces.Security
{
    public interface IAuditLogService
    {
        /// <summary>
        /// Ghi nhật ký kiểm toán hành vi người dùng và thay đổi dữ liệu nhạy cảm
        /// </summary>
        Task LogAsync(
            string action,
            string entityType,
            string entityId,
            string? oldValues = null,
            string? newValues = null,
            int? userId = null,
            string? userName = null,
            string? ipAddress = null,
            string? userAgent = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Truy vấn danh sách nhật ký kiểm toán có phân trang và bộ lọc
        /// </summary>
        Task<List<AuditLogDto>> GetLogsAsync(
            string? entityType = null,
            int? userId = null,
            DateTime? from = null,
            DateTime? to = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Đếm tổng số bản ghi nhật ký kiểm toán theo điều kiện lọc
        /// </summary>
        Task<int> CountLogsAsync(
            string? entityType = null,
            int? userId = null,
            DateTime? from = null,
            DateTime? to = null,
            CancellationToken cancellationToken = default);
    }
}
