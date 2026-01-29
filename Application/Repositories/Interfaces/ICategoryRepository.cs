using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface ICategoryRepository : IRepositoryBase<Category>
    {
        Task<IEnumerable<Category>> GetAllCategoriesAsync(bool trackChanges);
        Task<Category?> GetOneCategoryAsync(int id, bool trackChanges);
        Task<int> GetCategoriesCountAsync();
        Task<IEnumerable<Category>> GetParentCategoriesAsync(bool trackChanges);
        Task<IEnumerable<Category>> GetChildsOfOneCategoryAsync(int parentId, bool trackChanges);
        Task<int> CountBySlugAsync(string slug);
        void CreateCategory(Category category);
        void UpdateOneCategory(Category category);
        void DeleteOneCategory(Category category);
    }
}
