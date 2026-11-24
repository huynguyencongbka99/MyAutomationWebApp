using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AutoTechHub.Data
{
    public static class DatabaseConfigHelper
    {
        public static void ConfigureDbContext(DbContextOptionsBuilder options, IConfiguration configuration)
        {
            var (provider, connectionString) = ResolveDatabase(configuration);

            if (provider == "PostgreSQL")
            {
                options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
                });
            }
            else
            {
                options.UseSqlite(connectionString);
            }
        }

        public static (string Provider, string ConnectionString) ResolveDatabase(IConfiguration configuration)
        {
            var configuredProvider = (configuration["DatabaseProvider"] ?? "Auto").Trim();
            var databaseUrl = GetFirstSetting(
                configuration,
                "DATABASE_URL",
                "POSTGRES_URL",
                "NEON_DATABASE_URL",
                "POSTGRES_PRISMA_URL",
                "DATABASE_URL_UNPOOLED",
                "POSTGRES_URL_NON_POOLING");
            var pgConn = configuration.GetConnectionString("PostgreSqlConnection");
            var sqliteConn = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=autotechhub.db";

            if (!string.IsNullOrWhiteSpace(databaseUrl))
            {
                pgConn = ConvertPostgresUriToConnectionString(databaseUrl);
            }
            else if (!string.IsNullOrWhiteSpace(pgConn))
            {
                pgConn = ConvertPostgresUriToConnectionString(pgConn);
            }

            bool explicitPostgres = configuredProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase)
                                    || configuredProvider.Equals("Npgsql", StringComparison.OrdinalIgnoreCase);
            bool explicitSqlite = configuredProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase);

            if (explicitPostgres || (!explicitSqlite && !string.IsNullOrWhiteSpace(pgConn)))
            {
                if (string.IsNullOrWhiteSpace(pgConn))
                {
                    throw new InvalidOperationException(
                        "DatabaseProvider được cấu hình là PostgreSQL nhưng chưa có chuỗi kết nối PostgreSqlConnection hoặc biến môi trường DATABASE_URL / POSTGRES_URL.");
                }
                return ("PostgreSQL", pgConn);
            }

            return ("Sqlite", sqliteConn);
        }

        private static string GetFirstSetting(IConfiguration configuration, params string[] keys)
        {
            foreach (var key in keys)
            {
                var value = Environment.GetEnvironmentVariable(key) ?? configuration[key];
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
        }

        public static string ConvertPostgresUriToConnectionString(string uriOrConnString)
        {
            if (string.IsNullOrWhiteSpace(uriOrConnString))
                return uriOrConnString;

            var trimmed = uriOrConnString.Trim();
            if (!trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
                !trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }

            var uri = new Uri(trimmed);
            var userInfo = uri.UserInfo.Split(':', 2);
            var username = Uri.UnescapeDataString(userInfo[0]);
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
            var database = uri.AbsolutePath.TrimStart('/');
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5432;

            bool isLocal = host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                           || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                           || host.Equals("db", StringComparison.OrdinalIgnoreCase)
                           || host.Equals("postgres", StringComparison.OrdinalIgnoreCase);

            var querySslMode = ExtractQueryParam(uri.Query, "sslmode");
            var sslMode = ResolveSslMode(querySslMode, isLocal);
            var trustCert = (!isLocal && !sslMode.StartsWith("Verify", StringComparison.OrdinalIgnoreCase)) ? "true" : "false";

            // Neon serverless / PgBouncer compatible Npgsql 5.0.10 settings
            return $"Host={host};Port={port};Database={database};Username={username};Password={password};" +
                   $"Pooling=True;Minimum Pool Size=0;Maximum Pool Size=20;Timeout=30;" +
                   $"No Reset On Close=True;Max Auto Prepare=0;SSL Mode={sslMode};Trust Server Certificate={trustCert}";
        }

        private static string ExtractQueryParam(string query, string targetKey)
        {
            if (string.IsNullOrWhiteSpace(query))
                return null;

            var trimmedQuery = query.TrimStart('?');
            foreach (var pair in trimmedQuery.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = pair.Split('=', 2);
                var key = Uri.UnescapeDataString(kv[0]);
                if (key.Equals(targetKey, StringComparison.OrdinalIgnoreCase))
                {
                    return kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : string.Empty;
                }
            }

            return null;
        }

        private static string ResolveSslMode(string querySslMode, bool isLocal)
        {
            if (!string.IsNullOrWhiteSpace(querySslMode))
            {
                switch (querySslMode.Trim().ToLowerInvariant())
                {
                    case "disable": return "Disable";
                    case "allow": return "Allow";
                    case "prefer": return "Prefer";
                    case "require": return "Require";
                    case "verify-ca": return "VerifyCA";
                    case "verify-full": return "VerifyFull";
                }
            }

            return isLocal ? "Prefer" : "Require";
        }
    }
}
