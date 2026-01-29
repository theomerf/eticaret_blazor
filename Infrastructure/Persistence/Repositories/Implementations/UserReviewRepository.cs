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

        public async Task<IEnumerable<UserReview>> GetAllUserReviewsAsync(bool trackChanges) 
        {
            var reviews = await FindAll(trackChanges)
                .ToListAsync();

            return reviews;
        } 

        public async Task<int> GetUserReviewsCountAsync(bool trackChanges) => await CountAsync(trackChanges);

        public async Task<UserReview?> GetOneUserReviewAsync(int id, bool trackChanges)
        {
            var review = await FindByCondition(p => p.UserReviewId.Equals(id), trackChanges).SingleOrDefaultAsync();

            return review;
        }

        public async Task<IEnumerable<UserReview>> GetAllUserReviewsOfOneProductAdminAsync(int id, bool trackChanges)
        {
            var reviews = await FindAll(trackChanges)
                .Where(p => p.ProductId.Equals(id))
                .ToListAsync();

            return reviews;
        }

        public async Task<IEnumerable<UserReview>> GetAllUserReviewsOfOneProductAsync(int id, bool trackChanges)
        {
            var reviews = await FindAll(trackChanges)
                .Where(p => p.ProductId.Equals(id) && p.IsApproved)
                .ToListAsync();

            return reviews;
        }

        public async Task<IEnumerable<UserReview>> GetAllUserReviewsOfOneUserAsync(string id, bool trackChanges)
        {
            var reviews = await FindAll(trackChanges)
                .Where(u => u.UserId.Equals(id))
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

        public void CreateUserReview(UserReview userReview)
        {
            Create(userReview);
        }

        public void UpdateOneUserReview(UserReview userReview)
        {
            Update(userReview);
        }

        public void DeleteOneUserReview(UserReview userReview)
        {
            Remove(userReview);
        }
    }
}
