using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoTechHub.Data;
using AutoTechHub.Models;

namespace AutoTechHub.Services
{
    public class AdService : IAdService
    {
        private readonly AppDbContext _context;
        private readonly ISiteConfigService _siteConfigService;

        public AdService(AppDbContext context, ISiteConfigService siteConfigService)
        {
            _context = context;
            _siteConfigService = siteConfigService;
        }

        public async Task<List<Advertisement>> GetActiveAdsByPlacementAsync(AdPlacement placement)
        {
            var settings = await _siteConfigService.GetSettingsAsync();
            if (!settings.EnableAdsModule)
            {
                return new List<Advertisement>();
            }

            var now = DateTime.UtcNow;
            return await _context.Advertisements
                .Where(a => a.IsActive &&
                            a.Placement == placement &&
                            (!a.StartDate.HasValue || a.StartDate.Value <= now) &&
                            (!a.EndDate.HasValue || a.EndDate.Value >= now))
                .OrderBy(a => a.DisplayOrder)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Advertisement>> GetAllAdsAsync()
        {
            return await _context.Advertisements
                .OrderBy(a => a.Placement)
                .ThenBy(a => a.DisplayOrder)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<Advertisement> GetAdByIdAsync(int id)
        {
            return await _context.Advertisements.FindAsync(id);
        }

        public async Task<Advertisement> CreateAdAsync(Advertisement ad)
        {
            ad.CreatedAt = DateTime.UtcNow;
            _context.Advertisements.Add(ad);
            await _context.SaveChangesAsync();
            return ad;
        }

        public async Task<Advertisement> UpdateAdAsync(Advertisement model)
        {
            var ad = await _context.Advertisements.FindAsync(model.Id);
            if (ad == null) return null;

            ad.Title = model.Title;
            ad.Description = model.Description;
            ad.Placement = model.Placement;
            ad.AdType = model.AdType;
            ad.ImageUrl = model.ImageUrl;
            ad.TargetUrl = model.TargetUrl;
            ad.HtmlContent = model.HtmlContent;
            ad.CtaText = model.CtaText;
            ad.OpenInNewTab = model.OpenInNewTab;
            ad.IsActive = model.IsActive;
            ad.DisplayOrder = model.DisplayOrder;
            ad.StartDate = model.StartDate;
            ad.EndDate = model.EndDate;

            await _context.SaveChangesAsync();
            return ad;
        }

        public async Task<bool> DeleteAdAsync(int id)
        {
            var ad = await _context.Advertisements.FindAsync(id);
            if (ad == null) return false;

            _context.Advertisements.Remove(ad);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleAdActiveAsync(int id)
        {
            var ad = await _context.Advertisements.FindAsync(id);
            if (ad == null) return false;

            ad.IsActive = !ad.IsActive;
            await _context.SaveChangesAsync();
            return ad.IsActive;
        }

        public async Task<string> RecordClickAndGetTargetUrlAsync(int id)
        {
            var ad = await _context.Advertisements.FindAsync(id);
            if (ad == null) return null;

            ad.ClickCount++;
            await _context.SaveChangesAsync();
            return string.IsNullOrWhiteSpace(ad.TargetUrl) ? "/" : ad.TargetUrl;
        }
    }
}
