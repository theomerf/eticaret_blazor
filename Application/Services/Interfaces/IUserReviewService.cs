using Application.DTOs;
using Application.Queries.RequestParameters;
using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface IUserReviewService
    {
        Task<(IEnumerable<UserReviewDto> reviews, int count, int approvedCount)> GetAllAdminAsync(UserReviewRequestParametersAdmin p, bool trackChanges, CancellationToken ct = default);
        Task<int> CountAsync(CancellationToken ct = default);
        Task<int> CountApprovedAsync(CancellationToken ct = default);
        Task<UserReviewDto> GetByIdAsync(int userReviewId);
        Task<IEnumerable<UserReviewDto>> GetByProductIdAdminAsync(int productId);
        Task<IEnumerable<UserReviewDto>> GetByProductIdAsync(int productId);
        Task<IEnumerable<UserReviewDto>> GetByUserIdAsync(string userId);
        Task<OperationResult<UserReviewDto>> CreateAsync(UserReviewDtoForCreation userReview);
        Task<OperationResult<UserReviewDto>> ApproveAsync(int userReviewId, CancellationToken ct = default);
        Task<OperationResult<UserReviewDto>> UnapproveAsync(int userReviewId, CancellationToken ct = default);
        Task<OperationResult<UserReviewDto>> UpdateFeaturedStatusAsync(int userReviewId);
        Task<OperationResult<UserReviewDto>> DeleteAsync(int userReviewId, CancellationToken ct = default);
        Task<OperationResult<UserReviewDto>> DeleteAdminAsync(int userReviewId, CancellationToken ct = default);
        Task<OperationResult<UserReviewDto>> UpdateAsync(UserReviewDtoForUpdate userReview, CancellationToken ct = default);
        Task<OperationResult<(VoteType?, int, int)>> SetVoteAsync(int userReviewId, VoteType? desired, CancellationToken ct = default);
    }
}
