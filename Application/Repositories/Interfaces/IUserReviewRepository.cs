using Application.Queries.RequestParameters;
using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface IUserReviewRepository : IRepositoryBase<UserReview>
    {
        Task<(IEnumerable<UserReview> reviews, int count, int approvedCount)> GetAllAdminAsync(UserReviewRequestParametersAdmin p, bool trackChanges, CancellationToken ct = default);
        Task<UserReview?> GetByIdAsync(int userReviewId, bool trackChanges);
        Task<int> CountAsync(CancellationToken ct = default);
        Task<int> CountApprovedAsync(CancellationToken ct = default);
        Task<IEnumerable<UserReview>> GetByProductIdAsync(int productId, bool trackChanges);
        Task<IEnumerable<UserReview>> GetByProductIdAdminAsync(int productId, bool trackChanges);
        Task<IEnumerable<UserReview>> GetByUserIdAsync(string userId, bool trackChanges);
        Task<IEnumerable<UserReviewVote>> GetVotesByUserReviewIdAsync(int userReviewId, bool trackChanges);
        Task<IEnumerable<UserReviewVote>> GetVotesByUserIdAsync(string userId, bool trackChanges);
        Task<UserReviewVote?> GetVoteByUserIdAndReviewIdAsync(string userId, int userReviewId, bool trackChanges);
        void AddVote(UserReviewVote vote);
        void UpdateVote(UserReviewVote vote);
        void DeleteVote(UserReviewVote vote);
        void Create(UserReview userReview);
        void Update(UserReview entity);
        void Delete(UserReview userReview);
    }
}
