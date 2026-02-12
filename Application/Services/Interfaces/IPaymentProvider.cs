using Application.Common.Models;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface IPaymentProvider
    {
        Task<OperationResult<IyzicoCheckoutFormInitResponse>> CreateAsync(IyzicoCheckoutFormInitRequest request);
        Task<OperationResult<IyzicoCheckoutFormRetrieveResponse>> VerifyAsync(string token);
        Task<OperationResult<IyzicoRefundResponse>> RefundAsync(IyzicoRefundRequest request);
        Task<OperationResult<IyzicoBinCheckResponse>> GetBinDetailsAsync(string binNumber);
    }
}
