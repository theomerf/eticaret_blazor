using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface ICouponRepository
    {
        Task<Coupon?> GetCouponByIdAsync(int couponId, bool trackChanges);
        Task<Coupon?> GetCouponByCodeAsync(string code, bool trackChanges);
        Task<Coupon?> GetCouponWithUsagesAsync(int couponId, bool trackChanges);
        Task<IEnumerable<Coupon>> GetActiveCouponsAsync(bool trackChanges);
        Task<IEnumerable<Coupon>> GetAllCouponsAsync(bool trackChanges);
        Task<(IEnumerable<Coupon> coupons, int count)> GetCouponsPagedAsync(int pageNumber, int pageSize, bool trackChanges);

        Task<bool> IsCouponCodeUniqueAsync(string code, int? excludeCouponId = null);
        Task<int> GetUserCouponUsageCountAsync(int couponId, string userId);

        void CreateCoupon(Coupon coupon);
        void UpdateCoupon(Coupon coupon);
        void DeleteCoupon(Coupon coupon);
    }
}
