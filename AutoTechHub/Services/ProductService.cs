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
    public class ProductService : IProductService
    {
        private readonly AppDbContext _context;
        private readonly IMarkdownService _markdownService;
        private readonly ISiteConfigService _siteConfigService;

        public ProductService(AppDbContext context, IMarkdownService markdownService, ISiteConfigService siteConfigService)
        {
            _context = context;
            _markdownService = markdownService;
            _siteConfigService = siteConfigService;
        }

        public async Task<ProductListViewModel> GetProductsAsync(string search, string categoryName, string sortBy, int page, int pageSize, bool includeInactive = false)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 9;

            var query = _context.Products.AsQueryable();
            if (!includeInactive)
            {
                query = query.Where(p => p.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                query = query.Where(p => p.CategoryName == categoryName);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(lowerSearch) ||
                                         p.ShortDescription.ToLower().Contains(lowerSearch) ||
                                         (p.Sku != null && p.Sku.ToLower().Contains(lowerSearch)));
            }

            // Materialize before sorting to avoid SQLite decimal ORDER BY limitation
            var filtered = await query.ToListAsync();

            IEnumerable<Product> sorted = sortBy switch
            {
                "price_asc" => filtered.OrderBy(p => p.Price).ThenByDescending(p => p.CreatedAt),
                "price_desc" => filtered.OrderByDescending(p => p.Price).ThenByDescending(p => p.CreatedAt),
                "newest" => filtered.OrderByDescending(p => p.CreatedAt),
                _ => filtered.OrderByDescending(p => p.IsFeatured).ThenBy(p => p.DisplayOrder).ThenByDescending(p => p.CreatedAt)
            };

            var totalItems = filtered.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages == 0) totalPages = 1;

            var paged = sorted
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var categories = await GetProductCategoriesAsync();
            var settings = await _siteConfigService.GetSettingsAsync();

            return new ProductListViewModel
            {
                Products = paged,
                Categories = categories,
                SiteSetting = settings,
                SearchQuery = search,
                CategoryName = categoryName,
                SortBy = sortBy ?? "featured",
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalItems
            };
        }

        public async Task<List<Product>> GetFeaturedProductsAsync(int count = 3)
        {
            return await _context.Products
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.IsFeatured)
                .ThenBy(p => p.DisplayOrder)
                .ThenByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<Product>> GetAllProductsForAdminAsync()
        {
            return await _context.Products
                .OrderBy(p => p.DisplayOrder)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Product> GetProductBySlugAsync(string slug, bool includeInactive = false)
        {
            var query = _context.Products.AsQueryable();
            if (!includeInactive)
            {
                query = query.Where(p => p.IsActive);
            }
            return await query.FirstOrDefaultAsync(p => p.Slug == slug);
        }

        public async Task<Product> GetProductByIdAsync(int id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task<Product> CreateProductAsync(Product model)
        {
            var slug = _markdownService.GenerateSlug(model.Name);
            var baseSlug = slug;
            int counter = 1;
            while (await _context.Products.AnyAsync(p => p.Slug == slug))
            {
                slug = $"{baseSlug}-{counter++}";
            }

            model.Slug = slug;
            model.DescriptionHtml = _markdownService.ToHtml(model.DescriptionMarkdown);
            model.Currency = string.IsNullOrWhiteSpace(model.Currency) ? "VNĐ" : model.Currency.Trim();
            model.ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl)
                ? "https://images.unsplash.com/photo-1581092160607-ee22621dd758?auto=format&fit=crop&w=900&q=80"
                : model.ImageUrl.Trim();
            model.CreatedAt = DateTime.UtcNow;

            _context.Products.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<Product> UpdateProductAsync(Product model)
        {
            var product = await _context.Products.FindAsync(model.Id);
            if (product == null) return null;

            product.Name = model.Name;
            product.Sku = model.Sku;
            product.CategoryName = model.CategoryName;
            product.ShortDescription = model.ShortDescription;
            product.DescriptionMarkdown = model.DescriptionMarkdown;
            product.DescriptionHtml = _markdownService.ToHtml(model.DescriptionMarkdown);
            product.Price = model.Price;
            product.OriginalPrice = model.OriginalPrice;
            product.Currency = string.IsNullOrWhiteSpace(model.Currency) ? "VNĐ" : model.Currency.Trim();
            product.ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? product.ImageUrl : model.ImageUrl.Trim();
            product.ExternalBuyUrl = model.ExternalBuyUrl;
            product.StockStatus = model.StockStatus;
            product.IsFeatured = model.IsFeatured;
            product.IsActive = model.IsActive;
            product.DisplayOrder = model.DisplayOrder;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return false;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleProductActiveAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return false;

            product.IsActive = !product.IsActive;
            await _context.SaveChangesAsync();
            return product.IsActive;
        }

        public async Task<List<string>> GetProductCategoriesAsync()
        {
            return await _context.Products
                .Where(p => p.IsActive)
                .Select(p => p.CategoryName)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        public async Task<ProductInquiry> SubmitInquiryAsync(ProductInquiry inquiry)
        {
            inquiry.Status = ProductInquiryStatus.Pending;
            inquiry.CreatedAt = DateTime.UtcNow;
            _context.ProductInquiries.Add(inquiry);
            await _context.SaveChangesAsync();
            return inquiry;
        }

        public async Task<List<ProductInquiry>> GetInquiriesAsync()
        {
            return await _context.ProductInquiries
                .Include(i => i.Product)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> UpdateInquiryStatusAsync(int id, ProductInquiryStatus status)
        {
            var inquiry = await _context.ProductInquiries.FindAsync(id);
            if (inquiry == null) return false;

            inquiry.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
