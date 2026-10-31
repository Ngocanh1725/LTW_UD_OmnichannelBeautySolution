using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Settings;

namespace Omnichannel.Application.Interfaces.Settings
{
    public interface ISettingsService
    {
        /// <summary>
        /// Lấy giá trị cấu hình theo Key và tự động ép kiểu về kiểu dữ liệu T
        /// </summary>
        Task<T?> GetAsync<T>(string key, T? defaultValue = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy giá trị chuỗi cấu hình theo Key (tự động giải mã nếu IsEncrypted = true)
        /// </summary>
        Task<string?> GetValueAsync(string key, string? defaultValue = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lưu hoặc cập nhật một cấu hình
        /// </summary>
        Task<bool> SetAsync(string key, string value, string? group = null, string? dataType = null, string? description = null, bool isEncrypted = false, int? userId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy toàn bộ cấu hình theo nhóm dưới dạng Dictionary Key-Value
        /// </summary>
        Task<Dictionary<string, string>> GetGroupAsync(string group, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy danh sách SettingDto theo nhóm (GENERAL, SMTP, PAYMENT, SHIPPING)
        /// </summary>
        Task<List<SettingDto>> GetGroupSettingsAsync(string group, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy toàn bộ danh sách cấu hình hệ thống
        /// </summary>
        Task<List<SettingDto>> GetAllSettingsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Cập nhật hàng loạt cấu hình theo danh sách Key-Value
        /// </summary>
        Task<bool> UpdateBatchAsync(Dictionary<string, string> settings, int? userId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Xóa cache cấu hình (xóa toàn bộ hoặc theo Key cụ thể)
        /// </summary>
        Task InvalidateCacheAsync(string? key = null, CancellationToken cancellationToken = default);
    }
}
