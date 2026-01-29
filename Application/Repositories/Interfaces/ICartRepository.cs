using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface ICartRepository : IRepositoryBase<Cart>
    {
        void CreateCart(Cart cart);
        void UpdateCart(Cart cart);
        Task<Cart?> GetCartByUserIdAsync(string? userId, bool trackChanges);
        Task<int> GetCartLinesCountAsync(string userId);
        Task<int?> GetCartVersionAsync(string userId);
    }
}
