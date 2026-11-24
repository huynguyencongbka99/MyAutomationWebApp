using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoTechHub.Extensions;
using AutoTechHub.Models;
using AutoTechHub.Models.ViewModels;
using AutoTechHub.Services;

namespace AutoTechHub.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (user, error) = await _authService.AuthenticateAsync(model.UsernameOrEmail, model.Password);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, error);
                return View(model);
            }

            await SignInUserAsync(user, model.RememberMe);

            if (user.MustChangePassword)
            {
                TempData["ErrorMessage"] = "Vui lòng đổi mật khẩu mới trước khi tiếp tục sử dụng hệ thống.";
                return RedirectToAction(nameof(ForceChangePassword));
            }

            TempData["SuccessMessage"] = $"Chào mừng {user.FullName} đã đăng nhập!";

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            if (user.Role == UserRole.Admin || user.Role == UserRole.Manager)
            {
                return RedirectToAction("Index", "Admin");
            }

            return RedirectToAction("MyArticles", "Articles");
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (user, error) = await _authService.RegisterAsync(model);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, error);
                return View(model);
            }

            await SignInUserAsync(user, isPersistent: true);
            TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Bạn có thể viết bài và gửi chờ Admin phê duyệt.";
            return RedirectToAction("MyArticles", "Articles");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessMessage"] = "Bạn đã đăng xuất khỏi hệ thống.";
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = User.GetUserId();
            if (!userId.HasValue) return Challenge();

            var user = await _authService.GetUserByIdAsync(userId.Value);
            if (user == null) return NotFound();

            return View(new ProfileViewModel
            {
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                FullName = user.FullName,
                Bio = user.Bio
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var userId = User.GetUserId();
            if (!userId.HasValue) return Challenge();

            var currentUser = await _authService.GetUserByIdAsync(userId.Value);
            if (currentUser == null) return NotFound();

            model.Username = currentUser.Username;
            model.Email = currentUser.Email;
            model.Role = currentUser.Role;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (success, error) = await _authService.UpdateProfileAsync(userId.Value, model);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error);
                return View(model);
            }

            var updated = await _authService.GetUserByIdAsync(userId.Value);
            await SignInUserAsync(updated, isPersistent: true);

            TempData["SuccessMessage"] = "Cập nhật thông tin tài khoản thành công!";
            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ForceChangePassword()
        {
            var userId = User.GetUserId();
            if (!userId.HasValue) return Challenge();

            var user = await _authService.GetUserByIdAsync(userId.Value);
            if (user == null) return NotFound();

            if (!user.MustChangePassword)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new ForceChangePasswordViewModel
            {
                Username = user.Username,
                FullName = user.FullName
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForceChangePassword(ForceChangePasswordViewModel model)
        {
            var userId = User.GetUserId();
            if (!userId.HasValue) return Challenge();

            var user = await _authService.GetUserByIdAsync(userId.Value);
            if (user == null) return NotFound();

            model.Username = user.Username;
            model.FullName = user.FullName;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (success, error) = await _authService.CompleteForcedPasswordChangeAsync(userId.Value, model.NewPassword);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error);
                return View(model);
            }

            var updated = await _authService.GetUserByIdAsync(userId.Value);
            await SignInUserAsync(updated, isPersistent: true);
            TempData["SuccessMessage"] = "Đổi mật khẩu mới thành công!";

            if (updated.Role == UserRole.Admin || updated.Role == UserRole.Manager)
            {
                return RedirectToAction("Index", "Admin");
            }

            return RedirectToAction("MyArticles", "Articles");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task SignInUserAsync(AppUser user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.GivenName, user.FullName ?? user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("MustChangePassword", user.MustChangePassword ? "true" : "false")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);
        }
    }
}
