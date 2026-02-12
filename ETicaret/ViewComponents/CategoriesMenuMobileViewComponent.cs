using Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ETicaret.ViewComponents
{
    public class CategoriesMenuMobileViewComponent : ViewComponent
    {
        private readonly ICategoryService _categoryService;

        public CategoriesMenuMobileViewComponent(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await _categoryService.GetAllAsync();

            return View(categories);
        }
    }
}
