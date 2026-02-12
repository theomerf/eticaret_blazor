using Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ETicaret.ViewComponents
{
    public class CategorySummaryViewComponent : ViewComponent
    {
        private readonly ICategoryService _categoryService;

        public CategorySummaryViewComponent(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        public async Task<int> InvokeAsync()
        {
            var count = await _categoryService.CountAsync();

            return count;
        }
    }
}
