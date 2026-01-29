using Application.DTOs;

namespace Application.Services.Interfaces
{
    public interface IOrderService
    {
        Task<IEnumerable<OrderDto>> GetAllOrdersAsync();
        Task<OrderDto?> GetOneOrderAsync(int id);
        Task CompleteAsync(int id);
        Task SaveOrderAsync(OrderDto order);
        Task<int> GetNumberOfInProcessAsync();
        Task<IEnumerable<OrderDto>> GetOrdersOfUserAsync(string? userId);
        Task<IEnumerable<DailySalesDto>> GetLast30DaysSalesDataAsync();
    }
}
