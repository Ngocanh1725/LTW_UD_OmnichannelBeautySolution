using System.Collections.Generic;
using System.Threading.Tasks;
using Omnichannel.Application.DTOs.Media;

namespace Omnichannel.Application.Interfaces.Media
{
    public interface IMediaService
    {
        Task<MediaAssetDto> UploadAsync(MediaUploadRequest request);
        Task<List<MediaAssetDto>> UploadBatchAsync(IEnumerable<Microsoft.AspNetCore.Http.IFormFile> files, string category, int? userId);
        Task<MediaStreamResultDto?> GetMediaStreamAsync(int id);
        Task<MediaStreamResultDto?> GetMediaStreamByHashAsync(string hash);
        Task<(List<MediaAssetDto> Items, int TotalCount)> GetPagedListAsync(MediaFilterRequest filter);
        Task<bool> DeleteAsync(int id);
        Task<bool> UpdateAltTextAsync(int id, string altText);
    }
}
