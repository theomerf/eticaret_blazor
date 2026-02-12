using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface ICouponRepository
    {
        Task<IEnumerable<Coupon>> GetAllAsync(bool trackChanges);
        Task<Coupon?> GetByIdAsync(int couponId, bool trackChanges);
        Task<Coupon?> GetByCodeAsync(string code, bool trackChanges);
        Task<Coupon?> GetWithUsagesAsync(int couponId, bool trackChanges);
        Task<IEnumerable<Coupon>> GetActiveAsync(bool trackChanges);
        Task<(IEnumerable<Coupon> coupons, int count)> GetPagedAsync(int pageNumber, int pageSize, bool trackChanges);

        Task<bool> IsCouponCodeUniqueAsync(string code, int? excludeCouponId = null);
        Task<int> GetUserCouponUsageCountAsync(int couponId, string userId);

        void Create(Coupon coupon);
        void Update(Coupon coupon);
        void Delete(Coupon coupon);
    }
}
