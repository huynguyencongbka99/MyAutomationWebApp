using System.Collections.Generic;
using System.Threading.Tasks;
using AutoTechHub.Models;
using AutoTechHub.Models.ViewModels;

namespace AutoTechHub.Services
{
    public interface IAuthService
    {
        Task<(AppUser User, string ErrorMessage)> AuthenticateAsync(string usernameOrEmail, string password);
        Task<(AppUser User, string ErrorMessage)> RegisterAsync(RegisterViewModel model);
        Task<(AppUser User, string ErrorMessage)> CreateUserAsync(CreateUserViewModel model);
        Task<AppUser> GetUserByIdAsync(int id);
        Task<List<AppUser>> GetAllUsersAsync();
        Task<(bool Success, string ErrorMessage)> UpdateProfileAsync(int userId, ProfileViewModel model);
        Task<(bool Success, string ErrorMessage)> UpdateUserRoleAsync(int userId, UserRole newRole, int currentAdminId);
        Task<(bool Success, string ErrorMessage)> ToggleUserLockAsync(int userId, int currentAdminId);
        Task<(bool Success, string ErrorMessage)> DeleteUserAsync(int userId, int currentAdminId, bool deleteArticles = false);
        Task<(bool Success, string ErrorMessage)> ResetUserPasswordAsync(int userId, string newPassword, bool mustChangePassword);
        Task<(bool Success, string ErrorMessage)> CompleteForcedPasswordChangeAsync(int userId, string newPassword);
    }
}
