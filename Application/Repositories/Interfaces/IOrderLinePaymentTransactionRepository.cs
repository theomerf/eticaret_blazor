using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface IOrderLinePaymentTransactionRepository
    {
        Task<OrderLinePaymentTransaction?> GetByOrderLineIdAsync(int orderLineId, bool trackChanges);
        Task<IEnumerable<OrderLinePaymentTransaction?>> GetByOrderIdAsync(int orderId, bool trackChanges);
        void CreateOrderLinePaymentTransaction(OrderLinePaymentTransaction transaction);
        void UpdateOrderLinePaymentTransaction(OrderLinePaymentTransaction transaction);
        Task<bool> ExistsAsync(int orderLineId);
    }
}
