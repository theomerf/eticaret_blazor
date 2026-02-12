using Application.Repositories.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Implementations
{
    public class ProductVariantRepository : RepositoryBase<ProductVariant>, IProductVariantRepository
    {
        public ProductVariantRepository(RepositoryContext context) : base(context)
        {
        }

        public async Task<ProductVariant?> GetByIdAsync(int productVariantId, bool trackChanges)
        {
            return await FindByCondition(v => v.ProductVariantId == productVariantId, trackChanges)
                .Include(v => v.Images)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<ProductVariant>> GetByProductIdAsync(int productId, bool trackChanges)
        {
            return await FindByCondition(v => v.ProductId == productId, trackChanges)
                .OrderBy(v => v.ProductVariantId)
                .ToListAsync();
        }

        public void Create(ProductVariant variant) => CreateEntity(variant);

        public void Update(ProductVariant variant) => UpdateEntity(variant);

        public void Delete(ProductVariant variant) => RemoveEntity(variant);
    }
}
