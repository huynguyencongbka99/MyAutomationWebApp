using System.Collections.Generic;
using System.Threading.Tasks;
using AutoTechHub.Models;
using AutoTechHub.Models.ViewModels;

namespace AutoTechHub.Services
{
    public interface IArticleService
    {
        Task<HomeViewModel> GetHomeDataAsync();
        Task<ArticleListViewModel> GetArticlesAsync(string search, string categorySlug, string tagSlug, string sortBy, int page, int pageSize);
        Task<ArticleDetailViewModel> GetArticleDetailAsync(string slug);
        Task<Article> GetArticleByIdAsync(int id);
        Task<Article> CreateArticleAsync(ArticleEditViewModel model);
        Task<Article> UpdateArticleAsync(ArticleEditViewModel model);
        Task<bool> DeleteArticleAsync(int id);
        Task<bool> TogglePublishAsync(int id);
        Task<int> IncrementViewCountAsync(int id);
        Task<int> ToggleLikeAsync(int id);
        Task<Comment> AddCommentAsync(int articleId, Comment comment);
        Task<List<Category>> GetAllCategoriesAsync();
        Task<List<Tag>> GetAllTagsAsync();
        Task<AdminDashboardViewModel> GetAdminDashboardAsync();
    }
}
