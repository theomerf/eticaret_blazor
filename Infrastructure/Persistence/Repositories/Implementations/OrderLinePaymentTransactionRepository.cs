using Application.Repositories.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Implementations
{
    public class OrderLinePaymentTransactionRepository : RepositoryBase<OrderLinePaymentTransaction>, IOrderLinePaymentTransactionRepository
    {
        public OrderLinePaymentTransactionRepository(RepositoryContext context) : base(context)
        {
        }

        public async Task<OrderLinePaymentTransaction?> GetByOrderLineIdAsync(int orderLineId, bool trackChanges)
        {
            var orderLinePaymentTransaction = await FindByCondition(t => t.OrderLineId == orderLineId, trackChanges)
                .Include(t => t.OrderLine)
                .FirstOrDefaultAsync();

            return orderLinePaymentTransaction;
        }

        public async Task<IEnumerable<OrderLinePaymentTransaction?>> GetByOrderIdAsync(int orderId, bool trackChanges)
        {
            var orderLinePaymentTransactions = await FindAll(trackChanges)
                .Include(t => t.OrderLine)
                .Where(t => t.OrderLine.OrderId == orderId)
                .ToListAsync();

            return orderLinePaymentTransactions;
        }

        public async Task<bool> ExistsAsync(int orderLineId)
        {
            return await _context.OrderLinePaymentTransactions
                .AnyAsync(t => t.OrderLineId == orderLineId);
        }

        public void Create(OrderLinePaymentTransaction transaction)
        {
            CreateEntity(transaction);
        }

        public void Update(OrderLinePaymentTransaction transaction)
        {
            UpdateEntity(transaction);
        }
    }
}
