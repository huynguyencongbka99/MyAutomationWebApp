using System;
using System.ComponentModel.DataAnnotations;

namespace AutoTechHub.Models
{
    public enum WebBlockType
    {
        [Display(Name = "Khối Kêu gọi hành động (CTA Banner)")]
        CallToAction = 0,

        [Display(Name = "Banner Thông báo / Khuyến mãi")]
        PromoBanner = 1,

        [Display(Name = "Khối Nội dung Markdown / Dịch vụ")]
        MarkdownSection = 2,

        [Display(Name = "Mã HTML tùy chỉnh")]
        CustomHtml = 3
    }

    public enum WebBlockStyle
    {
        [Display(Name = "Nền chuẩn theo giao diện (Surface)")]
        Surface = 0,

        [Display(Name = "Gradient Xanh Công nghệ")]
        PrimaryGradient = 1,

        [Display(Name = "Nền tối Tech (Dark)")]
        DarkTech = 2,

        [Display(Name = "Nổi bật Cam / Cảnh báo")]
        AccentWarm = 3
    }

    public class WebBlock
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tiêu đề khối là bắt buộc")]
        [StringLength(200)]
        [Display(Name = "Tiêu đề khối")]
        public string Title { get; set; }

        [StringLength(400)]
        [Display(Name = "Mô tả / Phụ đề")]
        public string Subtitle { get; set; }

        [Display(Name = "Nội dung chi tiết (Markdown)")]
        public string ContentMarkdown { get; set; }

        public string ContentHtml { get; set; }

        [Display(Name = "Mã HTML tùy chỉnh")]
        public string CustomHtmlCode { get; set; }

        [StringLength(80)]
        [Display(Name = "Chữ nút bấm (CTA)")]
        public string ButtonText { get; set; }

        [StringLength(400)]
        [Display(Name = "Đường dẫn nút bấm (URL)")]
        public string ButtonUrl { get; set; }

        [Display(Name = "Kiểu khối")]
        public WebBlockType BlockType { get; set; } = WebBlockType.CallToAction;

        [Display(Name = "Phong cách màu nền")]
        public WebBlockStyle StyleVariant { get; set; } = WebBlockStyle.PrimaryGradient;

        [Display(Name = "Thứ tự hiển thị")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Hiển thị trên trang chủ")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
