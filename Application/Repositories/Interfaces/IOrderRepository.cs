using Application.DTOs;
using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface IOrderRepository
    {
        Task<IEnumerable<Order>> GetAllOrdersAsync();
        Task<Order?> GetOneOrderAsync(int id);
        Task CompleteAsync(int id);
        Task SaveOrderAsync(Order order);
        Task<int> GetNumberOfInProcessAsync();
        Task<IEnumerable<Order>> GetOrdersOfUserAsync(string? userId);
        Task<IEnumerable<DailySalesDto>> GetLast30DaysSalesDataAsync();
    }

}
