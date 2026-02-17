using Application.Common.Models;
using Application.DTOs;
using Application.Queries.RequestParameters;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface ICouponService
    {
        Task<(IEnumerable<CouponDto> coupons, int count, int activeCount)> GetAllAdminAsync(CouponRequestParametersAdmin p, CancellationToken ct = default);
        Task<int> CountOfActiveAsync(CancellationToken ct = default);
        Task<OperationResult<CouponDto>> GetByIdAsync(int couponId);
        Task<OperationResult<CouponDto>> GetByCodeAsync(string code);

        Task<OperationResult<int>> CreateAsync(CouponDtoForCreation couponDto);
        Task<OperationResult<CouponDto>> UpdateAsync(CouponDtoForUpdate couponDto);
        Task<OperationResult<CouponDto>> DeleteAsync(int couponId);

        Task<OperationResult<CouponDto>> ActivateAsync(int couponId);
        Task<OperationResult<CouponDto>> DeactivateAsync(int couponId);

        Task<OperationResult<decimal>> ValidateAndCalculateDiscountAsync(string code, decimal orderAmount, string userId);
        Task<OperationResult<Coupon>> ValidateForOrderAsync(string code, decimal orderAmount, string userId);
        Task<IEnumerable<CouponUsage>> GetCouponUsagesByCouponIdAsync(int couponId);
        Task<IEnumerable<CouponUsage>> GetCouponUsagesByUserIdAsync(string userId);
    }
}
