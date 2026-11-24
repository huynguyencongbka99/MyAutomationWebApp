using System.Collections.Generic;
using System.Threading.Tasks;
using AutoTechHub.Models;
using AutoTechHub.Models.ViewModels;

namespace AutoTechHub.Services
{
    public interface IProductService
    {
        Task<ProductListViewModel> GetProductsAsync(string search, string categoryName, string sortBy, int page, int pageSize, bool includeInactive = false);
        Task<List<Product>> GetFeaturedProductsAsync(int count = 3);
        Task<List<Product>> GetAllProductsForAdminAsync();
        Task<Product> GetProductBySlugAsync(string slug, bool includeInactive = false);
        Task<Product> GetProductByIdAsync(int id);
        Task<Product> CreateProductAsync(Product model);
        Task<Product> UpdateProductAsync(Product model);
        Task<bool> DeleteProductAsync(int id);
        Task<bool> ToggleProductActiveAsync(int id);
        Task<List<string>> GetProductCategoriesAsync();
        Task<ProductInquiry> SubmitInquiryAsync(ProductInquiry inquiry);
        Task<List<ProductInquiry>> GetInquiriesAsync();
        Task<bool> UpdateInquiryStatusAsync(int id, ProductInquiryStatus status);
    }
}
