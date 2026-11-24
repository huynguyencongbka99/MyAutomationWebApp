using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AutoTechHub.Models;

namespace AutoTechHub.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập hoặc email")]
        [Display(Name = "Tên đăng nhập hoặc Email")]
        public string UsernameOrEmail { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; } = true;

        public string ReturnUrl { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 50 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9_.-]+$", ErrorMessage = "Chỉ dùng chữ cái, số và các ký tự _ . -")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Họ và tên là bắt buộc")]
        [StringLength(100)]
        [Display(Name = "Họ và tên hiển thị")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(150)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [Display(Name = "Nhập lại mật khẩu")]
        public string ConfirmPassword { get; set; }
    }

    public class ProfileViewModel
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public UserRole Role { get; set; }

        [Required(ErrorMessage = "Họ và tên là bắt buộc")]
        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [StringLength(500)]
        [Display(Name = "Giới thiệu bản thân")]
        public string Bio { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại (chỉ nhập nếu muốn đổi mật khẩu)")]
        public string CurrentPassword { get; set; }

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; }
    }

    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [StringLength(50, MinimumLength = 3)]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Họ và tên là bắt buộc")]
        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress]
        [StringLength(150)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [Display(Name = "Vai trò")]
        public UserRole Role { get; set; } = UserRole.User;
    }

    public class AdminUsersViewModel
    {
        public List<AppUser> Users { get; set; } = new List<AppUser>();
        public CreateUserViewModel NewUser { get; set; } = new CreateUserViewModel();
    }

    public class AdminAdsViewModel
    {
        public SiteSetting SiteSetting { get; set; } = new SiteSetting();
        public List<Advertisement> Ads { get; set; } = new List<Advertisement>();
    }

    public class AdminProductsViewModel
    {
        public SiteSetting SiteSetting { get; set; } = new SiteSetting();
        public List<Product> Products { get; set; } = new List<Product>();
        public List<ProductInquiry> Inquiries { get; set; } = new List<ProductInquiry>();
    }

    public class ProductListViewModel
    {
        public List<Product> Products { get; set; } = new List<Product>();
        public List<string> Categories { get; set; } = new List<string>();
        public SiteSetting SiteSetting { get; set; } = new SiteSetting();

        public string SearchQuery { get; set; }
        public string CategoryName { get; set; }
        public string SortBy { get; set; } = "featured";
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; }
    }

    public class ProductDetailViewModel
    {
        public Product Product { get; set; }
        public List<Product> RelatedProducts { get; set; } = new List<Product>();
        public SiteSetting SiteSetting { get; set; } = new SiteSetting();
        public ProductInquiry Inquiry { get; set; } = new ProductInquiry();
    }

    public class ForceChangePasswordViewModel
    {
        public string Username { get; set; }
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới phải từ 6 ký tự trở lên")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [Display(Name = "Nhập lại mật khẩu mới")]
        public string ConfirmNewPassword { get; set; }
    }

    public class BrandingSettingsViewModel
    {
        [Required(ErrorMessage = "Tên website / thương hiệu là bắt buộc")]
        [StringLength(60)]
        [Display(Name = "Tên website / Thương hiệu")]
        public string SiteName { get; set; } = "AutoTechHub";

        [StringLength(150)]
        [Display(Name = "Khẩu hiệu (Tagline)")]
        public string SiteTagline { get; set; } = "Chia Sẻ Kiến Thức Tự Động Hóa";

        [StringLength(500)]
        [Display(Name = "Link ảnh Logo hiện tại")]
        public string LogoUrl { get; set; } = "/images/logo-default.svg";

        [StringLength(500)]
        [Display(Name = "Link Favicon hiện tại")]
        public string FaviconUrl { get; set; } = "/images/favicon.svg";

        [Display(Name = "Hiển thị tên thương hiệu bên cạnh Logo")]
        public bool ShowBrandTextWithLogo { get; set; } = true;
    }
}
