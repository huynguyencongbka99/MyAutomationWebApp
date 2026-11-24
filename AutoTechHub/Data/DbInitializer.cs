using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using AutoTechHub.Models;
using AutoTechHub.Services;

namespace AutoTechHub.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context, IMarkdownService markdownService)
        {
            context.Database.EnsureCreated();

            if (context.Database.IsSqlite())
            {
                EnsureSqliteSchemaUpgraded(context);
            }
            else if (context.Database.IsNpgsql())
            {
                EnsurePostgresSchemaUpgraded(context);
                MigrateFromSqliteIfPostgresEmpty(context);
            }

            // Seed Users & rotate legacy insecure demo passwords if still present
            SeedUsers(context);
            RotateLegacyDemoPasswords(context);

            // Seed Site Settings
            SeedSiteSettings(context);

            // Seed Categories, Tags, Articles, Comments, Snippets if empty
            if (!context.Categories.Any())
            {
                SeedBlogContent(context, markdownService);
            }

            // Seed Sample Advertisements & Products
            SeedAds(context);
            SeedProducts(context, markdownService);
        }

        private static void EnsureSqliteSchemaUpgraded(AppDbContext context)
        {
            var conn = context.Database.GetDbConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose)
            {
                conn.Open();
            }

            try
            {
                var ddlStatements = new[]
                {
                    @"CREATE TABLE IF NOT EXISTS ""Users"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Users"" PRIMARY KEY AUTOINCREMENT,
                        ""Username"" TEXT NOT NULL,
                        ""Email"" TEXT NOT NULL,
                        ""PasswordHash"" TEXT NOT NULL,
                        ""FullName"" TEXT NOT NULL,
                        ""Bio"" TEXT NULL,
                        ""Role"" INTEGER NOT NULL DEFAULT 0,
                        ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                        ""MustChangePassword"" INTEGER NOT NULL DEFAULT 0,
                        ""PasswordChangedAt"" TEXT NULL,
                        ""CreatedAt"" TEXT NOT NULL,
                        ""LastLoginAt"" TEXT NULL
                    );",
                    @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Users_Username"" ON ""Users"" (""Username"");",
                    @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Users_Email"" ON ""Users"" (""Email"");",

                    @"CREATE TABLE IF NOT EXISTS ""Advertisements"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Advertisements"" PRIMARY KEY AUTOINCREMENT,
                        ""Title"" TEXT NOT NULL,
                        ""Description"" TEXT NULL,
                        ""Placement"" INTEGER NOT NULL DEFAULT 1,
                        ""AdType"" INTEGER NOT NULL DEFAULT 0,
                        ""ImageUrl"" TEXT NULL,
                        ""TargetUrl"" TEXT NULL,
                        ""HtmlContent"" TEXT NULL,
                        ""CtaText"" TEXT NULL,
                        ""OpenInNewTab"" INTEGER NOT NULL DEFAULT 1,
                        ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                        ""DisplayOrder"" INTEGER NOT NULL DEFAULT 0,
                        ""StartDate"" TEXT NULL,
                        ""EndDate"" TEXT NULL,
                        ""ClickCount"" INTEGER NOT NULL DEFAULT 0,
                        ""CreatedAt"" TEXT NOT NULL
                    );",

                    @"CREATE TABLE IF NOT EXISTS ""Products"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Products"" PRIMARY KEY AUTOINCREMENT,
                        ""Name"" TEXT NOT NULL,
                        ""Slug"" TEXT NULL,
                        ""Sku"" TEXT NULL,
                        ""CategoryName"" TEXT NOT NULL,
                        ""ShortDescription"" TEXT NOT NULL,
                        ""DescriptionMarkdown"" TEXT NULL,
                        ""DescriptionHtml"" TEXT NULL,
                        ""Price"" TEXT NOT NULL,
                        ""OriginalPrice"" TEXT NULL,
                        ""Currency"" TEXT NULL,
                        ""ImageUrl"" TEXT NULL,
                        ""ExternalBuyUrl"" TEXT NULL,
                        ""StockStatus"" INTEGER NOT NULL DEFAULT 0,
                        ""IsFeatured"" INTEGER NOT NULL DEFAULT 0,
                        ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                        ""DisplayOrder"" INTEGER NOT NULL DEFAULT 0,
                        ""CreatedAt"" TEXT NOT NULL,
                        ""UpdatedAt"" TEXT NULL
                    );",
                    @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Products_Slug"" ON ""Products"" (""Slug"");",

                    @"CREATE TABLE IF NOT EXISTS ""ProductInquiries"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_ProductInquiries"" PRIMARY KEY AUTOINCREMENT,
                        ""ProductId"" INTEGER NOT NULL,
                        ""CustomerName"" TEXT NOT NULL,
                        ""Phone"" TEXT NOT NULL,
                        ""Email"" TEXT NULL,
                        ""Quantity"" INTEGER NOT NULL DEFAULT 1,
                        ""Note"" TEXT NULL,
                        ""Status"" INTEGER NOT NULL DEFAULT 0,
                        ""CreatedAt"" TEXT NOT NULL,
                        CONSTRAINT ""FK_ProductInquiries_Products_ProductId"" FOREIGN KEY (""ProductId"") REFERENCES ""Products"" (""Id"") ON DELETE CASCADE
                    );",

                    @"CREATE TABLE IF NOT EXISTS ""SiteSettings"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_SiteSettings"" PRIMARY KEY AUTOINCREMENT,
                        ""EnableAdsModule"" INTEGER NOT NULL DEFAULT 1,
                        ""EnableShopModule"" INTEGER NOT NULL DEFAULT 1,
                        ""ShopNavTitle"" TEXT NULL,
                        ""ShopBannerTitle"" TEXT NULL,
                        ""ShopBannerSubtitle"" TEXT NULL,
                        ""ContactPhone"" TEXT NULL,
                        ""ContactZalo"" TEXT NULL,
                        ""ContactEmail"" TEXT NULL,
                        ""SiteName"" TEXT NOT NULL DEFAULT 'AutoTechHub',
                        ""SiteTagline"" TEXT NULL,
                        ""LogoUrl"" TEXT NULL,
                        ""FaviconUrl"" TEXT NULL,
                        ""ShowBrandTextWithLogo"" INTEGER NOT NULL DEFAULT 1,
                        ""PrimaryColor"" TEXT NULL,
                        ""AccentColor"" TEXT NULL,
                        ""FontFamily"" TEXT NULL,
                        ""CardRadiusPx"" INTEGER NOT NULL DEFAULT 16,
                        ""DefaultTheme"" TEXT NULL,
                        ""CustomCss"" TEXT NULL,
                        ""HomeLayoutJson"" TEXT NULL,
                        ""HeroBadgeText"" TEXT NULL,
                        ""HeroTitlePrefix"" TEXT NULL,
                        ""HeroTitleHighlight"" TEXT NULL,
                        ""HeroSubtitle"" TEXT NULL,
                        ""HeroShowSearch"" INTEGER NOT NULL DEFAULT 1,
                        ""HeroShowTags"" INTEGER NOT NULL DEFAULT 1,
                        ""HeroShowStats"" INTEGER NOT NULL DEFAULT 1,
                        ""HeroCtaText"" TEXT NULL,
                        ""HeroCtaUrl"" TEXT NULL,
                        ""FeaturedSectionTitle"" TEXT NULL,
                        ""CategoriesSectionTitle"" TEXT NULL,
                        ""LatestSectionTitle"" TEXT NULL,
                        ""FooterDescription"" TEXT NULL,
                        ""GithubUrl"" TEXT NULL,
                        ""YoutubeUrl"" TEXT NULL,
                        ""LinkedinUrl"" TEXT NULL,
                        ""UpdatedAt"" TEXT NOT NULL
                    );",

                    @"CREATE TABLE IF NOT EXISTS ""WebBlocks"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_WebBlocks"" PRIMARY KEY AUTOINCREMENT,
                        ""Title"" TEXT NOT NULL,
                        ""Subtitle"" TEXT NULL,
                        ""ContentMarkdown"" TEXT NULL,
                        ""ContentHtml"" TEXT NULL,
                        ""CustomHtmlCode"" TEXT NULL,
                        ""ButtonText"" TEXT NULL,
                        ""ButtonUrl"" TEXT NULL,
                        ""BlockType"" INTEGER NOT NULL DEFAULT 0,
                        ""StyleVariant"" INTEGER NOT NULL DEFAULT 1,
                        ""DisplayOrder"" INTEGER NOT NULL DEFAULT 0,
                        ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                        ""CreatedAt"" TEXT NOT NULL
                    );",

                    @"CREATE TABLE IF NOT EXISTS ""DataProtectionKeys"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_DataProtectionKeys"" PRIMARY KEY AUTOINCREMENT,
                        ""FriendlyName"" TEXT NOT NULL,
                        ""Xml"" TEXT NOT NULL
                    );",

                    @"CREATE TABLE IF NOT EXISTS ""MediaAssets"" (
                        ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_MediaAssets"" PRIMARY KEY AUTOINCREMENT,
                        ""FileName"" TEXT NOT NULL,
                        ""ContentType"" TEXT NOT NULL,
                        ""Data"" BLOB NOT NULL,
                        ""CreatedAt"" TEXT NOT NULL
                    );"
                };

                foreach (var sql in ddlStatements)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }

                AddMissingSqliteColumns(conn, "Articles", new Dictionary<string, string>
                {
                    ["ApprovalStatus"] = @"ALTER TABLE ""Articles"" ADD COLUMN ""ApprovalStatus"" INTEGER NOT NULL DEFAULT 2;",
                    ["AuthorUserId"] = @"ALTER TABLE ""Articles"" ADD COLUMN ""AuthorUserId"" INTEGER NULL;",
                    ["ReviewedByUserId"] = @"ALTER TABLE ""Articles"" ADD COLUMN ""ReviewedByUserId"" INTEGER NULL;",
                    ["ReviewedAt"] = @"ALTER TABLE ""Articles"" ADD COLUMN ""ReviewedAt"" TEXT NULL;",
                    ["ReviewNote"] = @"ALTER TABLE ""Articles"" ADD COLUMN ""ReviewNote"" TEXT NULL;"
                });

                AddMissingSqliteColumns(conn, "Users", new Dictionary<string, string>
                {
                    ["MustChangePassword"] = @"ALTER TABLE ""Users"" ADD COLUMN ""MustChangePassword"" INTEGER NOT NULL DEFAULT 0;",
                    ["PasswordChangedAt"] = @"ALTER TABLE ""Users"" ADD COLUMN ""PasswordChangedAt"" TEXT NULL;"
                });

                AddMissingSqliteColumns(conn, "SiteSettings", new Dictionary<string, string>
                {
                    ["SiteName"] = @"ALTER TABLE ""SiteSettings"" ADD COLUMN ""SiteName"" TEXT NOT NULL DEFAULT 'AutoTechHub';",
                    ["SiteTagline"] = @"ALTER TABLE ""SiteSettings"" ADD COLUMN ""SiteTagline"" TEXT NULL;",
                    ["LogoUrl"] = @"ALTER TABLE ""SiteSettings"" ADD COLUMN ""LogoUrl"" TEXT NULL;",
                    ["FaviconUrl"] = @"ALTER TABLE ""SiteSettings"" ADD COLUMN ""FaviconUrl"" TEXT NULL;",
                    ["ShowBrandTextWithLogo"] = @"ALTER TABLE ""SiteSettings"" ADD COLUMN ""ShowBrandTextWithLogo"" INTEGER NOT NULL DEFAULT 1;",
                    ["PrimaryColor"] = @"ALTER TABLE ""SiteSettings"" ADD COLUMN ""PrimaryColor"" TEXT NULL;",
                    ["AccentColor"] = @"ALTER TABLE ""SiteSettings"" ADD COLUMN ""AccentColor"" TEXT NULL;",
                    ["FontFamily"] = @"ALTER TABLE ""SiteSettings"" ADD COLUMN ""FontFamily"" TEXT NULL;",
                    ["CardRadiusPx"] = @"ALTER TABLE ""SiteSettings"" ADD COLUMN ""CardRadiusPx"" INTEGER NOT NULL DEFAULT 16;",
                    ["DefaultTheme"] = @"ALTER TABLE ""SiteSettings"" ADD COLUMN ""DefaultTheme"" TEXT NULL;",
                    ["CustomCss"] = @"ALTER TABLE ""SiteSettings"" ADD COLUMN ""CustomCss"" TEXT NULL;",
                    ["HomeLayoutJson"] = @"ALTER TABLE ""SiteSettings"" ADD COLUMN ""HomeLayoutJson"" TEXT NULL;",
                    ["HeroBadgeText"] = @"ALTER TABLE ""SiteSettings"" ADD COLUMN ""HeroBadgeText"" TEXT NULL;",
                    ["HeroTitlePrefix"] = @"ALTER TABLE ""SiteSettings"" ADD COLUMN ""HeroTitlePrefix"" TEXT NULL;",
                    ["HeroTitleHighlight"] = @"ALTER TABLE ""SiteSettings"" ADD COLUMN ""HeroTitleHighlight"" TEXT NULL;",
                    ["HeroSubtitle"] = @"ALTER TABLE ""SiteSettings"" ADD COLUMN ""HeroSubtitle"" TEXT NULL;",
                    ["HeroShowSearch"] = @"ALTER TABLE ""SiteSettings"" ADD COLUMN ""HeroShowSearch"" INTEGER NOT NULL DEFAULT 1;",
                    ["HeroShowTags"] = @"ALTER TABLE ""SiteSettings"" ADD COLUMN ""HeroShowTags"" INTEGER NOT NULL DEFAULT 1;",
                    ["HeroShowStats"] = @"ALTER TABLE ""SiteSettings"" ADD COLUMN ""HeroShowStats"" INTEGER NOT NULL DEFAULT 1;",
                    ["HeroCtaText"] = @"ALTER TABLE ""SiteSettings"" ADD COLUMN ""HeroCtaText"" TEXT NULL;",
                    ["HeroCtaUrl"] = @"ALTER TABLE ""SiteSettings"" ADD COLUMN ""HeroCtaUrl"" TEXT NULL;",
                    ["FeaturedSectionTitle"] = @"ALTER TABLE ""SiteSettings"" ADD COLUMN ""FeaturedSectionTitle"" TEXT NULL;",
                    ["CategoriesSectionTitle"] = @"ALTER TABLE ""SiteSettings"" ADD COLUMN ""CategoriesSectionTitle"" TEXT NULL;",
                    ["LatestSectionTitle"] = @"ALTER TABLE ""SiteSettings"" ADD COLUMN ""LatestSectionTitle"" TEXT NULL;",
                    ["FooterDescription"] = @"ALTER TABLE ""SiteSettings"" ADD COLUMN ""FooterDescription"" TEXT NULL;",
                    ["GithubUrl"] = @"ALTER TABLE ""SiteSettings"" ADD COLUMN ""GithubUrl"" TEXT NULL;",
                    ["YoutubeUrl"] = @"ALTER TABLE ""SiteSettings"" ADD COLUMN ""YoutubeUrl"" TEXT NULL;",
                    ["LinkedinUrl"] = @"ALTER TABLE ""SiteSettings"" ADD COLUMN ""LinkedinUrl"" TEXT NULL;"
                });
            }
            finally
            {
                if (shouldClose)
                {
                    conn.Close();
                }
            }
        }

        private static void AddMissingSqliteColumns(System.Data.Common.DbConnection conn, string tableName, Dictionary<string, string> columns)
        {
            var existingCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = $@"PRAGMA table_info(""{tableName}"");";
                using var reader = pragmaCmd.ExecuteReader();
                while (reader.Read())
                {
                    existingCols.Add(reader["name"].ToString());
                }
            }

            foreach (var col in columns)
            {
                if (!existingCols.Contains(col.Key))
                {
                    using var alterCmd = conn.CreateCommand();
                    alterCmd.CommandText = col.Value;
                    alterCmd.ExecuteNonQuery();
                }
            }
        }

        private static void EnsurePostgresSchemaUpgraded(AppDbContext context)
        {
            var alterStatements = new[]
            {
                @"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""MustChangePassword"" boolean NOT NULL DEFAULT false;",
                @"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""PasswordChangedAt"" timestamp without time zone NULL;",
                @"ALTER TABLE ""SiteSettings"" ADD COLUMN IF NOT EXISTS ""SiteName"" character varying(60) NOT NULL DEFAULT 'AutoTechHub';",
                @"ALTER TABLE ""SiteSettings"" ADD COLUMN IF NOT EXISTS ""SiteTagline"" character varying(150) NULL;",
                @"ALTER TABLE ""SiteSettings"" ADD COLUMN IF NOT EXISTS ""LogoUrl"" character varying(500) NULL;",
                @"ALTER TABLE ""SiteSettings"" ADD COLUMN IF NOT EXISTS ""FaviconUrl"" character varying(500) NULL;",
                @"ALTER TABLE ""SiteSettings"" ADD COLUMN IF NOT EXISTS ""ShowBrandTextWithLogo"" boolean NOT NULL DEFAULT true;",
                @"ALTER TABLE ""SiteSettings"" ADD COLUMN IF NOT EXISTS ""PrimaryColor"" character varying(20) NULL;",
                @"ALTER TABLE ""SiteSettings"" ADD COLUMN IF NOT EXISTS ""AccentColor"" character varying(20) NULL;",
                @"ALTER TABLE ""SiteSettings"" ADD COLUMN IF NOT EXISTS ""FontFamily"" character varying(80) NULL;",
                @"ALTER TABLE ""SiteSettings"" ADD COLUMN IF NOT EXISTS ""CardRadiusPx"" integer NOT NULL DEFAULT 16;",
                @"ALTER TABLE ""SiteSettings"" ADD COLUMN IF NOT EXISTS ""DefaultTheme"" character varying(20) NULL;",
                @"ALTER TABLE ""SiteSettings"" ADD COLUMN IF NOT EXISTS ""CustomCss"" text NULL;",
                @"ALTER TABLE ""SiteSettings"" ADD COLUMN IF NOT EXISTS ""HomeLayoutJson"" text NULL;",
                @"ALTER TABLE ""SiteSettings"" ADD COLUMN IF NOT EXISTS ""HeroBadgeText"" character varying(120) NULL;",
                @"ALTER TABLE ""SiteSettings"" ADD COLUMN IF NOT EXISTS ""HeroTitlePrefix"" character varying(150) NULL;",
                @"ALTER TABLE ""SiteSettings"" ADD COLUMN IF NOT EXISTS ""HeroTitleHighlight"" character varying(150) NULL;",
                @"ALTER TABLE ""SiteSettings"" ADD COLUMN IF NOT EXISTS ""HeroSubtitle"" character varying(500) NULL;",
                @"ALTER TABLE ""SiteSettings"" ADD COLUMN IF NOT EXISTS ""HeroShowSearch"" boolean NOT NULL DEFAULT true;",
                @"ALTER TABLE ""SiteSettings"" ADD COLUMN IF NOT EXISTS ""HeroShowTags"" boolean NOT NULL DEFAULT true;",
                @"ALTER TABLE ""SiteSettings"" ADD COLUMN IF NOT EXISTS ""HeroShowStats"" boolean NOT NULL DEFAULT true;",
                @"ALTER TABLE ""SiteSettings"" ADD COLUMN IF NOT EXISTS ""HeroCtaText"" character varying(80) NULL;",
                @"ALTER TABLE ""SiteSettings"" ADD COLUMN IF NOT EXISTS ""HeroCtaUrl"" character varying(300) NULL;",
                @"ALTER TABLE ""SiteSettings"" ADD COLUMN IF NOT EXISTS ""FeaturedSectionTitle"" character varying(100) NULL;",
                @"ALTER TABLE ""SiteSettings"" ADD COLUMN IF NOT EXISTS ""CategoriesSectionTitle"" character varying(100) NULL;",
                @"ALTER TABLE ""SiteSettings"" ADD COLUMN IF NOT EXISTS ""LatestSectionTitle"" character varying(100) NULL;",
                @"ALTER TABLE ""SiteSettings"" ADD COLUMN IF NOT EXISTS ""FooterDescription"" character varying(500) NULL;",
                @"ALTER TABLE ""SiteSettings"" ADD COLUMN IF NOT EXISTS ""GithubUrl"" character varying(300) NULL;",
                @"ALTER TABLE ""SiteSettings"" ADD COLUMN IF NOT EXISTS ""YoutubeUrl"" character varying(300) NULL;",
                @"ALTER TABLE ""SiteSettings"" ADD COLUMN IF NOT EXISTS ""LinkedinUrl"" character varying(300) NULL;",
                @"CREATE TABLE IF NOT EXISTS ""WebBlocks"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                    ""Title"" character varying(200) NOT NULL,
                    ""Subtitle"" character varying(400) NULL,
                    ""ContentMarkdown"" text NULL,
                    ""ContentHtml"" text NULL,
                    ""CustomHtmlCode"" text NULL,
                    ""ButtonText"" character varying(80) NULL,
                    ""ButtonUrl"" character varying(400) NULL,
                    ""BlockType"" integer NOT NULL DEFAULT 0,
                    ""StyleVariant"" integer NOT NULL DEFAULT 1,
                    ""DisplayOrder"" integer NOT NULL DEFAULT 0,
                    ""IsActive"" boolean NOT NULL DEFAULT true,
                    ""CreatedAt"" timestamp without time zone NOT NULL
                );",
                @"CREATE TABLE IF NOT EXISTS ""DataProtectionKeys"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                    ""FriendlyName"" character varying(200) NOT NULL,
                    ""Xml"" text NOT NULL
                );",
                @"CREATE TABLE IF NOT EXISTS ""MediaAssets"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                    ""FileName"" character varying(200) NOT NULL,
                    ""ContentType"" character varying(120) NOT NULL,
                    ""Data"" bytea NOT NULL,
                    ""CreatedAt"" timestamp without time zone NOT NULL
                );"
            };

            foreach (var sql in alterStatements)
            {
                context.Database.ExecuteSqlRaw(sql);
            }
        }

        private static void MigrateFromSqliteIfPostgresEmpty(AppDbContext pgContext)
        {
            if (pgContext.Categories.Any() || pgContext.Users.Any())
                return;

            var candidatePaths = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "autotechhub.db"),
                Path.Combine(AppContext.BaseDirectory, "autotechhub.db")
            };

            var sqlitePath = candidatePaths.FirstOrDefault(File.Exists);
            if (string.IsNullOrEmpty(sqlitePath))
                return;

            var sqliteOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={sqlitePath}")
                .Options;

            using var sqliteContext = new AppDbContext(sqliteOptions);
            EnsureSqliteSchemaUpgraded(sqliteContext);

            if (!sqliteContext.Categories.Any() && !sqliteContext.Users.Any())
                return;

            var strategy = pgContext.Database.CreateExecutionStrategy();
            strategy.Execute(() =>
            {
                pgContext.ChangeTracker.Clear();
                using var tx = pgContext.Database.BeginTransaction();
                try
                {
                    var categories = sqliteContext.Categories.AsNoTracking().ToList();
                    var tags = sqliteContext.Tags.AsNoTracking().ToList();
                    var users = sqliteContext.Users.AsNoTracking().ToList();
                    var settings = sqliteContext.SiteSettings.AsNoTracking().ToList();
                    var articles = sqliteContext.Articles.AsNoTracking().ToList();
                    var articleTags = sqliteContext.ArticleTags.AsNoTracking().ToList();
                    var comments = sqliteContext.Comments.AsNoTracking().ToList();
                    var snippets = sqliteContext.CodeSnippets.AsNoTracking().ToList();
                    var ads = sqliteContext.Advertisements.AsNoTracking().ToList();
                    var products = sqliteContext.Products.AsNoTracking().ToList();
                    var inquiries = sqliteContext.ProductInquiries.AsNoTracking().ToList();
                    var webBlocks = sqliteContext.WebBlocks.AsNoTracking().ToList();
                    var mediaAssets = sqliteContext.MediaAssets.AsNoTracking().ToList();

                    // Remove placeholder default SiteSettings if created before migration completed
                    if (settings.Any() && pgContext.SiteSettings.Any())
                    {
                        pgContext.SiteSettings.RemoveRange(pgContext.SiteSettings.ToList());
                        pgContext.SaveChanges();
                    }

                    if (categories.Any()) pgContext.Categories.AddRange(categories);
                    if (tags.Any()) pgContext.Tags.AddRange(tags);
                    if (users.Any()) pgContext.Users.AddRange(users);
                    if (settings.Any()) pgContext.SiteSettings.AddRange(settings);
                    pgContext.SaveChanges();

                    if (articles.Any()) pgContext.Articles.AddRange(articles);
                    pgContext.SaveChanges();

                    if (articleTags.Any()) pgContext.ArticleTags.AddRange(articleTags);
                    if (comments.Any()) pgContext.Comments.AddRange(comments);
                    if (snippets.Any()) pgContext.CodeSnippets.AddRange(snippets);
                    if (ads.Any()) pgContext.Advertisements.AddRange(ads);
                    if (products.Any()) pgContext.Products.AddRange(products);
                    if (webBlocks.Any()) pgContext.WebBlocks.AddRange(webBlocks);
                    if (mediaAssets.Any()) pgContext.MediaAssets.AddRange(mediaAssets);
                    pgContext.SaveChanges();

                    if (inquiries.Any()) pgContext.ProductInquiries.AddRange(inquiries);
                    pgContext.SaveChanges();

                    var identityTables = new[]
                    {
                        "Categories", "Tags", "Users", "SiteSettings",
                        "Articles", "Comments", "CodeSnippets", "Advertisements",
                        "Products", "ProductInquiries", "WebBlocks", "MediaAssets"
                    };

                    foreach (var tbl in identityTables)
                    {
                        pgContext.Database.ExecuteSqlRaw(
                            $"SELECT setval(pg_get_serial_sequence('\"{tbl}\"', 'Id'), COALESCE((SELECT MAX(\"Id\") FROM \"{tbl}\"), 1), true);");
                    }

                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            });
        }

        private static string GetSeedPassword(string envKey, string fallback)
        {
            var val = Environment.GetEnvironmentVariable(envKey);
            return string.IsNullOrWhiteSpace(val) ? fallback : val.Trim();
        }

        private static void SeedUsers(AppDbContext context)
        {
            if (context.Users.Any())
                return;

            var adminPass = GetSeedPassword("SEED_ADMIN_PASSWORD", "AutoTech#Admin2026!");
            var managerPass = GetSeedPassword("SEED_MANAGER_PASSWORD", "AutoTech#Mgr2026!");
            var userPass = GetSeedPassword("SEED_USER_PASSWORD", "AutoTech#User2026!");

            var users = new List<AppUser>
            {
                new AppUser
                {
                    Username = "admin",
                    Email = "admin@autotechhub.vn",
                    FullName = "Quản Trị Viên (Admin)",
                    PasswordHash = PasswordHasherHelper.HashPassword(adminPass),
                    Role = UserRole.Admin,
                    Bio = "Quản trị viên hệ thống AutoTech Hub — Phê duyệt bài viết, phân quyền và quản lý nền tảng.",
                    IsActive = true,
                    MustChangePassword = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                },
                new AppUser
                {
                    Username = "manager",
                    Email = "manager@autotechhub.vn",
                    FullName = "Kỹ sư Quản lý (Manager)",
                    PasswordHash = PasswordHasherHelper.HashPassword(managerPass),
                    Role = UserRole.Manager,
                    Bio = "Biên tập viên & Kỹ sư Tự động hóa — Có quyền viết và đăng bài trực tiếp.",
                    IsActive = true,
                    MustChangePassword = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-20)
                },
                new AppUser
                {
                    Username = "user",
                    Email = "user@autotechhub.vn",
                    FullName = "Thành viên Cộng đồng",
                    PasswordHash = PasswordHasherHelper.HashPassword(userPass),
                    Role = UserRole.User,
                    Bio = "Thành viên đóng góp bài viết kỹ thuật chờ Admin phê duyệt.",
                    IsActive = true,
                    MustChangePassword = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                }
            };

            context.Users.AddRange(users);
            context.SaveChanges();
        }

        private static void RotateLegacyDemoPasswords(AppDbContext context)
        {
            var legacyMap = new Dictionary<string, (string OldPass, string NewPass, bool ForceChange)>(StringComparer.OrdinalIgnoreCase)
            {
                ["admin"] = ("Admin@123", GetSeedPassword("SEED_ADMIN_PASSWORD", "AutoTech#Admin2026!"), false),
                ["manager"] = ("Manager@123", GetSeedPassword("SEED_MANAGER_PASSWORD", "AutoTech#Mgr2026!"), true),
                ["user"] = ("User@123", GetSeedPassword("SEED_USER_PASSWORD", "AutoTech#User2026!"), true)
            };

            var users = context.Users
                .Where(u => u.Username == "admin" || u.Username == "manager" || u.Username == "user")
                .ToList();

            bool changed = false;
            foreach (var u in users)
            {
                if (legacyMap.TryGetValue(u.Username, out var rule) &&
                    PasswordHasherHelper.VerifyPassword(rule.OldPass, u.PasswordHash))
                {
                    u.PasswordHash = PasswordHasherHelper.HashPassword(rule.NewPass);
                    u.MustChangePassword = rule.ForceChange;
                    u.PasswordChangedAt = DateTime.UtcNow;
                    changed = true;
                }
            }

            if (changed)
            {
                context.SaveChanges();
            }
        }

        private static void SeedSiteSettings(AppDbContext context)
        {
            var existing = context.SiteSettings.OrderBy(s => s.Id).FirstOrDefault();
            if (existing != null)
            {
                bool updated = false;
                if (string.IsNullOrWhiteSpace(existing.SiteName))
                {
                    existing.SiteName = "AutoTechHub";
                    updated = true;
                }
                if (string.IsNullOrWhiteSpace(existing.SiteTagline))
                {
                    existing.SiteTagline = "Chia Sẻ Kiến Thức Tự Động Hóa";
                    updated = true;
                }
                if (string.IsNullOrWhiteSpace(existing.LogoUrl))
                {
                    existing.LogoUrl = "/images/logo-default.svg";
                    updated = true;
                }
                if (string.IsNullOrWhiteSpace(existing.FaviconUrl))
                {
                    existing.FaviconUrl = "/images/favicon.svg";
                    updated = true;
                }
                if (string.IsNullOrWhiteSpace(existing.PrimaryColor))
                {
                    existing.PrimaryColor = "#0d6efd";
                    updated = true;
                }
                if (string.IsNullOrWhiteSpace(existing.AccentColor))
                {
                    existing.AccentColor = "#0dcaf0";
                    updated = true;
                }
                if (string.IsNullOrWhiteSpace(existing.FontFamily))
                {
                    existing.FontFamily = "Segoe UI";
                    updated = true;
                }
                if (string.IsNullOrWhiteSpace(existing.DefaultTheme))
                {
                    existing.DefaultTheme = "auto";
                    updated = true;
                }
                if (string.IsNullOrWhiteSpace(existing.HeroBadgeText))
                {
                    existing.HeroBadgeText = "Kho Tri Thức & Giải Pháp Tự Động Hóa 4.0";
                    existing.HeroTitlePrefix = "Chia Sẻ Kiến Thức";
                    existing.HeroTitleHighlight = "Tự Động Hóa Thực Chiến";
                    existing.HeroSubtitle = "Khám phá các hướng dẫn chuyên sâu về PLC/SCADA, RPA Web & Desktop, Hệ thống IoT, và Thị giác máy tính AI từ các kỹ sư tự động hóa.";
                    existing.FeaturedSectionTitle = "Bài Viết Nổi Bật";
                    existing.CategoriesSectionTitle = "Chuyên Mục Tự Động Hóa";
                    existing.LatestSectionTitle = "Bài Viết Mới Cập Nhật";
                    existing.FooterDescription = "Nền tảng chia sẻ kiến thức, kinh nghiệm thực chiến và thư viện mã nguồn về Tự động hóa công nghiệp (PLC/SCADA/IoT), RPA, DevOps và AI Vision.";
                    existing.GithubUrl = "https://github.com";
                    existing.YoutubeUrl = "https://youtube.com";
                    existing.LinkedinUrl = "https://linkedin.com";
                    updated = true;
                }
                if (updated) context.SaveChanges();
                return;
            }

            context.SiteSettings.Add(new SiteSetting
            {
                EnableAdsModule = true,
                EnableShopModule = true,
                ShopNavTitle = "Sản phẩm",
                ShopBannerTitle = "Thiết Bị, Kit Thực Hành & Giải Pháp Tự Động Hóa",
                ShopBannerSubtitle = "Giới thiệu các bộ kit PLC/IoT, cảm biến công nghiệp và bộ mã nguồn tự động hóa sẵn sàng triển khai.",
                ContactPhone = "0900 123 456",
                ContactZalo = "https://zalo.me",
                ContactEmail = "sales@autotechhub.vn",
                SiteName = "AutoTechHub",
                SiteTagline = "Chia Sẻ Kiến Thức Tự Động Hóa",
                LogoUrl = "/images/logo-default.svg",
                FaviconUrl = "/images/favicon.svg",
                ShowBrandTextWithLogo = true,
                UpdatedAt = DateTime.UtcNow
            });
            context.SaveChanges();
        }

        private static void SeedAds(AppDbContext context)
        {
            if (context.Advertisements.Any())
                return;

            context.Advertisements.AddRange(new List<Advertisement>
            {
                new Advertisement
                {
                    Title = "Khóa Học Thực Chiến PLC Siemens S7-1200 & SCADA WinCC",
                    Description = "Làm chủ truyền thông công nghiệp Modbus TCP, S7 Protocol & kết nối C# SCADA từ con số 0.",
                    Placement = AdPlacement.Sidebar,
                    AdType = AdContentType.ImageBanner,
                    ImageUrl = "https://images.unsplash.com/photo-1581092160562-40aa08e78837?auto=format&fit=crop&w=800&q=80",
                    TargetUrl = "/Products",
                    CtaText = "Xem chi tiết khóa học",
                    OpenInNewTab = false,
                    IsActive = true,
                    DisplayOrder = 1,
                    CreatedAt = DateTime.UtcNow
                },
                new Advertisement
                {
                    Title = "Bộ Giải Pháp Giám Sát Nhà Xưởng IoT Gateway ESP32 / RS485",
                    Description = "Tích hợp sẵn MQTT, Dashboard Web theo dõi nhiệt độ - độ ẩm - điện năng 24/7.",
                    Placement = AdPlacement.InArticle,
                    AdType = AdContentType.ImageBanner,
                    ImageUrl = "https://images.unsplash.com/photo-1518770660439-4636190af475?auto=format&fit=crop&w=1000&q=80",
                    TargetUrl = "/Products",
                    CtaText = "Khám phá giải pháp",
                    OpenInNewTab = false,
                    IsActive = true,
                    DisplayOrder = 1,
                    CreatedAt = DateTime.UtcNow
                }
            });
            context.SaveChanges();
        }

        private static void SeedProducts(AppDbContext context, IMarkdownService markdownService)
        {
            if (context.Products.Any())
                return;

            var p1Desc = @"### Bộ Kit Thực Hành Lập Trình PLC Siemens S7-1200 & HMI
Bộ kit lắp ráp sẵn trên vali/khung nhôm định hình gọn nhẹ, phù hợp cho sinh viên và kỹ sư tự động hóa thực hành tại nhà hoặc đào tạo doanh nghiệp.

- **CPU**: Siemens S7-1200 DC/DC/DC (hoặc tương thích mở rộng IO)
- **Nguồn**: MeanWell 24VDC 2.5A công nghiệp
- **Tích hợp sẵn**: Nút nhấn Start/Stop, Emergency Stop, Công tắc chuyển mạch, Đèn báo 3 pha, Biến trở giả lập Analog 0-10V
- **Tặng kèm**: Bộ code mẫu C# S7.Net kết nối đọc/ghi thanh ghi và tài liệu hướng dẫn chi tiết";

            var p2Desc = @"### Module Thu Thập Dữ Liệu IoT ESP32 + RS485 Modbus RTU
Bo mạch tích hợp sẵn cách ly quang RS485, hỗ trợ đọc đồng thời đồng hồ điện năng, cảm biến nhiệt độ/độ ẩm SHT30/XY-MD02 và đẩy dữ liệu lên MQTT Broker hoặc Web API.

- **Vi điều khiển**: ESP32-WROOM-32 Dual Core WiFi/BLE
- **Chuẩn giao tiếp**: RS485 Modbus RTU, WiFi 2.4GHz, MQTT, HTTP REST
- **Nguồn cấp**: 12V - 24VDC công nghiệp
- **Firmware mẫu**: Cung cấp đầy đủ mã nguồn C++ Arduino/PlatformIO & ASP.NET Core Dashboard";

            var p3Desc = @"### Bộ Source Code SCADA & Web Dashboard C# .NET + SignalR
Giải pháp mã nguồn chuẩn doanh nghiệp giúp kỹ sư rút ngắn 80% thời gian phát triển phần mềm giám sát sản xuất (MES/SCADA mini).

- **Công nghệ**: ASP.NET Core MVC + SignalR Realtime + SQLite/SQL Server
- **Driver tích hợp**: Modbus TCP, Siemens S7.Net, Serial Port
- **Tính năng**: Biểu đồ Realtime, Xuất báo cáo Excel/CSV, Cảnh báo ngưỡng qua Telegram/Email";

            context.Products.AddRange(new List<Product>
            {
                new Product
                {
                    Name = "Vali Kit Thực Hành PLC Siemens S7-1200 & Mô Phỏng I/O",
                    Slug = "vali-kit-thuc-hanh-plc-siemens-s7-1200",
                    Sku = "ATH-PLC-001",
                    CategoryName = "Thiết bị PLC & Tự động hóa",
                    ShortDescription = "Bộ kit thực hành PLC công nghiệp tích hợp sẵn nguồn 24VDC, nút nhấn, đèn báo và biến trở Analog 0-10V.",
                    DescriptionMarkdown = p1Desc,
                    DescriptionHtml = markdownService.ToHtml(p1Desc),
                    Price = 6850000m,
                    OriginalPrice = 7500000m,
                    Currency = "VNĐ",
                    ImageUrl = "https://images.unsplash.com/photo-1581092160607-ee22621dd758?auto=format&fit=crop&w=900&q=80",
                    StockStatus = ProductStockStatus.InStock,
                    IsFeatured = true,
                    IsActive = true,
                    DisplayOrder = 1,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new Product
                {
                    Name = "Gateway IoT Công Nghiệp ESP32 RS485 Modbus RTU to MQTT",
                    Slug = "gateway-iot-cong-nghiep-esp32-rs485-mqtt",
                    Sku = "ATH-IOT-002",
                    CategoryName = "Cảm biến & IoT",
                    ShortDescription = "Bộ chuyển đổi thu thập dữ liệu cảm biến RS485 đẩy lên Cloud/MQTT Broker qua WiFi, nguồn 24VDC.",
                    DescriptionMarkdown = p2Desc,
                    DescriptionHtml = markdownService.ToHtml(p2Desc),
                    Price = 790000m,
                    OriginalPrice = 950000m,
                    Currency = "VNĐ",
                    ImageUrl = "https://images.unsplash.com/photo-1517077304055-6e89abbf09b0?auto=format&fit=crop&w=900&q=80",
                    StockStatus = ProductStockStatus.InStock,
                    IsFeatured = true,
                    IsActive = true,
                    DisplayOrder = 2,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new Product
                {
                    Name = "Bộ Source Code Web SCADA Realtime ASP.NET Core + SignalR",
                    Slug = "source-code-web-scada-realtime-aspnet-core",
                    Sku = "ATH-SW-003",
                    CategoryName = "Phần mềm & Source Code",
                    ShortDescription = "Mã nguồn đầy đủ hệ thống giám sát thiết bị công nghiệp thời gian thực, hỗ trợ Modbus TCP & S7.",
                    DescriptionMarkdown = p3Desc,
                    DescriptionHtml = markdownService.ToHtml(p3Desc),
                    Price = 1500000m,
                    OriginalPrice = null,
                    Currency = "VNĐ",
                    ImageUrl = "https://images.unsplash.com/photo-1518770660439-4636190af475?auto=format&fit=crop&w=900&q=80",
                    StockStatus = ProductStockStatus.InStock,
                    IsFeatured = true,
                    IsActive = true,
                    DisplayOrder = 3,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            });
            context.SaveChanges();
        }

        private static void SeedBlogContent(AppDbContext context, IMarkdownService markdownService)
        {

            // 1. Seed Categories
            var categories = new List<Category>
            {
                new Category
                {
                    Name = "Tự động hóa Công nghiệp",
                    Slug = "industrial-automation",
                    Description = "PLC Siemens, Mitsubishi, Omron, SCADA, HMI, Modbus RTU/TCP, OPC UA và mạng truyền thông công nghiệp.",
                    Icon = "bi-gear-wide-connected",
                    BadgeColor = "primary",
                    DisplayOrder = 1
                },
                new Category
                {
                    Name = "Tự động hóa Phần mềm & RPA",
                    Slug = "software-rpa",
                    Description = "Playwright, Selenium, C# Bot, Python Scripts, tự động hóa quy trình nghiệp vụ và thu thập dữ liệu web.",
                    Icon = "bi-robot",
                    BadgeColor = "success",
                    DisplayOrder = 2
                },
                new Category
                {
                    Name = "IoT & Hệ thống Nhúng",
                    Slug = "iot-embedded",
                    Description = "ESP32, STM32, Arduino, Raspberry Pi, MQTT Broker, Dashboard giám sát thời gian thực và cảm biến.",
                    Icon = "bi-cpu",
                    BadgeColor = "warning",
                    DisplayOrder = 3
                },
                new Category
                {
                    Name = "DevOps & CI/CD",
                    Slug = "devops-cicd",
                    Description = "Docker, GitHub Actions, Jenkins, tự động hóa đóng gói, kiểm thử và triển khai ứng dụng.",
                    Icon = "bi-terminal-split",
                    BadgeColor = "info",
                    DisplayOrder = 4
                },
                new Category
                {
                    Name = "AI & Thị giác Máy tính",
                    Slug = "ai-vision",
                    Description = "OpenCV, YOLO, AI Agents và các giải pháp trí tuệ nhân tạo nhận diện sản phẩm trong dây chuyền sản xuất.",
                    Icon = "bi-eye",
                    BadgeColor = "danger",
                    DisplayOrder = 5
                }
            };

            context.Categories.AddRange(categories);
            context.SaveChanges();

            // 2. Seed Tags
            var tags = new List<Tag>
            {
                new Tag { Name = "PLC Siemens", Slug = "plc-siemens" },
                new Tag { Name = "S7-1200", Slug = "s7-1200" },
                new Tag { Name = "Modbus TCP", Slug = "modbus-tcp" },
                new Tag { Name = "C# .NET", Slug = "csharp-dotnet" },
                new Tag { Name = "Playwright", Slug = "playwright" },
                new Tag { Name = "RPA", Slug = "rpa" },
                new Tag { Name = "ESP32", Slug = "esp32" },
                new Tag { Name = "MQTT", Slug = "mqtt" },
                new Tag { Name = "Python", Slug = "python" },
                new Tag { Name = "OpenCV", Slug = "opencv" },
                new Tag { Name = "Docker", Slug = "docker" },
                new Tag { Name = "CI/CD", Slug = "cicd" }
            };

            context.Tags.AddRange(tags);
            context.SaveChanges();

            var tagMap = tags.ToDictionary(t => t.Slug, t => t);

            // 3. Seed Articles
            var catIndustrial = categories.First(c => c.Slug == "industrial-automation");
            var catRpa = categories.First(c => c.Slug == "software-rpa");
            var catIot = categories.First(c => c.Slug == "iot-embedded");
            var catDevOps = categories.First(c => c.Slug == "devops-cicd");
            var catAiVision = categories.First(c => c.Slug == "ai-vision");

            var article1Content = @"# Hướng Dẫn Lập Trình Giao Tiếp PLC Siemens S7-1200 Bằng C# .NET Qua S7.Net

Trong các hệ thống tự động hóa công nghiệp hiện đại, việc thu thập dữ liệu từ PLC (Programmable Logic Controller) lên máy tính cấp SCADA/MES hoặc Web Dashboard là một yêu cầu cực kỳ phổ biến. Bài viết này sẽ hướng dẫn bạn từng bước cách kết nối và đọc/ghi dữ liệu từ **PLC Siemens S7-1200** bằng ngôn ngữ **C# .NET** sử dụng thư viện mã nguồn mở **S7.Net Plus**.

---

## 1. Chuẩn Bị & Cấu Hình Trên TIA Portal

Trước khi lập trình C#, bạn cần bật chế độ cho phép truy cập PUT/GET trên PLC S7-1200 trong TIA Portal:

1. Mở phần cứng PLC trong **TIA Portal** -> Nhấp đúp vào PLC -> Chọn tab **Properties**.
2. Tìm đến mục **Protection & Security** -> **Connection mechanisms**.
3. Tích chọn vào ô: **""Permit access with PUT/GET communication from remote partner""**.
4. Đối với Data Block (DB) mà bạn muốn đọc/ghi, nhấp chuột phải vào DB -> **Properties** -> Tab **Attributes** -> **Bỏ chọn** mục *""Optimized block access""* (chuyển sang dạng Non-optimized Standard DB để có địa chỉ Offset cố định).
5. Compile và Download lại xuống PLC.

> [!IMPORTANT]
> Nếu bạn không tắt *Optimized block access* hoặc quên tích *Permit access with PUT/GET*, C# sẽ báo lỗi Timeout hoặc Connection Refused khi cố gắng đọc dữ liệu!

---

## 2. Cài Đặt Thư Viện S7.Net Trên .NET

Mở Terminal của dự án .NET và cài đặt gói NuGet:

```bash
dotnet add package S7netplus
```

---

## 3. Viết Mã Nguồn C# Kết Nối PLC

Dưới đây là lớp `SiemensPlcManager` được thiết kế theo dạng dùng async/await và quản lý kết nối an toàn:

```csharp
using System;
using System.Threading.Tasks;
using S7.Net;

public class SiemensPlcManager : IDisposable
{
    private readonly Plc _plc;

    public SiemensPlcManager(string ipAddress, CpuType cpu = CpuType.S71200, short rack = 0, short slot = 1)
    {
        _plc = new Plc(cpu, ipAddress, rack, slot);
    }

    public async Task<bool> ConnectAsync()
    {
        try
        {
            await _plc.OpenAsync();
            Console.WriteLine($""[SUCCESS] Đã kết nối thành công tới PLC Siemens: {_plc.IP}"");
            return _plc.IsConnected;
        }
        catch (Exception ex)
        {
            Console.WriteLine($""[ERROR] Lỗi kết nối PLC: {ex.Message}"");
            return false;
        }
    }

    // Đọc một giá trị Real (Float) từ DB1 tại byte offset 0 (DB1.DBD0)
    public async Task<float?> ReadTemperatureAsync(int dbNumber = 1, int startByteAdr = 0)
    {
        if (!_plc.IsConnected) return null;

        try
        {
            // S7-1200 Real = 4 bytes (Float)
            var result = await _plc.ReadAsync(DataType.DataBlock, dbNumber, startByteAdr, VarType.Real, 1);
            return (float)result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($""Lỗi đọc dữ liệu: {ex.Message}"");
            return null;
        }
    }

    // Ghi một giá trị Boolean vào biến cờ hoặc DataBlock (DB1.DBX4.0)
    public async Task<bool> SetMotorRunStatusAsync(int dbNumber, int byteAdr, int bitAdr, bool value)
    {
        if (!_plc.IsConnected) return false;

        try
        {
            await _plc.WriteBitAsync(DataType.DataBlock, dbNumber, byteAdr, bitAdr, value);
            Console.WriteLine($""Đã ghi trạng thái Motor = {value} vào DB{dbNumber}.DBX{byteAdr}.{bitAdr}"");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($""Lỗi ghi dữ liệu: {ex.Message}"");
            return false;
        }
    }

    public void Dispose()
    {
        if (_plc != null && _plc.IsConnected)
        {
            _plc.Close();
        }
    }
}
```

---

## 4. Cách Sử Dụng Trong Ứng Dụng Console Hoặc Web API

```csharp
class Program
{
    static async Task Main(string[] args)
    {
        using var plc = new SiemensPlcManager(""192.168.0.10"");
        
        if (await plc.ConnectAsync())
        {
            // Đọc nhiệt độ lò nhiệt từ DB1.DBD0
            var temp = await plc.ReadTemperatureAsync(dbNumber: 1, startByteAdr: 0);
            Console.WriteLine($""Nhiệt độ hiện tại: {temp:F2} °C"");

            // Kích hoạt biến chạy bơm giải nhiệt DB1.DBX4.0 = true
            if (temp > 75.0f)
            {
                await plc.SetMotorRunStatusAsync(dbNumber: 1, byteAdr: 4, bitAdr: 0, value: true);
            }
        }
    }
}
```

---

## 5. Kinh Nghiệm Thực Tế Khi Triển Khai Trong Nhà Máy

- **Polling Rate hợp lý**: Không nên đọc vòng lặp với chu kỳ dưới 50ms nếu hệ thống mạng Ethernet qua nhiều switch công nghiệp không chuyên dụng. Chu kỳ 100ms - 500ms là tối ưu cho việc giám sát SCADA/Web.
- **Xử lý mất kết nối (Auto Reconnect)**: Luôn kiểm tra thuộc tính `IsConnected` và có cơ chế `Heartbeat` tự động kết nối lại sau mỗi 3 - 5 giây nếu đứt cáp mạng.
- **Đọc hàng loạt (Bulk Read)**: Thay vì đọc từng biến đơn lẻ, hãy gom các biến vào một `Class` hoặc `Struct` và dùng `_plc.ReadClassAsync(myObject, dbNumber)` để đọc 1 lần toàn bộ dữ liệu chỉ trong một frame TCP.

Chúc các bạn tích hợp thành công PLC Siemens vào ứng dụng .NET!";

            var article2Content = @"# Tự Động Hóa Web & Thu Thập Dữ Liệu Bằng Microsoft Playwright Với C# .NET

Trong thế giới tự động hóa phần mềm (Software Automation) và kiểm thử chất lượng (QA), **Microsoft Playwright** đang dần trở thành tiêu chuẩn mới thay thế Selenium nhờ tốc độ vượt trội, cơ chế Auto-waiting thông minh và khả năng hỗ trợ đa trình duyệt (Chromium, Firefox, WebKit) nguyên bản.

Bài viết này sẽ hướng dẫn bạn cách xây dựng một ứng dụng C# .NET tự động hóa các tác vụ lặp đi lặp lại trên trình duyệt và xuất báo cáo tự động.

---

## 1. Tại Sao Nên Dùng Playwright Thay Vì Selenium?

| Tính năng | Playwright | Selenium truyền thống |
| :--- | :--- | :--- |
| **Auto-wait** | Tự động chờ phần tử hiển thị / sẵn sàng click | Phải dùng `WebDriverWait` thủ công |
| **Tốc độ thực thi** | Rất nhanh nhờ kiến trúc WebSocket & DevTools Protocol | Chậm hơn do qua trung gian WebDriver HTTP |
| **Chụp ảnh & Video** | Tích hợp sẵn tracing, screenshot, video recording | Cần cấu hình phức tạp |
| **Hỗ trợ Headless** | Cực kỳ mượt mà, hỗ trợ cả browser context cô lập | Tốn tài nguyên hơn |

---

## 2. Khởi Tạo Dự Án & Cài Đặt Package

Tạo một project .NET Console:

```bash
dotnet new console -n WebAutomationBot
cd WebAutomationBot
dotnet add package Microsoft.Playwright
dotnet build
```

Sau khi build, hãy cài đặt các trình duyệt đi kèm của Playwright:

```powershell
powershell -Command ""& bin/Debug/net5.0/playwright.ps1 install""
```

---

## 3. Kịch Bản Tự Động Hóa Đăng Nhập & Tải Báo Cáo

Dưới đây là đoạn mã hoàn chỉnh thực hiện:
1. Mở trình duyệt Chromium.
2. Điều hướng đến trang quản trị nội bộ.
3. Tự động điền tài khoản, mật khẩu và đăng nhập.
4. Chờ trang tải, lọc báo cáo sản xuất theo ngày hôm nay.
5. Chụp ảnh màn hình bằng chứng và trích xuất dữ liệu bảng về dạng CSV.

```csharp
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright;

class WebBot
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine(""Khởi động Playwright Web Automation Bot..."");

        using var playwright = await Playwright.CreateAsync();
        
        // Khởi động Chromium (Headless = false để quan sát giao diện)
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false,
            SlowMo = 300 // Làm chậm 300ms giữa các thao tác để dễ quan sát
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        var page = await context.NewPageAsync();

        // 1. Điều hướng tới trang web
        Console.WriteLine(""Điều hướng tới trang hệ thống..."");
        await page.GotoAsync(""https://quotes.toscrape.com/login"");

        // 2. Điền form đăng nhập với cơ chế tự động tìm selector
        Console.WriteLine(""Điền thông tin đăng nhập..."");
        await page.FillAsync(""input#username"", ""automation_engineer"");
        await page.FillAsync(""input#password"", ""SecurePassword123!"");

        // 3. Click nút đăng nhập và chờ trang chuyển đổi
        await page.ClickAsync(""input[type='submit']"");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // 4. Chụp ảnh màn hình làm bằng chứng
        Directory.CreateDirectory(""Screenshots"");
        var screenshotPath = Path.Combine(""Screenshots"", $""dashboard_{DateTime.Now:yyyyMMdd_HHmmss}.png"");
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });
        Console.WriteLine($""Đã lưu ảnh chụp màn hình tại: {screenshotPath}"");

        // 5. Trích xuất dữ liệu danh sách trích dẫn / đơn hàng
        var items = await page.QuerySelectorAllAsync("".quote"");
        Console.WriteLine($""Tìm thấy {items.Count} mục dữ liệu trên trang."");

        foreach (var item in items)
        {
            var textElement = await item.QuerySelectorAsync("".text"");
            var authorElement = await item.QuerySelectorAsync("".author"");

            var text = textElement != null ? await textElement.InnerTextAsync() : """";
            var author = authorElement != null ? await authorElement.InnerTextAsync() : """";

            Console.WriteLine($""- [{author}]: {text}"");
        }

        Console.WriteLine(""Hoàn thành kịch bản tự động hóa!"");
    }
}
```

---

## 4. Tự Động Hóa Với Headless Chạy Định Kỳ (Cron Job)

Khi triển khai lên Server hoặc Docker Container, bạn chỉ cần chuyển cấu hình:

```csharp
Headless = true
```

Và kết hợp với **Windows Task Scheduler** hoặc **Worker Service** để bot tự động chạy mỗi sáng lúc 06:00 AM, lấy số liệu sản xuất từ phần mềm ERP và gửi email báo cáo cho ban giám đốc.

Một giải pháp tiết kiệm hàng chục giờ nhập liệu thủ công mỗi tháng!";

            var article3Content = @"# Xây Dựng Hệ Thống IoT Giám Sát Nhiệt Độ Nhà Xưởng Với ESP32, MQTT Broker Và ASP.NET Core

Giám sát điều kiện môi trường như nhiệt độ, độ ẩm, áp suất trong nhà máy và kho lạnh là một bài toán then chốt trong tự động hóa công nghiệp và nông nghiệp công nghệ cao. Trong bài viết này, chúng ta sẽ xây dựng một giải pháp IoT hoàn chỉnh khép kín từ phần cứng **ESP32**, truyền thông qua **MQTT Broker (Mosquitto)** và hiển thị lên **Web Dashboard bằng ASP.NET Core**.

---

## 1. Sơ Đồ Kiến Trúc Hệ Thống (Architecture)

```
+-------------------+        WiFi / MQTT         +------------------+
|   ESP32 Node      | ------------------------> |   Mosquitto      |
| (Sensor DHT22/DS) |   Topic: factory/temp1    |   MQTT Broker    |
+-------------------+                           +------------------+
                                                         |
                                                  MQTT Subscriber
                                                         v
                                                +------------------+
                                                |  ASP.NET Core    |
                                                |  Backend & UI    |
                                                | (SignalR WebApp) |
                                                +------------------+
```

---

## 2. Lập Trình Firmware ESP32 (Arduino C++)

Đoạn mã dưới đây nạp vào vi điều khiển **ESP32** để đọc cảm biến và gửi gói tin JSON định kỳ 2 giây/lần:

```cpp
#include <WiFi.h>
#include <PubSubClient.h>
#include <ArduinoJson.h>
#include ""DHT.h""

#define DHTPIN 4
#define DHTTYPE DHT22
DHT dht(DHTPIN, DHTTYPE);

const char* ssid = ""Factory_WiFi_AP"";
const char* password = ""IndustrialPass2026"";
const char* mqtt_server = ""192.168.1.100"";
const int mqtt_port = 1883;

WiFiClient espClient;
PubSubClient client(espClient);
unsigned long lastMsg = 0;

void setup_wifi() {
  delay(10);
  Serial.println(""Connecting to WiFi..."");
  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(""."");
  }
  Serial.println(""\nWiFi connected. IP: "");
  Serial.println(WiFi.localIP());
}

void reconnect() {
  while (!client.connected()) {
    Serial.print(""Connecting to MQTT broker..."");
    if (client.connect(""ESP32_Node_ZoneA"")) {
      Serial.println(""Connected!"");
    } else {
      Serial.print(""Failed, rc="");
      Serial.print(client.state());
      delay(5000);
    }
  }
}

void setup() {
  Serial.begin(115200);
  dht.begin();
  setup_wifi();
  client.setServer(mqtt_server, mqtt_port);
}

void loop() {
  if (!client.connected()) {
    reconnect();
  }
  client.loop();

  unsigned long now = millis();
  if (now - lastMsg > 3000) { // Gửi mỗi 3 giây
    lastMsg = now;
    
    float h = dht.readHumidity();
    float t = dht.readTemperature();

    if (isnan(h) || isnan(t)) {
      Serial.println(""Lỗi đọc cảm biến DHT22!"");
      return;
    }

    StaticJsonDocument<200> doc;
    doc[""deviceId""] = ""ESP32-ZONE-A1"";
    doc[""temperature""] = t;
    doc[""humidity""] = h;
    doc[""rssi""] = WiFi.RSSI();

    char buffer[256];
    serializeJson(doc, buffer);
    
    client.publish(""factory/sensors/zone-a"", buffer);
    Serial.printf(""Published: Temp=%.2f C, Hum=%.2f %%\n"", t, h);
  }
}
```

---

## 3. Xây Dựng Background Service Nhận MQTT Trong ASP.NET Core

Sử dụng thư viện `MQTTnet` trong C# để duy trì kết nối lắng nghe:

```bash
dotnet add package MQTTnet
```

```csharp
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Client;

public class MqttConsumerService : BackgroundService
{
    private IMqttClient _mqttClient;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(""192.168.1.100"", 1883)
            .WithClientId(""AspNetCore_Telemetry_Listener"")
            .Build();

        _mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            
            Console.WriteLine($""[MQTT NHẬN ĐƯỢC] Topic: {topic} | Data: {payload}"");
            
            // Xử lý lưu database hoặc push thời gian thực lên SignalR Web Dashboard
            return Task.CompletedTask;
        };

        await _mqttClient.ConnectAsync(options, stoppingToken);
        await _mqttClient.SubscribeAsync(""factory/sensors/#"");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

---

## 4. Kết Luận

Với kiến trúc này, hệ thống có thể mở rộng từ 1 cảm biến lên hàng trăm thiết bị IoT trong nhà máy một cách tin cậy, chi phí phần cứng thấp và quản lý tập trung trên nền tảng .NET!";

            var article4Content = @"# Ứng Dụng Computer Vision (OpenCV) Để Kiểm Tra Lỗi Sản Phẩm Trên Băng Chuyền Tự Động

Trong dây chuyền sản xuất tự động hóa, khâu kiểm tra ngoại quan (Visual Quality Inspection) đóng vai trò sống còn để loại bỏ sản phẩm lỗi trước khi đóng gói. Trước đây việc này cần nhân công soi lỗi thủ công, dễ mỏi mắt và sai sót.

Bài viết này giới thiệu cách sử dụng **OpenCV** kết hợp cùng thuật toán lọc ngưỡng màu sắc và phân tích Contour để phát hiện nắp chai bị kênh hoặc thiếu nắp trên băng chuyền.

---

## 1. Nguyên Lý Xử Lý Ảnh Công Nghiệp

Quy trình phát hiện cơ bản bao gồm 5 bước:
1. **Thu nhận ảnh (Image Acquisition)**: Camera công nghiệp chụp khi cảm biến quang phát hiện chai đến vị trí.
2. **Tiền xử lý (Preprocessing)**: Chuyển ảnh sang không gian màu HSV và làm mịn với Gaussian Blur để khử nhiễu ánh sáng.
3. **Lọc mặt nạ (Color Masking / Thresholding)**: Tách riêng vùng màu của nắp chai (ví dụ: nắp màu đỏ hoặc xanh dương).
4. **Tìm Contour (Contour Detection)**: Xác định diện tích và tọa độ tâm `(x, y)` của nắp.
5. **Đưa ra quyết định (Decision & PLC Trigger)**: Nếu diện tích `< Threshold` hoặc lệch tọa độ `> 10px`, gửi tín hiệu kích hoạt xi lanh khí nén thổi chai lỗi ra khỏi băng chuyền!

---

## 2. Mã Nguồn Xử Lý Bằng Python & OpenCV

```python
import cv2
import numpy as np

def inspect_bottle_cap(frame):
    # 1. Chuyển đổi sang không gian màu HSV
    hsv = cv2.cvtColor(frame, cv2.COLOR_BGR2HSV)

    # 2. Dải màu nắp chai màu đỏ
    lower_red1 = np.array([0, 100, 100])
    upper_red1 = np.array([10, 255, 255])
    lower_red2 = np.array([160, 100, 100])
    upper_red2 = np.array([179, 255, 255])

    mask1 = cv2.inRange(hsv, lower_red1, upper_red1)
    mask2 = cv2.inRange(hsv, lower_red2, upper_red2)
    mask = cv2.bitwise_or(mask1, mask2)

    # Khử nhiễu
    kernel = np.ones((5, 5), np.uint8)
    mask = cv2.morphologyEx(mask, cv2.MORPH_OPEN, kernel)
    mask = cv2.morphologyEx(mask, cv2.MORPH_CLOSE, kernel)

    # 3. Tìm các đường bao (Contours)
    contours, _ = cv2.findContours(mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)

    passed = False
    details = ""Không tìm thấy nắp chai (NG - FAIL)""

    for cnt in contours:
        area = cv2.contourArea(cnt)
        if area > 1200: # Diện tích chuẩn của nắp chai
            x, y, w, h = cv2.boundingRect(cnt)
            aspect_ratio = float(w) / h
            
            # Tỷ lệ dài/rộng của nắp chuẩn khoảng 1.8 - 2.5
            if 1.5 < aspect_ratio < 3.0:
                passed = True
                details = f""Nắp chuẩn (OK) - Diện tích: {area:.0f}px""
                cv2.rectangle(frame, (x, y), (x + w, y + h), (0, 255, 0), 2)
            else:
                details = f""Nắp bị kênh/nghiêng (NG) - Aspect: {aspect_ratio:.2f}""
                cv2.rectangle(frame, (x, y), (x + w, y + h), (0, 0, 255), 2)
            break

    # Vẽ kết quả lên hình ảnh
    status_text = ""PASS [OK]"" if passed else ""REJECT [NG]""
    color = (0, 255, 0) if passed else (0, 0, 255)
    cv2.putText(frame, status_text, (20, 50), cv2.FONT_HERSHEY_SIMPLEX, 1.2, color, 3)
    cv2.putText(frame, details, (20, 90), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (255, 255, 255), 2)

    return frame, passed

# Chạy thử nghiệm với Camera
cap = cv2.VideoCapture(0)
while cap.isOpened():
    ret, frame = cap.read()
    if not ret: break

    result_frame, is_ok = inspect_bottle_cap(frame)
    cv2.imshow(""Industrial Vision Inspection"", result_frame)

    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

cap.release()
cv2.destroyAllWindows()
```

---

## 3. Tích Hợp Giao Tiếp Với PLC Đẩy Sản Phẩm Lỗi

Khi hàm `inspect_bottle_cap()` trả về `is_ok = False`, hệ thống phần mềm sẽ gửi lệnh qua **Modbus TCP** hoặc **S7 protocol** tới PLC:

```csharp
if (!isOk)
{
    // Kích hoạt ngõ ra Q0.0 trong 200ms để van khí nén đẩy chai NG sang thùng phế
    await plc.TriggerEjectorPistonAsync();
}
```

Đây là mô hình tiêu chuẩn đang được áp dụng trong hàng nghìn nhà máy đóng chai và thực phẩm hiện đại!";

            var articles = new List<Article>
            {
                new Article
                {
                    Title = "Hướng Dẫn Lập Trình Giao Tiếp PLC Siemens S7-1200 Bằng C# .NET Qua S7.Net",
                    Slug = "huong-dan-lap-trinh-giao-tiep-plc-siemens-s7-1200-bang-csharp-net",
                    Summary = "Chi tiết cách kết nối, đọc thanh ghi Data Block (DB) và ghi trạng thái điều khiển PLC Siemens S7-1200 từ ứng dụng C# .NET với thư viện S7NetPlus an toàn và tốc độ cao.",
                    ContentMarkdown = article1Content,
                    ContentHtml = markdownService.ToHtml(article1Content),
                    ThumbnailUrl = "https://images.unsplash.com/photo-1581092160607-ee22621dd758?auto=format&fit=crop&w=800&q=80",
                    AuthorName = "Nguyễn Văn Tuấn (Automation Lead)",
                    AuthorEmail = "tuan.automation@example.com",
                    ReadingTimeMinutes = 6,
                    ViewCount = 342,
                    LikeCount = 48,
                    IsFeatured = true,
                    IsPublished = true,
                    CategoryId = catIndustrial.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new Article
                {
                    Title = "Tự Động Hóa Web & Thu Thập Dữ Liệu Bằng Microsoft Playwright Với C# .NET",
                    Slug = "tu-dong-hoa-web-thu-thap-du-lieu-playwright-csharp-net",
                    Summary = "Khám phá cách xây dựng Bot tự động hóa quy trình nghiệp vụ (RPA) bằng Playwright & C# .NET để tự động đăng nhập, cào số liệu và xuất báo cáo không cần thao tác người dùng.",
                    ContentMarkdown = article2Content,
                    ContentHtml = markdownService.ToHtml(article2Content),
                    ThumbnailUrl = "https://images.unsplash.com/photo-1518770660439-4636190af475?auto=format&fit=crop&w=800&q=80",
                    AuthorName = "Lê Hoàng Nam (RPA Architect)",
                    AuthorEmail = "nam.le@example.com",
                    ReadingTimeMinutes = 5,
                    ViewCount = 285,
                    LikeCount = 39,
                    IsFeatured = true,
                    IsPublished = true,
                    CategoryId = catRpa.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new Article
                {
                    Title = "Xây Dựng Hệ Thống IoT Giám Sát Nhiệt Độ Nhà Xưởng Với ESP32, MQTT Broker Và ASP.NET Core",
                    Slug = "xay-dung-he-thong-iot-giam-sat-nhiet-do-esp32-mqtt-aspnet-core",
                    Summary = "Hướng dẫn lập trình phần cứng ESP32 đọc cảm biến DHT22 truyền qua MQTT Broker và xây dựng Background Worker Service trên ASP.NET Core để giám sát thời gian thực.",
                    ContentMarkdown = article3Content,
                    ContentHtml = markdownService.ToHtml(article3Content),
                    ThumbnailUrl = "https://images.unsplash.com/photo-1517077304055-6e89abbf09b0?auto=format&fit=crop&w=800&q=80",
                    AuthorName = "Trần Minh Quang (IoT Specialist)",
                    AuthorEmail = "quang.tran@example.com",
                    ReadingTimeMinutes = 7,
                    ViewCount = 412,
                    LikeCount = 65,
                    IsFeatured = true,
                    IsPublished = true,
                    CategoryId = catIot.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new Article
                {
                    Title = "Ứng Dụng Computer Vision (OpenCV) Để Kiểm Tra Lỗi Sản Phẩm Trên Băng Chuyền Tự Động",
                    Slug = "ung-dung-computer-vision-opencv-kiem-tra-loi-san-pham-bang-chuyen",
                    Summary = "Giải pháp kiểm tra ngoại quan (Visual Inspection) tự động bằng OpenCV, phân tích contour nắp chai và tích hợp tín hiệu điều khiển van khí nén loại bỏ sản phẩm lỗi.",
                    ContentMarkdown = article4Content,
                    ContentHtml = markdownService.ToHtml(article4Content),
                    ThumbnailUrl = "https://images.unsplash.com/photo-1531297484001-80022131f5a1?auto=format&fit=crop&w=800&q=80",
                    AuthorName = "Phạm Đức Anh (AI Vision Eng)",
                    AuthorEmail = "anh.pham@example.com",
                    ReadingTimeMinutes = 6,
                    ViewCount = 198,
                    LikeCount = 31,
                    IsFeatured = false,
                    IsPublished = true,
                    CategoryId = catAiVision.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            };

            context.Articles.AddRange(articles);
            context.SaveChanges();

            // Link Article Tags
            var a1 = articles[0];
            var a2 = articles[1];
            var a3 = articles[2];
            var a4 = articles[3];

            context.ArticleTags.AddRange(new List<ArticleTag>
            {
                new ArticleTag { ArticleId = a1.Id, TagId = tagMap["plc-siemens"].Id },
                new ArticleTag { ArticleId = a1.Id, TagId = tagMap["s7-1200"].Id },
                new ArticleTag { ArticleId = a1.Id, TagId = tagMap["csharp-dotnet"].Id },
                new ArticleTag { ArticleId = a1.Id, TagId = tagMap["modbus-tcp"].Id },

                new ArticleTag { ArticleId = a2.Id, TagId = tagMap["playwright"].Id },
                new ArticleTag { ArticleId = a2.Id, TagId = tagMap["rpa"].Id },
                new ArticleTag { ArticleId = a2.Id, TagId = tagMap["csharp-dotnet"].Id },

                new ArticleTag { ArticleId = a3.Id, TagId = tagMap["esp32"].Id },
                new ArticleTag { ArticleId = a3.Id, TagId = tagMap["mqtt"].Id },
                new ArticleTag { ArticleId = a3.Id, TagId = tagMap["csharp-dotnet"].Id },

                new ArticleTag { ArticleId = a4.Id, TagId = tagMap["opencv"].Id },
                new ArticleTag { ArticleId = a4.Id, TagId = tagMap["python"].Id }
            });

            // 4. Seed Comments
            context.Comments.AddRange(new List<Comment>
            {
                new Comment
                {
                    ArticleId = a1.Id,
                    AuthorName = "Kỹ sư Bảo trì",
                    AuthorEmail = "maintenance@factory.vn",
                    Content = "Bài viết rất chi tiết và đúng thực tế! Phần lưu ý bỏ Optimized block access trên TIA Portal rất nhiều người mới làm hay bị mắc phải.",
                    CreatedAt = DateTime.UtcNow.AddDays(-4),
                    IsApproved = true
                },
                new Comment
                {
                    ArticleId = a1.Id,
                    AuthorName = "Hoàng Long",
                    AuthorEmail = "long.h@gmail.com",
                    Content = "Anh cho em hỏi nếu PLC đặt pass Protection thì cấu hình S7NetPlus như thế nào ạ?",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    IsApproved = true
                },
                new Comment
                {
                    ArticleId = a3.Id,
                    AuthorName = "Thanh Tùng",
                    AuthorEmail = "tung.iot@outlook.com",
                    Content = "Mô hình ESP32 + MQTT + ASP.NET Core này chạy rất ổn định trong xưởng may của mình, đã test 3 tháng liên tục không bị drop.",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    IsApproved = true
                }
            });

            // 5. Seed Code Snippets
            context.CodeSnippets.AddRange(new List<CodeSnippet>
            {
                new CodeSnippet
                {
                    Title = "Đọc dữ liệu Modbus TCP Holding Registers (C# NModbus)",
                    Description = "Hàm C# kết nối Modbus TCP Master để đọc 10 thanh ghi Holding Register từ slave address 1.",
                    Language = "csharp",
                    CategoryTag = "Industrial",
                    Author = "AutoTech Team",
                    CodeContent = @"using System.Net.Sockets;
using NModbus;

public async Task<ushort[]> ReadModbusRegistersAsync(string ip, int port = 502, byte slaveId = 1, ushort startAddress = 0, ushort numRegisters = 10)
{
    using var client = new TcpClient();
    await client.ConnectAsync(ip, port);
    
    var factory = new ModbusFactory();
    var master = factory.CreateMaster(client);
    
    // Đọc Holding Registers (Function Code 03)
    ushort[] registers = await master.ReadHoldingRegistersAsync(slaveId, startAddress, numRegisters);
    return registers;
}",
                    CreatedAt = DateTime.UtcNow.AddDays(-6)
                },
                new CodeSnippet
                {
                    Title = "ESP32 Auto Reconnect WiFi & MQTT Keepalive (C++)",
                    Description = "Hàm chuẩn kiểm tra trạng thái WiFi và tự động kết nối lại không làm treo tác vụ điều khiển.",
                    Language = "cpp",
                    CategoryTag = "IoT",
                    Author = "IoT Maker",
                    CodeContent = @"void checkAndReconnectNetwork() {
  if (WiFi.status() != WL_CONNECTED) {
    Serial.println(""WiFi mất kết nối, đang thử lại..."");
    WiFi.disconnect();
    WiFi.reconnect();
    int timeout = 0;
    while (WiFi.status() != WL_CONNECTED && timeout < 20) {
      delay(500);
      timeout++;
    }
  }

  if (WiFi.status() == WL_CONNECTED && !mqttClient.connected()) {
    if (mqttClient.connect(""ESP32_AutoNode"")) {
      mqttClient.subscribe(""command/control/#"");
    }
  }
}",
                    CreatedAt = DateTime.UtcNow.AddDays(-4)
                },
                new CodeSnippet
                {
                    Title = "Python Watchdog Giám Sát Thư Mục & Tự Động Xử Lý File (Python)",
                    Description = "Script tự động lắng nghe khi có file Excel/CSV mới xuất từ máy CNC/PLC và kích hoạt xử lý.",
                    Language = "python",
                    CategoryTag = "RPA",
                    Author = "DevOps Auto",
                    CodeContent = @"import time
from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler

class ProductionFileHandler(FileSystemEventHandler):
    def on_created(self, event):
        if not event.is_directory and event.src_path.endswith('.csv'):
            print(f""Phát hiện file dữ liệu mới: {event.src_path}"")
            # Kích hoạt xử lý số liệu tự động
            process_production_log(event.src_path)

def process_production_log(filepath):
    print(f""Đang phân tích dữ liệu từ {filepath}..."")

if __name__ == ""__main__"":
    observer = Observer()
    observer.schedule(ProductionFileHandler(), path=""./ProductionLogs"", recursive=False)
    observer.start()
    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        observer.stop()
    observer.join()",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                }
            });

            context.SaveChanges();
        }
    }
}
