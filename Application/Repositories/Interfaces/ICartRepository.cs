using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface ICartRepository : IRepositoryBase<Cart>
    {
        Task<Cart?> GetByUserIdAsync(string? userId, bool trackChanges);
        Task<int> CountOfLinesAsync(string userId);
        Task<int?> GetVersionAsync(string userId);
        void Create(Cart cart);
        void Update(Cart cart);
    }
}
