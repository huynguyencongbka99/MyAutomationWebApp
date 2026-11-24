using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AutoTechHub.Extensions;
using AutoTechHub.Models;
using AutoTechHub.Models.ViewModels;
using AutoTechHub.Services;

namespace AutoTechHub.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class AdminController : Controller
    {
        private readonly IArticleService _articleService;
        private readonly IAuthService _authService;
        private readonly IAdService _adService;
        private readonly IProductService _productService;
        private readonly ISiteConfigService _siteConfigService;
        private readonly IWebHostEnvironment _env;

        public AdminController(
            IArticleService articleService,
            IAuthService authService,
            IAdService adService,
            IProductService productService,
            ISiteConfigService siteConfigService,
            IWebHostEnvironment env)
        {
            _articleService = articleService;
            _authService = authService;
            _adService = adService;
            _productService = productService;
            _siteConfigService = siteConfigService;
            _env = env;
        }

        // GET: /Admin
        public async Task<IActionResult> Index()
        {
            var userId = User.GetUserId() ?? 0;
            var role = User.GetUserRole();
            var dashboard = await _articleService.GetAdminDashboardAsync(userId, role);
            return View(dashboard);
        }

        // POST: /Admin/TogglePublish/5
        [HttpPost]
        public async Task<IActionResult> TogglePublish(int id)
        {
            var article = await _articleService.GetArticleByIdAsync(id);
            if (article == null) return NotFound();

            var userId = User.GetUserId();
            var isAdmin = User.IsInRole(nameof(UserRole.Admin));
            if (!isAdmin && (!userId.HasValue || article.AuthorUserId != userId.Value))
            {
                return Forbid();
            }

            var newState = await _articleService.TogglePublishAsync(id);
            return Json(new { success = true, isPublished = newState });
        }

        // POST: /Admin/ApproveArticle/5
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveArticle(int id, string note)
        {
            var adminId = User.GetUserId() ?? 0;
            var ok = await _articleService.ApproveArticleAsync(id, adminId, note);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
                ? "Đã phê duyệt và xuất bản bài viết thành công!"
                : "Không tìm thấy bài viết.";

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/RejectArticle/5
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectArticle(int id, string note)
        {
            var adminId = User.GetUserId() ?? 0;
            var ok = await _articleService.RejectArticleAsync(id, adminId, note);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
                ? "Đã từ chối bài viết và gửi ghi chú cho tác giả."
                : "Không tìm thấy bài viết.";

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/DeleteArticle/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteArticle(int id)
        {
            var article = await _articleService.GetArticleByIdAsync(id);
            if (article == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy bài viết để xóa.";
                return RedirectToAction(nameof(Index));
            }

            var userId = User.GetUserId();
            var isAdmin = User.IsInRole(nameof(UserRole.Admin));
            if (!isAdmin && (!userId.HasValue || article.AuthorUserId != userId.Value))
            {
                return Forbid();
            }

            var result = await _articleService.DeleteArticleAsync(id);
            TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
                ? "Đã xóa bài viết thành công."
                : "Không tìm thấy bài viết để xóa.";

            return RedirectToAction(nameof(Index));
        }

        // ==================== USER & ROLE MANAGEMENT ====================

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var vm = new AdminUsersViewModel
            {
                Users = await _authService.GetAllUsersAsync(),
                NewUser = new CreateUserViewModel()
            };
            return View(vm);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser([Bind(Prefix = "NewUser")] CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var vm = new AdminUsersViewModel
                {
                    Users = await _authService.GetAllUsersAsync(),
                    NewUser = model
                };
                TempData["ErrorMessage"] = "Thông tin tạo tài khoản chưa hợp lệ.";
                return View("Users", vm);
            }

            var (user, error) = await _authService.CreateUserAsync(model);
            if (user == null)
            {
                TempData["ErrorMessage"] = error;
            }
            else
            {
                TempData["SuccessMessage"] = $"Đã tạo tài khoản '{user.Username}' với quyền {user.Role.GetRoleDisplayName()}.";
            }

            return RedirectToAction(nameof(Users));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRole(int userId, UserRole role)
        {
            var adminId = User.GetUserId() ?? 0;
            var (success, error) = await _authService.UpdateUserRoleAsync(userId, role, adminId);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "Cập nhật phân quyền thành công."
                : error;

            return RedirectToAction(nameof(Users));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserLock(int userId)
        {
            var adminId = User.GetUserId() ?? 0;
            var (success, error) = await _authService.ToggleUserLockAsync(userId, adminId);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "Đã thay đổi trạng thái tài khoản."
                : error;

            return RedirectToAction(nameof(Users));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int userId, bool deleteArticles = false)
        {
            var adminId = User.GetUserId() ?? 0;
            var (success, error) = await _authService.DeleteUserAsync(userId, adminId, deleteArticles);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "Đã xóa người dùng thành công."
                : error;

            return RedirectToAction(nameof(Users));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetUserPassword(int userId, string newPassword, bool mustChangePassword = false)
        {
            var (success, error) = await _authService.ResetUserPasswordAsync(userId, newPassword, mustChangePassword);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? (mustChangePassword
                    ? "Đã đặt lại mật khẩu thành viên (bắt buộc đổi ở lần đăng nhập tiếp theo)."
                    : "Đã đặt lại mật khẩu thành viên thành công.")
                : error;

            return RedirectToAction(nameof(Users));
        }

        // ==================== ADS MANAGEMENT ====================

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Ads()
        {
            var vm = new AdminAdsViewModel
            {
                SiteSetting = await _siteConfigService.GetSettingsAsync(),
                Ads = await _adService.GetAllAdsAsync()
            };
            return View(vm);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult CreateAd()
        {
            return View("AdForm", new Advertisement { IsActive = true, OpenInNewTab = true });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAd(Advertisement model)
        {
            if (!ModelState.IsValid)
            {
                return View("AdForm", model);
            }

            await _adService.CreateAdAsync(model);
            TempData["SuccessMessage"] = "Đã thêm quảng cáo mới thành công.";
            return RedirectToAction(nameof(Ads));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditAd(int id)
        {
            var ad = await _adService.GetAdByIdAsync(id);
            if (ad == null) return NotFound();

            return View("AdForm", ad);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAd(int id, Advertisement model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid)
            {
                return View("AdForm", model);
            }

            var updated = await _adService.UpdateAdAsync(model);
            if (updated == null) return NotFound();

            TempData["SuccessMessage"] = "Đã cập nhật quảng cáo.";
            return RedirectToAction(nameof(Ads));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAd(int id)
        {
            var ok = await _adService.DeleteAdAsync(id);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
                ? "Đã xóa quảng cáo."
                : "Không tìm thấy quảng cáo.";
            return RedirectToAction(nameof(Ads));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> ToggleAdActive(int id)
        {
            var state = await _adService.ToggleAdActiveAsync(id);
            return Json(new { success = true, isActive = state });
        }

        // ==================== PRODUCTS & STORE MANAGEMENT ====================

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Products()
        {
            var vm = new AdminProductsViewModel
            {
                SiteSetting = await _siteConfigService.GetSettingsAsync(),
                Products = await _productService.GetAllProductsForAdminAsync(),
                Inquiries = await _productService.GetInquiriesAsync()
            };
            return View(vm);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult CreateProduct()
        {
            return View("ProductForm", new Product
            {
                IsActive = true,
                Currency = "VNĐ",
                DescriptionMarkdown = @"### Mô tả tổng quan
Giới thiệu điểm nổi bật của sản phẩm...

### Thông số kỹ thuật
- Thông số 1: ...
- Thông số 2: ...

### Chính sách bảo hành & hỗ trợ
- Hỗ trợ kỹ thuật trực tiếp qua Zalo/Email"
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(Product model)
        {
            if (!ModelState.IsValid)
            {
                return View("ProductForm", model);
            }

            await _productService.CreateProductAsync(model);
            TempData["SuccessMessage"] = "Đã thêm mặt hàng mới vào hệ thống.";
            return RedirectToAction(nameof(Products));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();

            return View("ProductForm", product);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(int id, Product model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid)
            {
                return View("ProductForm", model);
            }

            var updated = await _productService.UpdateProductAsync(model);
            if (updated == null) return NotFound();

            TempData["SuccessMessage"] = "Cập nhật thông tin mặt hàng thành công.";
            return RedirectToAction(nameof(Products));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var ok = await _productService.DeleteProductAsync(id);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
                ? "Đã xóa mặt hàng."
                : "Không tìm thấy mặt hàng.";
            return RedirectToAction(nameof(Products));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> ToggleProductActive(int id)
        {
            var state = await _productService.ToggleProductActiveAsync(id);
            return Json(new { success = true, isActive = state });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInquiryStatus(int id, ProductInquiryStatus status)
        {
            var ok = await _productService.UpdateInquiryStatusAsync(id, status);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
                ? "Đã cập nhật trạng thái đơn yêu cầu."
                : "Không tìm thấy đơn yêu cầu.";
            return RedirectToAction(nameof(Products));
        }

        // ==================== SITE MODULE TOGGLES & SETTINGS ====================

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> ToggleAdsModule()
        {
            var enabled = await _siteConfigService.ToggleAdsModuleAsync();
            return Json(new { success = true, enabled });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> ToggleShopModule()
        {
            var enabled = await _siteConfigService.ToggleShopModuleAsync();
            return Json(new { success = true, enabled });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateShopSettings(SiteSetting model, string returnAction = "Products")
        {
            var current = await _siteConfigService.GetSettingsAsync();
            current.EnableShopModule = model.EnableShopModule;
            current.ShopNavTitle = model.ShopNavTitle;
            current.ShopBannerTitle = model.ShopBannerTitle;
            current.ShopBannerSubtitle = model.ShopBannerSubtitle;
            current.ContactPhone = model.ContactPhone;
            current.ContactZalo = model.ContactZalo;
            current.ContactEmail = model.ContactEmail;

            await _siteConfigService.UpdateSettingsAsync(current);
            TempData["SuccessMessage"] = "Đã lưu cấu hình phân mục Giới thiệu & Bán hàng.";
            return RedirectToAction(returnAction);
        }

        // ==================== BRANDING & LOGO MANAGEMENT ====================

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Branding()
        {
            var setting = await _siteConfigService.GetSettingsAsync();
            var vm = new BrandingSettingsViewModel
            {
                SiteName = setting.SiteName ?? "AutoTechHub",
                SiteTagline = setting.SiteTagline ?? "Chia Sẻ Kiến Thức Tự Động Hóa",
                LogoUrl = setting.LogoUrl ?? "/images/logo-default.svg",
                FaviconUrl = setting.FaviconUrl ?? "/images/favicon.svg",
                ShowBrandTextWithLogo = setting.ShowBrandTextWithLogo
            };
            return View(vm);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Branding(
            BrandingSettingsViewModel model,
            IFormFile logoFile,
            IFormFile faviconFile,
            bool resetDefaultLogo = false)
        {
            if (resetDefaultLogo)
            {
                model.LogoUrl = "/images/logo-default.svg";
                model.FaviconUrl = "/images/favicon.svg";
                await _siteConfigService.UpdateBrandingAsync(model);
                TempData["SuccessMessage"] = "Đã khôi phục Logo và Favicon mặc định.";
                return RedirectToAction(nameof(Branding));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (logoFile != null && logoFile.Length > 0)
            {
                var (savedUrl, error) = await SaveBrandingImageAsync(logoFile, "logo", 2 * 1024 * 1024);
                if (error != null)
                {
                    ModelState.AddModelError(string.Empty, error);
                    return View(model);
                }
                model.LogoUrl = savedUrl;
            }

            if (faviconFile != null && faviconFile.Length > 0)
            {
                var (savedUrl, error) = await SaveBrandingImageAsync(faviconFile, "favicon", 512 * 1024);
                if (error != null)
                {
                    ModelState.AddModelError(string.Empty, error);
                    return View(model);
                }
                model.FaviconUrl = savedUrl;
            }

            await _siteConfigService.UpdateBrandingAsync(model);
            TempData["SuccessMessage"] = "Đã cập nhật Logo và thông tin thương hiệu thành công!";
            return RedirectToAction(nameof(Branding));
        }

        private async Task<(string Url, string Error)> SaveBrandingImageAsync(IFormFile file, string prefix, long maxBytes)
        {
            if (file.Length > maxBytes)
            {
                return (null, $"File {prefix} vượt quá dung lượng cho phép ({maxBytes / 1024} KB).");
            }

            var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".webp", ".svg", ".ico" };
            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
            {
                return (null, "Chỉ hỗ trợ định dạng ảnh PNG, JPG, WEBP, SVG hoặc ICO.");
            }

            var contentType = ext switch
            {
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                ".ico" => "image/x-icon",
                _ => "application/octet-stream"
            };

            var fileName = $"{prefix}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}".Substring(0, 32) + ext;

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var asset = await _siteConfigService.SaveMediaAssetAsync(fileName, contentType, ms.ToArray());

            return ($"/media/{asset.Id}/{asset.FileName}", null);
        }

        // ==================== WEB VISUAL DESIGNER & LAYOUT STUDIO ====================

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Designer()
        {
            var setting = await _siteConfigService.GetSettingsAsync();
            var sections = _siteConfigService.GetHomeSections(setting);
            var blocks = await _siteConfigService.GetAllWebBlocksAsync();

            var vm = new WebDesignerViewModel
            {
                Design = new WebDesignerSaveModel
                {
                    PrimaryColor = string.IsNullOrWhiteSpace(setting.PrimaryColor) ? "#0d6efd" : setting.PrimaryColor,
                    AccentColor = string.IsNullOrWhiteSpace(setting.AccentColor) ? "#0dcaf0" : setting.AccentColor,
                    FontFamily = string.IsNullOrWhiteSpace(setting.FontFamily) ? "Segoe UI" : setting.FontFamily,
                    CardRadiusPx = setting.CardRadiusPx,
                    DefaultTheme = string.IsNullOrWhiteSpace(setting.DefaultTheme) ? "auto" : setting.DefaultTheme,
                    CustomCss = setting.CustomCss,
                    HomeLayoutJson = System.Text.Json.JsonSerializer.Serialize(sections),
                    HeroBadgeText = setting.HeroBadgeText,
                    HeroTitlePrefix = setting.HeroTitlePrefix,
                    HeroTitleHighlight = setting.HeroTitleHighlight,
                    HeroSubtitle = setting.HeroSubtitle,
                    HeroShowSearch = setting.HeroShowSearch,
                    HeroShowTags = setting.HeroShowTags,
                    HeroShowStats = setting.HeroShowStats,
                    HeroCtaText = setting.HeroCtaText,
                    HeroCtaUrl = setting.HeroCtaUrl,
                    FeaturedSectionTitle = setting.FeaturedSectionTitle,
                    CategoriesSectionTitle = setting.CategoriesSectionTitle,
                    LatestSectionTitle = setting.LatestSectionTitle,
                    FooterDescription = setting.FooterDescription,
                    GithubUrl = setting.GithubUrl,
                    YoutubeUrl = setting.YoutubeUrl,
                    LinkedinUrl = setting.LinkedinUrl
                },
                HomeSections = sections,
                WebBlocks = blocks,
                NewBlock = new WebBlock { IsActive = true, BlockType = WebBlockType.CallToAction, StyleVariant = WebBlockStyle.PrimaryGradient }
            };

            return View(vm);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDesigner([Bind(Prefix = "Design")] WebDesignerSaveModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Một số trường thiết kế chưa hợp lệ, vui lòng kiểm tra lại.";
                return RedirectToAction(nameof(Designer));
            }

            await _siteConfigService.SaveDesignerSettingsAsync(model);
            TempData["SuccessMessage"] = "Đã lưu thiết kế giao diện & bố cục trang web thành công!";
            return RedirectToAction(nameof(Designer));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetDesigner()
        {
            await _siteConfigService.ResetDesignerDefaultsAsync();
            TempData["SuccessMessage"] = "Đã khôi phục thiết kế giao diện & bố cục về mặc định.";
            return RedirectToAction(nameof(Designer));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWebBlock([Bind(Prefix = "NewBlock")] WebBlock model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ tiêu đề cho khối giao diện mới.";
                return RedirectToAction(nameof(Designer));
            }

            await _siteConfigService.CreateWebBlockAsync(model);
            TempData["SuccessMessage"] = "Đã thêm khối giao diện tự do mới!";
            return RedirectToAction(nameof(Designer));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateWebBlock(WebBlock model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Thông tin khối giao diện chưa hợp lệ.";
                return RedirectToAction(nameof(Designer));
            }

            await _siteConfigService.UpdateWebBlockAsync(model);
            TempData["SuccessMessage"] = "Đã cập nhật khối giao diện!";
            return RedirectToAction(nameof(Designer));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteWebBlock(int id)
        {
            var ok = await _siteConfigService.DeleteWebBlockAsync(id);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
                ? "Đã xóa khối giao diện."
                : "Không tìm thấy khối giao diện.";
            return RedirectToAction(nameof(Designer));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> ToggleWebBlockActive(int id)
        {
            var isActive = await _siteConfigService.ToggleWebBlockActiveAsync(id);
            return Json(new { success = true, isActive });
        }
    }
}
