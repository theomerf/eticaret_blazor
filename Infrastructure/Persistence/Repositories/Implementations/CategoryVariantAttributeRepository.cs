using Application.Repositories.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Implementations
{
    public class CategoryVariantAttributeRepository : RepositoryBase<CategoryVariantAttribute>, ICategoryVariantAttributeRepository
    {
        public CategoryVariantAttributeRepository(RepositoryContext context) : base(context)
        {
        }

        public async Task<IEnumerable<CategoryVariantAttribute>> GetByCategoryIdAsync(int categoryId, bool trackChanges)
        {
            return await FindByCondition(a => a.CategoryId == categoryId, trackChanges)
                .OrderBy(a => a.SortOrder)
                .ToListAsync();
        }

        public async Task<CategoryVariantAttribute?> GetByKeyAndCategoryIdAsync(string key, int categoryId, bool trackChanges)
        {
            return await FindByCondition(a => a.Key == key && a.CategoryId == categoryId, trackChanges)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> ExistsByKeyAsync(string key, int categoryId)
        {
            return await FindByCondition(a => a.Key == key && a.CategoryId == categoryId, false)
                .AnyAsync();
        }

        public void Create(CategoryVariantAttribute categoryVariantAttribute) => CreateEntity(categoryVariantAttribute);

        public void Delete(CategoryVariantAttribute categoryVariantAttribute) => RemoveEntity(categoryVariantAttribute);
    }
}
