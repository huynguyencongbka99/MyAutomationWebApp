using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AutoTechHub.Models;
using AutoTechHub.Services;

namespace AutoTechHub.ViewComponents
{
    public class AdSlotViewComponent : ViewComponent
    {
        private readonly IAdService _adService;

        public AdSlotViewComponent(IAdService adService)
        {
            _adService = adService;
        }

        public async Task<IViewComponentResult> InvokeAsync(AdPlacement placement)
        {
            var ads = await _adService.GetActiveAdsByPlacementAsync(placement);
            ViewBag.Placement = placement;
            return View(ads);
        }
    }
}
