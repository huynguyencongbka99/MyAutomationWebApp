using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        // GET: /Articles/Details/slug-name
        [Route("Articles/Details/{slug}")]
        [Route("bai-viet/{slug}")]
        public async Task<IActionResult> Details(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return NotFound();
            }

            var model = await _articleService.GetArticleDetailAsync(slug);
            if (model == null || model.Article == null)
            {
                return NotFound();
            }

            // Increment view count asynchronously
            await _articleService.IncrementViewCountAsync(model.Article.Id);

            return View(model);
        }

        // GET: /Articles/Create
        public async Task<IActionResult> Create()
        {
            var categories = await _articleService.GetAllCategoriesAsync();
            var model = new ArticleEditViewModel
            {
                AvailableCategories = categories,
                AuthorName = "Kỹ sư Tự động hóa",
                IsPublished = true,
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ArticleEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                var article = await _articleService.CreateArticleAsync(model);
                TempData["SuccessMessage"] = "Đăng bài viết mới thành công!";
                return RedirectToAction("Details", new { slug = article.Slug });
            }

            model.AvailableCategories = await _articleService.GetAllCategoriesAsync();
            return View(model);
        }

        // GET: /Articles/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var article = await _articleService.GetArticleByIdAsync(id);
            if (article == null)
            {
                return NotFound();
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
                AvailableCategories = categories
            };

            return View(model);
        }

        // POST: /Articles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ArticleEditViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                var article = await _articleService.UpdateArticleAsync(model);
                if (article == null)
                {
                    return NotFound();
                }

                TempData["SuccessMessage"] = "Cập nhật bài viết thành công!";
                return RedirectToAction("Details", new { slug = article.Slug });
            }

            model.AvailableCategories = await _articleService.GetAllCategoriesAsync();
            return View(model);
        }

        // POST: /Articles/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _articleService.DeleteArticleAsync(id);
            if (result)
            {
                TempData["SuccessMessage"] = "Đã xóa bài viết thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy bài viết để xóa.";
            }

            return RedirectToAction(nameof(Index));
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

            return RedirectToAction("Details", new { slug });
        }
    }
}
