using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoTechHub.Data;
using AutoTechHub.Models;
using AutoTechHub.Models.ViewModels;

namespace AutoTechHub.Services
{
    public class SiteConfigService : ISiteConfigService
    {
        private readonly AppDbContext _context;
        private readonly IMarkdownService _markdownService;

        public SiteConfigService(AppDbContext context, IMarkdownService markdownService)
        {
            _context = context;
            _markdownService = markdownService;
        }

        public async Task<SiteSetting> GetSettingsAsync()
        {
            var setting = await _context.SiteSettings.OrderBy(s => s.Id).FirstOrDefaultAsync();
            if (setting == null)
            {
                setting = new SiteSetting
                {
                    EnableAdsModule = true,
                    EnableShopModule = true,
                    SiteName = "AutoTechHub",
                    SiteTagline = "Chia Sẻ Kiến Thức Tự Động Hóa",
                    LogoUrl = "/images/logo-default.svg",
                    FaviconUrl = "/images/favicon.svg",
                    ShowBrandTextWithLogo = true,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.SiteSettings.Add(setting);
                await _context.SaveChangesAsync();
            }

            return setting;
        }

        public async Task<SiteSetting> UpdateSettingsAsync(SiteSetting model)
        {
            var setting = await GetSettingsAsync();

            setting.EnableAdsModule = model.EnableAdsModule;
            setting.EnableShopModule = model.EnableShopModule;
            setting.ShopNavTitle = string.IsNullOrWhiteSpace(model.ShopNavTitle) ? "Sản phẩm" : model.ShopNavTitle.Trim();
            setting.ShopBannerTitle = model.ShopBannerTitle?.Trim();
            setting.ShopBannerSubtitle = model.ShopBannerSubtitle?.Trim();
            setting.ContactPhone = model.ContactPhone?.Trim();
            setting.ContactZalo = model.ContactZalo?.Trim();
            setting.ContactEmail = model.ContactEmail?.Trim();
            setting.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return setting;
        }

        public async Task<SiteSetting> UpdateBrandingAsync(BrandingSettingsViewModel model)
        {
            var setting = await GetSettingsAsync();

            setting.SiteName = string.IsNullOrWhiteSpace(model.SiteName) ? "AutoTechHub" : model.SiteName.Trim();
            setting.SiteTagline = model.SiteTagline?.Trim();
            setting.LogoUrl = string.IsNullOrWhiteSpace(model.LogoUrl) ? "/images/logo-default.svg" : model.LogoUrl.Trim();
            setting.FaviconUrl = string.IsNullOrWhiteSpace(model.FaviconUrl) ? "/images/favicon.svg" : model.FaviconUrl.Trim();
            setting.ShowBrandTextWithLogo = model.ShowBrandTextWithLogo;
            setting.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return setting;
        }

        public async Task<bool> ToggleAdsModuleAsync()
        {
            var setting = await GetSettingsAsync();
            setting.EnableAdsModule = !setting.EnableAdsModule;
            setting.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return setting.EnableAdsModule;
        }

        public async Task<bool> ToggleShopModuleAsync()
        {
            var setting = await GetSettingsAsync();
            setting.EnableShopModule = !setting.EnableShopModule;
            setting.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return setting.EnableShopModule;
        }

        public List<HomeSectionItem> GetDefaultHomeSections()
        {
            return new List<HomeSectionItem>
            {
                new HomeSectionItem { Key = "Hero", Label = "Hero Banner & Tìm kiếm", Description = "Phần mở đầu đầu trang, ô tìm kiếm nhanh, từ khóa và thống kê.", Visible = true },
                new HomeSectionItem { Key = "FeaturedArticles", Label = "Bài Viết Nổi Bật", Description = "Các bài viết được đánh dấu nổi bật trên trang chủ.", Visible = true },
                new HomeSectionItem { Key = "Categories", Label = "Chuyên Mục Tự Động Hóa", Description = "Lưới chuyên mục kỹ thuật (PLC, RPA, IoT, DevOps, AI).", Visible = true },
                new HomeSectionItem { Key = "LatestAndSidebar", Label = "Bài Viết Mới & Sidebar", Description = "Luồng bài viết mới nhất cùng Kho Snippets, Thẻ & Quảng cáo Sidebar.", Visible = true },
                new HomeSectionItem { Key = "Products", Label = "Sản Phẩm & Giải Pháp", Description = "Khối giới thiệu bán hàng (hiển thị khi Module Bán hàng đang Bật).", Visible = true },
                new HomeSectionItem { Key = "CustomBlocks", Label = "Khối Giao Diện Tự Do", Description = "Các khối CTA / Banner / HTML tự tạo trong Web Designer.", Visible = true }
            };
        }

        public List<HomeSectionItem> GetHomeSections(SiteSetting setting)
        {
            var defaults = GetDefaultHomeSections();
            if (setting == null || string.IsNullOrWhiteSpace(setting.HomeLayoutJson))
            {
                return defaults;
            }

            try
            {
                var saved = JsonSerializer.Deserialize<List<HomeSectionItem>>(setting.HomeLayoutJson);
                if (saved == null || !saved.Any()) return defaults;

                var defaultMap = defaults.ToDictionary(d => d.Key, d => d, StringComparer.OrdinalIgnoreCase);
                var result = new List<HomeSectionItem>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var item in saved)
                {
                    if (!string.IsNullOrWhiteSpace(item.Key) && defaultMap.TryGetValue(item.Key, out var def) && seen.Add(item.Key))
                    {
                        result.Add(new HomeSectionItem
                        {
                            Key = def.Key,
                            Label = def.Label,
                            Description = def.Description,
                            Visible = item.Visible
                        });
                    }
                }

                foreach (var def in defaults)
                {
                    if (seen.Add(def.Key))
                    {
                        result.Add(def);
                    }
                }

                return result;
            }
            catch
            {
                return defaults;
            }
        }

