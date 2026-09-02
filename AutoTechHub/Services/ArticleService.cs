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
    public class ArticleService : IArticleService
    {
        private readonly AppDbContext _context;
        private readonly IMarkdownService _markdownService;

        public ArticleService(AppDbContext context, IMarkdownService markdownService)
        {
            _context = context;
            _markdownService = markdownService;
        }

        public async Task<HomeViewModel> GetHomeDataAsync()
        {
            var featured = await _context.Articles
                .Include(a => a.Category)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                .Where(a => a.IsPublished && a.IsFeatured)
                .OrderByDescending(a => a.CreatedAt)
                .Take(4)
                .ToListAsync();

            var latest = await _context.Articles
                .Include(a => a.Category)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                .Where(a => a.IsPublished)
                .OrderByDescending(a => a.CreatedAt)
                .Take(6)
                .ToListAsync();

            var categories = await _context.Categories
                .Include(c => c.Articles)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            var popularTags = await _context.Tags
                .Include(t => t.ArticleTags)
                .OrderByDescending(t => t.ArticleTags.Count)
                .Take(12)
                .ToListAsync();

            var snippets = await _context.CodeSnippets
                .OrderByDescending(s => s.CreatedAt)
                .Take(4)
                .ToListAsync();

            var totalArticles = await _context.Articles.CountAsync(a => a.IsPublished);
            var totalViews = await _context.Articles.SumAsync(a => a.ViewCount);
            var totalCategories = categories.Count;
            var totalSnippets = await _context.CodeSnippets.CountAsync();

            return new HomeViewModel
            {
                FeaturedArticles = featured,
                LatestArticles = latest,
                Categories = categories,
                PopularTags = popularTags,
                FeaturedSnippets = snippets,
                TotalArticles = totalArticles,
                TotalViews = totalViews,
                TotalCategories = totalCategories,
                TotalSnippets = totalSnippets
            };
        }

        public async Task<ArticleListViewModel> GetArticlesAsync(string search, string categorySlug, string tagSlug, string sortBy, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 9;

            var query = _context.Articles
                .Include(a => a.Category)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                .Where(a => a.IsPublished)
                .AsQueryable();

            Category currentCategory = null;
            if (!string.IsNullOrWhiteSpace(categorySlug))
            {
                currentCategory = await _context.Categories.FirstOrDefaultAsync(c => c.Slug == categorySlug);
                if (currentCategory != null)
                {
                    query = query.Where(a => a.CategoryId == currentCategory.Id);
                }
            }

            Tag currentTag = null;
            if (!string.IsNullOrWhiteSpace(tagSlug))
            {
                currentTag = await _context.Tags.FirstOrDefaultAsync(t => t.Slug == tagSlug);
                if (currentTag != null)
                {
                    query = query.Where(a => a.ArticleTags.Any(at => at.TagId == currentTag.Id));
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.Trim().ToLower();
                query = query.Where(a => a.Title.ToLower().Contains(lowerSearch) ||
                                         a.Summary.ToLower().Contains(lowerSearch) ||
                                         a.ContentMarkdown.ToLower().Contains(lowerSearch));
            }

            // Sorting
            query = sortBy switch
            {
                "views" => query.OrderByDescending(a => a.ViewCount),
                "likes" => query.OrderByDescending(a => a.LikeCount),
                "oldest" => query.OrderBy(a => a.CreatedAt),
                _ => query.OrderByDescending(a => a.CreatedAt)
            };

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages == 0) totalPages = 1;

            var articles = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var categories = await _context.Categories
                .Include(c => c.Articles)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            var tags = await _context.Tags
                .Include(t => t.ArticleTags)
                .Where(t => t.ArticleTags.Any())
                .OrderByDescending(t => t.ArticleTags.Count)
                .Take(20)
                .ToListAsync();

            return new ArticleListViewModel
            {
                Articles = articles,
                Categories = categories,
                Tags = tags,
                SearchQuery = search,
                CategorySlug = categorySlug,
                TagSlug = tagSlug,
                SortBy = sortBy,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                PageSize = pageSize,
                CurrentCategory = currentCategory,
                CurrentTag = currentTag
            };
        }

        public async Task<ArticleDetailViewModel> GetArticleDetailAsync(string slug)
        {
            var article = await _context.Articles
                .Include(a => a.Category)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                .Include(a => a.Comments.Where(c => c.IsApproved).OrderByDescending(c => c.CreatedAt))
                .FirstOrDefaultAsync(a => a.Slug == slug);

            if (article == null) return null;

            // Related articles in same category
            var related = await _context.Articles
                .Include(a => a.Category)
                .Where(a => a.IsPublished && a.CategoryId == article.CategoryId && a.Id != article.Id)
                .OrderByDescending(a => a.CreatedAt)
                .Take(3)
                .ToListAsync();

            // Previous & Next
            var prev = await _context.Articles
                .Where(a => a.IsPublished && a.CreatedAt < article.CreatedAt)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            var next = await _context.Articles
                .Where(a => a.IsPublished && a.CreatedAt > article.CreatedAt)
                .OrderBy(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            return new ArticleDetailViewModel
            {
                Article = article,
                RelatedArticles = related,
                PreviousArticle = prev,
                NextArticle = next
            };
        }

        public async Task<Article> GetArticleByIdAsync(int id)
        {
            return await _context.Articles
                .Include(a => a.Category)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<Article> CreateArticleAsync(ArticleEditViewModel model)
        {
            var slug = _markdownService.GenerateSlug(model.Title);
            
            // Ensure unique slug
            var baseSlug = slug;
            int counter = 1;
            while (await _context.Articles.AnyAsync(a => a.Slug == slug))
            {
                slug = $"{baseSlug}-{counter++}";
            }

            var html = _markdownService.ToHtml(model.ContentMarkdown);
            var readingTime = _markdownService.CalculateReadingTime(model.ContentMarkdown);

            var article = new Article
            {
                Title = model.Title,
                Slug = slug,
                Summary = model.Summary,
                ContentMarkdown = model.ContentMarkdown,
                ContentHtml = html,
                ThumbnailUrl = string.IsNullOrWhiteSpace(model.ThumbnailUrl) ? "/images/default-article.jpg" : model.ThumbnailUrl,
                AuthorName = model.AuthorName ?? "Kỹ sư Tự động hóa",
                AuthorEmail = model.AuthorEmail,
                ReadingTimeMinutes = readingTime,
                IsFeatured = model.IsFeatured,
                IsPublished = model.IsPublished,
                CategoryId = model.CategoryId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Articles.Add(article);
            await _context.SaveChangesAsync();

            // Process tags
            if (!string.IsNullOrWhiteSpace(model.TagsString))
            {
                var tagNames = model.TagsString.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct();

                foreach (var tagName in tagNames)
                {
                    var tagSlug = _markdownService.GenerateSlug(tagName);
                    var existingTag = await _context.Tags.FirstOrDefaultAsync(t => t.Slug == tagSlug || t.Name.ToLower() == tagName.ToLower());
                    if (existingTag == null)
                    {
                        existingTag = new Tag
                        {
                            Name = tagName,
                            Slug = tagSlug
                        };
                        _context.Tags.Add(existingTag);
                        await _context.SaveChangesAsync();
                    }

                    _context.ArticleTags.Add(new ArticleTag
                    {
                        ArticleId = article.Id,
                        TagId = existingTag.Id
                    });
                }
                await _context.SaveChangesAsync();
            }

            return article;
        }

        public async Task<Article> UpdateArticleAsync(ArticleEditViewModel model)
        {
            var article = await _context.Articles
                .Include(a => a.ArticleTags)
                .FirstOrDefaultAsync(a => a.Id == model.Id);

            if (article == null) return null;

            article.Title = model.Title;
            article.Summary = model.Summary;
            article.ContentMarkdown = model.ContentMarkdown;
            article.ContentHtml = _markdownService.ToHtml(model.ContentMarkdown);
            article.ReadingTimeMinutes = _markdownService.CalculateReadingTime(model.ContentMarkdown);
            article.ThumbnailUrl = string.IsNullOrWhiteSpace(model.ThumbnailUrl) ? article.ThumbnailUrl : model.ThumbnailUrl;
            article.AuthorName = model.AuthorName ?? article.AuthorName;
            article.AuthorEmail = model.AuthorEmail;
            article.CategoryId = model.CategoryId;
            article.IsFeatured = model.IsFeatured;
            article.IsPublished = model.IsPublished;
            article.UpdatedAt = DateTime.UtcNow;

            // Clear old tags
            _context.ArticleTags.RemoveRange(article.ArticleTags);

            // Re-add tags
            if (!string.IsNullOrWhiteSpace(model.TagsString))
            {
                var tagNames = model.TagsString.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct();

                foreach (var tagName in tagNames)
                {
                    var tagSlug = _markdownService.GenerateSlug(tagName);
                    var existingTag = await _context.Tags.FirstOrDefaultAsync(t => t.Slug == tagSlug || t.Name.ToLower() == tagName.ToLower());
                    if (existingTag == null)
                    {
                        existingTag = new Tag
                        {
                            Name = tagName,
                            Slug = tagSlug
                        };
                        _context.Tags.Add(existingTag);
                        await _context.SaveChangesAsync();
                    }

                    _context.ArticleTags.Add(new ArticleTag
                    {
                        ArticleId = article.Id,
                        TagId = existingTag.Id
                    });
                }
            }

            await _context.SaveChangesAsync();
            return article;
        }

        public async Task<bool> DeleteArticleAsync(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article == null) return false;

            _context.Articles.Remove(article);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> TogglePublishAsync(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article == null) return false;

            article.IsPublished = !article.IsPublished;
            await _context.SaveChangesAsync();
            return article.IsPublished;
        }

        public async Task<int> IncrementViewCountAsync(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article == null) return 0;

            article.ViewCount++;
            await _context.SaveChangesAsync();
            return article.ViewCount;
        }

        public async Task<int> ToggleLikeAsync(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article == null) return 0;

            article.LikeCount++;
            await _context.SaveChangesAsync();
            return article.LikeCount;
        }

        public async Task<Comment> AddCommentAsync(int articleId, Comment comment)
        {
            comment.ArticleId = articleId;
            comment.CreatedAt = DateTime.UtcNow;
            comment.IsApproved = true;

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();
            return comment;
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return await _context.Categories
                .Include(c => c.Articles)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();
        }

        public async Task<List<Tag>> GetAllTagsAsync()
        {
            return await _context.Tags
                .Include(t => t.ArticleTags)
                .OrderByDescending(t => t.ArticleTags.Count)
                .ToListAsync();
        }

        public async Task<AdminDashboardViewModel> GetAdminDashboardAsync()
        {
            var totalArticles = await _context.Articles.CountAsync();
            var publishedArticles = await _context.Articles.CountAsync(a => a.IsPublished);
            var totalViews = await _context.Articles.SumAsync(a => a.ViewCount);
            var totalLikes = await _context.Articles.SumAsync(a => a.LikeCount);
            var totalComments = await _context.Comments.CountAsync();
            var totalSnippets = await _context.CodeSnippets.CountAsync();

            var recentArticles = await _context.Articles
                .Include(a => a.Category)
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .ToListAsync();

            var recentComments = await _context.Comments
                .Include(c => c.Article)
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .ToListAsync();

            return new AdminDashboardViewModel
            {
                TotalArticles = totalArticles,
                PublishedArticles = publishedArticles,
                TotalViews = totalViews,
                TotalLikes = totalLikes,
                TotalComments = totalComments,
                TotalSnippets = totalSnippets,
                RecentArticles = recentArticles,
                RecentComments = recentComments
            };
        }
    }
}
