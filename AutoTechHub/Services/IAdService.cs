using System.Collections.Generic;
using System.Threading.Tasks;
using AutoTechHub.Models;

namespace AutoTechHub.Services
{
    public interface IAdService
    {
        Task<List<Advertisement>> GetActiveAdsByPlacementAsync(AdPlacement placement);
        Task<List<Advertisement>> GetAllAdsAsync();
        Task<Advertisement> GetAdByIdAsync(int id);
        Task<Advertisement> CreateAdAsync(Advertisement ad);
        Task<Advertisement> UpdateAdAsync(Advertisement ad);
        Task<bool> DeleteAdAsync(int id);
        Task<bool> ToggleAdActiveAsync(int id);
        Task<string> RecordClickAndGetTargetUrlAsync(int id);
    }
}
