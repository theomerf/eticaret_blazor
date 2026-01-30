using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface ICouponUsageRepository
    {
        Task<IEnumerable<CouponUsage>> GetCouponUsagesAsync(int couponId, bool trackChanges);
        Task<IEnumerable<CouponUsage>> GetUserCouponUsagesAsync(string userId, bool trackChanges);
        Task<CouponUsage?> GetUserCouponUsageAsync(int couponId, string userId, bool trackChanges);
        void CreateCouponUsage(CouponUsage couponUsage);
    }
}
