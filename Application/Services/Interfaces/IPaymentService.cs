using Application.Common.Models;
using Application.DTOs;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<OperationResult<PaymentResponse>> InitiatePaymentAsync(PaymentRequest request);
        Task<OperationResult<PaymentResponse>> VerifyPaymentAsync(string transactionId);
        Task<OperationResult<PaymentResponse>> RefundPaymentAsync(string transactionId, decimal amount);
        Task<OperationResult<bool>> ValidateCallbackAsync(PaymentCallbackDto callback);
    }
}
