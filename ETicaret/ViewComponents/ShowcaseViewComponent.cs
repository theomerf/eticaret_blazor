using Application.Services.Interfaces;
using ETicaret.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace ETicaret.ViewComponents
{
    public class ShowcaseViewComponent : ViewComponent
    {
        private readonly IProductService _productService;

        public ShowcaseViewComponent(IProductService productService)
        {
            _productService = productService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var products = await _productService.GetShowcaseProductsAsync();
            var favIds = CookieHelper.GetFavouriteProductIds(Request);
            ViewBag.FavouriteIds = CookieHelper.GetFavouriteProductIds(Request);

            return View(products);
        }

    }
}
