using Application.Queries.RequestParameters;
using Application.Repositories.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Implementations
{
    public sealed class CategoryRepository : RepositoryBase<Category>, ICategoryRepository
    {
        public CategoryRepository(RepositoryContext context) : base(context)
        {

        }

        public async Task<IEnumerable<Category>> GetAllAsync(bool trackChanges)
        {
            var categories = await FindAll(trackChanges)
                .Select(c => new Category
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    Slug = c.Slug,
                    ParentId = c.ParentId,
                    DisplayOrder = c.DisplayOrder,
                    IsFeatured = c.IsFeatured,
                })
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            return categories;  
        }

        public async Task<(IEnumerable<Category> categories, int count)> GetAllAdminAsync(RequestParametersAdmin p, bool trackChanges, CancellationToken ct)
        {
            var filteredCategoriesQuery = FindAll(trackChanges)
                .FilterBy(p.SearchTerm, c => c.CategoryName, FilterOperator.Contains);

            var count = await filteredCategoriesQuery.CountAsync(ct);

            var filteredCategories = await filteredCategoriesQuery
                .ToPaginate(p.PageNumber, p.PageSize)
                .OrderBy(c => c.CategoryId)
                .Select(c => new Category
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    Slug = c.Slug,
                    ParentId = c.ParentId,
                    IconUrl = c.IconUrl,
                    DisplayOrder = c.DisplayOrder,
                    IsFeatured = c.IsFeatured,
                })
                .ToListAsync(ct);

            return (filteredCategories, count);
        }

        public async Task<int> CountAsync(CancellationToken ct = default) => await CountAsync(false, ct);

        public async Task<Category?> GetByIdAsync(int categoryId, bool trackChanges)
        {
            var category = await FindByCondition(p => p.CategoryId.Equals(categoryId), trackChanges)
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync();

            return category;
        }

        public async Task<IEnumerable<Category>> GetParentsAsync(bool trackChanges)
        {
            var categories = await FindAll(trackChanges)
                .Where(c => c.ParentId == null)
                .ToListAsync();

            return categories;
        }

        public async Task<IEnumerable<Category>> GetChildrenByIdAsync(int parentCategoryId, bool trackChanges)
        {
            var categories = await FindAll(trackChanges)
                .Where(c => c.ParentId == parentCategoryId)
                .ToListAsync();

            return categories;
        }

        public async Task<int> CountBySlugAsync(string slug)
        {
            return await FindByCondition(c => c.Slug == slug, false).CountAsync();
        }

        public void Create(Category category)
        {
            CreateEntity(category);
        }

        public void Delete(Category category)
        {
            RemoveEntity(category);
        }

        public void Update(Category category)
        {
            UpdateEntity(category);
        }
    }
}
