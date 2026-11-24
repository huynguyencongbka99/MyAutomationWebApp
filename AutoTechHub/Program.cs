using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AutoTechHub.Data;
using AutoTechHub.Services;

namespace AutoTechHub
{
    public class Program
    {
        public static void Main(string[] args)
        {
            int resetIdx = Array.FindIndex(args, a => string.Equals(a, "--reset-password", StringComparison.OrdinalIgnoreCase));
            var host = CreateHostBuilder(resetIdx >= 0 ? Array.Empty<string>() : args).Build();

            // Create database and seed data
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<AppDbContext>();
                    var markdownService = services.GetRequiredService<IMarkdownService>();
                    DbInitializer.Initialize(context, markdownService);

                    if (resetIdx >= 0)
                    {
                        Environment.ExitCode = HandleResetPasswordCli(context, args, resetIdx);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Lỗi xảy ra trong quá trình khởi tạo cơ sở dữ liệu.");
                    if (resetIdx >= 0)
                    {
                        Console.Error.WriteLine($"[ERROR] {ex.Message}");
                        Environment.ExitCode = 1;
                        return;
                    }
                }
            }

            host.Run();
        }

        private static int HandleResetPasswordCli(AppDbContext context, string[] args, int resetIdx)
        {
            if (args.Length <= resetIdx + 2)
            {
                Console.Error.WriteLine("Cách dùng: dotnet run --project AutoTechHub -- --reset-password <username> <new_password>");
                return 1;
            }

            var username = args[resetIdx + 1].Trim();
            var newPassword = args[resetIdx + 2];

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                Console.Error.WriteLine("Mật khẩu mới phải có ít nhất 6 ký tự.");
                return 1;
            }

            var user = context.Users.FirstOrDefault(u => u.Username.ToLower() == username.ToLower());
            if (user == null)
            {
                Console.Error.WriteLine($"Không tìm thấy tài khoản '{username}'.");
                return 1;
            }

            user.PasswordHash = PasswordHasherHelper.HashPassword(newPassword);
            user.IsActive = true;
            user.MustChangePassword = false;
            user.PasswordChangedAt = DateTime.UtcNow;
            context.SaveChanges();

            Console.WriteLine($"[OK] Đã đặt lại mật khẩu và mở khóa cho tài khoản '{user.Username}' (Role: {user.Role}).");
            return 0;
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();

                    // Bind to $PORT injected by Vercel/Render/PaaS when ASPNETCORE_URLS is not explicitly provided
                    if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
                    {
                        var port = Environment.GetEnvironmentVariable("PORT");
                        if (!string.IsNullOrWhiteSpace(port) && int.TryParse(port.Trim(), out var parsedPort) && parsedPort > 0)
                        {
                            webBuilder.UseUrls($"http://0.0.0.0:{parsedPort}");
                        }
                    }
                });
    }
}
