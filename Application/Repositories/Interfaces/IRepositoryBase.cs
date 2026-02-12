using System.Linq.Expressions;

namespace Application.Repositories.Interfaces
{
    public interface IRepositoryBase<T>{
        IQueryable<T> FindAll(bool trackChanges);
        IQueryable<T?> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges);
        IQueryable<T> FindAllByCondition(Expression<Func<T, bool>> expression, bool trackChanges);
        void CreateEntity(T entity);
        void RemoveEntity(T entity);
        void UpdateEntity(T entity);
        void UpdateRange(IEnumerable<T> entities);
        void RemoveRange(IEnumerable<T> entities);
        int Count(bool trackChanges);
        Task<int> CountAsync(bool trackChanges, CancellationToken ct = default);
    }
}