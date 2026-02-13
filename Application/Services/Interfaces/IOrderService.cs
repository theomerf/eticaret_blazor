using Application.Common.Models;
using Application.DTOs;
using Application.Queries.RequestParameters;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface IOrderService
    {
        Task<IEnumerable<OrderDto>> GetByUserIdAsync(string userId);
        Task<OrderWithDetailsDto> GetByIdAsync(int orderId);
        Task<OrderWithDetailsDto> GetByNumberAsync(string orderNumber);
        Task<OperationResult<int>> CreateAsync(OrderDtoForCreation orderDto);
        Task<OperationResult<OrderWithDetailsDto>> UpdateStatusAsync(OrderDtoForUpdate orderDto);

        Task<OperationResult<OrderWithDetailsDto>> CancelAsync(int orderId, string reason);
        Task<OperationResult<OrderWithDetailsDto>> MarkAsShippedAsync(int orderId, string trackingNumber, string? companyName, string? serviceName);
        Task<OperationResult<OrderWithDetailsDto>> MarkAsDeliveredAsync(int orderId);

        // Payment
        Task<OperationResult<OrderWithDetailsDto>> HandlePaymentCallbackAsync(PaymentCallbackDto callback);
        Task<OperationResult<OrderWithDetailsDto>> RefundAsync(int orderId);

        Task<int> CountByUserIdAsync(string userId);
        Task<decimal> GetUserTotalSpentAsync(string userId);
        Task<IEnumerable<ProductSalesDto>> GetTopSellingProductsAsync(int topN, CancellationToken ct = default);
        Task<OperationResult<(IEnumerable<OrderDto> orders, int count)>> GetAllAdminAsync(OrderFilterParametersAdmin p);

        Task<int> CountOfInProcessAsync(CancellationToken ct = default);
    }
}
