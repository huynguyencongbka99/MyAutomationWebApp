using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTechHub.Data;
using AutoTechHub.Models;

namespace AutoTechHub.Services
{
    /// <summary>
    /// Stores ASP.NET Core DataProtection keys in the primary database (PostgreSQL / SQLite)
    /// so authentication cookies and antiforgery tokens remain valid across Vercel container restarts.
    /// </summary>
    public class DbXmlRepository : IXmlRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DbXmlRepository> _logger;

        public DbXmlRepository(IServiceScopeFactory scopeFactory, ILogger<DbXmlRepository> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public IReadOnlyCollection<XElement> GetAllElements()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var keys = db.DataProtectionKeys.AsNoTracking().ToList();
                var result = new List<XElement>(keys.Count);
                foreach (var key in keys)
                {
                    if (!string.IsNullOrWhiteSpace(key.Xml))
                    {
                        result.Add(XElement.Parse(key.Xml));
                    }
                }
                return result.AsReadOnly();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể đọc DataProtection keys từ database; dùng tập key rỗng tạm thời.");
                return Array.Empty<XElement>();
            }
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.DataProtectionKeys.Add(new DataProtectionKey
            {
                FriendlyName = string.IsNullOrWhiteSpace(friendlyName) ? Guid.NewGuid().ToString("N") : friendlyName,
                Xml = element.ToString(SaveOptions.DisableFormatting)
            });
            db.SaveChanges();
        }
    }
}
