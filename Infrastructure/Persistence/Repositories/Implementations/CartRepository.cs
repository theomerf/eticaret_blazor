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

        public async Task<Cart?> GetByUserIdAsync(string? userId, bool trackChanges)
        {
            var cart = await FindByCondition(c => c.UserId == userId, trackChanges)
                .OfType<Cart>()
                .Include(c => c.Lines)
                .FirstOrDefaultAsync();

            return cart;
        }

        public async Task<int> CountOfLinesAsync(string userId) => await _context.CartLines.Where(cl => cl.Cart.UserId == userId).CountAsync();

        public async Task<int?> GetVersionAsync(string userId) 
        {
            var version = await FindByCondition(c => c.UserId == userId, false)
                .Select(c => c.Version)
                .FirstOrDefaultAsync();

            return version;
        }

        public void Create(Cart cart)
        {
            CreateEntity(cart);
        }

        public void Update(Cart cart)
        {
            UpdateEntity(cart);
        }
    }
}
