using Application.DTOs;
using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface IOrderRepository
    {
        Task<Order?> GetOrderByIdAsync(int orderId, bool trackChanges);
        Task<Order?> GetOrderByNumberAsync(string orderNumber, bool trackChanges);
        Task<Order?> GetOrderWithDetailsAsync(int orderId, bool trackChanges);
        Task<IEnumerable<Order>> GetUserOrdersAsync(string userId, bool trackChanges);
        Task<IEnumerable<Order>> GetOrdersByStatusAsync(OrderStatus status, bool trackChanges);
        Task<IEnumerable<Order>> GetOrdersByPaymentStatusAsync(PaymentStatus status, bool trackChanges);
        Task<(IEnumerable<Order> orders, int count)> GetOrdersPagedAsync(int pageNumber, int pageSize, bool trackChanges);
        Task<IEnumerable<Order>> GetPendingPaymentOrdersAsync(bool trackChanges);

        Task<int> GetOrdersCountAsync();
        Task<int> GetUserOrdersCountAsync(string userId);
        Task<decimal> GetTotalRevenueAsync();
        Task<decimal> GetUserTotalSpentAsync(string userId);
        Task<IEnumerable<DailySalesDto>> GetDailySalesAsync(DateTime startDate, DateTime endDate);

        void CreateOrder(Order order);
        void UpdateOrder(Order order);
        void DeleteOrder(Order order);
    }
}
