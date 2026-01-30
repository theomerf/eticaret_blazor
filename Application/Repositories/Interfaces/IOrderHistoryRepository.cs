using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface IOrderHistoryRepository
    {
        Task<IEnumerable<OrderHistory>> GetOrderHistoryAsync(int orderId, bool trackChanges);
        void CreateOrderHistory(OrderHistory orderHistory);
    }
}
