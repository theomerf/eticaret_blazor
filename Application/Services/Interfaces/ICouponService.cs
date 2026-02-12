using Application.Common.Models;
using Application.DTOs;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface ICouponService
    {
        Task<OperationResult<IEnumerable<CouponDto>>> GetAllAsync();
        Task<OperationResult<CouponDto>> GetByIdAsync(int couponId);
        Task<OperationResult<CouponDto>> GetByCodeAsync(string code);

        Task<OperationResult<int>> CreateAsync(CouponDtoForCreation couponDto);
        Task<OperationResult<CouponDto>> UpdateAsync(CouponDtoForUpdate couponDto);
        Task<OperationResult<CouponDto>> DeleteAsync(int couponId);

        Task<OperationResult<CouponDto>> ActivateAsync(int couponId);
        Task<OperationResult<CouponDto>> DeactivateAsync(int couponId);

        Task<OperationResult<decimal>> ValidateAndCalculateDiscountAsync(string code, decimal orderAmount, string userId);
        Task<OperationResult<Coupon>> ValidateForOrderAsync(string code, decimal orderAmount, string userId);
    }
}
