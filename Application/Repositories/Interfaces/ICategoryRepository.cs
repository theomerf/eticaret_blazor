using Application.Queries.RequestParameters;
using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface ICategoryRepository : IRepositoryBase<Category>
    {
        Task<IEnumerable<Category>> GetAllAsync(bool trackChanges);
        Task<(IEnumerable<Category> categories, int count)> GetAllAdminAsync(RequestParametersAdmin p, bool trackChanges, CancellationToken ct);
        Task<Category?> GetByIdAsync(int categoryId, bool trackChanges);
        Task<int> CountAsync(CancellationToken ct = default);
        Task<int> CountBySlugAsync(string slug);
        Task<IEnumerable<Category>> GetParentsAsync(bool trackChanges);
        Task<IEnumerable<Category>> GetChildrenByIdAsync(int parentCategoryId, bool trackChanges);
        void Create(Category category);
        void Update(Category category);
        void Delete(Category category);
    }
}
