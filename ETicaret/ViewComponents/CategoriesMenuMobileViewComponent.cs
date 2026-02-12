using Application.DTOs;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace ETicaret.ViewComponents
{
    public class CategoriesMenuMobileViewComponent : ViewComponent
    {
        private readonly ICategoryService _categoryService;
        private readonly IMemoryCache _cache;

        public CategoriesMenuMobileViewComponent(ICategoryService categoryService, IMemoryCache cache)
        {
            _categoryService = categoryService;
            _cache = cache;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            string cacheKey = "allCategories_Mobile";

            if (_cache.TryGetValue(cacheKey, out List<CategoryDto>? cachedCategories))
            {
                return View(cachedCategories);
            }

            var categories = await _categoryService.GetAllAsync(false);

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(30));

            _cache.Set(cacheKey, categories, cacheOptions);

            return View(categories);
        }
    }
}
