using System.Collections.Generic;
using System.Threading.Tasks;
using AutoTechHub.Models;
using AutoTechHub.Models.ViewModels;

namespace AutoTechHub.Services
{
    public interface ISiteConfigService
    {
        Task<SiteSetting> GetSettingsAsync();
        Task<SiteSetting> UpdateSettingsAsync(SiteSetting model);
        Task<SiteSetting> UpdateBrandingAsync(BrandingSettingsViewModel model);
        Task<bool> ToggleAdsModuleAsync();
        Task<bool> ToggleShopModuleAsync();

        // Web Designer & Layout
        List<HomeSectionItem> GetDefaultHomeSections();
        List<HomeSectionItem> GetHomeSections(SiteSetting setting);
        Task<SiteSetting> SaveDesignerSettingsAsync(WebDesignerSaveModel model);
        Task<SiteSetting> ResetDesignerDefaultsAsync();

        // Custom Web Blocks
        Task<List<WebBlock>> GetAllWebBlocksAsync();
        Task<List<WebBlock>> GetActiveWebBlocksAsync();
        Task<WebBlock> GetWebBlockByIdAsync(int id);
        Task<WebBlock> CreateWebBlockAsync(WebBlock model);
        Task<WebBlock> UpdateWebBlockAsync(WebBlock model);
        Task<bool> DeleteWebBlockAsync(int id);
        Task<bool> ToggleWebBlockActiveAsync(int id);

        // Database-backed Media Storage
        Task<MediaAsset> SaveMediaAssetAsync(string fileName, string contentType, byte[] data);
        Task<MediaAsset> GetMediaAssetAsync(int id);
    }
}
