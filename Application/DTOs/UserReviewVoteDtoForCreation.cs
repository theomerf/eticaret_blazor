using Domain.Entities;

namespace Application.DTOs
{
    public record UserReviewVoteDtoForCreation
    {
        public int UserReviewId { get; set; }
        public bool IsHelpful { get; set; }
    }
}