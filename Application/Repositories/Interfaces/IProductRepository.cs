using Application.Queries.RequestParameters;
using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface IProductRepository : IRepositoryBase<Product>
    {
        Task<(IEnumerable<Product> products, int count)> GetAllProductsAsync(ProductRequestParameters p, bool trackChanges);
        Task<(IEnumerable<Product> products, int count)> GetAllProductsAdminAsync(ProductRequestParametersAdmin p, bool trackChanges);
        Task<int> GetProductsCountAsync();
        Task<Product?> GetOneProductAsync(int id, bool trackChanges);
        Task<Product?> GetOneProductBySlugAsync(string slug, bool trackChanges);
        Task<IEnumerable<Product>> GetRecommendedProductsAsync(bool trackChanges);
        Task<IEnumerable<Product>> GetFavouriteProductsAsync(ICollection<int> favouriteProductIds, bool trackChanges);
        Task<IEnumerable<Product>> GetShowcaseProductsAsync(bool trackChanges);
        Task<IEnumerable<Product>> GetLastestProductsAsync(int n, bool trackChanges);
        Task<int> CountBySlugAsync(string slug);
        void CreateProductImage(ProductImage productImage);
        void CreateProduct(Product product);
        void DeleteProduct(Product product);
        void UpdateProduct(Product entity);
    }
}