using Application.DTOs;
using Application.Queries.RequestParameters;

namespace ETicaret.Models
{
    public class UserReviewListViewModelAdmin
    {
        public IEnumerable<UserReviewDto> UserReviews { get; set; } = [];
        public Pagination Pagination { get; set; } = new();
        public int TotalCount { get; set; }
        public UserReviewRequestParametersAdmin FilterParams { get; set; } = new();
    }
}
