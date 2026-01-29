using Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ETicaret.ViewComponents
{
    public class ProductSummaryViewComponent : ViewComponent
    {
        private readonly IProductService _productService;

        public ProductSummaryViewComponent(IProductService productService)
        {
            _productService = productService;
        }

        public async Task<int> InvokeAsync()
        {
            var productsCount = await _productService.GetProductsCountAsync();

            return productsCount;
        }
    }
}
