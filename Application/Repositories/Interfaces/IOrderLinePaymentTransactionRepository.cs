using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface IOrderLinePaymentTransactionRepository
    {
        Task<OrderLinePaymentTransaction?> GetByOrderLineIdAsync(int orderLineId, bool trackChanges);
        Task<IEnumerable<OrderLinePaymentTransaction?>> GetByOrderIdAsync(int orderId, bool trackChanges);
        Task<bool> ExistsAsync(int orderLineId);
        void Create(OrderLinePaymentTransaction transaction);
        void Update(OrderLinePaymentTransaction transaction);
    }
}
