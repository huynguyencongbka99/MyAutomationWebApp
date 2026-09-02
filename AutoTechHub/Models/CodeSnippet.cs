using System;
using System.ComponentModel.DataAnnotations;

namespace AutoTechHub.Models
{
    public class CodeSnippet
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tiêu đề snippet là bắt buộc")]
        [StringLength(200)]
        [Display(Name = "Tiêu đề đoạn mã")]
        public string Title { get; set; }

        [StringLength(500)]
        [Display(Name = "Mô tả ngắn")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Ngôn ngữ lập trình là bắt buộc")]
        [StringLength(50)]
        [Display(Name = "Ngôn ngữ (C#, Python, StructuredText, C++, Bash, JSON...)")]
        public string Language { get; set; } = "csharp";

        [Required(ErrorMessage = "Nội dung mã nguồn không được để trống")]
        [Display(Name = "Mã nguồn")]
        public string CodeContent { get; set; }

        [StringLength(100)]
        [Display(Name = "Tác giả / Nguồn")]
        public string Author { get; set; } = "AutoTech Hub";

        [StringLength(50)]
        [Display(Name = "Danh mục tự động hóa")]
        public string CategoryTag { get; set; } = "Industrial";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
