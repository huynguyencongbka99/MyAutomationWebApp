using System;
using System.ComponentModel.DataAnnotations;

namespace AutoTechHub.Models
{
    public class Comment
    {
        public int Id { get; set; }

        public int ArticleId { get; set; }
        public virtual Article Article { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên của bạn")]
        [StringLength(100)]
        [Display(Name = "Họ tên")]
        public string AuthorName { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(150)]
        [Display(Name = "Email (tùy chọn)")]
        public string AuthorEmail { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung bình luận")]
        [StringLength(2000)]
        [Display(Name = "Bình luận")]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsApproved { get; set; } = true;
    }
}
