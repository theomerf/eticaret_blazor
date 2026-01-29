using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface IUserReviewRepository : IRepositoryBase<UserReview>
    {
        Task<IEnumerable<UserReview>> GetAllUserReviewsAsync(bool trackChanges);
        Task<int> GetUserReviewsCountAsync(bool trackChanges);
        Task<UserReview?> GetOneUserReviewAsync(int id, bool trackChanges);
        Task<IEnumerable<UserReview>> GetAllUserReviewsOfOneProductAsync(int id, bool trackChanges);
        Task<IEnumerable<UserReview>> GetAllUserReviewsOfOneProductAdminAsync(int id, bool trackChanges);
        Task<IEnumerable<UserReview>> GetAllUserReviewsOfOneUserAsync(string id, bool trackChanges);
        void CreateUserReview(UserReview userReview);
        void DeleteOneUserReview(UserReview userReview);
        void UpdateOneUserReview(UserReview entity);
    }
}
