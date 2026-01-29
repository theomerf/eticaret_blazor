using Application.DTOs;

namespace ETicaret.Models
{
    public class ProductReviewsViewModel
    {
        public UserReviewDtoForCreation? UserReviewForCreation { get; set; }
        public IEnumerable<UserReviewDto>? UserReviews { get; set; }
        public int ProductId { get; set; }
    }
}
