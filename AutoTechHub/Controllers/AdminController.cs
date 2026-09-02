using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AutoTechHub.Services;

namespace AutoTechHub.Controllers
{
    public class AdminController : Controller
    {
        private readonly IArticleService _articleService;

        public AdminController(IArticleService articleService)
        {
            _articleService = articleService;
        }

        // GET: /Admin
        public async Task<IActionResult> Index()
        {
            var dashboard = await _articleService.GetAdminDashboardAsync();
            return View(dashboard);
        }

        // POST: /Admin/TogglePublish/5
        [HttpPost]
        public async Task<IActionResult> TogglePublish(int id)
        {
            var newState = await _articleService.TogglePublishAsync(id);
            return Json(new { success = true, isPublished = newState });
        }

        // POST: /Admin/DeleteArticle/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteArticle(int id)
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
    }
}
