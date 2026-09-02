using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AutoTechHub.Services;

namespace AutoTechHub.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly IArticleService _articleService;

        public CategoriesController(IArticleService articleService)
        {
            _articleService = articleService;
        }

        // GET: /Categories
        public async Task<IActionResult> Index()
        {
            var categories = await _articleService.GetAllCategoriesAsync();
            return View(categories);
        }

        // GET: /Categories/Details/industrial-automation
        [Route("Categories/Details/{slug}")]
        [Route("chuyen-muc/{slug}")]
        public async Task<IActionResult> Details(string slug, string q, string sort = "newest", int page = 1)
        {
            var model = await _articleService.GetArticlesAsync(q, slug, null, sort, page, pageSize: 9);
            if (model.CurrentCategory == null)
            {
                return NotFound();
            }

            return View(model);
        }
    }
}
