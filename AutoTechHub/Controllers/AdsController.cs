using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AutoTechHub.Services;

namespace AutoTechHub.Controllers
{
    public class AdsController : Controller
    {
        private readonly IAdService _adService;

        public AdsController(IAdService adService)
        {
            _adService = adService;
        }

        // GET: /Ads/Go/5
        [HttpGet]
        public async Task<IActionResult> Go(int id)
        {
            var targetUrl = await _adService.RecordClickAndGetTargetUrlAsync(id);
            if (string.IsNullOrWhiteSpace(targetUrl))
            {
                return RedirectToAction("Index", "Home");
            }

            if (Url.IsLocalUrl(targetUrl) ||
                targetUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                targetUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return Redirect(targetUrl);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
