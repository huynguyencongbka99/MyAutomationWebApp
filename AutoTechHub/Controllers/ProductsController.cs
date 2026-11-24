using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AutoTechHub.Models;
using AutoTechHub.Models.ViewModels;
using AutoTechHub.Services;

namespace AutoTechHub.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;
        private readonly ISiteConfigService _siteConfigService;

        public ProductsController(IProductService productService, ISiteConfigService siteConfigService)
        {
            _productService = productService;
            _siteConfigService = siteConfigService;
        }

        // GET: /Products or /san-pham
        [Route("Products")]
        [Route("Products/Index")]
        [Route("san-pham")]
        public async Task<IActionResult> Index(string q, string category, string sort = "featured", int page = 1)
        {
            var settings = await _siteConfigService.GetSettingsAsync();
            bool isAdmin = User.IsInRole(nameof(UserRole.Admin));

            if (!settings.EnableShopModule && !isAdmin)
            {
                return View("Disabled", settings);
            }

            var model = await _productService.GetProductsAsync(q, category, sort, page, pageSize: 9, includeInactive: false);
            return View(model);
        }

        // GET: /Products/Details/slug or /san-pham/slug
        [Route("Products/Details/{slug}")]
        [Route("san-pham/{slug}")]
        public async Task<IActionResult> Details(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return NotFound();
            }

            var settings = await _siteConfigService.GetSettingsAsync();
            bool isAdmin = User.IsInRole(nameof(UserRole.Admin));

            if (!settings.EnableShopModule && !isAdmin)
            {
                return View("Disabled", settings);
            }

            var product = await _productService.GetProductBySlugAsync(slug, includeInactive: isAdmin);
            if (product == null)
            {
                return NotFound();
            }

            var allProducts = await _productService.GetFeaturedProductsAsync(6);
            var related = allProducts.Where(p => p.Id != product.Id).Take(3).ToList();

            var model = new ProductDetailViewModel
            {
                Product = product,
                RelatedProducts = related,
                SiteSetting = settings,
                Inquiry = new ProductInquiry { ProductId = product.Id, Quantity = 1 }
            };

            return View(model);
        }

        // POST: /Products/SubmitInquiry
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitInquiry(int productId, string slug, ProductInquiry inquiry)
        {
            var settings = await _siteConfigService.GetSettingsAsync();
            bool isAdmin = User.IsInRole(nameof(UserRole.Admin));

            if (!settings.EnableShopModule && !isAdmin)
            {
                return View("Disabled", settings);
            }

            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            inquiry.ProductId = productId;

            if (ModelState.IsValid)
            {
                await _productService.SubmitInquiryAsync(inquiry);
                TempData["SuccessMessage"] = "Cảm ơn bạn! Yêu cầu đặt hàng / tư vấn đã được gửi tới bộ phận hỗ trợ.";
            }
            else
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ Họ tên và Số điện thoại liên hệ hợp lệ.";
            }

            return RedirectToAction(nameof(Details), new { slug = product.Slug ?? slug });
        }
    }
}
