using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoTechHub.Data;
using AutoTechHub.Models;
using AutoTechHub.Models.ViewModels;

namespace AutoTechHub.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;

        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(AppUser User, string ErrorMessage)> AuthenticateAsync(string usernameOrEmail, string password)
        {
            if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
                return (null, "Vui lòng nhập đầy đủ thông tin đăng nhập.");

            var normalized = usernameOrEmail.Trim().ToLower();
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == normalized || u.Email.ToLower() == normalized);

            if (user == null || !PasswordHasherHelper.VerifyPassword(password, user.PasswordHash))
                return (null, "Tên đăng nhập/Email hoặc mật khẩu không chính xác.");

            if (!user.IsActive)
                return (null, "Tài khoản này đã bị tạm khóa bởi quản trị viên.");

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (user, null);
        }

        public async Task<(AppUser User, string ErrorMessage)> RegisterAsync(RegisterViewModel model)
        {
            var createModel = new CreateUserViewModel
            {
                Username = model.Username,
                FullName = model.FullName,
                Email = model.Email,
                Password = model.Password,
                Role = UserRole.User // Public registration always gets User role
            };

            return await CreateUserAsync(createModel);
        }

        public async Task<(AppUser User, string ErrorMessage)> CreateUserAsync(CreateUserViewModel model)
        {
            var username = model.Username.Trim();
            var email = model.Email.Trim();

            if (await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower()))
                return (null, "Tên đăng nhập đã được sử dụng.");

            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower()))
                return (null, "Email này đã được đăng ký.");

            var user = new AppUser
            {
                Username = username,
                Email = email,
                FullName = model.FullName.Trim(),
                PasswordHash = PasswordHasherHelper.HashPassword(model.Password),
                Role = model.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return (user, null);
        }

        public async Task<AppUser> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<List<AppUser>> GetAllUsersAsync()
        {
            return await _context.Users
                .Include(u => u.AuthoredArticles)
                .OrderByDescending(u => u.Role)
                .ThenByDescending(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task<(bool Success, string ErrorMessage)> UpdateProfileAsync(int userId, ProfileViewModel model)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return (false, "Không tìm thấy tài khoản.");

            user.FullName = model.FullName.Trim();
            user.Bio = model.Bio?.Trim();

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                if (string.IsNullOrWhiteSpace(model.CurrentPassword) ||
                    !PasswordHasherHelper.VerifyPassword(model.CurrentPassword, user.PasswordHash))
                {
                    return (false, "Mật khẩu hiện tại không đúng.");
                }

                user.PasswordHash = PasswordHasherHelper.HashPassword(model.NewPassword);
                user.MustChangePassword = false;
                user.PasswordChangedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string ErrorMessage)> ResetUserPasswordAsync(int userId, string newPassword, bool mustChangePassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return (false, "Mật khẩu mới phải có ít nhất 6 ký tự.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return (false, "Không tìm thấy người dùng.");

            user.PasswordHash = PasswordHasherHelper.HashPassword(newPassword);
            user.MustChangePassword = mustChangePassword;
            user.PasswordChangedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string ErrorMessage)> CompleteForcedPasswordChangeAsync(int userId, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return (false, "Mật khẩu mới phải có ít nhất 6 ký tự.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return (false, "Không tìm thấy tài khoản.");

            if (PasswordHasherHelper.VerifyPassword(newPassword, user.PasswordHash))
                return (false, "Mật khẩu mới không được trùng với mật khẩu hiện tại.");

            user.PasswordHash = PasswordHasherHelper.HashPassword(newPassword);
            user.MustChangePassword = false;
            user.PasswordChangedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string ErrorMessage)> UpdateUserRoleAsync(int userId, UserRole newRole, int currentAdminId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return (false, "Không tìm thấy người dùng.");

            if (user.Id == currentAdminId)
                return (false, "Không thể thay đổi phân quyền của chính bạn.");

            if (user.Role == UserRole.Admin)
                return (false, "Không thể thay đổi phân quyền của tài khoản Quản trị viên (Admin).");

            user.Role = newRole;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string ErrorMessage)> ToggleUserLockAsync(int userId, int currentAdminId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return (false, "Không tìm thấy người dùng.");

            if (user.Id == currentAdminId)
                return (false, "Không thể tự khóa tài khoản của chính bạn.");

            if (user.Role == UserRole.Admin)
                return (false, "Không thể khóa tài khoản Quản trị viên (Admin).");

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string ErrorMessage)> DeleteUserAsync(int userId, int currentAdminId, bool deleteArticles = false)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return (false, "Không tìm thấy người dùng.");

            if (user.Id == currentAdminId)
                return (false, "Không thể tự xóa tài khoản của chính bạn.");

            if (user.Role == UserRole.Admin)
                return (false, "Không được phép xóa tài khoản Quản trị viên (Admin).");

            var authoredArticles = await _context.Articles
                .Where(a => a.AuthorUserId == userId)
                .ToListAsync();

            if (deleteArticles)
            {
                _context.Articles.RemoveRange(authoredArticles);
            }
            else
            {
                foreach (var article in authoredArticles)
                {
                    article.AuthorUserId = null;
                }
            }

            var reviewedArticles = await _context.Articles
                .Where(a => a.ReviewedByUserId == userId)
                .ToListAsync();
            foreach (var article in reviewedArticles)
            {
                article.ReviewedByUserId = null;
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return (true, null);
        }
    }
}
