using Application.Common.Models;
using Application.DTOs;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface ICouponService
    {
        Task<OperationResult<IEnumerable<CouponDto>>> GetAllCouponsAsync();
        Task<OperationResult<CouponDto>> GetCouponByIdAsync(int couponId);
        Task<OperationResult<CouponDto>> GetCouponByCodeAsync(string code);

        Task<OperationResult<int>> CreateCouponAsync(CouponDtoForCreation couponDto);
        Task<OperationResult<CouponDto>> UpdateCouponAsync(CouponDtoForUpdate couponDto);
        Task<OperationResult<CouponDto>> DeleteCouponAsync(int couponId);

        Task<OperationResult<CouponDto>> ActivateCouponAsync(int couponId);
        Task<OperationResult<CouponDto>> DeactivateCouponAsync(int couponId);

        Task<OperationResult<decimal>> ValidateAndCalculateDiscountAsync(string code, decimal orderAmount, string userId);
        Task<OperationResult<Coupon>> ValidateCouponForOrderAsync(string code, decimal orderAmount, string userId);
    }
}
