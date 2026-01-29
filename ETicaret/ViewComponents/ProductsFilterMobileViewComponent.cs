using Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ETicaret.ViewComponents
{
    public class ProductsFilterMobileViewComponent : ViewComponent
    {
        private readonly ICategoryService _categoryService;

        public ProductsFilterMobileViewComponent(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await _categoryService.GetAllCategoriesAsync(false);

            return View(categories);
        }
    }
}
