using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AutoTechHub.Models;

namespace AutoTechHub.Models.ViewModels
{
    public class HomeSectionItem
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        public bool Visible { get; set; } = true;
    }

    public class WebDesignerSaveModel
    {
        [StringLength(20)]
        public string PrimaryColor { get; set; } = "#0d6efd";

        [StringLength(20)]
        public string AccentColor { get; set; } = "#0dcaf0";

        [StringLength(80)]
        public string FontFamily { get; set; } = "Segoe UI";

        [Range(0, 32)]
        public int CardRadiusPx { get; set; } = 16;

        [StringLength(20)]
        public string DefaultTheme { get; set; } = "auto";

        public string CustomCss { get; set; }
        public string HomeLayoutJson { get; set; }

        [StringLength(120)]
        public string HeroBadgeText { get; set; }

        [StringLength(150)]
        public string HeroTitlePrefix { get; set; }

        [StringLength(150)]
        public string HeroTitleHighlight { get; set; }

        [StringLength(500)]
        public string HeroSubtitle { get; set; }

        public bool HeroShowSearch { get; set; } = true;
        public bool HeroShowTags { get; set; } = true;
        public bool HeroShowStats { get; set; } = true;

        [StringLength(80)]
        public string HeroCtaText { get; set; }

        [StringLength(300)]
        public string HeroCtaUrl { get; set; }

        [StringLength(100)]
        public string FeaturedSectionTitle { get; set; }

        [StringLength(100)]
        public string CategoriesSectionTitle { get; set; }

        [StringLength(100)]
        public string LatestSectionTitle { get; set; }

        [StringLength(500)]
        public string FooterDescription { get; set; }

        [StringLength(300)]
        public string GithubUrl { get; set; }

        [StringLength(300)]
        public string YoutubeUrl { get; set; }

        [StringLength(300)]
        public string LinkedinUrl { get; set; }
    }

    public class WebDesignerViewModel
    {
        public WebDesignerSaveModel Design { get; set; } = new WebDesignerSaveModel();
        public List<HomeSectionItem> HomeSections { get; set; } = new List<HomeSectionItem>();
        public List<WebBlock> WebBlocks { get; set; } = new List<WebBlock>();
        public WebBlock NewBlock { get; set; } = new WebBlock();
    }
}
