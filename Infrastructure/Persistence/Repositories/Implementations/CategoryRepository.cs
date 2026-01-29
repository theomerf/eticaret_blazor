using Application.Repositories.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Implementations
{
    public sealed class CategoryRepository : RepositoryBase<Category>, ICategoryRepository
    {
        public CategoryRepository(RepositoryContext context) : base(context)
        {

        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync(bool trackChanges)
        {
            var categories = await FindAll(trackChanges)
                .ToListAsync();

            return categories;  
        }

        public async Task<int> GetCategoriesCountAsync() => await CountAsync(false);

        public async Task<Category?> GetOneCategoryAsync(int id, bool trackChanges)
        {
            var category = await FindByCondition(p => p.CategoryId.Equals(id), trackChanges)
                .FirstOrDefaultAsync();

            return category;
        }

        public async Task<IEnumerable<Category>> GetParentCategoriesAsync(bool trackChanges)
        {
            var categories = await FindAll(trackChanges)
                .Where(c => c.ParentId == null)
                .ToListAsync();

            return categories;
        }

        public async Task<IEnumerable<Category>> GetChildsOfOneCategoryAsync(int parentId, bool trackChanges)
        {
            var categories = await FindAll(trackChanges)
                .Where(c => c.ParentId == parentId)
                .ToListAsync();

            return categories;
        }

        public async Task<int> CountBySlugAsync(string slug)
        {
            return await FindByCondition(c => c.Slug == slug, false).CountAsync();
        }

        public void CreateCategory(Category category)
        {
            Create(category);
        }

        public void DeleteOneCategory(Category category)
        {
            Remove(category);
        }

        public void UpdateOneCategory(Category category)
        {
            Update(category);
        }
    }
}
