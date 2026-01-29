using Application.DTOs;
using Application.Services.Interfaces;
using Domain.Entities;
using ETicaret.Components.Pages.Admin;
using Infrastructure.Persistence;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ETicaret.Extensions
{
    public class ImportFromCsvExtension
    {
        private readonly RepositoryContext _context;
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly ILogger<ImportFromCsvExtension> _logger;

        public ImportFromCsvExtension(RepositoryContext context, IProductService productService, ICategoryService categoryService, ILogger<ImportFromCsvExtension> logger)
        {
            _context = context;
            _productService = productService;
            _categoryService = categoryService;
            _logger = logger;
        }

        public async Task ImportCategoriesFromCsv(string csvFilePath)
        {
            if (_context.Categories.Any())
            {
                return;
            }
            var categories = new List<CategoryDtoForCreation>();
            var lines = File.ReadAllLines(csvFilePath, Encoding.UTF8);

            for (int i = 1; i < lines.Length; i++)
            {
                var columns = lines[i].Split(';');

                var category = new CategoryDtoForCreation()
                {
                    CategoryName = columns[1].Trim(),
                    ParentId = columns[2] == "null" ? null : int.TryParse(columns[2], out var categoryId) ? categoryId : 0,
                    Description = columns[3].Trim(),
                };

                categories.Add(category);
            }

            foreach (var category in categories)
            {
                await _categoryService.CreateCategoryAsync(category);
            }

            _logger.LogInformation("Categories imported successfully from CSV.");
        }

        public async Task ImportProductsFromCsv(string csvFilePath)
        {
            if (_context.Products.Any())
            {
                return;
            }
            var products = new List<ProductDtoForCreation>();
            var productImages = new List<ProductImageDtoForCreation>();
            var lines = File.ReadAllLines(csvFilePath, Encoding.UTF8);

            for (int i = 1; i < lines.Length; i++)
            {
                var columns = lines[i].Split(';');

                var product = new ProductDtoForCreation()
                {
                    ProductName = columns[0].Trim(),
                    CategoryId = int.TryParse(columns[1], out var categoryId) ? categoryId : 0,
                    ActualPrice = decimal.TryParse(columns[4], out var actualPrice) ? actualPrice : 0,
                    DiscountPrice = decimal.TryParse(columns[3], out var discountPrice) ? discountPrice : 0,
                    Summary = columns[5].Trim(),
                    ShowCase = columns[6] == "true" ? true : false,
                };

                var productImage = new ProductImageDtoForCreation()
                {
                    ProductId = i,
                    ImageUrl = $"/images/products/{columns[2].Trim()}",
                    IsPrimary = true,
                    DisplayOrder = 1
                };

                products.Add(product);
                productImages.Add(productImage);
            }

            foreach (var product in products)
            {
                await _productService.CreateProductAsync(product);
            }

            foreach (var productImage in productImages)
            {
                await _productService.UpdateProductImagesAsync(new List<ProductImageDtoForCreation> { productImage });
            }

            _logger.LogInformation("Products imported successfully from CSV.");
        }
    }
}
