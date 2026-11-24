using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoTechHub.Extensions;
using AutoTechHub.Models;
using AutoTechHub.Models.ViewModels;
using AutoTechHub.Services;

namespace AutoTechHub.Controllers
{
    public class ArticlesController : Controller
    {
        private readonly IArticleService _articleService;

        public ArticlesController(IArticleService articleService)
        {
            _articleService = articleService;
        }

        // GET: /Articles
        public async Task<IActionResult> Index(string q, string category, string tag, string sort = "newest", int page = 1)
        {
            var model = await _articleService.GetArticlesAsync(q, category, tag, sort, page, pageSize: 9);
            return View(model);
        }

        // GET: /Articles/MyArticles
        [Authorize]
        public async Task<IActionResult> MyArticles()
        {
            var userId = User.GetUserId();
            if (!userId.HasValue) return Challenge();

            var articles = await _articleService.GetArticlesByAuthorAsync(userId.Value);
            return View(articles);
        }

        // GET: /Articles/Details/slug-name
        [Route("Articles/Details/{slug}")]
        [Route("bai-viet/{slug}")]
        public async Task<IActionResult> Details(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return NotFound();
            }

            var viewerId = User.GetUserId();
            var isAdmin = User.IsInRole(nameof(UserRole.Admin));

            var model = await _articleService.GetArticleDetailAsync(slug, viewerId, isAdmin);
            if (model == null || model.Article == null)
            {
                return NotFound();
            }

            // Increment view count only for publicly visible articles
            if (model.Article.IsPublished && model.Article.ApprovalStatus == ArticleApprovalStatus.Approved)
            {
                await _articleService.IncrementViewCountAsync(model.Article.Id);
            }

            return View(model);
        }

        // GET: /Articles/Create
        [Authorize]
        public async Task<IActionResult> Create()
        {
            var categories = await _articleService.GetAllCategoriesAsync();
            var role = User.GetUserRole();
            var fullName = User.GetFullName();
            var email = User.FindFirstValue(ClaimTypes.Email);

            var model = new ArticleEditViewModel
            {
                AvailableCategories = categories,
                AuthorName = string.IsNullOrWhiteSpace(fullName) ? "Kỹ sư Tự động hóa" : fullName,
                AuthorEmail = email,
                IsPublished = role != UserRole.User,
                ContentMarkdown = @"## 1. Giới thiệu

Nội dung phần mở đầu ở đây...

## 2. Chuẩn bị phần cứng & phần mềm

- Thiết bị 1
- Thiết bị 2

## 3. Hướng dẫn lập trình

```csharp
// Viết mã nguồn của bạn ở đây
public void AutomationTask()
{
    Console.WriteLine(""Tự động hóa thành công!"");
}
```

## 4. Kết luận & Mở rộng
"
            };

            return View(model);
        }

        // POST: /Articles/Create
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ArticleEditViewModel model)
        {
            var userId = User.GetUserId();
            if (!userId.HasValue) return Challenge();
            var role = User.GetUserRole();

            if (ModelState.IsValid)
            {
                var article = await _articleService.CreateArticleAsync(model, userId, role);

                if (role == UserRole.User)
                {
                    TempData["SuccessMessage"] = article.ApprovalStatus == ArticleApprovalStatus.Draft
                        ? "Đã lưu bản nháp bài viết thành công."
                        : "Bài viết của bạn đã được gửi và đang chờ Admin phê duyệt!";
                    return RedirectToAction(nameof(MyArticles));
                }

                if (article.IsPublished)
                {
                    TempData["SuccessMessage"] = "Đăng bài viết mới thành công!";
                    return RedirectToAction(nameof(Details), new { slug = article.Slug });
                }

                TempData["SuccessMessage"] = "Đã lưu bản nháp bài viết thành công!";
                return RedirectToAction(nameof(MyArticles));
            }

            model.AvailableCategories = await _articleService.GetAllCategoriesAsync();
            return View(model);
        }

        // GET: /Articles/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var article = await _articleService.GetArticleByIdAsync(id);
            if (article == null)
            {
                return NotFound();
            }

            var userId = User.GetUserId();
            var isAdmin = User.IsInRole(nameof(UserRole.Admin));
            if (!isAdmin && (!userId.HasValue || article.AuthorUserId != userId.Value))
            {
                return Forbid();
            }

            var categories = await _articleService.GetAllCategoriesAsync();
            var tagsString = string.Join(", ", article.ArticleTags.Select(at => at.Tag.Name));

            var model = new ArticleEditViewModel
            {
                Id = article.Id,
                Title = article.Title,
                Summary = article.Summary,
                ContentMarkdown = article.ContentMarkdown,
                ThumbnailUrl = article.ThumbnailUrl,
                AuthorName = article.AuthorName,
                AuthorEmail = article.AuthorEmail,
                CategoryId = article.CategoryId,
                TagsString = tagsString,
                IsFeatured = article.IsFeatured,
                IsPublished = article.IsPublished,
                ApprovalStatus = article.ApprovalStatus,
                ReviewNote = article.ReviewNote,
                AvailableCategories = categories
            };

            return View(model);
        }

        // POST: /Articles/Edit/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ArticleEditViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var existing = await _articleService.GetArticleByIdAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            var userId = User.GetUserId();
            if (!userId.HasValue) return Challenge();
            var role = User.GetUserRole();

            if (role != UserRole.Admin && existing.AuthorUserId != userId.Value)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                var article = await _articleService.UpdateArticleAsync(model, userId.Value, role);
                if (article == null)
                {
                    return NotFound();
                }

                if (role == UserRole.User)
                {
                    TempData["SuccessMessage"] = article.ApprovalStatus == ArticleApprovalStatus.Draft
                        ? "Đã lưu thay đổi vào bản nháp."
                        : "Đã cập nhật và gửi bài viết chờ Admin phê duyệt!";
                    return RedirectToAction(nameof(MyArticles));
                }

                TempData["SuccessMessage"] = "Cập nhật bài viết thành công!";
                return article.IsPublished
                    ? RedirectToAction(nameof(Details), new { slug = article.Slug })
                    : RedirectToAction(nameof(MyArticles));
            }

            model.AvailableCategories = await _articleService.GetAllCategoriesAsync();
            return View(model);
        }

        // POST: /Articles/Delete/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _articleService.GetArticleByIdAsync(id);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy bài viết để xóa.";
                return RedirectToAction(nameof(MyArticles));
            }

            var userId = User.GetUserId();
            var isAdmin = User.IsInRole(nameof(UserRole.Admin));
            if (!isAdmin && (!userId.HasValue || existing.AuthorUserId != userId.Value))
            {
                return Forbid();
            }

            var result = await _articleService.DeleteArticleAsync(id);
            TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
                ? "Đã xóa bài viết thành công."
                : "Không tìm thấy bài viết để xóa.";

            return RedirectToAction(nameof(MyArticles));
        }

        // POST: /Articles/ToggleLike/5 (AJAX)
        [HttpPost]
        public async Task<IActionResult> ToggleLike(int id)
        {
            var likes = await _articleService.ToggleLikeAsync(id);
            return Json(new { success = true, likes });
        }

        // POST: /Articles/AddComment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int articleId, string slug, Comment comment)
        {
            if (ModelState.IsValid)
            {
                await _articleService.AddCommentAsync(articleId, comment);
                TempData["SuccessMessage"] = "Cảm ơn bạn đã gửi bình luận!";
            }
            else
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ họ tên và nội dung bình luận.";
            }

            return RedirectToAction(nameof(Details), new { slug });
        }
    }
}
