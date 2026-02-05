using Application.Queries.RequestParameters;
using Application.Repositories.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Infrastructure.Persistence.Repositories.Implementations
{
    public sealed class ProductRepository : RepositoryBase<Product>, IProductRepository
    {

        public ProductRepository(RepositoryContext context) : base(context)
        {

        }

        public async Task<(IEnumerable<Product> products, int count)> GetAllProductsAsync(ProductRequestParameters p, bool trackChanges)
        {
            var filteredProductsQuery = FindAll(trackChanges)
                .FilteredByCategoryId(p.CategoryId)
                .FilteredByBrand(p.Brand)
                .FilteredBySearchTerm(p.SearchTerm)
                .FilteredByPrice(p.MinPrice, p.MaxPrice, p.IsValidPrice)
                .FilteredByShowcase(p.IsShowCase)
                .FilteredByDiscount(p.IsDiscount);

            var count = await filteredProductsQuery.CountAsync();

            // A) RELOAD DURUMU (page > 1, cursor yok)
            if (p.Page > 1 && !p.CursorId.HasValue)
            {
                filteredProductsQuery = filteredProductsQuery
                    .Sort(p.SortEnum, new ProductRequestParameters())
                    .Skip((p.Page - 1) * p.PageSize)
                    .Take(p.PageSize);
            }
            // B) INFINITE SCROLL (cursor var)
            else if (p.CursorId.HasValue)
            {
                filteredProductsQuery = filteredProductsQuery
                    .Sort(p.SortEnum, p)
                    .Take(p.PageSize);
            }
            // C) İLK SAYFA (page=1, cursor yok)
            else
            {
                filteredProductsQuery = filteredProductsQuery
                    .Sort(p.SortEnum, new ProductRequestParameters())
                    .Take(p.PageSize);
            }

            var filteredProducts = await filteredProductsQuery
                .Select(p => new Product
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Slug = p.Slug,
                    Images = new List<ProductImage>
                    {
                    p.Images != null && p.Images.Any(pi => pi.IsPrimary)
                    ? p.Images.First(pi => pi.IsPrimary)
                    : new ProductImage()
                    },
                    ActualPrice = p.ActualPrice,
                    DiscountPrice = p.DiscountPrice,
                    ShowCase = p.ShowCase,
                    Stock = p.Stock,
                    AverageRating = p.AverageRating,
                    ReviewCount = p.ReviewCount,
                    Brand = p.Brand,
                    CategoryId = p.CategoryId
                })
                .ToListAsync();

            return (filteredProducts, count);
        }

        public async Task<(IEnumerable<Product> products, int count)> GetAllProductsAdminAsync(ProductRequestParametersAdmin p, bool trackChanges)
        {
            var filteredProductsQuery = FindAll(trackChanges)
                .FilteredByCategoryId(p.CategoryId)
                .FilteredByBrand(p.Brand)
                .FilteredBySearchTerm(p.SearchTerm)
                .FilteredByPrice(p.MinPrice, p.MaxPrice, p.IsValidPrice)
                .FilteredByShowcase(p.IsShowCase)
                .FilteredByDiscount(p.IsDiscount);

            var count = await filteredProductsQuery.CountAsync();

            var filteredProducts = await filteredProductsQuery
                .SortAdmin(p.SortEnum)
                .ToPaginate(p.PageNumber, p.PageSize)
                .Select(p => new Product
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Slug = p.Slug,
                    Images = new List<ProductImage>
                    {
                        p.Images != null ? p.Images.Where(pi => pi.IsPrimary).FirstOrDefault()! : new ProductImage()
                    },
                    ActualPrice = p.ActualPrice,
                    DiscountPrice = p.DiscountPrice,
                    ShowCase = p.ShowCase,
                    Stock = p.Stock,
                    AverageRating = p.AverageRating,
                    ReviewCount = p.ReviewCount,
                    Brand = p.Brand,
                    CategoryId = p.CategoryId
                })
                .ToListAsync();

            return (filteredProducts, count);
        }

        public async Task<int> GetProductsCountAsync() => await CountAsync(false);

        public async Task<Product?> GetOneProductAsync(int id, bool trackChanges)
        {
            var product = await FindByCondition(p => p.ProductId.Equals(id), trackChanges)
                .Include(p => p.Images)
                .FirstOrDefaultAsync();

            return product;
        }

        public async Task<Product?> GetOneProductBySlugAsync(string slug, bool trackChanges)
        {
            var product = await FindByCondition(p => p.Slug.Equals(slug), trackChanges)
                .Include(p => p.Images)
                .FirstOrDefaultAsync();

            return product;
        }

        public async Task<IEnumerable<Product>> GetRecommendedProductsAsync(bool trackChanges)
        {
            var placeholderIds = new List<int> { 1, 2, 3, 4, 5 };

            var productsQuery = FindAll(trackChanges)
                .Where(p => placeholderIds.Contains(p.ProductId));

            var products = await productsQuery
                .Select(p => new Product
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Slug = p.Slug,
                    Images = new List<ProductImage>
                    {
                    p.Images != null && p.Images.Any(pi => pi.IsPrimary)
                    ? p.Images.First(pi => pi.IsPrimary)
                    : new ProductImage()
                    },
                    ActualPrice = p.ActualPrice,
                    DiscountPrice = p.DiscountPrice,
                    ShowCase = p.ShowCase,
                    Stock = p.Stock,
                    AverageRating = p.AverageRating,
                    ReviewCount = p.ReviewCount,
                    Brand = p.Brand,
                    CategoryId = p.CategoryId
                })
                .ToListAsync();

            return products;
        }

        public async Task<IEnumerable<Product>> GetFavouriteProductsAsync(ICollection<int> favouriteProductIds, bool trackChanges)
        {
            var products = await FindAllByCondition(p => favouriteProductIds.Contains(p.ProductId), trackChanges)
                .Select(p => new Product
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Slug = p.Slug,
                    Images = new List<ProductImage>
                    {
                        p.Images != null ? p.Images.Where(pi => pi.IsPrimary).FirstOrDefault()! : new ProductImage()
                    },
                    DiscountPrice = p.DiscountPrice,
                    ActualPrice = p.ActualPrice,
                    Stock = p.Stock,
                    AverageRating = p.AverageRating,
                    ReviewCount = p.ReviewCount,
                })
                .OrderBy(p => p.ProductId)
                .ToListAsync();

            return products;
        }

        public async Task<IEnumerable<Product>> GetShowcaseProductsAsync(bool trackChanges)
        {
            var products = await FindAll(trackChanges)
               .Where(p => p.ShowCase.Equals(true))
               .OrderBy(p => p.ProductId)
                .Select(p => new Product
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Slug = p.Slug,
                    Images = new List<ProductImage>
                    {
                        p.Images != null ? p.Images.Where(pi => pi.IsPrimary).FirstOrDefault()! : new ProductImage()
                    },
                    ActualPrice = p.ActualPrice,
                    DiscountPrice = p.DiscountPrice,
                    ShowCase = p.ShowCase,
                    Stock = p.Stock,
                    AverageRating = p.AverageRating,
                    ReviewCount = p.ReviewCount,
                    Brand = p.Brand,
                    CategoryId = p.CategoryId
                })
               .ToListAsync();

            return products;
        }

        public async Task<IEnumerable<Product>> GetLastestProductsAsync(int n, bool trackChanges)
        {
            var products = await FindAll(trackChanges)
                .OrderByDescending(prd => prd.ProductId)
                .Take(n)
                .ToListAsync();

            return products;
        }
        
        public async Task<int> CountBySlugAsync(string slug)
        {
            return await FindByCondition(p => p.Slug == slug, false).CountAsync();
        }

        public void CreateProductImage(ProductImage productImage)
        {
            _context.ProductImages.Add(productImage);
        }

        public void CreateProduct(Product product) => Create(product);

        public void UpdateProduct(Product entity) => Update(entity);

        public void DeleteProduct(Product product) => Remove(product);
    }
}