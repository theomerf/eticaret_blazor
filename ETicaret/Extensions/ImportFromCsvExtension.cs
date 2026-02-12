using Application.DTOs;
using Application.Services.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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
                await _categoryService.CreateAsync(category);
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
                    Summary = columns[5].Trim(),
                    ShowCase = columns[6].Trim() == "true",
                    Variants = GenerateMockVariants(
                        decimal.TryParse(columns[4].Trim(), out var actualPrice) ? actualPrice : 0,
                        decimal.TryParse(columns[3].Trim(), out var discountPrice) && discountPrice > 0 ? discountPrice : null
                    )
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

            for (int i = 0; i < products.Count; i++)
            {
                var product = products[i];
                var createdResult = await _productService.CreateAsync(product);

                if (createdResult.IsSuccess)
                {
                    var createdProduct = await _context.Products
                        .Include(p => p.Variants)
                        .FirstOrDefaultAsync(p => p.ProductId == createdResult.Data!.ProductId);

                    if (createdProduct == null) continue;



                    if (i < productImages.Count)
                    {
                        var baseImage = productImages[i];

                        foreach (var variant in createdProduct.Variants)
                        {
                            var imageDto = new ProductImageDtoForCreation
                            {
                                ProductId = createdResult.Data!.ProductId,
                                ProductVariantId = variant.ProductVariantId,
                                ImageUrl = baseImage.ImageUrl,
                                IsPrimary = true,
                                DisplayOrder = 1
                            };

                            await _productService.UpdateImagesAsync(new List<ProductImageDtoForCreation> { imageDto });
                        }
                    }

                    await EnsureCategoryVariantAttributesAsync(createdResult.Data!.ProductId);
                }
            }

            _logger.LogInformation("Products imported successfully from CSV.");
        }

        private async Task EnsureCategoryVariantAttributesAsync(int productId)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product?.Category == null) return;

            var categoryId = product.Category.CategoryId;

            var renkAttr = await _context.CategoryVariantAttributes
                .FirstOrDefaultAsync(a => a.CategoryId == categoryId && a.Key == "Renk");

            if (renkAttr == null)
            {
                _context.CategoryVariantAttributes.Add(new Domain.Entities.CategoryVariantAttribute
                {
                    CategoryId = categoryId,
                    Key = "Renk",
                    DisplayName = "Renk",
                    Type = Domain.Entities.VariantAttributeType.Color,
                    IsVariantDefiner = true,
                    IsTechnicalSpec = false,
                    SortOrder = 1,
                    IsRequired = true
                });
            }
            else if (renkAttr.Type != Domain.Entities.VariantAttributeType.Color)
            {
                renkAttr.Type = Domain.Entities.VariantAttributeType.Color;
            }

            var bedenAttr = await _context.CategoryVariantAttributes
                .FirstOrDefaultAsync(a => a.CategoryId == categoryId && a.Key == "Beden");

            if (bedenAttr == null)
            {
                _context.CategoryVariantAttributes.Add(new Domain.Entities.CategoryVariantAttribute
                {
                    CategoryId = categoryId,
                    Key = "Beden",
                    DisplayName = "Beden",
                    Type = Domain.Entities.VariantAttributeType.Select,
                    IsVariantDefiner = true,
                    IsTechnicalSpec = false,
                    SortOrder = 2,
                    IsRequired = true
                });
            }

            await _context.SaveChangesAsync();
        }

        private List<ProductVariantDtoForCreation> GenerateMockVariants(decimal price, decimal? discountPrice)
        {
            var variants = new List<ProductVariantDtoForCreation>();
            var colors = new[] { "Siyah", "Beyaz", "Mavi", "Kırmızı" };
            var sizes = new[] { "S", "M", "L", "XL" };
            var random = new Random();

            var selectedColors = colors.OrderBy(x => random.Next()).Take(random.Next(2, 4)).ToList();
            var selectedSizes = sizes.OrderBy(x => random.Next()).Take(random.Next(2, 4)).ToList();

            bool isFirstVariant = true;
            foreach (var color in selectedColors)
            {
                foreach (var size in selectedSizes)
                {
                    var variant = new ProductVariantDtoForCreation
                    {
                        Color = color,
                        Size = size,
                        Stock = random.Next(5, 50),
                        Price = price,
                        DiscountPrice = discountPrice,
                        IsActive = true,
                        IsDefault = isFirstVariant,
                        Sku = $"MOCK-{color.Substring(0, 1)}-{size}-{random.Next(1000, 9999)}"
                    };

                    isFirstVariant = false;

                    variant.VariantSpecifications.Add(new ProductSpecificationDto { Key = "Renk", Value = color });
                    variant.VariantSpecifications.Add(new ProductSpecificationDto { Key = "Beden", Value = size });

                    variants.Add(variant);
                }
            }

            return variants;
        }
    }
}