using LifeHub.DTOs;

namespace LifeHub.Services.AllowedWebsites
{
    public interface IAllowedWebsiteService
    {
        Task<ServiceResult<List<AllowedWebsiteDto>>> GetAllowedWebsitesAsync();
        Task<ServiceResult<List<string>>> GetActiveDomainsAsync();
        Task<ServiceResult<AllowedWebsiteDto>> CreateAllowedWebsiteAsync(CreateAllowedWebsiteDto dto);
        Task<ServiceResult<AllowedWebsiteDto>> UpdateAllowedWebsiteAsync(int id, UpdateAllowedWebsiteDto dto);
        Task<ServiceResult<bool>> DeleteAllowedWebsiteAsync(int id);
    }
}
