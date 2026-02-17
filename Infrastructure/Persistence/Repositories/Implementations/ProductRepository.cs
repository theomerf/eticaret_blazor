using Application.DTOs;
using Application.Queries.RequestParameters;
using Application.Repositories.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Infrastructure.Persistence.Repositories.Implementations
{
    public sealed class ProductRepository : RepositoryBase<Product>, IProductRepository
    {

        public ProductRepository(RepositoryContext context) : base(context)
        {

        }

        public async Task<(IEnumerable<Product> products, int count)> GetAllAsync(ProductRequestParameters p, bool trackChanges)
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
                .AsSplitQuery()
                .Select(p => new Product
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Slug = p.Slug,
                    AverageRating = p.AverageRating,
                    ReviewCount = p.ReviewCount,
                    Brand = p.Brand,
                    CategoryId = p.CategoryId,
                    Variants = p.Variants
                        .OrderByDescending(v => v.IsDefault)
                        .Take(1)
                        .Select(pv => new ProductVariant
                        {
                            ProductVariantId = pv.ProductVariantId,
                            Images = pv.Images != null ? pv.Images.Where(pi => pi.IsPrimary).ToList() : new List<ProductImage>(),
                            Price = pv.Price,
                            Stock = pv.Stock,
                            DiscountPrice = pv.DiscountPrice,
                        })
                        .ToList()
                })
                .ToListAsync();

            return (filteredProducts, count);
        }

        public async Task<(IEnumerable<Product> products, int count, int showcaseCount)> GetAllAdminAsync(ProductRequestParametersAdmin p, bool trackChanges, CancellationToken ct = default)
        {
            var filteredProductsQuery = FindAll(trackChanges)
                .FilteredByCategoryId(p.CategoryId)
                .FilteredByBrand(p.Brand)
                .FilteredBySearchTerm(p.SearchTerm)
                .FilteredByPrice(p.MinPrice, p.MaxPrice, p.IsValidPrice)
                .FilteredByShowcase(p.IsShowCase)
                .FilteredByDiscount(p.IsDiscount);

            var count = await filteredProductsQuery.CountAsync(ct);
            var showcaseCount = await filteredProductsQuery.CountAsync(p => p.ShowCase == true, ct);

            var filteredProducts = await filteredProductsQuery
                .SortAdmin(p.SortEnum)
                .ToPaginate(p.PageNumber, p.PageSize)
                .AsSplitQuery()
                .Select(p => new Product
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Slug = p.Slug,
                    ShowCase = p.ShowCase,
                    AverageRating = p.AverageRating,
                    ReviewCount = p.ReviewCount,
                    Brand = p.Brand,
                    CategoryId = p.CategoryId,
                    Variants = p.Variants
                        .OrderByDescending(v => v.IsDefault)
                        .Take(1)
                        .Select(pv => new ProductVariant
                        {
                            ProductVariantId = pv.ProductVariantId,
                            Images = pv.Images != null ? pv.Images.Where(pi => pi.IsPrimary).ToList() : new List<ProductImage>(),
                            Price = pv.Price,
                            Stock = pv.Stock,
                            DiscountPrice = pv.DiscountPrice,
                        })
                        .ToList()
                })
                .ToListAsync(ct);

            return (filteredProducts, count, showcaseCount);
        }

        public async Task<int> CountAsync(CancellationToken ct) => await CountAsync(false, ct);

        public async Task<int> CountBySlugAsync(string slug)
        {
            return await FindByCondition(p => p.Slug == slug, false).CountAsync();
        }

        public async Task<Product?> GetByIdAsync(int productId, bool forUpdate, bool trackChanges)
        {
            if (forUpdate)
            {
                return await FindByCondition(p => p.ProductId == productId, trackChanges)
                    .Include(p => p.Variants)
                        .ThenInclude(v => v.Images)
                    .FirstOrDefaultAsync();
            }

            return await FindByCondition(p => p.ProductId == productId, trackChanges)
                    .Select(p => new Product
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        Slug = p.Slug,
                        CategoryId = p.CategoryId,
                        AverageRating = p.AverageRating,
                        ReviewCount = p.ReviewCount,
                        Brand = p.Brand,
                        Variants = p.Variants.Select(pv => new ProductVariant
                        {
                            ProductVariantId = pv.ProductVariantId,
                        }).ToList(),
                        ShowCase = p.ShowCase
                    })
                    .FirstOrDefaultAsync();
        }

        public async Task<ProductWithDetailsDto?> GetBySlugAsync(string slug, bool trackChanges)
        {
            var dbResult = await FindByCondition(p => p.Slug == slug, trackChanges)
                .Select(p => new
                {
                    p.ProductId,
                    p.ProductName,
                    p.Slug,
                    p.MetaTitle,
                    p.MetaDescription,
                    p.Summary,
                    p.LongDescription,
                    p.CategoryId,
                    p.AverageRating,
                    p.ReviewCount,
                    p.Brand,
                    p.ManufacturingCountry,
                    p.WarrantyInfo,
                    p.ShowCase,
                    p.SpecificationsJson,
                    VariantSelectors = p.Category!.VariantAttributes!
                        .Where(va => va.IsVariantDefiner)
                        .OrderBy(va => va.SortOrder)
                        .Select(va => new ProductVariantSelectorDto
                        {
                            Key = va.Key,
                            DisplayName = va.DisplayName,
                            Type = va.Type
                        })
                        .ToList(),
                    Variants = p.Variants
                        .OrderByDescending(v => v.IsDefault)
                        .Select(v => new
                        {
                            v.ProductVariantId,
                            v.IsDefault,
                            v.Stock,
                            v.Price,
                            v.DiscountPrice,
                            v.Color,
                            v.Size,
                            v.VariantSpecificationsJson
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (dbResult == null) return null;

            return new ProductWithDetailsDto
            {
                ProductId = dbResult.ProductId,
                ProductName = dbResult.ProductName,
                Slug = dbResult.Slug,
                MetaTitle = dbResult.MetaTitle,
                MetaDescription = dbResult.MetaDescription,
                Summary = dbResult.Summary,
                LongDescription = dbResult.LongDescription,
                CategoryId = dbResult.CategoryId,
                AverageRating = dbResult.AverageRating,
                ReviewCount = dbResult.ReviewCount,
                Brand = dbResult.Brand,
                ManufacturingCountry = dbResult.ManufacturingCountry,
                WarrantyInfo = dbResult.WarrantyInfo,
                ShowCase = dbResult.ShowCase,
                Specifications = JsonSerializer.Deserialize<List<ProductSpecificationDto>>(
                    dbResult.SpecificationsJson ?? "[]"
                ) ?? new List<ProductSpecificationDto>(),
                VariantSelectors = dbResult.VariantSelectors,
                Variants = dbResult.Variants.Select(v => new ProductVariantDto
                {
                    ProductVariantId = v.ProductVariantId,
                    IsDefault = v.IsDefault,
                    Stock = v.Stock,
                    Price = v.Price,
                    DiscountPrice = v.DiscountPrice,
                    Color = v.Color,
                    Size = v.Size,
                    VariantSpecifications = JsonSerializer.Deserialize<List<ProductSpecificationDto>>(
                        v.VariantSpecificationsJson ?? "[]"
                    ) ?? []
                }).ToList()
            };
        }

        public async Task<IEnumerable<Product>> GetRecommendationsAsync(bool trackChanges)
        {
            var placeholderIds = new List<int> { 1, 2, 3, 4, 5 };

            var productsQuery = FindAll(trackChanges)
                .Where(p => placeholderIds.Contains(p.ProductId));

            var products = await productsQuery
                .AsSplitQuery()
                .Select(p => new Product
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Slug = p.Slug,
                    AverageRating = p.AverageRating,
                    ReviewCount = p.ReviewCount,
                    Brand = p.Brand,
                    CategoryId = p.CategoryId,
                    Variants = p.Variants
                        .OrderByDescending(v => v.IsDefault)
                        .Take(1)
                        .Select(pv => new ProductVariant
                        {
                            ProductVariantId = pv.ProductVariantId,
                            Images = pv.Images != null ? pv.Images.Where(pi => pi.IsPrimary).ToList() : new List<ProductImage>(),
                            Price = pv.Price,
                            DiscountPrice = pv.DiscountPrice,
                        })
                        .ToList()
                })
                .ToListAsync();

            return products;
        }

        public async Task<IEnumerable<Product>> GetFavouritesAsync(ICollection<int> favouriteProductIds, bool trackChanges)
        {
            var products = await FindAllByCondition(p => favouriteProductIds.Contains(p.ProductId), trackChanges)
                .AsSplitQuery()
                .Select(p => new Product
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Slug = p.Slug,
                    AverageRating = p.AverageRating,
                    ReviewCount = p.ReviewCount,
                    Brand = p.Brand,
                    CategoryId = p.CategoryId,
                    Variants = p.Variants
                        .OrderByDescending(v => v.IsDefault)
                        .Take(1)
                        .Select(pv => new ProductVariant
                        {
                            ProductVariantId = pv.ProductVariantId,
                            Images = pv.Images != null ? pv.Images.Where(pi => pi.IsPrimary).ToList() : new List<ProductImage>(),
                            Price = pv.Price,
                            DiscountPrice = pv.DiscountPrice,
                        })
                        .ToList()
                })
                .OrderBy(p => p.ProductId)
                .ToListAsync();

            return products;
        }

        public async Task<IEnumerable<Product>> GetShowcaseListAsync(bool trackChanges, CancellationToken ct = default)
        {
            var products = await FindAll(trackChanges)
                .Where(p => p.ShowCase.Equals(true))
                .OrderBy(p => p.ProductId)
                .AsSplitQuery()
                .Select(p => new Product
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Slug = p.Slug,
                    AverageRating = p.AverageRating,
                    ReviewCount = p.ReviewCount,
                    Brand = p.Brand,
                    CategoryId = p.CategoryId,
                    Variants = p.Variants
                        .OrderByDescending(v => v.IsDefault)
                        .Take(1)
                        .Select(pv => new ProductVariant
                        {
                            ProductVariantId = pv.ProductVariantId,
                            Images = pv.Images != null ? pv.Images.Where(pi => pi.IsPrimary).ToList() : new List<ProductImage>(),
                            Price = pv.Price,
                            DiscountPrice = pv.DiscountPrice,
                        })
                        .ToList()
                })
               .ToListAsync(ct);

            return products;
        }

        public async Task<IEnumerable<Product>> GetLatestAsync(int count, bool trackChanges)
        {
            var products = await FindAll(trackChanges)
                .OrderByDescending(prd => prd.ProductId)
                .Take(count)
                .ToListAsync();

            return products;
        }
        
        public void CreateImage(ProductImage productImage)
        {
            _context.ProductImages.Add(productImage);
        }

        public void DeleteImage(ProductImage productImage)
        {
            _context.ProductImages.Remove(productImage);
        }

        public void Create(Product product) => CreateEntity(product);

        public void Update(Product product) => UpdateEntity(product);

        public void Delete(Product product) => RemoveEntity(product);
    }
}