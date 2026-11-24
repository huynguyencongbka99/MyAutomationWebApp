using System;
using System.Security.Cryptography;

namespace AutoTechHub.Services
{
    public static class PasswordHasherHelper
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100_000;

        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Mật khẩu không được để trống.", nameof(password));

            var salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var key = pbkdf2.GetBytes(KeySize);

            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
                return false;

            var parts = storedHash.Split('.', 3);
            if (parts.Length != 3 || !int.TryParse(parts[0], out int iterations))
                return false;

            try
            {
                var salt = Convert.FromBase64String(parts[1]);
                var expectedKey = Convert.FromBase64String(parts[2]);

                using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
                var actualKey = pbkdf2.GetBytes(expectedKey.Length);

                return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
            }
            catch
            {
                return false;
            }
        }
    }
}
