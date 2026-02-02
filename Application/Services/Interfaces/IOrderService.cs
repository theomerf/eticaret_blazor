using Application.Common.Models;
using Application.DTOs;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface IOrderService
    {
        Task<IEnumerable<OrderDto>> GetUserOrdersAsync(string userId);
        Task<OrderWithDetailsDto> GetOrderByIdAsync(int orderId);
        Task<OrderWithDetailsDto> GetOrderByNumberAsync(string orderNumber);
        Task<OperationResult<int>> CreateOrderAsync(OrderDtoForCreation orderDto);
        Task<OperationResult<OrderWithDetailsDto>> UpdateOrderStatusAsync(OrderDtoForUpdate orderDto);

        Task<OperationResult<OrderWithDetailsDto>> CancelOrderAsync(int orderId, string reason);
        Task<OperationResult<OrderWithDetailsDto>> MarkAsShippedAsync(int orderId, string trackingNumber, string? companyName, string? serviceName);
        Task<OperationResult<OrderWithDetailsDto>> MarkAsDeliveredAsync(int orderId);

        // Payment
        Task<OperationResult<OrderWithDetailsDto>> HandlePaymentCallbackAsync(PaymentCallbackDto callback);
        Task<OperationResult<OrderWithDetailsDto>> RefundOrderAsync(int orderId);

        Task<int> GetUserOrdersCountAsync(string userId);
        Task<decimal> GetUserTotalSpentAsync(string userId);
    }
}
