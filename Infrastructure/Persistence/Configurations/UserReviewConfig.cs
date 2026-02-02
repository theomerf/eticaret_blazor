using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class UserReviewConfig : IEntityTypeConfiguration<UserReview>
    {
        public void Configure(EntityTypeBuilder<UserReview> builder)
        {
            builder.HasKey(ur => ur.UserReviewId);

            builder.Property(ur => ur.ReviewTitle)
                .HasMaxLength(200);

            builder.Property(ur => ur.ReviewText)
                .HasMaxLength(2000);

            builder.Property(ur => ur.ReviewDate)
                .IsRequired();

            builder.Property(ur => ur.ReviewPictureUrl)
                .HasMaxLength(500);

            builder.Property(ur => ur.ReviewerName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(ur => ur.DeletedByUserId)
                .HasMaxLength(450);

            builder.HasQueryFilter(ur => !ur.IsDeleted);

            builder.HasIndex(r => r.ProductId)
                .HasDatabaseName("IX_UserReviews_ProductId");

            builder.HasIndex(r => r.UserId)
                .HasDatabaseName("IX_UserReviews_UserId");

            builder.HasIndex(r => new { r.ProductId, r.IsApproved, r.IsDeleted })
                .HasFilter("\"IsApproved\" = true AND \"IsDeleted\" = false")
                .HasDatabaseName("IX_UserReviews_Product_Approved");

            builder.HasIndex(r => r.ReviewDate)
                .IsDescending()
                .HasDatabaseName("IX_UserReviews_ReviewDate_Desc");

            builder.HasOne(ur => ur.User)
                .WithMany(u => u.UserReviews)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ur => ur.Product)
                .WithMany(p => p.UserReviews)
                .HasForeignKey(ur => ur.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