        public async Task<SiteSetting> SaveDesignerSettingsAsync(WebDesignerSaveModel model)
        {
            var setting = await GetSettingsAsync();

            setting.PrimaryColor = string.IsNullOrWhiteSpace(model.PrimaryColor) ? "#0d6efd" : model.PrimaryColor.Trim();
            setting.AccentColor = string.IsNullOrWhiteSpace(model.AccentColor) ? "#0dcaf0" : model.AccentColor.Trim();
            setting.FontFamily = string.IsNullOrWhiteSpace(model.FontFamily) ? "Segoe UI" : model.FontFamily.Trim();
            setting.CardRadiusPx = Math.Clamp(model.CardRadiusPx, 0, 32);
            setting.DefaultTheme = (model.DefaultTheme == "light" || model.DefaultTheme == "dark") ? model.DefaultTheme : "auto";
            setting.CustomCss = model.CustomCss;

            if (!string.IsNullOrWhiteSpace(model.HomeLayoutJson))
            {
                setting.HomeLayoutJson = model.HomeLayoutJson;
            }

            setting.HeroBadgeText = model.HeroBadgeText?.Trim();
            setting.HeroTitlePrefix = model.HeroTitlePrefix?.Trim();
            setting.HeroTitleHighlight = model.HeroTitleHighlight?.Trim();
            setting.HeroSubtitle = model.HeroSubtitle?.Trim();
            setting.HeroShowSearch = model.HeroShowSearch;
            setting.HeroShowTags = model.HeroShowTags;
            setting.HeroShowStats = model.HeroShowStats;
            setting.HeroCtaText = model.HeroCtaText?.Trim();
            setting.HeroCtaUrl = model.HeroCtaUrl?.Trim();

            setting.FeaturedSectionTitle = string.IsNullOrWhiteSpace(model.FeaturedSectionTitle) ? "Bài Viết Nổi Bật" : model.FeaturedSectionTitle.Trim();
            setting.CategoriesSectionTitle = string.IsNullOrWhiteSpace(model.CategoriesSectionTitle) ? "Chuyên Mục Tự Động Hóa" : model.CategoriesSectionTitle.Trim();
            setting.LatestSectionTitle = string.IsNullOrWhiteSpace(model.LatestSectionTitle) ? "Bài Viết Mới Cập Nhật" : model.LatestSectionTitle.Trim();

            setting.FooterDescription = model.FooterDescription?.Trim();
            setting.GithubUrl = model.GithubUrl?.Trim();
            setting.YoutubeUrl = model.YoutubeUrl?.Trim();
            setting.LinkedinUrl = model.LinkedinUrl?.Trim();
            setting.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return setting;
        }

