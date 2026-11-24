using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoTechHub.Extensions;
using AutoTechHub.Models;
using AutoTechHub.Services;

namespace AutoTechHub.Controllers
{
    public class SnippetsController : Controller
    {
        private readonly ISnippetService _snippetService;

        public SnippetsController(ISnippetService snippetService)
        {
            _snippetService = snippetService;
        }

        // GET: /Snippets
        public async Task<IActionResult> Index(string lang, string cat, string q)
        {
            var snippets = await _snippetService.GetAllSnippetsAsync(lang, cat, q);
            var languages = await _snippetService.GetAvailableLanguagesAsync();

            ViewBag.SelectedLanguage = lang;
            ViewBag.SelectedCategory = cat;
            ViewBag.SearchQuery = q;
            ViewBag.Languages = languages;

            return View(snippets);
        }

        // GET: /Snippets/Create
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Create()
        {
            return View(new CodeSnippet { Author = User.GetFullName() });
        }

        // POST: /Snippets/Create
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CodeSnippet snippet)
        {
            if (ModelState.IsValid)
            {
                await _snippetService.CreateSnippetAsync(snippet);
                TempData["SuccessMessage"] = "Thêm đoạn mã mẫu thành công!";
                return RedirectToAction(nameof(Index));
            }

            return View(snippet);
        }

        // POST: /Snippets/Delete/5
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _snippetService.DeleteSnippetAsync(id);
            TempData["SuccessMessage"] = "Đã xóa đoạn mã thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}
