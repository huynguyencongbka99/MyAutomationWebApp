using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using AutoTechHub.Data;
using AutoTechHub.Filters;
using AutoTechHub.Services;

namespace AutoTechHub
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Reverse proxy headers (Vercel Edge / Render / Docker reverse proxy TLS termination)
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            // Scalable Database (PostgreSQL/Neon primary, SQLite local fallback)
            services.AddDbContext<AppDbContext>(options =>
                DatabaseConfigHelper.ConfigureDbContext(options, Configuration));

            // Persist DataProtection keys in database so auth cookies & CSRF survive Vercel cold starts
            services.AddDataProtection()
                .SetApplicationName("AutoTechHub");
            services.AddSingleton<IXmlRepository, DbXmlRepository>();
            services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(sp =>
                new ConfigureOptions<KeyManagementOptions>(options =>
                {
                    options.XmlRepository = sp.GetRequiredService<IXmlRepository>();
                }));

            // Cookie Authentication
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                    options.SlidingExpiration = true;
                });

            // Application Services
            services.AddSingleton<IMarkdownService, MarkdownService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ISiteConfigService, SiteConfigService>();
            services.AddScoped<IAdService, AdService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IArticleService, ArticleService>();
            services.AddScoped<ISnippetService, SnippetService>();
            services.AddScoped<EnforcePasswordChangeFilter>();

            services.AddControllersWithViews(options =>
            {
                options.Filters.Add<EnforcePasswordChangeFilter>();
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            bool isContainerOrProxied =
                !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PORT")) ||
                string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase);

            if (!isContainerOrProxied)
            {
                app.UseHttpsRedirection();
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "article_detail",
                    pattern: "bai-viet/{slug}",
                    defaults: new { controller = "Articles", action = "Details" });

                endpoints.MapControllerRoute(
                    name: "category_detail",
                    pattern: "chuyen-muc/{slug}",
                    defaults: new { controller = "Categories", action = "Details" });

                endpoints.MapControllerRoute(
                    name: "product_detail",
                    pattern: "san-pham/{slug}",
                    defaults: new { controller = "Products", action = "Details" });

                endpoints.MapGet("/healthz", async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsync("ok");
                });

                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
