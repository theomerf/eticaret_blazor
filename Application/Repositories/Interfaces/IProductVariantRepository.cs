using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface IProductVariantRepository
    {
        Task<ProductVariant?> GetByIdAsync(int productVariantId, bool trackChanges);
        Task<IEnumerable<ProductVariant>> GetByProductIdAsync(int productId, bool trackChanges);
        void Create(ProductVariant variant);
        void Update(ProductVariant variant);
        void Delete(ProductVariant variant);
    }
}
