using Application.DTOs;

namespace ETicaret.Models
{
    public class ProfileViewModel
    {
        public UserDto User { get; set; } = new();
        public IEnumerable<OrderDto> Orders { get; set; } = new List<OrderDto>();
        public IEnumerable<UserReviewDto> UserReviews { get; set; } = new List<UserReviewDto>();
        public UserDtoForUpdate UserDtoForUpdate { get; set; } = new();
        public ChangePasswordDto ChangePasswordDto { get; set; } = new();
        public UserReviewDtoForUpdate UserReviewDtoForUpdate { get; set; } = new();
    }
}
