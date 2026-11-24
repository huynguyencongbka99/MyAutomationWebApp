using System;
using System.ComponentModel.DataAnnotations;

namespace AutoTechHub.Models
{
    public enum AdPlacement
    {
        [Display(Name = "Banner đầu trang (Header)")]
        HeaderBanner = 0,

        [Display(Name = "Thanh bên (Sidebar)")]
        Sidebar = 1,

        [Display(Name = "Trong bài viết (Cuối bài)")]
        InArticle = 2,

        [Display(Name = "Banner chân trang (Footer)")]
        FooterBanner = 3
    }

    public enum AdContentType
    {
        [Display(Name = "Hình ảnh & Liên kết (Banner)")]
        ImageBanner = 0,

        [Display(Name = "Mã HTML / Script tùy chỉnh")]
        HtmlCode = 1
    }

    public class Advertisement
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tiêu đề quảng cáo là bắt buộc")]
        [StringLength(150)]
        [Display(Name = "Tiêu đề / Tên chiến dịch")]
        public string Title { get; set; }

        [StringLength(400)]
        [Display(Name = "Mô tả ngắn")]
        public string Description { get; set; }

        [Display(Name = "Vị trí hiển thị")]
        public AdPlacement Placement { get; set; } = AdPlacement.Sidebar;

        [Display(Name = "Loại nội dung")]
        public AdContentType AdType { get; set; } = AdContentType.ImageBanner;

        [StringLength(500)]
        [Display(Name = "Link ảnh Banner (URL)")]
        public string ImageUrl { get; set; }

        [StringLength(500)]
        [Display(Name = "Link đích khi bấm vào (URL)")]
        public string TargetUrl { get; set; }

        [Display(Name = "Mã HTML / Script quảng cáo")]
        public string HtmlContent { get; set; }

        [StringLength(50)]
        [Display(Name = "Chữ trên nút CTA")]
        public string CtaText { get; set; } = "Tìm hiểu thêm";

        [Display(Name = "Mở ở tab mới")]
        public bool OpenInNewTab { get; set; } = true;

        [Display(Name = "Đang bật")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Thứ tự ưu tiên")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Ngày bắt đầu")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Ngày kết thúc")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Lượt nhấp")]
        public int ClickCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
