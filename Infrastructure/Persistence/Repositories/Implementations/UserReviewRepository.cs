using Application.Repositories.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Implementations
{
    public sealed class UserReviewRepository : RepositoryBase<UserReview>, IUserReviewRepository
    {
        public UserReviewRepository(RepositoryContext context) : base(context)
        {
        }

        public async Task<IEnumerable<UserReview>> GetAllAsync(bool trackChanges) 
        {
            var reviews = await FindAll(trackChanges)
                .ToListAsync();

            return reviews;
        } 

        public async Task<int> CountAsync(CancellationToken ct = default) => await CountAsync(false, ct);

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

        public void Create(UserReview userReview)
        {
            CreateEntity(userReview);
        }

        public void Update(UserReview userReview)
        {
            UpdateEntity(userReview);
        }

        public void Delete(UserReview userReview)
        {
            RemoveEntity(userReview);
        }
    }
}
