using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface IOrderHistoryRepository
    {
        Task<IEnumerable<OrderHistory>> GetAllByOrderIdAsync(int orderId, bool trackChanges);
        void Create(OrderHistory orderHistory);
    }
}
