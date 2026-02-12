using Application.DTOs;
using Application.Queries.RequestParameters;
using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface IProductRepository : IRepositoryBase<Product>
    {
        Task<(IEnumerable<Product> products, int count)> GetAllAsync(ProductRequestParameters p, bool trackChanges);
        Task<(IEnumerable<Product> products, int count)> GetAllAdminAsync(ProductRequestParametersAdmin p, bool trackChanges, CancellationToken ct = default);
        Task<int> CountAsync(CancellationToken ct = default);
        Task<int> CountBySlugAsync(string slug);
        Task<Product?> GetByIdAsync(int productId, bool trackChanges);
        Task<ProductWithDetailsDto?> GetBySlugAsync(string slug, bool trackChanges);
        Task<IEnumerable<Product>> GetRecommendationsAsync(bool trackChanges);
        Task<IEnumerable<Product>> GetFavouritesAsync(ICollection<int> favouriteProductIds, bool trackChanges);
        Task<IEnumerable<Product>> GetShowcaseListAsync(bool trackChanges, CancellationToken ct = default);
        Task<IEnumerable<Product>> GetLatestAsync(int count, bool trackChanges);
        void CreateImage(ProductImage productImage);
        void DeleteImage(ProductImage productImage);
        void Create(Product product);
        void Delete(Product product);
        void Update(Product product);
    }
}