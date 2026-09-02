using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AutoTechHub.Models;
using AutoTechHub.Services;

namespace AutoTechHub.Controllers
{
    public class HomeController : Controller
    {
        private readonly IArticleService _articleService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IArticleService articleService, ILogger<HomeController> logger)
        {
            _articleService = articleService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var model = await _articleService.GetHomeDataAsync();
            return View(model);
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
