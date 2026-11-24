using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AutoTechHub.Models
{
    public enum UserRole
    {
        User = 0,
        Manager = 1,
        Admin = 2
    }

    public class AppUser
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 50 ký tự")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(150)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(256)]
        public string PasswordHash { get; set; }

        [Required(ErrorMessage = "Họ và tên là bắt buộc")]
        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [StringLength(500)]
        [Display(Name = "Giới thiệu ngắn")]
        public string Bio { get; set; }

        [Display(Name = "Vai trò")]
        public UserRole Role { get; set; } = UserRole.User;

        [Display(Name = "Đang hoạt động")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Bắt buộc đổi mật khẩu")]
        public bool MustChangePassword { get; set; } = false;

        [Display(Name = "Đổi mật khẩu gần nhất")]
        public DateTime? PasswordChangedAt { get; set; }

        [Display(Name = "Ngày tham gia")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Đăng nhập gần nhất")]
        public DateTime? LastLoginAt { get; set; }

        public virtual ICollection<Article> AuthoredArticles { get; set; } = new List<Article>();
    }
}
