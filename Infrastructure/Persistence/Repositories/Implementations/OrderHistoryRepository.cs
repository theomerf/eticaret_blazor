using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Application.Repositories.Interfaces;

namespace Infrastructure.Persistence.Repositories.Implementations
{
    public sealed class OrderHistoryRepository : RepositoryBase<OrderHistory>, IOrderHistoryRepository
    {
        public OrderHistoryRepository(RepositoryContext context) : base(context)
        {
        }

        public async Task<IEnumerable<OrderHistory>> GetAllByOrderIdAsync(int orderId, bool trackChanges)
        {
            var orderHistory = await FindAllByCondition(oh => oh.OrderId == orderId, trackChanges)
                .Include(oh => oh.CreatedByUser)
                .OrderBy(oh => oh.CreatedAt)
                .ToListAsync();

            return orderHistory;
        }

        public void Create(OrderHistory orderHistory) => CreateEntity(orderHistory);
    }
}
