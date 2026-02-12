using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface ICouponUsageRepository
    {
        Task<IEnumerable<CouponUsage>> GetAllAsync(int couponId, bool trackChanges);
        Task<IEnumerable<CouponUsage>> GetByUserIdAsync(string userId, bool trackChanges);
        Task<CouponUsage?> GetByUserIdForCouponAsync(int couponId, string userId, bool trackChanges);
        void Create(CouponUsage couponUsage);
    }
}
