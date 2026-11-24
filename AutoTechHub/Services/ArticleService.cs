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
        private readonly ISiteConfigService _siteConfigService;

        public ArticleService(
            AppDbContext context,
            IMarkdownService markdownService,
            ISiteConfigService siteConfigService)
        {
            _context = context;
            _markdownService = markdownService;
            _siteConfigService = siteConfigService;
        }

        public async Task<HomeViewModel> GetHomeDataAsync()
        {
            var featured = await _context.Articles
                .Include(a => a.Category)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                .Where(a => a.IsPublished && a.ApprovalStatus == ArticleApprovalStatus.Approved && a.IsFeatured)
                .OrderByDescending(a => a.CreatedAt)
                .Take(4)
                .ToListAsync();

            var latest = await _context.Articles
                .Include(a => a.Category)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                .Where(a => a.IsPublished && a.ApprovalStatus == ArticleApprovalStatus.Approved)
                .OrderByDescending(a => a.CreatedAt)
                .Take(6)
                .ToListAsync();

            var categories = await _context.Categories
                .Include(c => c.Articles)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            var popularTags = await _context.Tags
                .Include(t => t.ArticleTags)
                    .ThenInclude(at => at.Article)
                .OrderByDescending(t => t.ArticleTags.Count(at => at.Article.IsPublished && at.Article.ApprovalStatus == ArticleApprovalStatus.Approved))
                .Take(12)
                .ToListAsync();

            var snippets = await _context.CodeSnippets
                .OrderByDescending(s => s.CreatedAt)
                .Take(4)
                .ToListAsync();

            var totalArticles = await _context.Articles
                .CountAsync(a => a.IsPublished && a.ApprovalStatus == ArticleApprovalStatus.Approved);
            var totalViews = await _context.Articles
                .Where(a => a.IsPublished && a.ApprovalStatus == ArticleApprovalStatus.Approved)
                .SumAsync(a => a.ViewCount);
            var totalCategories = categories.Count;
            var totalSnippets = await _context.CodeSnippets.CountAsync();

            var siteSetting = await _siteConfigService.GetSettingsAsync();
            var featuredProducts = siteSetting.EnableShopModule
                ? await _context.Products
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.IsFeatured)
                    .ThenBy(p => p.DisplayOrder)
                    .ThenByDescending(p => p.CreatedAt)
                    .Take(3)
                    .ToListAsync()
                : new List<Product>();

            var homeSections = _siteConfigService.GetHomeSections(siteSetting);
            var customBlocks = await _siteConfigService.GetActiveWebBlocksAsync();

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
                TotalSnippets = totalSnippets,
                SiteSetting = siteSetting,
                FeaturedProducts = featuredProducts,
                HomeSections = homeSections,
                CustomBlocks = customBlocks
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
                .Where(a => a.IsPublished && a.ApprovalStatus == ArticleApprovalStatus.Approved)
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
                    .ThenInclude(at => at.Article)
                .Where(t => t.ArticleTags.Any(at => at.Article.IsPublished && at.Article.ApprovalStatus == ArticleApprovalStatus.Approved))
                .OrderByDescending(t => t.ArticleTags.Count(at => at.Article.IsPublished && at.Article.ApprovalStatus == ArticleApprovalStatus.Approved))
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

        public async Task<ArticleDetailViewModel> GetArticleDetailAsync(string slug, int? viewerUserId = null, bool isAdmin = false)
        {
            var article = await _context.Articles
                .Include(a => a.Category)
                .Include(a => a.AuthorUser)
                .Include(a => a.ReviewedByUser)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                .Include(a => a.Comments.Where(c => c.IsApproved).OrderByDescending(c => c.CreatedAt))
                .FirstOrDefaultAsync(a => a.Slug == slug);

            if (article == null) return null;

            bool isPublic = article.IsPublished && article.ApprovalStatus == ArticleApprovalStatus.Approved;
            bool isAuthor = viewerUserId.HasValue && article.AuthorUserId == viewerUserId.Value;

            if (!isPublic && !isAdmin && !isAuthor)
            {
                return null;
            }

            var related = await _context.Articles
                .Include(a => a.Category)
                .Where(a => a.IsPublished && a.ApprovalStatus == ArticleApprovalStatus.Approved && a.CategoryId == article.CategoryId && a.Id != article.Id)
                .OrderByDescending(a => a.CreatedAt)
                .Take(3)
                .ToListAsync();

            var prev = await _context.Articles
                .Where(a => a.IsPublished && a.ApprovalStatus == ArticleApprovalStatus.Approved && a.CreatedAt < article.CreatedAt)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            var next = await _context.Articles
                .Where(a => a.IsPublished && a.ApprovalStatus == ArticleApprovalStatus.Approved && a.CreatedAt > article.CreatedAt)
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
                .Include(a => a.AuthorUser)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<List<Article>> GetArticlesByAuthorAsync(int authorUserId)
        {
            return await _context.Articles
                .Include(a => a.Category)
                .Include(a => a.ReviewedByUser)
                .Where(a => a.AuthorUserId == authorUserId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<Article> CreateArticleAsync(ArticleEditViewModel model, int? authorUserId, UserRole authorRole)
        {
            var slug = _markdownService.GenerateSlug(model.Title);

            var baseSlug = slug;
            int counter = 1;
            while (await _context.Articles.AnyAsync(a => a.Slug == slug))
            {
                slug = $"{baseSlug}-{counter++}";
            }

            var html = _markdownService.ToHtml(model.ContentMarkdown);
            var readingTime = _markdownService.CalculateReadingTime(model.ContentMarkdown);

            bool isDraft = string.Equals(model.SubmitAction, "draft", StringComparison.OrdinalIgnoreCase);
            ArticleApprovalStatus status;
            bool isPublished;
            bool isFeatured;

            if (authorRole == UserRole.User)
            {
                isFeatured = false;
                isPublished = false;
                status = isDraft ? ArticleApprovalStatus.Draft : ArticleApprovalStatus.Pending;
            }
            else
            {
                isFeatured = model.IsFeatured;
                if (isDraft || !model.IsPublished)
                {
                    status = ArticleApprovalStatus.Draft;
                    isPublished = false;
                }
                else
                {
                    status = ArticleApprovalStatus.Approved;
                    isPublished = true;
                }
            }

            var article = new Article
            {
                Title = model.Title,
                Slug = slug,
                Summary = model.Summary,
                ContentMarkdown = model.ContentMarkdown,
                ContentHtml = html,
                ThumbnailUrl = string.IsNullOrWhiteSpace(model.ThumbnailUrl) ? "https://images.unsplash.com/photo-1581092160607-ee22621dd758?auto=format&fit=crop&w=800&q=80" : model.ThumbnailUrl.Trim(),
                AuthorName = model.AuthorName ?? "Kỹ sư Tự động hóa",
                AuthorEmail = model.AuthorEmail,
                ReadingTimeMinutes = readingTime,
                IsFeatured = isFeatured,
                IsPublished = isPublished,
                ApprovalStatus = status,
                AuthorUserId = authorUserId,
                CategoryId = model.CategoryId,
                CreatedAt = DateTime.UtcNow
            };

            if (status == ArticleApprovalStatus.Approved && authorUserId.HasValue)
            {
                article.ReviewedByUserId = authorUserId;
                article.ReviewedAt = DateTime.UtcNow;
            }

            _context.Articles.Add(article);
            await _context.SaveChangesAsync();

            await SyncTagsAsync(article, model.TagsString);

            return article;
        }

        public async Task<Article> UpdateArticleAsync(ArticleEditViewModel model, int editorUserId, UserRole editorRole)
        {
            var article = await _context.Articles
                .Include(a => a.ArticleTags)
                .FirstOrDefaultAsync(a => a.Id == model.Id);

            if (article == null) return null;

            if (editorRole != UserRole.Admin && article.AuthorUserId != editorUserId)
            {
                return null;
            }

            article.Title = model.Title;
            article.Summary = model.Summary;
            article.ContentMarkdown = model.ContentMarkdown;
            article.ContentHtml = _markdownService.ToHtml(model.ContentMarkdown);
            article.ReadingTimeMinutes = _markdownService.CalculateReadingTime(model.ContentMarkdown);
            article.ThumbnailUrl = string.IsNullOrWhiteSpace(model.ThumbnailUrl) ? article.ThumbnailUrl : model.ThumbnailUrl.Trim();
            article.AuthorName = model.AuthorName ?? article.AuthorName;
            article.AuthorEmail = model.AuthorEmail;
            article.CategoryId = model.CategoryId;
            article.UpdatedAt = DateTime.UtcNow;

            bool isDraft = string.Equals(model.SubmitAction, "draft", StringComparison.OrdinalIgnoreCase);

            if (editorRole == UserRole.User)
            {
                article.IsFeatured = false;
                article.IsPublished = false;
                if (isDraft)
                {
                    article.ApprovalStatus = ArticleApprovalStatus.Draft;
                }
                else
                {
                    article.ApprovalStatus = ArticleApprovalStatus.Pending;
                    article.ReviewNote = null;
                    article.ReviewedAt = null;
                    article.ReviewedByUserId = null;
                }
            }
            else
            {
                article.IsFeatured = model.IsFeatured;
                if (isDraft || !model.IsPublished)
                {
                    article.IsPublished = false;
                    article.ApprovalStatus = ArticleApprovalStatus.Draft;
                }
                else
                {
                    article.IsPublished = true;
                    article.ApprovalStatus = ArticleApprovalStatus.Approved;
                }
            }

            _context.ArticleTags.RemoveRange(article.ArticleTags);
            await _context.SaveChangesAsync();

            await SyncTagsAsync(article, model.TagsString);

            return article;
        }

        private async Task SyncTagsAsync(Article article, string tagsString)
        {
            if (string.IsNullOrWhiteSpace(tagsString)) return;

            var tagNames = tagsString.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
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
            if (article.IsPublished && article.ApprovalStatus != ArticleApprovalStatus.Approved)
            {
                article.ApprovalStatus = ArticleApprovalStatus.Approved;
                article.ReviewedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            return article.IsPublished;
        }

        public async Task<bool> ApproveArticleAsync(int id, int adminUserId, string note = null)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article == null) return false;

            article.ApprovalStatus = ArticleApprovalStatus.Approved;
            article.IsPublished = true;
            article.ReviewedByUserId = adminUserId;
            article.ReviewedAt = DateTime.UtcNow;
            article.ReviewNote = string.IsNullOrWhiteSpace(note) ? "Đã phê duyệt và xuất bản." : note.Trim();

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectArticleAsync(int id, int adminUserId, string note)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article == null) return false;

            article.ApprovalStatus = ArticleApprovalStatus.Rejected;
            article.IsPublished = false;
            article.ReviewedByUserId = adminUserId;
            article.ReviewedAt = DateTime.UtcNow;
            article.ReviewNote = string.IsNullOrWhiteSpace(note) ? "Bài viết cần bổ sung nội dung trước khi đăng." : note.Trim();

            await _context.SaveChangesAsync();
            return true;
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

        public async Task<AdminDashboardViewModel> GetAdminDashboardAsync(int currentUserId, UserRole currentUserRole)
        {
            bool isAdmin = currentUserRole == UserRole.Admin;
            var siteSetting = await _siteConfigService.GetSettingsAsync();

            var articlesQuery = _context.Articles
                .Include(a => a.Category)
                .Include(a => a.AuthorUser)
                .AsQueryable();

            if (!isAdmin)
            {
                articlesQuery = articlesQuery.Where(a => a.AuthorUserId == currentUserId);
            }

            var totalArticles = await articlesQuery.CountAsync();
            var publishedArticles = await articlesQuery.CountAsync(a => a.IsPublished && a.ApprovalStatus == ArticleApprovalStatus.Approved);
            var totalViews = await articlesQuery.SumAsync(a => a.ViewCount);
            var totalLikes = await articlesQuery.SumAsync(a => a.LikeCount);

            var recentArticles = await articlesQuery
                .OrderByDescending(a => a.CreatedAt)
                .Take(20)
                .ToListAsync();

            var pendingArticles = isAdmin
                ? await _context.Articles
                    .Include(a => a.Category)
                    .Include(a => a.AuthorUser)
                    .Where(a => a.ApprovalStatus == ArticleApprovalStatus.Pending)
                    .OrderBy(a => a.CreatedAt)
                    .ToListAsync()
                : new List<Article>();

            var commentsQuery = _context.Comments
                .Include(c => c.Article)
                .AsQueryable();

            if (!isAdmin)
            {
                commentsQuery = commentsQuery.Where(c => c.Article.AuthorUserId == currentUserId);
            }

            var totalComments = await commentsQuery.CountAsync();
            var recentComments = await commentsQuery
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .ToListAsync();

            return new AdminDashboardViewModel
            {
                IsAdmin = isAdmin,
                TotalArticles = totalArticles,
                PublishedArticles = publishedArticles,
                PendingApprovalCount = pendingArticles.Count,
                TotalViews = totalViews,
                TotalLikes = totalLikes,
                TotalComments = totalComments,
                TotalSnippets = await _context.CodeSnippets.CountAsync(),
                TotalUsers = isAdmin ? await _context.Users.CountAsync() : 0,
                ActiveAdsCount = isAdmin ? await _context.Advertisements.CountAsync(a => a.IsActive) : 0,
                ActiveProductsCount = isAdmin ? await _context.Products.CountAsync(p => p.IsActive) : 0,
                PendingInquiriesCount = isAdmin ? await _context.ProductInquiries.CountAsync(i => i.Status == ProductInquiryStatus.Pending) : 0,
                SiteSetting = siteSetting,
                PendingArticles = pendingArticles,
                RecentArticles = recentArticles,
                RecentComments = recentComments
            };
        }
    }
}
