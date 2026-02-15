using Application.DTOs;
using Application.Queries.RequestParameters;
using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface IOrderRepository
    {
        Task<(IEnumerable<Order> orders, int count)> GetAllAdminAsync(OrderRequestParametersAdmin p, bool trackChanges, CancellationToken ct = default);
        Task<Order?> GetByIdAsync(int orderId, bool trackChanges);
        Task<Order?> GetByNumberAsync(string orderNumber, bool trackChanges);
        Task<Order?> GetWithDetailsAsync(int orderId, bool trackChanges);
        Task<IEnumerable<Order>> GetByUserIdAsync(string userId, bool trackChanges);
        Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status, bool trackChanges);
        Task<IEnumerable<Order>> GetByPaymentStatusAsync(PaymentStatus status, bool trackChanges);
        Task<(IEnumerable<Order> orders, int count)> GetPagedAsync(int pageNumber, int pageSize, bool trackChanges);
        Task<IEnumerable<Order>> GetPaymentPendingAsync(bool trackChanges);

        Task<int> CountAsync();
        Task<int> CountByUserIdAsync(string userId);
        Task<int> CountOfInProcessAsync(CancellationToken ct = default);
        Task<decimal> GetTotalRevenueAsync();
        Task<decimal> GetUserTotalSpentAsync(string userId);
        Task<IEnumerable<DailySalesDto>> GetDailySalesAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<ProductSalesDto>> GetTopSellingProductsAsync(int topN, CancellationToken ct = default);

        void Create(Order order);
        void Update(Order order);
        void Delete(Order order);
    }
}
