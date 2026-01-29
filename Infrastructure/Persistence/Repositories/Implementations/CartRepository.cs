using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Application.Repositories.Interfaces;

namespace Infrastructure.Persistence.Repositories.Implementations
{
    public sealed class CartRepository : RepositoryBase<Cart>, ICartRepository
    {
        public CartRepository(RepositoryContext context) : base(context)
        {
        }

        public void CreateCart(Cart cart)
        {
            Create(cart);
        }

        public void UpdateCart(Cart cart)
        {
            Update(cart);
        }

        public async Task<Cart?> GetCartByUserIdAsync(string? userId, bool trackChanges)
        {
            var cart = await FindByCondition(c => c.UserId == userId, trackChanges)
                .OfType<Cart>()
                .Include(c => c.Lines)
                .FirstOrDefaultAsync();

            return cart;
        }

        public async Task<int> GetCartLinesCountAsync(string userId) => await _context.CartLines.Where(cl => cl.Cart.UserId == userId).CountAsync();

        public async Task<int?> GetCartVersionAsync(string userId) 
        {
            var version = await FindByCondition(c => c.UserId == userId, false)
                .Select(c => c.Version)
                .FirstOrDefaultAsync();

            return version;
        } 
    }
}
