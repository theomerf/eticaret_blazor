using Application.Queries.RequestParameters;
using Application.Repositories.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Implementations
{
    public sealed class UserReviewRepository : RepositoryBase<UserReview>, IUserReviewRepository
    {
        public UserReviewRepository(RepositoryContext context) : base(context)
        {
        }

        public async Task<(IEnumerable<UserReview> reviews, int count, int approvedCount)> GetAllAdminAsync(UserReviewRequestParametersAdmin p, bool trackChanges, CancellationToken ct = default)
        {
            var query = FindAll(trackChanges)
                .Include(r => r.Product)
                .FilterBy(p.IsApproved, r => r.IsApproved, FilterOperator.Equal)
                .FilterBy(p.IsFeatured, r => r.IsFeatured, FilterOperator.Equal)
                .FilterBy(p.StartDate, r => r.ReviewDate, FilterOperator.GreaterThanOrEqual)
                .FilterBy(p.EndDate, r => r.ReviewDate, FilterOperator.LessThanOrEqual);

            if (!string.IsNullOrWhiteSpace(p.SearchTerm))
            {
                var searchLower = p.SearchTerm.ToLower();
                query = query.Where(u =>
                    u.ProductId.ToString().ToLower().Contains(searchLower) ||
                    u.ReviewerName.ToLower().Contains(searchLower));
            }

            var count = await query.CountAsync(ct);
            var approvedCount = await query.CountAsync(r => r.IsApproved, ct);

            query = p.SortBy switch
            {
                "date_asc" => query.OrderBy(u => u.ReviewDate),
                "rating_desc" => query.OrderByDescending(u => u.Rating),
                "rating_asc" => query.OrderBy(u => u.Rating),
                "helpful_asc" => query.OrderBy(u => u.HelpfulCount),
                "helpful_desc" => query.OrderByDescending(u => u.HelpfulCount),
                "nothelpful_asc" => query.OrderBy(u => u.NotHelpfulCount),
                "nothelpful_desc" => query.OrderByDescending(u => u.NotHelpfulCount),
                _ => query.OrderByDescending(u => u.ReviewDate)
            };

            var userReviews = await query
                .ToPaginate(p.PageNumber, p.PageSize)
                .ToListAsync(ct);

            return (userReviews, count, approvedCount);
        } 

        public async Task<int> CountAsync(CancellationToken ct = default) => await CountAsync(false, ct);
        public async Task<int> CountApprovedAsync(CancellationToken ct = default)
            => await FindAll(false).CountAsync(r => r.IsApproved, ct);

        public async Task<UserReview?> GetByIdAsync(int userReviewId, bool trackChanges)
        {
            var review = await FindByCondition(p => p.UserReviewId.Equals(userReviewId), trackChanges).SingleOrDefaultAsync();

            return review;
        }

        public async Task<IEnumerable<UserReview>> GetByProductIdAdminAsync(int productId, bool trackChanges)
        {
            var reviews = await FindAll(trackChanges)
                .Where(p => p.ProductId.Equals(productId))
                .ToListAsync();

            return reviews;
        }

        public async Task<IEnumerable<UserReview>> GetByProductIdAsync(int productId, bool trackChanges)
        {
            var reviews = await FindAll(trackChanges)
                .Where(p => p.ProductId.Equals(productId) && p.IsApproved)
                .ToListAsync();

            return reviews;
        }

        public async Task<IEnumerable<UserReview>> GetByUserIdAsync(string userId, bool trackChanges)
        {
            var reviews = await FindAll(trackChanges)
                .Where(u => u.UserId.Equals(userId))
                .Select(u => new UserReview
                {
                    UserReviewId = u.UserReviewId,
                    Rating = u.Rating,
                    ReviewTitle = u.ReviewTitle,
                    ReviewText = u.ReviewText,
                    ReviewDate = u.ReviewDate,
                    ReviewUpdateDate = u.ReviewUpdateDate,
                    ReviewPictureUrl = u.ReviewPictureUrl,
                    IsApproved = u.IsApproved,
                    IsFeatured = u.IsFeatured,
                    ReviewerName = u.ReviewerName,
                    HelpfulCount = u.HelpfulCount,
                    NotHelpfulCount = u.NotHelpfulCount,
                    UserId = u.UserId,
                    ProductId = u.ProductId,
                    Product = new Product
                    {
                        ProductName = u.Product != null ? u.Product.ProductName : string.Empty,
                    }
                })
                .ToListAsync();

            return reviews;
        }

        public async Task<IEnumerable<UserReviewVote>> GetVotesByUserReviewIdAsync(int userReviewId, bool trackChanges)
        {
            var query = _context.UserReviewVotes
                .Where(v => v.UserReviewId == userReviewId);

            return await (trackChanges ? query.ToListAsync() : query.AsNoTracking().ToListAsync());
        }

        public async Task<IEnumerable<UserReviewVote>> GetVotesByUserIdAsync(string userId, bool trackChanges)
        {
            var query = _context.UserReviewVotes
                .Where(v => v.UserId == userId);

            return await (trackChanges ? query.ToListAsync() : query.AsNoTracking().ToListAsync());
        }

        public async Task<UserReviewVote?> GetVoteByUserIdAndReviewIdAsync(string userId, int userReviewId, bool trackChanges)
        {
            var query = _context.UserReviewVotes
                .Where(v => v.UserId == userId && v.UserReviewId == userReviewId);

            return await (trackChanges ? query.SingleOrDefaultAsync() : query.AsNoTracking().SingleOrDefaultAsync());
        }

        public void AddVote(UserReviewVote vote) => _context.UserReviewVotes.Add(vote);
        public void UpdateVote(UserReviewVote vote) => _context.UserReviewVotes.Update(vote);
        public void DeleteVote(UserReviewVote vote) => _context.UserReviewVotes.Remove(vote);

        public void Create(UserReview userReview) => CreateEntity(userReview);

        public void Update(UserReview userReview) => UpdateEntity(userReview);

        public void Delete(UserReview userReview) => RemoveEntity(userReview);
    }
}
