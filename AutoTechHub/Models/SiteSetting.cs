using System;
using System.ComponentModel.DataAnnotations;

namespace AutoTechHub.Models
{
    public class SiteSetting
    {
        public int Id { get; set; } = 1;

        [Display(Name = "Bật phân mục Quảng cáo")]
        public bool EnableAdsModule { get; set; } = true;

        [Display(Name = "Bật phân mục Giới thiệu & Bán hàng")]
        public bool EnableShopModule { get; set; } = true;

        [StringLength(50)]
        [Display(Name = "Tên hiển thị trên Menu bán hàng")]
        public string ShopNavTitle { get; set; } = "Sản phẩm";

        [StringLength(150)]
        [Display(Name = "Tiêu đề khối Sản phẩm")]
        public string ShopBannerTitle { get; set; } = "Thiết Bị, Kit Thực Hành & Giải Pháp Tự Động Hóa";

        [StringLength(400)]
        [Display(Name = "Mô tả khối Sản phẩm")]
        public string ShopBannerSubtitle { get; set; } = "Các sản phẩm phần cứng PLC/IoT, cảm biến và bộ mã nguồn mẫu được chọn lọc, hỗ trợ kỹ thuật trực tiếp.";

        [StringLength(30)]
        [Display(Name = "Số Hotline / Đặt hàng")]
        public string ContactPhone { get; set; } = "0900 000 000";

        [StringLength(200)]
        [Display(Name = "Link Zalo tư vấn")]
        public string ContactZalo { get; set; } = "https://zalo.me";

        [StringLength(150)]
        [Display(Name = "Email liên hệ bán hàng")]
        public string ContactEmail { get; set; } = "sales@autotechhub.vn";

        [Required(ErrorMessage = "Tên thương hiệu là bắt buộc")]
        [StringLength(60)]
        [Display(Name = "Tên website / Thương hiệu")]
        public string SiteName { get; set; } = "AutoTechHub";

        [StringLength(150)]
        [Display(Name = "Khẩu hiệu (Tagline)")]
        public string SiteTagline { get; set; } = "Chia Sẻ Kiến Thức Tự Động Hóa";

        [StringLength(500)]
        [Display(Name = "Đường dẫn Logo")]
        public string LogoUrl { get; set; } = "/images/logo-default.svg";

        [StringLength(500)]
        [Display(Name = "Đường dẫn Favicon")]
        public string FaviconUrl { get; set; } = "/images/favicon.svg";

        [Display(Name = "Hiển thị chữ thương hiệu bên cạnh Logo")]
        public bool ShowBrandTextWithLogo { get; set; } = true;

        // ==================== WEB DESIGNER: THEME & LAYOUT ====================

        [StringLength(20)]
        [Display(Name = "Màu chủ đạo (Primary)")]
        public string PrimaryColor { get; set; } = "#0d6efd";

        [StringLength(20)]
        [Display(Name = "Màu nhấn (Accent)")]
        public string AccentColor { get; set; } = "#0dcaf0";

        [StringLength(80)]
        [Display(Name = "Phông chữ chính")]
        public string FontFamily { get; set; } = "Segoe UI";

        [Range(0, 32)]
        [Display(Name = "Độ bo góc thẻ (px)")]
        public int CardRadiusPx { get; set; } = 16;

        [StringLength(20)]
        [Display(Name = "Chế độ Sáng / Tối mặc định")]
        public string DefaultTheme { get; set; } = "auto";

        [Display(Name = "CSS tùy chỉnh nâng cao")]
        public string CustomCss { get; set; }

        [Display(Name = "Cấu hình thứ tự & ẩn/hiện khối trang chủ (JSON)")]
        public string HomeLayoutJson { get; set; }

        // ==================== WEB DESIGNER: HERO & FOOTER CONTENT ====================

        [StringLength(120)]
        [Display(Name = "Nhãn Hero Badge")]
        public string HeroBadgeText { get; set; } = "Kho Tri Thức & Giải Pháp Tự Động Hóa 4.0";

        [StringLength(150)]
        [Display(Name = "Tiêu đề Hero (Dòng 1)")]
        public string HeroTitlePrefix { get; set; } = "Chia Sẻ Kiến Thức";

        [StringLength(150)]
        [Display(Name = "Tiêu đề Hero (Nổi bật)")]
        public string HeroTitleHighlight { get; set; } = "Tự Động Hóa Thực Chiến";

        [StringLength(500)]
        [Display(Name = "Mô tả Hero")]
        public string HeroSubtitle { get; set; } = "Khám phá các hướng dẫn chuyên sâu về PLC/SCADA, RPA Web & Desktop, Hệ thống IoT, và Thị giác máy tính AI từ các kỹ sư tự động hóa.";

        [Display(Name = "Hiện thanh tìm kiếm trên Hero")]
        public bool HeroShowSearch { get; set; } = true;

        [Display(Name = "Hiện từ khóa hot trên Hero")]
        public bool HeroShowTags { get; set; } = true;

        [Display(Name = "Hiện ô thống kê trên Hero")]
        public bool HeroShowStats { get; set; } = true;

        [StringLength(80)]
        [Display(Name = "Chữ nút CTA Hero (tùy chọn)")]
        public string HeroCtaText { get; set; }

        [StringLength(300)]
        [Display(Name = "Link nút CTA Hero")]
        public string HeroCtaUrl { get; set; }

        [StringLength(100)]
        [Display(Name = "Tiêu đề mục Bài viết nổi bật")]
        public string FeaturedSectionTitle { get; set; } = "Bài Viết Nổi Bật";

        [StringLength(100)]
        [Display(Name = "Tiêu đề mục Chuyên mục")]
        public string CategoriesSectionTitle { get; set; } = "Chuyên Mục Tự Động Hóa";

        [StringLength(100)]
        [Display(Name = "Tiêu đề mục Bài viết mới")]
        public string LatestSectionTitle { get; set; } = "Bài Viết Mới Cập Nhật";

        [StringLength(500)]
        [Display(Name = "Mô tả chân trang (Footer)")]
        public string FooterDescription { get; set; } = "Nền tảng chia sẻ kiến thức, kinh nghiệm thực chiến và thư viện mã nguồn về Tự động hóa công nghiệp (PLC/SCADA/IoT), RPA, DevOps và AI Vision.";

        [StringLength(300)]
        [Display(Name = "Link GitHub")]
        public string GithubUrl { get; set; } = "https://github.com";

        [StringLength(300)]
        [Display(Name = "Link YouTube")]
        public string YoutubeUrl { get; set; } = "https://youtube.com";

        [StringLength(300)]
        [Display(Name = "Link LinkedIn")]
        public string LinkedinUrl { get; set; } = "https://linkedin.com";

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
