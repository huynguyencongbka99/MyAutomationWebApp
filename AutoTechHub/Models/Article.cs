using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AutoTechHub.Models
{
    public class Article
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tiêu đề bài viết là bắt buộc")]
        [StringLength(250, ErrorMessage = "Tiêu đề không được vượt quá 250 ký tự")]
        [Display(Name = "Tiêu đề bài viết")]
        public string Title { get; set; }

        [StringLength(300)]
        public string Slug { get; set; }

        [Required(ErrorMessage = "Tóm tắt bài viết là bắt buộc")]
        [StringLength(600, ErrorMessage = "Tóm tắt không được vượt quá 600 ký tự")]
        [Display(Name = "Tóm tắt ngắn")]
        public string Summary { get; set; }

        [Required(ErrorMessage = "Nội dung bài viết (Markdown) là bắt buộc")]
        [Display(Name = "Nội dung Markdown")]
        public string ContentMarkdown { get; set; }

        public string ContentHtml { get; set; }

        [StringLength(500)]
        [Display(Name = "Ảnh đại diện (URL)")]
        public string ThumbnailUrl { get; set; }

        [Required(ErrorMessage = "Tên tác giả là bắt buộc")]
        [StringLength(100)]
        [Display(Name = "Tác giả")]
        public string AuthorName { get; set; } = "Kỹ sư Tự động hóa";

        [StringLength(150)]
        [Display(Name = "Email tác giả")]
        public string AuthorEmail { get; set; }

        [Display(Name = "Thời gian đọc (phút)")]
        public int ReadingTimeMinutes { get; set; } = 5;

        public int ViewCount { get; set; } = 0;

        public int LikeCount { get; set; } = 0;

        [Display(Name = "Bài viết nổi bật")]
        public bool IsFeatured { get; set; } = false;

        [Display(Name = "Đã xuất bản")]
        public bool IsPublished { get; set; } = true;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Cập nhật lần cuối")]
        public DateTime? UpdatedAt { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn chuyên mục")]
        [Display(Name = "Chuyên mục")]
        public int CategoryId { get; set; }
        public virtual Category Category { get; set; }

        public virtual ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
