using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface IUserReviewRepository : IRepositoryBase<UserReview>
    {
        Task<IEnumerable<UserReview>> GetAllAsync(bool trackChanges);
        Task<UserReview?> GetByIdAsync(int userReviewId, bool trackChanges);
        Task<int> CountAsync(CancellationToken ct = default);
        Task<IEnumerable<UserReview>> GetByProductIdAsync(int productId, bool trackChanges);
        Task<IEnumerable<UserReview>> GetByProductIdAdminAsync(int productId, bool trackChanges);
        Task<IEnumerable<UserReview>> GetByUserIdAsync(string userId, bool trackChanges);
        void Create(UserReview userReview);
        void Update(UserReview entity);
        void Delete(UserReview userReview);
    }
}
