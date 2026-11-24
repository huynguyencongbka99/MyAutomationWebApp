using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoTechHub.Services;

namespace AutoTechHub.Controllers
{
    [AllowAnonymous]
    public class MediaController : Controller
    {
        private readonly ISiteConfigService _siteConfigService;

        public MediaController(ISiteConfigService siteConfigService)
        {
            _siteConfigService = siteConfigService;
        }

        [HttpGet("/media/{id:int}/{fileName?}")]
        public async Task<IActionResult> Get(int id, string fileName = null)
        {
            var asset = await _siteConfigService.GetMediaAssetAsync(id);
            if (asset == null || asset.Data == null || asset.Data.Length == 0)
            {
                return NotFound();
            }

            Response.Headers["Cache-Control"] = "public, max-age=604800";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            if (string.Equals(asset.ContentType, "image/svg+xml", StringComparison.OrdinalIgnoreCase))
            {
                Response.Headers["Content-Security-Policy"] = "default-src 'none'; style-src 'unsafe-inline';";
            }

            return File(asset.Data, asset.ContentType);
        }
    }
}
