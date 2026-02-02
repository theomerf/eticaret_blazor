using Application.Common.Models;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface IPaymentProvider
    {
        Task<OperationResult<IyzicoCheckoutFormInitResponse>> CreatePaymentAsync(IyzicoCheckoutFormInitRequest request);
        Task<OperationResult<IyzicoCheckoutFormRetrieveResponse>> VerifyPaymentAsync(string token);
        Task<OperationResult<IyzicoRefundResponse>> RefundPaymentAsync(IyzicoRefundRequest request);
        Task<OperationResult<IyzicoBinCheckResponse>> GetBinDetailsAsync(string binNumber);
    }
}
