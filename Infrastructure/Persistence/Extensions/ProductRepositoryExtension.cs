using Application.Queries.RequestParameters;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Infrastructure.Persistence.Extensions
{
    public static class ProductRepositoryExtension
    {
        public static IQueryable<Product> FilteredByCategoryId(this IQueryable<Product> products, int? categoryId)
        {
            if (categoryId == null)
                return products;

            return products.Where(prd => prd.CategoryId == categoryId);
        }

        public static IQueryable<Product> FilteredBySearchTerm(this IQueryable<Product> products, string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return products;

            return products
                .Where(p => p.SearchVector.Matches(EF.Functions.PlainToTsQuery("turkish", searchTerm)))
                .OrderByDescending(p => p.SearchVector.Rank(EF.Functions.PlainToTsQuery("turkish", searchTerm)));
        }

        public static IQueryable<Product> FilteredByBrand(this IQueryable<Product> products, string? brand)
        {
            if (string.IsNullOrWhiteSpace(brand))
                return products;

            return products.Where(prd => prd.Brand!.ToLower()
            .Contains(brand.ToLower()));
        }

        public static IQueryable<Product> FilteredByPrice(this IQueryable<Product> products, int? minPrice, int? maxPrice, bool? isValidPrice)
        {
            if ((minPrice == null && maxPrice == null) || isValidPrice == false)
                return products;

            if (minPrice == null)
                minPrice = 0;

            if (maxPrice == null)
                maxPrice = int.MaxValue;

            return products.Where(prd => prd.Variants.Any(v => (v.DiscountPrice ?? v.Price) >= minPrice && (v.DiscountPrice ?? v.Price) <= maxPrice));
        }

        public static IQueryable<Product> FilteredByShowcase(this IQueryable<Product> products, bool? isShowCase)
        {
            if (isShowCase == null || isShowCase == false)
                return products;

            return products.Where(prd => prd.ShowCase == true);
        }

        public static IQueryable<Product> FilteredByDiscount(this IQueryable<Product> products, bool? isDiscount)
        {
            if (isDiscount == null || isDiscount == false)
                return products;

            return products.Where(prd => prd.Variants.Any(v => v.DiscountPrice != null));
        }

        public static IQueryable<Product> ToPaginate(this IQueryable<Product> query, int pageNumber, int pageSize)
        {
            return query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);
        }

        public static IQueryable<Product> Sort(this IQueryable<Product> products, ProductSort sort, ProductRequestParameters request)
        {
            return sort switch
            {
                ProductSort.PriceAsc => products
                    .WhereIf(request.CursorPrice.HasValue, p =>
                        p.Variants.Min(v => (decimal?)(v.DiscountPrice ?? v.Price)) > request.CursorPrice ||
                        (p.Variants.Min(v => (decimal?)(v.DiscountPrice ?? v.Price)) == request.CursorPrice && p.ProductId > request.CursorId))
                    .OrderBy(p => p.Variants.Min(v => (decimal?)(v.DiscountPrice ?? v.Price)))
                    .ThenBy(p => p.ProductId),

                ProductSort.PriceDesc => products
                    .WhereIf(request.CursorPrice.HasValue, p =>
                        p.Variants.Min(v => (decimal?)(v.DiscountPrice ?? v.Price)) < request.CursorPrice ||
                        (p.Variants.Min(v => (decimal?)(v.DiscountPrice ?? v.Price)) == request.CursorPrice && p.ProductId < request.CursorId))
                    .OrderByDescending(p => p.Variants.Min(v => (decimal?)(v.DiscountPrice ?? v.Price)))
                    .ThenByDescending(p => p.ProductId),

                ProductSort.TopReviews => products
                    .WhereIf(request.CursorRating.HasValue, p =>
                        p.AverageRating < request.CursorRating ||
                        (p.AverageRating == request.CursorRating && p.ProductId < request.CursorId))
                    .OrderByDescending(p => p.AverageRating)
                    .ThenByDescending(p => p.ProductId),

                ProductSort.MostReviews => products
                    .WhereIf(request.CursorReviewCount.HasValue, p =>
                        p.ReviewCount < request.CursorReviewCount ||
                        (p.ReviewCount == request.CursorReviewCount && p.ProductId < request.CursorId))
                    .OrderByDescending(p => p.ReviewCount)
                    .ThenByDescending(p => p.ProductId),

                _ => products
                    .WhereIf(request.CursorId.HasValue, p => p.ProductId > request.CursorId)
                    .OrderBy(p => p.ProductId)
            };
        }

        public static IQueryable<Product> SortAdmin(this IQueryable<Product> products, ProductSort sort)
        {
            return sort switch
            {
                ProductSort.PriceAsc => products.OrderBy(prd => prd.Variants.Min(v => v.DiscountPrice ?? v.Price)),
                ProductSort.PriceDesc => products.OrderByDescending(prd => prd.Variants.Min(v => v.DiscountPrice ?? v.Price)),
                ProductSort.TopReviews => products.OrderByDescending(prd => prd.AverageRating),
                ProductSort.MostReviews => products.OrderByDescending(prd => prd.ReviewCount),
                _ => products.OrderBy(prd => prd.ProductId),
            };
        }
    }
}