        public async Task<SiteSetting> ResetDesignerDefaultsAsync()
        {
            var setting = await GetSettingsAsync();
            var fresh = new SiteSetting();

            setting.PrimaryColor = fresh.PrimaryColor;
            setting.AccentColor = fresh.AccentColor;
            setting.FontFamily = fresh.FontFamily;
            setting.CardRadiusPx = fresh.CardRadiusPx;
            setting.DefaultTheme = fresh.DefaultTheme;
            setting.CustomCss = null;
            setting.HomeLayoutJson = null;

            setting.HeroBadgeText = fresh.HeroBadgeText;
            setting.HeroTitlePrefix = fresh.HeroTitlePrefix;
            setting.HeroTitleHighlight = fresh.HeroTitleHighlight;
            setting.HeroSubtitle = fresh.HeroSubtitle;
            setting.HeroShowSearch = true;
            setting.HeroShowTags = true;
            setting.HeroShowStats = true;
            setting.HeroCtaText = null;
            setting.HeroCtaUrl = null;

            setting.FeaturedSectionTitle = fresh.FeaturedSectionTitle;
            setting.CategoriesSectionTitle = fresh.CategoriesSectionTitle;
            setting.LatestSectionTitle = fresh.LatestSectionTitle;
            setting.FooterDescription = fresh.FooterDescription;
            setting.GithubUrl = fresh.GithubUrl;
            setting.YoutubeUrl = fresh.YoutubeUrl;
            setting.LinkedinUrl = fresh.LinkedinUrl;
            setting.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return setting;
        }

        public async Task<List<WebBlock>> GetAllWebBlocksAsync()
        {
            return await _context.WebBlocks
                .OrderBy(b => b.DisplayOrder)
                .ThenByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<WebBlock>> GetActiveWebBlocksAsync()
        {
            return await _context.WebBlocks
                .Where(b => b.IsActive)
                .OrderBy(b => b.DisplayOrder)
                .ThenByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<WebBlock> GetWebBlockByIdAsync(int id)
        {
            return await _context.WebBlocks.FindAsync(id);
        }

        public async Task<WebBlock> CreateWebBlockAsync(WebBlock model)
        {
            model.ContentHtml = _markdownService.ToHtml(model.ContentMarkdown);
            model.CreatedAt = DateTime.UtcNow;
            _context.WebBlocks.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<WebBlock> UpdateWebBlockAsync(WebBlock model)
        {
            var block = await _context.WebBlocks.FindAsync(model.Id);
            if (block == null) return null;

            block.Title = model.Title;
            block.Subtitle = model.Subtitle;
            block.ContentMarkdown = model.ContentMarkdown;
            block.ContentHtml = _markdownService.ToHtml(model.ContentMarkdown);
            block.CustomHtmlCode = model.CustomHtmlCode;
            block.ButtonText = model.ButtonText;
            block.ButtonUrl = model.ButtonUrl;
            block.BlockType = model.BlockType;
            block.StyleVariant = model.StyleVariant;
            block.DisplayOrder = model.DisplayOrder;
            block.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            return block;
        }

        public async Task<bool> DeleteWebBlockAsync(int id)
        {
            var block = await _context.WebBlocks.FindAsync(id);
            if (block == null) return false;

            _context.WebBlocks.Remove(block);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleWebBlockActiveAsync(int id)
        {
            var block = await _context.WebBlocks.FindAsync(id);
            if (block == null) return false;

            block.IsActive = !block.IsActive;
            await _context.SaveChangesAsync();
            return block.IsActive;
        }

        public async Task<MediaAsset> SaveMediaAssetAsync(string fileName, string contentType, byte[] data)
        {
            var asset = new MediaAsset
            {
                FileName = fileName,
                ContentType = contentType,
                Data = data,
                CreatedAt = DateTime.UtcNow
            };
            _context.MediaAssets.Add(asset);
            await _context.SaveChangesAsync();
            return asset;
        }

        public async Task<MediaAsset> GetMediaAssetAsync(int id)
        {
            return await _context.MediaAssets.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
        }
    }
}
