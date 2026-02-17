using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class UserReviewVoteConfig : IEntityTypeConfiguration<UserReviewVote>
    {
        public void Configure(EntityTypeBuilder<UserReviewVote> builder)
        {
            builder.HasKey(x => new { x.UserId, x.UserReviewId });

            builder.HasOne(x => x.UserReview)
                .WithMany(r => r.Votes)
                .HasForeignKey(x => x.UserReviewId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User) 
                .WithMany(u => u.Votes)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(x => !x.User!.IsDeleted && !x.UserReview!.IsDeleted);

            builder.HasIndex(x => x.UserReviewId)
                .HasDatabaseName("IX_UserReviewVotes_UserReviewId");

            builder.HasIndex(x => x.UserId)
                .HasDatabaseName("IX_UserReviewVotes_UserId");
        }
    }
}
