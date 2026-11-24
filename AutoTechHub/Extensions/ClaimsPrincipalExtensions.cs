using System;
using System.Security.Claims;
using AutoTechHub.Models;

namespace AutoTechHub.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static int? GetUserId(this ClaimsPrincipal user)
        {
            if (user == null || !user.Identity.IsAuthenticated)
                return null;

            var raw = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(raw, out int id) ? id : (int?)null;
        }

        public static UserRole GetUserRole(this ClaimsPrincipal user)
        {
            if (user == null || !user.Identity.IsAuthenticated)
                return UserRole.User;

            var roleStr = user.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<UserRole>(roleStr, out var role) ? role : UserRole.User;
        }

        public static string GetFullName(this ClaimsPrincipal user)
        {
            if (user == null || !user.Identity.IsAuthenticated)
                return string.Empty;

            return user.FindFirstValue(ClaimTypes.GivenName) ?? user.Identity.Name ?? string.Empty;
        }

        public static string GetRoleDisplayName(this UserRole role)
        {
            return role switch
            {
                UserRole.Admin => "Quản trị viên (Admin)",
                UserRole.Manager => "Quản lý (Manager)",
                _ => "Thành viên (User)"
            };
        }
    }
}
