using Application.Common.Models;
using Application.DTOs;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface IOrderService
    {
        Task<OperationResult<int>> CreateOrderAsync(OrderDtoForCreation orderDto, string userId);
        Task<OperationResult<OrderWithDetailsDto>> GetOrderByIdAsync(int orderId, string userId);
        Task<OperationResult<OrderWithDetailsDto>> GetOrderByNumberAsync(string orderNumber, string userId);
        Task<OperationResult<IEnumerable<OrderDto>>> GetUserOrdersAsync(string userId);
        Task<OperationResult<OrderWithDetailsDto>> UpdateOrderStatusAsync(OrderDtoForUpdate orderDto);

        Task<OperationResult<OrderWithDetailsDto>> CancelOrderAsync(int orderId, string reason, string userId);
        Task<OperationResult<OrderWithDetailsDto>> MarkAsShippedAsync(int orderId, string trackingNumber, string? companyName, string? serviceName);
        Task<OperationResult<OrderWithDetailsDto>> MarkAsDeliveredAsync(int orderId);

        // Payment
        Task<OperationResult<PaymentResponse>> InitiatePaymentAsync(int orderId, string userId);
        Task<OperationResult<OrderWithDetailsDto>> HandlePaymentCallbackAsync(PaymentCallbackDto callback);
        Task<OperationResult<OrderWithDetailsDto>> RefundOrderAsync(int orderId);

        Task<int> GetUserOrdersCountAsync(string userId);
        Task<decimal> GetUserTotalSpentAsync(string userId);
    }
}
