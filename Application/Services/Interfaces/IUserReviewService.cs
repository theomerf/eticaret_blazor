using Application.DTOs;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface IUserReviewService
    {
        Task<IEnumerable<UserReviewDto>> GetAllUserReviewsAsync();
        Task<int> GetCountAsync();
        Task<UserReviewDto> GetOneUserReviewAsync(int id);
        Task<IEnumerable<UserReviewDto>> GetAllUserReviewsOfOneProductAdminAsync(int id);
        Task<IEnumerable<UserReviewDto>> GetAllUserReviewsOfOneProductAsync(int id);
        Task<IEnumerable<UserReviewDto>> GetAllUserReviewsOfOneUserAsync(string id);
        Task<OperationResult<UserReviewDto>> CreateUserReviewAsync(UserReviewDtoForCreation userReview);
        Task<OperationResult<UserReviewDto>> ApproveUserReviewAsync(int id);
        Task<OperationResult<UserReviewDto>> UpdateUserReviewFeaturedStatusAsync(int id);
        Task<OperationResult<UserReviewDto>> DeleteUserReviewAsync(int id);
        Task<OperationResult<UserReviewDto>> DeleteUserReviewForAdminAsync(int id);
        Task<OperationResult<UserReviewDto>> UpdateUserReviewAsync(UserReviewDtoForUpdate userReview);
    }
}
