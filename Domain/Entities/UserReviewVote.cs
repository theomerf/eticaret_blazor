namespace Domain.Entities
{
    public class UserReviewVote
    {
        public string UserId { get; set; } = null!;
        public User? User { get; set; }
        public int UserReviewId { get; set; }
        public UserReview? UserReview { get; set; }
        public VoteType VoteType { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set;} = DateTime.UtcNow;
    }

    public enum VoteType
    {
        Helpful = 1,
        NotHelpful = 2
    }
}
