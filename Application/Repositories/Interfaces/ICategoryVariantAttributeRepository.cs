using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface ICategoryVariantAttributeRepository : IRepositoryBase<CategoryVariantAttribute>
    {
        Task<IEnumerable<CategoryVariantAttribute>> GetByCategoryIdAsync(int categoryId, bool trackChanges);
        Task<CategoryVariantAttribute?> GetByKeyAndCategoryIdAsync(string key, int categoryId, bool trackChanges);
        Task<bool> ExistsByKeyAsync(string key, int categoryId);
        void Create(CategoryVariantAttribute categoryVariantAttribute);
        void Delete(CategoryVariantAttribute categoryVariantAttribute);
    }
}
