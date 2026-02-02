using Application.DTOs;
using Application.Services.Interfaces;
using Infrastructure.Persistence;
using System.Text;

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
                var line = lines[i].Trim();
                if (line.StartsWith("\"") && line.EndsWith("\""))
                {
                    line = line.Substring(1, line.Length - 2);
                }

                var columns = line.Split(';');

                var category = new CategoryDtoForCreation()
                {
                    CategoryName = columns[1].Trim(),
                    ParentId = columns[2].Trim() == "null" ? null : int.TryParse(columns[2].Trim(), out var categoryId) ? categoryId : 0,
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
                var line = lines[i].Trim();
                if (line.StartsWith("\"") && line.EndsWith("\""))
                {
                    line = line.Substring(1, line.Length - 2);
                }

                var columns = line.Split(';');

                var product = new ProductDtoForCreation()
                {
                    ProductName = columns[0].Trim(),
                    CategoryId = int.TryParse(columns[1].Trim(), out var categoryId) ? categoryId : 0,
                    ActualPrice = decimal.TryParse(columns[4].Trim(), out var actualPrice) ? actualPrice : 0,
                    DiscountPrice = decimal.TryParse(columns[3].Trim(), out var discountPrice) ? discountPrice : 0,
                    Summary = columns[5].Trim(),
                    ShowCase = columns[6].Trim() == "true",
                    Stock = 30
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