using Application.DTOs;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface IUserReviewService
    {
        Task<IEnumerable<UserReviewDto>> GetAllAsync();
        Task<int> CountAsync(CancellationToken ct = default);
        Task<UserReviewDto> GetByIdAsync(int userReviewId);
        Task<IEnumerable<UserReviewDto>> GetByProductIdAdminAsync(int productId);
        Task<IEnumerable<UserReviewDto>> GetByProductIdAsync(int productId);
        Task<IEnumerable<UserReviewDto>> GetByUserIdAsync(string userId);
        Task<OperationResult<UserReviewDto>> CreateAsync(UserReviewDtoForCreation userReview);
        Task<OperationResult<UserReviewDto>> ApproveAsync(int userReviewId);
        Task<OperationResult<UserReviewDto>> UpdateFeaturedStatusAsync(int userReviewId);
        Task<OperationResult<UserReviewDto>> DeleteAsync(int userReviewId);
        Task<OperationResult<UserReviewDto>> DeleteAdminAsync(int userReviewId);
        Task<OperationResult<UserReviewDto>> UpdateAsync(UserReviewDtoForUpdate userReview);
    }
}
